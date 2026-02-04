"""
Build vocabulary for narrative LSTM from exported training data (calendars + expressions).
Writes vocab.json with word->id and id->word, and special tokens pad, unk, eos.
Usage: python build_narrative_vocab.py --training_dir <path_to_Training> [--output <vocab.json path>]
"""

import argparse
import json
import os
import re
from pathlib import Path
from collections import Counter


PAD = "<pad>"
UNK = "<unk>"
EOS = "<eos>"
SPECIAL = [PAD, UNK, EOS]


def tokenize_text(text):
    """Simple word tokenize: lowercase, split on non-alphanumeric, keep words."""
    if not text or not isinstance(text, str):
        return []
    text = text.lower().strip()
    tokens = re.findall(r"[a-z0-9]+", text)
    return tokens


def collect_words_from_calendars(calendars_dir):
    words = []
    path = Path(calendars_dir)
    if not path.exists():
        return words
    for jf in path.glob("*.json"):
        try:
            with open(jf, "r", encoding="utf-8") as f:
                data = json.load(f)
            events = data.get("events") or []
            for e in events:
                if e.get("title"):
                    words.extend(tokenize_text(e["title"]))
                if e.get("notes"):
                    words.extend(tokenize_text(e["notes"]))
                for t in e.get("tags") or []:
                    words.extend(tokenize_text(t))
        except Exception as ex:
            print(f"Warning: skip {jf}: {ex}")
    return words


def collect_words_from_expressions(expressions_dir):
    words = []
    path = Path(expressions_dir)
    if not path.exists():
        return words
    for jf in path.glob("*.json"):
        try:
            with open(jf, "r", encoding="utf-8") as f:
                data = json.load(f)
            entries = data.get("entries") or []
            for ent in entries:
                if ent.get("label"):
                    words.extend(tokenize_text(ent["label"]))
                if ent.get("type"):
                    words.append(ent["type"].lower())
        except Exception as ex:
            print(f"Warning: skip {jf}: {ex}")
    return words


def build_vocab(training_dir, min_count=1, max_size=8000):
    calendars_dir = Path(training_dir) / "calendars"
    expressions_dir = Path(training_dir) / "expressions"
    all_words = []
    all_words.extend(collect_words_from_calendars(calendars_dir))
    all_words.extend(collect_words_from_expressions(expressions_dir))
    # Add common words for prompts/summaries
    all_words.extend(tokenize_text("event meeting schedule add at time day today tomorrow morning afternoon"))
    counter = Counter(all_words)
    vocab_list = list(SPECIAL)
    for w, c in counter.most_common(max_size - len(SPECIAL)):
        if c >= min_count and w not in vocab_list:
            vocab_list.append(w)
    word2id = {w: i for i, w in enumerate(vocab_list)}
    # id2word as array so C#/Unity can use index directly (JSON array)
    return {"word2id": word2id, "id2word": vocab_list, "pad_id": 0, "unk_id": 1, "eos_id": 2}


def main():
    parser = argparse.ArgumentParser(description="Build narrative LSTM vocabulary from training export")
    parser.add_argument("--training_dir", type=str, default="./NarrativeLSTM_Training",
                        help="Training folder containing calendars/ and expressions/")
    parser.add_argument("--output", type=str, default=None,
                        help="Output vocab.json path (default: <training_dir>/vocab.json)")
    parser.add_argument("--min_count", type=int, default=1)
    parser.add_argument("--max_size", type=int, default=8000)
    args = parser.parse_args()
    out_path = args.output or str(Path(args.training_dir) / "vocab.json")
    vocab = build_vocab(args.training_dir, args.min_count, args.max_size)
    Path(out_path).parent.mkdir(parents=True, exist_ok=True)
    with open(out_path, "w", encoding="utf-8") as f:
        json.dump(vocab, f, indent=2)
    print(f"Vocab size: {len(vocab['word2id'])} written to {out_path}")


if __name__ == "__main__":
    main()
