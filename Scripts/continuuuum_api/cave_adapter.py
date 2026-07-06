"""


Cave adapter for continuuuum API: library search, upload, geocode, audit forwarding.


Cave = library/search server (caveBaseUrl, default localhost:3000).


Config: CAVE_BASE_URL, CAVE_AUDIT_ENABLED.


"""





from __future__ import annotations





import os


import urllib.parse


import urllib.request


from typing import Any








def _base_url() -> str:


    return os.environ.get("CAVE_BASE_URL", "http://localhost:3000").rstrip("/")








def _tenant_header() -> str:


    return os.environ.get("CONTINUUUUM_TENANT", "default")








def search_library(


    query: str | None = None,


    lat: float | None = None,


    lon: float | None = None,


    distance_mi: float | int = -1,


    document_type: str | None = None,


    tenant: str | None = None,


) -> list[dict[str, Any]]:


    """Search library documents. Returns list of document dicts."""


    params = []


    if query:


        params.append(("q", query))


    if lat is not None:


        params.append(("lat", str(lat)))


    if lon is not None:


        params.append(("lon", str(lon)))


    params.append(("distance_mi", "infinite" if distance_mi < 0 else str(int(distance_mi))))


    if document_type:


        params.append(("document_type", document_type))


    url = _base_url() + "/api/library/search?" + urllib.parse.urlencode(params)


    req = urllib.request.Request(url, headers={"X-Tenant-ID": tenant or _tenant_header()})


    with urllib.request.urlopen(req, timeout=10) as resp:


        import json


        return json.loads(resp.read().decode())








def geocode(address: str) -> tuple[float, float] | None:


    """Geocode address. Returns (lat, lon) or None."""


    url = _base_url() + "/api/geocode?address=" + urllib.parse.quote(address)


    req = urllib.request.Request(url)


    with urllib.request.urlopen(req, timeout=10) as resp:


        import json


        data = json.loads(resp.read().decode())


        if isinstance(data, dict) and "lat" in data and "lon" in data:


            return (float(data["lat"]), float(data["lon"]))


        return None








def upload_document(file_path: str, metadata: dict[str, Any] | None = None, tenant: str | None = None) -> dict | None:


    """Upload document to Cave library. Returns response dict or None.


    Cave may expose POST /api/library/upload - structure depends on Cave API."""


    # Placeholder: Cave upload API shape varies; document for implementers


    return None








def forward_audit(entry: dict[str, Any]) -> bool:


    """Forward audit log entry to Cave. Returns True if sent."""


    if not os.environ.get("CAVE_AUDIT_ENABLED", "").lower() in ("1", "true", "yes"):


        return False


    url = _base_url() + "/api/audit"


    data = __import__("json").dumps(entry).encode()


    req = urllib.request.Request(url, data=data, method="POST", headers={"Content-Type": "application/json"})


    try:


        with urllib.request.urlopen(req, timeout=5) as resp:


            return resp.status == 200


    except Exception:


        return False








def get_tome_header() -> str:


    """Fetch Tome container header HTML from Cave. GET /api/tome/container?slot=header.


    Returns minimal stub if Cave has no Tome API."""


    try:


        url = _base_url() + "/api/tome/container?slot=header"


        req = urllib.request.Request(url)


        with urllib.request.urlopen(req, timeout=5) as resp:


            return resp.read().decode()


    except Exception:


        return '<header class="tome-header"><nav><a href="/">Continuuuum</a></nav></header>'








def get_tome_footer() -> str:


    """Fetch Tome container footer HTML from Cave. GET /api/tome/container?slot=footer.


    Returns minimal stub if Cave has no Tome API."""


    try:


        url = _base_url() + "/api/tome/container?slot=footer"


        req = urllib.request.Request(url)


        with urllib.request.urlopen(req, timeout=5) as resp:


            return resp.read().decode()


    except Exception:


        return '<footer class="tome-footer"><small>Continuuuum Episodic Content</small></footer>'








def get_config_overview() -> dict[str, Any]:


    """Fetch Cave config overview for Cave, Tome, LogViewMachine, RobotCopy, CaveRobit.


    GET /api/config/overview. Returns empty structure if Cave has no config API."""


    try:


        url = _base_url() + "/api/config/overview"


        req = urllib.request.Request(url)


        with urllib.request.urlopen(req, timeout=5) as resp:


            import json


            return json.loads(resp.read().decode())


    except Exception:


        return {


            "cave": {"base_url": _base_url()},


            "tome": {},


            "logViewMachine": {},


            "robotCopy": {},


            "caveRobit": {},


        }


