"""Galactic telephone registry — format and parse movie-style numbers."""

from __future__ import annotations

import re
from dataclasses import dataclass

# Earth registry defaults (Galactic Code Registry v1)
EARTH_GALACTIC_CODE = 1
EARTH_PLANETARY_AREA = 1

_DISPLAY_RE = re.compile(
    r"^(?P<galactic>\d+)-(?P<planetary>\d+)-(?P<exchange>\d{3})-(?P<subscriber>\d{3}-\d{4})$"
)
_DISPLAY_SHORT_RE = re.compile(
    r"^(?P<galactic>\d+)-(?P<planetary>\d+)-(?P<exchange>\d{3})-(?P<shortsub>\d{4})$"
)
_E164_RE = re.compile(
    r"^\+G(?P<galactic>\d+)\.(?P<planetary>\d+)\.(?P<subscriber>\d{10})$"
)


@dataclass(frozen=True)
class GalacticPhone:
    galactic: int
    planetary: int
    exchange: int
    subscriber: int

    @property
    def display(self) -> str:
        sub = f"{self.subscriber:07d}"
        return f"{self.galactic}-{self.planetary}-{self.exchange:03d}-{sub[:3]}-{sub[3:]}"

    @property
    def e164(self) -> str:
        sub = f"{self.exchange:03d}{self.subscriber:07d}"
        return f"+G{self.galactic}.{self.planetary}.{sub}"


def parse_display(value: str) -> GalacticPhone:
    v = value.strip()
    m = _DISPLAY_RE.match(v)
    if m:
        sub_raw = m.group("subscriber").replace("-", "")
        return GalacticPhone(
            galactic=int(m.group("galactic")),
            planetary=int(m.group("planetary")),
            exchange=int(m.group("exchange")),
            subscriber=int(sub_raw),
        )
    m_short = _DISPLAY_SHORT_RE.match(v)
    if m_short:
        # Shorthand extension (e.g. 1-1-555-0100) — pad to 7-digit subscriber.
        return GalacticPhone(
            galactic=int(m_short.group("galactic")),
            planetary=int(m_short.group("planetary")),
            exchange=int(m_short.group("exchange")),
            subscriber=int(m_short.group("shortsub")),
        )
    raise ValueError(f"invalid display phone: {value!r}")


def parse_e164(value: str) -> GalacticPhone:
    m = _E164_RE.match(value.strip())
    if not m:
        raise ValueError(f"invalid E.164 galactic phone: {value!r}")
    digits = m.group("subscriber")
    return GalacticPhone(
        galactic=int(m.group("galactic")),
        planetary=int(m.group("planetary")),
        exchange=int(digits[:3]),
        subscriber=int(digits[3:]),
    )


def parse_phone(value: str) -> GalacticPhone:
    v = value.strip()
    if v.startswith("+G"):
        return parse_e164(v)
    return parse_display(v)


def format_display(phone: GalacticPhone) -> str:
    return phone.display


def format_e164(phone: GalacticPhone) -> str:
    return phone.e164


def earth_default_phone(exchange: int, subscriber: int) -> GalacticPhone:
    return GalacticPhone(EARTH_GALACTIC_CODE, EARTH_PLANETARY_AREA, exchange, subscriber)
