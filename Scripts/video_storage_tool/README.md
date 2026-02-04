# video_storage_tool

Store a large video as three artifacts: **audio** (few MB), **script** (text), and **resultant video** (T2V-generated). Reconstitute later into a single playable file.

## Prerequisites

- **Python** 3.9+
- **ffmpeg** on PATH (required for audio extraction and reconstitute)

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
- `--t2v-backend` – `stub` (placeholder video) or e.g. `cogvideo` when implemented
- `--t2v-model-path` – path to T2V model (overrides config)
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
- `script.txt` – transcript or placeholder
- `resultant.mp4` – T2V-generated short clip (or stub placeholder)
- `manifest.json` – paths and metadata for downstream use

## Config

Optional `config.yaml` in the package dir or via `--config`:

- `audio.format`, `audio.max_mb`
- `script.backend`, `script.model` (e.g. Whisper model name)
- `t2v.backend`, `t2v.model_path`, `t2v.duration_sec` (stub length)

## Audio size

“A few megs” is achieved by choosing a bitrate from the video duration and `--audio-max-mb`: `bitrate ≈ (max_mb * 8) / duration_sec` (capped between 32–320 kbps). No duration truncation by default.

## T2V backends

- **stub** (default): generates a short black placeholder with ffmpeg so the pipeline runs without a real T2V model.
- **cogvideo** (or others): can be wired in via `script_to_video.py`; keep backend configurable so the tool is not locked to one vendor.
