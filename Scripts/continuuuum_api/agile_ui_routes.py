"""Serve story board, project calendar, legal tracker, and budget dashboard SPAs."""

from __future__ import annotations

from pathlib import Path

from flask import redirect, send_from_directory

STATIC_STORY = Path(__file__).resolve().parent / "static" / "story-board"
STATIC_CAL = Path(__file__).resolve().parent / "static" / "project-calendar"
STATIC_LEGAL = Path(__file__).resolve().parent / "static" / "legal-tracker"
STATIC_BUDGET = Path(__file__).resolve().parent / "static" / "budget-dashboard"
STATIC_SETTINGS = Path(__file__).resolve().parent / "static" / "settings"
STATIC_CREDITS = Path(__file__).resolve().parent / "static" / "credits"
STATIC_INVENTORY_LOADOUTS = Path(__file__).resolve().parent / "static" / "inventory-loadouts"
STATIC_GARBAGE_BAGS = Path(__file__).resolve().parent / "static" / "garbage-bags"


def register_agile_ui_routes(app) -> None:
    @app.route("/story-board")
    def story_board_redirect():
        return redirect("/story-board/", code=302)

    @app.route("/story-board/")
    def story_board():
        return send_from_directory(STATIC_STORY, "index.html")

    @app.route("/story-board/<path:asset>")
    def story_board_assets(asset: str):
        return send_from_directory(STATIC_STORY, asset)

    @app.route("/project-calendar")
    def project_calendar_redirect():
        return redirect("/project-calendar/", code=302)

    @app.route("/project-calendar/")
    def project_calendar():
        return send_from_directory(STATIC_CAL, "index.html")

    @app.route("/project-calendar/<path:asset>")
    def project_calendar_assets(asset: str):
        return send_from_directory(STATIC_CAL, asset)

    @app.route("/legal-tracker")
    def legal_tracker_redirect():
        return redirect("/legal-tracker/", code=302)

    @app.route("/legal-tracker/")
    def legal_tracker():
        return send_from_directory(STATIC_LEGAL, "index.html")

    @app.route("/legal-tracker/<path:asset>")
    def legal_tracker_assets(asset: str):
        return send_from_directory(STATIC_LEGAL, asset)

    @app.route("/budget-dashboard")
    def budget_dashboard_redirect():
        return redirect("/budget-dashboard/", code=302)

    @app.route("/budget-dashboard/")
    def budget_dashboard():
        return send_from_directory(STATIC_BUDGET, "index.html")

    @app.route("/budget-dashboard/<path:asset>")
    def budget_dashboard_assets(asset: str):
        return send_from_directory(STATIC_BUDGET, asset)

    @app.route("/settings")
    def settings_redirect():
        return redirect("/settings/", code=302)

    @app.route("/settings/")
    def settings_page():
        return send_from_directory(STATIC_SETTINGS, "index.html")

    @app.route("/settings/<path:asset>")
    def settings_assets(asset: str):
        return send_from_directory(STATIC_SETTINGS, asset)

    @app.route("/credits")
    def credits_redirect():
        return redirect("/credits/", code=302)

    @app.route("/credits/")
    def credits_page():
        return send_from_directory(STATIC_CREDITS, "index.html")

    @app.route("/credits/<path:asset>")
    def credits_assets(asset: str):
        return send_from_directory(STATIC_CREDITS, asset)

    @app.route("/inventory-loadouts")
    def inventory_loadouts_redirect():
        return redirect("/inventory-loadouts/", code=302)

    @app.route("/inventory-loadouts/")
    def inventory_loadouts_page():
        return send_from_directory(STATIC_INVENTORY_LOADOUTS, "index.html")

    @app.route("/inventory-loadouts/<path:asset>")
    def inventory_loadouts_assets(asset: str):
        return send_from_directory(STATIC_INVENTORY_LOADOUTS, asset)

    @app.route("/garbage-bags")
    def garbage_bags_redirect():
        return redirect("/garbage-bags/", code=302)

    @app.route("/garbage-bags/")
    def garbage_bags_page():
        return send_from_directory(STATIC_GARBAGE_BAGS, "index.html")

    @app.route("/garbage-bags/<path:asset>")
    def garbage_bags_assets(asset: str):
        return send_from_directory(STATIC_GARBAGE_BAGS, asset)
