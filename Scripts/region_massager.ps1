# Optional PowerShell wrapper for region_massager.py.
# Usage: .\Scripts\region_massager.ps1 -Path "C:\path\to\file" [-Passes 2] [-Force]

param(
    [Parameter(Mandatory = $true)]
    [string]$Path,
    [int]$Passes = 1,
    [switch]$Force
)

$scriptDir = $PSScriptRoot
if (-not $scriptDir) { $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path }
$pyScript = Join-Path $scriptDir "region_massager.py"

if (-not (Test-Path -LiteralPath $pyScript -PathType Leaf)) {
    Write-Error "region_massager.py not found at: $pyScript"
    exit 1
}

$python = $null
try {
    $python = (Get-Command python -ErrorAction Stop).Source
} catch {
    try {
        $python = (Get-Command python3 -ErrorAction Stop).Source
    } catch {
        Write-Error "Python not found on PATH. Install Python or use the full path to python.exe."
        exit 1
    }
}

$args = @(
    $pyScript,
    $Path
)
if ($Passes -ne 1) {
    $args += "--passes", $Passes
}
if ($Force) {
    $args += "--force"
}

& $python $args
exit $LASTEXITCODE
