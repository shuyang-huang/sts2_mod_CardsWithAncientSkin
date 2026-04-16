$ErrorActionPreference = 'Stop'

$validatorDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $validatorDir
$workspaceRoot = Split-Path -Parent $projectRoot
$runtimeExe = Join-Path $workspaceRoot 'Godot_Card_Render_Setup\runtime\SlayTheSpire2.exe'
$runtimeModDir = Join-Path $workspaceRoot 'Godot_Card_Render_Setup\runtime\mods\CardsWithAncientSkin'
$logFile = Join-Path $validatorDir 'validator_run.log'

Push-Location $projectRoot
try {
    Get-ChildItem -Path (Join-Path $validatorDir '*') -File -Include *.png,*.txt,*.log -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -ne 'run_mod_test_validator.ps1' } |
        Remove-Item -Force -ErrorAction SilentlyContinue

    dotnet build .\CardsWithAncientSkin.csproj -c Debug
    powershell -ExecutionPolicy Bypass -File .\stage_mod.ps1 Debug
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
