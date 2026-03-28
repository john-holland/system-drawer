"""
Stream cache for reconstituted video. LRU eviction with configurable budget (GB).
Used when stream_cache.enabled: first request runs reconstitute and stores in cache;
subsequent requests stream from cache.
"""

import json
from typing import Callable, TypeVar
import logging
import shutil
import threading
import time
from pathlib import Path

log = logging.getLogger("video_storage_tool.stream_cache")
T = TypeVar("T")

INDEX_FILENAME = "index.json"


def _cache_key(job_id: str, use_original: bool) -> str:
    return f"{job_id}_{'original' if use_original else 'resultant'}"


class StreamCache:
    """LRU cache for reconstituted MP4 files with size budget."""

    def __init__(self, cache_dir: Path, budget_bytes: int) -> None:
        self.cache_dir = Path(cache_dir)
        self.budget_bytes = max(0, budget_bytes)
        self._lock = threading.Lock()
        self._key_locks: dict[str, threading.Lock] = {}
        self._key_locks_lock = threading.Lock()
        self._index: dict[str, dict] = {}  # key -> {"path": str, "size": int, "last_access": float}
        self._total_size = 0
        self.cache_dir.mkdir(parents=True, exist_ok=True)
        self._load_index()

    def _key_lock(self, key: str) -> threading.Lock:
        with self._key_locks_lock:
            if key not in self._key_locks:
                self._key_locks[key] = threading.Lock()
            return self._key_locks[key]

    def _load_index(self) -> None:
        index_path = self.cache_dir / INDEX_FILENAME
        if not index_path.exists():
            self._index = {}
            self._total_size = 0
            return
        try:
            with open(index_path, "r", encoding="utf-8") as f:
                raw = json.load(f)
            self._index = {}
            self._total_size = 0
            for k, v in (raw or {}).items():
                if not isinstance(v, dict):
                    continue
                path_str = v.get("path")
                size = int(v.get("size", 0))
                last_access = float(v.get("last_access", 0))
                path = Path(path_str) if path_str else self.cache_dir / f"{k}.mp4"
                if not path.is_absolute():
                    path = self.cache_dir / path.name
                if path.exists():
                    self._index[k] = {"path": str(path), "size": size, "last_access": last_access}
                    self._total_size += size
                else:
                    log.debug("Pruning stale cache entry: %s", k)
        except Exception as e:
            log.warning("Could not load stream cache index: %s", e)
            self._index = {}
            self._total_size = 0

    def _save_index(self) -> None:
        index_path = self.cache_dir / INDEX_FILENAME
        try:
            with open(index_path, "w", encoding="utf-8") as f:
                json.dump(self._index, f, indent=2)
        except Exception as e:
            log.warning("Could not save stream cache index: %s", e)

    def _evict_until_under_budget(self, extra_bytes: int) -> None:
        """Evict LRU entries until total_size + extra_bytes <= budget_bytes."""
        target = self.budget_bytes - extra_bytes
        if self._total_size <= target:
            return
        entries = sorted(
            self._index.items(),
            key=lambda x: x[1]["last_access"],
        )
        for key, info in entries:
            if self._total_size <= target:
                break
            path = Path(info["path"])
            size = info["size"]
            try:
                if path.exists():
                    path.unlink()
                    log.info("Evicted stream cache: %s (%d bytes)", key, size)
                self._total_size -= size
                del self._index[key]
            except OSError as e:
                log.warning("Could not evict %s: %s", key, e)
        self._save_index()

    def get(self, job_id: str, use_original: bool) -> Path | None:
        """Return path if cached, else None. Updates last_access on hit."""
        key = _cache_key(job_id, use_original)
        with self._lock:
            if key not in self._index:
                return None
            info = self._index[key]
            path = Path(info["path"])
            if not path.exists():
                self._total_size -= info["size"]
                del self._index[key]
                self._save_index()
                return None
            info["last_access"] = time.time()
            self._save_index()
            return path

    def put(self, job_id: str, use_original: bool, source_path: Path) -> Path:
        """
        Copy source_path into cache, evict LRU if over budget. Returns cache path.
        """
        source_path = Path(source_path)
        if not source_path.exists():
            raise FileNotFoundError(f"Source not found: {source_path}")
        size = source_path.stat().st_size
        key = _cache_key(job_id, use_original)
        cache_path = self.cache_dir / f"{key}.mp4"

        with self._lock:
            self._evict_until_under_budget(size)
            try:
                shutil.copy2(source_path, cache_path)
            except OSError as e:
                raise RuntimeError(f"Failed to copy to cache: {e}") from e
            self._index[key] = {
                "path": str(cache_path),
                "size": size,
                "last_access": time.time(),
            }
            self._total_size += size
            self._save_index()
            log.info("Cached stream: %s (%d bytes)", key, size)
            return cache_path

    def with_key_lock(
        self, job_id: str, use_original: bool, fn: Callable[[], T]
    ) -> T:
        """Run fn under per-key lock. Use to serialize reconstitute for same (job_id, original)."""
        key = _cache_key(job_id, use_original)
        with self._key_lock(key):
            return fn()
