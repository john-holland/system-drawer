# UCC Docker — sibling repos as API access

Containers **do not vendor** Modly GPU weights, Continuuuum/USC git trees, or Unity WebGL binaries. They reach those checkouts as **HTTP APIs** and **bind-mounts**.

## Run

From Drawer 2:

```text
docker compose -f docker/ucc/docker-compose.yml up --build
```

UCC: http://127.0.0.1:5050/image-to-model  
Webcam voxel-ragdoll: http://127.0.0.1:5050/webcam-animations

## Environment (HTTP APIs)

| Variable | Default | Role |
|----------|---------|------|
| `CONTINUUUUM_API_BASE` | `http://127.0.0.1:5050` | This UCC |
| `USC_API_BASE` | `http://host.docker.internal:5050` | Library `/api/library`, `/api/media` (often the same Flask process) |
| `MODLY_ROOT` | `/opt/modly` (mount) | Local Modly CLI; unset/empty dir → `{ available: false }` |
| `MINECRAFTUUUUM_API` | empty | Optional voxel ingest proxy (`http://host.docker.internal:5050` if Spring is on the host) |
| `CONTINUUUUM_REPO` | `/opt/continuuuum` | WebGL editor path + library.html |

`extra_hosts: host.docker.internal:host-gateway` so the container can call APIs on the host.

## Bind-mounts (read-only checkouts)

| Mount | Typical host path |
|-------|-------------------|
| `/app/Scripts` | Drawer 2 `Scripts/` |
| `/opt/continuuuum` | `C:\Users\John\continuuuum` (`CONTINUUUUM_HOST_REPO`) |
| `/opt/usc` | `C:\Users\John\unified-semantic-compressor` (`USC_HOST_REPO`) so `unified_semantic_archiver` is importable |
| `/opt/modly` | `MODLY_ROOT` on the host — **not** copied into the image |

Uploads persist in the `ucc-data` volume (`CONTINUUUUM_LIBRARY_UPLOADS=/data/library_uploads`).
