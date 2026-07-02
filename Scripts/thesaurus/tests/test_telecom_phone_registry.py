import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[2]))

from telecom.phone_registry import format_e164, parse_display, parse_e164, earth_default_phone


def test_parse_display():
    p = parse_display("1-1-555-555-5555")
    assert p.galactic == 1
    assert p.exchange == 555


def test_e164_roundtrip():
    p = earth_default_phone(555, 5555555)
    assert parse_e164(format_e164(p)).display == p.display


def test_movie_phone():
    assert parse_display("1-1-555-555-5555").display == "1-1-555-555-5555"


def test_short_extension_phone():
    p = parse_display("1-1-555-0100")
    assert p.exchange == 555
    assert p.subscriber == 100
    assert format_e164(p) == "+G1.1.5550000100"
