# Continuuuum API (UCC) — Flask on 5050.
# Context: Drawer 2 repo root. Source is bind-mounted at runtime; this image only has Python deps.
FROM python:3.12-slim
WORKDIR /app
COPY Scripts/continuuuum_api/requirements.txt /tmp/requirements.txt
RUN pip install --no-cache-dir -r /tmp/requirements.txt
ENV PYTHONPATH=/app/Scripts
EXPOSE 5050
CMD ["python", "-m", "continuuuum_api.server", "--port", "5050"]
