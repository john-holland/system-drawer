import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[2]))

from telecom.address_codec import decode_address, earth_default_prefix, encode_address, TerrestrialSuffix


def test_encode_decode_roundtrip():
    prefix = earth_default_prefix()
    suffix = TerrestrialSuffix(1, 2, 3, 0xABCD, 0x1234)
    addr = encode_address(prefix, suffix)
    back = decode_address(addr)
    assert back.galactic.value == prefix.value


def test_earth_prefix():
    assert earth_default_prefix().hex8 == "01000001"
