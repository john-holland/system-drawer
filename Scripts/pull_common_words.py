"""
Pull the N most common words for a language via wordfreq (Zipf-ranked).

Install:  pip install wordfreq
Usage:
  python pull_common_words.py
  python pull_common_words.py --n 5000 --lang en --output path.json
  python pull_common_words.py --format txt
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from typing import Any


DEFAULT_OUTPUT = (
    Path(__file__).resolve().parent
    / "continuuuum_api"
    / "data"
    / "common_words_en_5000.json"
)


def pull_common_words(lang: str = "en", n: int = 5000) -> list[dict[str, Any]]:
    """Return rank/word/zipf rows for the top-n words in ``lang``."""
    if n < 1:
        raise ValueError("n must be >= 1")

    try:
        from wordfreq import top_n_list, zipf_frequency
    except ImportError as e:
        raise SystemExit(
            "wordfreq is required. Install with: pip install wordfreq"
        ) from e

    words = top_n_list(lang, n)
    return [
        {"rank": i, "word": w, "zipf": round(zipf_frequency(w, lang), 3)}
        for i, w in enumerate(words, start=1)
    ]


def build_payload(lang: str, n: int, rows: list[dict[str, Any]]) -> dict[str, Any]:
    return {
        "source": "wordfreq",
        "language": lang,
        "n": len(rows),
        "requested_n": n,
        "words": rows,
    }


def write_output(path: Path, rows: list[dict[str, Any]], payload: dict[str, Any], fmt: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    if fmt == "txt":
        path.write_text("\n".join(r["word"] for r in rows) + "\n", encoding="utf-8")
    else:
        path.write_text(json.dumps(payload, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(
        description="Pull the N most common words via wordfreq"
    )
    parser.add_argument("--n", type=int, default=5000, help="Number of words (default: 5000)")
    parser.add_argument("--lang", type=str, default="en", help="Language code (default: en)")
    parser.add_argument(
        "--output",
        type=str,
        default=None,
        help=f"Output path (default: {DEFAULT_OUTPUT})",
    )
    parser.add_argument(
        "--format",
        choices=("json", "txt"),
        default="json",
        help="Output format (default: json)",
    )
    args = parser.parse_args(argv)

    out = Path(args.output) if args.output else DEFAULT_OUTPUT
    if args.output is None and args.format == "txt":
        out = DEFAULT_OUTPUT.with_suffix(".txt")
    if args.n != 5000 and args.output is None and args.lang == "en":
        stem = f"common_words_{args.lang}_{args.n}"
        out = DEFAULT_OUTPUT.with_name(stem + out.suffix)

    rows = pull_common_words(lang=args.lang, n=args.n)
    payload = build_payload(args.lang, args.n, rows)
    write_output(out, rows, payload, args.format)
    print(f"Wrote {len(rows)} words to {out}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
