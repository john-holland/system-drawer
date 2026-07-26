"""Language-pair change-of-basis rewrite engine with validation and loop guards."""

from __future__ import annotations

import hashlib
import json
import sqlite3
import uuid
from typing import Any

try:
    from continuuuum_api.nsm_wiring_db import ensure_nsm_schema
except ImportError:
    from nsm_wiring_db import ensure_nsm_schema


def _new_id(prefix: str = "cob") -> str:
    return f"{prefix}_{uuid.uuid4().hex[:12]}"


def _parse_json(raw: Any) -> dict[str, Any]:
    if raw is None:
        return {}
    if isinstance(raw, dict):
        return raw
    if isinstance(raw, str) and raw.strip():
        try:
            v = json.loads(raw)
            return v if isinstance(v, dict) else {}
        except json.JSONDecodeError:
            return {}
    return {}


def get_engine_defaults(conn: sqlite3.Connection) -> dict[str, Any]:
    ensure_nsm_schema(conn)
    row = conn.execute(
        "SELECT * FROM change_of_basis_engine_defaults WHERE id = 'global' LIMIT 1"
    ).fetchone()
    if not row:
        return {
            "max_global_passes": 32,
            "max_rule_applications": 8,
            "max_clause_expansions": 64,
            "warn_on_loop": 1,
            "fail_on_loop": 0,
            "require_validation": 0,
        }
    return dict(row)


def set_engine_defaults(conn: sqlite3.Connection, body: dict[str, Any]) -> dict[str, Any]:
    ensure_nsm_schema(conn)
    cur = get_engine_defaults(conn)
    fields = (
        "max_global_passes",
        "max_rule_applications",
        "max_clause_expansions",
        "warn_on_loop",
        "fail_on_loop",
        "require_validation",
    )
    vals = []
    for f in fields:
        camel = "".join(
            w.capitalize() if i else w for i, w in enumerate(f.split("_"))
        )
        # accept both snake and camel
        if f in body:
            cur[f] = int(body[f])
        else:
            # camelCase variants
            alt = {
                "max_global_passes": "maxGlobalPasses",
                "max_rule_applications": "maxRuleApplications",
                "max_clause_expansions": "maxClauseExpansions",
                "warn_on_loop": "warnOnLoop",
                "fail_on_loop": "failOnLoop",
                "require_validation": "requireValidation",
            }[f]
            if alt in body:
                cur[f] = int(body[alt])
        vals.append(cur[f])
    conn.execute(
        """INSERT INTO change_of_basis_engine_defaults (
            id, max_global_passes, max_rule_applications, max_clause_expansions,
            warn_on_loop, fail_on_loop, require_validation
        ) VALUES ('global',?,?,?,?,?,?)
        ON CONFLICT(id) DO UPDATE SET
            max_global_passes=excluded.max_global_passes,
            max_rule_applications=excluded.max_rule_applications,
            max_clause_expansions=excluded.max_clause_expansions,
            warn_on_loop=excluded.warn_on_loop,
            fail_on_loop=excluded.fail_on_loop,
            require_validation=excluded.require_validation
        """,
        tuple(vals),
    )
    conn.commit()
    return get_engine_defaults(conn)


def list_rules(
    conn: sqlite3.Connection,
    source_language_id: str | None = None,
    target_language_id: str | None = None,
) -> list[dict[str, Any]]:
    ensure_nsm_schema(conn)
    q = "SELECT * FROM change_of_basis_rules WHERE 1=1"
    args: list[Any] = []
    if source_language_id:
        q += " AND (source_language_id IS NULL OR source_language_id = ?)"
        args.append(source_language_id)
    if target_language_id:
        q += " AND target_language_id = ?"
        args.append(target_language_id)
    q += " ORDER BY COALESCE(priority, 100), id"
    return [dict(r) for r in conn.execute(q, args).fetchall()]


def upsert_rule(conn: sqlite3.Connection, body: dict[str, Any]) -> dict[str, Any]:
    ensure_nsm_schema(conn)
    rid = body.get("id") or _new_id("rule")
    act = body.get("activation") or body.get("activation_json")
    rep = body.get("replacement") or body.get("replacement_json")
    if isinstance(act, dict):
        act = json.dumps(act)
    if isinstance(rep, dict):
        rep = json.dumps(rep)
    existing = conn.execute(
        "SELECT id FROM change_of_basis_rules WHERE id = ?", (rid,)
    ).fetchone()
    cols = {
        "target_language_id": body.get("target_language_id") or body.get("targetLanguageId"),
        "source_language_id": body.get("source_language_id") or body.get("sourceLanguageId"),
        "clause_kind": body.get("clause_kind") or body.get("clauseKind"),
        "clause_position": body.get("clause_position") or body.get("clausePosition"),
        "before_pos_whitelist": body.get("before_pos_whitelist") or body.get("beforePosWhitelist"),
        "before_pos_blacklist": body.get("before_pos_blacklist") or body.get("beforePosBlacklist"),
        "after_pos_whitelist": body.get("after_pos_whitelist") or body.get("afterPosWhitelist"),
        "after_pos_blacklist": body.get("after_pos_blacklist") or body.get("afterPosBlacklist"),
        "reorder_action": body.get("reorder_action") or body.get("reorderAction") or "none",
        "priority": body.get("priority", 100),
        "source_pos": body.get("source_pos") or body.get("sourcePos"),
        "conjugation_mood": body.get("conjugation_mood") or body.get("conjugationMood"),
        "conjugation_tense": body.get("conjugation_tense") or body.get("conjugationTense"),
        "conjugation_person": body.get("conjugation_person") or body.get("conjugationPerson"),
        "conjugation_number": body.get("conjugation_number") or body.get("conjugationNumber"),
        "activation_json": act,
        "replacement_json": rep,
        "match_mode": body.get("match_mode") or body.get("matchMode") or "sequence",
        "max_applications": body.get("max_applications") or body.get("maxApplications"),
        "enabled": int(body.get("enabled", 1)),
    }
    if not cols["target_language_id"]:
        raise ValueError("target_language_id required")
    if existing:
        sets = ", ".join(f"{k}=?" for k in cols)
        conn.execute(
            f"UPDATE change_of_basis_rules SET {sets} WHERE id=?",
            (*cols.values(), rid),
        )
    else:
        conn.execute(
            f"""INSERT INTO change_of_basis_rules (id, {', '.join(cols)})
                VALUES (?, {', '.join('?' for _ in cols)})""",
            (rid, *cols.values()),
        )
    conn.commit()
    row = conn.execute("SELECT * FROM change_of_basis_rules WHERE id=?", (rid,)).fetchone()
    return dict(row) if row else {"id": rid}


def list_conjugations(
    conn: sqlite3.Connection,
    target_language_id: str | None = None,
    lemma: str | None = None,
) -> list[dict[str, Any]]:
    ensure_nsm_schema(conn)
    q = "SELECT * FROM change_of_basis_conjugations WHERE 1=1"
    args: list[Any] = []
    if target_language_id:
        q += " AND target_language_id = ?"
        args.append(target_language_id)
    if lemma:
        q += " AND lower(lemma_term) = lower(?)"
        args.append(lemma)
    q += " ORDER BY lemma_term, mood, tense, person, number LIMIT 2000"
    return [dict(r) for r in conn.execute(q, args).fetchall()]


def upsert_conjugation(conn: sqlite3.Connection, body: dict[str, Any]) -> dict[str, Any]:
    ensure_nsm_schema(conn)
    cid = body.get("id") or _new_id("conj")
    cols = {
        "target_language_id": body.get("target_language_id") or body.get("targetLanguageId"),
        "entry_id": body.get("entry_id") or body.get("entryId"),
        "lemma_term": body.get("lemma_term") or body.get("lemmaTerm") or body.get("lemma"),
        "pos_tag": body.get("pos_tag") or body.get("posTag") or "verb",
        "mood": body.get("mood") or "indicative",
        "tense": body.get("tense") or "present",
        "person": str(body.get("person") or "3"),
        "number": body.get("number") or "singular",
        "aspect": body.get("aspect") or "none",
        "politeness": body.get("politeness") or "plain",
        "polarity": body.get("polarity") or "affirmative",
        "voice": body.get("voice") or "active",
        "formality": body.get("formality") or "plain",
        "target_form": body.get("target_form") or body.get("targetForm"),
    }
    if not cols["target_language_id"] or not cols["lemma_term"] or not cols["target_form"]:
        raise ValueError("target_language_id, lemma_term, and target_form required")
    existing = conn.execute(
        "SELECT id FROM change_of_basis_conjugations WHERE id = ?", (cid,)
    ).fetchone()
    if existing:
        sets = ", ".join(f"{k}=?" for k in cols)
        conn.execute(
            f"UPDATE change_of_basis_conjugations SET {sets} WHERE id=?",
            (*cols.values(), cid),
        )
    else:
        try:
            conn.execute(
                f"""INSERT INTO change_of_basis_conjugations (id, {', '.join(cols)})
                    VALUES (?, {', '.join('?' for _ in cols)})""",
                (cid, *cols.values()),
            )
        except sqlite3.IntegrityError:
            # Upsert on unique slot key
            conn.execute(
                """UPDATE change_of_basis_conjugations SET target_form = ?, id = ?
                   WHERE target_language_id = ? AND lower(lemma_term) = lower(?)
                     AND pos_tag = ? AND mood = ? AND tense = ? AND person = ? AND number = ?
                     AND COALESCE(aspect,'none') = ? AND COALESCE(politeness,'plain') = ?
                     AND COALESCE(polarity,'affirmative') = ? AND COALESCE(voice,'active') = ?
                     AND COALESCE(formality,'plain') = ?""",
                (
                    cols["target_form"],
                    cid,
                    cols["target_language_id"],
                    cols["lemma_term"],
                    cols["pos_tag"],
                    cols["mood"],
                    cols["tense"],
                    cols["person"],
                    cols["number"],
                    cols["aspect"],
                    cols["politeness"],
                    cols["polarity"],
                    cols["voice"],
                    cols["formality"],
                ),
            )
    conn.commit()
    row = conn.execute("SELECT * FROM change_of_basis_conjugations WHERE id=?", (cid,)).fetchone()
    return dict(row) if row else {"id": cid}


def _lang_code_and_ref(conn: sqlite3.Connection, target_language_id: str) -> tuple[str | None, str | None]:
    try:
        row = conn.execute(
            "SELECT code, morphology_rules_ref FROM languages WHERE id = ? LIMIT 1",
            (target_language_id,),
        ).fetchone()
        if not row:
            return None, None
        if isinstance(row, sqlite3.Row):
            return row["code"], row["morphology_rules_ref"] if "morphology_rules_ref" in row.keys() else None
        return row[0], row[1] if len(row) > 1 else None
    except sqlite3.OperationalError:
        try:
            row = conn.execute(
                "SELECT code FROM languages WHERE id = ? LIMIT 1",
                (target_language_id,),
            ).fetchone()
            if row:
                return (row["code"] if isinstance(row, sqlite3.Row) else row[0]), None
        except sqlite3.OperationalError:
            pass
    return None, None


def resolve_conjugation(
    conn: sqlite3.Connection,
    target_language_id: str,
    lemma: str,
    pos_tag: str = "verb",
    mood: str = "indicative",
    tense: str = "present",
    person: str = "3",
    number: str = "singular",
    *,
    aspect: str = "none",
    politeness: str = "plain",
    polarity: str = "affirmative",
    voice: str = "active",
    formality: str = "plain",
    honorific: str = "0",
    slots: dict[str, Any] | None = None,
) -> str | None:
    """Resolve surface form: DB override → default-slot alternatives → generative morph."""
    if slots:
        mood = str(slots.get("mood") or mood)
        tense = str(slots.get("tense") or tense)
        person = str(slots.get("person") or person)
        number = str(slots.get("number") or number)
        aspect = str(slots.get("aspect") or aspect)
        politeness = str(slots.get("politeness") or politeness)
        polarity = str(slots.get("polarity") or polarity)
        voice = str(slots.get("voice") or voice)
        formality = str(slots.get("formality") or formality)
        honorific = str(slots.get("honorific") or honorific)

    # 1) Exact / slot-aware DB override
    try:
        cur = conn.execute(
            """SELECT target_form FROM change_of_basis_conjugations
               WHERE target_language_id = ? AND lower(lemma_term) = lower(?)
                 AND lower(pos_tag) = lower(?) AND mood = ? AND tense = ?
                 AND person = ? AND number = ?
                 AND COALESCE(aspect, 'none') = ?
                 AND COALESCE(politeness, 'plain') = ?
                 AND COALESCE(polarity, 'affirmative') = ?
                 AND COALESCE(voice, 'active') = ?
                 AND COALESCE(formality, 'plain') = ?
               LIMIT 1""",
            (
                target_language_id,
                lemma,
                pos_tag,
                mood,
                tense,
                person,
                number,
                aspect or "none",
                politeness or "plain",
                polarity or "affirmative",
                voice or "active",
                formality or "plain",
            ),
        ).fetchone()
        if cur:
            return cur[0] if not isinstance(cur, sqlite3.Row) else cur["target_form"]
    except sqlite3.OperationalError:
        # Pre-migration schema without extended columns
        try:
            cur = conn.execute(
                """SELECT target_form FROM change_of_basis_conjugations
                   WHERE target_language_id = ? AND lower(lemma_term) = lower(?)
                     AND lower(pos_tag) = lower(?) AND mood = ? AND tense = ?
                     AND person = ? AND number = ?
                   LIMIT 1""",
                (target_language_id, lemma, pos_tag, mood, tense, person, number),
            ).fetchone()
            if cur:
                return cur[0] if not isinstance(cur, sqlite3.Row) else cur["target_form"]
        except sqlite3.OperationalError:
            pass

    # 2) thesaurus_alternatives only for default indicative present 3sg (slot-safe)
    is_default_slots = (
        mood == "indicative"
        and tense == "present"
        and person == "3"
        and number == "singular"
        and (aspect or "none") in ("", "none")
        and (politeness or "plain") in ("", "plain")
        and (polarity or "affirmative") in ("", "affirmative")
        and (voice or "active") in ("", "active")
    )
    if is_default_slots:
        try:
            cur = conn.execute(
                """SELECT a.form FROM thesaurus_alternatives a
                   JOIN thesaurus_entries e ON e.id = a.entry_id
                   WHERE e.language_id = ? AND lower(e.term) = lower(?)
                     AND lower(COALESCE(a.role,'')) = 'conjugation'
                     AND (a.pos_tag IS NULL OR lower(a.pos_tag) = lower(?))
                   LIMIT 1""",
                (target_language_id, lemma, pos_tag),
            ).fetchone()
            if cur:
                return cur[0] if not isinstance(cur, sqlite3.Row) else cur["form"]
        except sqlite3.OperationalError:
            pass

    # 3) Generative morphology
    lang_code, rules_ref = _lang_code_and_ref(conn, target_language_id)
    if lang_code and lemma:
        try:
            try:
                from continuuuum_api.morphology import conjugate as morph_conjugate
            except ImportError:
                from morphology import conjugate as morph_conjugate

            form = morph_conjugate(
                lang_code,
                lemma,
                {
                    "mood": mood,
                    "tense": tense,
                    "person": person,
                    "number": number,
                    "aspect": aspect,
                    "politeness": politeness,
                    "polarity": polarity,
                    "voice": voice,
                    "formality": formality,
                    "honorific": honorific,
                },
                rules_ref=rules_ref,
            )
            if form:
                return form
        except Exception:
            pass
    return None


def _bind_tokens(
    conn: sqlite3.Connection, tokens: list[str], source_language_id: str | None
) -> list[dict[str, Any]]:
    bound = []
    for tok in tokens:
        entry_id = None
        pos_tag = None
        if source_language_id:
            try:
                row = conn.execute(
                    """SELECT id, pos_tag FROM thesaurus_entries
                       WHERE language_id = ? AND (term = ? OR term = ?) LIMIT 1""",
                    (source_language_id, tok.lower(), tok),
                ).fetchone()
                if row:
                    entry_id = row["id"] if isinstance(row, sqlite3.Row) else row[0]
                    pos_tag = row["pos_tag"] if isinstance(row, sqlite3.Row) else row[1]
            except sqlite3.OperationalError:
                pass
        bound.append({"term": tok, "entryId": entry_id, "pos": pos_tag})
    return bound


def _span_hash(rule_id: str, start: int, end: int, terms: list[str]) -> str:
    raw = f"{rule_id}:{start}:{end}:{'|'.join(t.lower() for t in terms)}"
    return hashlib.sha1(raw.encode("utf-8")).hexdigest()[:16]


def _match_sequence(
    bound: list[dict[str, Any]], activation: dict[str, Any]
) -> list[tuple[int, int, dict[str, list[str]]]]:
    """Return list of (start, end_exclusive, captures)."""
    pattern = list(activation.get("tokens") or [])
    if not pattern:
        # wordIds only
        word_ids = [str(x) for x in (activation.get("wordIds") or [])]
        if not word_ids:
            return []
        require_all = bool(activation.get("requireAllWordIds"))
        ids_in = {b.get("entryId") for b in bound if b.get("entryId")}
        if require_all and not all(w in ids_in for w in word_ids):
            return []
        if not require_all and not any(w in ids_in for w in word_ids):
            return []
        # Match contiguous span covering first matching id through last
        idxs = [i for i, b in enumerate(bound) if b.get("entryId") in word_ids]
        if not idxs:
            return []
        return [(min(idxs), max(idxs) + 1, {})]

    matches: list[tuple[int, int, dict[str, list[str]]]] = []
    n = len(bound)
    i = 0
    while i < n:
        j = i
        captures: dict[str, list[str]] = {}
        ok = True
        for pat in pattern:
            if j >= n:
                ok = False
                break
            if pat.get("wildcard") == "span":
                cap = str(pat.get("capture") or "span")
                mn = int(pat.get("min", 1))
                mx = int(pat.get("max", 6))
                take = min(mx, n - j)
                if take < mn:
                    ok = False
                    break
                # greedy minimal for remaining pattern length
                remaining = len(pattern) - (pattern.index(pat) + 1)
                take = max(mn, min(take, n - j - remaining))
                captures[cap] = [bound[k]["term"] for k in range(j, j + take)]
                j += take
                continue
            b = bound[j]
            if pat.get("entryId") and b.get("entryId") != pat.get("entryId"):
                ok = False
                break
            if pat.get("term") and b["term"].lower() != str(pat["term"]).lower():
                ok = False
                break
            if pat.get("pos") and b.get("pos") and str(b["pos"]).lower() != str(pat["pos"]).lower():
                ok = False
                break
            j += 1
        if ok:
            matches.append((i, j, captures))
            i = j
        else:
            i += 1
    return matches


def _apply_replacement(
    conn: sqlite3.Connection,
    replacement: dict[str, Any],
    captures: dict[str, list[str]],
    target_language_id: str,
    conjugation: dict[str, str] | None,
) -> list[str]:
    out: list[str] = []
    conj_defaults = conjugation or {}
    for tok in replacement.get("tokens") or []:
        if tok.get("capture"):
            out.extend(captures.get(str(tok["capture"]), []))
            continue
        if tok.get("conj"):
            c = tok["conj"]
            slot_bag = {
                "mood": c.get("mood") or conj_defaults.get("mood") or "indicative",
                "tense": c.get("tense") or conj_defaults.get("tense") or "present",
                "person": c.get("person") or conj_defaults.get("person") or "3",
                "number": c.get("number") or conj_defaults.get("number") or "singular",
                "aspect": c.get("aspect") or conj_defaults.get("aspect") or "none",
                "politeness": c.get("politeness") or conj_defaults.get("politeness") or "plain",
                "polarity": c.get("polarity") or conj_defaults.get("polarity") or "affirmative",
                "voice": c.get("voice") or conj_defaults.get("voice") or "active",
                "formality": c.get("formality") or conj_defaults.get("formality") or "plain",
                "honorific": c.get("honorific") or conj_defaults.get("honorific") or "0",
            }
            form = resolve_conjugation(
                conn,
                target_language_id,
                str(c.get("lemma") or ""),
                str(c.get("pos") or c.get("pos_tag") or "verb"),
                slots=slot_bag,
            )
            # zh (and others) may return multi-token strings
            if form and " " in form:
                out.extend(form.split())
            else:
                out.append(form or str(c.get("lemma") or "?"))
            continue
        if tok.get("term"):
            out.append(str(tok["term"]))
    return out


def _lexicon_fill(
    conn: sqlite3.Connection,
    tokens: list[str],
    source_language_id: str | None,
    target_language_id: str,
    overrides: dict[tuple[str, str], str | None],
) -> list[str]:
    result = []
    for token in tokens:
        used = False
        for ctx in ("default", "place", "person"):
            if (token.lower(), ctx) in overrides:
                val = overrides[(token.lower(), ctx)]
                result.append(token if val is None else val)
                used = True
                break
        if used:
            continue
        entry_id = None
        if source_language_id:
            try:
                entry = conn.execute(
                    """SELECT id FROM thesaurus_entries
                       WHERE language_id = ? AND (term = ? OR term = ?) LIMIT 1""",
                    (source_language_id, token.lower(), token),
                ).fetchone()
                if entry:
                    entry_id = entry["id"] if isinstance(entry, sqlite3.Row) else entry[0]
            except sqlite3.OperationalError:
                pass
        if entry_id:
            try:
                tr = conn.execute(
                    """SELECT form FROM thesaurus_translations
                       WHERE entry_id = ? AND language_id = ? LIMIT 1""",
                    (entry_id, target_language_id),
                ).fetchone()
                if tr:
                    result.append(tr["form"] if isinstance(tr, sqlite3.Row) else tr[0])
                    continue
            except sqlite3.OperationalError:
                pass
        result.append(token)
    return result


def apply_change_of_basis(
    conn: sqlite3.Connection,
    script_text: str,
    source_language_id: str | None,
    target_language_id: str,
    *,
    conjugation: dict[str, str] | None = None,
    max_global_passes: int | None = None,
    dry_run: bool = False,
) -> dict[str, Any]:
    ensure_nsm_schema(conn)
    defaults = get_engine_defaults(conn)
    max_passes = int(max_global_passes or defaults["max_global_passes"])
    max_rule = int(defaults["max_rule_applications"])
    warn_on_loop = bool(defaults["warn_on_loop"])
    fail_on_loop = bool(defaults["fail_on_loop"])

    tokens = script_text.split()
    if not tokens:
        return {"scriptText": "", "appliedRules": [], "warnings": [], "stats": {"passes": 0}}

    overrides: dict[tuple[str, str], str | None] = {}
    try:
        for r in conn.execute(
            "SELECT term, context_type, target_form FROM change_of_basis_word_overrides WHERE target_language_id = ?",
            (target_language_id,),
        ).fetchall():
            overrides[(r["term"].lower(), r["context_type"])] = r["target_form"]
    except sqlite3.OperationalError:
        pass

    rules = [
        r
        for r in list_rules(conn, source_language_id, target_language_id)
        if int(r.get("enabled") if r.get("enabled") is not None else 1) == 1
    ]
    applied: list[dict[str, Any]] = []
    warnings: list[dict[str, Any]] = []
    rule_counts: dict[str, int] = {}
    seen_spans: set[str] = set()
    disabled_rules: set[str] = set()

    for pass_i in range(max_passes):
        bound = _bind_tokens(conn, tokens, source_language_id)
        changed = False
        for rule in rules:
            rid = rule["id"]
            if rid in disabled_rules:
                continue
            limit = rule.get("max_applications")
            limit = int(limit) if limit is not None else max_rule
            if rule_counts.get(rid, 0) >= limit:
                continue
            activation = _parse_json(rule.get("activation_json"))
            replacement = _parse_json(rule.get("replacement_json"))
            if not activation and not replacement:
                # Legacy reorder: swap_with_prev on source_pos
                if (rule.get("reorder_action") or "none") == "swap_with_prev" and rule.get("source_pos"):
                    for i in range(1, len(bound)):
                        if bound[i].get("pos") and str(bound[i]["pos"]).lower() == str(rule["source_pos"]).lower():
                            tokens[i - 1], tokens[i] = tokens[i], tokens[i - 1]
                            applied.append({"ruleId": rid, "pass": pass_i, "action": "swap_with_prev", "at": i})
                            rule_counts[rid] = rule_counts.get(rid, 0) + 1
                            changed = True
                            break
                continue
            matches = _match_sequence(bound, activation)
            for start, end, captures in matches:
                sh = _span_hash(rid, start, end, [t["term"] for t in bound[start:end]])
                if sh in seen_spans:
                    if warn_on_loop:
                        warnings.append(
                            {
                                "type": "loop_warning",
                                "ruleId": rid,
                                "spanHash": sh,
                                "pass": pass_i,
                            }
                        )
                    disabled_rules.add(rid)
                    if fail_on_loop:
                        return {
                            "scriptText": " ".join(tokens),
                            "appliedRules": applied,
                            "warnings": warnings,
                            "stats": {"passes": pass_i + 1, "aborted": True},
                            "error": "loop_detected",
                        }
                    break
                if not replacement:
                    continue
                # Rule-level conjugation_* fills between request bag and hard defaults
                rule_conj = {
                    k: v
                    for k, v in {
                        "mood": rule.get("conjugation_mood"),
                        "tense": rule.get("conjugation_tense"),
                        "person": rule.get("conjugation_person"),
                        "number": rule.get("conjugation_number"),
                    }.items()
                    if v
                }
                merged_conj = {**(rule_conj or {}), **(conjugation or {})}
                new_toks = _apply_replacement(
                    conn, replacement, captures, target_language_id, merged_conj
                )
                tokens = tokens[:start] + new_toks + tokens[end:]
                seen_spans.add(sh)
                rule_counts[rid] = rule_counts.get(rid, 0) + 1
                applied.append(
                    {
                        "ruleId": rid,
                        "pass": pass_i,
                        "start": start,
                        "end": end,
                        "replacement": new_toks,
                    }
                )
                changed = True
                break
            if changed:
                break
        if not changed:
            break

    filled = _lexicon_fill(conn, tokens, source_language_id, target_language_id, overrides)
    return {
        "scriptText": " ".join(filled),
        "appliedRules": applied,
        "warnings": warnings,
        "stats": {
            "passes": pass_i + 1 if tokens else 0,
            "ruleCounts": rule_counts,
            "dryRun": dry_run,
        },
    }


def validate_ruleset(
    conn: sqlite3.Connection,
    source_language_id: str | None,
    target_language_id: str,
) -> dict[str, Any]:
    ensure_nsm_schema(conn)
    rules = list_rules(conn, source_language_id, target_language_id)
    errors: list[dict[str, Any]] = []
    warnings: list[dict[str, Any]] = []
    graph: dict[str, set[str]] = {}

    for rule in rules:
        rid = rule["id"]
        if int(rule.get("enabled") if rule.get("enabled") is not None else 1) != 1:
            continue
        activation = _parse_json(rule.get("activation_json"))
        replacement = _parse_json(rule.get("replacement_json"))
        if not activation and (rule.get("reorder_action") or "none") == "none":
            errors.append({"ruleId": rid, "error": "empty_activation"})
        for tok in activation.get("tokens") or []:
            eid = tok.get("entryId")
            if eid:
                row = conn.execute(
                    "SELECT 1 FROM thesaurus_entries WHERE id = ? LIMIT 1", (eid,)
                ).fetchone()
                if not row:
                    errors.append({"ruleId": rid, "error": "dangling_activation_entryId", "entryId": eid})
            term = tok.get("term")
            if term and source_language_id and not eid:
                row = conn.execute(
                    """SELECT 1 FROM thesaurus_entries
                       WHERE language_id = ? AND lower(term) = lower(?) LIMIT 1""",
                    (source_language_id, term),
                ).fetchone()
                if not row:
                    warnings.append(
                        {"ruleId": rid, "warning": "activation_term_not_in_lexicon", "term": term}
                    )
        for tok in replacement.get("tokens") or []:
            eid = tok.get("entryId")
            if eid:
                row = conn.execute(
                    "SELECT 1 FROM thesaurus_entries WHERE id = ? LIMIT 1", (eid,)
                ).fetchone()
                if not row:
                    errors.append({"ruleId": rid, "error": "dangling_replacement_entryId", "entryId": eid})
            if tok.get("conj"):
                lemma = str(tok["conj"].get("lemma") or "")
                c = tok["conj"]
                form = resolve_conjugation(
                    conn,
                    target_language_id,
                    lemma,
                    str(c.get("pos") or "verb"),
                    slots={
                        "mood": c.get("mood") or "indicative",
                        "tense": c.get("tense") or "present",
                        "person": c.get("person") or "3",
                        "number": c.get("number") or "singular",
                        "aspect": c.get("aspect") or "none",
                        "politeness": c.get("politeness") or "plain",
                        "polarity": c.get("polarity") or "affirmative",
                        "voice": c.get("voice") or "active",
                        "formality": c.get("formality") or "plain",
                    },
                )
                if not form:
                    warnings.append(
                        {"ruleId": rid, "warning": "missing_conjugation", "lemma": lemma}
                    )
        # Graph edges: activation terms -> replacement terms
        act_terms = [str(t.get("term")).lower() for t in (activation.get("tokens") or []) if t.get("term")]
        rep_terms = [str(t.get("term")).lower() for t in (replacement.get("tokens") or []) if t.get("term")]
        for a in act_terms:
            graph.setdefault(a, set()).update(rep_terms)

    # Detect simple 2-cycles A->B->A
    cycles = []
    for a, outs in graph.items():
        for b in outs:
            if a in graph.get(b, set()):
                pair = tuple(sorted((a, b)))
                if pair not in cycles:
                    cycles.append(pair)
                    warnings.append(
                        {"type": "loop_warning", "cycle": list(pair), "message": f"{pair[0]} <-> {pair[1]}"}
                    )

    if errors:
        completion = "incomplete"
    elif cycles:
        completion = "divergent"
    else:
        completion = "complete"

    return {
        "completion": completion,
        "errors": errors,
        "warnings": warnings,
        "ruleCount": len(rules),
    }
