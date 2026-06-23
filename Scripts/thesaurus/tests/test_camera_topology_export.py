import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[2]))

from train_camera_topology_lstm import vectorize


def test_vectorize_dimensions():
    x, y = vectorize(
        {
            "topologyVector": [0.1] * 64,
            "focusMode": "Character",
            "actorVisionSalience": 0.5,
            "memorabilityMl": 0.8,
            "userRatingMean": 4.0,
        }
    )
    assert len(x) == 72
    assert len(y) == 9
    assert y[8] > 0.5
