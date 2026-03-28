"""Unit tests for StreamCache: get, put, LRU eviction."""
from pathlib import Path

import pytest

from video_storage_tool.stream_cache import StreamCache


def test_put_and_get(tmp_path: Path) -> None:
    """Put file in cache, get returns path."""
    cache_dir = tmp_path / "cache"
    cache = StreamCache(cache_dir, budget_bytes=100 * 1024 * 1024)
    src = tmp_path / "source.mp4"
    src.write_bytes(b"fake mp4 content")
    path = cache.put("job-1", use_original=False, source_path=src)
    assert path.is_file()
    assert path.read_bytes() == b"fake mp4 content"
    got = cache.get("job-1", use_original=False)
    assert got is not None
    assert got == path
    assert cache.get("job-1", use_original=True) is None


def test_get_miss_returns_none(tmp_path: Path) -> None:
    """Get for uncached key returns None."""
    cache = StreamCache(tmp_path / "cache", budget_bytes=10**9)
    assert cache.get("missing", use_original=False) is None


def test_eviction_when_over_budget(tmp_path: Path) -> None:
    """LRU eviction when adding would exceed budget."""
    cache_dir = tmp_path / "cache"
    # Budget 100 bytes; each file ~20 bytes
    cache = StreamCache(cache_dir, budget_bytes=100)
    for i in range(6):
        src = tmp_path / f"src{i}.mp4"
        src.write_bytes(b"x" * 20)
        cache.put(f"job-{i}", use_original=False, source_path=src)
    # Should have evicted oldest; total ~100 bytes, so ~5 files
    assert cache.get("job-0", use_original=False) is None
    assert cache.get("job-5", use_original=False) is not None
    total_files = sum(1 for p in cache_dir.glob("*.mp4") if p.name != "index.json")
    assert total_files <= 6


def test_lru_order(tmp_path: Path) -> None:
    """Eviction removes least recently accessed."""
    cache_dir = tmp_path / "cache"
    # Budget 100 bytes; 4 files of 25 = 100 (all fit). Access 0,1 to make them recent.
    cache = StreamCache(cache_dir, budget_bytes=100)
    for i in range(4):
        src = tmp_path / f"src{i}.mp4"
        src.write_bytes(b"x" * 25)
        cache.put(f"job-{i}", use_original=False, source_path=src)
    cache.get("job-0", use_original=False)
    cache.get("job-1", use_original=False)
    # Add 5th file: need to evict 25 bytes. job-2 and job-3 are oldest.
    src_new = tmp_path / "src_new.mp4"
    src_new.write_bytes(b"x" * 25)
    cache.put("job-new", use_original=False, source_path=src_new)
    assert cache.get("job-0", use_original=False) is not None
    assert cache.get("job-1", use_original=False) is not None
    assert cache.get("job-2", use_original=False) is None  # evicted (oldest, not recently accessed)
