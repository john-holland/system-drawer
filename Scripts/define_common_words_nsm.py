"""
Define top-N common words via local LM Studio using NSM primes + higher-precedence lemmas.

Install/runtime: LM Studio OpenAI-compatible API (default http://localhost:1234/v1)

Usage:
  python define_common_words_nsm.py --limit 5 --dry-run
  python define_common_words_nsm.py --start-rank 1 --end-rank 50 --resume
  python define_common_words_nsm.py --db path/to/continuuuum.db --base-url http://localhost:1234/v1
"""

from __future__ import annotations

import argparse
import json
import re
import sqlite3
import sys
import urllib.error
import urllib.request
from pathlib import Path
from typing import Any, Callable

SCRIPTS = Path(__file__).resolve().parent
API = SCRIPTS / "continuuuum_api"
DATA = API / "data"
sys.path.insert(0, str(API))
sys.path.insert(0, str(SCRIPTS))

from lemma_completion_db import (  # noqa: E402
    ensure_lemma_completion_schema,
    load_common_words,
    load_primes,
    seed_lemma_completion,
    upsert_definition,
)

PREFACE_PATH = DATA / "lemma_build_preface" / "LemmaNsmDefinePreface.md"
BUILTIN_PATH = DATA / "builtin_vocabulary.json"
DEFAULT_OUT = SCRIPTS.parent / "Library" / "LemmaNsmDefine" / "definitions.jsonl"

DESCRIPTOR_FENCE = re.compile(
    r"```(?:json\s+)?lemma-mechanism-descriptor\s*([\s\S]*?)```",
    re.IGNORECASE,
)


ChatFn = Callable[[str, str, list[dict[str, str]], float], str]


def load_preface() -> str:
    return PREFACE_PATH.read_text(encoding="utf-8")


def load_builtins() -> list[dict[str, Any]]:
    data = json.loads(BUILTIN_PATH.read_text(encoding="utf-8"))
    return list(data.get("items") or [])


def allow_list_for_rank(
    target_rank: int,
    *,
    primes: list[dict[str, Any]] | None = None,
    builtins: list[dict[str, Any]] | None = None,
    prior_words: list[dict[str, Any]] | None = None,
) -> list[dict[str, str]]:
    """Build composition allow-list: primes, builtins, then words with rank < target_rank."""
    primes = primes if primes is not None else load_primes()
    builtins = builtins if builtins is not None else load_builtins()
    prior_words = prior_words if prior_words is not None else []

    seen: set[str] = set()
    out: list[dict[str, str]] = []

    def add(term: str, entry_id: str = "", pos: str = "", source: str = "") -> None:
        key = term.lower()
        if not term or key in seen:
            return
        seen.add(key)
        out.append(
            {
                "term": term,
                "id": entry_id or term,
                "posTag": pos or "",
                "source": source,
            }
        )

    for p in primes:
        add(str(p.get("term") or ""), pos=str(p.get("posTag") or ""), source="prime")
    for b in builtins:
        add(
            str(b.get("term") or ""),
            entry_id=str(b.get("id") or ""),
            pos=str(b.get("posTag") or ""),
            source="builtin",
        )
    for w in prior_words:
        rank = w.get("rank")
        try:
            r = int(rank)
        except (TypeError, ValueError):
            continue
        if r < target_rank:
            add(str(w.get("word") or w.get("term") or ""), source="prior")
    return out


def parse_nsm_response(text: str) -> dict[str, Any] | None:
    if not text:
        return None
    m = DESCRIPTOR_FENCE.search(text)
    blob = m.group(1).strip() if m else None
    if not blob:
        # try raw JSON object
        start = text.find("{")
        end = text.rfind("}")
        if start >= 0 and end > start:
            blob = text[start : end + 1]
    if not blob:
        return None
    try:
        data = json.loads(blob)
    except json.JSONDecodeError:
        return None
    if not isinstance(data, dict):
        return None
    if not data.get("lemma") or not data.get("posTag") or not data.get("mechanicalRole"):
        return None
    if not data.get("nsmDefinition"):
        # allow functionalDescription fallback
        if data.get("functionalDescription"):
            data["nsmDefinition"] = data["functionalDescription"]
        else:
            return None
    return data


def chat_completions(
    base_url: str,
    model: str,
    messages: list[dict[str, str]],
    temperature: float = 0.3,
) -> str:
    url = base_url.rstrip("/") + "/chat/completions"
    payload = {
        "model": model,
        "messages": messages,
        "temperature": temperature,
    }
    req = urllib.request.Request(
        url,
        data=json.dumps(payload).encode("utf-8"),
        headers={"Content-Type": "application/json"},
        method="POST",
    )
    try:
        with urllib.request.urlopen(req, timeout=180) as resp:
            data = json.loads(resp.read().decode())
    except urllib.error.HTTPError as e:
        body = e.read().decode() if e.fp else ""
        raise RuntimeError(f"chat_completions_failed:{e.code}:{body[:400]}") from e
    except urllib.error.URLError as e:
        raise RuntimeError(f"model_unreachable:{e}") from e
    return (data.get("choices") or [{}])[0].get("message", {}).get("content") or ""


def build_user_prompt(word: str, rank: int, allow: list[dict[str, str]]) -> str:
    # Cap allow-list size in prompt for context; keep primes+builtins priority already ordered
    max_items = 400
    slim = allow[:max_items]
    lines = [
        f"Define the English word `{word}` (common-words rank {rank}).",
        "Use only the allow-list below for composition children and NSM paraphrase atoms.",
        f"Allow-list ({len(slim)} of {len(allow)} shown):",
    ]
    for a in slim:
        lines.append(f"- {a['term']}\t{a.get('posTag') or '-'}\t{a.get('id')}\t{a.get('source')}")
    lines.append(
        "Respond with one ```json lemma-mechanism-descriptor fence including nsmDefinition."
    )
    return "\n".join(lines)


def already_defined(conn: sqlite3.Connection, term: str, language_code: str = "en") -> bool:
    row = conn.execute(
        """SELECT nsm_definition FROM lemma_completion
           WHERE language_code = ? AND term = ?""",
        (language_code, term),
    ).fetchone()
    if not row:
        return False
    return bool((row["nsm_definition"] or "").strip())


def process_word(
    word: str,
    rank: int,
    *,
    preface: str,
    allow: list[dict[str, str]],
    chat: ChatFn,
    base_url: str,
    model: str,
    temperature: float,
) -> dict[str, Any]:
    messages = [
        {"role": "system", "content": preface},
        {"role": "user", "content": build_user_prompt(word, rank, allow)},
    ]
    content = chat(base_url, model, messages, temperature)
    descriptor = parse_nsm_response(content)
    if descriptor is None:
        raise RuntimeError(f"parse_failed for {word!r}: {content[:300]}")
    return {
        "rank": rank,
        "word": word,
        "descriptor": descriptor,
        "nsmDefinition": descriptor.get("nsmDefinition"),
        "allowListSize": len(allow),
        "raw": content,
    }


def open_db(path: Path | None) -> sqlite3.Connection | None:
    if path is None:
        return None
    path.parent.mkdir(parents=True, exist_ok=True)
    conn = sqlite3.connect(path)
    conn.row_factory = sqlite3.Row
    ensure_lemma_completion_schema(conn)
    seed_lemma_completion(conn)
    return conn


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Define common words via NSM + LM Studio")
    parser.add_argument("--base-url", default="http://localhost:1234/v1")
    parser.add_argument("--model", default="mistralai/codestral-22b-v0.1")
    parser.add_argument("--temperature", type=float, default=0.3)
    parser.add_argument("--start-rank", type=int, default=1)
    parser.add_argument("--end-rank", type=int, default=5000)
    parser.add_argument("--limit", type=int, default=None)
    parser.add_argument("--resume", action="store_true", help="Skip words that already have nsm_definition")
    parser.add_argument("--dry-run", action="store_true", help="Build allow-lists only; no LM calls")
    parser.add_argument("--db", type=str, default=None, help="SQLite path for lemma_completion upserts")
    parser.add_argument(
        "--output",
        type=str,
        default=str(DEFAULT_OUT),
        help="JSONL output path",
    )
    args = parser.parse_args(argv)

    words = sorted(load_common_words(), key=lambda w: int(w.get("rank") or 0))
    primes = load_primes()
    builtins = load_builtins()
    preface = load_preface()
    out_path = Path(args.output)
    out_path.parent.mkdir(parents=True, exist_ok=True)

    conn = open_db(Path(args.db) if args.db else None)
    chat: ChatFn = chat_completions
    processed = 0
    skipped = 0

    with out_path.open("a", encoding="utf-8") as out_f:
        for w in words:
            try:
                rank = int(w.get("rank") or 0)
            except (TypeError, ValueError):
                continue
            if rank < args.start_rank or rank > args.end_rank:
                continue
            term = str(w.get("word") or "").strip()
            if not term:
                continue
            if args.resume and conn is not None and already_defined(conn, term):
                skipped += 1
                continue
            if args.limit is not None and processed >= args.limit:
                break

            allow = allow_list_for_rank(
                rank, primes=primes, builtins=builtins, prior_words=words
            )
            if args.dry_run:
                rec = {
                    "rank": rank,
                    "word": term,
                    "allowListSize": len(allow),
                    "dryRun": True,
                }
                out_f.write(json.dumps(rec, ensure_ascii=False) + "\n")
                print(f"[dry-run] rank={rank} word={term} allow={len(allow)}")
                processed += 1
                continue

            try:
                rec = process_word(
                    term,
                    rank,
                    preface=preface,
                    allow=allow,
                    chat=chat,
                    base_url=args.base_url,
                    model=args.model,
                    temperature=args.temperature,
                )
            except Exception as e:
                print(f"[error] rank={rank} word={term}: {e}", file=sys.stderr)
                continue

            slim = {
                "rank": rec["rank"],
                "word": rec["word"],
                "descriptor": rec["descriptor"],
                "nsmDefinition": rec["nsmDefinition"],
                "allowListSize": rec["allowListSize"],
            }
            out_f.write(json.dumps(slim, ensure_ascii=False) + "\n")
            out_f.flush()

            if conn is not None:
                desc = rec["descriptor"]
                upsert_definition(
                    conn,
                    term=term,
                    rank=rank,
                    nsm_definition=str(rec["nsmDefinition"] or ""),
                    composition=desc.get("compositionChildren"),
                    descriptor=desc,
                )
            print(f"[ok] rank={rank} word={term} allow={rec['allowListSize']}")
            processed += 1

    if conn is not None:
        conn.close()
    print(f"Done. processed={processed} skipped={skipped} output={out_path}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
