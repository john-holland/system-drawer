# Port-Broker and Cave Adapter Integration

Integration notes for [port-broker](https://github.com/john-holland/port-broker) and the Cave adapter with the episodic content pipeline.

---

## Port-Broker

Port-broker distributes server ports for Docker containers and build configuration.

### Problem

Episodic tools use multiple services that can collide on ports:

- **Continuum API** – 5050 (Flask)
- **Script-output** – 5174 (Vite)
- **Cave** – 3000 (library/search)
- **Continuum Library** – 5050 (when run from continuum repo)

### Options

1. **Static ports + env override** – Use fixed ports for local dev; override with `PORT` env var per service. Document in README.
2. **Port-broker** – Services request ports from the broker at startup; broker returns an allocated port. Requires services to be broker-aware.
3. **Named pipes** – Use Unix sockets (e.g. `/tmp/continuum-api.sock`). No port collision; requires reverse proxy.

### Recommendation

- **Local dev:** Use static ports (5050, 5174, 3000) or `PORT` env override.
- **CI/Docker:** Use port-broker when running multiple services in containers. Add broker client to continuum API and script-output startup.
- **Port-broker validator** – Add to port-broker: validate and warn when `PORT` env conflicts with existing allocations; offer override via broker.

### Usage with port-broker

```bash
# Request a port from broker
curl -X POST http://localhost:PORT_BROKER/allocate -d '{"service":"continuum-api"}' 
# Returns {"port": 5050}

# Start continuum API with allocated port
PORT=$(curl -s -X POST ... | jq -r .port) python -m continuum_api.server --port $PORT
```

---

## Cave Adapter

Location: `Scripts/continuum_api/cave_adapter.py`

### Functions

- `search_library(params)` – Search Cave `/api/library/search` by query, lat/lon, distance, document type
- `geocode(address)` – Call Cave `/api/geocode`
- `upload_document(file_path, metadata)` – Placeholder; Cave upload API shape varies
- `forward_audit(entry)` – POST audit log entry to Cave when `CAVE_AUDIT_ENABLED=1`

### Config

- `CAVE_BASE_URL` – Cave server base URL (default `http://localhost:3000`)
- `CAVE_AUDIT_ENABLED` – Set to `1` to forward audit logs to Cave
- `CONTINUUM_TENANT` – Tenant for `X-Tenant-ID` header

### CaveDB / RobotCopy

If CaveDB is a separate database:

- **Sync:** Mirror `api_audit_log` from continuum.db to CaveDB for centralized storage
- **RobotCopy:** Use as sync/copy utility; document: "optional: sync continuum.db audit tables to CaveDB via RobotCopy"

---

## Quick reference

| Service        | Default port | Env override |
|----------------|--------------|--------------|
| Continuum API  | 5050         | `PORT` or `--port` |
| Script-output  | 5174         | Vite `server.port` |
| Cave           | 3000         | Cave config |
| Port-broker    | (configurable) | - |
