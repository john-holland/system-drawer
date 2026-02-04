# Narrative LSTM: Prompt Interpreter and Calendar Summarizer

## Overview

- **Prompt interpreter**: Natural language prompt → narrative events (+ optional 4D). Trained on exported calendar/4D data.
- **Summarizer**: Calendar (or time window) → short "what's going on" summary.

Training runs in Python; inference in Unity via Barracuda (ONNX).

## Steps

### 1. Export training data (Unity Editor)

- **Calendars**: Menu **Locomotion > Narrative > Export for LSTM training...**  
  Choose a folder (e.g. `NarrativeLSTM_Training`). Exports all calendars in the scene to `calendars/*.json`.
- **4D expressions** (optional): **Locomotion > Narrative > Export 4D expressions for LSTM training...**  
  Choose the same folder and optionally select a 4D expressions JSON file. It will be copied to `expressions/`.

### 2. Build vocabulary (Python)

```bash
python build_narrative_vocab.py --training_dir ./NarrativeLSTM_Training [--output ./NarrativeLSTM_Training/vocab.json]
```

Copy `vocab.json` to `Assets/StreamingAssets/NarrativeLSTM/vocab.json` (or place in `Assets/Resources/NarrativeLSTM/vocab.json` as a TextAsset).

### 3. Train summarizer (Python)

```bash
python train_narrative_summarizer.py --training_dir ./NarrativeLSTM_Training [--vocab ./NarrativeLSTM_Training/vocab.json] --output_dir ./Models/NarrativeLSTM
```

Produces `narrative_summarizer.onnx`. Copy to `StreamingAssets/NarrativeLSTM/`.

### 4. Train prompt interpreter (Python)

```bash
python train_narrative_lstm.py --training_dir ./NarrativeLSTM_Training [--vocab ...] --output_dir ./Models/NarrativeLSTM
```

Produces `narrative_prompt_interpreter.onnx`. Copy to `StreamingAssets/NarrativeLSTM/`.

### 5. Unity runtime

- Add **NarrativeLSTMSummarizer** and/or **NarrativeLSTMPromptInterpreter** to a GameObject.
- Set **vocab** path (e.g. `NarrativeLSTM/vocab`) or assign a TextAsset. Set **model path** to the ONNX path under StreamingAssets.
- Optionally add **NarrativeLSTMUI** for a small in-game panel (prompt input, Summarize/Interpret buttons, summary and result text).

## Dependencies

- **Unity**: Barracuda package (`com.unity.barracuda`) in `Packages/manifest.json`.
- **Python**: PyTorch, `onnx` (optional). Vocab/tokenizer use only stdlib + `narrative_tokenizer.py` (and `build_narrative_vocab.py`).

## Vocab / tokenizer

- **Python**: `narrative_tokenizer.py` — `NarrativeTokenizer(vocab_path)`, `.encode(text)`, `.decode(ids)`.
- **Unity**: `NarrativeLSTMTokenizer` in `Locomotion.Narrative.Serialization` — `LoadFromJson(json)`, `Encode(string)`, `Decode(int[])`.

Same tokenization rules (lowercase, alphanumeric chunks) so runtime matches training.
