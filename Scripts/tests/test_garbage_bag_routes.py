"""EditMode-style API coverage for garbage-bag create+ default slot."""

from garbage_bag_routes import RANDOM_BAG_ID, _BAGS, _ensure_default_bag, register_garbage_bag_routes


def test_default_random_bag_always_available():
    _BAGS.clear()
    bag = _ensure_default_bag()
    assert bag["id"] == RANDOM_BAG_ID
    assert bag["isDefault"] is True


def test_create_and_list_bags(client_app=None):
    from flask import Flask

    app = Flask(__name__)
    register_garbage_bag_routes(app)
    _BAGS.clear()
    _ensure_default_bag()
    client = app.test_client()
    r = client.get("/api/garbage-bags")
    assert r.status_code == 200
    data = r.get_json()
    assert data["defaultBagId"] == RANDOM_BAG_ID
    assert any(b["id"] == RANDOM_BAG_ID for b in data["bags"])

    created = client.post(
        "/api/garbage-bags",
        json={"title": "Organic Mix", "commodities": [{"key": "organic", "weight": 1.0}]},
    )
    assert created.status_code == 201
    body = created.get_json()
    assert body["title"] == "Organic Mix"
    assert body["id"] != RANDOM_BAG_ID

    deny = client.delete(f"/api/garbage-bags/{RANDOM_BAG_ID}")
    assert deny.status_code == 400
