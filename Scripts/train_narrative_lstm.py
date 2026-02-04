"""
Train LSTM prompt interpreter: natural language prompt -> narrative events + 4D params.
Uses synthetic (prompt, events) pairs from exported calendars. Output = fixed-size vector
decodeable to up to 3 events (title index, start, duration, center, size, tMin, tMax each).
Usage: python train_narrative_lstm.py --training_dir <path> [--vocab <vocab.json>] [--output_dir <models>]
"""

import argparse
import json
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
    ONNX_AVAILABLE = True
except ImportError:
    ONNX_AVAILABLE = False

from narrative_tokenizer import NarrativeTokenizer

PROMPT_MAX_LEN = 128
# Per event: title_idx, start_norm, duration_norm, cx, cy, cz, sx, sy, sz, tMin, tMax (11) + 2 for count = 35
EVENT_PARAMS = 11
MAX_EVENTS = 3
OUTPUT_DIM = 2 + MAX_EVENTS * EVENT_PARAMS  # 35


def datetime_to_seconds(e):
    """Convert event startDateTime to narrative seconds (epoch 2025-01-01)."""
    d = e.get("startDateTime") or {}
    from datetime import datetime
    try:
        dt = datetime(
            d.get("year", 2025), d.get("month", 1), d.get("day", 1),
            d.get("hour", 0), d.get("minute", 0), d.get("second", 0)
        )
        epoch = datetime(2025, 1, 1, 0, 0, 0)
        return (dt - epoch).total_seconds()
    except Exception:
        return 0.0


def load_prompt_event_pairs(training_dir, vocab_path):
    """Build synthetic (prompt, target_vector) from calendars. Target = 35 floats."""
    calendars_dir = Path(training_dir) / "calendars"
    if not calendars_dir.exists():
        return []
    tok = NarrativeTokenizer(vocab_path)
    vocab_size = len(tok.word2id)
    pairs = []
    for jf in calendars_dir.glob("*.json"):
        try:
            with open(jf, "r", encoding="utf-8") as f:
                data = json.load(f)
            events = data.get("events") or []
            for e in events:
                title = e.get("title") or "event"
                prompt = f"Add event {title}"
                start_s = datetime_to_seconds(e)
                dur = float(e.get("durationSeconds", 0))
                # Normalize start to [0,1] (e.g. 0-86400*7 for a week)
                start_norm = np.clip(start_s / (86400.0 * 7), 0, 1)
                dur_norm = np.clip(dur / 3600.0, 0, 1)
                cx = e.get("centerX") or 0.0
                cy = e.get("centerY") or 0.0
                cz = e.get("centerZ") or 0.0
                sx = e.get("sizeX") or 1.0
                sy = e.get("sizeY") or 1.0
                sz = e.get("sizeZ") or 1.0
                t_min = e.get("tMin") or 0.0
                t_max = e.get("tMax") or 3600.0
                t_min_norm = np.clip(t_min / (86400.0 * 7), 0, 1)
                t_max_norm = np.clip(t_max / (86400.0 * 7), 0, 1)
                title_ids = tok.encode(title, add_eos=False, max_length=1)
                title_idx = title_ids[0] if title_ids else 0
                title_idx = min(title_idx, vocab_size - 1)
                # Target: [count, pad, e0_title, e0_start, e0_dur, e0_cx,cy,cz, sx,sy,sz, tMin,tMax, ...]
                target = [
                    1.0 / MAX_EVENTS, 0.0,  # 1 event
                    title_idx / max(1, vocab_size - 1), start_norm, dur_norm,
                    cx, cy, cz, sx, sy, sz, t_min_norm, t_max_norm,
                ]
                for _ in range(MAX_EVENTS - 1):
                    target.extend([0.0] * EVENT_PARAMS)
                pairs.append((prompt, target[:OUTPUT_DIM]))
        except Exception as ex:
            print(f"Warning: skip {jf}: {ex}")
    return pairs


class PromptInterpreterDataset(Dataset):
    def __init__(self, pairs, tokenizer, prompt_max_len=PROMPT_MAX_LEN, output_dim=OUTPUT_DIM):
        self.pairs = pairs
        self.tok = tokenizer
        self.prompt_max_len = prompt_max_len
        self.output_dim = output_dim

    def __len__(self):
        return len(self.pairs)

    def __getitem__(self, idx):
        prompt, target = self.pairs[idx]
        ids = self.tok.encode(prompt, add_eos=False, max_length=self.prompt_max_len)
        while len(ids) < self.prompt_max_len:
            ids.append(self.tok.pad_id)
        ids = ids[:self.prompt_max_len]
        t = target + [0.0] * (self.output_dim - len(target))
        t = t[:self.output_dim]
        return {"prompt": torch.tensor(ids, dtype=torch.float32), "target": torch.tensor(t, dtype=torch.float32)}


class PromptInterpreterLSTM(nn.Module):
    def __init__(self, vocab_size, embed_dim=64, hidden_dim=128, prompt_max_len=PROMPT_MAX_LEN, output_dim=OUTPUT_DIM):
        super().__init__()
        self.embed = nn.Embedding(vocab_size, embed_dim, padding_idx=0)
        self.lstm = nn.LSTM(embed_dim, hidden_dim, batch_first=True)
        self.fc = nn.Linear(hidden_dim, output_dim)

    def forward(self, x):
        x_long = x.long().clamp(0, self.embed.num_embeddings - 1)
        emb = self.embed(x_long)
        _, (h, _) = self.lstm(emb)
        return self.fc(h.squeeze(0))


def train(training_dir, vocab_path, output_dir, epochs=30, batch_size=8, lr=0.001):
    if not TORCH_AVAILABLE:
        print("PyTorch not available.")
        return
    pairs = load_prompt_event_pairs(training_dir, vocab_path)
    if not pairs:
        print("No prompt/event pairs. Export calendars first.")
        return
    tok = NarrativeTokenizer(vocab_path)
    dataset = PromptInterpreterDataset(pairs, tok)
    loader = DataLoader(dataset, batch_size=batch_size, shuffle=True)
    model = PromptInterpreterLSTM(len(tok.word2id), prompt_max_len=PROMPT_MAX_LEN, output_dim=OUTPUT_DIM)
    opt = torch.optim.Adam(model.parameters(), lr=lr)
    for epoch in range(epochs):
        total_loss = 0.0
        for batch in loader:
            opt.zero_grad()
            out = model(batch["prompt"])
            loss = nn.functional.mse_loss(out, batch["target"])
            loss.backward()
            opt.step()
            total_loss += loss.item()
        if (epoch + 1) % 10 == 0:
            print(f"Epoch {epoch+1}/{epochs}, Loss: {total_loss / len(loader):.4f}")
    Path(output_dir).mkdir(parents=True, exist_ok=True)
    torch.save(model.state_dict(), Path(output_dir) / "narrative_prompt_interpreter.pth")
    if ONNX_AVAILABLE:
        try:
            dummy = torch.zeros(1, PROMPT_MAX_LEN, dtype=torch.float32)
            onnx_path = Path(output_dir) / "narrative_prompt_interpreter.onnx"
            torch.onnx.export(
                model, dummy, str(onnx_path),
                input_names=["prompt"],
                output_names=["output"],
                dynamic_axes={"prompt": {0: "batch"}, "output": {0: "batch"}},
            )
            print(f"ONNX saved to {onnx_path}")
        except Exception as e:
            print(f"ONNX export error: {e}")
    return model


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--training_dir", type=str, default="./NarrativeLSTM_Training")
    parser.add_argument("--vocab", type=str, default=None)
    parser.add_argument("--output_dir", type=str, default="./Models/NarrativeLSTM")
    parser.add_argument("--epochs", type=int, default=30)
    parser.add_argument("--batch_size", type=int, default=8)
    parser.add_argument("--lr", type=float, default=0.001)
    args = parser.parse_args()
    vocab_path = args.vocab or str(Path(args.training_dir) / "vocab.json")
    if not Path(vocab_path).is_file():
        print(f"Vocab not found: {vocab_path}. Run build_narrative_vocab.py first.")
        return
    train(args.training_dir, vocab_path, args.output_dir, args.epochs, args.batch_size, args.lr)
    print("Done.")


if __name__ == "__main__":
    main()
