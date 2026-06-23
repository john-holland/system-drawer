"""Frame processor client — unified CRUD surface across backends."""

from __future__ import annotations

import json
import os
import urllib.error
import urllib.request
from typing import Any


class FrameProcessorClient:
    def __init__(self, backend: str | None = None, base_url: str | None = None):
        self.backend = backend or os.environ.get("TELECOM_FRAME_BACKEND", "flask")
        self.base_url = (base_url or os.environ.get("TELECOM_FRAME_BASE_URL", "")).rstrip("/")

    def status(self) -> dict[str, Any]:
        if self.backend == "flask":
            return {"backend": "flask", "healthy": True, "baseUrl": None}
        if self.backend == "proton_unity":
            return {"backend": "proton_unity", "healthy": False, "message": "stub — not provisioned"}
        if not self.base_url:
            return {"backend": self.backend, "healthy": False, "error": "missing base_url"}
        try:
            req = urllib.request.Request(f"{self.base_url}/health", method="GET")
            with urllib.request.urlopen(req, timeout=5) as resp:
                data = json.loads(resp.read().decode())
            return {"backend": self.backend, "healthy": True, **data}
        except (urllib.error.URLError, TimeoutError, json.JSONDecodeError) as e:
            return {"backend": self.backend, "healthy": False, "error": str(e)}

    def relay_frame(self, payload: dict[str, Any]) -> dict[str, Any]:
        if self.backend != "icp_mock" or not self.base_url:
            return {"accepted": False, "reason": "frame relay requires icp_mock backend"}
        req = urllib.request.Request(
            f"{self.base_url}/api/frames",
            data=json.dumps(payload).encode(),
            headers={"Content-Type": "application/json"},
            method="POST",
        )
        with urllib.request.urlopen(req, timeout=10) as resp:
            return json.loads(resp.read().decode())
