"""Game/dimension catalog, visibility, associations, change-list reviews, SG prewarm, SPA."""

from __future__ import annotations

import sqlite3
from pathlib import Path
from typing import Any, Callable, Optional

from flask import g, jsonify, request, send_from_directory

try:
    from continuuuum_api import game_dimension_dao as dao
    from continuuuum_api.gd_route_annotations import accepts_game_dimension
    from continuuuum_api import sg_dimension_prewarm as prewarm
    from continuuuum_api.game_dimension_db import ensure_game_dimension_schema
except ImportError:
    import game_dimension_dao as dao  # type: ignore
    from gd_route_annotations import accepts_game_dimension  # type: ignore
    import sg_dimension_prewarm as prewarm  # type: ignore
    from game_dimension_db import ensure_game_dimension_schema  # type: ignore

GetConn = Callable[[], sqlite3.Connection]
IsAdmin = Callable[[], bool]


def _user_id() -> str:
    return (request.headers.get("X-User-ID") or "anonymous").strip() or "anonymous"


def register_gd_association_routes(
    app: Any, get_conn: GetConn, is_admin: IsAdmin | None = None
) -> None:
    admin_fn = is_admin or (
        lambda: request.headers.get("X-Admin", "").lower() in ("1", "true", "yes")
    )
    static_dir = Path(__file__).resolve().parent / "static" / "game-dimensions"

    def _conn() -> sqlite3.Connection:
        conn = get_conn()
        ensure_game_dimension_schema(conn)
        return conn

    # --- SPA ---
    @app.route("/game-dimensions")
    @app.route("/game-dimensions/")
    @app.route("/game-dimensions/<path:subpath>")
    def game_dimensions_spa(subpath: str = ""):
        if not static_dir.is_dir():
            return jsonify({"error": "game-dimensions SPA missing"}), 404
        # serve static assets if present
        if subpath and (static_dir / subpath).is_file():
            return send_from_directory(static_dir, subpath)
        return send_from_directory(static_dir, "index.html")

    # --- Catalog ---
    @app.route("/api/gd/games", methods=["GET"])
    def gd_list_games():
        """Visibility-filtered catalog (no context visibility gate — empty list if none granted)."""
        conn = _conn()
        return jsonify(dao.list_games_for_user(conn, _user_id(), admin_fn()))

    @app.route("/api/gd/games", methods=["POST"])
    def gd_create_game():
        if not admin_fn():
            return jsonify({"error": "admin required"}), 403
        body = request.get_json() or {}
        slug = (body.get("slug") or "").strip()
        name = (body.get("displayName") or body.get("display_name") or slug).strip()
        if not slug:
            return jsonify({"error": "slug required"}), 400
        conn = _conn()
        try:
            g_row = dao.create_game(conn, slug, name, bool(body.get("isPublic")))
        except sqlite3.IntegrityError as exc:
            return jsonify({"error": str(exc)}), 409
        return jsonify(g_row), 201

    @app.route("/api/gd/dimensions", methods=["GET"])
    def gd_list_dimensions():
        """Visibility-filtered catalog; dim 0 always listed for authenticated callers."""
        conn = _conn()
        return jsonify(dao.list_dimensions_for_user(conn, _user_id(), admin_fn()))

    @app.route("/api/gd/dimensions", methods=["POST"])
    def gd_create_dimension():
        if not admin_fn():
            return jsonify({"error": "admin required"}), 403
        body = request.get_json() or {}
        try:
            idx = int(body.get("dimIndex", body.get("dim_index")))
        except (TypeError, ValueError):
            return jsonify({"error": "dimIndex required"}), 400
        name = (body.get("displayName") or body.get("display_name") or f"Dimension {idx}").strip()
        conn = _conn()
        try:
            d = dao.create_dimension(
                conn,
                idx,
                name,
                slug=body.get("slug"),
                is_public=bool(body.get("isPublic")),
            )
        except sqlite3.IntegrityError as exc:
            return jsonify({"error": str(exc)}), 409
        return jsonify(d), 201

    # --- Visibility (admin) ---
    @app.route("/api/gd/visibility", methods=["GET"])
    def gd_get_visibility():
        if not admin_fn():
            return jsonify({"error": "admin required"}), 403
        conn = _conn()
        return jsonify(dao.list_visibility_matrix(conn))

    @app.route("/api/gd/visibility", methods=["PUT"])
    def gd_put_visibility():
        if not admin_fn():
            return jsonify({"error": "admin required"}), 403
        body = request.get_json() or {}
        kind = body.get("subjectKind") or body.get("subject_kind")
        sid = body.get("subjectId") or body.get("subject_id")
        if not kind or not sid:
            return jsonify({"error": "subjectKind and subjectId required"}), 400
        grants = body.get("grantUserIds")
        if grants is None:
            grants = body.get("grant_user_ids")
        is_public = body.get("isPublic")
        if is_public is None and "is_public" in body:
            is_public = body["is_public"]
        conn = _conn()
        try:
            matrix = dao.put_visibility(
                conn,
                kind,
                sid,
                is_public=is_public,
                grant_user_ids=grants,
                granted_by=_user_id(),
            )
        except ValueError as exc:
            return jsonify({"error": str(exc)}), 400
        return jsonify(matrix)

    # --- User context ---
    @app.route("/api/gd/user-context", methods=["GET"])
    @accepts_game_dimension
    def gd_get_user_context():
        """Query/headers: game, dimension (X-Game, X-Dimension)."""
        conn = _conn()
        return jsonify(dao.get_user_context(conn, _user_id()))

    @app.route("/api/gd/user-context", methods=["PATCH"])
    @accepts_game_dimension
    def gd_patch_user_context():
        """Query/headers: game, dimension (X-Game, X-Dimension)."""
        body = request.get_json() or {}
        conn = _conn()
        uid = _user_id()
        admin = admin_fn()
        game_id = None
        dim_id = None
        if "game" in body or "gameSlug" in body or "gameId" in body:
            ref = body.get("gameId") or body.get("game") or body.get("gameSlug")
            game = dao.resolve_game_ref(conn, ref)
            if not game:
                return jsonify({"error": "unknown game"}), 404
            err = dao.assert_game_visible(conn, game, uid, admin)
            if err:
                return jsonify({"error": err, "code": err}), 403
            game_id = game["id"]
        if "dimension" in body or "dimIndex" in body or "dimensionId" in body:
            ref = body.get("dimensionId")
            if ref is None:
                ref = body.get("dimension") if "dimension" in body else body.get("dimIndex")
            dim = dao.resolve_dimension_ref(conn, ref)
            if not dim:
                return jsonify({"error": "unknown dimension"}), 404
            err = dao.assert_dimension_visible(conn, dim, uid, admin)
            if err:
                return jsonify({"error": err, "code": err}), 403
            dim_id = dim["id"]
        ctx = dao.set_user_context(conn, uid, game_id=game_id, dimension_id=dim_id)
        return jsonify(ctx)

    # --- Dimension switch + prewarm ---
    @app.route("/api/gd/dimension-switch", methods=["POST"])
    @accepts_game_dimension
    def gd_dimension_switch():
        """Query/headers: game, dimension (X-Game, X-Dimension)."""
        body = request.get_json() or {}
        conn = _conn()
        uid = _user_id()
        admin = admin_fn()
        game_ref = body.get("game") or body.get("gameSlug") or getattr(g, "game_slug", "main")
        dim_ref = body.get("dimension")
        if dim_ref is None:
            dim_ref = body.get("dimIndex")
        if dim_ref is None:
            dim_ref = getattr(g, "dim_index", 0)
        game = dao.resolve_game_ref(conn, game_ref)
        dim = dao.resolve_dimension_ref(conn, dim_ref)
        if not game or not dim:
            return jsonify({"error": "unknown game or dimension"}), 404
        for err in (
            dao.assert_game_visible(conn, game, uid, admin),
            dao.assert_dimension_visible(conn, dim, uid, admin),
        ):
            if err:
                return jsonify({"error": err, "code": err}), 403
        ctx = dao.set_user_context(conn, uid, game_id=game["id"], dimension_id=dim["id"])
        kinds = body.get("kinds") or list(prewarm.SG_KINDS)
        existing = prewarm.get_prewarm(conn, game["slug"], dim["dimIndex"])
        if existing.get("missing"):
            built = prewarm.build_or_refresh(conn, game["slug"], dim["dimIndex"], kinds)
            snaps = built["snapshots"]
        else:
            snaps = existing["snapshots"]
            # fill any missing kinds
            miss = [k for k in kinds if k not in snaps]
            if miss:
                built = prewarm.build_or_refresh(conn, game["slug"], dim["dimIndex"], miss)
                snaps.update(built["snapshots"])
        return jsonify(
            {
                "userContext": ctx,
                "game": game,
                "dimension": dim,
                "snapshots": snaps,
            }
        )

    @app.route("/api/gd/sg-prewarm", methods=["GET"])
    @accepts_game_dimension
    def gd_get_sg_prewarm():
        """Query/headers: game, dimension (X-Game, X-Dimension)."""
        conn = _conn()
        game_ref = request.args.get("game") or getattr(g, "game_slug", "main")
        dim_ref = request.args.get("dimension")
        if dim_ref is None or dim_ref == "":
            dim_ref = getattr(g, "dim_index", 0)
        kind = request.args.get("kind")
        result = prewarm.get_prewarm(conn, game_ref, dim_ref, kind)
        if not result["snapshots"]:
            return jsonify(result), 404
        return jsonify(result)

    @app.route("/api/gd/sg-prewarm", methods=["POST"])
    @accepts_game_dimension
    def gd_post_sg_prewarm():
        """Query/headers: game, dimension (X-Game, X-Dimension)."""
        body = request.get_json() or {}
        conn = _conn()
        game_ref = body.get("game") or getattr(g, "game_slug", "main")
        dim_ref = body.get("dimension")
        if dim_ref is None:
            dim_ref = body.get("dimIndex", getattr(g, "dim_index", 0))
        kinds = body.get("kinds")
        built = prewarm.build_or_refresh(conn, game_ref, dim_ref, kinds)
        return jsonify(
            {
                "etags": built["etags"],
                "builtAt": built["builtAt"],
                "kinds": built["kinds"],
                "game": built["game"],
                "dimension": built["dimension"],
            }
        )

    @app.route("/api/gd/sg-prewarm/invalidate", methods=["POST"])
    def gd_invalidate_sg_prewarm():
        if not admin_fn():
            return jsonify({"error": "admin required"}), 403
        body = request.get_json() or {}
        conn = _conn()
        n = prewarm.invalidate(conn, body.get("game"), body.get("dimension"))
        return jsonify({"deleted": n})

    # --- Associable + associations ---
    @app.route("/api/gd/associable", methods=["GET"])
    def gd_associable():
        table = (request.args.get("table") or "").strip()
        if table not in dao.ASSOCIABLE_TABLES:
            return jsonify({"error": "unsupported table", "allowed": sorted(dao.ASSOCIABLE_TABLES)}), 400
        conn = _conn()
        ids: list[dict[str, Any]] = []
        if table == "thesaurus_entries":
            try:
                rows = conn.execute(
                    "SELECT id, term FROM thesaurus_entries ORDER BY term LIMIT 500"
                ).fetchall()
                ids = [{"id": r["id"], "label": r["term"]} for r in rows]
            except sqlite3.OperationalError:
                ids = []
        elif table == "localization_property_specs":
            try:
                rows = conn.execute(
                    "SELECT property_key FROM localization_property_specs ORDER BY property_key LIMIT 500"
                ).fetchall()
                ids = [{"id": r["property_key"], "label": r["property_key"]} for r in rows]
            except sqlite3.OperationalError:
                ids = []
        return jsonify({"table": table, "items": ids})

    @app.route("/api/gd/associations", methods=["GET"])
    @accepts_game_dimension
    def gd_get_associations():
        """Query/headers: game, dimension (X-Game, X-Dimension)."""
        conn = _conn()
        return jsonify(
            dao.list_associations(
                conn,
                table_name=request.args.get("table"),
                entity_id=request.args.get("entityId"),
            )
        )

    @app.route("/api/gd/associations", methods=["PUT"])
    @accepts_game_dimension
    def gd_put_associations():
        """Query/headers: game, dimension (X-Game, X-Dimension)."""
        body = request.get_json() or {}
        conn = _conn()
        matrix = body.get("matrix")
        if matrix is not None:
            n = dao.put_association_matrix(conn, matrix)
        else:
            rows = body.get("rows") or []
            n = dao.upsert_associations(conn, rows)
        # invalidate warm for current game/dim
        game = getattr(g, "game", None)
        dim = getattr(g, "dimension", None)
        if game and dim:
            prewarm.invalidate(conn, game["slug"], dim["dimIndex"])
        return jsonify({"upserted": n})

    # --- Change lists / reviews ---
    @app.route("/api/gd/change-lists", methods=["GET"])
    def gd_list_change_lists():
        conn = _conn()
        return jsonify(dao.list_change_lists(conn, status=request.args.get("status")))

    @app.route("/api/gd/change-lists", methods=["POST"])
    def gd_create_change_list():
        body = request.get_json() or {}
        conn = _conn()
        cl = dao.create_change_list(
            conn,
            _user_id(),
            body.get("title") or "Association changes",
            body.get("items") or [],
        )
        return jsonify(cl), 201

    @app.route("/api/gd/change-lists/<cl_id>", methods=["GET"])
    def gd_get_change_list(cl_id: str):
        conn = _conn()
        cl = dao.get_change_list(conn, cl_id)
        if not cl:
            return jsonify({"error": "not found"}), 404
        return jsonify(cl)

    @app.route("/api/gd/change-lists/<cl_id>/submit-for-review", methods=["POST"])
    def gd_submit_cl(cl_id: str):
        conn = _conn()
        cl = dao.set_change_list_status(conn, cl_id, "in_review")
        if not cl:
            return jsonify({"error": "not found"}), 404
        return jsonify(cl)

    @app.route("/api/gd/change-lists/<cl_id>/withdraw", methods=["POST"])
    def gd_withdraw_cl(cl_id: str):
        conn = _conn()
        cl = dao.set_change_list_status(conn, cl_id, "in_progress")
        if not cl:
            return jsonify({"error": "not found"}), 404
        return jsonify(cl)

    @app.route("/api/gd/change-lists/<cl_id>/reviewers", methods=["POST"])
    def gd_add_reviewer(cl_id: str):
        body = request.get_json() or {}
        rid = body.get("reviewerUserId") or body.get("userId")
        if not rid:
            return jsonify({"error": "reviewerUserId required"}), 400
        conn = _conn()
        return jsonify(dao.add_reviewer(conn, cl_id, rid))

    @app.route("/api/gd/change-lists/<cl_id>/reviewers/<reviewer_user_id>", methods=["PATCH"])
    def gd_patch_reviewer(cl_id: str, reviewer_user_id: str):
        body = request.get_json() or {}
        status = body.get("status")
        if status not in ("approved", "request_changes", "pending"):
            return jsonify({"error": "status must be approved|request_changes|pending"}), 400
        conn = _conn()
        cl = dao.patch_reviewer_status(conn, cl_id, reviewer_user_id, status)
        if not cl:
            return jsonify({"error": "not found"}), 404
        return jsonify(cl)

    @app.route("/api/gd/change-lists/<cl_id>/comments", methods=["GET", "POST"])
    def gd_comments(cl_id: str):
        conn = _conn()
        if request.method == "GET":
            cl = dao.get_change_list(conn, cl_id)
            if not cl:
                return jsonify({"error": "not found"}), 404
            return jsonify(cl.get("comments") or [])
        body = request.get_json() or {}
        body_text = (body.get("body") or "").strip()
        if not body_text:
            return jsonify({"error": "body required"}), 400
        return jsonify(
            dao.add_comment(conn, cl_id, _user_id(), body_text, item_id=body.get("itemId"))
        )

    @app.route("/api/gd/change-lists/<cl_id>/suggestions", methods=["GET", "POST"])
    def gd_suggestions(cl_id: str):
        conn = _conn()
        if request.method == "GET":
            cl = dao.get_change_list(conn, cl_id)
            if not cl:
                return jsonify({"error": "not found"}), 404
            return jsonify(cl.get("suggestions") or [])
        body = request.get_json() or {}
        return jsonify(dao.add_suggestion(conn, cl_id, _user_id(), body.get("payload") or body))

    @app.route("/api/gd/change-lists/<cl_id>/commit", methods=["POST"])
    def gd_commit_cl(cl_id: str):
        if not admin_fn():
            # allow if all reviewers approved
            conn = _conn()
            cl = dao.get_change_list(conn, cl_id)
            if not cl:
                return jsonify({"error": "not found"}), 404
            revs = cl.get("reviewers") or []
            if not revs or any(r.get("status") != "approved" for r in revs):
                return jsonify({"error": "all reviewers must approve or admin required"}), 403
        else:
            conn = _conn()
        cl = dao.commit_change_list(conn, cl_id)
        if not cl:
            return jsonify({"error": "not found"}), 404
        return jsonify(cl)
