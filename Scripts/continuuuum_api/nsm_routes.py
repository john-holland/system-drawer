"""NSM primes / associations / evaluate / fuzzy cache / seed routes."""

from __future__ import annotations

import json
from typing import Any, Callable

from flask import Flask, jsonify, request

try:
    from continuuuum_api.nsm_fuzzy_cache import (
        adjust_grade,
        clear_session,
        env_from_session,
        list_vars,
        replace_vars,
        upsert_vars_batch,
    )
    from continuuuum_api.nsm_logical_form import (
        evaluate_bool,
        evaluate_fuzzy,
        normalize,
        pretty_print,
        validate,
    )
    from continuuuum_api.nsm_wiring_db import (
        ensure_nsm_schema,
        load_primes,
        seed_nsm_prime_wiring,
    )
except ImportError:
    from nsm_fuzzy_cache import (
        adjust_grade,
        clear_session,
        env_from_session,
        list_vars,
        replace_vars,
        upsert_vars_batch,
    )
    from nsm_logical_form import evaluate_bool, evaluate_fuzzy, normalize, pretty_print, validate
    from nsm_wiring_db import (
        ensure_nsm_schema,
        load_primes,
        seed_nsm_prime_wiring,
    )

GetConn = Callable[[], Any]


def _load_hedges_map(conn, language_code: str = "en") -> dict[str, dict[str, Any]]:
    ensure_nsm_schema(conn)
    out: dict[str, dict[str, Any]] = {}
    for r in conn.execute(
        "SELECT id, phrase, aliases_json, band, curve_json, linked_primes_json FROM nsm_fuzzy_hedges WHERE language_code = ?",
        (language_code,),
    ).fetchall():
        d = dict(r)
        try:
            d["curve"] = json.loads(d.pop("curve_json") or "{}")
        except json.JSONDecodeError:
            d["curve"] = {}
        try:
            d["aliases"] = json.loads(d.pop("aliases_json") or "[]")
        except json.JSONDecodeError:
            d["aliases"] = []
        try:
            d["linked_primes"] = json.loads(d.pop("linked_primes_json") or "[]")
        except json.JSONDecodeError:
            d["linked_primes"] = []
        out[d["id"]] = d
        out[d["phrase"]] = d
        for a in d["aliases"]:
            out[str(a)] = d
    return out


def register_nsm_routes(app: Flask, get_conn: GetConn) -> None:
    @app.route("/api/nsm/primes", methods=["GET"])
    def nsm_primes():
        lang = (request.args.get("language") or "en").strip()
        conn = get_conn()
        try:
            ensure_nsm_schema(conn)
            primes = load_primes()
            # Merge completion descriptors when present
            by_term = {}
            try:
                for r in conn.execute(
                    """SELECT term, nsm_definition, descriptor_json, is_implemented
                       FROM lemma_completion WHERE language_code = ? AND is_prime = 1""",
                    (lang,),
                ).fetchall():
                    by_term[str(r["term"]).lower()] = dict(r)
            except Exception:
                pass
            out = []
            for p in primes:
                term = str(p.get("term") or "")
                row = by_term.get(term.lower(), {})
                desc = {}
                if row.get("descriptor_json"):
                    try:
                        desc = json.loads(row["descriptor_json"])
                    except json.JSONDecodeError:
                        desc = {}
                out.append(
                    {
                        **p,
                        "nsmDefinition": row.get("nsm_definition") or desc.get("nsmDefinition"),
                        "logicalForm": desc.get("logicalForm"),
                        "causalityRole": desc.get("causalityRole"),
                        "temporalRole": desc.get("temporalRole"),
                        "isImplemented": bool(row.get("is_implemented")),
                    }
                )
            return jsonify({"primes": out, "count": len(out)})
        finally:
            conn.close()

    @app.route("/api/nsm/associations", methods=["GET"])
    def nsm_associations():
        lang = (request.args.get("language") or "en").strip()
        term = (request.args.get("term") or "").strip()
        kind = (request.args.get("relationKind") or request.args.get("relation_kind") or "").strip()
        conn = get_conn()
        try:
            ensure_nsm_schema(conn)
            q = "SELECT * FROM nsm_prime_associations WHERE language_code = ?"
            args: list[Any] = [lang]
            if term:
                q += " AND (source_term = ? OR target_term = ?)"
                args.extend([term, term])
            if kind:
                q += " AND relation_kind = ?"
                args.append(kind)
            rows = [dict(r) for r in conn.execute(q, args).fetchall()]
            for r in rows:
                if r.get("math_form_json"):
                    try:
                        r["mathForm"] = json.loads(r["math_form_json"])
                    except json.JSONDecodeError:
                        pass
            return jsonify({"associations": rows, "count": len(rows)})
        finally:
            conn.close()

    @app.route("/api/nsm/evaluate", methods=["POST"])
    def nsm_evaluate():
        body = request.get_json() or {}
        form = body.get("form")
        env = dict(body.get("env") or {})
        mode = (body.get("mode") or "bool").strip().lower()
        session_id = body.get("sessionId") or body.get("session_id")
        lang = (body.get("language") or body.get("languageCode") or "en").strip()
        upserts = body.get("upsertVars") or body.get("upsert_vars") or []
        conn = get_conn()
        try:
            ensure_nsm_schema(conn)
            hedges = _load_hedges_map(conn, lang)
            cache_side = []
            if session_id and upserts:
                cache_side = upsert_vars_batch(conn, str(session_id), upserts, lang)
            if session_id:
                env.update(env_from_session(conn, str(session_id), lang))
            env["_hedges"] = hedges
            errs = validate(form)
            if errs:
                return jsonify({"ok": False, "errors": errs}), 400
            norm = normalize(form)
            if mode == "fuzzy":
                value = evaluate_fuzzy(norm, env, hedges)
            else:
                value = evaluate_bool(norm, env)
            return jsonify(
                {
                    "ok": True,
                    "value": value,
                    "mode": mode,
                    "pretty": pretty_print(norm),
                    "form": norm,
                    "cache": cache_side,
                }
            )
        finally:
            conn.close()

    @app.route("/api/nsm/fuzzy/hedges", methods=["GET"])
    def nsm_fuzzy_hedges():
        lang = (request.args.get("language") or "en").strip()
        conn = get_conn()
        try:
            hedges = _load_hedges_map(conn, lang)
            # Deduplicate by id
            by_id = {}
            for v in hedges.values():
                by_id[v["id"]] = v
            return jsonify({"hedges": list(by_id.values())})
        finally:
            conn.close()

    @app.route("/api/nsm/fuzzy/hedges/<hedge_id>", methods=["PATCH"])
    def nsm_patch_hedge(hedge_id: str):
        body = request.get_json() or {}
        conn = get_conn()
        try:
            ensure_nsm_schema(conn)
            curve = body.get("curve") or body.get("curve_json")
            if curve is None:
                return jsonify({"error": "curve required"}), 400
            if isinstance(curve, dict):
                curve = json.dumps(curve)
            from datetime import datetime, timezone

            now = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
            cur = conn.execute(
                "UPDATE nsm_fuzzy_hedges SET curve_json = ?, updated_at = ? WHERE id = ?",
                (curve, now, hedge_id),
            )
            if cur.rowcount == 0:
                return jsonify({"error": "hedge not found"}), 404
            conn.commit()
            return jsonify({"ok": True, "id": hedge_id})
        finally:
            conn.close()

    @app.route("/api/nsm/fuzzy/vars/<session_id>", methods=["GET", "PUT", "DELETE"])
    def nsm_fuzzy_vars(session_id: str):
        lang = (request.args.get("language") or (request.get_json(silent=True) or {}).get("language") or "en").strip()
        conn = get_conn()
        try:
            if request.method == "GET":
                return jsonify({"sessionId": session_id, "vars": list_vars(conn, session_id, lang)})
            if request.method == "DELETE":
                n = clear_session(conn, session_id, lang)
                return jsonify({"ok": True, "deleted": n})
            body = request.get_json() or {}
            vars_list = body.get("vars") or []
            out = replace_vars(conn, session_id, vars_list, lang)
            return jsonify({"sessionId": session_id, "vars": out})
        finally:
            conn.close()

    @app.route("/api/nsm/fuzzy/vars/<session_id>/adjust", methods=["POST"])
    def nsm_fuzzy_vars_adjust(session_id: str):
        body = request.get_json() or {}
        lang = (body.get("language") or "en").strip()
        var_key = body.get("varKey") or body.get("var_key")
        if not var_key:
            return jsonify({"error": "varKey required"}), 400
        conn = get_conn()
        try:
            row = adjust_grade(
                conn,
                session_id,
                str(var_key),
                delta=body.get("delta"),
                hedge_id=body.get("hedgeId") or body.get("hedge_id"),
                curve=body.get("curve"),
                language_code=lang,
                create_kind=str(body.get("varKind") or body.get("var_kind") or "predicate"),
            )
            return jsonify({"ok": True, "var": row})
        finally:
            conn.close()

    @app.route("/api/nsm/seed-wiring", methods=["POST"])
    def nsm_seed_wiring():
        body = request.get_json(silent=True) or {}
        lang = (body.get("language") or "en").strip()
        conn = get_conn()
        try:
            stats = seed_nsm_prime_wiring(conn, lang)
            return jsonify({"ok": True, **stats})
        finally:
            conn.close()


def register_change_of_basis_routes(app: Flask, get_conn: GetConn) -> None:
    """Additional CoB CRUD / validate / defaults (apply stays on existing path)."""
    try:
        from continuuuum_api.change_of_basis_engine import (
            get_engine_defaults,
            list_conjugations,
            list_rules,
            resolve_conjugation,
            set_engine_defaults,
            upsert_conjugation,
            upsert_rule,
            validate_ruleset,
        )
        from continuuuum_api.morphology import conjugate as morph_conjugate
    except ImportError:
        from change_of_basis_engine import (
            get_engine_defaults,
            list_conjugations,
            list_rules,
            resolve_conjugation,
            set_engine_defaults,
            upsert_conjugation,
            upsert_rule,
            validate_ruleset,
        )
        from morphology import conjugate as morph_conjugate

    @app.route("/api/thesaurus/change-of-basis/defaults", methods=["GET", "PUT"])
    def cob_defaults():
        conn = get_conn()
        try:
            if request.method == "GET":
                return jsonify(get_engine_defaults(conn))
            body = request.get_json() or {}
            return jsonify(set_engine_defaults(conn, body))
        finally:
            conn.close()

    @app.route("/api/thesaurus/change-of-basis/rules", methods=["GET", "PUT", "POST"])
    def cob_rules():
        conn = get_conn()
        try:
            if request.method == "GET":
                src = request.args.get("sourceLanguageId") or request.args.get("source_language_id")
                tgt = request.args.get("targetLanguageId") or request.args.get("target_language_id")
                return jsonify({"rules": list_rules(conn, src, tgt)})
            body = request.get_json() or {}
            rule = upsert_rule(conn, body)
            return jsonify({"ok": True, "rule": rule})
        except ValueError as e:
            return jsonify({"error": str(e)}), 400
        finally:
            conn.close()

    @app.route("/api/thesaurus/change-of-basis/validate", methods=["POST"])
    def cob_validate():
        body = request.get_json() or {}
        conn = get_conn()
        try:
            src_code = (body.get("sourceLanguage") or body.get("source_language") or "").strip()
            tgt_code = (body.get("targetLanguage") or body.get("target_language") or "").strip()
            src_id = body.get("sourceLanguageId") or body.get("source_language_id")
            tgt_id = body.get("targetLanguageId") or body.get("target_language_id")
            if tgt_code and not tgt_id:
                row = conn.execute("SELECT id FROM languages WHERE code = ? LIMIT 1", (tgt_code,)).fetchone()
                tgt_id = row["id"] if row else None
            if src_code and not src_id:
                row = conn.execute("SELECT id FROM languages WHERE code = ? LIMIT 1", (src_code,)).fetchone()
                src_id = row["id"] if row else None
            if not tgt_id:
                return jsonify({"error": "target language required"}), 400
            return jsonify(validate_ruleset(conn, src_id, tgt_id))
        finally:
            conn.close()

    @app.route("/api/thesaurus/change-of-basis/conjugations", methods=["GET", "POST"])
    def cob_conjugations():
        conn = get_conn()
        try:
            if request.method == "GET":
                tgt = request.args.get("targetLanguageId") or request.args.get("target_language_id")
                lemma = request.args.get("lemma") or request.args.get("lemmaTerm")
                return jsonify({"items": list_conjugations(conn, tgt, lemma)})
            body = request.get_json() or {}
            return jsonify({"ok": True, "conjugation": upsert_conjugation(conn, body)}), 201
        except ValueError as e:
            return jsonify({"error": str(e)}), 400
        finally:
            conn.close()

    @app.route("/api/thesaurus/change-of-basis/conjugate", methods=["POST"])
    def cob_conjugate_preview():
        """Dry morph preview: {language|languageId, lemma, slots}."""
        body = request.get_json() or {}
        lemma = (body.get("lemma") or body.get("lemmaTerm") or "").strip()
        if not lemma:
            return jsonify({"error": "lemma required"}), 400
        slots = body.get("slots") or body.get("conjugation") or {}
        lang = (body.get("language") or body.get("languageCode") or "").strip().lower()
        lang_id = body.get("languageId") or body.get("targetLanguageId")
        conn = get_conn()
        try:
            if lang_id and not lang:
                row = conn.execute("SELECT code FROM languages WHERE id = ? LIMIT 1", (lang_id,)).fetchone()
                lang = (row["code"] if row else "") or ""
            if lang_id:
                form = resolve_conjugation(
                    conn,
                    lang_id,
                    lemma,
                    str(body.get("pos") or body.get("posTag") or "verb"),
                    slots=slots if isinstance(slots, dict) else {},
                )
            else:
                form = morph_conjugate(lang or "es", lemma, slots if isinstance(slots, dict) else {})
            return jsonify({"lemma": lemma, "language": lang, "form": form, "slots": slots})
        finally:
            conn.close()
