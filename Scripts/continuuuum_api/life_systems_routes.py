"""Thin life-systems tool routes: mood/organ query helpers and property-spec seed data."""

from __future__ import annotations

from typing import Any, Callable

from flask import Flask, jsonify, request

GetConn = Callable[[], Any]

LIFE_PROPERTY_SPECS: list[dict[str, str | None]] = [
    {
        "key": "life-op",
        "value_type": "String",
        "default_value": "query",
        "description": "life op: set|adjust|query|buff|illness|organ",
        "allowed_values_json": '["set","adjust","query","buff","illness","organ"]',
    },
    {
        "key": "life-channel",
        "value_type": "String",
        "default_value": "",
        "description": "Life-systems channel id",
        "allowed_values_json": None,
    },
    {
        "key": "life-q",
        "value_type": "String",
        "default_value": "mood",
        "description": "Query: mood|organ|channel",
        "allowed_values_json": None,
    },
    {
        "key": "life-id",
        "value_type": "String",
        "default_value": "heart",
        "description": "Organ id",
        "allowed_values_json": None,
    },
    {
        "key": "life-difficulty",
        "value_type": "String",
        "default_value": "normal",
        "description": "easy|normal",
        "allowed_values_json": '["easy","normal"]',
    },
    {
        "key": "life-lifeForce",
        "value_type": "Float",
        "default_value": "0",
        "description": "Life force delta for buff",
        "allowed_values_json": None,
    },
    {
        "key": "life-duration",
        "value_type": "Float",
        "default_value": "0",
        "description": "Effect duration seconds",
        "allowed_values_json": None,
    },
]


def ensure_life_property_specs(conn) -> int:
    """Insert life-systems property specs if missing. Returns rows inserted."""
    inserted = 0
    for spec in LIFE_PROPERTY_SPECS:
        cur = conn.execute(
            "SELECT 1 FROM localization_property_specs WHERE key = ?",
            (spec["key"],),
        )
        if cur.fetchone():
            continue
        conn.execute(
            """INSERT INTO localization_property_specs
               (key, value_type, allowed_values_json, default_value, description)
               VALUES (?, ?, ?, ?, ?)""",
            (
                spec["key"],
                spec["value_type"],
                spec.get("allowed_values_json"),
                spec["default_value"],
                spec["description"],
            ),
        )
        inserted += 1
    conn.commit()
    return inserted


def mood_rubric(
    depression: float = 0.15,
    mania: float = 0.15,
    morale: float = 0.7,
    empathy: float = 0.65,
) -> dict[str, Any]:
    valence = max(0.0, min(1.0, 0.5 + (morale - depression) * 0.35 + (empathy - mania) * 0.15))
    label = "upbeat" if valence >= 0.7 else "even" if valence >= 0.45 else "low"
    return {
        "label": label,
        "valence": valence,
        "depression": depression,
        "mania": mania,
        "morale": morale,
        "empathy": empathy,
        "summary": (
            f"mood: {label} (valence={valence:.2f}; depression={depression:.2f}, "
            f"mania={mania:.2f}, morale={morale:.2f}, empathy={empathy:.2f})"
        ),
    }


def organ_label(normalized01: float) -> str:
    if normalized01 >= 0.95:
        return "Great"
    if normalized01 >= 0.75:
        return "Good"
    if normalized01 >= 0.5:
        return "Fair"
    if normalized01 >= 0.35:
        return "Poor"
    return "Critical"


def soft_clamp01(raw: float) -> float:
    if raw != raw:  # NaN
        return 0.0
    if raw >= 1.0:
        return 1.0
    if raw >= 0.0:
        return raw
    return 1.0 / (1.0 - raw)


def register_life_systems_routes(app: Flask, get_conn: GetConn) -> None:
    @app.route("/api/life-systems/specs/ensure", methods=["POST"])
    def life_specs_ensure():
        conn = get_conn()
        try:
            n = ensure_life_property_specs(conn)
            return jsonify({"ok": True, "inserted": n, "specs": LIFE_PROPERTY_SPECS}), 200
        finally:
            conn.close()

    @app.route("/api/life-systems/query/mood", methods=["POST", "GET"])
    def life_query_mood():
        body = request.get_json(silent=True) or {}
        args = request.args
        result = mood_rubric(
            depression=float(body.get("depression", args.get("depression", 0.15))),
            mania=float(body.get("mania", args.get("mania", 0.15))),
            morale=float(body.get("morale", args.get("morale", 0.7))),
            empathy=float(body.get("empathy", args.get("empathy", 0.65))),
        )
        return jsonify(result), 200

    @app.route("/api/life-systems/query/organ", methods=["POST", "GET"])
    def life_query_organ():
        body = request.get_json(silent=True) or {}
        organ_id = body.get("id") or request.args.get("id") or "heart"
        raw = float(body.get("raw", request.args.get("raw", 1.05)))
        easy = str(body.get("difficulty", request.args.get("difficulty", "normal"))).lower() == "easy"
        n = soft_clamp01(raw)
        if easy:
            n = max(0.15, n)
        label = organ_label(n)
        return jsonify(
            {
                "id": organ_id,
                "raw": raw,
                "normalized": n,
                "label": label,
                "summary": f"{organ_id}: {label} (normalized={n:.2f}, raw={raw:.2f})",
            }
        ), 200

    @app.route("/api/life-systems/prompt-hints", methods=["GET"])
    def life_prompt_hints():
        return jsonify(
            {
                "placeholder": "life",
                "examples": [
                    "{P:life|op=query|q=mood}",
                    "{P:life|op=query|q=organ|id=liver}",
                    "{P:life|op=buff|lifeForce=0.1|duration=300|label=supplement}",
                    "{P:life|op=set|difficulty=easy}",
                    "{P:life|op=organ|id=heart|delta=-0.4|raw=1}",
                ],
                "discoveryTokens": sorted(LIFE_DISCOVERY_TOKENS),
            }
        ), 200


LIFE_DISCOVERY_TOKENS = {
    "mood",
    "depressed",
    "depression",
    "manic",
    "mania",
    "morale",
    "empathy",
    "heart",
    "liver",
    "lungs",
    "brain",
    "organ",
    "supplement",
    "illness",
    "adrenaline",
    "hydration",
    "immune",
}
