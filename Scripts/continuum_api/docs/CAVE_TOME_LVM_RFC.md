# RFC: Full Cave → Tome → LVM Adoption

**Status:** Accepted (supersedes tiered policy)  
**Review:** [CAVE_TOME_LVM_UI_REVIEW.md](./CAVE_TOME_LVM_UI_REVIEW.md)  
**How-to:** [CAVE_ROUTING_GUIDE.md](./CAVE_ROUTING_GUIDE.md)

---

## Decision

Adopt **full Cave → Tome → LVM** on Continuum Flask. Python domain handlers remain; **YAML + `POST /cave/route`** is the public browser boundary.

Every Continuum UI uses:

1. [`cave/cave.yaml`](../cave/cave.yaml) — spelunk UI route tree  
2. [`cave/cave.manifest.yaml`](../cave/cave.manifest.yaml) — structural routes, message aliases, handlers, LVM  
3. [`cave/tomes/*.yaml`](../cave/tomes/) — per-surface machines, events, RobotCopy flows  

Transport: **envelope v2** on same-origin `POST /cave/route`:

```json
{
  "schema_version": "2.0",
  "route": "continuum:stories/list",
  "payload": {},
  "trace_id": "<uuid>",
  "reply_mode": "sync_http"
}
```

Cross-service routes use explicit prefixes (`resaurce:production/budget/list`); Continuum proxies to `RESAURCE_CAVE_URL`.

---

## Architecture

```
Browser → POST /cave/route
       → cave/router.py (YAML dispatch)
       → existing *_routes.py handlers (internal) OR resaurce proxy
       → cave/lvm_hooks.py (mutations)
```

Legacy `/api/tomes/.../message` remains a thin shim mapping tome machine events → manifest routes.

Legacy direct `/api/*` routes remain for Unity, scripts, and migration; **browsers should not call them**.

---

## Client requirements

- Load [`continuum-cave-shell.js`](../static/shared/continuum-cave-shell/continuum-cave-shell.js) + [`continuum-user-session.js`](../static/shared/continuum-user-session/continuum-user-session.js)
- Use `ContinuumCaveShell.caveRoute(route, payload)` or `caveMessage('list_stories', payload)`
- RobotCopy resolves `(tome, machine, event)` via tome YAML → manifest messages → `/cave/route`

---

## Non-goals

- Rewriting Python handlers in Node
- Mandating client-side ViewStateMachine on every vanilla SPA (server-side LVM2 append is sufficient for v1)
- Removing `/api/*` until all edge clients migrate

---

## References

- [Routing guide](./CAVE_ROUTING_GUIDE.md)
- [log-view-machine ARCHITECTURE_AND_CAVE.md](D:/Development/log-view-machine/docs/ARCHITECTURE_AND_CAVE.md)
- [resaurce cave.manifest.yaml](D:/Development/resaurce/cave.manifest.yaml)
