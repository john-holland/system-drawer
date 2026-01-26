# Third-Party Assets Manifest

This project uses a "light package" mode where third-party assets are excluded from git. These assets are free and available from the Unity Asset Store. Developers need to import them separately.

## Required Third-Party Assets

### Character Models & Animations
- **unity-chan!** - Unity-chan character model with animations
  - Asset Store: Search "unity-chan"
  - License: Unity-chan License 2.0
  - Location: `Assets/unity-chan!/`
  
- **CityPeople_Free** - Free city people character pack
  - Asset Store: Search "CityPeople Free"
  - Location: `Assets/CityPeople_Free/`
  
- **Kiki** - Character model
  - Location: `Assets/Kiki/`
  
- **DevilWoman** - Character model
  - Location: `Assets/DevilWoman/`
  
- **Tiger** - Character model
  - Location: `Assets/Tiger/`
  
- **UrsaAnimation** - Animation pack
  - Location: `Assets/UrsaAnimation/`

### Environment Assets
- **Enviroment** - Environment assets
  - Location: `Assets/Enviroment/`
  
- **Mars Landscape 3D** - Terrain assets
  - Location: `Assets/Mars Landscape 3D/`
  
- **Mountain Terrain rocks and tree** - Terrain assets
  - Location: `Assets/Mountain Terrain rocks and tree/`
  
- **Terrain Assets** - Terrain assets
  - Location: `Assets/Terrain Assets/`
  
- **Sci-Fi Styled Modular Pack** - Modular sci-fi assets
  - Location: `Assets/Sci-Fi Styled Modular Pack/`
  
- **WhiteCity** - City environment assets
  - Location: `Assets/WhiteCity/`
  
- **Studio Horizon** - Environment assets
  - Location: `Assets/Studio Horizon/`

### Effects & Utilities
- **Pixelation** - Pixelation effect
  - Location: `Assets/Pixelation/`
  
- **LargeBitmaskSystem** - Bitmask system utility
  - Location: `Assets/LargeBitmaskSystem/`

## Import Instructions

### Option 1: Manual Import (Recommended)
1. Open Unity Asset Store (Window > Asset Store)
2. Search for each asset by name
3. Click "Download" then "Import" for each asset
4. Ensure assets are imported to the correct locations as listed above

### Option 2: Unity Package Import
If you have `.unitypackage` files for these assets:
1. Assets > Import Package > Custom Package
2. Select the `.unitypackage` file
3. Import all assets

### Option 3: Asset Store Package Manager
1. Window > Package Manager
2. Click "My Assets" tab
3. Find purchased/downloaded assets
4. Click "Download" then "Import"

## Validation

After importing, verify the following prefabs/scenes still work:
- `Assets/unitychan_cardsolver.prefab` (requires unity-chan!)
- Any scenes in `Assets/Scenes/` that reference these assets

## Notes

- These assets are excluded from git via `.gitignore`
- Only custom code and project structure are tracked
- Asset Store assets maintain their own licensing
- If an asset is no longer available, check the Asset Store for alternatives
