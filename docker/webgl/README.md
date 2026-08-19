# Continuuuum WebGL (Docker)

Serves the Unity library editor used by webcam/dance preview.

```bash
# Full Unity build (needs UNITY_LICENSE or UNITY_EMAIL + UNITY_PASSWORD)
export UNITY_LICENSE="$(cat Unity_lic.ulf)"   # do not commit
docker compose -f docker/webgl/docker-compose.webgl.yml build
docker compose -f docker/webgl/docker-compose.webgl.yml up

# Already-built folder from scripts/build_continuuuum_webgl.*
export CONTINUUUUM_WEBGL_OUT="/path/to/continuuuum/library/continuuuum_editor_webgl"
docker compose -f docker/webgl/docker-compose.webgl.yml --profile prebuilt up webgl-prebuilt
```

Open `http://127.0.0.1:8088/continuuuum_editor/?apiBase=http://127.0.0.1:5050`.
