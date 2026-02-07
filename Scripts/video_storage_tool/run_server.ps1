# Run video_storage_tool server. From repo root: .\Scripts\video_storage_tool\run_server.ps1
# Or from Scripts: .\video_storage_tool\run_server.ps1
$ScriptsDir = Split-Path $PSScriptRoot -Parent
if ((Split-Path $PSScriptRoot -Leaf) -eq "video_storage_tool") { Set-Location $ScriptsDir }
python -m video_storage_tool.server --host 127.0.0.1 --port 5000 @args
