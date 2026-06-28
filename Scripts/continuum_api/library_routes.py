"""Proxy USC library UI and API from serve_library (default :5051) onto continuum_api."""

from __future__ import annotations

import os
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path

from flask import Response, request, send_file

CONTINUUM_REPO = Path(__file__).resolve().parents[2] / "continuum"
if not CONTINUUM_REPO.is_dir():
    CONTINUUM_REPO = Path(os.environ.get("CONTINUUM_REPO", r"C:\Users\John\continuum"))
LIBRARY_HTML = CONTINUUM_REPO / "library" / "library.html"
LIBRARY_APP_BASE = os.environ.get("CONTINUUM_LIBRARY_BASE", "http://127.0.0.1:5051").rstrip("/")


def _proxy_request(method: str, url: str, body: bytes | None = None, headers: dict | None = None) -> tuple[int, bytes, list[tuple[str, str]]]:
    req = urllib.request.Request(url, data=body, method=method)
    skip = {"host", "content-length", "connection"}
    for k, v in (headers or {}).items():
        if k.lower() not in skip:
            req.add_header(k, v)
    try:
        with urllib.request.urlopen(req, timeout=30) as resp:
            return resp.status, resp.read(), list(resp.headers.items())
    except urllib.error.HTTPError as e:
        return e.code, e.read(), list(e.headers.items())
    except urllib.error.URLError as e:
        return 502, f'{{"error":"library backend unavailable at {LIBRARY_APP_BASE}","detail":"{e}"}}'.encode(), [
            ("Content-Type", "application/json")
        ]


def _flask_response(status: int, body: bytes, resp_headers: list[tuple[str, str]]) -> Response:
    excluded = {"content-encoding", "content-length", "transfer-encoding", "connection"}
    headers = {k: v for k, v in resp_headers if k.lower() not in excluded}
    return Response(body, status=status, headers=headers)


def register_library_routes(app) -> None:
    @app.route("/library")
    @app.route("/library/")
    def serve_library_spa():
        if LIBRARY_HTML.is_file():
            return send_file(str(LIBRARY_HTML))
        # fallback: proxy HTML from backend
        status, body, hdrs = _proxy_request("GET", f"{LIBRARY_APP_BASE}/library")
        return _flask_response(status, body, hdrs)

    @app.route("/api/library/<path:subpath>", methods=["GET", "POST", "PATCH", "PUT", "DELETE"])
    def proxy_library_api(subpath: str):
        qs = request.query_string.decode() if request.query_string else ""
        url = f"{LIBRARY_APP_BASE}/api/library/{subpath}"
        if qs:
            url = f"{url}?{qs}"
        status, body, hdrs = _proxy_request(request.method, url, request.get_data(), dict(request.headers))
        return _flask_response(status, body, hdrs)

    @app.route("/api/spatial/<path:subpath>", methods=["GET", "POST", "PATCH", "PUT", "DELETE"])
    def proxy_spatial_api(subpath: str):
        qs = request.query_string.decode() if request.query_string else ""
        url = f"{LIBRARY_APP_BASE}/api/spatial/{subpath}"
        if qs:
            url = f"{url}?{qs}"
        status, body, hdrs = _proxy_request(request.method, url, request.get_data(), dict(request.headers))
        return _flask_response(status, body, hdrs)

    @app.route("/continuum_editor/")
    @app.route("/continuum_editor/<path:subpath>")
    def proxy_continuum_editor(subpath: str | None = None):
        suffix = subpath or ""
        url = f"{LIBRARY_APP_BASE}/continuum_editor/{suffix}".rstrip("/") + "/"
        if request.query_string:
            url = f"{url}?{request.query_string.decode()}"
        status, body, hdrs = _proxy_request("GET", url)
        if status in (301, 302, 303, 307, 308):
            location = next((v for k, v in hdrs if k.lower() == "location"), None)
            if location and location.startswith(LIBRARY_APP_BASE):
                from flask import redirect

                return redirect(location.replace(LIBRARY_APP_BASE, "", 1) or "/library")
        return _flask_response(status, body, hdrs)
