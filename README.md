# Asylum Coop Prototype

Atmospheric co-op horror prototype built in Unity.

## Project Structure

- `Assets/` game code, scenes, generated materials, third-party assets
- `Packages/` Unity package manifest
- `ProjectSettings/` Unity project settings
- `automation/run_full_prototype.ps1` local setup/build automation

## Open In Unity

1. Open `C:\Users\alka\Documents\project\AsylumCoopPrototype` in Unity Hub.
2. Let Unity reimport the project and regenerate `Library/`.
3. Open the main menu scene and press Play, or build from Unity.

## Multiplayer

- Host can start a direct session from the main menu.
- Join works by direct host address / public IP.
- For internet play, the host may need UDP port `7777` forwarded if UPnP does not succeed automatically.

## Running The Existing Windows Build

- `RUN_HOST.bat`
- `RUN_JOIN.bat`
- `RUN_LOCAL_2_PLAYERS.bat`

## Assets And Licenses

- See `ASSET_LICENSES.md`
- Russian project notes: `README_RU.md`
