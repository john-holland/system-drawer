"""USC library UI and API on continuuuum_api (inline serve_library or optional proxy)."""





from __future__ import annotations





import os


import sys


import urllib.error


import urllib.request


from pathlib import Path





from flask import Response, redirect, request, send_file, send_from_directory





CONTINUUUUM_REPO = Path(__file__).resolve().parents[2] / "continuuuum"


if not CONTINUUUUM_REPO.is_dir():


    CONTINUUUUM_REPO = Path(os.environ.get("CONTINUUUUM_REPO", r"C:\Users\John\continuuuum"))


LIBRARY_HTML = CONTINUUUUM_REPO / "library" / "library.html"


WEBGL_EDITOR_INDEX = CONTINUUUUM_REPO / "library" / "continuuuum_editor_webgl" / "index.html"


LIBRARY_STATIC = CONTINUUUUM_REPO / "library"


_INLINE_LIBRARY_MOUNTED = False








def _continuuuum_db_path() -> str:


    scripts = Path(__file__).resolve().parents[1]


    drawer = Path(__file__).resolve().parents[2]


    for candidate in (


        os.environ.get("CONTINUUUUM_DB"),


        os.environ.get("CONTINUUUUM_DB_PATH"),


        str(drawer / "continuuuum.db"),


        str(scripts / "continuuuum.db"),


    ):


        if candidate:


            return candidate


    return str(scripts / "continuuuum.db")








def _library_proxy_base() -> str | None:


    """External library server URL, or None to use inline handlers."""


    env = os.environ.get("CONTINUUUUM_LIBRARY_BASE", "").strip()


    if env.lower() in ("", "same-origin", "inline", "self"):


        return None


    return env.rstrip("/")








def _proxy_request(


    method: str, url: str, body: bytes | None = None, headers: dict | None = None


) -> tuple[int, bytes, list[tuple[str, str]]]:


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


        base = _library_proxy_base() or "inline"


        return 502, f'{{"error":"library backend unavailable at {base}","detail":"{e}"}}'.encode(), [


            ("Content-Type", "application/json")


        ]








def _flask_response(status: int, body: bytes, resp_headers: list[tuple[str, str]]) -> Response:


    excluded = {"content-encoding", "content-length", "transfer-encoding", "connection"}


    headers = {k: v for k, v in resp_headers if k.lower() not in excluded}


    return Response(body, status=status, headers=headers)








def _mount_inline_library_api(app) -> bool:


    """Register serve_library API routes on this Flask app (single-server dev)."""


    global _INLINE_LIBRARY_MOUNTED


    if _INLINE_LIBRARY_MOUNTED:


        return True


    if _library_proxy_base() is not None:


        return False


    if not CONTINUUUUM_REPO.is_dir():


        return False





    repo_path = str(CONTINUUUUM_REPO)


    if repo_path not in sys.path:


        sys.path.insert(0, repo_path)





    try:


        import serve_library as sl  # noqa: WPS433


        from spatial_routes import register_spatial_routes  # noqa: WPS433


    except ImportError:


        return False





    db_path = _continuuuum_db_path()


    os.environ.setdefault("CONTINUUUUM_DB_PATH", db_path)


    sl.DB_PATH = db_path


    sl._db = None  # noqa: SLF001


    sl.UPLOADS_DIR.mkdir(parents=True, exist_ok=True)





    register_spatial_routes(app, lambda: db_path)





    for rule in sl.app.url_map.iter_rules():


        if not rule.rule.startswith(("/api/library", "/api/geocode")):


            continue


        view_func = sl.app.view_functions.get(rule.endpoint)


        if view_func is None:


            continue


        endpoint = "inline_usc_" + rule.endpoint.replace(".", "_")


        if endpoint in app.view_functions:


            continue


        methods = sorted(m for m in rule.methods if m not in {"HEAD", "OPTIONS"})


        app.add_url_rule(rule.rule, endpoint=endpoint, view_func=view_func, methods=methods)





    _INLINE_LIBRARY_MOUNTED = True


    return True








def register_library_routes(app) -> None:


    use_inline = _mount_inline_library_api(app)


    proxy_base = _library_proxy_base() or "http://127.0.0.1:5051"





    @app.route("/library")


    @app.route("/library/")


    def serve_library_spa():


        if LIBRARY_HTML.is_file():


            return send_file(str(LIBRARY_HTML))


        status, body, hdrs = _proxy_request("GET", f"{proxy_base}/library")


        return _flask_response(status, body, hdrs)





    @app.route("/library/<path:asset>")


    def serve_library_assets(asset: str):


        if LIBRARY_STATIC.is_dir():


            target = (LIBRARY_STATIC / asset).resolve()


            try:


                target.relative_to(LIBRARY_STATIC.resolve())


            except ValueError:


                return Response("Not found", status=404)


            if target.is_file():


                return send_from_directory(LIBRARY_STATIC, asset)


        status, body, hdrs = _proxy_request("GET", f"{proxy_base}/library/{asset}")


        return _flask_response(status, body, hdrs)





    if not use_inline:





        @app.route("/api/library/<path:subpath>", methods=["GET", "POST", "PATCH", "PUT", "DELETE"])


        def proxy_library_api(subpath: str):


            qs = request.query_string.decode() if request.query_string else ""


            url = f"{proxy_base}/api/library/{subpath}"


            if qs:


                url = f"{url}?{qs}"


            status, body, hdrs = _proxy_request(request.method, url, request.get_data(), dict(request.headers))


            return _flask_response(status, body, hdrs)





        @app.route("/api/spatial/<path:subpath>", methods=["GET", "POST", "PATCH", "PUT", "DELETE"])


        def proxy_spatial_api(subpath: str):


            qs = request.query_string.decode() if request.query_string else ""


            url = f"{proxy_base}/api/spatial/{subpath}"


            if qs:


                url = f"{url}?{qs}"


            status, body, hdrs = _proxy_request(request.method, url, request.get_data(), dict(request.headers))


            return _flask_response(status, body, hdrs)





    @app.route("/continuuuum_editor/")


    @app.route("/continuuuum_editor/<path:subpath>")


    def serve_continuuuum_editor(subpath: str | None = None):


        if WEBGL_EDITOR_INDEX.is_file() and (subpath is None or subpath in ("", "index.html")):


            return redirect("/library/continuuuum_editor_webgl/index.html")


        if subpath and (LIBRARY_STATIC / subpath).is_file():


            return send_from_directory(LIBRARY_STATIC, subpath)


        if subpath is None or subpath == "":


            from urllib.parse import urlencode





            params = request.args.to_dict(flat=True)


            params["panel"] = "upload"


            return redirect("/library?" + urlencode(params))


        if use_inline:


            return Response("Not found", status=404)


        url = f"{proxy_base}/continuuuum_editor/{subpath}".rstrip("/") + "/"


        if request.query_string:


            url = f"{url}?{request.query_string.decode()}"


        status, body, hdrs = _proxy_request("GET", url)


        if status in (301, 302, 303, 307, 308):


            location = next((v for k, v in hdrs if k.lower() == "location"), None)


            if location and location.startswith(proxy_base):


                return redirect(location.replace(proxy_base, "", 1) or "/library")


        return _flask_response(status, body, hdrs)


