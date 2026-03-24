# Asset & License Notes

## Free third-party assets in active use

### Kenney

Used from:
- `Assets/ThirdParty/Kenney/animated-characters-1/...`
- `Assets/ThirdParty/Kenney/furniture-kit/...`
- `Assets/ThirdParty/Kenney/impact-sounds/...`
- `Assets/ThirdParty/Kenney/rpg-audio/...`
- `Assets/ThirdParty/Kenney/sci-fi-sounds/...`

Used for:
- survivor/player support visuals;
- furniture and room dressing;
- footsteps;
- door, machinery and ambient SFX.

Source:
- [Kenney](https://kenney.nl/assets)

### Poly Pizza / Quaternius

Used from:
- `Assets/ThirdParty/PolyPizza/SurvivalPack/...`
- `Assets/ThirdParty/PolyPizza/PostApocalypse/...`
- `Assets/ThirdParty/Downloaded/PolyPizza/LargeElectricGenerator/...`
- `Assets/ThirdParty/Downloaded/PolyPizza/HoodieCharacter/...`

Used for:
- battery pickup mesh;
- debris, industrial clutter and blood props;
- generator mesh and texture;
- current survivor/player model.

Licenses:
- local SurvivalPack/PostApocalypse folders are free Poly Pizza exports already present in the project;
- downloaded generator asset `large_electric-generator.fbx` is from Poly Pizza and marked `CC-BY 3.0`.

Required attribution for the downloaded generator:
- source page: [Poly Pizza - Large Electric Generator](https://poly.pizza/m/ZPlQHwiqTp)

### OpenGameArt

Used from:
- `Assets/ThirdParty/Downloaded/OGA/MobileReadyZombie/...`
- `Assets/ThirdParty/Downloaded/OGA/HomeInterior/...`
- `Assets/ThirdParty/Downloaded/Quaternius/ModularSciFi/...`

Used for:
- boss/monster mesh and texture;
- additional room dressing props.

Licenses:
- `Mobile Ready Zombie` is used as the current boss model and is `CC0`.
- `3D Interior Home Assets` includes `License.txt` in the asset folder and is `CC0 1.0 Universal`.

Source pages:
- [OpenGameArt - Mobile Ready Zombie](https://opengameart.org/content/mobile-ready-zombie)
- [OpenGameArt - 3D Interior Home Assets](https://opengameart.org/content/3d-interior-home-assets)
- [OpenGameArt - Lowpoly Modular Sci-Fi Environments](https://opengameart.org/content/lowpoly-modular-sci-fi-environments)

Additional note:
- `Lowpoly Modular Sci-Fi Environments` is by Quaternius and distributed on OpenGameArt under `CC0`.
- It is used here for the reinforced maintenance/server-block visual pass without replacing the main hospital layout.

## In-project generated assets

Generated directly by `Assets/Editor/HorrorPrototypeBuilder.cs`:
- level blockout and layout;
- generated materials;
- blood stains and decals made from primitives/materials;
- menus and HUD layout;
- placement/assembly of downloaded props into the final playable scene.

These generated assets are project-authored content.

## Procedural audio fallback

Fallback clips can still be synthesized by:
- `Assets/Scripts/Audio/ProceduralAudioFactory.cs`

The current build prefers imported audio assets where assigned; procedural clips remain only as runtime fallback if an asset reference is missing.

## Networking framework

- Mirror Networking source is synchronized into `Assets/Mirror`.
- License is provided by the Mirror project in the imported source tree.
