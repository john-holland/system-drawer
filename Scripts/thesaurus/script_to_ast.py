"""
Build thesaurus AST from episode script text. Detects quote blocks (text between "...")
and creates AST nodes with node_kind 'token' or 'quote'; quote nodes have child token nodes.
Uses Farey intervals for ordering. Requires thesaurus_ast_nodes (with node_kind, quote_id
from continuuuum_screenplay_schema.sql).
"""

from __future__ import annotations

import re
import uuid
from typing import Any

from . import farey_ast


#todo: review: again!

def _tokenize(text: str) -> list[str]:
    """Split on whitespace, preserving tokens."""
    return text.split()


def _detect_quote_spans(tokens: list[str]) -> list[tuple[int, int]]:
    """
    Detect double-quoted spans: (start_idx, end_idx) for each contiguous quote.
    A token containing " opens or closes a quote; we merge adjacent spans.
    """
    spans: list[tuple[int, int]] = []
    in_quote = False
    start = -1
    for i, t in enumerate(tokens):
        if '"' not in t:
            continue
        if t.startswith('"'):
            if in_quote:
                spans.append((start, i))
                in_quote = False
            else:
                in_quote = True
                start = i
        if t.endswith('"') and in_quote:
            spans.append((start, i))
            in_quote = False
    if in_quote:
        spans.append((start, len(tokens) - 1))
    return spans


def _segment_script(tokens: list[str]) -> list[dict[str, Any]]:
    """
    Segment into a list of items: each item is either
    - {"type": "token", "token": str, "index": int}
    - {"type": "quote", "start": int, "end": int, "tokens": list[str]}
    """
    spans = _detect_quote_spans(tokens)
    merged: list[tuple[int, int]] = []
    for s, e in spans:
        if merged and s <= merged[-1][1] + 1:
            merged[-1] = (merged[-1][0], max(merged[-1][1], e))
        else:
            merged.append((s, e))
    result: list[dict[str, Any]] = []
    i = 0
    while i < len(tokens):
        in_span = None
        for s, e in merged:
            if s <= i <= e:
                in_span = (s, e)
                break
        if in_span is not None:
            s, e = in_span
            quote_tokens = [t.strip('"') for t in tokens[s : e + 1]]
            result.append({"type": "quote", "start": s, "end": e, "tokens": quote_tokens})
            i = e + 1
        else:
            result.append({"type": "token", "token": tokens[i], "index": i})
            i += 1
    return result


def build_ast_from_script(
    conn,
    episode_script_id: str,
    script_text: str,
    language_id: str,
) -> list[str]:
    """
    Build AST nodes from script text. Detects quote blocks ("..."), creates
    thesaurus_ast_nodes with node_kind 'token' or 'quote', assigns Farey intervals.
    Returns list of root-level node ids created. Existing nodes for this
    episode_script_id are deleted first.
    """
    tokens = _tokenize(script_text.strip())
    if not tokens:
        return []
    segments = _segment_script(tokens)
    # Delete existing AST nodes for this script
    conn.execute("DELETE FROM thesaurus_ast_nodes WHERE episode_script_id = ?", (episode_script_id,))
    # Build nodes: root-level list of nodes (each is either token node or quote parent node)
    root_nodes: list[dict[str, Any]] = []
    for seg in segments:
        if seg["type"] == "token":
            nid = "ast_" + uuid.uuid4().hex[:12]
            root_nodes.append({
                "id": nid,
                "parent_id": None,
                "token_or_phrase": seg["token"],
                "pos_tag": None,
                "node_kind": "token",
                "quote_id": None,
            })
        else:
            quote_id = "q_" + uuid.uuid4().hex[:8]
            parent_id = "ast_" + uuid.uuid4().hex[:12]
            root_nodes.append({
                "id": parent_id,
                "parent_id": None,
                "token_or_phrase": "",
                "pos_tag": None,
                "node_kind": "quote",
                "quote_id": quote_id,
            })
            # Create child token nodes for the quote (will assign Farey under parent later)
            parent_interval = None  # assign after we have root order
            children = []
            for t in seg["tokens"]:
                if not t:
                    continue
                cid = "ast_" + uuid.uuid4().hex[:12]
                children.append({
                    "id": cid,
                    "parent_id": parent_id,
                    "token_or_phrase": t,
                    "pos_tag": None,
                    "node_kind": "token",
                    "quote_id": None,
                })
            # Assign Farey to children under parent; we need parent's interval first.
            # So: first assign Farey to root_nodes, then for each quote node assign children Farey.
            # We'll do a second pass after rebalancing root_nodes.
            for c in children:
                c["_parent_quote_id"] = quote_id
            root_nodes[-1]["_children"] = children
    # Assign Farey to root-level nodes
    rebalanced_root = farey_ast.rebalance_intervals(root_nodes, None)
    # Persist root nodes (without _children)
    has_node_kind = _table_has_column(conn, "thesaurus_ast_nodes", "node_kind")
    has_quote_id = _table_has_column(conn, "thesaurus_ast_nodes", "quote_id")
    for n in rebalanced_root:
        row = (
            n["id"],
            n.get("parent_id"),
            n["farey_left_num"],
            n["farey_left_den"],
            n["farey_right_num"],
            n["farey_right_den"],
            n["token_or_phrase"],
            n.get("pos_tag"),
            language_id,
            episode_script_id,
            n.get("sort_key"),
        )
        if has_node_kind and has_quote_id:
            conn.execute(
                """INSERT INTO thesaurus_ast_nodes
                   (id, parent_id, farey_left_num, farey_left_den, farey_right_num, farey_right_den,
                    token_or_phrase, pos_tag, language_id, episode_script_id, sort_key, node_kind, quote_id)
                   VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""",
                row + (n.get("node_kind", "token"), n.get("quote_id")),
            )
        else:
            conn.execute(
                """INSERT INTO thesaurus_ast_nodes
                   (id, parent_id, farey_left_num, farey_left_den, farey_right_num, farey_right_den,
                    token_or_phrase, pos_tag, language_id, episode_script_id, sort_key)
                   VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""",
                row,
            )
        children = n.pop("_children", None)
        if children:
            parent_interval = farey_ast.FareyInterval(
                n["farey_left_num"], n["farey_left_den"],
                n["farey_right_num"], n["farey_right_den"],
            )
            rebalanced_children = farey_ast.rebalance_intervals(children, parent_interval)
            for c in rebalanced_children:
                c.setdefault("node_kind", "token")
                c.setdefault("quote_id", None)
                row = (
                    c["id"],
                    c["parent_id"],
                    c["farey_left_num"], c["farey_left_den"], c["farey_right_num"], c["farey_right_den"],
                    c["token_or_phrase"], c.get("pos_tag"), language_id, episode_script_id, c.get("sort_key"),
                )
                if has_node_kind and has_quote_id:
                    conn.execute(
                        """INSERT INTO thesaurus_ast_nodes
                           (id, parent_id, farey_left_num, farey_left_den, farey_right_num, farey_right_den,
                            token_or_phrase, pos_tag, language_id, episode_script_id, sort_key, node_kind, quote_id)
                           VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""",
                        row + (c.get("node_kind", "token"), c.get("quote_id")),
                    )
                else:
                    conn.execute(
                        """INSERT INTO thesaurus_ast_nodes
                           (id, parent_id, farey_left_num, farey_left_den, farey_right_num, farey_right_den,
                            token_or_phrase, pos_tag, language_id, episode_script_id, sort_key)
                           VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""",
                        row,
                    )
    conn.commit()
    return [n["id"] for n in rebalanced_root]


def _table_has_column(conn, table: str, column: str) -> bool:
    cur = conn.execute(f"PRAGMA table_info({table})")
    for row in cur.fetchall():
        if row[1] == column:
            return True
    return False
