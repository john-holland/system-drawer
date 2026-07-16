"""Integration tests for POST/GET /api/deeplink (Unity DeepLinkHandler contract)."""

from __future__ import annotations

import json
import sys
from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parents[1]
API = ROOT / "continuuuum_api"
sys.path.insert(0, str(API))
sys.path.insert(0, str(ROOT))

from continuuuum_api import server as srv  # noqa: E402

LEMMA_BUILD_WINDOW = "System Drawer/Lemmas/Lemma Build"
LEMMA_PROPERTIES_WINDOW = "Continuuuum/Lemma Properties"

SAMPLE_FORM = {
    "lemma": "unlock",
    "partOfSpeech": "verb",
    "posTag": "verb",
    "mechanicalRole": "AtomicAction",
    "outputTier": 1,
    "functionalDescription": "Opens a latch",
    "mechanismPrompt": "door latch",
    "synonyms": ["unbolt", "open"],
    "compositionChildren": [
        {
            "entryId": "urn:unity:continuuuum:builtin:v1:/en/noun/object",
            "sortOrder": 0,
        }
    ],
    "properties": [{"propertyKey": "causality-tree", "propertyValue": "open"}],
    "engine": "haxe",
}


@pytest.fixture
def deeplink_path(tmp_path, monkeypatch):
    path = tmp_path / "continuuuum-deeplink.json"
    monkeypatch.setenv("CONTINUUUUM_DEEPLINK_PATH", str(path))
    if path.exists():
        path.unlink()
    yield path
    if path.exists():
        path.unlink()


def test_post_lemma_build_deeplink_writes_form_envelope(deeplink_path):
    client = srv.app.test_client()
    r = client.post(
        "/api/deeplink",
        json={"window": LEMMA_BUILD_WINDOW, "form": SAMPLE_FORM},
    )
    assert r.status_code == 200, r.get_data(as_text=True)
    body = r.get_json()
    assert body["ok"] is True
    assert Path(body["path"]) == deeplink_path
    assert deeplink_path.is_file()

    payload = json.loads(deeplink_path.read_text(encoding="utf-8"))
    assert payload["window"] == LEMMA_BUILD_WINDOW
    assert payload["form"]["lemma"] == "unlock"
    assert payload["form"]["engine"] == "haxe"
    assert payload["form"]["synonyms"] == ["unbolt", "open"]
    assert payload["form"]["compositionChildren"][0]["entryId"].endswith("/object")
    assert "Lemma Build" in payload["window"]
    # Unity DeepLinkContract prefers "Lemma Build" over bare "Lemma".
    assert "Lemma Build" in payload["window"]


def test_get_lemma_properties_deeplink_writes_entry_id(deeplink_path):
    client = srv.app.test_client()
    entry_id = "urn:unity:continuuuum:builtin:v1:/en/verb/open"
    r = client.get(
        "/api/deeplink",
        query_string={"window": LEMMA_PROPERTIES_WINDOW, "entryId": entry_id},
    )
    assert r.status_code == 200
    payload = json.loads(deeplink_path.read_text(encoding="utf-8"))
    assert payload["window"] == LEMMA_PROPERTIES_WINDOW
    assert payload["entryId"] == entry_id
    assert "form" not in payload


def test_write_deeplink_file_helper_matches_web_client(deeplink_path):
    path = srv._write_deeplink_file(LEMMA_BUILD_WINDOW, form=SAMPLE_FORM)
    assert Path(path) == deeplink_path
    payload = json.loads(deeplink_path.read_text(encoding="utf-8"))
    assert payload == {"window": LEMMA_BUILD_WINDOW, "form": SAMPLE_FORM}
