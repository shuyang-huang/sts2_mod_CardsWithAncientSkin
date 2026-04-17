$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptRoot
$workspaceRoot = Split-Path -Parent $projectRoot
$configuration = if ($args.Count -gt 0) { $args[0] } else { 'Debug' }
$runtimeModsRoot = Join-Path $workspaceRoot 'Godot_Card_Render_Setup\runtime\mods'
$targetDir = Join-Path $runtimeModsRoot 'CardsWithAncientSkin'
$builtDll = Join-Path $projectRoot "bin\$configuration\net9.0\CardsWithAncientSkin.dll"
$manifest = Join-Path $projectRoot 'CardsWithAncientSkin.json'
$resourcesDir = Join-Path $projectRoot 'resources'

if (-not (Test-Path -LiteralPath $builtDll)) {
    throw "Built DLL not found: $builtDll"
}

New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
Copy-Item -LiteralPath $builtDll -Destination (Join-Path $targetDir 'CardsWithAncientSkin.dll') -Force
Copy-Item -LiteralPath $manifest -Destination (Join-Path $targetDir 'CardsWithAncientSkin.json') -Force
Remove-Item -LiteralPath (Join-Path $targetDir 'card_config.json') -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath (Join-Path $targetDir 'card_config.data') -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath (Join-Path $targetDir 'cards_with_ancient_skin.config') -Force -ErrorAction SilentlyContinue
if (Test-Path -LiteralPath $resourcesDir) {
    $targetResourcesDir = Join-Path $targetDir 'resources'
    Remove-Item -LiteralPath $targetResourcesDir -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Path $targetResourcesDir -Force | Out-Null
    Copy-Item -Path (Join-Path $resourcesDir '*') -Destination $targetResourcesDir -Recurse -Force
}

Write-Host "Staged mod to: $targetDir"
