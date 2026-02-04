"""
Tokenizer for narrative LSTM: load vocab.json, encode text to list of ints, decode back.
Must match C# NarrativeLSTMTokenizer behavior (same vocab and tokenization rules).
"""

import json
import re
from pathlib import Path


def tokenize_text(text):
    """Match build_narrative_vocab: lowercase, split on non-alphanumeric."""
    if not text or not isinstance(text, str):
        return []
    text = text.lower().strip()
    return re.findall(r"[a-z0-9]+", text)


class NarrativeTokenizer:
    def __init__(self, vocab_path):
        with open(vocab_path, "r", encoding="utf-8") as f:
            data = json.load(f)
        self.word2id = data["word2id"]
        id2w = data["id2word"]
        self.id2word = id2w if isinstance(id2w, dict) else {i: w for i, w in enumerate(id2w)}
        self.pad_id = data.get("pad_id", 0)
        self.unk_id = data.get("unk_id", 1)
        self.eos_id = data.get("eos_id", 2)

    def encode(self, text, add_eos=False, max_length=None):
        tokens = tokenize_text(text)
        ids = [self.word2id.get(w, self.unk_id) for w in tokens]
        if add_eos:
            ids.append(self.eos_id)
        if max_length is not None:
            ids = ids[:max_length]
        return ids

    def decode(self, ids, skip_special=True):
        words = []
        for i in ids:
            if isinstance(i, float):
                i = int(i)
            w = self.id2word.get(i, "<unk>")
            if skip_special and w in ("<pad>", "<unk>", "<eos>"):
                continue
            words.append(w)
        return " ".join(words)


if __name__ == "__main__":
    import sys
    if len(sys.argv) < 2:
        print("Usage: python narrative_tokenizer.py <vocab.json> [text to encode]")
        sys.exit(1)
    tok = NarrativeTokenizer(sys.argv[1])
    if len(sys.argv) > 2:
        text = " ".join(sys.argv[2:])
        enc = tok.encode(text, add_eos=True)
        print("Encode:", enc)
        print("Decode:", tok.decode(enc))
    else:
        print("Vocab size:", len(tok.word2id))
