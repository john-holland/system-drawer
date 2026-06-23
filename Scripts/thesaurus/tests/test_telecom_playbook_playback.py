import sys
from pathlib import Path

import yaml

sys.path.insert(0, str(Path(__file__).resolve().parents[2]))

from telecom.sim.network_sim import load_playbook_into_sim

PLAYBOOK = Path(__file__).resolve().parents[3] / "telecom" / "playbooks" / "base" / "ubiquitous-net.playbook.yaml"


def test_playbook_playback():
    data = yaml.safe_load(PLAYBOOK.read_text(encoding="utf-8"))
    sim = load_playbook_into_sim(data)
    dev = sim.discover(device_id="terminal-lobby")
    assert dev is not None
    assert sim.route_call("ubiquitous", "terminal-lobby")
    assert sim.pam_allow("operator", "terminal-lobby", "call")


def test_discovery_cross_route():
    data = yaml.safe_load(PLAYBOOK.read_text(encoding="utf-8"))
    sim = load_playbook_into_sim(data)
    assert sim.route_call("other-net", "terminal-lobby")
