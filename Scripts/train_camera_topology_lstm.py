#!/usr/bin/env python3
"""Train camera topology LSTM: topology + mode -> hint biases + memorability."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import torch
import torch.nn as nn


class CameraTopologyModel(nn.Module):
    def __init__(self, input_dim: int = 72, hidden: int = 128, output_dim: int = 9):
        super().__init__()
        self.lstm = nn.LSTM(input_dim, hidden, batch_first=True)
        self.fc = nn.Linear(hidden, output_dim)

    def forward(self, x):
        out, _ = self.lstm(x)
        return self.fc(out[:, -1, :])


MODE_INDEX = {
    "ObjectFocus": 0,
    "Character": 1,
    "FirstPerson": 2,
    "SceneFocus": 3,
    "CentroidFocus": 4,
    "MlActorVisionTrainingFocus": 5,
    "Transition": 6,
}


def load_samples(training_dir: Path) -> list[dict]:
    samples = []
    for p in training_dir.glob("**/*.json"):
        data = json.loads(p.read_text(encoding="utf-8"))
        if isinstance(data, dict):
            samples.append(data)
    return samples


def vectorize(sample: dict) -> tuple[list[float], list[float]]:
    topo = sample.get("topologyVector") or [0.0] * 64
    topo = (list(topo) + [0.0] * 64)[:64]
    mode = MODE_INDEX.get(sample.get("focusMode", "Character"), 1)
    one_hot = [0.0] * 7
    one_hot[mode] = 1.0
    salience = float(sample.get("actorVisionSalience") or 0.0)
    x = topo + one_hot + [salience]

    mem_ml = float(sample.get("memorabilityMl") or 0.5)
    user = float(sample.get("userRatingMean") or 3.0) / 5.0
    merged = 0.6 * user + 0.4 * mem_ml
    y_bias = [0.05 * (i % 3) for i in range(8)]
    y = y_bias + [merged]
    return x, y


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--training_dir", required=True)
    ap.add_argument("--output_dir", default="./Models/CameraLSTM")
    ap.add_argument("--epochs", type=int, default=20)
    args = ap.parse_args()

    samples = load_samples(Path(args.training_dir))
    if not samples:
        raise SystemExit("no training samples found")

    xs, ys = zip(*[vectorize(s) for s in samples])
    x_t = torch.tensor(xs, dtype=torch.float32).unsqueeze(1)
    y_t = torch.tensor(ys, dtype=torch.float32)

    model = CameraTopologyModel()
    opt = torch.optim.Adam(model.parameters(), lr=1e-3)
    loss_fn = nn.MSELoss()

    for epoch in range(args.epochs):
        opt.zero_grad()
        pred = model(x_t)
        loss = loss_fn(pred, y_t)
        loss.backward()
        opt.step()
        print(f"epoch {epoch + 1} loss={loss.item():.4f}")

    out_dir = Path(args.output_dir)
    out_dir.mkdir(parents=True, exist_ok=True)
    onnx_path = out_dir / "camera_topology_lstm.onnx"
    torch.onnx.export(model, x_t[:1], str(onnx_path), input_names=["input"], output_names=["output"], opset_version=17)
    print(f"wrote {onnx_path}")


if __name__ == "__main__":
    main()
