"""Restaurant domain DB ensure + helpers."""

from __future__ import annotations

import json
import sqlite3
from pathlib import Path
from typing import Any

SCHEMA_PATH = Path(__file__).resolve().parents[1] / "continuuuum_restaurant_schema.sql"

ORDER_STATUSES = ("queued", "prep", "plating", "served", "cancelled")
BIN_STRATEGIES = ("continuous_delivery", "batch_bin", "just_in_time")


def ensure_restaurant_tables(conn: sqlite3.Connection) -> None:
    sql = SCHEMA_PATH.read_text(encoding="utf-8")
    conn.executescript(sql)
    conn.commit()
    _seed_defaults(conn)


def _seed_defaults(conn: sqlite3.Connection) -> None:
    cur = conn.execute("SELECT COUNT(*) AS c FROM restaurants")
    row = cur.fetchone()
    count = int(row[0] if not isinstance(row, sqlite3.Row) else row["c"])
    if count > 0:
        return
    conn.execute(
        """INSERT INTO restaurants (city_id, building_stable_id, name, cuisine, open_state)
           VALUES (?, ?, ?, ?, ?)""",
        ("demo-city", "bldg-restaurant-1", "Demo Kitchen", "american", "closed"),
    )
    rid = conn.execute("SELECT last_insert_rowid()").fetchone()[0]
    conn.execute(
        """INSERT INTO menu_items
           (restaurant_id, sku, name, category, description, price, available, ingredient_refs_json, chef_card_hints_json)
           VALUES (?, ?, ?, ?, ?, ?, 1, ?, ?)""",
        (
            rid,
            "burger-01",
            "House Burger",
            "entree",
            "Sim ticket demo item",
            12.0,
            json.dumps(["beef-patty", "bun"]),
            json.dumps(["sear", "place", "plating"]),
        ),
    )
    conn.execute(
        """INSERT INTO ingredients (restaurant_id, name, unit, on_hand, reorder_at, commodity_key)
           VALUES (?, ?, ?, ?, ?, ?)""",
        (rid, "beef-patty", "ea", 40, 10, "labor"),
    )
    conn.execute(
        """INSERT INTO retinue_members
           (restaurant_id, persona_key, role, pay_rate, pecking_order, duty_cron, waypoint_group)
           VALUES (?, ?, ?, ?, ?, ?, ?)""",
        (rid, "line-chef-a", "line-chef", 18.0, 40, "0 11-22 * * *", "kitchen-line"),
    )
    conn.execute(
        """INSERT INTO retinue_members
           (restaurant_id, persona_key, role, pay_rate, pecking_order, duty_cron, waypoint_group)
           VALUES (?, ?, ?, ?, ?, ?, ?)""",
        (rid, "head-chef", "head-chef", 28.0, 10, "0 10-23 * * *", "kitchen-pass"),
    )
    conn.execute(
        """INSERT INTO commodity_schedules
           (restaurant_id, commodity_key, cron_expr, surge_mult, quantity, price, availability)
           VALUES (?, ?, ?, ?, ?, ?, 1)""",
        (rid, "labor", "0 */6 * * *", 1.0, 10, 1.0),
    )
    conn.commit()


def row_to_dict(row: sqlite3.Row | None) -> dict[str, Any] | None:
    if row is None:
        return None
    return {k: row[k] for k in row.keys()}


def list_restaurants(conn: sqlite3.Connection) -> list[dict[str, Any]]:
    cur = conn.execute("SELECT * FROM restaurants ORDER BY id")
    return [row_to_dict(r) for r in cur.fetchall()]


def get_restaurant(conn: sqlite3.Connection, restaurant_id: int) -> dict[str, Any] | None:
    cur = conn.execute("SELECT * FROM restaurants WHERE id = ?", (restaurant_id,))
    return row_to_dict(cur.fetchone())


def get_menu(conn: sqlite3.Connection, restaurant_id: int) -> list[dict[str, Any]]:
    cur = conn.execute(
        "SELECT * FROM menu_items WHERE restaurant_id = ? ORDER BY sort_order, id",
        (restaurant_id,),
    )
    items = []
    for r in cur.fetchall():
        item = row_to_dict(r)
        mods = conn.execute(
            "SELECT * FROM menu_modifiers WHERE menu_item_id = ?",
            (item["id"],),
        ).fetchall()
        item["modifiers"] = [row_to_dict(m) for m in mods]
        items.append(item)
    return items


def replace_menu(conn: sqlite3.Connection, restaurant_id: int, items: list[dict[str, Any]]) -> list[dict[str, Any]]:
    conn.execute("DELETE FROM menu_modifiers WHERE menu_item_id IN (SELECT id FROM menu_items WHERE restaurant_id = ?)", (restaurant_id,))
    conn.execute("DELETE FROM menu_items WHERE restaurant_id = ?", (restaurant_id,))
    for i, it in enumerate(items or []):
        conn.execute(
            """INSERT INTO menu_items
               (restaurant_id, sku, name, category, description, price, available, ingredient_refs_json, chef_card_hints_json, sort_order)
               VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)""",
            (
                restaurant_id,
                it.get("sku") or f"sku-{i}",
                it.get("name") or "Item",
                it.get("category") or "entree",
                it.get("description") or "",
                float(it.get("price") or 0),
                1 if it.get("available", True) else 0,
                json.dumps(it.get("ingredientRefs") or it.get("ingredient_refs") or []),
                json.dumps(it.get("chefCardHints") or it.get("chef_card_hints") or []),
                int(it.get("sortOrder") or it.get("sort_order") or i),
            ),
        )
        mid = conn.execute("SELECT last_insert_rowid()").fetchone()[0]
        for mod in it.get("modifiers") or []:
            conn.execute(
                """INSERT INTO menu_modifiers (menu_item_id, name, price_delta, available)
                   VALUES (?, ?, ?, ?)""",
                (
                    mid,
                    mod.get("name") or "mod",
                    float(mod.get("priceDelta") or mod.get("price_delta") or 0),
                    1 if mod.get("available", True) else 0,
                ),
            )
    conn.commit()
    return get_menu(conn, restaurant_id)


def create_order(conn: sqlite3.Connection, restaurant_id: int, payload: dict[str, Any]) -> dict[str, Any]:
    label = payload.get("ticketLabel") or payload.get("ticket_label") or ""
    notes = payload.get("notes") or ""
    conn.execute(
        """INSERT INTO orders (restaurant_id, status, ticket_label, notes)
           VALUES (?, 'queued', ?, ?)""",
        (restaurant_id, label, notes),
    )
    oid = conn.execute("SELECT last_insert_rowid()").fetchone()[0]
    for line in payload.get("lines") or []:
        conn.execute(
            """INSERT INTO order_lines (order_id, menu_item_id, name, qty, modifiers_json)
               VALUES (?, ?, ?, ?, ?)""",
            (
                oid,
                line.get("menuItemId") or line.get("menu_item_id"),
                line.get("name") or "line",
                float(line.get("qty") or 1),
                json.dumps(line.get("modifiers") or []),
            ),
        )
    conn.commit()
    return get_order(conn, restaurant_id, oid)


def get_order(conn: sqlite3.Connection, restaurant_id: int, order_id: int) -> dict[str, Any] | None:
    cur = conn.execute(
        "SELECT * FROM orders WHERE id = ? AND restaurant_id = ?",
        (order_id, restaurant_id),
    )
    order = row_to_dict(cur.fetchone())
    if not order:
        return None
    lines = conn.execute("SELECT * FROM order_lines WHERE order_id = ?", (order_id,)).fetchall()
    order["lines"] = [row_to_dict(l) for l in lines]
    return order


def list_orders(conn: sqlite3.Connection, restaurant_id: int) -> list[dict[str, Any]]:
    cur = conn.execute(
        "SELECT * FROM orders WHERE restaurant_id = ? ORDER BY id DESC",
        (restaurant_id,),
    )
    out = []
    for r in cur.fetchall():
        o = row_to_dict(r)
        lines = conn.execute("SELECT * FROM order_lines WHERE order_id = ?", (o["id"],)).fetchall()
        o["lines"] = [row_to_dict(l) for l in lines]
        out.append(o)
    return out


def patch_order_status(conn: sqlite3.Connection, restaurant_id: int, order_id: int, status: str) -> dict[str, Any] | None:
    return patch_order(conn, restaurant_id, order_id, {"status": status})


def patch_order(
    conn: sqlite3.Connection, restaurant_id: int, order_id: int, payload: dict[str, Any]
) -> dict[str, Any] | None:
    """Update order status and/or ticket fields (no payment fields)."""
    existing = get_order(conn, restaurant_id, order_id)
    if not existing:
        return None
    status = payload.get("status", existing.get("status"))
    if status not in ORDER_STATUSES:
        raise ValueError(f"invalid status: {status}")
    ticket = payload.get("ticketLabel")
    if ticket is None:
        ticket = payload.get("ticket_label", existing.get("ticket_label") or "")
    notes = payload.get("notes")
    if notes is None:
        notes = existing.get("notes") or ""
    conn.execute(
        """UPDATE orders SET status = ?, ticket_label = ?, notes = ?, updated_at = datetime('now')
           WHERE id = ? AND restaurant_id = ?""",
        (status, ticket, notes, order_id, restaurant_id),
    )
    conn.commit()
    return get_order(conn, restaurant_id, order_id)


def list_retinue(conn: sqlite3.Connection, restaurant_id: int) -> list[dict[str, Any]]:
    cur = conn.execute(
        "SELECT * FROM retinue_members WHERE restaurant_id = ? ORDER BY pecking_order, id",
        (restaurant_id,),
    )
    return [row_to_dict(r) for r in cur.fetchall()]


def upsert_retinue(conn: sqlite3.Connection, restaurant_id: int, members: list[dict[str, Any]]) -> list[dict[str, Any]]:
    conn.execute("DELETE FROM retinue_members WHERE restaurant_id = ?", (restaurant_id,))
    for m in members or []:
        conn.execute(
            """INSERT INTO retinue_members
               (restaurant_id, persona_key, role, pay_rate, pecking_order, duty_cron, shift_window_json, waypoint_group)
               VALUES (?, ?, ?, ?, ?, ?, ?, ?)""",
            (
                restaurant_id,
                m.get("personaKey") or m.get("persona_key") or "staff",
                m.get("role") or "line-chef",
                float(m.get("payRate") or m.get("pay_rate") or 0),
                int(m.get("peckingOrder") or m.get("pecking_order") or 100),
                m.get("dutyCron") or m.get("duty_cron"),
                json.dumps(m.get("shiftWindow") or m.get("shift_window") or {}),
                m.get("waypointGroup") or m.get("waypoint_group") or "",
            ),
        )
    conn.commit()
    return list_retinue(conn, restaurant_id)


def list_commodity_schedules(conn: sqlite3.Connection, restaurant_id: int) -> list[dict[str, Any]]:
    cur = conn.execute(
        "SELECT * FROM commodity_schedules WHERE restaurant_id = ? ORDER BY id",
        (restaurant_id,),
    )
    return [row_to_dict(r) for r in cur.fetchall()]


def replace_commodity_schedules(
    conn: sqlite3.Connection, restaurant_id: int, schedules: list[dict[str, Any]]
) -> list[dict[str, Any]]:
    conn.execute("DELETE FROM commodity_schedules WHERE restaurant_id = ?", (restaurant_id,))
    for s in schedules or []:
        conn.execute(
            """INSERT INTO commodity_schedules
               (restaurant_id, commodity_key, cron_expr, one_shot_at, surge_mult, quantity, price, availability)
               VALUES (?, ?, ?, ?, ?, ?, ?, ?)""",
            (
                restaurant_id,
                s.get("commodityKey") or s.get("commodity_key") or "labor",
                s.get("cronExpr") or s.get("cron_expr"),
                s.get("oneShotAt") or s.get("one_shot_at"),
                float(s.get("surgeMult") or s.get("surge_mult") or 1),
                float(s.get("quantity") or 1),
                float(s.get("price") or 0),
                1 if s.get("availability", True) else 0,
            ),
        )
    conn.commit()
    return list_commodity_schedules(conn, restaurant_id)


def list_ingredients(conn: sqlite3.Connection, restaurant_id: int) -> list[dict[str, Any]]:
    cur = conn.execute(
        "SELECT * FROM ingredients WHERE restaurant_id = ? ORDER BY id",
        (restaurant_id,),
    )
    return [row_to_dict(r) for r in cur.fetchall()]


def list_supply_links(conn: sqlite3.Connection, restaurant_id: int) -> list[dict[str, Any]]:
    cur = conn.execute(
        "SELECT * FROM supply_links WHERE restaurant_id = ? ORDER BY id",
        (restaurant_id,),
    )
    return [row_to_dict(r) for r in cur.fetchall()]


MANAGER_ROLE_TOKENS = (
    "owner",
    "gm",
    "manager",
    "sous",
    "expo",
    "chef de cuisine",
    "executive",
)


def is_manager_role(role: str | None) -> bool:
    r = (role or "").lower()
    return any(tok in r for tok in MANAGER_ROLE_TOKENS)


def _member_pecking(m: dict[str, Any]) -> int:
    return int(m.get("pecking_order") or m.get("peckingOrder") or 100)


def _member_pay(m: dict[str, Any]) -> float:
    try:
        return float(m.get("pay_rate") or m.get("payRate") or 0)
    except (TypeError, ValueError):
        return 0.0


def _member_key(m: dict[str, Any]) -> str:
    return str(m.get("persona_key") or m.get("personaKey") or m.get("id") or "staff")


def _member_role(m: dict[str, Any]) -> str:
    return str(m.get("role") or "")


def _member_group(m: dict[str, Any]) -> str:
    return str(m.get("waypoint_group") or m.get("waypointGroup") or "")


def _tile_value(m: dict[str, Any]) -> float:
    pecking = _member_pecking(m)
    return max(1.0, _member_pay(m)) + max(1.0, 200.0 - pecking)


def _leaf_from_member(m: dict[str, Any], kind: str = "staff") -> dict[str, Any]:
    return {
        "name": _member_key(m),
        "kind": kind,
        "role": _member_role(m),
        "pecking_order": _member_pecking(m),
        "waypoint_group": _member_group(m),
        "value": _tile_value(m),
    }


def build_retinue_treemap(members: list[dict[str, Any]] | None) -> dict[str, Any]:
    """
    Derive a d3 hierarchy from pecking_order + roles (no parent IDs).
    Managers = role tokens and/or lowest pecking per waypoint_group.
    Staff attach under same-group manager with nearest lower-or-equal pecking.
    """
    people = [m for m in (members or []) if m]
    if not people:
        return {"name": "retinue", "kind": "root", "children": []}

    role_managers: list[dict[str, Any]] = [m for m in people if is_manager_role(_member_role(m))]
    role_mgr_ids = {id(m) for m in role_managers}

    by_group: dict[str, list[dict[str, Any]]] = {}
    for m in people:
        by_group.setdefault(_member_group(m), []).append(m)

    managers: list[dict[str, Any]] = list(role_managers)
    manager_ids = set(role_mgr_ids)
    for _group, group_members in by_group.items():
        if any(id(m) in role_mgr_ids for m in group_members):
            continue
        # Lowest pecking_order in group becomes display manager
        best = min(group_members, key=lambda x: (_member_pecking(x), _member_key(x)))
        if id(best) not in manager_ids:
            managers.append(best)
            manager_ids.add(id(best))

    managers.sort(key=lambda m: (_member_pecking(m), _member_key(m)))

    # persona_key -> manager node skeleton
    manager_nodes: dict[int, dict[str, Any]] = {}
    for m in managers:
        node = {
            "name": _member_key(m),
            "kind": "manager",
            "role": _member_role(m),
            "pecking_order": _member_pecking(m),
            "waypoint_group": _member_group(m),
            "children": [_leaf_from_member(m, kind="manager-self")],
        }
        manager_nodes[id(m)] = node

    unassigned_children: list[dict[str, Any]] = []

    for m in people:
        if id(m) in manager_ids:
            continue
        group = _member_group(m)
        staff_p = _member_pecking(m)
        candidates = [
            mgr
            for mgr in managers
            if _member_group(mgr) == group and _member_pecking(mgr) <= staff_p
        ]
        if not candidates:
            unassigned_children.append(_leaf_from_member(m, kind="staff"))
            continue
        # Nearest authority: closest pecking from below (max pecking among <= staff)
        boss = max(candidates, key=lambda mgr: (_member_pecking(mgr), _member_key(mgr)))
        manager_nodes[id(boss)]["children"].append(_leaf_from_member(m, kind="staff"))

    children: list[dict[str, Any]] = [manager_nodes[id(m)] for m in managers]
    if unassigned_children:
        children.append(
            {
                "name": "Unassigned",
                "kind": "unassigned",
                "children": unassigned_children,
            }
        )
    return {"name": "retinue", "kind": "root", "children": children}


def chef_card_attachment_graph(conn: sqlite3.Connection, restaurant_id: int) -> dict[str, Any]:
    """d3-friendly nodes/links: menu items ↔ chef activity hints ↔ retinue roles."""
    menu = get_menu(conn, restaurant_id)
    retinue = list_retinue(conn, restaurant_id)
    nodes = []
    links = []
    for m in menu:
        nid = f"menu-{m['id']}"
        nodes.append({"id": nid, "label": m["name"], "kind": "menu"})
        hints = []
        raw = m.get("chef_card_hints_json") or "[]"
        try:
            hints = json.loads(raw) if isinstance(raw, str) else (raw or [])
        except json.JSONDecodeError:
            hints = []
        for h in hints:
            hid = f"activity-{h}"
            if not any(n["id"] == hid for n in nodes):
                nodes.append({"id": hid, "label": str(h), "kind": "chef_activity"})
            links.append({"source": nid, "target": hid})
    for r in retinue:
        rid = f"staff-{r['id']}"
        nodes.append({"id": rid, "label": r["persona_key"], "kind": "retinue", "role": r["role"]})
        for n in nodes:
            if n["kind"] == "chef_activity":
                links.append({"source": rid, "target": n["id"]})
    return {"nodes": nodes, "links": links}
