# CardsWithAncientSkin

A Slay the Spire 2 visual mod that selectively applies an Ancient-style card layout to configured cards.

## Included

- Harmony-based card render hook
- External `card_config.json` toggle file
- Custom borders and Ancient-form portrait resources
- Staging script for a local STS2 runtime copy

## Build

```powershell
dotnet build .\CardsWithAncientSkin.csproj -c Debug
```

## Stage To A Local Runtime Copy

```powershell
powershell -ExecutionPolicy Bypass -File .\stage_mod.ps1 Debug
```

This repository expects to sit next to `Godot_Card_Render_Setup`, because the project references game assemblies through relative paths.

## Runtime Mod Folder Layout

```text
CardsWithAncientSkin/
  CardsWithAncientSkin.json
  CardsWithAncientSkin.dll
  card_config.json
  resources/
```

## Config

Only cards explicitly set to `true` in `card_config.json` will use the Ancient visual style.