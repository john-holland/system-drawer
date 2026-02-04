using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Locomotion.Narrative.Serialization
{
    /// <summary>
    /// Word-level tokenizer for narrative LSTM. Load vocab from JSON (build_narrative_vocab.py);
    /// encode text to token ids, decode back. Matches Python narrative_tokenizer behavior.
    /// </summary>
    public class NarrativeLSTMTokenizer
    {
        private Dictionary<string, int> _word2id;
        private List<string> _id2word;
        private int _padId;
        private int _unkId;
        private int _eosId;

        public int PadId => _padId;
        public int UnkId => _unkId;
        public int EosId => _eosId;
        public int VocabSize => _id2word != null ? _id2word.Count : 0;

        /// <summary>Load vocab from JSON string (from vocab.json).</summary>
        public bool LoadFromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return false;
            try
            {
                var jobj = JObject.Parse(json);
                _word2id = jobj["word2id"]?.ToObject<Dictionary<string, int>>();
                var id2wordTok = jobj["id2word"];
                if (id2wordTok is JArray arr)
                {
                    _id2word = new List<string>();
                    foreach (var t in arr)
                        _id2word.Add(t.ToString());
                }
                else
                    _id2word = null;
                _padId = jobj["pad_id"] != null ? (int)jobj["pad_id"] : 0;
                _unkId = jobj["unk_id"] != null ? (int)jobj["unk_id"] : 1;
                _eosId = jobj["eos_id"] != null ? (int)jobj["eos_id"] : 2;
                return _word2id != null && _id2word != null && _id2word.Count > 0;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[NarrativeLSTMTokenizer] Load failed: {e.Message}");
                return false;
            }
        }

        /// <summary>Tokenize text to words (lowercase, alphanumeric chunks).</summary>
        public static string[] TokenizeText(string text)
        {
            if (string.IsNullOrEmpty(text)) return Array.Empty<string>();
            text = text.ToLowerInvariant().Trim();
            var matches = Regex.Matches(text, @"[a-z0-9]+");
            var list = new List<string>();
            foreach (Match m in matches)
                if (m.Success) list.Add(m.Value);
            return list.ToArray();
        }

        /// <summary>Encode text to token ids. Optionally add EOS and/or cap length.</summary>
        public int[] Encode(string text, bool addEos = false, int? maxLength = null)
        {
            if (_word2id == null) return Array.Empty<int>();
            var words = TokenizeText(text);
            var ids = new List<int>();
            foreach (var w in words)
                ids.Add(_word2id.TryGetValue(w, out int id) ? id : _unkId);
            if (addEos) ids.Add(_eosId);
            if (maxLength.HasValue && ids.Count > maxLength.Value)
                ids = ids.GetRange(0, maxLength.Value);
            return ids.ToArray();
        }

        /// <summary>Decode token ids to string. Skip special tokens by default.</summary>
        public string Decode(int[] ids, bool skipSpecial = true)
        {
            if (_id2word == null || ids == null) return "";
            var words = new List<string>();
            foreach (int i in ids)
            {
                if (i < 0 || i >= _id2word.Count) continue;
                string w = _id2word[i];
                if (skipSpecial && (w == "<pad>" || w == "<unk>" || w == "<eos>")) continue;
                words.Add(w);
            }
            return string.Join(" ", words);
        }
    }
}
