"""Spatial prepositional phrases for timed visual description chunks (grid cells)."""

from video_storage_tool.video_to_script import _grid_position_labels


def test_grid_position_labels_3x3_upper_lower_middle():
    labels = _grid_position_labels(3, 3)
    assert len(labels) == 9
    assert labels[0] == "In the upper left"
    assert labels[1] == "In the upper center"
    assert labels[2] == "In the upper right"
    assert labels[3] == "In the middle left"
    assert labels[4] == "In the center"
    assert labels[5] == "In the middle right"
    assert labels[6] == "In the lower left"
    assert labels[7] == "In the lower center"
    assert labels[8] == "In the lower right"


def test_grid_position_labels_2x2_corners():
    assert _grid_position_labels(2, 2) == [
        "In the upper left",
        "In the upper right",
        "In the lower left",
        "In the lower right",
    ]


def test_grid_position_labels_1x1():
    assert _grid_position_labels(1, 1) == ["In the center"]


def test_grid_position_labels_single_row():
    assert _grid_position_labels(1, 3) == ["On the left", "In the center", "On the right"]


def test_grid_position_labels_single_column():
    assert _grid_position_labels(3, 1) == [
        "In the upper area",
        "In the middle",
        "In the lower area",
    ]
