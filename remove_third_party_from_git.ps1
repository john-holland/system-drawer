# PowerShell script to remove third-party assets from git tracking
# This keeps the files locally but stops tracking them in git
# Run this after setting up .gitignore

Write-Host "Removing third-party assets from git tracking..." -ForegroundColor Yellow
Write-Host "Files will remain on disk but won't be tracked by git." -ForegroundColor Yellow
Write-Host ""

$assets = @(
    "Assets/CityPeople_Free",
    "Assets/DevilWoman",
    "Assets/Enviroment",
    "Assets/Kiki",
    "Assets/Mars Landscape 3D",
    "Assets/Mountain Terrain rocks and tree",
    "Assets/Pixelation",
    "Assets/Sci-Fi Styled Modular Pack",
    "Assets/Studio Horizon",
    "Assets/Terrain Assets",
    "Assets/Tiger",
    "Assets/unity-chan!",
    "Assets/UrsaAnimation",
    "Assets/WhiteCity",
    "Assets/LargeBitmaskSystem"
)

$removed = 0
$notFound = 0

foreach ($asset in $assets) {
    if (Test-Path $asset) {
        Write-Host "Removing: $asset" -ForegroundColor Cyan
        git rm -r --cached "$asset" 2>$null
        if ($LASTEXITCODE -eq 0) {
            $removed++
        } else {
            Write-Host "  (not tracked in git)" -ForegroundColor Gray
        }
    } else {
        Write-Host "Not found: $asset" -ForegroundColor Gray
        $notFound++
    }
}

Write-Host ""
Write-Host "Removed $removed assets from git tracking." -ForegroundColor Green
if ($notFound -gt 0) {
    Write-Host "$notFound assets were not found (may already be ignored)." -ForegroundColor Yellow
}
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Review changes: git status" -ForegroundColor White
Write-Host "2. Commit the removal: git commit -m 'Remove third-party assets from tracking (light package mode)'" -ForegroundColor White
Write-Host "3. Verify with: Tools > Validate Third-Party Assets in Unity" -ForegroundColor White
