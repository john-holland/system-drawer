# Street Blocks Designer

Menu **Locomotion → Street Blocks Designer** (`StreetBlocksPlanAsset`).

## Layers

| Layer | Depth |
|-------|-------|
| Deep subsurface | -10..-50 m |
| Shallow utility | 0..-3/-5 m |
| Street level | 0 |
| Podium | ~15–20 m |
| Mid/high-rise | up to ~150 m |
| Skyscraper | 150–300+ |
| Airspace | above |

## Tools

- Pixel select (mouse)
- Brushes: 2-way, multilane (3+), one-way (arrows), trash (zoom warning), phone poles (zoom warning + SO config)
- Unforgiving overlap: size desc; zoom to reveal; show/hide layers
- Auto-Connect Streets = Kruskal MST
- Seed Sewer Graph from building/sewer/dry-well cells

## HeightMapInteriorShaderBuffer

Prebakes descending mesh cutouts from heightmap; `OnMove` invalidates volume-intersect quads.

## FeatureBudget

`street_blocks` (`FeatureBudgetIds.StreetBlocks`).
