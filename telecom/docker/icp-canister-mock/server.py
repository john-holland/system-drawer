"""ICP canister mock — same CRUD subset as frame processor OpenAPI."""

from __future__ import annotations

import json
from http.server import BaseHTTPRequestHandler, HTTPServer


class Handler(BaseHTTPRequestHandler):
    frames: list[dict] = []

    def _json(self, code: int, body: dict) -> None:
        raw = json.dumps(body).encode()
        self.send_response(code)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(raw)))
        self.end_headers()
        self.wfile.write(raw)

    def do_GET(self) -> None:
        if self.path == "/health":
            self._json(200, {"healthy": True, "backend": "icp_mock"})
            return
        self._json(404, {"error": "not found"})

    def do_POST(self) -> None:
        if self.path == "/api/frames":
            length = int(self.headers.get("Content-Length", 0))
            body = json.loads(self.rfile.read(length) or b"{}")
            Handler.frames.append(body)
            self._json(201, {"accepted": True, "id": len(Handler.frames)})
            return
        self._json(404, {"error": "not found"})

    def log_message(self, fmt: str, *args) -> None:
        pass


if __name__ == "__main__":
    HTTPServer(("0.0.0.0", 4943), Handler).serve_forever()
