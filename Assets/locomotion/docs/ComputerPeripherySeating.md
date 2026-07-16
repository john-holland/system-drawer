# Computer Periphery Seating

Greenfield desk stack that **consumes** seated IK.

## Components

| Type | Role |
|------|------|
| `ComputerPeripheryStation` | Desk + chair + optional monitor/keyboard/mouse anchors |
| `PeripherySeatSlot` | `SitSurfaceContact` + approach waypoint + default `Sit` / `StandOn` |
| `PeripheryToolUseGate` | Opens while occupied (sit; optionally stand-on) |

## Flow

1. Path to `ApproachPosition`
2. `Occupy(actor, Sit|StandOn)` → tow chain + gate open
3. Tool-use cards / telecom webtop while `AllowsToolUse()`
4. Rotate / schooch as needed (`ChairRotateNode` / `ChairSchoochNode`)
5. `Vacate` → floor ambulation resumes

Default desk work occupancy is `Sit`. Set `allowToolUseWhileStandOn` if standing at the desk should still gate keyboard/mouse tools.
