"""
Train LSTM summarizer: calendar snapshot text -> summary text.
Reads training export (calendars/*.json), builds (calendar_text, summary) pairs with template summaries,
trains PyTorch model, exports ONNX for Barracuda.
Usage: python train_narrative_summarizer.py --training_dir <path> [--vocab <vocab.json>] [--output_dir <models>]
"""

import argparse
import json
import os
from pathlib import Path

import numpy as np

try:
    import torch
    import torch.nn as nn
    from torch.utils.data import Dataset, DataLoader
    TORCH_AVAILABLE = True
except ImportError:
    TORCH_AVAILABLE = False

try:
    import onnx
    import onnxruntime as ort
    ONNX_AVAILABLE = True
except ImportError:
    ONNX_AVAILABLE = False

from narrative_tokenizer import NarrativeTokenizer, tokenize_text

CALENDAR_MAX_LEN = 256
SUMMARY_MAX_LEN = 32


def calendar_events_to_snapshot_text(events):
    """Serialize calendar events to a single text string (one line per event)."""
    lines = []
    for e in events:
        title = e.get("title") or ""
        notes = (e.get("notes") or "")[:200]
        start = e.get("startDateTime") or {}
        start_str = f"{start.get('year', 0)}-{start.get('month', 0)}-{start.get('day', 0)} {start.get('hour', 0)}:{start.get('minute', 0)}"
        dur = e.get("durationSeconds", 0)
        tags = " ".join(e.get("tags") or [])
        lines.append(f"Event: {title} | {notes} | {start_str} | {dur}s | {tags}")
    return "\n".join(lines) if lines else "No events"


def make_template_summary(events):
    """Generate a simple template summary from events."""
    n = len(events)
    if n == 0:
        return "Nothing scheduled."
    titles = [e.get("title") or "event" for e in events[:3]]
    return f"{n} events: " + ", ".join(titles) + (" and more" if n > 3 else "")


def load_calendar_summary_pairs(training_dir, vocab_path):
    """Load (calendar_snapshot_text, summary_text) from training export."""
    calendars_dir = Path(training_dir) / "calendars"
    if not calendars_dir.exists():
        return []
    tok = NarrativeTokenizer(vocab_path)
    pairs = []
    for jf in calendars_dir.glob("*.json"):
        try:
            with open(jf, "r", encoding="utf-8") as f:
                data = json.load(f)
            events = data.get("events") or []
            if not events:
                continue
            calendar_text = calendar_events_to_snapshot_text(events)
            summary_text = make_template_summary(events)
            pairs.append((calendar_text, summary_text))
        except Exception as ex:
            print(f"Warning: skip {jf}: {ex}")
    return pairs


class SummarizerDataset(Dataset):
    def __init__(self, pairs, tokenizer, calendar_max_len=CALENDAR_MAX_LEN, summary_max_len=SUMMARY_MAX_LEN, vocab_size=8000):
        self.pairs = pairs
        self.tok = tokenizer
        self.calendar_max_len = calendar_max_len
        self.summary_max_len = summary_max_len
        self.vocab_size = vocab_size

    def __len__(self):
        return len(self.pairs)

    def __getitem__(self, idx):
        cal_text, sum_text = self.pairs[idx]
        cal_ids = self.tok.encode(cal_text, add_eos=False, max_length=self.calendar_max_len)
        sum_ids = self.tok.encode(sum_text, add_eos=True, max_length=self.summary_max_len)
        # Pad calendar to fixed length
        while len(cal_ids) < self.calendar_max_len:
            cal_ids.append(self.tok.pad_id)
        cal_ids = cal_ids[:self.calendar_max_len]
        # Pad summary to fixed length (normalize to [0,1] for regression)
        while len(sum_ids) < self.summary_max_len:
            sum_ids.append(self.tok.pad_id)
        sum_ids = sum_ids[:self.summary_max_len]
        sum_normalized = [i / max(1, self.vocab_size - 1) for i in sum_ids]
        return {
            "calendar": torch.tensor(cal_ids, dtype=torch.float32),
            "summary": torch.tensor(sum_normalized, dtype=torch.float32),
        }


class SummarizerLSTM(nn.Module):
    def __init__(self, vocab_size, embed_dim=64, hidden_dim=128, calendar_max_len=CALENDAR_MAX_LEN, summary_max_len=SUMMARY_MAX_LEN):
        super().__init__()
        self.embed = nn.Embedding(vocab_size, embed_dim, padding_idx=0)
        self.lstm = nn.LSTM(embed_dim, hidden_dim, batch_first=True)
        self.fc = nn.Linear(hidden_dim, summary_max_len)

    def forward(self, x):
        # x: (batch, calendar_max_len) float token ids -> cast to long for embedding
        x_long = x.long().clamp(0, self.embed.num_embeddings - 1)
        emb = self.embed(x_long)
        _, (h, _) = self.lstm(emb)
        out = self.fc(h.squeeze(0))
        return out


def train(training_dir, vocab_path, output_dir, epochs=50, batch_size=8, lr=0.001):
    if not TORCH_AVAILABLE:
        print("PyTorch not available.")
        return
    pairs = load_calendar_summary_pairs(training_dir, vocab_path)
    if not pairs:
        print("No calendar/summary pairs found. Export calendars first.")
        return
    tok = NarrativeTokenizer(vocab_path)
    vocab_size = len(tok.word2id)
    if vocab_size == 0:
        print("Empty vocab.")
        return
    dataset = SummarizerDataset(pairs, tok, vocab_size=vocab_size)
    loader = DataLoader(dataset, batch_size=batch_size, shuffle=True)
    model = SummarizerLSTM(vocab_size, calendar_max_len=CALENDAR_MAX_LEN, summary_max_len=SUMMARY_MAX_LEN)
    opt = torch.optim.Adam(model.parameters(), lr=lr)
    for epoch in range(epochs):
        total_loss = 0.0
        for batch in loader:
            opt.zero_grad()
            out = model(batch["calendar"])
            loss = nn.functional.mse_loss(out, batch["summary"])
            loss.backward()
            opt.step()
            total_loss += loss.item()
        if (epoch + 1) % 10 == 0:
            print(f"Epoch {epoch+1}/{epochs}, Loss: {total_loss / len(loader):.4f}")
    Path(output_dir).mkdir(parents=True, exist_ok=True)
    torch.save(model.state_dict(), Path(output_dir) / "narrative_summarizer.pth")
    # Export ONNX: input (1, CALENDAR_MAX_LEN) float, output (1, SUMMARY_MAX_LEN) float
    if ONNX_AVAILABLE:
        try:
            dummy = torch.zeros(1, CALENDAR_MAX_LEN, dtype=torch.float32)
            onnx_path = Path(output_dir) / "narrative_summarizer.onnx"
            torch.onnx.export(
                model, dummy, str(onnx_path),
                input_names=["calendar"],
                output_names=["summary"],
                dynamic_axes={"calendar": {0: "batch"}, "summary": {0: "batch"}},
            )
            print(f"ONNX saved to {onnx_path}")
        except Exception as e:
            print(f"ONNX export error: {e}")
    return model


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--training_dir", type=str, default="./NarrativeLSTM_Training")
    parser.add_argument("--vocab", type=str, default=None, help="vocab.json path (default: training_dir/vocab.json)")
    parser.add_argument("--output_dir", type=str, default="./Models/NarrativeLSTM")
    parser.add_argument("--epochs", type=int, default=50)
    parser.add_argument("--batch_size", type=int, default=8)
    parser.add_argument("--lr", type=float, default=0.001)
    args = parser.parse_args()
    vocab_path = args.vocab or str(Path(args.training_dir) / "vocab.json")
    if not os.path.isfile(vocab_path):
        print(f"Vocab not found: {vocab_path}. Run build_narrative_vocab.py first.")
        return
    train(args.training_dir, vocab_path, args.output_dir, args.epochs, args.batch_size, args.lr)
    print("Done.")


if __name__ == "__main__":
    main()
