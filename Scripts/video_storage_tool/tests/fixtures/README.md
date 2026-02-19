# Test Fixtures

Fixture videos for integration tests are generated on first run using ffmpeg (no committed binaries).

## Default fixture (fixture_3s.mp4)

To generate the default fixture manually:

```bash
# From Scripts directory, with ffmpeg on PATH:
ffmpeg -y -f lavfi -i "color=c=blue:s=320x240:d=3" -f lavfi -i "sine=frequency=440:duration=3" -c:v libx264 -c:a aac -shortest video_storage_tool/tests/fixtures/fixture_3s.mp4
```

This creates a 3-second, 320x240 blue frame with 440Hz sine tone. The tests use this or generate it automatically via `conftest.py` if the file is missing.

## Custom media tests

To run tests with custom media (audio-heavy video, video-heavy video, image):

```bash
cd Scripts
python -m video_storage_tool.tests.run_custom_media_tests
```

Edit paths in `run_custom_media_tests.py` or `test_custom_media.py` to point to your files:
- royksopp-meets-chopin.mp4 (audio-heavy)
- Pepsi_Can_001.mp4 (video-heavy)
- Random image from Unsplash/Picsum (downloaded on first run, cached as `random_test_image.jpg`)

The random image is downloaded from Unsplash (or Picsum as fallback) and converted to `flight_sim_image_3s.mp4` on first run. Requires network access.
