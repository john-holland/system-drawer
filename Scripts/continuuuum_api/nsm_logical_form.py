"""NSM logical-form AST: crisp bool eval + fuzzy membership with hedge curves."""

from __future__ import annotations

import math
from typing import Any


OPS = frozenset(
    {
        "prime",
        "var",
        "not",
        "and",
        "or",
        "if",
        "because",
        "can",
        "maybe",
        "before",
        "after",
        "when",
        "like",
        "true",
        "hedge",
        "grade",
    }
)


def normalize(form: Any) -> dict[str, Any]:
    if isinstance(form, str):
        return {"op": "prime", "term": form}
    if not isinstance(form, dict):
        raise ValueError("form must be object or string")
    op = str(form.get("op") or "").strip()
    if not op:
        raise ValueError("form.op required")
    if op not in OPS:
        raise ValueError(f"unknown op: {op}")
    out: dict[str, Any] = {"op": op}
    if "term" in form:
        out["term"] = form["term"]
    if "name" in form:
        out["name"] = form["name"]
    if "hedgeId" in form:
        out["hedgeId"] = form["hedgeId"]
    if "args" in form:
        out["args"] = [normalize(a) for a in (form.get("args") or [])]
    if "value" in form:
        out["value"] = float(form["value"])
    return out


def validate(form: Any) -> list[str]:
    errors: list[str] = []
    try:
        normalize(form)
    except ValueError as e:
        errors.append(str(e))
    return errors


def pretty_print(form: Any) -> str:
    f = normalize(form)
    op = f["op"]
    if op == "prime":
        return str(f.get("term") or "?")
    if op == "var":
        return str(f.get("name") or "?")
    if op == "grade":
        return f"grade({f.get('value', 0)})"
    if op == "hedge":
        args = f.get("args") or []
        inner = pretty_print(args[0]) if args else "?"
        return f"hedge({f.get('hedgeId')}, {inner})"
    args = f.get("args") or []
    return f"{op}({', '.join(pretty_print(a) for a in args)})"


def _env_bool(env: dict[str, Any] | None, name: str) -> bool:
    if not env:
        return False
    v = env.get(name)
    if isinstance(v, bool):
        return v
    if isinstance(v, (int, float)):
        return float(v) >= 0.5
    if isinstance(v, str):
        return v.lower() in ("1", "true", "yes")
    return bool(v)


def _env_float(env: dict[str, Any] | None, name: str, default: float = 0.0) -> float:
    if not env:
        return default
    v = env.get(name, default)
    try:
        return float(v)
    except (TypeError, ValueError):
        return default


def apply_curve(curve: dict[str, Any] | None, x: float) -> float:
    x = max(0.0, min(1.0, float(x)))
    if not curve:
        return x
    kind = str(curve.get("kind") or "logistic")
    y: float
    if kind == "power":
        p = float(curve.get("p", 1.0))
        y = x**p
        scale = float(curve.get("yScale", 1.0))
        y *= scale
    elif kind == "piecewise":
        pts = list(curve.get("points") or [])
        if not pts:
            y = x
        else:
            pts = sorted(
                ((float(p["x"]), float(p["y"])) for p in pts),
                key=lambda t: t[0],
            )
            if x <= pts[0][0]:
                y = pts[0][1]
            elif x >= pts[-1][0]:
                y = pts[-1][1]
            else:
                y = pts[-1][1]
                for i in range(len(pts) - 1):
                    x0, y0 = pts[i]
                    x1, y1 = pts[i + 1]
                    if x0 <= x <= x1:
                        t = 0.0 if x1 == x0 else (x - x0) / (x1 - x0)
                        y = y0 + t * (y1 - y0)
                        break
    else:
        k = float(curve.get("k", 8.0))
        x0 = float(curve.get("x0", 0.5))
        y_min = float(curve.get("yMin", 0.0))
        y_max = float(curve.get("yMax", 1.0))
        sig = 1.0 / (1.0 + math.exp(-k * (x - x0)))
        y = y_min + sig * (y_max - y_min)
    if curve.get("clamp", True):
        y = max(0.0, min(1.0, y))
    return y


def evaluate_bool(form: Any, env: dict[str, Any] | None = None) -> bool:
    f = normalize(form)
    op = f["op"]
    args = f.get("args") or []
    if op == "true":
        return True
    if op == "prime":
        term = str(f.get("term") or "")
        return _env_bool(env, term) or _env_bool(env, f"prime:{term}")
    if op == "var":
        return _env_bool(env, str(f.get("name") or ""))
    if op == "grade":
        return float(f.get("value") or 0) >= 0.5
    if op == "not":
        return not evaluate_bool(args[0], env) if args else True
    if op == "and":
        return all(evaluate_bool(a, env) for a in args) if args else True
    if op == "or":
        return any(evaluate_bool(a, env) for a in args) if args else False
    if op == "if":
        if len(args) < 2:
            return False
        return (not evaluate_bool(args[0], env)) or evaluate_bool(args[1], env)
    if op == "because":
        if len(args) < 2:
            return evaluate_bool(args[0], env) if args else False
        return evaluate_bool(args[0], env) and evaluate_bool(args[1], env)
    if op in ("can", "maybe"):
        return evaluate_bool(args[0], env) if args else _env_bool(env, op)
    if op in ("before", "after", "when", "like"):
        # Temporal/similarity: env key or child truth
        if args:
            return evaluate_bool(args[0], env)
        return _env_bool(env, op)
    if op == "hedge":
        # Crisp path: membership >= 0.5
        return evaluate_fuzzy(f, env) >= 0.5
    return False


def evaluate_fuzzy(
    form: Any,
    env: dict[str, Any] | None = None,
    hedges: dict[str, dict[str, Any]] | None = None,
) -> float:
    f = normalize(form)
    op = f["op"]
    args = f.get("args") or []
    hedges = hedges or (env or {}).get("_hedges") or {}

    if op == "true":
        return 1.0
    if op == "grade":
        return max(0.0, min(1.0, float(f.get("value") or 0)))
    if op == "prime":
        term = str(f.get("term") or "")
        if env and f"grade:{term}" in env:
            return _env_float(env, f"grade:{term}")
        return 1.0 if evaluate_bool(f, env) else 0.0
    if op == "var":
        name = str(f.get("name") or "")
        if env and name in env and isinstance(env[name], (int, float)):
            return max(0.0, min(1.0, float(env[name])))
        return 1.0 if evaluate_bool(f, env) else 0.0
    if op == "not":
        return 1.0 - (evaluate_fuzzy(args[0], env, hedges) if args else 0.0)
    if op == "and":
        if not args:
            return 1.0
        return min(evaluate_fuzzy(a, env, hedges) for a in args)
    if op == "or":
        if not args:
            return 0.0
        return max(evaluate_fuzzy(a, env, hedges) for a in args)
    if op == "if":
        if len(args) < 2:
            return 0.0
        # Fuzzy implication: max(1-a, b)
        a = evaluate_fuzzy(args[0], env, hedges)
        b = evaluate_fuzzy(args[1], env, hedges)
        return max(1.0 - a, b)
    if op == "because":
        if len(args) < 2:
            return evaluate_fuzzy(args[0], env, hedges) if args else 0.0
        return min(
            evaluate_fuzzy(args[0], env, hedges),
            evaluate_fuzzy(args[1], env, hedges),
        )
    if op in ("can", "maybe"):
        base = evaluate_fuzzy(args[0], env, hedges) if args else _env_float(env, op, 0.5)
        hid = op
        curve = (hedges.get(hid) or {}).get("curve")
        return apply_curve(curve, base) if curve else base * 0.7
    if op in ("before", "after", "when", "like"):
        if args:
            return evaluate_fuzzy(args[0], env, hedges)
        return _env_float(env, op, 0.0)
    if op == "hedge":
        hid = str(f.get("hedgeId") or "")
        child = evaluate_fuzzy(args[0], env, hedges) if args else _env_float(env, "x", 0.5)
        curve = (hedges.get(hid) or {}).get("curve")
        if not curve and env and isinstance(env.get("_curves"), dict):
            curve = env["_curves"].get(hid)
        return apply_curve(curve, child)
    return 0.0
