# Cave Routing Guide

How to add or change a Continuum API surface using YAML-first Cave routing.

---

## Sources of truth (read these first)

| File | You edit this when… |
|------|---------------------|
| [`cave/cave.yaml`](../cave/cave.yaml) | Adding a new UI page / spelunk route |
| [`cave/cave.manifest.yaml`](../cave/cave.manifest.yaml) | Adding a structural route or message alias |
| [`cave/tomes/<surface>-tome.yaml`](../cave/tomes/) | Wiring a UI machine/event to a message |
| [`cave/cave-robit.yaml`](../cave/cave-robit.yaml) | Federating a tome to resaurce/saurce |

Inspect live config: `GET /api/config/overview` and `GET /api/routes`.

---

## Add a new route (checklist)

### 1. Implement the Python handler (if new behavior)

Add or extend a function in an existing `*_routes.py` module. Register the Flask route as today (internal implementation).

### 2. Register in `cave.manifest.yaml`

```yaml
messages:
  my_new_action: my-domain/action

handlers:
  my-domain/action:
    internal:
      method: POST
      path: /api/my-domain/resource
    mutating: true          # optional
    lvm_events:             # optional
      - MyDomainActionCommitted
      - TransitionCommitted
```

For resaurce-owned domains:

```yaml
handlers:
  production/budget/list:
    proxy: resaurce
```

For composite behavior, add a named handler under [`cave/handlers/`](../cave/handlers/):

```yaml
handlers:
  table-read/session/open:
    handler: table_read.session_open
```

### 3. Expose to a UI tome

In [`cave/tomes/my-surface-tome.yaml`](../cave/tomes/):

```yaml
id: my-surface-tome
machines:
  myMachine:
    events:
      DO_THING: my_new_action
robotCopy:
  flows:
    do_thing:
      message: my_new_action
```

### 4. Add spelunk entry (new page)

In [`cave/cave.yaml`](../cave/cave.yaml):

```yaml
spelunk:
  childCaves:
    my-surface:
      route: /my-surface
      container: main
      tomeId: my-surface-tome
```

### 5. Call from the browser

```javascript
ContinuumCaveShell.init({ tomeId: 'my-surface-tome' });
ContinuumCaveShell.caveMessage('my_new_action', { foo: 'bar' });
// or
ContinuumCaveShell.caveRoute('continuum:my-domain/action', { foo: 'bar' });
// or via RobotCopy
shell.robotCopy.sendMessage('my-surface-tome/myMachine', { event: 'DO_THING', data: { foo: 'bar' } });
```

### 6. Test

Add a case to [`Scripts/tests/test_cave_router.py`](../../tests/test_cave_router.py):

```python
r = client.post("/cave/route", json={
    "route": "continuum:my-domain/action",
    "payload": {},
    "trace_id": "test_1",
})
assert r.status_code == 200
```

---

## Envelope fields

| Field | Required | Description |
|-------|----------|-------------|
| `schema_version` | recommended | `"2.0"` |
| `route` | yes* | `continuum:stories/list` or `resaurce:chat/message/send` |
| `message` | yes* | Manifest alias, e.g. `list_stories` (alternative to `route`) |
| `payload` | no | Handler input object |
| `trace_id` | recommended | Correlates LVM2 audit events |
| `reply_mode` | no | `sync_http` (default) |

---

## Debugging

- **404 unknown_route** — structural path missing from `handlers` in manifest  
- **unknown_message** — message alias missing from `messages`  
- **502 upstream_unavailable** — resaurce Cave not running (`RESAURCE_CAVE_URL`)  
- **RobotCopy event not found** — machine `events` map missing in tome YAML  

---

## Python modules

| Module | Role |
|--------|------|
| [`cave/router.py`](../cave/router.py) | Entry: `handle_cave_route` |
| [`cave/dispatch_registry.py`](../cave/dispatch_registry.py) | Runs internal/proxy/handler specs |
| [`cave/resaurce_proxy.py`](../cave/resaurce_proxy.py) | Forwards `resaurce:*` / `saurce:*` |
| [`cave/lvm_hooks.py`](../cave/lvm_hooks.py) | Appends LVM2 events on mutations |
| [`cave/tome_dispatch.py`](../cave/tome_dispatch.py) | Maps tome machine events → routes |
| [`cave_routes.py`](../cave_routes.py) | Flask wiring: `/cave/route`, legacy shims |
