$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$workspaceRoot = Split-Path -Parent $projectRoot
$configuration = if ($args.Count -gt 0) { $args[0] } else { 'Debug' }
$runtimeModsRoot = Join-Path $workspaceRoot 'Godot_Card_Render_Setup\runtime\mods'
$targetDir = Join-Path $runtimeModsRoot 'CardsWithAncientSkin'
$builtDll = Join-Path $projectRoot "bin\$configuration\net9.0\CardsWithAncientSkin.dll"
$manifest = Join-Path $projectRoot 'CardsWithAncientSkin.json'
$config = Join-Path $projectRoot 'card_config.json'
$resourcesDir = Join-Path $projectRoot 'resources'

if (-not (Test-Path -LiteralPath $builtDll)) {
    throw "Built DLL not found: $builtDll"
}

New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
Copy-Item -LiteralPath $builtDll -Destination (Join-Path $targetDir 'CardsWithAncientSkin.dll') -Force
Copy-Item -LiteralPath $manifest -Destination (Join-Path $targetDir 'CardsWithAncientSkin.json') -Force
if (Test-Path -LiteralPath $config) {
    Copy-Item -LiteralPath $config -Destination (Join-Path $targetDir 'card_config.json') -Force
}
if (Test-Path -LiteralPath $resourcesDir) {
    $targetResourcesDir = Join-Path $targetDir 'resources'
    New-Item -ItemType Directory -Path $targetResourcesDir -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $resourcesDir '*') -Destination $targetResourcesDir -Recurse -Force
}

Write-Host "Staged mod to: $targetDir"
