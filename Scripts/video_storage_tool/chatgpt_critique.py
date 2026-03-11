"""
ChatGPT / local LLM critique pipeline for video descriptions.
Sends full script + visual description to LLM; parses structured critique with
quotes, index locations, and narrative type labels (linear, non-linear, hub_and_spoke).

Configured for local LLM via LM Studio (user preference).
"""

import json
import logging
from pathlib import Path
from typing import Any

log = logging.getLogger(__name__)

CRITIQUE_PROMPT = """Analyze this video script and visual description. For each narrative segment, identify:
1. Whether it is LINEAR (strictly sequential), NON-LINEAR (contains loops/cycles), or HUB_AND_SPOKE (choices could have happened at different times; branching).
2. Provide exact quotes from the script with character offsets or line numbers.
3. Brief reasoning for each classification.

Respond in JSON format:
{
  "segments": [
    {
      "narrative_type": "linear" | "non-linear" | "hub_and_spoke",
      "quote": "exact text from script",
      "start_index": 0,
      "end_index": 0,
      "line_num": 1,
      "reasoning": "brief explanation"
    }
  ]
}

Script:
---
{script}
---
"""


def run_critique_via_lm_studio(
    script_text: str,
    *,
    base_url: str = "http://localhost:1234/v1",
    model: str = "local-model",
) -> dict[str, Any]:
    """
    Send script to LM Studio local LLM and parse JSON critique.
    LM Studio exposes OpenAI-compatible API at localhost:1234 by default.
    """
    try:
        import urllib.request
        payload = {
            "model": model,
            "messages": [
                {"role": "user", "content": CRITIQUE_PROMPT.format(script=script_text[:8000])}
            ],
            "temperature": 0.2,
        }
        req = urllib.request.Request(
            f"{base_url}/chat/completions",
            data=json.dumps(payload).encode("utf-8"),
            headers={"Content-Type": "application/json"},
            method="POST",
        )
        with urllib.request.urlopen(req, timeout=120) as resp:
            data = json.loads(resp.read().decode())
        content = (data.get("choices") or [{}])[0].get("message", {}).get("content", "{}")
        # Extract JSON from markdown code block if present
        if "```" in content:
            start = content.find("```")
            if "json" in content[start : start + 10].lower():
                start = content.find("\n", start) + 1
            end = content.find("```", start)
            content = content[start:end] if end > 0 else content[start:]
        return json.loads(content)
    except Exception as e:
        log.exception("LM Studio critique failed: %s", e)
        return {"segments": [], "error": str(e)}


def critique_script_file(
    script_path: Path,
    *,
    lm_studio_url: str = "http://localhost:1234/v1",
) -> dict[str, Any]:
    """Load script from file and run critique."""
    if not script_path.exists():
        return {"segments": [], "error": f"Script not found: {script_path}"}
    text = script_path.read_text(encoding="utf-8")
    return run_critique_via_lm_studio(text, base_url=lm_studio_url)
