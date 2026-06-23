# Camera Topology LSTM

Train hint biases and memorability scores from exported camera topology JSON.

## Export (Unity Editor)

**Locomotion → Camera → Export topology for LSTM training...**

Select a `CameraPathingRig` and choose an output folder (e.g. `CameraTopology_Training/`).

## Train (Python)

```bash
pip install torch
python train_camera_topology_lstm.py --training_dir ./CameraTopology_Training --output_dir ./Models/CameraLSTM
```

Copy `camera_topology_lstm.onnx` to `Assets/StreamingAssets/CameraLSTM/`.

## Runtime

Add `CameraTopologyLSTM` + `CameraPathingRig` to the main camera. Hints feed `CameraPathingHeuristic` and `HierarchicalCameraPathingSolver`.

## User ratings

Rate scenes in **Camera Pathing** web UI (`/camera-pathing`). Ratings merge into training labels:

`merged = 0.6 * (userMean/5) + 0.4 * memorabilityMl`

## API

- `GET /api/camera/hints/:sceneId` — runtime hint bias for Unity
- `POST /api/camera/scenes/:id/rate` — user score 1–5
- Threaded comments with `@mentions` and direct links
