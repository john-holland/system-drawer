# Unity 6000.3.2f1 WebGL → nginx. License via UNITY_LICENSE (never commit).
# Context: Drawer 2 repo root.
#   docker compose -f docker/webgl/docker-compose.webgl.yml build
ARG UNITY_VERSION=6000.3.2f1
FROM unityci/editor:ubuntu-${UNITY_VERSION}-webgl-3 AS builder

WORKDIR /project
COPY . /project

ARG UNITY_LICENSE=
ARG UNITY_EMAIL=
ARG UNITY_PASSWORD=
ENV UNITY_LICENSE=${UNITY_LICENSE}
ENV UNITY_EMAIL=${UNITY_EMAIL}
ENV UNITY_PASSWORD=${UNITY_PASSWORD}

RUN mkdir -p /root/.local/share/unity3d/Unity /out \
    && if [ -z "$UNITY_LICENSE" ] && [ -z "$UNITY_PASSWORD" ]; then \
         echo "UNITY_LICENSE or UNITY_EMAIL/UNITY_PASSWORD required for WebGL build." >&2; \
         exit 1; \
       fi \
    && if [ -n "$UNITY_LICENSE" ]; then \
         printf '%s\n' "$UNITY_LICENSE" > /root/.local/share/unity3d/Unity/Unity_lic.ulf; \
       fi \
    && /opt/unity/Editor/Unity -batchmode -nographics -quit \
         -projectPath /project \
         -executeMethod BuildContinuuuumWebGL.BuildFromCli \
         -continuuuumWebGlOut /out \
         -logFile /dev/stdout

FROM nginx:alpine
COPY docker/webgl/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=builder /out /usr/share/nginx/html/library/continuuuum_editor_webgl
EXPOSE 80
