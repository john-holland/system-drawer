# video_storage_tool

Store a large video as three artifacts: **audio** (few MB), **script** (text), and **resultant video** (T2V-generated). Reconstitute later into a single playable file.

## Prerequisites

- **Python** 3.9+
- **ffmpeg** on PATH (required for audio extraction and reconstitute). On Windows, add the folder containing `ffmpeg.exe` to your PATH, or set `audio.ffmpeg_path` in `config.yaml` / Settings (server) to the full path to `ffmpeg.exe`.

## Install

From repo root:

```bash
pip install -r Scripts/video_storage_tool/requirements.txt
```

Run from **Scripts** so the package is on the module path, or set `PYTHONPATH=Scripts` when running from repo root:

```bash
cd Scripts
python -m video_storage_tool --input path/to/video.mp4 --out ./out
```

Optional: for transcription use `openai-whisper`:

```bash
pip install openai-whisper
```

## Usage

### Store (default)

Produce `audio.aac`, `script.txt`, and `resultant.mp4` in an output directory:

```bash
python -m video_storage_tool --input /path/to/large_video.mp4 --out ./stored_my_video --audio-max-mb 5 --t2v-backend stub
```

If `--out` is omitted, output is written to `./<input_stem>_stored/`.

Options:

- `--audio-format` – `aac` (default) or `mp3`
- `--audio-max-mb` – target max size for extracted audio (default 5)
- `--t2v-backend` – `stub` (placeholder video) or `cogvideox` (Diffusers CogVideoX)
- `--t2v-model-id` – Hub model id (e.g. `THUDM/CogVideoX-2b`; overrides config)
- `--t2v-model-path` – path to T2V model (overrides config; use for local clone)
- `--script-backend` – `whisper` (if installed) or `stub`
- `--config` – path to optional `config.yaml`

### Reconstitute

Merge stored resultant video and audio into one file:

```bash
python -m video_storage_tool --reconstitute --input ./stored_my_video [--out ./reconstituted.mp4]
```

If `--out` is omitted, output is `./stored_my_video/reconstituted.mp4`.

The tool looks for `audio.aac`/`audio.mp3` and `resultant.mp4` in the input directory (or paths in `manifest.json`). If the resultant video is shorter than the audio, it is looped so the output duration matches the audio.

## Output layout

After **store**, the output directory contains:

- `audio.aac` (or `audio.mp3`) – compressed audio, target few MB
- `script.txt` – transcript (Whisper) and optional exhaustive visual description (frame-by-frame BLIP/BLIP2 captions)
- `resultant.mp4` – T2V-generated short clip (or stub placeholder)
- `manifest.json` – paths and metadata for downstream use

## Config

Optional `config.yaml` in the package dir or via `--config`:

- `audio.format`, `audio.max_mb`
- `script.backend`, `script.model` (e.g. Whisper model name), `script.visual_backend` (none | blip | blip2), `script.visual_interval_sec`, `script.visual_max_frames`
- `t2v.backend`, `t2v.model_id`, `t2v.model_path`, `t2v.duration_sec` (stub length)

## Audio size

“A few megs” is achieved by choosing a bitrate from the video duration and `--audio-max-mb`: `bitrate ≈ (max_mb * 8) / duration_sec` (capped between 32–320 kbps). No duration truncation by default.

## Visual description (script)

In addition to the Whisper transcript, you can add an exhaustive visual description of the video by setting `script.visual_backend` to `blip` or `blip2`. The pipeline samples frames (e.g. one per second, up to `script.visual_max_frames`), runs an image-captioning model (BLIP or BLIP2), and appends timestamped captions to `script.txt` under a `[Visual description]` section. Requires `pip install transformers torch` (and Pillow). Use the Settings UI or config: `script.visual_backend: blip`, `script.visual_interval_sec: 1.0`, `script.visual_max_frames: 60`.

## GPU (CUDA)

To use your NVIDIA GPU for BLIP and CogVideoX (much faster than CPU), install a CUDA build of PyTorch **after** installing other deps. You need an NVIDIA GPU and up-to-date drivers (`nvidia-smi` should show a version).

1. Install T2V deps first (CPU torch is fine): `pip install -r Scripts/video_storage_tool/requirements-t2v.txt`
2. Replace CPU PyTorch with a CUDA build (pick one; use the CUDA version closest to what `nvidia-smi` reports):
   - **CUDA 12.8** (e.g. driver CUDA 13.x):  
     `pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu128`
   - **CUDA 12.6**:  
     `pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu126`
   - **CUDA 11.8**:  
     `pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu118`
3. Restart the server and run Store; `torch.cuda.is_available()` will be True and CogVideoX/BLIP will use the GPU.

**Device option:** You can force GPU or CPU via config or CLI. In Settings, set **Device (T2V / BLIP)** to `cuda` or `auto`; or in `config.yaml` set `device: cuda` or `t2v.device: cuda`; or run the server with `--device cuda` or `--device cpu`. Default is `auto` (use GPU if available).

If no CUDA wheel exists for your Python version (e.g. 3.14), use Python 3.11 or 3.12 and recreate the env.

## T2V backends

- **stub** (default): generates a short black placeholder with ffmpeg so the pipeline runs without a real T2V model.
- **cogvideox**: uses the pretrained CogVideoX model from the Hugging Face Hub. In diffusers, the **smallest** T2V model is **THUDM/CogVideoX-2b** (~14GB download); **THUDM/CogVideoX-5b** is larger and higher quality. Set `t2v.backend: cogvideox` and `t2v.model_id: "THUDM/CogVideoX-2b"` (or `CogVideoX-5b`) in config. The first run downloads the model (no API key). For local clones use `t2v.model_path` instead of `model_id`. Install T2V deps with `pip install -r Scripts/video_storage_tool/requirements-t2v.txt`. If torch/diffusers are missing or generation fails (e.g. CUDA OOM), the pipeline falls back to the stub. (The original **THUDM/CogVideo** is a different codebase and not supported by our CogVideoXPipeline.)

## Server (store + reconstitute + streaming)

A small Flask server provides an upload UI and seekable streaming of reconstituted video:

```bash
cd Scripts
python -m pip install -r video_storage_tool/requirements.txt
python -m video_storage_tool.server --host 127.0.0.1 --port 5000
```
(If `pip` is not on PATH, use `python -m pip` instead of `pip`.)

Then open http://127.0.0.1:5000 in a browser:

- **Upload & Store**: upload a video; the store pipeline runs in the background. When ready, the job appears in the list.
- **Stored items**: each job has "Stream (resultant)" and "Stream (original)" buttons. Click one to reconstitute and play in the page.
- The video player uses standard HTML5 `<video>` with range requests, so seeking to any time is supported.

API:

- `POST /api/store` – upload a video (multipart), returns `202` with `{ "id": "<uuid>", "status": "processing" }`.
- `GET /api/stored` – list stored job ids.
- `GET /api/stored/<id>/status` – `{ "status": "ready"|"processing", "manifest": ... }`.
- `POST /api/reconstitute` – JSON `{ "stored_id": "<id>", "original": false }`, returns `{ "stream_url": "/stream/..." }`.
- `GET /stream/<id>?original=0|1` – serve reconstituted MP4 with Range support (seekable).
