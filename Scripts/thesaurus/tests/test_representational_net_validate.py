import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[2]))

from telecom.representational_net.validate import validate_site

SITE = Path(__file__).resolve().parents[3] / "telecom" / "playbooks" / "resources" / "sites" / "corp-intranet"


def test_corp_intranet_validates():
    errors = validate_site(SITE, device_ids=["terminal-lobby"])
    assert errors == [], errors
