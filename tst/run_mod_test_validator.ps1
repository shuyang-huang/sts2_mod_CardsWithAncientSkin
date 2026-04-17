$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptRoot
$workspaceRoot = Split-Path -Parent $projectRoot
$outputDir = Join-Path $projectRoot 'test_output'
$runtimeExe = Join-Path $workspaceRoot 'Godot_Card_Render_Setup\runtime\SlayTheSpire2.exe'
$runtimeModDir = Join-Path $workspaceRoot 'Godot_Card_Render_Setup\runtime\mods\CardsWithAncientSkin'
$logFile = Join-Path $outputDir 'validator_run.log'

Push-Location $projectRoot
try {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null

    Get-ChildItem -Path (Join-Path $outputDir '*') -File -Include *.png,*.txt,*.log -ErrorAction SilentlyContinue |
        Remove-Item -Force -ErrorAction SilentlyContinue

    dotnet build .\CardsWithAncientSkin.csproj -c Debug /p:IncludeTestHooks=true
    powershell -ExecutionPolicy Bypass -File .\scripts\stage_mod.ps1 Debug
    Remove-Item -LiteralPath (Join-Path $runtimeModDir 'card_config.json') -Force -ErrorAction SilentlyContinue

    & $runtimeExe `
        --path $workspaceRoot `
        --scene 'res://Godot_Card_Render_Setup/render_apotheosis.tscn' `
        --audio-driver Dummy `
        --quit-after 30000 `
        --log-file $logFile `
        -- `
        --mod-validation
}
finally {
    Pop-Location
}
