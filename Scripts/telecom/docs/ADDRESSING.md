# Galactic addressing (Continuuuum Telecom v1)

## Telephone numbers

| Segment | Example | Notes |
|---------|---------|-------|
| Galactic code | `1` | Earth registry |
| Planetary area | `1` | Earth prefix |
| Exchange | `555` | Fiction-safe block |
| Subscriber | `555-5555` | Local extension |

**Display:** `1-1-555-555-5555`

**Storage (E.164-like):** `+G1.1.5555555555`

```python
from telecom.phone_registry import parse_phone, format_e164

phone = parse_phone("1-1-555-555-5555")
assert format_e164(phone) == "+G1.1.5555555555"
```

## Dual-layer IPv6 (128-bit)

### Galactic prefix (32-bit)

```
[dimensional:8 | galactic:8 | system:8 | planet:8]
```

Earth default: `0x01000001` (dimensional=0, galactic=1, system=0, planet=1).

### Terrestrial suffix (96-bit)

```
[global:8 | region:8 | country:8 | city_grid:16 | device:56]
```

Initial assignments use **USC geohash** + **SG4D bucket** (`Q*`, `O*`, `S{n}.O*`), not in-scene network topology.

### Discovery vs routing

- **Routing tables** prefer paths by prefix/metric.
- **Discovery** uses `_telecom._discovery` records indexed by `(network_id, device_id)`.
- Virtual networks set `discovery.crossRoute: true` so origin subnet does not block lookup.

```python
from telecom.geo_assign import auto_assign_ip
from telecom.address_codec import decode_address

ip = auto_assign_ip(geohash="9q8yy", causality_leaf_id="O2.1")
addr = decode_address(ip)
```
