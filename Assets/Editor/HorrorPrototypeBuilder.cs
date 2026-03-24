using System.Collections.Generic;
using System;
using System.IO;
using AsylumHorror.Audio;
using AsylumHorror.Core;
using AsylumHorror.Monster;
using AsylumHorror.Network;
using AsylumHorror.Player;
using AsylumHorror.Tasks;
using AsylumHorror.UI;
using AsylumHorror.World;
using Mirror;
using kcp2k;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AsylumHorror.EditorTools
{
    public static class HorrorPrototypeBuilder
    {
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string LobbyScenePath = "Assets/Scenes/Lobby.unity";
        private const string HospitalScenePath = "Assets/Scenes/HospitalLevel.unity";

        private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
        private const string MonsterPrefabPath = "Assets/Prefabs/Monster.prefab";
        private const string GeneratorPrefabPath = "Assets/Prefabs/Generator.prefab";
        private const string KeycardPrefabPath = "Assets/Prefabs/Keycard.prefab";
        private const string PowerConsolePrefabPath = "Assets/Prefabs/PowerConsole.prefab";
        private const string ExitDoorPrefabPath = "Assets/Prefabs/ExitDoor.prefab";
        private const string HookPrefabPath = "Assets/Prefabs/Hook.prefab";
        private const string DoorPrefabPath = "Assets/Prefabs/Door.prefab";
        private const string BatteryPrefabPath = "Assets/Prefabs/Battery.prefab";
        private const string HideLockerPrefabPath = "Assets/Prefabs/HideLocker.prefab";
        private const string HideCratePrefabPath = "Assets/Prefabs/HideCrate.prefab";
        private const string HideCurtainPrefabPath = "Assets/Prefabs/HideCurtain.prefab";

        private const string GeneratedRootPath = "Assets/Generated";
        private const string GeneratedMaterialFolderPath = "Assets/Generated/Materials";
        private const string GeneratedTextureFolderPath = "Assets/Generated/Textures";

        private const string PlayerModelAssetPath = "Assets/ThirdParty/Downloaded/PolyPizza/WorkerCharacter/Worker.fbx";
        private const string MonsterModelAssetPath = "Assets/ThirdParty/Downloaded/OGA/MobileReadyZombie/Zombie.fbx";
        private const string MonsterTextureAssetPath = "Assets/ThirdParty/Downloaded/OGA/MobileReadyZombie/Zombie.png";
        private const string GeneratorModelAssetPath = "Assets/ThirdParty/Downloaded/PolyPizza/LargeElectricGenerator/large_electric-generator.fbx";
        private const string GeneratorTextureAssetPath = "Assets/ThirdParty/Downloaded/PolyPizza/LargeElectricGenerator/Big_EG.png";
        private const string BatteryModelAssetPath = "Assets/ThirdParty/PolyPizza/SurvivalPack/Battery/Battery_Small.fbx";
        private const string HomeInteriorTextureAssetPath = "Assets/ThirdParty/Downloaded/OGA/HomeInterior/texture.png";
        private const string HomeBedAssetPath = "Assets/ThirdParty/Downloaded/OGA/HomeInterior/bed.fbx";
        private const string HomeBenchAssetPath = "Assets/ThirdParty/Downloaded/OGA/HomeInterior/bench.fbx";
        private const string HomeBookcaseAssetPath = "Assets/ThirdParty/Downloaded/OGA/HomeInterior/bookcase.fbx";
        private const string HomeCabinetAssetPath = "Assets/ThirdParty/Downloaded/OGA/HomeInterior/cabinet.fbx";
        private const string HomeChairAssetPath = "Assets/ThirdParty/Downloaded/OGA/HomeInterior/chair01.fbx";
        private const string HomeTableAssetPath = "Assets/ThirdParty/Downloaded/OGA/HomeInterior/table.fbx";
        private const string HomeShelfAssetPath = "Assets/ThirdParty/Downloaded/OGA/HomeInterior/shelf.fbx";
        private const string KenneyDeskAssetPath = "Assets/ThirdParty/Kenney/furniture-kit/Models/FBX format/desk.fbx";
        private const string KenneyChairDeskAssetPath = "Assets/ThirdParty/Kenney/furniture-kit/Models/FBX format/chairDesk.fbx";
        private const string KenneyComputerScreenAssetPath = "Assets/ThirdParty/Kenney/furniture-kit/Models/FBX format/computerScreen.fbx";
        private const string KenneyComputerKeyboardAssetPath = "Assets/ThirdParty/Kenney/furniture-kit/Models/FBX format/computerKeyboard.fbx";
        private const string KenneyTrashcanAssetPath = "Assets/ThirdParty/Kenney/furniture-kit/Models/FBX format/trashcan.fbx";
        private const string KenneyCoatRackAssetPath = "Assets/ThirdParty/Kenney/furniture-kit/Models/FBX format/coatRackStanding.fbx";
        private const string KenneyBathroomCabinetAssetPath = "Assets/ThirdParty/Kenney/furniture-kit/Models/FBX format/bathroomCabinet.fbx";
        private const string KenneyBathroomSinkAssetPath = "Assets/ThirdParty/Kenney/furniture-kit/Models/FBX format/bathroomSinkSquare.fbx";
        private const string KenneyToiletAssetPath = "Assets/ThirdParty/Kenney/furniture-kit/Models/FBX format/toiletSquare.fbx";
        private const string KenneyWasherStackAssetPath = "Assets/ThirdParty/Kenney/furniture-kit/Models/FBX format/washerDryerStacked.fbx";
        private const string KenneyPanelingAssetPath = "Assets/ThirdParty/Kenney/furniture-kit/Models/FBX format/paneling.fbx";
        private const string KenneyLampWallAssetPath = "Assets/ThirdParty/Kenney/furniture-kit/Models/FBX format/lampWall.fbx";
        private const string DebrisBarrelAssetPath = "Assets/ThirdParty/PolyPizza/PostApocalypse/Barrel/Barrel.fbx";
        private const string DebrisContainerRedAssetPath = "Assets/ThirdParty/PolyPizza/PostApocalypse/ContainerRed/Container_Red.fbx";
        private const string DebrisContainerGreenAssetPath = "Assets/ThirdParty/PolyPizza/PostApocalypse/ContainerGreen/Container_Green.fbx";
        private const string DebrisCinderBlockAssetPath = "Assets/ThirdParty/PolyPizza/PostApocalypse/CinderBlock/CinderBlock.fbx";
        private const string DebrisTrashBagAssetPath = "Assets/ThirdParty/PolyPizza/PostApocalypse/TrashBag/TrashBag_1.fbx";
        private const string DebrisPalletBrokenAssetPath = "Assets/ThirdParty/PolyPizza/PostApocalypse/PalletBroken/Pallet_Broken.fbx";
        private const string DebrisWheelsStackAssetPath = "Assets/ThirdParty/PolyPizza/PostApocalypse/WheelsStack/Wheels_Stack.fbx";
        private const string DebrisGasCanAssetPath = "Assets/ThirdParty/PolyPizza/SurvivalPack/GasCan/GasCan.fbx";
        private const string QuaterniusWall2AssetPath = "Assets/ThirdParty/Downloaded/Quaternius/ModularSciFi/FBX/Walls/Wall_2.fbx";
        private const string QuaterniusWall5AssetPath = "Assets/ThirdParty/Downloaded/Quaternius/ModularSciFi/FBX/Walls/Wall_5.fbx";
        private const string QuaterniusColumn2AssetPath = "Assets/ThirdParty/Downloaded/Quaternius/ModularSciFi/FBX/Column_2.fbx";
        private const string QuaterniusCrateLongAssetPath = "Assets/ThirdParty/Downloaded/Quaternius/ModularSciFi/FBX/Props_CrateLong.fbx";
        private const string QuaterniusComputerAssetPath = "Assets/ThirdParty/Downloaded/Quaternius/ModularSciFi/FBX/Props_Computer.fbx";
        private const string QuaterniusShelfTallAssetPath = "Assets/ThirdParty/Downloaded/Quaternius/ModularSciFi/FBX/Props_Shelf_Tall.fbx";
        private const string QuaterniusPipesLongAssetPath = "Assets/ThirdParty/Downloaded/Quaternius/ModularSciFi/FBX/Details/Details_Pipes_Long.fbx";
        private const string QuaterniusDoorSingleAssetPath = "Assets/ThirdParty/Downloaded/Quaternius/ModularSciFi/FBX/Door_Single.fbx";
        private const string ConcreteFootstepAPath = "Assets/ThirdParty/Kenney/impact-sounds/Audio/footstep_concrete_000.ogg";
        private const string ConcreteFootstepBPath = "Assets/ThirdParty/Kenney/impact-sounds/Audio/footstep_concrete_001.ogg";
        private const string ConcreteFootstepCPath = "Assets/ThirdParty/Kenney/impact-sounds/Audio/footstep_concrete_002.ogg";
        private const string ConcreteFootstepDPath = "Assets/ThirdParty/Kenney/impact-sounds/Audio/footstep_concrete_003.ogg";
        private const string CarpetFootstepAPath = "Assets/ThirdParty/Kenney/impact-sounds/Audio/footstep_carpet_001.ogg";
        private const string CarpetFootstepBPath = "Assets/ThirdParty/Kenney/impact-sounds/Audio/footstep_carpet_003.ogg";
        private const string MonsterStepAPath = "Assets/ThirdParty/Kenney/impact-sounds/Audio/impactMetal_heavy_000.ogg";
        private const string MonsterStepBPath = "Assets/ThirdParty/Kenney/impact-sounds/Audio/impactMetal_heavy_002.ogg";
        private const string MonsterStepCPath = "Assets/ThirdParty/Kenney/impact-sounds/Audio/impactMetal_heavy_004.ogg";
        private const string DoorOpenClipPath = "Assets/ThirdParty/Kenney/rpg-audio/Audio/doorOpen_2.ogg";
        private const string DoorCloseClipPath = "Assets/ThirdParty/Kenney/rpg-audio/Audio/doorClose_3.ogg";
        private const string LockedDoorClipPath = "Assets/ThirdParty/Kenney/rpg-audio/Audio/metalClick.ogg";
        private const string PickupClipPath = "Assets/ThirdParty/Kenney/rpg-audio/Audio/metalClick.ogg";
        private const string GeneratorStartClipPath = "Assets/ThirdParty/Kenney/sci-fi-sounds/Audio/computerNoise_003.ogg";
        private const string GeneratorLoopClipPath = "Assets/ThirdParty/Kenney/sci-fi-sounds/Audio/computerNoise_000.ogg";
        private const string PowerRestoreClipPath = "Assets/ThirdParty/Kenney/sci-fi-sounds/Audio/spaceEngineLarge_003.ogg";
        private const string AmbientLoopClipPath = "Assets/ThirdParty/Kenney/sci-fi-sounds/Audio/lowFrequency_explosion_001.ogg";
        private const string AmbientOneShotAPath = "Assets/ThirdParty/Kenney/impact-sounds/Audio/impactMetal_medium_001.ogg";
        private const string AmbientOneShotBPath = "Assets/ThirdParty/Kenney/impact-sounds/Audio/impactMetal_medium_003.ogg";
        private const string AmbientOneShotCPath = "Assets/ThirdParty/Kenney/impact-sounds/Audio/impactWood_medium_003.ogg";
        private const string MonsterPatrolClipPath = "Assets/ThirdParty/Kenney/sci-fi-sounds/Audio/slime_001.ogg";
        private const string ScareStingerClipPath = "Assets/ThirdParty/Kenney/impact-sounds/Audio/impactMetal_heavy_003.ogg";

        private static Font defaultFont;
        private static readonly Dictionary<string, Material> materialCache = new Dictionary<string, Material>();
        private static readonly Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();

        private enum RoomDoorSide
        {
            North,
            South,
            East,
            West
        }

        private sealed class PrototypeAssets
        {
            public GameObject PlayerPrefab;
            public GameObject MonsterPrefab;
            public GameObject GeneratorPrefab;
            public GameObject KeycardPrefab;
            public GameObject PowerConsolePrefab;
            public GameObject ExitDoorPrefab;
            public GameObject HookPrefab;
            public GameObject DoorPrefab;
            public GameObject BatteryPrefab;
            public GameObject HideLockerPrefab;
            public GameObject HideCratePrefab;
            public GameObject HideCurtainPrefab;

            public List<GameObject> ToSpawnableList()
            {
                return new List<GameObject>
                {
                    MonsterPrefab,
                    GeneratorPrefab,
                    KeycardPrefab,
                    PowerConsolePrefab,
                    ExitDoorPrefab,
                    HookPrefab,
                    DoorPrefab,
                    BatteryPrefab
                };
            }
        }

        [MenuItem("Tools/Horror/Create Full Prototype")]
        public static void CreateFullPrototype()
        {
            try
            {
                ResetGeneratedCaches();
                EnsureFolders();
                PrototypeAssets assets = CreateOrUpdatePrefabs();
                CreateMainMenuScene(assets);
                CreateLobbyScene();
                CreateHospitalScene(assets);
                ConfigureBuildSettings();
                RepairUnsupportedProjectMaterials();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog("Horror Prototype", "Prototype created successfully.", "OK");
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog("Horror Prototype", $"Build failed:\n{exception.Message}", "OK");
                }

                throw;
            }
        }

        [MenuItem("Tools/Horror/Build Windows EXE")]
        public static void BuildWindowsExeMenu()
        {
            BuildWindowsExe();
        }

        public static void CreatePrototypeAndBuildWindowsExe()
        {
            CreateFullPrototype();
            BuildWindowsExe();
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }

            if (!AssetDatabase.IsValidFolder("Assets/Editor"))
            {
                AssetDatabase.CreateFolder("Assets", "Editor");
            }

            if (!AssetDatabase.IsValidFolder(GeneratedRootPath))
            {
                AssetDatabase.CreateFolder("Assets", "Generated");
            }

            if (!AssetDatabase.IsValidFolder(GeneratedMaterialFolderPath))
            {
                AssetDatabase.CreateFolder(GeneratedRootPath, "Materials");
            }

            if (!AssetDatabase.IsValidFolder(GeneratedTextureFolderPath))
            {
                AssetDatabase.CreateFolder(GeneratedRootPath, "Textures");
            }
        }

        private static PrototypeAssets CreateOrUpdatePrefabs()
        {
            PrototypeAssets assets = new PrototypeAssets
            {
                PlayerPrefab = BuildPlayerPrefab(),
                MonsterPrefab = BuildMonsterPrefab(),
                GeneratorPrefab = BuildGeneratorPrefab(),
                KeycardPrefab = BuildKeycardPrefab(),
                PowerConsolePrefab = BuildPowerConsolePrefab(),
                ExitDoorPrefab = BuildExitDoorPrefab(),
                HookPrefab = BuildHookPrefab(),
                DoorPrefab = BuildDoorPrefab(),
                BatteryPrefab = BuildBatteryPrefab(),
                HideLockerPrefab = BuildHideLockerPrefab(),
                HideCratePrefab = BuildHideCratePrefab(),
                HideCurtainPrefab = BuildHideCurtainPrefab()
            };

            return assets;
        }

        private static GameObject BuildPlayerPrefab()
        {
            GameObject root = new GameObject("Player");

            CharacterController characterController = root.AddComponent<CharacterController>();
            characterController.height = 1.8f;
            characterController.radius = 0.35f;
            characterController.center = new Vector3(0f, 0.9f, 0f);

            root.AddComponent<NetworkIdentity>();
            SimpleNetworkTransform networkTransform = root.AddComponent<SimpleNetworkTransform>();
            SetBool(networkTransform, "ownerAuthoritative", true);

            NetworkPlayerStatus playerStatus = root.AddComponent<NetworkPlayerStatus>();
            NetworkPlayerController playerController = root.AddComponent<NetworkPlayerController>();
            DownedReviveInteractable downedRevive = root.AddComponent<DownedReviveInteractable>();
            PlayerInteractor interactor = root.AddComponent<PlayerInteractor>();
            root.AddComponent<PlayerStressController>();
            PlayerFlashlight flashlight = root.AddComponent<PlayerFlashlight>();
            PlayerAudioController audioController = root.AddComponent<PlayerAudioController>();

            List<Object> hiddenRenderers = new List<Object>();

            Material playerMaterial = CreateMaterial(new Color(0.19f, 0.22f, 0.24f), "player_jacket");
            GameObject playerVisual = InstantiateStyledModel(
                root.transform,
                PlayerModelAssetPath,
                "PlayerVisual",
                new Vector3(0f, 0f, 0f),
                Quaternion.identity,
                1.82f,
                playerMaterial,
                true,
                true);

            if (playerVisual == null)
            {
                GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                torso.name = "Torso";
                torso.transform.SetParent(root.transform, false);
                torso.transform.localPosition = new Vector3(0f, 1.05f, 0f);
                torso.transform.localScale = new Vector3(0.72f, 0.72f, 0.52f);
                torso.GetComponent<Renderer>().sharedMaterial = playerMaterial;
                Object.DestroyImmediate(torso.GetComponent<Collider>());
                hiddenRenderers.Add(torso.GetComponent<Renderer>());
            }
            else
            {
                foreach (Renderer renderer in playerVisual.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer != null)
                    {
                        hiddenRenderers.Add(renderer);
                    }
                }
            }

            Transform cameraRoot = new GameObject("CameraRoot").transform;
            cameraRoot.SetParent(root.transform, false);
            cameraRoot.localPosition = new Vector3(0f, 1.6f, 0f);

            GameObject cameraObject = new GameObject("PlayerCamera");
            cameraObject.transform.SetParent(cameraRoot, false);
            Camera playerCamera = cameraObject.AddComponent<Camera>();
            playerCamera.fieldOfView = 75f;
            playerCamera.nearClipPlane = 0.03f;
            playerCamera.useOcclusionCulling = false;
            playerCamera.allowHDR = true;
            AudioListener audioListener = cameraObject.AddComponent<AudioListener>();

            GameObject flashlightObject = new GameObject("FlashlightLight");
            flashlightObject.transform.SetParent(cameraObject.transform, false);
            flashlightObject.transform.localPosition = new Vector3(0.12f, -0.05f, 0.2f);
            Light flashlightLight = flashlightObject.AddComponent<Light>();
            flashlightLight.type = LightType.Spot;
            flashlightLight.range = 23f;
            flashlightLight.intensity = 2.8f;
            flashlightLight.spotAngle = 52f;

            AudioSource footstepsSource = root.AddComponent<AudioSource>();
            footstepsSource.spatialBlend = 1f;
            footstepsSource.playOnAwake = false;
            // Assign footstep clips in PlayerAudioController (walk/run/crouch arrays) after generation.

            AudioSource heartbeatSource = root.AddComponent<AudioSource>();
            heartbeatSource.spatialBlend = 0f;
            heartbeatSource.loop = true;
            heartbeatSource.playOnAwake = false;
            // Assign heartbeat loop clip here.

            AudioSource breathingSource = root.AddComponent<AudioSource>();
            breathingSource.spatialBlend = 0f;
            breathingSource.loop = true;
            breathingSource.playOnAwake = false;
            // Assign breathing loop clip here.

            AudioSource chaseMusicSource = root.AddComponent<AudioSource>();
            chaseMusicSource.spatialBlend = 0f;
            chaseMusicSource.loop = true;
            chaseMusicSource.playOnAwake = false;
            // Assign chase music clip here.

            SetReference(playerController, "cameraRoot", cameraRoot);
            SetReference(playerController, "playerCamera", playerCamera);
            SetReference(playerController, "audioListener", audioListener);
            SetReferenceArray(playerController, "firstPersonHiddenRenderers", hiddenRenderers.ToArray());

            SetReference(downedRevive, "ownerStatus", playerStatus);
            SetString(downedRevive, "interactionName", "Revive Teammate");
            SetFloat(downedRevive, "holdDuration", 1.8f);
            SetFloat(downedRevive, "serverInteractDistance", 3.2f);

            SetReference(interactor, "interactionCamera", playerCamera);
            SetFloat(interactor, "interactionDistance", 3.2f);
            SetReferenceArray(flashlight, "flashlightLights", new Object[] { flashlightLight });
            SetReference(flashlight, "toggleAudioSource", null);
            SetReference(flashlight, "toggleOnClip", null);
            SetReference(flashlight, "toggleOffClip", null);
            SetReference(audioController, "footstepsSource", footstepsSource);
            SetReference(audioController, "heartbeatSource", heartbeatSource);
            SetReference(audioController, "breathingSource", breathingSource);
            SetReference(audioController, "chaseMusicSource", chaseMusicSource);
            SetReferenceArray(audioController, "walkFootsteps", LoadAudioClipAssets(
                ConcreteFootstepAPath,
                ConcreteFootstepBPath,
                ConcreteFootstepCPath,
                ConcreteFootstepDPath));
            SetReferenceArray(audioController, "runFootsteps", LoadAudioClipAssets(
                ConcreteFootstepBPath,
                ConcreteFootstepCPath,
                ConcreteFootstepDPath,
                ConcreteFootstepAPath));
            SetReferenceArray(audioController, "crouchFootsteps", LoadAudioClipAssets(
                CarpetFootstepAPath,
                CarpetFootstepBPath));

            return SavePrefab(root, PlayerPrefabPath);
        }

        private static GameObject BuildMonsterPrefab()
        {
            GameObject root = new GameObject("Monster");

            CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 1.2f, 0f);
            collider.height = 2.6f;
            collider.radius = 0.42f;

            root.AddComponent<NetworkIdentity>();
            SimpleNetworkTransform networkTransform = root.AddComponent<SimpleNetworkTransform>();
            SetBool(networkTransform, "ownerAuthoritative", false);

            UnityEngine.AI.NavMeshAgent navMeshAgent = root.AddComponent<UnityEngine.AI.NavMeshAgent>();
            navMeshAgent.speed = 4.8f;
            navMeshAgent.angularSpeed = 240f;
            navMeshAgent.acceleration = 14f;
            navMeshAgent.radius = 0.45f;
            navMeshAgent.height = 2.2f;
            navMeshAgent.stoppingDistance = 0.3f;

            MonsterAI monsterAI = root.AddComponent<MonsterAI>();
            MonsterPresentation monsterPresentation = root.AddComponent<MonsterPresentation>();
            AudioSource audioSource = root.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.loop = true;
            audioSource.minDistance = 5f;
            audioSource.maxDistance = 28f;
            audioSource.volume = 0f;

            AudioSource stepAudioSource = root.AddComponent<AudioSource>();
            stepAudioSource.playOnAwake = false;
            stepAudioSource.spatialBlend = 1f;
            stepAudioSource.loop = false;
            stepAudioSource.minDistance = 8f;
            stepAudioSource.maxDistance = 44f;
            stepAudioSource.volume = 1.05f;
            stepAudioSource.pitch = 0.84f;

            Material monsterMaterial = CreateTexturedMaterial(MonsterTextureAssetPath, new Color(0.78f, 0.8f, 0.78f), "monster_skin");
            GameObject monsterVisual = InstantiateStyledModel(
                root.transform,
                MonsterModelAssetPath,
                "MonsterVisual",
                new Vector3(0f, 0f, 0f),
                Quaternion.Euler(-90f, 0f, 0f),
                2.55f,
                monsterMaterial,
                true,
                true);

            if (monsterVisual == null)
            {
                GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                torso.name = "Torso";
                torso.transform.SetParent(root.transform, false);
                torso.transform.localPosition = new Vector3(0f, 1.38f, 0f);
                torso.transform.localScale = new Vector3(1.02f, 1.28f, 0.94f);
                torso.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.12f, 0.12f, 0.14f), "monster_skin");
                Object.DestroyImmediate(torso.GetComponent<Collider>());

                GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                head.name = "Head";
                head.transform.SetParent(root.transform, false);
                head.transform.localPosition = new Vector3(0f, 2.34f, 0.12f);
                head.transform.localScale = new Vector3(0.56f, 0.56f, 0.56f);
                head.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.15f, 0.15f, 0.17f), "monster_skin");
                Object.DestroyImmediate(head.GetComponent<Collider>());
            }

            GameObject eyeLightObject = new GameObject("EyeGlow");
            eyeLightObject.transform.SetParent(root.transform, false);
            eyeLightObject.transform.localPosition = new Vector3(0f, 2.18f, 0.35f);
            Light eyeLight = eyeLightObject.AddComponent<Light>();
            eyeLight.type = LightType.Point;
            eyeLight.color = new Color(0.82f, 0.1f, 0.1f);
            eyeLight.intensity = 1.18f;
            eyeLight.range = 3.1f;

            GameObject eyeLeft = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eyeLeft.name = "EyeL";
            eyeLeft.transform.SetParent(root.transform, false);
            eyeLeft.transform.localPosition = new Vector3(-0.12f, 2.22f, 0.3f);
            eyeLeft.transform.localScale = new Vector3(0.07f, 0.07f, 0.07f);
            eyeLeft.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.52f, 0.05f, 0.05f), "monster_eye", true, new Color(0.5f, 0f, 0f));
            Object.DestroyImmediate(eyeLeft.GetComponent<Collider>());

            GameObject eyeRight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eyeRight.name = "EyeR";
            eyeRight.transform.SetParent(root.transform, false);
            eyeRight.transform.localPosition = new Vector3(0.12f, 2.22f, 0.3f);
            eyeRight.transform.localScale = new Vector3(0.07f, 0.07f, 0.07f);
            eyeRight.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.52f, 0.05f, 0.05f), "monster_eye", true, new Color(0.5f, 0f, 0f));
            Object.DestroyImmediate(eyeRight.GetComponent<Collider>());

            Transform carryAnchor = new GameObject("CarryAnchor").transform;
            carryAnchor.SetParent(root.transform, false);
            carryAnchor.localPosition = new Vector3(0f, 1.5f, 0.85f);

            Transform roundStart = new GameObject("RoundStart").transform;
            roundStart.SetParent(root.transform, false);
            roundStart.localPosition = Vector3.zero;

            SetReference(monsterAI, "navMeshAgent", navMeshAgent);
            SetReference(monsterAI, "carryAnchor", carryAnchor);
            SetReference(monsterAI, "roundStartPoint", roundStart);
            SetReference(monsterAI, "audioSource", audioSource);
            SetReference(monsterAI, "stepAudioSource", stepAudioSource);
            SetReference(monsterAI, "patrolClip", null);
            SetReference(monsterAI, "chaseClip", null);
            SetReferenceArray(monsterAI, "footstepClips", LoadAudioClipAssets(
                ConcreteFootstepAPath,
                ConcreteFootstepBPath,
                ConcreteFootstepCPath,
                ConcreteFootstepDPath));

            if (monsterVisual != null)
            {
                SetReference(monsterPresentation, "visualRoot", monsterVisual.transform);
            }

            SetReference(monsterPresentation, "eyeGlowRoot", eyeLightObject.transform);

            return SavePrefab(root, MonsterPrefabPath);
        }

        private static GameObject BuildGeneratorPrefab()
        {
            GameObject root = new GameObject("Generator");
            BoxCollider interactionCollider = root.AddComponent<BoxCollider>();
            interactionCollider.center = new Vector3(0f, 1.2f, 0f);
            interactionCollider.size = new Vector3(2.2f, 2.4f, 1.8f);

            root.AddComponent<NetworkIdentity>();
            GeneratorTask task = root.AddComponent<GeneratorTask>();
            AudioSource audioSource = root.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.minDistance = 3f;
            audioSource.maxDistance = 18f;
            audioSource.volume = 0.28f;

            AudioSource loopAudioSource = root.AddComponent<AudioSource>();
            loopAudioSource.playOnAwake = false;
            loopAudioSource.loop = true;
            loopAudioSource.spatialBlend = 1f;
            loopAudioSource.minDistance = 2f;
            loopAudioSource.maxDistance = 9f;
            loopAudioSource.volume = 0.17f;

            Material generatorMaterial = CreateTexturedMaterial(GeneratorTextureAssetPath, new Color(0.9f, 0.92f, 0.9f), "metal");
            GameObject generatorVisual = InstantiateStyledModel(
                root.transform,
                GeneratorModelAssetPath,
                "GeneratorVisual",
                new Vector3(0f, 0f, 0f),
                Quaternion.Euler(0f, 180f, 0f),
                2.35f,
                generatorMaterial,
                true,
                true);

            if (generatorVisual == null)
            {
                GameObject fallbackBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fallbackBody.name = "GeneratorBody";
                fallbackBody.transform.SetParent(root.transform, false);
                fallbackBody.transform.localScale = new Vector3(1.8f, 2f, 1.45f);
                fallbackBody.transform.localPosition = new Vector3(0f, 1f, 0f);
                fallbackBody.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.17f, 0.17f, 0.19f), "metal");
                Object.DestroyImmediate(fallbackBody.GetComponent<Collider>());
            }

            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = "GeneratorPlatform";
            platform.transform.SetParent(root.transform, false);
            platform.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            platform.transform.localScale = new Vector3(2.3f, 0.16f, 1.9f);
            platform.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.12f, 0.12f, 0.13f), "metal");
            Object.DestroyImmediate(platform.GetComponent<Collider>());

            GameObject indicatorObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            indicatorObject.name = "Indicator";
            indicatorObject.transform.SetParent(root.transform, false);
            indicatorObject.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            indicatorObject.transform.localPosition = new Vector3(0f, 1.58f, 0.55f);
            indicatorObject.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.7f, 0.1f, 0.1f), "monster_eye", true, new Color(0.42f, 0f, 0f));
            Object.DestroyImmediate(indicatorObject.GetComponent<Collider>());

            SetReference(task, "indicatorRenderer", indicatorObject.GetComponent<Renderer>());
            SetReference(task, "audioSource", audioSource);
            SetReference(task, "loopAudioSource", loopAudioSource);
            SetReference(task, "activateClip", null);
            SetReference(task, "loopClip", null);
            SetString(task, "interactionName", "Start Generator");
            SetFloat(task, "holdDuration", 2f);

            return SavePrefab(root, GeneratorPrefabPath);
        }

        private static GameObject BuildKeycardPrefab()
        {
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = "Keycard";
            root.transform.localScale = new Vector3(0.42f, 0.045f, 0.62f);
            root.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.78f, 0.75f, 0.66f), "metal");

            GameObject signal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            signal.name = "Signal";
            signal.transform.SetParent(root.transform, false);
            signal.transform.localPosition = new Vector3(0f, 0.12f, 0f);
            signal.transform.localScale = new Vector3(0.16f, 0.16f, 0.16f);
            signal.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.18f, 0.62f, 0.9f), "monster_eye", true, new Color(0.04f, 0.18f, 0.26f));
            Object.DestroyImmediate(signal.GetComponent<Collider>());

            GameObject keyLightObject = new GameObject("KeycardGlow");
            keyLightObject.transform.SetParent(root.transform, false);
            keyLightObject.transform.localPosition = new Vector3(0f, 0.16f, 0f);
            Light keyLight = keyLightObject.AddComponent<Light>();
            keyLight.type = LightType.Point;
            keyLight.range = 2.8f;
            keyLight.intensity = 0.38f;
            keyLight.color = new Color(0.18f, 0.62f, 0.92f);

            root.AddComponent<NetworkIdentity>();
            KeycardTask task = root.AddComponent<KeycardTask>();
            AudioSource audioSource = root.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;

            SetReference(task, "keycardRenderer", root.GetComponent<Renderer>());
            SetReference(task, "keycardCollider", root.GetComponent<Collider>());
            SetReference(task, "audioSource", audioSource);
            SetReference(task, "pickupClip", LoadAudioClipAsset(PickupClipPath));
            SetString(task, "interactionName", "Take Keycard");
            SetFloat(task, "holdDuration", 0f);

            return SavePrefab(root, KeycardPrefabPath);
        }

        private static GameObject BuildPowerConsolePrefab()
        {
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = "PowerConsole";
            root.transform.localScale = new Vector3(1.5f, 1.1f, 0.45f);
            root.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.16f, 0.16f, 0.18f), "metal");

            root.AddComponent<NetworkIdentity>();
            PowerRestoreTask task = root.AddComponent<PowerRestoreTask>();
            AudioSource audioSource = root.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;

            SetReference(task, "panelRenderer", root.GetComponent<Renderer>());
            SetReference(task, "audioSource", audioSource);
            SetReference(task, "restoreClip", LoadAudioClipAsset(PowerRestoreClipPath));
            SetString(task, "interactionName", "Restore Power");
            SetFloat(task, "holdDuration", 2.5f);

            return SavePrefab(root, PowerConsolePrefabPath);
        }

        private static GameObject BuildExitDoorPrefab()
        {
            GameObject root = new GameObject("ExitDoor");
            BoxCollider interactionCollider = root.AddComponent<BoxCollider>();
            interactionCollider.center = new Vector3(0f, 1.55f, 0f);
            interactionCollider.size = new Vector3(5.1f, 3.2f, 1.8f);
            interactionCollider.isTrigger = true;

            root.AddComponent<NetworkIdentity>();
            ExitDoorTask task = root.AddComponent<ExitDoorTask>();
            AudioSource audioSource = root.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "DoorVisual";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(0f, 1.55f, 0f);
            visual.transform.localScale = new Vector3(4.4f, 3.1f, 0.25f);
            visual.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.16f, 0.18f, 0.2f), "door_leaf");

            SetReference(task, "doorVisual", visual.transform);
            SetReference(task, "audioSource", audioSource);
            SetReference(task, "openClip", LoadAudioClipAsset(DoorOpenClipPath));
            SetString(task, "interactionName", "Open Exit");
            SetFloat(task, "holdDuration", 1.6f);

            return SavePrefab(root, ExitDoorPrefabPath);
        }

        private static GameObject BuildHookPrefab()
        {
            GameObject root = new GameObject("Hook");
            BoxCollider interactionCollider = root.AddComponent<BoxCollider>();
            interactionCollider.center = new Vector3(0f, 1.4f, 0f);
            interactionCollider.size = new Vector3(1.4f, 2.8f, 1.4f);
            interactionCollider.isTrigger = true;

            root.AddComponent<NetworkIdentity>();
            HookPoint hookPoint = root.AddComponent<HookPoint>();
            AudioSource rescueAudioSource = root.AddComponent<AudioSource>();
            rescueAudioSource.playOnAwake = false;
            rescueAudioSource.spatialBlend = 1f;

            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            pole.transform.SetParent(root.transform, false);
            pole.transform.localScale = new Vector3(0.2f, 1.3f, 0.2f);
            pole.transform.localPosition = new Vector3(0f, 1.3f, 0f);
            pole.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.22f, 0.22f, 0.24f), "metal");

            GameObject spike = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spike.name = "Spike";
            spike.transform.SetParent(root.transform, false);
            spike.transform.localScale = new Vector3(0.08f, 0.08f, 0.5f);
            spike.transform.localPosition = new Vector3(0f, 1.7f, 0.3f);
            spike.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.34f, 0.34f, 0.36f), "monster_bone");

            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            indicator.name = "Indicator";
            indicator.transform.SetParent(root.transform, false);
            indicator.transform.localScale = new Vector3(0.24f, 0.24f, 0.24f);
            indicator.transform.localPosition = new Vector3(0f, 2.6f, 0f);
            indicator.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.7f, 0.08f, 0.08f), "monster_eye", true, new Color(0.38f, 0f, 0f));
            Object.DestroyImmediate(indicator.GetComponent<Collider>());

            Transform attachPoint = new GameObject("AttachPoint").transform;
            attachPoint.SetParent(root.transform, false);
            attachPoint.localPosition = new Vector3(0f, 1.7f, 0.45f);

            SetReference(hookPoint, "hookAttachPoint", attachPoint);
            SetReference(hookPoint, "indicatorRenderer", indicator.GetComponent<Renderer>());
            SetReference(hookPoint, "rescueAudioSource", rescueAudioSource);
            SetReference(hookPoint, "rescueClip", LoadAudioClipAsset(PickupClipPath));
            SetString(hookPoint, "interactionName", "Rescue");
            SetFloat(hookPoint, "holdDuration", 2f);

            return SavePrefab(root, HookPrefabPath);
        }

        private static GameObject BuildDoorPrefab()
        {
            GameObject root = new GameObject("Door");
            BoxCollider interactionCollider = root.AddComponent<BoxCollider>();
            interactionCollider.isTrigger = true;
            interactionCollider.center = new Vector3(0f, 1.58f, 0f);
            interactionCollider.size = new Vector3(6.1f, 3.3f, 2.35f);

            root.AddComponent<NetworkIdentity>();
            SimpleNetworkTransform doorTransform = root.AddComponent<SimpleNetworkTransform>();
            SetBool(doorTransform, "ownerAuthoritative", false);
            NetworkDoor door = root.AddComponent<NetworkDoor>();
            AudioSource audioSource = root.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.minDistance = 2f;
            audioSource.maxDistance = 16f;
            audioSource.volume = 0.56f;

            GameObject frameBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frameBase.name = "FrameBase";
            frameBase.transform.SetParent(root.transform, false);
            frameBase.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            frameBase.transform.localScale = new Vector3(5.1f, 0.12f, 0.38f);
            frameBase.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.12f, 0.12f, 0.14f), "door_frame");
            Object.DestroyImmediate(frameBase.GetComponent<Collider>());

            GameObject frameLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frameLeft.name = "FrameLeft";
            frameLeft.transform.SetParent(root.transform, false);
            frameLeft.transform.localPosition = new Vector3(-2.38f, 1.58f, 0f);
            frameLeft.transform.localScale = new Vector3(0.24f, 3.16f, 0.34f);
            frameLeft.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.18f, 0.18f, 0.2f), "door_frame");
            Object.DestroyImmediate(frameLeft.GetComponent<Collider>());

            GameObject frameRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frameRight.name = "FrameRight";
            frameRight.transform.SetParent(root.transform, false);
            frameRight.transform.localPosition = new Vector3(2.38f, 1.58f, 0f);
            frameRight.transform.localScale = new Vector3(0.24f, 3.16f, 0.34f);
            frameRight.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.18f, 0.18f, 0.2f), "door_frame");
            Object.DestroyImmediate(frameRight.GetComponent<Collider>());

            GameObject frameTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frameTop.name = "FrameTop";
            frameTop.transform.SetParent(root.transform, false);
            frameTop.transform.localPosition = new Vector3(0f, 3.08f, 0f);
            frameTop.transform.localScale = new Vector3(5.02f, 0.22f, 0.34f);
            frameTop.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.18f, 0.18f, 0.2f), "door_frame");
            Object.DestroyImmediate(frameTop.GetComponent<Collider>());

            GameObject leftLeafPivot = new GameObject("DoorLeafPivotLeft");
            leftLeafPivot.transform.SetParent(root.transform, false);
            leftLeafPivot.transform.localPosition = new Vector3(-2.12f, 1.52f, 0f);

            GameObject leftLeaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftLeaf.name = "DoorLeafLeft";
            leftLeaf.transform.SetParent(leftLeafPivot.transform, false);
            leftLeaf.transform.localPosition = new Vector3(1.02f, 0f, 0f);
            leftLeaf.transform.localScale = new Vector3(2.04f, 2.92f, 0.14f);
            leftLeaf.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.16f, 0.17f, 0.2f), "door_leaf");

            BoxCollider leftLeafCollider = leftLeaf.GetComponent<BoxCollider>();
            leftLeafCollider.center = Vector3.zero;
            leftLeafCollider.size = new Vector3(1f, 1f, 1f);

            UnityEngine.AI.NavMeshObstacle leftNavMeshObstacle = leftLeaf.AddComponent<UnityEngine.AI.NavMeshObstacle>();
            leftNavMeshObstacle.shape = UnityEngine.AI.NavMeshObstacleShape.Box;
            leftNavMeshObstacle.size = new Vector3(2.0f, 2.88f, 0.18f);
            leftNavMeshObstacle.center = Vector3.zero;
            leftNavMeshObstacle.carving = true;
            leftNavMeshObstacle.carveOnlyStationary = false;

            GameObject rightLeafPivot = new GameObject("DoorLeafPivotRight");
            rightLeafPivot.transform.SetParent(root.transform, false);
            rightLeafPivot.transform.localPosition = new Vector3(2.12f, 1.52f, 0f);

            GameObject rightLeaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightLeaf.name = "DoorLeafRight";
            rightLeaf.transform.SetParent(rightLeafPivot.transform, false);
            rightLeaf.transform.localPosition = new Vector3(-1.02f, 0f, 0f);
            rightLeaf.transform.localScale = new Vector3(2.04f, 2.92f, 0.14f);
            rightLeaf.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.16f, 0.17f, 0.2f), "door_leaf");

            BoxCollider rightLeafCollider = rightLeaf.GetComponent<BoxCollider>();
            rightLeafCollider.center = Vector3.zero;
            rightLeafCollider.size = new Vector3(1f, 1f, 1f);

            UnityEngine.AI.NavMeshObstacle rightNavMeshObstacle = rightLeaf.AddComponent<UnityEngine.AI.NavMeshObstacle>();
            rightNavMeshObstacle.shape = UnityEngine.AI.NavMeshObstacleShape.Box;
            rightNavMeshObstacle.size = new Vector3(2.0f, 2.88f, 0.18f);
            rightNavMeshObstacle.center = Vector3.zero;
            rightNavMeshObstacle.carving = true;
            rightNavMeshObstacle.carveOnlyStationary = false;

            GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            handle.name = "DoorHandle";
            handle.transform.SetParent(leftLeaf.transform, false);
            handle.transform.localScale = new Vector3(0.09f, 0.09f, 0.09f);
            handle.transform.localPosition = new Vector3(0.78f, -0.08f, 0.08f);
            handle.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.52f, 0.52f, 0.5f), "door_handle");
            Object.DestroyImmediate(handle.GetComponent<Collider>());

            GameObject handleRight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            handleRight.name = "DoorHandleRight";
            handleRight.transform.SetParent(rightLeaf.transform, false);
            handleRight.transform.localScale = new Vector3(0.09f, 0.09f, 0.09f);
            handleRight.transform.localPosition = new Vector3(-0.78f, -0.08f, 0.08f);
            handleRight.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.52f, 0.52f, 0.5f), "door_handle");
            Object.DestroyImmediate(handleRight.GetComponent<Collider>());

            SetReference(door, "doorVisual", leftLeafPivot.transform);
            SetReference(door, "secondaryDoorVisual", rightLeafPivot.transform);
            SetReference(door, "navObstacle", leftNavMeshObstacle);
            SetReference(door, "secondaryNavObstacle", rightNavMeshObstacle);
            SetReferenceArray(door, "blockingColliders", new Object[] { leftLeafCollider, rightLeafCollider });
            SetReference(door, "audioSource", audioSource);
            SetReference(door, "openClip", LoadAudioClipAsset(DoorOpenClipPath));
            SetReference(door, "closeClip", LoadAudioClipAsset(DoorCloseClipPath));
            SetReference(door, "lockedClip", LoadAudioClipAsset(LockedDoorClipPath));
            SetString(door, "interactionName", "Operate Door");
            SetFloat(door, "holdDuration", 0f);
            SetFloat(door, "serverInteractDistance", 4.4f);

            return SavePrefab(root, DoorPrefabPath);
        }

        private static GameObject BuildBatteryPrefab()
        {
            GameObject root = new GameObject("Battery");
            SphereCollider pickupCollider = root.AddComponent<SphereCollider>();
            pickupCollider.radius = 0.24f;
            pickupCollider.center = new Vector3(0f, 0.21f, 0f);

            Material batteryMaterial = CreateMaterial(new Color(0.31f, 0.33f, 0.35f), "metal");
            GameObject batteryVisual = InstantiateStyledModel(
                root.transform,
                BatteryModelAssetPath,
                "BatteryVisual",
                Vector3.zero,
                Quaternion.Euler(0f, 180f, 0f),
                0.42f,
                batteryMaterial,
                true,
                true);

            if (batteryVisual == null)
            {
                GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                fallback.name = "BatteryFallback";
                fallback.transform.SetParent(root.transform, false);
                fallback.transform.localScale = new Vector3(0.22f, 0.24f, 0.22f);
                fallback.transform.localPosition = new Vector3(0f, 0.22f, 0f);
                fallback.GetComponent<Renderer>().sharedMaterial = batteryMaterial;
                Object.DestroyImmediate(fallback.GetComponent<Collider>());
                batteryVisual = fallback;
            }

            Renderer primaryRenderer = batteryVisual.GetComponentInChildren<Renderer>();
            Renderer[] batteryRenderers = batteryVisual.GetComponentsInChildren<Renderer>(true);

            root.AddComponent<NetworkIdentity>();
            SimpleNetworkTransform batteryTransform = root.AddComponent<SimpleNetworkTransform>();
            SetBool(batteryTransform, "ownerAuthoritative", false);
            BatteryPickupTask batteryTask = root.AddComponent<BatteryPickupTask>();
            AudioSource audioSource = root.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.minDistance = 2f;
            audioSource.maxDistance = 14f;

            GameObject batteryGlowObject = new GameObject("BatteryGlow");
            batteryGlowObject.transform.SetParent(root.transform, false);
            batteryGlowObject.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            Light batteryGlow = batteryGlowObject.AddComponent<Light>();
            batteryGlow.type = LightType.Point;
            batteryGlow.range = 2.4f;
            batteryGlow.intensity = 0.28f;
            batteryGlow.color = new Color(0.9f, 0.78f, 0.18f);

            SetReference(batteryTask, "visualRenderer", primaryRenderer);
            SetReferenceArray(batteryTask, "visualRenderers", batteryRenderers);
            SetReference(batteryTask, "pickupCollider", pickupCollider);
            SetReference(batteryTask, "audioSource", audioSource);
            SetReference(batteryTask, "pickupClip", LoadAudioClipAsset(PickupClipPath));
            SetFloat(batteryTask, "rechargeSeconds", 90f);
            SetString(batteryTask, "interactionName", "Insert Battery");
            SetFloat(batteryTask, "holdDuration", 0f);

            return SavePrefab(root, BatteryPrefabPath);
        }

        private static GameObject BuildHideLockerPrefab()
        {
            GameObject root = new GameObject("HideLocker");
            BoxCollider interactionCollider = root.AddComponent<BoxCollider>();
            interactionCollider.isTrigger = true;
            interactionCollider.center = new Vector3(0f, 1.1f, 0f);
            interactionCollider.size = new Vector3(1.8f, 2.5f, 1.9f);

            root.AddComponent<NetworkIdentity>();
            HideLocker locker = root.AddComponent<HideLocker>();
            AudioSource audioSource = root.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.minDistance = 1.5f;
            audioSource.maxDistance = 8f;
            audioSource.volume = 0.2f;

            Material shellMaterial = CreateMaterial(new Color(0.14f, 0.16f, 0.18f), "metal");
            Material accentMaterial = CreateMaterial(new Color(0.32f, 0.05f, 0.05f), "metal");

            GameObject shell = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shell.name = "Shell";
            shell.transform.SetParent(root.transform, false);
            shell.transform.localPosition = new Vector3(0f, 1.1f, -0.05f);
            shell.transform.localScale = new Vector3(1.35f, 2.2f, 0.72f);
            shell.GetComponent<Renderer>().sharedMaterial = shellMaterial;
            Object.DestroyImmediate(shell.GetComponent<Collider>());

            GameObject leftDoor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftDoor.name = "LeftDoor";
            leftDoor.transform.SetParent(root.transform, false);
            leftDoor.transform.localPosition = new Vector3(-0.32f, 1.1f, 0.33f);
            leftDoor.transform.localScale = new Vector3(0.56f, 2.06f, 0.06f);
            leftDoor.GetComponent<Renderer>().sharedMaterial = shellMaterial;
            Object.DestroyImmediate(leftDoor.GetComponent<Collider>());

            GameObject rightDoor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightDoor.name = "RightDoor";
            rightDoor.transform.SetParent(root.transform, false);
            rightDoor.transform.localPosition = new Vector3(0.32f, 1.1f, 0.33f);
            rightDoor.transform.localScale = new Vector3(0.56f, 2.06f, 0.06f);
            rightDoor.GetComponent<Renderer>().sharedMaterial = shellMaterial;
            Object.DestroyImmediate(rightDoor.GetComponent<Collider>());

            GameObject warningStrip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            warningStrip.name = "WarningStrip";
            warningStrip.transform.SetParent(root.transform, false);
            warningStrip.transform.localPosition = new Vector3(0f, 1.92f, 0.34f);
            warningStrip.transform.localScale = new Vector3(1.08f, 0.12f, 0.02f);
            warningStrip.GetComponent<Renderer>().sharedMaterial = accentMaterial;
            Object.DestroyImmediate(warningStrip.GetComponent<Collider>());

            Transform hidePoint = new GameObject("HidePoint").transform;
            hidePoint.SetParent(root.transform, false);
            hidePoint.localPosition = new Vector3(0f, 0f, -0.18f);
            hidePoint.localRotation = Quaternion.identity;

            Transform exitPoint = new GameObject("ExitPoint").transform;
            exitPoint.SetParent(root.transform, false);
            exitPoint.localPosition = new Vector3(0f, 0f, 1.3f);
            exitPoint.localRotation = Quaternion.identity;

            SetReference(locker, "hidePoint", hidePoint);
            SetReference(locker, "exitPoint", exitPoint);
            SetReference(locker, "audioSource", audioSource);
            SetReference(locker, "enterClip", null);
            SetReference(locker, "exitClip", null);
            SetString(locker, "interactionName", "Hide");
            SetFloat(locker, "holdDuration", 0f);
            SetFloat(locker, "serverInteractDistance", 3.1f);

            return SavePrefab(root, HideLockerPrefabPath);
        }

        private static GameObject BuildHideCratePrefab()
        {
            GameObject root = new GameObject("HideCrate");
            BoxCollider interactionCollider = root.AddComponent<BoxCollider>();
            interactionCollider.isTrigger = true;
            interactionCollider.center = new Vector3(0f, 1.05f, 0.12f);
            interactionCollider.size = new Vector3(2.3f, 2.3f, 2.2f);

            root.AddComponent<NetworkIdentity>();
            HideLocker hideout = root.AddComponent<HideLocker>();
            AudioSource audioSource = root.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.minDistance = 1.5f;
            audioSource.maxDistance = 8f;
            audioSource.volume = 0.18f;

            Material crateMaterial = CreateMaterial(new Color(0.2f, 0.17f, 0.12f), "wood");
            Material tarpMaterial = CreateMaterial(new Color(0.07f, 0.09f, 0.1f), "metal");
            Material bloodMaterial = CreateMaterial(new Color(0.16f, 0.03f, 0.03f), "blood");

            GameObject basePallet = GameObject.CreatePrimitive(PrimitiveType.Cube);
            basePallet.name = "BasePallet";
            basePallet.transform.SetParent(root.transform, false);
            basePallet.transform.localPosition = new Vector3(0f, 0.18f, 0f);
            basePallet.transform.localScale = new Vector3(1.5f, 0.16f, 1.4f);
            basePallet.GetComponent<Renderer>().sharedMaterial = crateMaterial;
            Object.DestroyImmediate(basePallet.GetComponent<Collider>());

            GameObject leftStack = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftStack.name = "LeftStack";
            leftStack.transform.SetParent(root.transform, false);
            leftStack.transform.localPosition = new Vector3(-0.62f, 0.88f, -0.08f);
            leftStack.transform.localScale = new Vector3(0.32f, 1.38f, 1.36f);
            leftStack.GetComponent<Renderer>().sharedMaterial = crateMaterial;
            Object.DestroyImmediate(leftStack.GetComponent<Collider>());

            GameObject rightStack = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightStack.name = "RightStack";
            rightStack.transform.SetParent(root.transform, false);
            rightStack.transform.localPosition = new Vector3(0.62f, 0.88f, -0.08f);
            rightStack.transform.localScale = new Vector3(0.32f, 1.38f, 1.36f);
            rightStack.GetComponent<Renderer>().sharedMaterial = crateMaterial;
            Object.DestroyImmediate(rightStack.GetComponent<Collider>());

            GameObject rearStack = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rearStack.name = "RearStack";
            rearStack.transform.SetParent(root.transform, false);
            rearStack.transform.localPosition = new Vector3(0f, 0.92f, -0.62f);
            rearStack.transform.localScale = new Vector3(1.32f, 1.46f, 0.28f);
            rearStack.GetComponent<Renderer>().sharedMaterial = crateMaterial;
            Object.DestroyImmediate(rearStack.GetComponent<Collider>());

            GameObject tarpRoof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tarpRoof.name = "TarpRoof";
            tarpRoof.transform.SetParent(root.transform, false);
            tarpRoof.transform.localPosition = new Vector3(0f, 1.58f, -0.08f);
            tarpRoof.transform.localScale = new Vector3(1.4f, 0.1f, 1.32f);
            tarpRoof.GetComponent<Renderer>().sharedMaterial = tarpMaterial;
            Object.DestroyImmediate(tarpRoof.GetComponent<Collider>());

            GameObject bloodMark = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bloodMark.name = "BloodMark";
            bloodMark.transform.SetParent(root.transform, false);
            bloodMark.transform.localPosition = new Vector3(0.22f, 0.22f, 0.46f);
            bloodMark.transform.localScale = new Vector3(0.44f, 0.01f, 0.28f);
            bloodMark.GetComponent<Renderer>().sharedMaterial = bloodMaterial;
            Object.DestroyImmediate(bloodMark.GetComponent<Collider>());

            Transform hidePoint = new GameObject("HidePoint").transform;
            hidePoint.SetParent(root.transform, false);
            hidePoint.localPosition = new Vector3(0f, 0f, -0.16f);
            hidePoint.localRotation = Quaternion.identity;

            Transform exitPoint = new GameObject("ExitPoint").transform;
            exitPoint.SetParent(root.transform, false);
            exitPoint.localPosition = new Vector3(0f, 0f, 1.55f);
            exitPoint.localRotation = Quaternion.identity;

            SetReference(hideout, "hidePoint", hidePoint);
            SetReference(hideout, "exitPoint", exitPoint);
            SetReference(hideout, "audioSource", audioSource);
            SetReference(hideout, "enterClip", null);
            SetReference(hideout, "exitClip", null);
            SetString(hideout, "interactionName", "Hide In Supply Crate");
            SetFloat(hideout, "holdDuration", 0f);
            SetFloat(hideout, "serverInteractDistance", 3.2f);

            return SavePrefab(root, HideCratePrefabPath);
        }

        private static GameObject BuildHideCurtainPrefab()
        {
            GameObject root = new GameObject("HideCurtain");
            BoxCollider interactionCollider = root.AddComponent<BoxCollider>();
            interactionCollider.isTrigger = true;
            interactionCollider.center = new Vector3(0f, 1.05f, 0f);
            interactionCollider.size = new Vector3(2.4f, 2.4f, 2.6f);

            root.AddComponent<NetworkIdentity>();
            HideLocker hideout = root.AddComponent<HideLocker>();
            AudioSource audioSource = root.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.minDistance = 1.5f;
            audioSource.maxDistance = 8f;
            audioSource.volume = 0.18f;

            Material poleMaterial = CreateMaterial(new Color(0.24f, 0.26f, 0.28f), "metal");
            Material curtainMaterial = CreateMaterial(new Color(0.12f, 0.08f, 0.08f), "fabric");
            Material bloodMaterial = CreateMaterial(new Color(0.17f, 0.03f, 0.03f), "blood");

            for (int index = 0; index < 4; index++)
            {
                Vector3 polePosition = new Vector3(index < 2 ? -0.78f : 0.78f, 1.05f, index % 2 == 0 ? -0.84f : 0.84f);
                GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pole.name = $"Pole_{index}";
                pole.transform.SetParent(root.transform, false);
                pole.transform.localPosition = polePosition;
                pole.transform.localScale = new Vector3(0.04f, 1.05f, 0.04f);
                pole.GetComponent<Renderer>().sharedMaterial = poleMaterial;
                Object.DestroyImmediate(pole.GetComponent<Collider>());
            }

            GameObject backCurtain = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backCurtain.name = "BackCurtain";
            backCurtain.transform.SetParent(root.transform, false);
            backCurtain.transform.localPosition = new Vector3(0f, 1.2f, -0.84f);
            backCurtain.transform.localScale = new Vector3(1.72f, 1.92f, 0.08f);
            backCurtain.GetComponent<Renderer>().sharedMaterial = curtainMaterial;
            Object.DestroyImmediate(backCurtain.GetComponent<Collider>());

            GameObject leftCurtain = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftCurtain.name = "LeftCurtain";
            leftCurtain.transform.SetParent(root.transform, false);
            leftCurtain.transform.localPosition = new Vector3(-0.82f, 1.2f, -0.02f);
            leftCurtain.transform.localScale = new Vector3(0.08f, 1.92f, 1.56f);
            leftCurtain.GetComponent<Renderer>().sharedMaterial = curtainMaterial;
            Object.DestroyImmediate(leftCurtain.GetComponent<Collider>());

            GameObject rightCurtain = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightCurtain.name = "RightCurtain";
            rightCurtain.transform.SetParent(root.transform, false);
            rightCurtain.transform.localPosition = new Vector3(0.82f, 1.2f, -0.02f);
            rightCurtain.transform.localScale = new Vector3(0.08f, 1.92f, 1.56f);
            rightCurtain.GetComponent<Renderer>().sharedMaterial = curtainMaterial;
            Object.DestroyImmediate(rightCurtain.GetComponent<Collider>());

            GameObject bloodStreak = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bloodStreak.name = "BloodStreak";
            bloodStreak.transform.SetParent(root.transform, false);
            bloodStreak.transform.localPosition = new Vector3(-0.46f, 0.9f, -0.79f);
            bloodStreak.transform.localScale = new Vector3(0.16f, 0.95f, 0.01f);
            bloodStreak.GetComponent<Renderer>().sharedMaterial = bloodMaterial;
            Object.DestroyImmediate(bloodStreak.GetComponent<Collider>());

            GameObject gurney = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gurney.name = "Gurney";
            gurney.transform.SetParent(root.transform, false);
            gurney.transform.localPosition = new Vector3(0f, 0.36f, -0.18f);
            gurney.transform.localScale = new Vector3(1.16f, 0.18f, 1.92f);
            gurney.GetComponent<Renderer>().sharedMaterial = poleMaterial;
            Object.DestroyImmediate(gurney.GetComponent<Collider>());

            Transform hidePoint = new GameObject("HidePoint").transform;
            hidePoint.SetParent(root.transform, false);
            hidePoint.localPosition = new Vector3(0f, 0f, 0.08f);
            hidePoint.localRotation = Quaternion.identity;

            Transform exitPoint = new GameObject("ExitPoint").transform;
            exitPoint.SetParent(root.transform, false);
            exitPoint.localPosition = new Vector3(0f, 0f, 1.42f);
            exitPoint.localRotation = Quaternion.identity;

            SetReference(hideout, "hidePoint", hidePoint);
            SetReference(hideout, "exitPoint", exitPoint);
            SetReference(hideout, "audioSource", audioSource);
            SetReference(hideout, "enterClip", null);
            SetReference(hideout, "exitClip", null);
            SetString(hideout, "interactionName", "Slip Behind Curtain");
            SetFloat(hideout, "holdDuration", 0f);
            SetFloat(hideout, "serverInteractDistance", 3.2f);

            return SavePrefab(root, HideCurtainPrefabPath);
        }
        private static void CreateMainMenuScene(PrototypeAssets assets)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "MainMenu";

            CreateDirectionalLight(new Color(0.78f, 0.82f, 0.9f), 0.18f, new Vector3(42f, -30f, 0f));
            EnsureEventSystem();

            GameObject managerObject = new GameObject("NetworkManager");
            HorrorNetworkManager networkManager = managerObject.AddComponent<HorrorNetworkManager>();
            KcpTransport transport = managerObject.AddComponent<KcpTransport>();
            managerObject.AddComponent<CommandLineAutoConnect>();
            networkManager.transport = transport;
            networkManager.playerPrefab = assets.PlayerPrefab;
            networkManager.spawnPrefabs = assets.ToSpawnableList();

            SetString(networkManager, "menuSceneName", "MainMenu");
            SetString(networkManager, "lobbySceneName", "Lobby");
            SetString(networkManager, "gameplaySceneName", "HospitalLevel");
            SetInt(networkManager, "maxPlayers", 4);

            Canvas canvas = CreateCanvas("MainMenuCanvas");
            RectTransform rootRect = canvas.GetComponent<RectTransform>();
            MainMenuUI menuUI = canvas.gameObject.AddComponent<MainMenuUI>();

            Image background = CreatePanel(rootRect, "Background", new Color(0.015f, 0.016f, 0.018f, 1f));
            StretchToRect(background.rectTransform, Vector2.zero, Vector2.one);
            Image topFog = CreatePanel(rootRect, "TopFog", new Color(0.05f, 0.08f, 0.09f, 0.14f));
            StretchToRect(topFog.rectTransform, new Vector2(0f, 0.58f), new Vector2(1f, 1f));
            Image bottomVoid = CreatePanel(rootRect, "BottomVoid", new Color(0f, 0f, 0f, 0.9f));
            StretchToRect(bottomVoid.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.18f));
            Image verticalCut = CreatePanel(rootRect, "VerticalCut", new Color(0.48f, 0.05f, 0.06f, 0.32f));
            StretchToRect(verticalCut.rectTransform, new Vector2(0.405f, 0.08f), new Vector2(0.416f, 0.95f));
            verticalCut.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -5.5f);

            Image leftField = CreatePanel(rootRect, "LeftField", new Color(0f, 0f, 0f, 0.46f));
            StretchToRect(leftField.rectTransform, new Vector2(0.03f, 0.08f), new Vector2(0.59f, 0.92f));

            Image feedFrame = CreatePanel(rootRect, "FeedFrame", new Color(0.04f, 0.045f, 0.05f, 0.94f));
            StretchToRect(feedFrame.rectTransform, new Vector2(0.09f, 0.28f), new Vector2(0.52f, 0.78f));
            Image feedGlass = CreatePanel(rootRect, "FeedGlass", new Color(0.06f, 0.11f, 0.12f, 0.45f));
            StretchToRect(feedGlass.rectTransform, new Vector2(0.1f, 0.295f), new Vector2(0.51f, 0.765f));
            Image scanlineA = CreatePanel(feedFrame.transform, "ScanlineA", new Color(0.82f, 0.88f, 0.92f, 0.04f));
            StretchToRect(scanlineA.rectTransform, new Vector2(0.05f, 0.72f), new Vector2(0.95f, 0.725f));
            Image scanlineB = CreatePanel(feedFrame.transform, "ScanlineB", new Color(0.82f, 0.88f, 0.92f, 0.03f));
            StretchToRect(scanlineB.rectTransform, new Vector2(0.05f, 0.38f), new Vector2(0.95f, 0.384f));
            Image feedMaskLeft = CreatePanel(feedFrame.transform, "FeedMaskLeft", new Color(0f, 0f, 0f, 0.46f));
            StretchToRect(feedMaskLeft.rectTransform, new Vector2(0.1f, 0.12f), new Vector2(0.2f, 0.92f));
            Image feedMaskRight = CreatePanel(feedFrame.transform, "FeedMaskRight", new Color(0f, 0f, 0f, 0.42f));
            StretchToRect(feedMaskRight.rectTransform, new Vector2(0.74f, 0.08f), new Vector2(0.9f, 0.92f));
            Image monsterSilhouette = CreatePanel(feedFrame.transform, "MonsterSilhouette", new Color(0.05f, 0.02f, 0.02f, 0.86f));
            StretchToRect(monsterSilhouette.rectTransform, new Vector2(0.48f, 0.11f), new Vector2(0.72f, 0.78f));
            Image survivorSilhouette = CreatePanel(feedFrame.transform, "SurvivorSilhouette", new Color(0.03f, 0.04f, 0.05f, 0.92f));
            StretchToRect(survivorSilhouette.rectTransform, new Vector2(0.22f, 0.18f), new Vector2(0.33f, 0.66f));
            Image feedWarning = CreatePanel(feedFrame.transform, "FeedWarning", new Color(0.5f, 0.05f, 0.06f, 0.18f));
            StretchToRect(feedWarning.rectTransform, new Vector2(0.58f, 0.15f), new Vector2(0.82f, 0.2f));

            Text feedLabel = CreateText(rootRect, "FeedLabel", "SURVEILLANCE FEED / BLOCK C", 15, TextAnchor.MiddleLeft);
            feedLabel.color = new Color(0.16f, 0.74f, 0.79f, 0.96f);
            StretchToRect(feedLabel.rectTransform, new Vector2(0.1f, 0.785f), new Vector2(0.38f, 0.82f));

            Text title = CreateText(rootRect, "Title", "ASYLUM", 88, TextAnchor.LowerLeft);
            title.color = new Color(0.95f, 0.91f, 0.87f);
            StretchToRect(title.rectTransform, new Vector2(0.08f, 0.11f), new Vector2(0.42f, 0.28f));

            Text subtitle = CreateText(rootRect, "Subtitle", "CONTAINMENT WATCH // NIGHT INSERTION", 22, TextAnchor.MiddleLeft);
            subtitle.color = new Color(0.19f, 0.68f, 0.74f, 0.94f);
            StretchToRect(subtitle.rectTransform, new Vector2(0.085f, 0.085f), new Vector2(0.42f, 0.12f));

            Image briefingPanel = CreatePanel(rootRect, "BriefingPanel", new Color(0.04f, 0.043f, 0.048f, 0.96f));
            StretchToRect(briefingPanel.rectTransform, new Vector2(0.08f, 0.02f), new Vector2(0.54f, 0.11f));
            Image briefingAccent = CreatePanel(rootRect, "BriefingAccent", new Color(0.48f, 0.05f, 0.06f, 0.94f));
            StretchToRect(briefingAccent.rectTransform, new Vector2(0.08f, 0.105f), new Vector2(0.54f, 0.11f));
            Text flavor = CreateText(rootRect, "Flavor", "Restore auxiliary power, recover the access card and force the north gate.\nThe creature reacts to sight, impact noise and exposed light. Lose line-of-sight before it commits.", 15, TextAnchor.UpperLeft);
            flavor.color = new Color(0.84f, 0.8f, 0.77f, 0.9f);
            StretchToRect(flavor.rectTransform, new Vector2(0.095f, 0.025f), new Vector2(0.53f, 0.1f));

            Image operationsPanel = CreatePanel(rootRect, "OperationsPanel", new Color(0.03f, 0.035f, 0.04f, 0.97f));
            StretchToRect(operationsPanel.rectTransform, new Vector2(0.63f, 0.08f), new Vector2(0.95f, 0.93f));
            Image operationsAccent = CreatePanel(rootRect, "OperationsAccent", new Color(0.18f, 0.66f, 0.74f, 0.88f));
            StretchToRect(operationsAccent.rectTransform, new Vector2(0.63f, 0.91f), new Vector2(0.95f, 0.93f));
            Image operationsSubAccent = CreatePanel(rootRect, "OperationsSubAccent", new Color(0.48f, 0.05f, 0.06f, 0.76f));
            StretchToRect(operationsSubAccent.rectTransform, new Vector2(0.63f, 0.515f), new Vector2(0.95f, 0.52f));

            Text boardTitle = CreateText(rootRect, "BoardTitle", "OPERATIONS CONSOLE", 28, TextAnchor.MiddleLeft);
            boardTitle.color = new Color(0.94f, 0.9f, 0.86f);
            StretchToRect(boardTitle.rectTransform, new Vector2(0.66f, 0.84f), new Vector2(0.92f, 0.89f));

            Text boardHint = CreateText(rootRect, "BoardHint", "Host launches the listen-server. Friends join by LAN or public internet endpoint shown below.", 13, TextAnchor.UpperLeft);
            boardHint.color = new Color(0.76f, 0.72f, 0.68f, 0.85f);
            StretchToRect(boardHint.rectTransform, new Vector2(0.66f, 0.77f), new Vector2(0.92f, 0.83f));

            Text addressLabel = CreateText(rootRect, "AddressLabel", "JOIN ADDRESS", 13, TextAnchor.MiddleLeft);
            addressLabel.color = new Color(0.74f, 0.71f, 0.67f, 0.82f);
            StretchToRect(addressLabel.rectTransform, new Vector2(0.66f, 0.695f), new Vector2(0.92f, 0.73f));
            InputField addressInput = CreateInputField(rootRect, "AddressInput", "localhost or public ip", "localhost");
            StretchToRect(addressInput.GetComponent<RectTransform>(), new Vector2(0.66f, 0.64f), new Vector2(0.92f, 0.69f));

            Text portLabel = CreateText(rootRect, "PortLabel", "UDP PORT", 13, TextAnchor.MiddleLeft);
            portLabel.color = new Color(0.74f, 0.71f, 0.67f, 0.82f);
            StretchToRect(portLabel.rectTransform, new Vector2(0.66f, 0.58f), new Vector2(0.92f, 0.615f));
            InputField portInput = CreateInputField(rootRect, "PortInput", "7777", "7777");
            StretchToRect(portInput.GetComponent<RectTransform>(), new Vector2(0.66f, 0.525f), new Vector2(0.92f, 0.575f));

            Button hostButton = CreateButton(rootRect, "HostButton", "HOST NIGHT SHIFT");
            StretchToRect(hostButton.GetComponent<RectTransform>(), new Vector2(0.665f, 0.435f), new Vector2(0.92f, 0.502f));
            UnityEventTools.AddPersistentListener(hostButton.onClick, menuUI.OnHostClicked);

            Button joinButton = CreateButton(rootRect, "JoinButton", "JOIN INSERTION");
            StretchToRect(joinButton.GetComponent<RectTransform>(), new Vector2(0.665f, 0.35f), new Vector2(0.92f, 0.417f));
            UnityEventTools.AddPersistentListener(joinButton.onClick, menuUI.OnJoinClicked);

            Button settingsButton = CreateButton(rootRect, "SettingsButton", "FIELD SETTINGS");
            StretchToRect(settingsButton.GetComponent<RectTransform>(), new Vector2(0.665f, 0.265f), new Vector2(0.92f, 0.332f));
            UnityEventTools.AddPersistentListener(settingsButton.onClick, menuUI.OnToggleSettingsClicked);

            Button quitButton = CreateButton(rootRect, "QuitButton", "TERMINATE FEED");
            StretchToRect(quitButton.GetComponent<RectTransform>(), new Vector2(0.665f, 0.18f), new Vector2(0.92f, 0.247f));
            UnityEventTools.AddPersistentListener(quitButton.onClick, menuUI.OnQuitClicked);

            Text settingsSummaryText = CreateText(rootRect, "SettingsSummary", string.Empty, 13, TextAnchor.UpperLeft);
            settingsSummaryText.color = new Color(0.76f, 0.72f, 0.68f, 0.84f);
            StretchToRect(settingsSummaryText.rectTransform, new Vector2(0.66f, 0.12f), new Vector2(0.92f, 0.165f));

            Text statusText = CreateText(rootRect, "StatusText", "Waiting for network status...", 14, TextAnchor.UpperLeft);
            statusText.color = new Color(0.92f, 0.88f, 0.82f, 0.96f);
            StretchToRect(statusText.rectTransform, new Vector2(0.66f, 0.02f), new Vector2(0.93f, 0.115f));

            GameObject settingsPanel = CreatePanel(rootRect, "SettingsPanel", new Color(0.05f, 0.055f, 0.06f, 0.98f)).gameObject;
            StretchToRect(settingsPanel.GetComponent<RectTransform>(), new Vector2(0.1f, 0.16f), new Vector2(0.54f, 0.55f));
            Image settingsAccent = CreatePanel(settingsPanel.transform, "SettingsAccent", new Color(0.18f, 0.66f, 0.74f, 0.9f));
            StretchToRect(settingsAccent.rectTransform, new Vector2(0f, 0.94f), new Vector2(1f, 1f));

            Text settingsTitle = CreateText(settingsPanel.transform, "SettingsTitle", "FIELD SETTINGS", 24, TextAnchor.MiddleLeft);
            settingsTitle.color = new Color(0.94f, 0.9f, 0.86f);
            StretchToRect(settingsTitle.rectTransform, new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.92f));

            Text volumeLabel = CreateText(settingsPanel.transform, "VolumeLabel", "MASTER VOLUME", 13, TextAnchor.MiddleLeft);
            volumeLabel.color = new Color(0.78f, 0.74f, 0.7f, 0.84f);
            StretchToRect(volumeLabel.rectTransform, new Vector2(0.06f, 0.64f), new Vector2(0.94f, 0.74f));
            Slider masterVolumeSlider = CreateSlider(settingsPanel.transform, "MasterVolumeSlider");
            StretchToRect(masterVolumeSlider.GetComponent<RectTransform>(), new Vector2(0.06f, 0.56f), new Vector2(0.94f, 0.64f));
            UnityEventTools.AddPersistentListener(masterVolumeSlider.onValueChanged, menuUI.OnMasterVolumeChanged);

            Text sensitivityLabel = CreateText(settingsPanel.transform, "SensitivityLabel", "LOOK SENSITIVITY", 13, TextAnchor.MiddleLeft);
            sensitivityLabel.color = new Color(0.78f, 0.74f, 0.7f, 0.84f);
            StretchToRect(sensitivityLabel.rectTransform, new Vector2(0.06f, 0.42f), new Vector2(0.94f, 0.52f));
            Slider sensitivitySlider = CreateSlider(settingsPanel.transform, "SensitivitySlider");
            StretchToRect(sensitivitySlider.GetComponent<RectTransform>(), new Vector2(0.06f, 0.34f), new Vector2(0.94f, 0.42f));
            UnityEventTools.AddPersistentListener(sensitivitySlider.onValueChanged, menuUI.OnSensitivityChanged);

            Text fovLabel = CreateText(settingsPanel.transform, "FovLabel", "FIELD OF VIEW", 13, TextAnchor.MiddleLeft);
            fovLabel.color = new Color(0.78f, 0.74f, 0.7f, 0.84f);
            StretchToRect(fovLabel.rectTransform, new Vector2(0.06f, 0.2f), new Vector2(0.56f, 0.3f));
            Slider fovSlider = CreateSlider(settingsPanel.transform, "FovSlider");
            StretchToRect(fovSlider.GetComponent<RectTransform>(), new Vector2(0.06f, 0.12f), new Vector2(0.56f, 0.2f));
            fovSlider.minValue = 60f;
            fovSlider.maxValue = 100f;
            UnityEventTools.AddPersistentListener(fovSlider.onValueChanged, menuUI.OnFovChanged);

            Toggle subtitlesToggle = CreateToggle(settingsPanel.transform, "SubtitlesToggle", "SUBTITLES");
            StretchToRect(subtitlesToggle.GetComponent<RectTransform>(), new Vector2(0.64f, 0.15f), new Vector2(0.94f, 0.3f));
            UnityEventTools.AddPersistentListener(subtitlesToggle.onValueChanged, menuUI.OnSubtitlesChanged);

            settingsPanel.SetActive(false);

            SetReference(menuUI, "addressInput", addressInput);
            SetReference(menuUI, "portInput", portInput);
            SetReference(menuUI, "statusText", statusText);
            SetReference(menuUI, "settingsPanel", settingsPanel);
            SetReference(menuUI, "masterVolumeSlider", masterVolumeSlider);
            SetReference(menuUI, "sensitivitySlider", sensitivitySlider);
            SetReference(menuUI, "fovSlider", fovSlider);
            SetReference(menuUI, "subtitlesToggle", subtitlesToggle);
            SetReference(menuUI, "settingsSummaryText", settingsSummaryText);

            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
        }

        private static void CreateLobbyScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Lobby";

            CreateDirectionalLight(new Color(0.56f, 0.58f, 0.64f), 0.12f, new Vector3(34f, -28f, 0f));
            EnsureEventSystem();

            GameObject lobbyStateObject = new GameObject("LobbyState");
            lobbyStateObject.AddComponent<NetworkIdentity>();
            lobbyStateObject.AddComponent<LobbyState>();

            GameObject cameraObject = new GameObject("LobbyCamera");
            cameraObject.transform.position = new Vector3(0f, 4.4f, -10.5f);
            cameraObject.transform.rotation = Quaternion.Euler(14f, 0f, 0f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.012f, 0.013f, 0.016f);
            camera.fieldOfView = 56f;
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = 120f;
            camera.allowHDR = true;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<LobbyFallbackCamera>();

            Transform stageRoot = new GameObject("StagingRoom").transform;
            Material concreteMaterial = CreateMaterial(new Color(0.13f, 0.14f, 0.15f), "floor");
            Material wallMaterial = CreateMaterial(new Color(0.08f, 0.085f, 0.095f), "wall");
            Material steelMaterial = CreateMaterial(new Color(0.22f, 0.24f, 0.26f), "metal");

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "StageFloor";
            floor.transform.SetParent(stageRoot, false);
            floor.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(18f, 1f, 12f);
            floor.GetComponent<Renderer>().sharedMaterial = concreteMaterial;

            GameObject rearWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rearWall.name = "RearWall";
            rearWall.transform.SetParent(stageRoot, false);
            rearWall.transform.localPosition = new Vector3(0f, 2.2f, 5.7f);
            rearWall.transform.localScale = new Vector3(18f, 5.4f, 0.5f);
            rearWall.GetComponent<Renderer>().sharedMaterial = wallMaterial;

            GameObject leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftWall.name = "LeftWall";
            leftWall.transform.SetParent(stageRoot, false);
            leftWall.transform.localPosition = new Vector3(-8.7f, 2.2f, 0f);
            leftWall.transform.localScale = new Vector3(0.5f, 5.4f, 12f);
            leftWall.GetComponent<Renderer>().sharedMaterial = wallMaterial;

            GameObject rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightWall.name = "RightWall";
            rightWall.transform.SetParent(stageRoot, false);
            rightWall.transform.localPosition = new Vector3(8.7f, 2.2f, 0f);
            rightWall.transform.localScale = new Vector3(0.5f, 5.4f, 12f);
            rightWall.GetComponent<Renderer>().sharedMaterial = wallMaterial;

            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Ceiling";
            ceiling.transform.SetParent(stageRoot, false);
            ceiling.transform.localPosition = new Vector3(0f, 5.1f, 0f);
            ceiling.transform.localScale = new Vector3(18f, 0.4f, 12f);
            ceiling.GetComponent<Renderer>().sharedMaterial = steelMaterial;

            CreateLobbyStartPositions(stageRoot);

            for (int index = 0; index < 4; index++)
            {
                float x = index < 2 ? -6.2f : 6.2f;
                float z = index % 2 == 0 ? -3.8f : 1.8f;
                GameObject barrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
                barrier.name = $"StagingBarrier_{index}";
                barrier.transform.SetParent(stageRoot, false);
                barrier.transform.localPosition = new Vector3(x, 0.8f, z);
                barrier.transform.localScale = new Vector3(0.45f, 1.6f, 2.4f);
                barrier.GetComponent<Renderer>().sharedMaterial = steelMaterial;
            }

            GameObject centralTable = InstantiateStyledModel(
                stageRoot,
                HomeTableAssetPath,
                "BriefingTable",
                new Vector3(0f, 0f, -0.2f),
                Quaternion.identity,
                1.15f,
                CreateTexturedMaterial(HomeInteriorTextureAssetPath, new Color(0.55f, 0.55f, 0.55f), "wood"),
                true,
                true);

            if (centralTable == null)
            {
                centralTable = GameObject.CreatePrimitive(PrimitiveType.Cube);
                centralTable.name = "BriefingTable";
                centralTable.transform.SetParent(stageRoot, false);
                centralTable.transform.localPosition = new Vector3(0f, 0.74f, -0.2f);
                centralTable.transform.localScale = new Vector3(2.8f, 0.12f, 1.4f);
                centralTable.GetComponent<Renderer>().sharedMaterial = steelMaterial;
            }

            InstantiateStyledModel(
                stageRoot,
                BatteryModelAssetPath,
                "StageBattery",
                new Vector3(-0.62f, 0.78f, -0.1f),
                Quaternion.Euler(0f, 28f, 0f),
                0.22f,
                CreateMaterial(new Color(0.22f, 0.24f, 0.26f), "metal"),
                true,
                true);
            InstantiateStyledModel(
                stageRoot,
                DebrisGasCanAssetPath,
                "StageGasCan",
                new Vector3(0.58f, 0.78f, 0.08f),
                Quaternion.Euler(0f, -18f, 0f),
                0.52f,
                CreateMaterial(new Color(0.4f, 0.08f, 0.07f), "metal"),
                true,
                true);
            InstantiateStyledModel(
                stageRoot,
                KenneyComputerScreenAssetPath,
                "StageScreen",
                new Vector3(0f, 0.82f, -0.42f),
                Quaternion.Euler(0f, 180f, 0f),
                0.46f,
                CreateMaterial(new Color(0.12f, 0.12f, 0.14f), "metal"),
                true,
                true);

            for (int index = 0; index < 3; index++)
            {
                float offset = -4.2f + index * 4.2f;
                GameObject plinth = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plinth.name = $"DisplayPlinth_{index}";
                plinth.transform.SetParent(stageRoot, false);
                plinth.transform.localPosition = new Vector3(offset, 0.34f, 3.2f);
                plinth.transform.localScale = new Vector3(1.35f, 0.7f, 1.2f);
                plinth.GetComponent<Renderer>().sharedMaterial = steelMaterial;

                InstantiateStyledModel(
                    stageRoot,
                    PlayerModelAssetPath,
                    $"SurvivorDisplay_{index}",
                    new Vector3(offset, 0.7f, 3.2f),
                    Quaternion.identity,
                    1.82f,
                    CreateMaterial(new Color(0.19f, 0.22f, 0.24f), "player_jacket"),
                    true,
                    true);
            }

            for (int index = 0; index < 2; index++)
            {
                float x = index == 0 ? -5.8f : 5.8f;
                GameObject debrisStack = GameObject.CreatePrimitive(PrimitiveType.Cube);
                debrisStack.name = $"DebrisStack_{index}";
                debrisStack.transform.SetParent(stageRoot, false);
                debrisStack.transform.localPosition = new Vector3(x, 0.8f, -2.9f);
                debrisStack.transform.localScale = new Vector3(1.2f, 1.6f, 1.1f);
                debrisStack.GetComponent<Renderer>().sharedMaterial = wallMaterial;
            }

            Light overheadLight = new GameObject("OverheadLight").AddComponent<Light>();
            overheadLight.transform.SetParent(stageRoot, false);
            overheadLight.transform.localPosition = new Vector3(0f, 3.8f, -0.3f);
            overheadLight.type = LightType.Spot;
            overheadLight.color = new Color(0.88f, 0.82f, 0.74f);
            overheadLight.intensity = 5.5f;
            overheadLight.range = 18f;
            overheadLight.spotAngle = 78f;
            overheadLight.shadows = LightShadows.Soft;

            Light redAccent = new GameObject("RedAccentLight").AddComponent<Light>();
            redAccent.transform.SetParent(stageRoot, false);
            redAccent.transform.localPosition = new Vector3(0f, 2.1f, 4.1f);
            redAccent.type = LightType.Point;
            redAccent.color = new Color(0.65f, 0.08f, 0.08f);
            redAccent.intensity = 1.6f;
            redAccent.range = 9f;

            Canvas canvas = CreateCanvas("LobbyCanvas");
            RectTransform rootRect = canvas.GetComponent<RectTransform>();
            Image background = CreatePanel(rootRect, "Background", new Color(0.008f, 0.01f, 0.012f, 0.2f));
            StretchToRect(background.rectTransform, Vector2.zero, Vector2.one);
            LobbyUI lobbyUI = canvas.gameObject.AddComponent<LobbyUI>();

            Image redTint = CreatePanel(rootRect, "RedTint", new Color(0.16f, 0.02f, 0.03f, 0.14f));
            StretchToRect(redTint.rectTransform, Vector2.zero, Vector2.one);
            Image vignetteTop = CreatePanel(rootRect, "VignetteTop", new Color(0f, 0f, 0f, 0.52f));
            StretchToRect(vignetteTop.rectTransform, new Vector2(0f, 0.82f), new Vector2(1f, 1f));
            Image vignetteBottom = CreatePanel(rootRect, "VignetteBottom", new Color(0f, 0f, 0f, 0.72f));
            StretchToRect(vignetteBottom.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.24f));

            Image leftInfo = CreatePanel(rootRect, "BriefingCard", new Color(0f, 0f, 0f, 0.5f));
            StretchToRect(leftInfo.rectTransform, new Vector2(0.03f, 0.05f), new Vector2(0.31f, 0.34f));
            Image leftAccent = CreatePanel(rootRect, "BriefingAccent", new Color(0.55f, 0.06f, 0.08f, 0.82f));
            StretchToRect(leftAccent.rectTransform, new Vector2(0.03f, 0.33f), new Vector2(0.31f, 0.34f));

            Text title = CreateText(rootRect, "LobbyTitle", "STAGING CHAMBER", 40, TextAnchor.LowerLeft);
            title.color = new Color(0.95f, 0.91f, 0.84f);
            StretchToRect(title.rectTransform, new Vector2(0.05f, 0.25f), new Vector2(0.29f, 0.33f));
            Text summary = CreateText(rootRect, "LobbySummary", "Walk the prep room in third person, brief the team and mark ready.\nHost opens the ward only when everyone confirms.\nYou cannot leave the staging boundary before deployment.", 17, TextAnchor.UpperLeft);
            summary.color = new Color(0.84f, 0.82f, 0.77f, 0.92f);
            StretchToRect(summary.rectTransform, new Vector2(0.05f, 0.13f), new Vector2(0.29f, 0.25f));

            Text briefingText = CreateText(rootRect, "BriefingText", "BRIEFING", 18, TextAnchor.UpperLeft);
            briefingText.color = new Color(0.94f, 0.86f, 0.78f, 0.95f);
            StretchToRect(briefingText.rectTransform, new Vector2(0.05f, 0.055f), new Vector2(0.29f, 0.12f));

            Image rosterBoard = CreatePanel(rootRect, "RosterBoard", new Color(0.03f, 0.036f, 0.04f, 0.94f));
            StretchToRect(rosterBoard.rectTransform, new Vector2(0.71f, 0.05f), new Vector2(0.97f, 0.42f));
            Image rosterAccent = CreatePanel(rootRect, "RosterAccent", new Color(0.55f, 0.06f, 0.08f, 0.9f));
            StretchToRect(rosterAccent.rectTransform, new Vector2(0.71f, 0.41f), new Vector2(0.97f, 0.42f));

            Text rosterTitle = CreateText(rootRect, "RosterTitle", "SURVIVOR ROSTER", 24, TextAnchor.MiddleLeft);
            rosterTitle.color = new Color(0.95f, 0.9f, 0.84f);
            StretchToRect(rosterTitle.rectTransform, new Vector2(0.73f, 0.36f), new Vector2(0.95f, 0.41f));

            Text playersText = CreateText(rootRect, "PlayersText", "Survivors in staging: 0/4", 22, TextAnchor.UpperLeft);
            playersText.color = new Color(0.9f, 0.9f, 0.94f);
            StretchToRect(playersText.rectTransform, new Vector2(0.73f, 0.27f), new Vector2(0.95f, 0.35f));

            Text infoText = CreateText(rootRect, "InfoText", "Awaiting ready confirmation...", 18, TextAnchor.UpperLeft);
            infoText.color = new Color(0.88f, 0.6f, 0.58f, 0.95f);
            StretchToRect(infoText.rectTransform, new Vector2(0.73f, 0.22f), new Vector2(0.95f, 0.27f));

            Text rosterText = CreateText(rootRect, "RosterText", "Scanning roster...", 20, TextAnchor.UpperLeft);
            rosterText.color = new Color(0.9f, 0.9f, 0.94f, 0.96f);
            StretchToRect(rosterText.rectTransform, new Vector2(0.73f, 0.12f), new Vector2(0.95f, 0.21f));

            Button readyButton = CreateButton(rootRect, "ReadyButton", "READY UP");
            StretchToRect(readyButton.GetComponent<RectTransform>(), new Vector2(0.36f, 0.05f), new Vector2(0.5f, 0.11f));
            UnityEventTools.AddPersistentListener(readyButton.onClick, lobbyUI.OnReadyClicked);

            Button startButton = CreateButton(rootRect, "StartButton", "OPEN WARD");
            StretchToRect(startButton.GetComponent<RectTransform>(), new Vector2(0.52f, 0.05f), new Vector2(0.66f, 0.11f));
            UnityEventTools.AddPersistentListener(startButton.onClick, lobbyUI.OnStartGameClicked);

            Button leaveButton = CreateButton(rootRect, "LeaveButton", "LEAVE LOBBY");
            StretchToRect(leaveButton.GetComponent<RectTransform>(), new Vector2(0.68f, 0.05f), new Vector2(0.82f, 0.11f));
            UnityEventTools.AddPersistentListener(leaveButton.onClick, lobbyUI.OnLeaveClicked);

            SetReference(lobbyUI, "playersText", playersText);
            SetReference(lobbyUI, "infoText", infoText);
            SetReference(lobbyUI, "rosterText", rosterText);
            SetReference(lobbyUI, "briefingText", briefingText);
            SetReference(lobbyUI, "startGameButton", startButton);
            SetReference(lobbyUI, "readyButton", readyButton);
            SetReference(lobbyUI, "readyButtonLabel", readyButton.GetComponentInChildren<Text>());
            SetReference(lobbyUI, "leaveButton", leaveButton);
            Text postMatchText = CreateText(rootRect, "PostMatchText", "LAST OUTCOME\nNo previous shift record.", 16, TextAnchor.UpperLeft);
            postMatchText.color = new Color(0.85f, 0.82f, 0.78f, 0.92f);
            StretchToRect(postMatchText.rectTransform, new Vector2(0.35f, 0.85f), new Vector2(0.66f, 0.96f));
            SetReference(lobbyUI, "postMatchText", postMatchText);

            EditorSceneManager.SaveScene(scene, LobbyScenePath);
        }

        private static void CreateHospitalScene(PrototypeAssets assets)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "HospitalLevel";

            ConfigureHospitalRenderSettings();
            CreateDirectionalLight(new Color(0.8f, 0.85f, 1f), 0.2f, new Vector3(35f, -55f, 0f));

            Transform systemsRoot = new GameObject("Systems").transform;

            GameObject gameStateObject = new GameObject("GameStateManager");
            gameStateObject.transform.SetParent(systemsRoot, false);
            gameStateObject.AddComponent<NetworkIdentity>();
            GameStateManager gameStateManager = gameStateObject.AddComponent<GameStateManager>();

            GameObject randomizerObject = new GameObject("RoundRandomizer");
            randomizerObject.transform.SetParent(systemsRoot, false);
            randomizerObject.AddComponent<NetworkIdentity>();
            RoundRandomizer roundRandomizer = randomizerObject.AddComponent<RoundRandomizer>();
            SetFloat(roundRandomizer, "lockedDoorChance", 0.45f);
            SetFloat(roundRandomizer, "openDoorChance", 0.22f);
            SetReference(gameStateManager, "roundRandomizer", roundRandomizer);

            GameObject bootstrapObject = new GameObject("GameplayBootstrapper");
            bootstrapObject.transform.SetParent(systemsRoot, false);
            bootstrapObject.AddComponent<NetworkIdentity>();
            GameplayBootstrapper gameplayBootstrapper = bootstrapObject.AddComponent<GameplayBootstrapper>();
            SetReference(gameplayBootstrapper, "monsterPrefab", assets.MonsterPrefab.GetComponent<MonsterAI>());
            SetReference(gameplayBootstrapper, "generatorPrefab", assets.GeneratorPrefab.GetComponent<GeneratorTask>());
            SetReference(gameplayBootstrapper, "keycardPrefab", assets.KeycardPrefab.GetComponent<KeycardTask>());
            SetReference(gameplayBootstrapper, "powerRestorePrefab", assets.PowerConsolePrefab.GetComponent<PowerRestoreTask>());
            SetReference(gameplayBootstrapper, "exitDoorPrefab", assets.ExitDoorPrefab.GetComponent<ExitDoorTask>());
            SetReference(gameplayBootstrapper, "hookPrefab", assets.HookPrefab.GetComponent<HookPoint>());
            SetReference(gameplayBootstrapper, "doorPrefab", assets.DoorPrefab.GetComponent<NetworkDoor>());
            SetReference(gameplayBootstrapper, "batteryPrefab", assets.BatteryPrefab.GetComponent<BatteryPickupTask>());

            GameObject navMeshObject = new GameObject("RuntimeNavMesh");
            navMeshObject.transform.SetParent(systemsRoot, false);
            navMeshObject.AddComponent<RuntimeNavMeshBootstrap>();

            GameObject ambientAudioObject = new GameObject("AmbientAudio");
            ambientAudioObject.transform.SetParent(systemsRoot, false);
            AmbientAudioController ambientAudioController = ambientAudioObject.AddComponent<AmbientAudioController>();
            AudioSource ambientLoopSource = ambientAudioObject.AddComponent<AudioSource>();
            ambientLoopSource.loop = true;
            ambientLoopSource.playOnAwake = false;
            ambientLoopSource.spatialBlend = 0f;
            ambientLoopSource.volume = 0.08f;
            ambientLoopSource.clip = LoadAudioClipAsset(AmbientLoopClipPath);
            AudioSource ambientOneShotSource = ambientAudioObject.AddComponent<AudioSource>();
            ambientOneShotSource.playOnAwake = false;
            ambientOneShotSource.spatialBlend = 0f;
            ambientOneShotSource.volume = 0.08f;
            SetReference(ambientAudioController, "loopSource", ambientLoopSource);
            SetReference(ambientAudioController, "oneShotSource", ambientOneShotSource);
            SetBool(ambientAudioController, "enableRandomOneShots", false);
            SetReferenceArray(ambientAudioController, "randomAmbientClips", new Object[0]);
            SetFloat(ambientAudioController, "minOneShotDelay", 18f);
            SetFloat(ambientAudioController, "maxOneShotDelay", 34f);

            GameObject falsePresenceObject = new GameObject("FalsePresenceDirector");
            falsePresenceObject.transform.SetParent(systemsRoot, false);
            FalsePresenceDirector falsePresenceDirector = falsePresenceObject.AddComponent<FalsePresenceDirector>();
            SetReferenceArray(falsePresenceDirector, "distantFootstepClips", LoadAudioClipAssets(
                ConcreteFootstepAPath,
                ConcreteFootstepBPath,
                ConcreteFootstepCPath,
                ConcreteFootstepDPath));
            SetReferenceArray(falsePresenceDirector, "metallicPresenceClips", LoadAudioClipAssets(
                AmbientOneShotAPath,
                AmbientOneShotBPath,
                MonsterStepAPath));

            Transform mapRoot = new GameObject("HospitalMap").transform;
            BuildHospitalMapGeometry(mapRoot);
            CreateHospitalLights(mapRoot);
            CreateFalsePresenceAnchors(mapRoot);
            CreateScareMoments(mapRoot);
            CreateScriptedHorrorMoments(mapRoot);
            CreatePlayerSpawnPoints(mapRoot);
            CreatePatrolPoints(mapRoot);
            CreateObjectiveSpawnPoints(mapRoot);
            PlaceGameplayObjects(assets, mapRoot);
            PurgeLegacyWorldText(mapRoot);
            CreateHudCanvas();

            Debug.Log("Hospital scene regenerated.");
            EditorSceneManager.SaveScene(scene, HospitalScenePath);
        }

        private static void PurgeLegacyWorldText(Transform root)
        {
            if (root == null)
            {
                return;
            }

            HashSet<GameObject> toDestroy = new HashSet<GameObject>();
            foreach (TextMesh textMesh in root.GetComponentsInChildren<TextMesh>(true))
            {
                if (textMesh != null)
                {
                    toDestroy.Add(textMesh.gameObject);
                }
            }

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child == null)
                {
                    continue;
                }

                if (child.name.StartsWith("Warning_", StringComparison.Ordinal) ||
                    child.name.StartsWith("Sign_", StringComparison.Ordinal))
                {
                    toDestroy.Add(child.gameObject);
                }
            }

            foreach (GameObject candidate in toDestroy)
            {
                if (candidate != null)
                {
                    Object.DestroyImmediate(candidate);
                }
            }
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(LobbyScenePath, true),
                new EditorBuildSettingsScene(HospitalScenePath, true)
            };
        }

        private static void BuildWindowsExe()
        {
            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string outputPath = System.Environment.GetEnvironmentVariable("HORROR_BUILD_EXE_PATH");
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                outputPath = Path.Combine(projectRoot, "Builds", "Windows", "AsylumHorrorPrototype.exe");
            }
            else
            {
                outputPath = outputPath.Trim();
            }

            string outputDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(outputDirectory) && !Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[]
                {
                    MainMenuScenePath,
                    LobbyScenePath,
                    HospitalScenePath
                },
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException($"Windows build failed: {report.summary.result}");
            }

            Debug.Log($"Windows build ready: {outputPath}");
        }

        private static void BuildHospitalMapGeometry(Transform root)
        {
            CreateCube(root, "Floor", new Vector3(0f, -0.5f, 0f), new Vector3(112f, 1f, 112f), new Color(0.07f, 0.07f, 0.08f), "floor");
            CreateCube(root, "Ceiling", new Vector3(0f, 4.1f, 0f), new Vector3(112f, 0.2f, 112f), new Color(0.05f, 0.05f, 0.06f), "ceiling");

            Color perimeterColor = new Color(0.13f, 0.13f, 0.15f);
            Color partitionColor = new Color(0.11f, 0.11f, 0.13f);

            CreateWallWithGapX(root, "NorthWall", new Vector3(0f, 1.9f, 56f), 112f, 4f, 0.5f, 5.2f, 0f, perimeterColor);
            CreateCube(root, "SouthWall", new Vector3(0f, 1.9f, -56f), new Vector3(112f, 4f, 0.5f), perimeterColor, "wall");
            CreateCube(root, "EastWall", new Vector3(56f, 1.9f, 0f), new Vector3(0.5f, 4f, 112f), perimeterColor, "wall");
            CreateCube(root, "WestWall", new Vector3(-56f, 1.9f, 0f), new Vector3(0.5f, 4f, 112f), perimeterColor, "wall");

            BuildRoom(root, "Security", new Vector3(0f, 0f, 42f), 24f, 14f, RoomDoorSide.South, 5.4f);
            BuildRoom(root, "AdminWest", new Vector3(-34f, 0f, 34f), 18f, 14f, RoomDoorSide.East, 5f);
            BuildRoom(root, "AdminEast", new Vector3(34f, 0f, 34f), 18f, 14f, RoomDoorSide.West, 5f);
            BuildRoom(root, "Archive", new Vector3(-34f, 0f, 4f), 18f, 22f, RoomDoorSide.East, 5f);
            BuildRoom(root, "Lab", new Vector3(34f, 0f, 4f), 18f, 22f, RoomDoorSide.West, 5f);
            BuildRoom(root, "Service", new Vector3(-32f, 0f, -28f), 22f, 18f, RoomDoorSide.East, 5.2f);
            BuildRoom(root, "Maintenance", new Vector3(32f, 0f, -28f), 22f, 18f, RoomDoorSide.West, 5.2f);
            BuildRoom(root, "Morgue", new Vector3(0f, 0f, -42f), 18f, 12f, RoomDoorSide.North, 5f);

            CreateCube(root, "HubDividerNorth", new Vector3(0f, 1.9f, 20f), new Vector3(16f, 4f, 0.5f), partitionColor, "wall");
            CreateCube(root, "HubDividerSouth", new Vector3(0f, 1.9f, -18f), new Vector3(16f, 4f, 0.5f), partitionColor, "wall");
            CreateCube(root, "HubDividerWest", new Vector3(-18f, 1.9f, 0f), new Vector3(0.5f, 4f, 18f), partitionColor, "wall");
            CreateCube(root, "HubDividerEast", new Vector3(18f, 1.9f, 0f), new Vector3(0.5f, 4f, 18f), partitionColor, "wall");

            CreateCube(root, "CoverWest", new Vector3(-8f, 0.75f, -6f), new Vector3(3f, 1.5f, 1.2f), new Color(0.18f, 0.18f, 0.2f), "metal");
            CreateCube(root, "CoverEast", new Vector3(8f, 0.75f, 8f), new Vector3(3f, 1.5f, 1.2f), new Color(0.18f, 0.18f, 0.2f), "metal");
            CreateCube(root, "CoverSouth", new Vector3(0f, 0.75f, -24f), new Vector3(4f, 1.5f, 1.2f), new Color(0.18f, 0.18f, 0.2f), "metal");
            CreateCube(root, "CoverNorth", new Vector3(0f, 0.75f, 24f), new Vector3(4f, 1.5f, 1.2f), new Color(0.18f, 0.18f, 0.2f), "metal");

            CreateCube(root, "ArchiveDivider_A", new Vector3(-33.2f, 1.45f, 10f), new Vector3(9.5f, 2.9f, 0.45f), new Color(0.12f, 0.12f, 0.14f), "wall");
            CreateCube(root, "ArchiveDivider_B", new Vector3(-33.2f, 1.45f, 2f), new Vector3(9.5f, 2.9f, 0.45f), new Color(0.12f, 0.12f, 0.14f), "wall");
            CreateCube(root, "ArchiveDivider_C", new Vector3(-33.2f, 1.45f, -6f), new Vector3(9.5f, 2.9f, 0.45f), new Color(0.12f, 0.12f, 0.14f), "wall");
            CreateCube(root, "ArchiveNook_A", new Vector3(-27f, 1.45f, -1.8f), new Vector3(0.45f, 2.9f, 6f), new Color(0.12f, 0.12f, 0.14f), "wall");

            CreateCube(root, "OperationDivider_A", new Vector3(35.2f, 1.45f, 8.8f), new Vector3(7.2f, 2.9f, 0.4f), new Color(0.12f, 0.12f, 0.14f), "wall");
            CreateCube(root, "OperationDivider_B", new Vector3(35.2f, 1.45f, -4.8f), new Vector3(7.2f, 2.9f, 0.4f), new Color(0.12f, 0.12f, 0.14f), "wall");
            CreateCube(root, "OperationPillar_A", new Vector3(27.6f, 1.45f, 7.2f), new Vector3(0.8f, 2.9f, 0.8f), new Color(0.14f, 0.14f, 0.16f), "wall");
            CreateCube(root, "OperationPillar_B", new Vector3(27.6f, 1.45f, -2.8f), new Vector3(0.8f, 2.9f, 0.8f), new Color(0.14f, 0.14f, 0.16f), "wall");

            CreateCube(root, "ServerBay_A", new Vector3(35f, 1.45f, -24f), new Vector3(0.5f, 2.9f, 8.4f), new Color(0.12f, 0.12f, 0.14f), "wall");
            CreateCube(root, "ServerBay_B", new Vector3(30f, 1.45f, -24f), new Vector3(0.5f, 2.9f, 8.4f), new Color(0.12f, 0.12f, 0.14f), "wall");
            CreateCube(root, "ServerBay_C", new Vector3(25f, 1.45f, -24f), new Vector3(0.5f, 2.9f, 8.4f), new Color(0.12f, 0.12f, 0.14f), "wall");

            CreateCube(root, "ServiceChoke_A", new Vector3(-24f, 1.45f, -23f), new Vector3(0.5f, 2.9f, 8f), new Color(0.12f, 0.12f, 0.14f), "wall");
            CreateCube(root, "ServiceChoke_B", new Vector3(-39.5f, 1.45f, -23f), new Vector3(0.5f, 2.9f, 8f), new Color(0.12f, 0.12f, 0.14f), "wall");
            CreateCube(root, "ServicePipeCover_A", new Vector3(-31.8f, 0.8f, -19.5f), new Vector3(2.8f, 1.6f, 1f), new Color(0.17f, 0.17f, 0.19f), "metal");

            CreateCube(root, "MorgueBayDivider_A", new Vector3(-5.8f, 1.45f, -40.5f), new Vector3(0.35f, 2.9f, 4.4f), new Color(0.14f, 0.14f, 0.16f), "wall");
            CreateCube(root, "MorgueBayDivider_B", new Vector3(5.8f, 1.45f, -40.5f), new Vector3(0.35f, 2.9f, 4.4f), new Color(0.14f, 0.14f, 0.16f), "wall");
            CreateCube(root, "MorgueColdStorage", new Vector3(0f, 1.45f, -47.2f), new Vector3(10f, 2.9f, 0.45f), new Color(0.11f, 0.11f, 0.13f), "wall");
            CreateCube(root, "HallTrim_NorthA", new Vector3(-24f, 0.74f, 19.6f), new Vector3(30f, 1.15f, 0.14f), new Color(0.15f, 0.16f, 0.17f), "metal");
            CreateCube(root, "HallTrim_NorthB", new Vector3(24f, 0.74f, 19.6f), new Vector3(30f, 1.15f, 0.14f), new Color(0.15f, 0.16f, 0.17f), "metal");
            CreateCube(root, "HallTrim_SouthA", new Vector3(-24f, 0.74f, -18.4f), new Vector3(30f, 1.15f, 0.14f), new Color(0.14f, 0.15f, 0.16f), "metal");
            CreateCube(root, "HallTrim_SouthB", new Vector3(24f, 0.74f, -18.4f), new Vector3(30f, 1.15f, 0.14f), new Color(0.14f, 0.15f, 0.16f), "metal");

            PlaceArchitecturalLandmarks(root);
            PlaceHorrorDetails(root);
            PlaceEnvironmentalDebris(root);
            PlaceHospitalSetDressing(root);
            PlaceModularShellDressings(root);
        }

        private static void BuildRoom(Transform parent, string roomName, Vector3 center, float width, float length, RoomDoorSide doorSide, float doorwayWidth)
        {
            Transform roomRoot = new GameObject(roomName).transform;
            roomRoot.SetParent(parent, false);
            roomRoot.position = center;

            float halfWidth = width * 0.5f;
            float halfLength = length * 0.5f;
            Color wallColor = new Color(0.15f, 0.15f, 0.17f);

            if (doorSide == RoomDoorSide.North)
            {
                CreateWallWithGapX(roomRoot, "North", new Vector3(0f, 1.9f, halfLength), width, 4f, 0.5f, doorwayWidth, 0f, wallColor);
            }
            else
            {
                CreateCube(roomRoot, "North", new Vector3(0f, 1.9f, halfLength), new Vector3(width, 4f, 0.5f), wallColor, "wall");
            }

            if (doorSide == RoomDoorSide.South)
            {
                CreateWallWithGapX(roomRoot, "South", new Vector3(0f, 1.9f, -halfLength), width, 4f, 0.5f, doorwayWidth, 0f, wallColor);
            }
            else
            {
                CreateCube(roomRoot, "South", new Vector3(0f, 1.9f, -halfLength), new Vector3(width, 4f, 0.5f), wallColor, "wall");
            }

            if (doorSide == RoomDoorSide.East)
            {
                CreateWallWithGapZ(roomRoot, "East", new Vector3(halfWidth, 1.9f, 0f), length, 4f, 0.5f, doorwayWidth, 0f, wallColor);
            }
            else
            {
                CreateCube(roomRoot, "East", new Vector3(halfWidth, 1.9f, 0f), new Vector3(0.5f, 4f, length), wallColor, "wall");
            }

            if (doorSide == RoomDoorSide.West)
            {
                CreateWallWithGapZ(roomRoot, "West", new Vector3(-halfWidth, 1.9f, 0f), length, 4f, 0.5f, doorwayWidth, 0f, wallColor);
            }
            else
            {
                CreateCube(roomRoot, "West", new Vector3(-halfWidth, 1.9f, 0f), new Vector3(0.5f, 4f, length), wallColor, "wall");
            }
        }

        private static void CreateWallWithGapX(
            Transform parent,
            string name,
            Vector3 center,
            float totalWidth,
            float height,
            float thickness,
            float gapWidth,
            float gapOffset,
            Color color,
            string style = "wall")
        {
            float half = totalWidth * 0.5f;
            float gapHalf = Mathf.Clamp(gapWidth * 0.5f, 0.1f, half - 0.1f);
            float gapMin = Mathf.Clamp(gapOffset - gapHalf, -half + 0.1f, half - 0.1f);
            float gapMax = Mathf.Clamp(gapOffset + gapHalf, gapMin + 0.1f, half - 0.1f);

            float leftWidth = gapMin + half;
            if (leftWidth > 0.1f)
            {
                Vector3 leftCenter = center + new Vector3((-half + gapMin) * 0.5f, 0f, 0f);
                CreateCube(parent, $"{name}_Left", leftCenter, new Vector3(leftWidth, height, thickness), color, style);
            }

            float rightWidth = half - gapMax;
            if (rightWidth > 0.1f)
            {
                Vector3 rightCenter = center + new Vector3((gapMax + half) * 0.5f, 0f, 0f);
                CreateCube(parent, $"{name}_Right", rightCenter, new Vector3(rightWidth, height, thickness), color, style);
            }
        }

        private static void CreateWallWithGapZ(
            Transform parent,
            string name,
            Vector3 center,
            float totalLength,
            float height,
            float thickness,
            float gapWidth,
            float gapOffset,
            Color color,
            string style = "wall")
        {
            float half = totalLength * 0.5f;
            float gapHalf = Mathf.Clamp(gapWidth * 0.5f, 0.1f, half - 0.1f);
            float gapMin = Mathf.Clamp(gapOffset - gapHalf, -half + 0.1f, half - 0.1f);
            float gapMax = Mathf.Clamp(gapOffset + gapHalf, gapMin + 0.1f, half - 0.1f);

            float frontLength = gapMin + half;
            if (frontLength > 0.1f)
            {
                Vector3 frontCenter = center + new Vector3(0f, 0f, (-half + gapMin) * 0.5f);
                CreateCube(parent, $"{name}_Front", frontCenter, new Vector3(thickness, height, frontLength), color, style);
            }

            float backLength = half - gapMax;
            if (backLength > 0.1f)
            {
                Vector3 backCenter = center + new Vector3(0f, 0f, (gapMax + half) * 0.5f);
                CreateCube(parent, $"{name}_Back", backCenter, new Vector3(thickness, height, backLength), color, style);
            }
        }

        private static void PlaceHorrorDetails(Transform root)
        {
            CreateBloodStain(root, "BloodPool_A", new Vector3(-34f, 0.03f, -30f), new Vector2(1.8f, 1.3f), 18f);
            CreateBloodStain(root, "BloodPool_B", new Vector3(31f, 0.03f, 28f), new Vector2(2.1f, 1.5f), -12f);
            CreateBloodStain(root, "BloodPool_C", new Vector3(0f, 0.03f, -38f), new Vector2(2.6f, 1.7f), 8f);
            CreateBloodStain(root, "BloodTrail_A", new Vector3(-8f, 0.03f, -26f), new Vector2(1.1f, 0.45f), 30f);
            CreateBloodStain(root, "BloodTrail_B", new Vector3(-5f, 0.03f, -28f), new Vector2(0.8f, 0.4f), 42f);
            CreateBloodStain(root, "BloodTrail_C", new Vector3(-2f, 0.03f, -30f), new Vector2(0.9f, 0.5f), 55f);
            CreateBloodStain(root, "BloodTrail_D", new Vector3(18f, 0.03f, 7f), new Vector2(1.25f, 0.45f), 70f);
            CreateBloodStain(root, "BloodTrail_E", new Vector3(21f, 0.03f, 5f), new Vector2(0.8f, 0.35f), 52f);
            CreateBloodStain(root, "BloodTrail_F", new Vector3(24f, 0.03f, 3f), new Vector2(0.95f, 0.42f), 46f);
            CreateBloodStain(root, "BloodHook_A", new Vector3(-45f, 0.03f, 41f), new Vector2(1.9f, 1.2f), 14f);
            CreateBloodStain(root, "BloodHook_B", new Vector3(45f, 0.03f, -41f), new Vector2(1.8f, 1.15f), -26f);
            CreateBloodStain(root, "BloodHook_C", new Vector3(0f, 0.03f, -45f), new Vector2(2.2f, 1.5f), 3f);
            CreateWallBloodStreak(root, "ClawMark_A", new Vector3(-32.8f, 1.45f, -27.1f), new Vector2(0.9f, 1.4f), 90f);
            CreateWallBloodStreak(root, "ClawMark_B", new Vector3(29.8f, 1.25f, 27.3f), new Vector2(0.65f, 1.2f), -90f);
            CreateWallBloodStreak(root, "ClawMark_C", new Vector3(-45.7f, 1.2f, 42.4f), new Vector2(0.75f, 1.35f), 40f);
            CreateWallBloodStreak(root, "ClawMark_D", new Vector3(46.1f, 1.25f, -42.2f), new Vector2(0.7f, 1.4f), -140f);
            CreateWallBloodStreak(root, "ClawMark_E", new Vector3(0f, 1.32f, -45.7f), new Vector2(0.8f, 1.6f), 180f);
            CreateWallBloodStreak(root, "ClawMark_F", new Vector3(-26.5f, 1.32f, -0.8f), new Vector2(0.7f, 1.5f), 90f);
            CreateWallBloodStreak(root, "ClawMark_G", new Vector3(26.5f, 1.32f, 8f), new Vector2(0.8f, 1.35f), -90f);
            CreateBloodStain(root, "DragTrail_ArchiveA", new Vector3(-33f, 0.03f, 12f), new Vector2(1.2f, 0.45f), 90f);
            CreateBloodStain(root, "DragTrail_ArchiveB", new Vector3(-33f, 0.03f, 6f), new Vector2(1.3f, 0.45f), 90f);
            CreateBloodStain(root, "DragTrail_ArchiveC", new Vector3(-33f, 0.03f, 0f), new Vector2(1.1f, 0.42f), 90f);
            CreateBloodStain(root, "OpBlood_A", new Vector3(35f, 0.03f, 7.2f), new Vector2(1.8f, 1.2f), 18f);
            CreateBloodStain(root, "OpBlood_B", new Vector3(35f, 0.03f, -2.6f), new Vector2(1.4f, 0.8f), -12f);
            CreateBloodStain(root, "MorgueDrag_A", new Vector3(0f, 0.03f, -43.2f), new Vector2(3.1f, 0.62f), 0f);
            PlaceAssetProp(root, "Assets/ThirdParty/PolyPizza/PostApocalypse/BloodSplat/Blood_1.fbx", "BloodSplat_A", new Vector3(-32f, 0.01f, -34f), Quaternion.Euler(0f, 24f, 0f), 0.12f, "blood");
            PlaceAssetProp(root, "Assets/ThirdParty/PolyPizza/PostApocalypse/BloodSplat/Blood_2.fbx", "BloodSplat_B", new Vector3(30f, 0.01f, 26f), Quaternion.Euler(0f, -12f, 0f), 0.12f, "blood");
        }

        private static void PlaceEnvironmentalDebris(Transform root)
        {
            CreateCube(root, "CollapsedShelf_A", new Vector3(-6f, 0.34f, 33f), new Vector3(2.6f, 0.42f, 0.6f), new Color(0.14f, 0.14f, 0.16f), "metal");
            CreateCube(root, "CollapsedShelf_B", new Vector3(8f, 0.34f, -36f), new Vector3(2.8f, 0.42f, 0.6f), new Color(0.14f, 0.14f, 0.16f), "metal");

            PlaceAssetProp(root, DebrisBarrelAssetPath, "Barrel_A", new Vector3(-31f, 0f, -36f), Quaternion.Euler(0f, 20f, 0f), 1.2f, "metal");
            PlaceAssetProp(root, DebrisBarrelAssetPath, "Barrel_B", new Vector3(29f, 0f, 34f), Quaternion.Euler(0f, -18f, 0f), 1.2f, "metal");
            PlaceAssetProp(root, DebrisContainerGreenAssetPath, "Container_A", new Vector3(-34f, 0f, 28f), Quaternion.Euler(0f, 90f, 0f), 1.15f, "metal");
            PlaceAssetProp(root, DebrisContainerRedAssetPath, "Container_B", new Vector3(34f, 0f, -30f), Quaternion.Euler(0f, -90f, 0f), 1.15f, "metal");
            PlaceAssetProp(root, DebrisCinderBlockAssetPath, "Cinder_A", new Vector3(-24f, 0f, 10f), Quaternion.Euler(0f, 15f, 0f), 0.4f, "wall");
            PlaceAssetProp(root, DebrisCinderBlockAssetPath, "Cinder_B", new Vector3(24f, 0f, -9f), Quaternion.Euler(0f, 35f, 0f), 0.4f, "wall");
            PlaceAssetProp(root, DebrisTrashBagAssetPath, "Trash_A", new Vector3(-40f, 0f, 4f), Quaternion.Euler(0f, -25f, 0f), 0.9f, "wall");
            PlaceAssetProp(root, DebrisTrashBagAssetPath, "Trash_B", new Vector3(40f, 0f, -4f), Quaternion.Euler(0f, 30f, 0f), 0.9f, "wall");
            PlaceAssetProp(root, DebrisPalletBrokenAssetPath, "Pallet_A", new Vector3(-2.5f, 0f, 24f), Quaternion.Euler(0f, 40f, 0f), 0.95f, "wall");
            PlaceAssetProp(root, DebrisPalletBrokenAssetPath, "Pallet_B", new Vector3(3.5f, 0f, -24f), Quaternion.Euler(0f, -36f, 0f), 0.95f, "wall");
            PlaceAssetProp(root, DebrisWheelsStackAssetPath, "Wheels_A", new Vector3(22f, 0f, 9f), Quaternion.Euler(0f, 32f, 0f), 1f, "metal");
            PlaceAssetProp(root, DebrisGasCanAssetPath, "GasCan_A", new Vector3(-14f, 0f, -10f), Quaternion.Euler(0f, -28f, 0f), 0.8f, "metal");
        }

        private static void PlaceArchitecturalLandmarks(Transform root)
        {
            Color dividerColor = new Color(0.12f, 0.12f, 0.14f);
            Color deskColor = new Color(0.17f, 0.16f, 0.15f);

            CreateCube(root, "HubDesk_Front", new Vector3(0f, 0.72f, 10f), new Vector3(10f, 1.44f, 1.2f), deskColor, "wood");
            CreateCube(root, "HubDesk_Left", new Vector3(-4.4f, 0.72f, 6.4f), new Vector3(1.2f, 1.44f, 6f), deskColor, "wood");
            CreateCube(root, "HubDesk_Right", new Vector3(4.4f, 0.72f, 6.4f), new Vector3(1.2f, 1.44f, 6f), deskColor, "wood");
            CreateCube(root, "HubScreen_Left", new Vector3(-12f, 1.9f, 10f), new Vector3(0.5f, 4f, 12f), dividerColor, "wall");
            CreateCube(root, "HubScreen_Right", new Vector3(12f, 1.9f, 10f), new Vector3(0.5f, 4f, 12f), dividerColor, "wall");

            CreateCube(root, "SouthServiceDivider_Left", new Vector3(-14f, 1.9f, -18f), new Vector3(8f, 4f, 0.5f), dividerColor, "wall");
            CreateCube(root, "SouthServiceDivider_Right", new Vector3(14f, 1.9f, -18f), new Vector3(8f, 4f, 0.5f), dividerColor, "wall");
            CreateCube(root, "TriagePillar_W", new Vector3(-20f, 1.9f, 20f), new Vector3(1.2f, 4f, 1.2f), dividerColor, "wall");
            CreateCube(root, "TriagePillar_E", new Vector3(20f, 1.9f, 20f), new Vector3(1.2f, 4f, 1.2f), dividerColor, "wall");

            PlaceAssetProp(root, KenneyLampWallAssetPath, "Lamp_NorthWest", new Vector3(-18f, 1.8f, 28f), Quaternion.Euler(0f, 180f, 0f), 0.7f, "metal");
            PlaceAssetProp(root, KenneyLampWallAssetPath, "Lamp_NorthEast", new Vector3(18f, 1.8f, 28f), Quaternion.Euler(0f, 180f, 0f), 0.7f, "metal");
            PlaceAssetProp(root, KenneyLampWallAssetPath, "Lamp_MidWest", new Vector3(-18f, 1.8f, -4f), Quaternion.identity, 0.7f, "metal");
            PlaceAssetProp(root, KenneyLampWallAssetPath, "Lamp_MidEast", new Vector3(18f, 1.8f, -4f), Quaternion.identity, 0.7f, "metal");

        }

        private static void PlaceHospitalSetDressing(Transform root)
        {
            PlaceAssetProp(root, KenneyDeskAssetPath, "Security_DeskA", new Vector3(-5.2f, 0f, 45f), Quaternion.Euler(0f, 180f, 0f), 1.2f, "wood");
            PlaceAssetProp(root, KenneyDeskAssetPath, "Security_DeskB", new Vector3(5.2f, 0f, 45f), Quaternion.Euler(0f, 180f, 0f), 1.2f, "wood");
            PlaceAssetProp(root, KenneyChairDeskAssetPath, "Security_ChairA", new Vector3(-5.2f, 0f, 42.2f), Quaternion.identity, 1.15f, "metal");
            PlaceAssetProp(root, KenneyChairDeskAssetPath, "Security_ChairB", new Vector3(5.2f, 0f, 42.2f), Quaternion.identity, 1.15f, "metal");
            PlaceAssetProp(root, KenneyComputerScreenAssetPath, "Security_ScreenA", new Vector3(-5.25f, 0f, 45.8f), Quaternion.Euler(0f, 180f, 0f), 0.7f, "metal");
            PlaceAssetProp(root, KenneyComputerScreenAssetPath, "Security_ScreenB", new Vector3(5.15f, 0f, 45.8f), Quaternion.Euler(0f, 180f, 0f), 0.7f, "metal");
            PlaceAssetProp(root, KenneyComputerKeyboardAssetPath, "Security_KeyboardA", new Vector3(-5.2f, 0f, 44.7f), Quaternion.Euler(0f, 180f, 0f), 0.18f, "metal");
            PlaceAssetProp(root, KenneyComputerKeyboardAssetPath, "Security_KeyboardB", new Vector3(5.2f, 0f, 44.7f), Quaternion.Euler(0f, 180f, 0f), 0.18f, "metal");

            PlaceTexturedAssetProp(root, HomeBookcaseAssetPath, HomeInteriorTextureAssetPath, "AdminWest_Bookcase", new Vector3(-40.4f, 0f, 38.4f), Quaternion.Euler(0f, 90f, 0f), 2.1f, "wood", new Color(0.7f, 0.7f, 0.68f));
            PlaceTexturedAssetProp(root, HomeTableAssetPath, HomeInteriorTextureAssetPath, "AdminWest_Table", new Vector3(-31f, 0f, 34f), Quaternion.identity, 1f, "wood", new Color(0.68f, 0.68f, 0.66f));
            PlaceTexturedAssetProp(root, HomeChairAssetPath, HomeInteriorTextureAssetPath, "AdminWest_Chair", new Vector3(-29.2f, 0f, 32.8f), Quaternion.Euler(0f, -135f, 0f), 1.1f, "wood", new Color(0.66f, 0.66f, 0.64f));
            PlaceTexturedAssetProp(root, HomeBookcaseAssetPath, HomeInteriorTextureAssetPath, "AdminEast_Bookcase", new Vector3(40.4f, 0f, 38.4f), Quaternion.Euler(0f, -90f, 0f), 2.1f, "wood", new Color(0.7f, 0.7f, 0.68f));
            PlaceTexturedAssetProp(root, HomeTableAssetPath, HomeInteriorTextureAssetPath, "AdminEast_Table", new Vector3(31f, 0f, 34f), Quaternion.identity, 1f, "wood", new Color(0.68f, 0.68f, 0.66f));
            PlaceTexturedAssetProp(root, HomeChairAssetPath, HomeInteriorTextureAssetPath, "AdminEast_Chair", new Vector3(29.2f, 0f, 32.8f), Quaternion.Euler(0f, 135f, 0f), 1.1f, "wood", new Color(0.66f, 0.66f, 0.64f));

            PlaceTexturedAssetProp(root, HomeShelfAssetPath, HomeInteriorTextureAssetPath, "Archive_ShelfA", new Vector3(-38.6f, 0f, 8f), Quaternion.Euler(0f, 90f, 0f), 1.95f, "wood", new Color(0.66f, 0.66f, 0.64f));
            PlaceTexturedAssetProp(root, HomeShelfAssetPath, HomeInteriorTextureAssetPath, "Archive_ShelfB", new Vector3(-38.6f, 0f, -2f), Quaternion.Euler(0f, 90f, 0f), 1.95f, "wood", new Color(0.66f, 0.66f, 0.64f));
            PlaceTexturedAssetProp(root, HomeBookcaseAssetPath, HomeInteriorTextureAssetPath, "Archive_Bookcase", new Vector3(-28.2f, 0f, 9.8f), Quaternion.Euler(0f, 180f, 0f), 2.2f, "wood", new Color(0.68f, 0.68f, 0.66f));
            PlaceTexturedAssetProp(root, HomeShelfAssetPath, HomeInteriorTextureAssetPath, "Archive_AisleShelfA", new Vector3(-35.2f, 0f, 12f), Quaternion.Euler(0f, 90f, 0f), 1.95f, "wood", new Color(0.66f, 0.66f, 0.64f));
            PlaceTexturedAssetProp(root, HomeShelfAssetPath, HomeInteriorTextureAssetPath, "Archive_AisleShelfB", new Vector3(-35.2f, 0f, 4f), Quaternion.Euler(0f, 90f, 0f), 1.95f, "wood", new Color(0.66f, 0.66f, 0.64f));
            PlaceTexturedAssetProp(root, HomeShelfAssetPath, HomeInteriorTextureAssetPath, "Archive_AisleShelfC", new Vector3(-35.2f, 0f, -4f), Quaternion.Euler(0f, 90f, 0f), 1.95f, "wood", new Color(0.66f, 0.66f, 0.64f));
            PlaceAssetProp(root, DebrisPalletBrokenAssetPath, "Archive_FallenShelf", new Vector3(-30f, 0f, -8f), Quaternion.Euler(0f, 92f, 0f), 1.2f, "wood");

            PlaceTexturedAssetProp(root, HomeCabinetAssetPath, HomeInteriorTextureAssetPath, "Lab_CabinetA", new Vector3(40.2f, 0f, 10.2f), Quaternion.Euler(0f, -90f, 0f), 1.8f, "metal", new Color(0.8f, 0.82f, 0.82f));
            PlaceTexturedAssetProp(root, HomeCabinetAssetPath, HomeInteriorTextureAssetPath, "Lab_CabinetB", new Vector3(40.2f, 0f, -1.6f), Quaternion.Euler(0f, -90f, 0f), 1.8f, "metal", new Color(0.8f, 0.82f, 0.82f));
            PlaceAssetProp(root, KenneyBathroomSinkAssetPath, "Lab_Sink", new Vector3(27f, 0f, 10.2f), Quaternion.Euler(0f, 90f, 0f), 1.05f, "metal");
            PlaceTexturedAssetProp(root, HomeBenchAssetPath, HomeInteriorTextureAssetPath, "Lab_Bench", new Vector3(31.8f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), 1.1f, "metal", new Color(0.76f, 0.76f, 0.76f));
            CreateCube(root, "OperatingTable", new Vector3(35f, 0.72f, 2.4f), new Vector3(2.8f, 0.22f, 1.2f), new Color(0.24f, 0.24f, 0.26f), "metal");
            CreateCube(root, "OpCurtain_A", new Vector3(35f, 1.5f, 8.8f), new Vector3(4.2f, 2.6f, 0.06f), new Color(0.15f, 0.1f, 0.1f), "fabric");
            CreateCube(root, "OpCurtain_B", new Vector3(35f, 1.5f, -4.8f), new Vector3(4.2f, 2.6f, 0.06f), new Color(0.15f, 0.1f, 0.1f), "fabric");
            PlaceAssetProp(root, KenneyComputerScreenAssetPath, "OpMonitor_A", new Vector3(33.8f, 0f, 3.2f), Quaternion.Euler(0f, 180f, 0f), 0.72f, "metal");
            PlaceAssetProp(root, KenneyComputerKeyboardAssetPath, "OpKeyboard_A", new Vector3(34.1f, 0f, 2.4f), Quaternion.Euler(0f, 180f, 0f), 0.2f, "metal");

            PlaceTexturedAssetProp(root, HomeBenchAssetPath, HomeInteriorTextureAssetPath, "Service_Bench", new Vector3(-35.8f, 0f, -32.4f), Quaternion.Euler(0f, 90f, 0f), 1.1f, "wood", new Color(0.72f, 0.72f, 0.7f));
            PlaceAssetProp(root, KenneyTrashcanAssetPath, "Service_Trash", new Vector3(-24.8f, 0f, -20.8f), Quaternion.identity, 0.85f, "metal");
            PlaceTexturedAssetProp(root, HomeCabinetAssetPath, HomeInteriorTextureAssetPath, "Service_Cabinet", new Vector3(-24.6f, 0f, -33f), Quaternion.Euler(0f, 180f, 0f), 1.8f, "metal", new Color(0.78f, 0.78f, 0.8f));
            CreatePipeRun(root, "ServicePipes_A", new Vector3(-32f, 2.9f, -18f), 15f, true);
            CreatePipeRun(root, "ServicePipes_B", new Vector3(-32f, 2.45f, -22f), 14f, true);
            CreatePipeRun(root, "ServicePipes_C", new Vector3(-27.6f, 2.65f, -26f), 8f, false);

            PlaceAssetProp(root, KenneyWasherStackAssetPath, "Maint_Stack", new Vector3(38.4f, 0f, -31.8f), Quaternion.Euler(0f, 180f, 0f), 2.2f, "metal");
            PlaceTexturedAssetProp(root, HomeBenchAssetPath, HomeInteriorTextureAssetPath, "Maint_Workbench", new Vector3(27f, 0f, -31.8f), Quaternion.identity, 1.15f, "wood", new Color(0.72f, 0.72f, 0.7f));
            PlaceAssetProp(root, KenneyTrashcanAssetPath, "Maint_Trash", new Vector3(24.8f, 0f, -20.8f), Quaternion.identity, 0.85f, "metal");
            CreateCube(root, "ServerRack_A", new Vector3(34.6f, 1.2f, -27f), new Vector3(1.1f, 2.4f, 1.8f), new Color(0.12f, 0.13f, 0.15f), "metal");
            CreateCube(root, "ServerRack_B", new Vector3(29.6f, 1.2f, -27f), new Vector3(1.1f, 2.4f, 1.8f), new Color(0.12f, 0.13f, 0.15f), "metal");
            CreateCube(root, "ServerRack_C", new Vector3(24.6f, 1.2f, -27f), new Vector3(1.1f, 2.4f, 1.8f), new Color(0.12f, 0.13f, 0.15f), "metal");
            PlaceAssetProp(root, KenneyComputerScreenAssetPath, "ServerPanel_A", new Vector3(34.6f, 0f, -27f), Quaternion.Euler(0f, 180f, 0f), 0.6f, "metal");
            PlaceAssetProp(root, KenneyComputerScreenAssetPath, "ServerPanel_B", new Vector3(29.6f, 0f, -27f), Quaternion.Euler(0f, 180f, 0f), 0.6f, "metal");
            PlaceRawAssetProp(root, QuaterniusWall2AssetPath, "SciFiWall_A", new Vector3(42.2f, 0f, -33.6f), Quaternion.Euler(0f, -90f, 0f), 3.15f);
            PlaceRawAssetProp(root, QuaterniusWall5AssetPath, "SciFiWall_B", new Vector3(42.2f, 0f, -24.4f), Quaternion.Euler(0f, -90f, 0f), 3.15f);
            PlaceRawAssetProp(root, QuaterniusColumn2AssetPath, "SciFiColumn_A", new Vector3(39.1f, 0f, -35.8f), Quaternion.identity, 3.2f);
            PlaceRawAssetProp(root, QuaterniusColumn2AssetPath, "SciFiColumn_B", new Vector3(39.1f, 0f, -19.2f), Quaternion.identity, 3.2f);
            PlaceRawAssetProp(root, QuaterniusShelfTallAssetPath, "SciFiShelf_A", new Vector3(22.4f, 0f, -20.8f), Quaternion.Euler(0f, 90f, 0f), 2.55f);
            PlaceRawAssetProp(root, QuaterniusCrateLongAssetPath, "SciFiCrate_A", new Vector3(39.6f, 0f, -27.8f), Quaternion.Euler(0f, -90f, 0f), 1.35f);
            PlaceRawAssetProp(root, QuaterniusComputerAssetPath, "SciFiConsole_A", new Vector3(28.4f, 0f, -21.2f), Quaternion.Euler(0f, 180f, 0f), 1.08f);
            PlaceRawAssetProp(root, QuaterniusPipesLongAssetPath, "SciFiPipe_A", new Vector3(31.4f, 2.95f, -18.1f), Quaternion.identity, 0.62f);
            PlaceRawAssetProp(root, QuaterniusDoorSingleAssetPath, "SciFiDoor_A", new Vector3(43.7f, 0f, -27.8f), Quaternion.Euler(0f, -90f, 0f), 3.05f);

            PlaceTexturedAssetProp(root, HomeShelfAssetPath, HomeInteriorTextureAssetPath, "Morgue_Shelf", new Vector3(-5.4f, 0f, -45f), Quaternion.Euler(0f, 180f, 0f), 1.8f, "metal", new Color(0.68f, 0.68f, 0.68f));
            PlaceTexturedAssetProp(root, HomeTableAssetPath, HomeInteriorTextureAssetPath, "Morgue_Slab", new Vector3(0f, 0f, -40.8f), Quaternion.identity, 1f, "metal", new Color(0.74f, 0.74f, 0.74f));
            PlaceAssetProp(root, KenneyBathroomCabinetAssetPath, "Morgue_Cabinet", new Vector3(5.4f, 0f, -45f), Quaternion.Euler(0f, 180f, 0f), 1.3f, "metal");
            CreateCube(root, "MorgueCurtain_A", new Vector3(-5.8f, 1.5f, -40.5f), new Vector3(0.08f, 2.6f, 4.2f), new Color(0.14f, 0.16f, 0.18f), "fabric");
            CreateCube(root, "MorgueCurtain_B", new Vector3(5.8f, 1.5f, -40.5f), new Vector3(0.08f, 2.6f, 4.2f), new Color(0.14f, 0.16f, 0.18f), "fabric");
            CreateCube(root, "MorgueBodyBag_A", new Vector3(-3.2f, 0.42f, -40.8f), new Vector3(1.7f, 0.28f, 0.6f), new Color(0.18f, 0.18f, 0.2f), "fabric");
            CreateCube(root, "MorgueBodyBag_B", new Vector3(3.2f, 0.42f, -40.8f), new Vector3(1.7f, 0.28f, 0.6f), new Color(0.18f, 0.18f, 0.2f), "fabric");

            PlaceTexturedAssetProp(root, HomeBenchAssetPath, HomeInteriorTextureAssetPath, "HallBench_NorthW", new Vector3(-14f, 0f, 22f), Quaternion.Euler(0f, 90f, 0f), 1.1f, "wood", new Color(0.7f, 0.7f, 0.68f));
            PlaceTexturedAssetProp(root, HomeBenchAssetPath, HomeInteriorTextureAssetPath, "HallBench_NorthE", new Vector3(14f, 0f, 22f), Quaternion.Euler(0f, -90f, 0f), 1.1f, "wood", new Color(0.7f, 0.7f, 0.68f));
            PlaceAssetProp(root, KenneyTrashcanAssetPath, "HallTrash_West", new Vector3(-19.6f, 0f, -6.4f), Quaternion.identity, 0.85f, "metal");
            PlaceAssetProp(root, KenneyTrashcanAssetPath, "HallTrash_East", new Vector3(19.6f, 0f, 6.4f), Quaternion.identity, 0.85f, "metal");
            CreateCube(root, "HallReceptionBarrier", new Vector3(0f, 1.1f, 13.8f), new Vector3(6.2f, 2.2f, 0.2f), new Color(0.18f, 0.18f, 0.2f), "glass");
        }

        private static void PlaceModularShellDressings(Transform root)
        {
            PlaceRawAssetProp(root, QuaterniusWall5AssetPath, "SecurityPanel_NorthWest", new Vector3(-17.8f, 0f, 48.4f), Quaternion.identity, 3.15f);
            PlaceRawAssetProp(root, QuaterniusWall5AssetPath, "SecurityPanel_NorthEast", new Vector3(17.8f, 0f, 48.4f), Quaternion.identity, 3.15f);
            PlaceRawAssetProp(root, QuaterniusColumn2AssetPath, "SecurityColumn_West", new Vector3(-12.4f, 0f, 34.8f), Quaternion.identity, 3.05f);
            PlaceRawAssetProp(root, QuaterniusColumn2AssetPath, "SecurityColumn_East", new Vector3(12.4f, 0f, 34.8f), Quaternion.identity, 3.05f);

            PlaceRawAssetProp(root, QuaterniusWall2AssetPath, "ArchivePanel_A", new Vector3(-42.2f, 0f, 10.6f), Quaternion.Euler(0f, 90f, 0f), 3.05f);
            PlaceRawAssetProp(root, QuaterniusWall2AssetPath, "ArchivePanel_B", new Vector3(-42.2f, 0f, -4.6f), Quaternion.Euler(0f, 90f, 0f), 3.05f);
            PlaceRawAssetProp(root, QuaterniusColumn2AssetPath, "ArchiveColumn_A", new Vector3(-26.9f, 0f, 15.1f), Quaternion.identity, 3f);
            PlaceRawAssetProp(root, QuaterniusPipesLongAssetPath, "ArchivePipe_A", new Vector3(-33.8f, 2.95f, 15.2f), Quaternion.Euler(0f, 90f, 0f), 0.58f);

            PlaceRawAssetProp(root, QuaterniusWall5AssetPath, "LabPanel_A", new Vector3(42.4f, 0f, 11.2f), Quaternion.Euler(0f, -90f, 0f), 3.15f);
            PlaceRawAssetProp(root, QuaterniusWall2AssetPath, "LabPanel_B", new Vector3(42.4f, 0f, -7.8f), Quaternion.Euler(0f, -90f, 0f), 3.15f);
            PlaceRawAssetProp(root, QuaterniusColumn2AssetPath, "LabColumn_A", new Vector3(26.8f, 0f, 13.6f), Quaternion.identity, 3.05f);
            PlaceRawAssetProp(root, QuaterniusComputerAssetPath, "LabConsole_A", new Vector3(28.6f, 0f, 12.1f), Quaternion.Euler(0f, 90f, 0f), 1.04f);

            PlaceRawAssetProp(root, QuaterniusWall2AssetPath, "ServicePanel_A", new Vector3(-42.1f, 0f, -34.8f), Quaternion.Euler(0f, 90f, 0f), 3.15f);
            PlaceRawAssetProp(root, QuaterniusWall5AssetPath, "ServicePanel_B", new Vector3(-42.1f, 0f, -21.4f), Quaternion.Euler(0f, 90f, 0f), 3.15f);
            PlaceRawAssetProp(root, QuaterniusColumn2AssetPath, "ServiceColumn_A", new Vector3(-21.6f, 0f, -35.4f), Quaternion.identity, 3.1f);
            PlaceRawAssetProp(root, QuaterniusCrateLongAssetPath, "ServiceCrate_A", new Vector3(-21.4f, 0f, -17.8f), Quaternion.Euler(0f, 90f, 0f), 1.32f);

            PlaceRawAssetProp(root, QuaterniusWall5AssetPath, "MaintPanel_A", new Vector3(21.6f, 0f, -35.8f), Quaternion.Euler(0f, 180f, 0f), 3.15f);
            PlaceRawAssetProp(root, QuaterniusWall2AssetPath, "MaintPanel_B", new Vector3(42.1f, 0f, -20.8f), Quaternion.Euler(0f, -90f, 0f), 3.15f);
            PlaceRawAssetProp(root, QuaterniusColumn2AssetPath, "MaintColumn_A", new Vector3(21.5f, 0f, -18.9f), Quaternion.identity, 3.05f);
            PlaceRawAssetProp(root, QuaterniusPipesLongAssetPath, "MaintPipe_A", new Vector3(32.2f, 2.92f, -35.1f), Quaternion.identity, 0.6f);

            PlaceRawAssetProp(root, QuaterniusWall5AssetPath, "MorguePanel_A", new Vector3(-8.6f, 0f, -47.1f), Quaternion.identity, 3.05f);
            PlaceRawAssetProp(root, QuaterniusWall5AssetPath, "MorguePanel_B", new Vector3(8.6f, 0f, -47.1f), Quaternion.identity, 3.05f);
            PlaceRawAssetProp(root, QuaterniusColumn2AssetPath, "MorgueColumn_A", new Vector3(-9.2f, 0f, -36.4f), Quaternion.identity, 3f);
            PlaceRawAssetProp(root, QuaterniusColumn2AssetPath, "MorgueColumn_B", new Vector3(9.2f, 0f, -36.4f), Quaternion.identity, 3f);

            PlaceRawAssetProp(root, QuaterniusDoorSingleAssetPath, "HubDoorDress_A", new Vector3(-18.1f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), 3.05f);
            PlaceRawAssetProp(root, QuaterniusDoorSingleAssetPath, "HubDoorDress_B", new Vector3(18.1f, 0f, 0f), Quaternion.Euler(0f, -90f, 0f), 3.05f);
            PlaceRawAssetProp(root, QuaterniusDoorSingleAssetPath, "ServiceDoorDress", new Vector3(0f, 0f, -18.3f), Quaternion.identity, 3.05f);
        }

        private static void CreateHospitalLights(Transform root)
        {
            CreatePointLight(root, "Light_Security", new Vector3(0f, 3.2f, 42f), 14f, 0.16f, true);
            CreatePointLight(root, "Light_AdminWest", new Vector3(-34f, 3.2f, 34f), 12f, 0.13f, true);
            CreatePointLight(root, "Light_AdminEast", new Vector3(34f, 3.2f, 34f), 12f, 0.13f, true);
            CreatePointLight(root, "Light_HubNorth", new Vector3(0f, 3.2f, 16f), 16f, 0.14f, true);
            CreatePointLight(root, "Light_HubCenter", new Vector3(0f, 3.2f, 0f), 18f, 0.1f, false);
            CreatePointLight(root, "Light_Archive", new Vector3(-34f, 3.2f, 4f), 13f, 0.13f, true);
            CreatePointLight(root, "Light_Lab", new Vector3(34f, 3.2f, 4f), 13f, 0.13f, true);
            CreatePointLight(root, "Light_Service", new Vector3(-32f, 3.2f, -28f), 14f, 0.11f, true);
            CreatePointLight(root, "Light_Maint", new Vector3(32f, 3.2f, -28f), 14f, 0.11f, true);
            CreatePointLight(root, "Light_Morgue", new Vector3(0f, 3.2f, -42f), 12f, 0.09f, true);
            CreateColoredPointLight(root, "Light_ArchiveCold", new Vector3(-34f, 2.6f, 2f), 10f, 0.08f, new Color(0.72f, 0.78f, 0.88f), false);
            CreateColoredPointLight(root, "Light_OperationRed", new Vector3(35f, 2.7f, 2.4f), 10f, 0.24f, new Color(0.8f, 0.08f, 0.08f), true);
            CreateColoredPointLight(root, "Light_ServerBlue", new Vector3(30f, 2.7f, -27f), 11f, 0.18f, new Color(0.16f, 0.4f, 0.76f), true);
            CreateColoredPointLight(root, "Light_ServiceGreen", new Vector3(-32f, 2.6f, -20f), 10f, 0.1f, new Color(0.42f, 0.54f, 0.42f), true);
            CreateColoredPointLight(root, "Light_MorgueCold", new Vector3(0f, 2.7f, -40.2f), 9f, 0.14f, new Color(0.7f, 0.86f, 0.98f), true);

            GameObject emergency = new GameObject("EmergencyRedLight_Morgue");
            emergency.transform.SetParent(root, false);
            emergency.transform.localPosition = new Vector3(0f, 3f, -42f);
            Light emergencyLight = emergency.AddComponent<Light>();
            emergencyLight.type = LightType.Point;
            emergencyLight.range = 18f;
            emergencyLight.intensity = 0.82f;
            emergencyLight.color = new Color(0.74f, 0.08f, 0.08f);
            FlickerLight emergencyFlicker = emergency.AddComponent<FlickerLight>();
            emergencyFlicker.SetBaseIntensity(0.88f);

            GameObject emergencyHall = new GameObject("EmergencyRedLight_Hall");
            emergencyHall.transform.SetParent(root, false);
            emergencyHall.transform.localPosition = new Vector3(-2f, 3f, 12f);
            Light emergencyHallLight = emergencyHall.AddComponent<Light>();
            emergencyHallLight.type = LightType.Point;
            emergencyHallLight.range = 12f;
            emergencyHallLight.intensity = 0.57f;
            emergencyHallLight.color = new Color(0.7f, 0.12f, 0.1f);
            FlickerLight hallFlicker = emergencyHall.AddComponent<FlickerLight>();
            hallFlicker.SetBaseIntensity(0.57f);

            GameObject corridorAlarm = new GameObject("EmergencyRedLight_Cross");
            corridorAlarm.transform.SetParent(root, false);
            corridorAlarm.transform.localPosition = new Vector3(18f, 3f, -18f);
            Light corridorAlarmLight = corridorAlarm.AddComponent<Light>();
            corridorAlarmLight.type = LightType.Point;
            corridorAlarmLight.range = 10f;
            corridorAlarmLight.intensity = 0.46f;
            corridorAlarmLight.color = new Color(0.65f, 0.1f, 0.1f);
            FlickerLight corridorAlarmFlicker = corridorAlarm.AddComponent<FlickerLight>();
            corridorAlarmFlicker.SetBaseIntensity(0.46f);
        }

        private static void CreatePlayerSpawnPoints(Transform root)
        {
            Transform spawnsRoot = new GameObject("PlayerSpawnPoints").transform;
            spawnsRoot.SetParent(root, false);

            CreatePlayerSpawn(spawnsRoot, "Spawn_0", new Vector3(-4f, 0f, 39.5f), Quaternion.Euler(0f, 180f, 0f), 0);
            CreatePlayerSpawn(spawnsRoot, "Spawn_1", new Vector3(4f, 0f, 39.5f), Quaternion.Euler(0f, 180f, 0f), 1);
            CreatePlayerSpawn(spawnsRoot, "Spawn_2", new Vector3(-7f, 0f, 43.5f), Quaternion.Euler(0f, 160f, 0f), 2);
            CreatePlayerSpawn(spawnsRoot, "Spawn_3", new Vector3(7f, 0f, 43.5f), Quaternion.Euler(0f, 200f, 0f), 3);
        }

        private static void CreateLobbyStartPositions(Transform root)
        {
            Transform spawnsRoot = new GameObject("LobbyStartPositions").transform;
            spawnsRoot.SetParent(root, false);

            CreateLobbyStartPosition(spawnsRoot, "LobbySpawn_A", new Vector3(-2.8f, 0f, -2.2f), Quaternion.Euler(0f, 18f, 0f));
            CreateLobbyStartPosition(spawnsRoot, "LobbySpawn_B", new Vector3(2.8f, 0f, -2.2f), Quaternion.Euler(0f, -18f, 0f));
            CreateLobbyStartPosition(spawnsRoot, "LobbySpawn_C", new Vector3(-1.4f, 0f, -0.8f), Quaternion.Euler(0f, 26f, 0f));
            CreateLobbyStartPosition(spawnsRoot, "LobbySpawn_D", new Vector3(1.4f, 0f, -0.8f), Quaternion.Euler(0f, -26f, 0f));
        }

        private static void CreatePatrolPoints(Transform root)
        {
            Transform patrolRoot = new GameObject("PatrolPoints").transform;
            patrolRoot.SetParent(root, false);

            CreatePatrolPoint(patrolRoot, "Patrol_A", new Vector3(-24f, 0f, 34f));
            CreatePatrolPoint(patrolRoot, "Patrol_B", new Vector3(0f, 0f, 28f));
            CreatePatrolPoint(patrolRoot, "Patrol_C", new Vector3(24f, 0f, 34f));
            CreatePatrolPoint(patrolRoot, "Patrol_D", new Vector3(30f, 0f, 4f));
            CreatePatrolPoint(patrolRoot, "Patrol_E", new Vector3(24f, 0f, -26f));
            CreatePatrolPoint(patrolRoot, "Patrol_F", new Vector3(0f, 0f, -34f));
            CreatePatrolPoint(patrolRoot, "Patrol_G", new Vector3(-24f, 0f, -26f));
            CreatePatrolPoint(patrolRoot, "Patrol_H", new Vector3(-30f, 0f, 4f));
            CreatePatrolPoint(patrolRoot, "Patrol_I", new Vector3(0f, 0f, 6f));
            CreatePatrolPoint(patrolRoot, "Patrol_J", new Vector3(-35f, 0f, 8f));
            CreatePatrolPoint(patrolRoot, "Patrol_K", new Vector3(35f, 0f, 2f));
            CreatePatrolPoint(patrolRoot, "Patrol_L", new Vector3(30f, 0f, -27f));
            CreatePatrolPoint(patrolRoot, "Patrol_M", new Vector3(-32f, 0f, -20f));
            CreatePatrolPoint(patrolRoot, "Patrol_N", new Vector3(0f, 0f, -42f));
        }

        private static void CreateFalsePresenceAnchors(Transform root)
        {
            Transform anchorsRoot = new GameObject("FalsePresenceAnchors").transform;
            anchorsRoot.SetParent(root, false);

            CreateFalsePresenceAnchor(anchorsRoot, "Presence_SecurityFlash", new Vector3(0f, 0f, 31f), Quaternion.Euler(0f, 180f, 0f), FalsePresenceEventType.FlashSilhouette);
            CreateFalsePresenceAnchor(anchorsRoot, "Presence_ArchiveSteps", new Vector3(-35f, 0f, 9f), Quaternion.Euler(0f, 90f, 0f), FalsePresenceEventType.DistantFootsteps);
            CreateFalsePresenceAnchor(anchorsRoot, "Presence_ArchiveShadow", new Vector3(-26f, 0f, 4f), Quaternion.Euler(0f, 90f, 0f), FalsePresenceEventType.DoorwayShadow, new Vector3(-20f, 0f, 4f));
            CreateFalsePresenceAnchor(anchorsRoot, "Presence_OperationFlash", new Vector3(27.6f, 0f, 2.4f), Quaternion.Euler(0f, -90f, 0f), FalsePresenceEventType.FlashSilhouette);
            CreateFalsePresenceAnchor(anchorsRoot, "Presence_ServerSteps", new Vector3(30f, 0f, -27f), Quaternion.identity, FalsePresenceEventType.DistantFootsteps);
            CreateFalsePresenceAnchor(anchorsRoot, "Presence_ServiceNoise", new Vector3(-31.8f, 0f, -20f), Quaternion.identity, FalsePresenceEventType.HideoutNoise);
            CreateFalsePresenceAnchor(anchorsRoot, "Presence_MorgueShadow", new Vector3(0f, 0f, -38.8f), Quaternion.identity, FalsePresenceEventType.DoorwayShadow, new Vector3(4f, 0f, -38.8f));
            CreateFalsePresenceAnchor(anchorsRoot, "Presence_CrossFlash", new Vector3(14f, 0f, -18f), Quaternion.Euler(0f, 180f, 0f), FalsePresenceEventType.FlashSilhouette);
        }

        private static void CreateScareMoments(Transform root)
        {
            CreateScareTrigger(root, "ScareTrigger_SecurityExit", new Vector3(0f, 1f, 30f), new Vector3(8f, 2.5f, 4f));
            CreateScareTrigger(root, "ScareTrigger_Morgue", new Vector3(0f, 1f, -38f), new Vector3(8f, 2.5f, 4f));
            CreateScareTrigger(root, "ScareTrigger_Lab", new Vector3(28f, 1f, 4f), new Vector3(6f, 2.5f, 4f));
        }

        private static void CreateScriptedHorrorMoments(Transform root)
        {
            Transform scriptedRoot = new GameObject("ScriptedMoments").transform;
            scriptedRoot.SetParent(root, false);

            CreateScriptedMomentFlash(scriptedRoot, "Moment_FlashSilhouette", new Vector3(0f, 1f, 29.2f), new Vector3(7f, 2.8f, 4f), new Vector3(0f, 0f, 31.2f));
            CreateScriptedMomentCorpse(scriptedRoot, "Moment_CurtainCorpse", new Vector3(-2.6f, 1f, -39.5f), new Vector3(4f, 2.6f, 4f), new Vector3(-3.6f, 0f, -40.6f));
            CreateScriptedMomentShadow(scriptedRoot, "Moment_ArchiveShadow", new Vector3(-27.2f, 1f, 2f), new Vector3(4f, 2.6f, 6f), new Vector3(-28.8f, 0f, 2f), new Vector3(-22.6f, 0f, 2f));
            CreateScriptedMomentDoorSlam(scriptedRoot, "Moment_DoorSlam", new Vector3(-4f, 1f, -16f), new Vector3(5f, 2.6f, 5f), new Vector3(-1.5f, 0f, -16.4f));
        }

        private static void CreateObjectiveSpawnPoints(Transform root)
        {
            Transform spawnRoot = new GameObject("ObjectiveSpawnPoints").transform;
            spawnRoot.SetParent(root, false);

            CreateObjectiveSpawnPoint(spawnRoot, "Generator_A", new Vector3(-37.5f, 0.02f, 34f), Quaternion.Euler(0f, 90f, 0f), ObjectiveSpawnType.Generator);
            CreateObjectiveSpawnPoint(spawnRoot, "Generator_B", new Vector3(37.5f, 0.02f, 34f), Quaternion.Euler(0f, -90f, 0f), ObjectiveSpawnType.Generator);
            CreateObjectiveSpawnPoint(spawnRoot, "Generator_C", new Vector3(-37.5f, 0.02f, 4f), Quaternion.Euler(0f, 90f, 0f), ObjectiveSpawnType.Generator);
            CreateObjectiveSpawnPoint(spawnRoot, "Generator_D", new Vector3(37.5f, 0.02f, 4f), Quaternion.Euler(0f, -90f, 0f), ObjectiveSpawnType.Generator);
            CreateObjectiveSpawnPoint(spawnRoot, "Generator_E", new Vector3(-32f, 0.02f, -30f), Quaternion.Euler(0f, 180f, 0f), ObjectiveSpawnType.Generator);
            CreateObjectiveSpawnPoint(spawnRoot, "Generator_F", new Vector3(32f, 0.02f, -30f), Quaternion.identity, ObjectiveSpawnType.Generator);

            CreateObjectiveSpawnPoint(spawnRoot, "Keycard_A", new Vector3(-31f, 0.82f, 34f), Quaternion.identity, ObjectiveSpawnType.Keycard);
            CreateObjectiveSpawnPoint(spawnRoot, "Keycard_B", new Vector3(31f, 0.82f, 34f), Quaternion.identity, ObjectiveSpawnType.Keycard);
            CreateObjectiveSpawnPoint(spawnRoot, "Keycard_C", new Vector3(-31.8f, 0.82f, 0f), Quaternion.Euler(0f, 90f, 0f), ObjectiveSpawnType.Keycard);
            CreateObjectiveSpawnPoint(spawnRoot, "Keycard_D", new Vector3(31.8f, 0.82f, 0f), Quaternion.Euler(0f, -90f, 0f), ObjectiveSpawnType.Keycard);
            CreateObjectiveSpawnPoint(spawnRoot, "Keycard_E", new Vector3(-5.2f, 0.82f, 45f), Quaternion.Euler(0f, 180f, 0f), ObjectiveSpawnType.Keycard);

            CreateObjectiveSpawnPoint(spawnRoot, "Power_A", new Vector3(-24.6f, 0.55f, -33f), Quaternion.Euler(0f, 180f, 0f), ObjectiveSpawnType.PowerConsole);
            CreateObjectiveSpawnPoint(spawnRoot, "Power_B", new Vector3(27f, 0.55f, -31.8f), Quaternion.identity, ObjectiveSpawnType.PowerConsole);
            CreateObjectiveSpawnPoint(spawnRoot, "Power_C", new Vector3(0f, 0.55f, -18f), Quaternion.Euler(0f, 180f, 0f), ObjectiveSpawnType.PowerConsole);

            CreateObjectiveSpawnPoint(spawnRoot, "Hook_A", new Vector3(-49.2f, 0f, 18f), Quaternion.Euler(0f, 70f, 0f), ObjectiveSpawnType.Hook);
            CreateObjectiveSpawnPoint(spawnRoot, "Hook_B", new Vector3(49.2f, 0f, 18f), Quaternion.Euler(0f, -70f, 0f), ObjectiveSpawnType.Hook);
            CreateObjectiveSpawnPoint(spawnRoot, "Hook_C", new Vector3(-50f, 0f, -46f), Quaternion.Euler(0f, 140f, 0f), ObjectiveSpawnType.Hook);
            CreateObjectiveSpawnPoint(spawnRoot, "Hook_D", new Vector3(50f, 0f, -46f), Quaternion.Euler(0f, -140f, 0f), ObjectiveSpawnType.Hook);
            CreateObjectiveSpawnPoint(spawnRoot, "Hook_E", new Vector3(0f, 0f, -18.4f), Quaternion.Euler(0f, 180f, 0f), ObjectiveSpawnType.Hook);

            CreateObjectiveSpawnPoint(spawnRoot, "Monster_A", new Vector3(0f, 0f, -18f), Quaternion.Euler(0f, 180f, 0f), ObjectiveSpawnType.Monster);
            CreateObjectiveSpawnPoint(spawnRoot, "Monster_B", new Vector3(-35f, 0f, -24f), Quaternion.Euler(0f, 35f, 0f), ObjectiveSpawnType.Monster);
            CreateObjectiveSpawnPoint(spawnRoot, "Monster_C", new Vector3(35f, 0f, -24f), Quaternion.Euler(0f, -35f, 0f), ObjectiveSpawnType.Monster);
            CreateObjectiveSpawnPoint(spawnRoot, "Monster_D", new Vector3(12f, 0f, -10f), Quaternion.Euler(0f, 180f, 0f), ObjectiveSpawnType.Monster);

            CreateObjectiveSpawnPoint(spawnRoot, "Battery_A", new Vector3(-40.4f, 0.01f, 38.4f), Quaternion.identity, ObjectiveSpawnType.Battery);
            CreateObjectiveSpawnPoint(spawnRoot, "Battery_B", new Vector3(40.4f, 0.01f, 38.4f), Quaternion.identity, ObjectiveSpawnType.Battery);
            CreateObjectiveSpawnPoint(spawnRoot, "Battery_C", new Vector3(-28.2f, 0.01f, 9.8f), Quaternion.identity, ObjectiveSpawnType.Battery);
            CreateObjectiveSpawnPoint(spawnRoot, "Battery_D", new Vector3(31.8f, 0.01f, 0f), Quaternion.identity, ObjectiveSpawnType.Battery);
            CreateObjectiveSpawnPoint(spawnRoot, "Battery_E", new Vector3(-35.8f, 0.01f, -32.4f), Quaternion.identity, ObjectiveSpawnType.Battery);
            CreateObjectiveSpawnPoint(spawnRoot, "Battery_F", new Vector3(38.4f, 0.01f, -31.8f), Quaternion.identity, ObjectiveSpawnType.Battery);
            CreateObjectiveSpawnPoint(spawnRoot, "Battery_G", new Vector3(-5.4f, 0.01f, -45f), Quaternion.identity, ObjectiveSpawnType.Battery);
            CreateObjectiveSpawnPoint(spawnRoot, "Battery_H", new Vector3(5.2f, 0.01f, 45f), Quaternion.identity, ObjectiveSpawnType.Battery);
        }

        private static void PlaceGameplayObjects(PrototypeAssets assets, Transform root)
        {
            Transform gameplayRoot = new GameObject("GameplayObjects").transform;
            gameplayRoot.SetParent(root, false);
            Transform monsterSpawn = new GameObject("MonsterSpawnPoint").transform;
            monsterSpawn.SetParent(gameplayRoot, false);
            monsterSpawn.localPosition = new Vector3(0f, 0f, -18f);

            InstantiatePrefabAt(assets.GeneratorPrefab, gameplayRoot, new Vector3(-37.5f, 0.02f, 34f), Quaternion.Euler(0f, 90f, 0f));
            InstantiatePrefabAt(assets.GeneratorPrefab, gameplayRoot, new Vector3(32f, 0.02f, -30f), Quaternion.identity);
            InstantiatePrefabAt(assets.KeycardPrefab, gameplayRoot, new Vector3(-5.2f, 0.82f, 45f), Quaternion.Euler(0f, 180f, 0f));
            InstantiatePrefabAt(assets.PowerConsolePrefab, gameplayRoot, new Vector3(-24.6f, 0.55f, -33f), Quaternion.Euler(0f, 180f, 0f));

            InstantiatePrefabAt(assets.ExitDoorPrefab, gameplayRoot, new Vector3(0f, 0f, 52.2f), Quaternion.identity);

            GameObject doorA = InstantiatePrefabAt(assets.DoorPrefab, gameplayRoot, new Vector3(0f, 0f, 35f), Quaternion.identity);
            if (doorA != null && doorA.TryGetComponent(out NetworkDoor doorAState))
            {
                SetBool(doorAState, "startLocked", true);
                SetBool(doorAState, "requiresKeycard", true);
            }

            GameObject doorB = InstantiatePrefabAt(assets.DoorPrefab, gameplayRoot, new Vector3(0f, 0f, -36f), Quaternion.Euler(0f, 180f, 0f));
            if (doorB != null && doorB.TryGetComponent(out NetworkDoor doorBState))
            {
                SetBool(doorBState, "startLocked", true);
                SetBool(doorBState, "requiresPower", true);
            }

            InstantiatePrefabAt(assets.DoorPrefab, gameplayRoot, new Vector3(-25f, 0f, 34f), Quaternion.Euler(0f, 90f, 0f));
            InstantiatePrefabAt(assets.DoorPrefab, gameplayRoot, new Vector3(25f, 0f, 34f), Quaternion.Euler(0f, -90f, 0f));
            InstantiatePrefabAt(assets.DoorPrefab, gameplayRoot, new Vector3(-25f, 0f, 4f), Quaternion.Euler(0f, 90f, 0f));
            InstantiatePrefabAt(assets.DoorPrefab, gameplayRoot, new Vector3(25f, 0f, 4f), Quaternion.Euler(0f, -90f, 0f));
            InstantiatePrefabAt(assets.DoorPrefab, gameplayRoot, new Vector3(-21f, 0f, -28f), Quaternion.Euler(0f, 90f, 0f));
            InstantiatePrefabAt(assets.DoorPrefab, gameplayRoot, new Vector3(21f, 0f, -28f), Quaternion.Euler(0f, -90f, 0f));

            InstantiatePrefabAt(assets.BatteryPrefab, gameplayRoot, new Vector3(-40.4f, 0.01f, 38.4f), Quaternion.identity);
            InstantiatePrefabAt(assets.BatteryPrefab, gameplayRoot, new Vector3(40.4f, 0.01f, 38.4f), Quaternion.identity);
            InstantiatePrefabAt(assets.BatteryPrefab, gameplayRoot, new Vector3(-28.2f, 0.01f, 9.8f), Quaternion.identity);
            InstantiatePrefabAt(assets.BatteryPrefab, gameplayRoot, new Vector3(31.8f, 0.01f, 0f), Quaternion.identity);
            InstantiatePrefabAt(assets.BatteryPrefab, gameplayRoot, new Vector3(-35.8f, 0.01f, -32.4f), Quaternion.identity);
            InstantiatePrefabAt(assets.BatteryPrefab, gameplayRoot, new Vector3(38.4f, 0.01f, -31.8f), Quaternion.identity);
            InstantiatePrefabAt(assets.BatteryPrefab, gameplayRoot, new Vector3(-5.4f, 0.01f, -45f), Quaternion.identity);
            InstantiatePrefabAt(assets.BatteryPrefab, gameplayRoot, new Vector3(5.2f, 0.01f, 45f), Quaternion.identity);

            InstantiatePrefabAt(assets.HookPrefab, gameplayRoot, new Vector3(-49.2f, 0f, 18f), Quaternion.Euler(0f, 70f, 0f));
            InstantiatePrefabAt(assets.HookPrefab, gameplayRoot, new Vector3(49.2f, 0f, 18f), Quaternion.Euler(0f, -70f, 0f));
            InstantiatePrefabAt(assets.HookPrefab, gameplayRoot, new Vector3(-50f, 0f, -46f), Quaternion.Euler(0f, 140f, 0f));
            InstantiatePrefabAt(assets.HookPrefab, gameplayRoot, new Vector3(50f, 0f, -46f), Quaternion.Euler(0f, -140f, 0f));
            InstantiatePrefabAt(assets.HookPrefab, gameplayRoot, new Vector3(0f, 0f, -18.4f), Quaternion.Euler(0f, 180f, 0f));

            InstantiatePrefabAt(assets.HideLockerPrefab, gameplayRoot, new Vector3(-41.6f, 0f, 29.8f), Quaternion.Euler(0f, 90f, 0f));
            InstantiatePrefabAt(assets.HideLockerPrefab, gameplayRoot, new Vector3(41.6f, 0f, 29.8f), Quaternion.Euler(0f, -90f, 0f));
            InstantiatePrefabAt(assets.HideLockerPrefab, gameplayRoot, new Vector3(-41.6f, 0f, -5.8f), Quaternion.Euler(0f, 90f, 0f));
            InstantiatePrefabAt(assets.HideLockerPrefab, gameplayRoot, new Vector3(41.6f, 0f, -5.8f), Quaternion.Euler(0f, -90f, 0f));
            InstantiatePrefabAt(assets.HideCratePrefab, gameplayRoot, new Vector3(-7.2f, 0f, 26.8f), Quaternion.Euler(0f, 180f, 0f));
            InstantiatePrefabAt(assets.HideCratePrefab, gameplayRoot, new Vector3(7.2f, 0f, -16.4f), Quaternion.identity);
            InstantiatePrefabAt(assets.HideCurtainPrefab, gameplayRoot, new Vector3(-43.2f, 0f, -36f), Quaternion.Euler(0f, 90f, 0f));
            InstantiatePrefabAt(assets.HideCurtainPrefab, gameplayRoot, new Vector3(43.2f, 0f, -36f), Quaternion.Euler(0f, -90f, 0f));
            InstantiatePrefabAt(assets.HideCratePrefab, gameplayRoot, new Vector3(-31.4f, 0f, 12.4f), Quaternion.Euler(0f, 90f, 0f));
            InstantiatePrefabAt(assets.HideCratePrefab, gameplayRoot, new Vector3(31.6f, 0f, 7.2f), Quaternion.Euler(0f, -90f, 0f));
            InstantiatePrefabAt(assets.HideLockerPrefab, gameplayRoot, new Vector3(23.8f, 0f, -31.2f), Quaternion.identity);
            InstantiatePrefabAt(assets.HideCurtainPrefab, gameplayRoot, new Vector3(0f, 0f, -40.2f), Quaternion.identity);

            InstantiatePrefabAt(assets.MonsterPrefab, gameplayRoot, monsterSpawn.localPosition, monsterSpawn.localRotation);
        }
        private static void CreateHudCanvas()
        {
            Canvas canvas = CreateCanvas("HUDCanvas");
            HudController hudController = canvas.gameObject.AddComponent<HudController>();
            RectTransform rootRect = canvas.GetComponent<RectTransform>();

            Image stressOverlay = CreatePanel(rootRect, "StressOverlay", new Color(0.16f, 0.02f, 0.03f, 0f));
            StretchToRect(stressOverlay.rectTransform, Vector2.zero, Vector2.one);
            stressOverlay.raycastTarget = false;

            Image revealFlashOverlay = CreatePanel(rootRect, "RevealFlashOverlay", new Color(0.82f, 0.88f, 0.95f, 0f));
            StretchToRect(revealFlashOverlay.rectTransform, Vector2.zero, Vector2.one);
            revealFlashOverlay.raycastTarget = false;

            Text objectivesText = CreateText(rootRect, "ObjectivesText", "Objectives", 20, TextAnchor.UpperLeft);
            StretchToRect(objectivesText.rectTransform, new Vector2(0.02f, 0.76f), new Vector2(0.32f, 0.98f));

            Text roundResultText = CreateText(rootRect, "RoundResult", string.Empty, 48, TextAnchor.MiddleCenter);
            roundResultText.color = new Color(0.95f, 0.25f, 0.25f);
            StretchToRect(roundResultText.rectTransform, new Vector2(0.2f, 0.45f), new Vector2(0.8f, 0.58f));

            Text roundSummaryText = CreateText(rootRect, "RoundSummary", string.Empty, 18, TextAnchor.UpperCenter);
            roundSummaryText.color = new Color(0.92f, 0.9f, 0.86f, 0.96f);
            StretchToRect(roundSummaryText.rectTransform, new Vector2(0.24f, 0.18f), new Vector2(0.76f, 0.43f));

            Text interactionText = CreateText(rootRect, "InteractionText", string.Empty, 24, TextAnchor.MiddleCenter);
            StretchToRect(interactionText.rectTransform, new Vector2(0.3f, 0.18f), new Vector2(0.7f, 0.24f));

            Slider holdSlider = CreateSlider(rootRect, "InteractionHoldSlider");
            StretchToRect(holdSlider.GetComponent<RectTransform>(), new Vector2(0.34f, 0.14f), new Vector2(0.66f, 0.17f));

            Slider staminaSlider = CreateSlider(rootRect, "StaminaSlider");
            StretchToRect(staminaSlider.GetComponent<RectTransform>(), new Vector2(0.02f, 0.06f), new Vector2(0.26f, 0.09f));
            AddLabelAbove(staminaSlider.transform, "Stamina");

            Slider batterySlider = CreateSlider(rootRect, "BatterySlider");
            StretchToRect(batterySlider.GetComponent<RectTransform>(), new Vector2(0.02f, 0.02f), new Vector2(0.26f, 0.05f));
            AddLabelAbove(batterySlider.transform, "Flashlight");

            Text statusText = CreateText(rootRect, "PlayerStatusText", "State: Healthy", 18, TextAnchor.MiddleLeft);
            StretchToRect(statusText.rectTransform, new Vector2(0.28f, 0.02f), new Vector2(0.48f, 0.06f));

            Text abilityText = CreateText(rootRect, "AbilityText", "Q: Steady Hands ready", 18, TextAnchor.MiddleLeft);
            abilityText.color = new Color(0.88f, 0.9f, 0.94f, 0.94f);
            StretchToRect(abilityText.rectTransform, new Vector2(0.28f, 0.06f), new Vector2(0.5f, 0.1f));

            Slider hookTimer = CreateSlider(rootRect, "HookTimerSlider");
            StretchToRect(hookTimer.GetComponent<RectTransform>(), new Vector2(0.36f, 0.08f), new Vector2(0.64f, 0.11f));
            AddLabelAbove(hookTimer.transform, "Hook Timer");
            hookTimer.gameObject.SetActive(false);

            Image hookWheelPanel = CreatePanel(rootRect, "HookWheelPanel", new Color(0.02f, 0.025f, 0.03f, 0.86f));
            StretchToRect(hookWheelPanel.rectTransform, new Vector2(0.365f, 0.12f), new Vector2(0.635f, 0.31f));
            hookWheelPanel.gameObject.SetActive(false);
            Image hookWheelAccent = CreatePanel(hookWheelPanel.transform, "HookWheelAccent", new Color(0.55f, 0.08f, 0.08f, 0.82f));
            StretchToRect(hookWheelAccent.rectTransform, new Vector2(0f, 0.92f), new Vector2(1f, 1f));
            Text hookWheelNorth = CreateText(hookWheelPanel.transform, "HookWheelNorth", "BREAK", 15, TextAnchor.MiddleCenter);
            StretchToRect(hookWheelNorth.rectTransform, new Vector2(0.36f, 0.74f), new Vector2(0.64f, 0.9f));
            Text hookWheelSouth = CreateText(hookWheelPanel.transform, "HookWheelSouth", "BLOOD", 15, TextAnchor.MiddleCenter);
            StretchToRect(hookWheelSouth.rectTransform, new Vector2(0.34f, 0.08f), new Vector2(0.66f, 0.2f));
            Text hookWheelWest = CreateText(hookWheelPanel.transform, "HookWheelWest", "RUST", 15, TextAnchor.MiddleCenter);
            StretchToRect(hookWheelWest.rectTransform, new Vector2(0.08f, 0.38f), new Vector2(0.26f, 0.62f));
            Text hookWheelEast = CreateText(hookWheelPanel.transform, "HookWheelEast", "BLOOD", 15, TextAnchor.MiddleCenter);
            StretchToRect(hookWheelEast.rectTransform, new Vector2(0.74f, 0.38f), new Vector2(0.92f, 0.62f));
            Image hookWheelPointer = CreatePanel(hookWheelPanel.transform, "HookWheelPointer", new Color(0.96f, 0.9f, 0.82f, 0.98f));
            RectTransform hookWheelPointerRect = hookWheelPointer.rectTransform;
            hookWheelPointerRect.anchorMin = new Vector2(0.5f, 0.5f);
            hookWheelPointerRect.anchorMax = new Vector2(0.5f, 0.5f);
            hookWheelPointerRect.sizeDelta = new Vector2(10f, 84f);
            hookWheelPointerRect.anchoredPosition = Vector2.zero;
            hookWheelPointerRect.localRotation = Quaternion.identity;
            Text hookWheelText = CreateText(hookWheelPanel.transform, "HookWheelText", "F: SPIN FOR 25% ESCAPE", 16, TextAnchor.MiddleCenter);
            hookWheelText.color = new Color(0.92f, 0.88f, 0.82f, 0.98f);
            StretchToRect(hookWheelText.rectTransform, new Vector2(0.16f, 0.28f), new Vector2(0.84f, 0.42f));

            Text teammatesText = CreateText(rootRect, "TeammatesText", "Team", 17, TextAnchor.UpperLeft);
            teammatesText.color = new Color(0.9f, 0.9f, 0.92f, 0.92f);
            StretchToRect(teammatesText.rectTransform, new Vector2(0.78f, 0.72f), new Vector2(0.97f, 0.96f));

            SetReference(hudController, "objectivesText", objectivesText);
            SetReference(hudController, "roundResultText", roundResultText);
            SetReference(hudController, "roundSummaryText", roundSummaryText);
            SetReference(hudController, "interactionText", interactionText);
            SetReference(hudController, "interactionHoldSlider", holdSlider);
            SetReference(hudController, "abilityText", abilityText);
            SetReference(hudController, "staminaSlider", staminaSlider);
            SetReference(hudController, "batterySlider", batterySlider);
            SetReference(hudController, "statusText", statusText);
            SetReference(hudController, "hookTimerSlider", hookTimer);
            SetReference(hudController, "hookWheelRoot", hookWheelPanel.rectTransform);
            SetReference(hudController, "hookWheelPointer", hookWheelPointerRect);
            SetReference(hudController, "hookWheelText", hookWheelText);
            SetReference(hudController, "teammatesText", teammatesText);
            SetReference(hudController, "stressOverlay", stressOverlay);
            SetReference(hudController, "revealFlashOverlay", revealFlashOverlay);
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private static Canvas CreateCanvas(string name)
        {
            GameObject canvasObject = new GameObject(name);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<GraphicRaycaster>();
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static Image CreatePanel(Transform parent, string name, Color color)
        {
            GameObject panelObject = new GameObject(name);
            panelObject.transform.SetParent(parent, false);
            Image image = panelObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text CreateText(Transform parent, string name, string text, int fontSize, TextAnchor alignment)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            Text uiText = textObject.AddComponent<Text>();
            uiText.font = GetDefaultFont();
            uiText.text = text;
            uiText.fontSize = fontSize;
            uiText.alignment = alignment;
            uiText.color = Color.white;
            return uiText;
        }

        private static Button CreateButton(Transform parent, string name, string label)
        {
            GameObject buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.14f, 0.02f, 0.03f, 0.96f);
            Button button = buttonObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.14f, 0.02f, 0.03f, 0.96f);
            colors.highlightedColor = new Color(0.26f, 0.05f, 0.07f, 1f);
            colors.pressedColor = new Color(0.09f, 0.015f, 0.02f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.08f, 0.08f, 0.08f, 0.54f);
            button.colors = colors;

            Text labelText = CreateText(buttonObject.transform, "Label", label, 24, TextAnchor.MiddleCenter);
            labelText.color = new Color(0.96f, 0.92f, 0.86f);
            StretchToRect(labelText.rectTransform, Vector2.zero, Vector2.one);
            return button;
        }

        private static InputField CreateInputField(Transform parent, string name, string initialText)
        {
            return CreateInputField(parent, name, initialText, "Enter value");
        }

        private static InputField CreateInputField(Transform parent, string name, string initialText, string placeholderText)
        {
            GameObject inputObject = new GameObject(name);
            inputObject.transform.SetParent(parent, false);
            Image background = inputObject.AddComponent<Image>();
            background.color = new Color(0.08f, 0.02f, 0.025f, 0.95f);
            InputField inputField = inputObject.AddComponent<InputField>();

            Text text = CreateText(inputObject.transform, "Text", initialText, 22, TextAnchor.MiddleLeft);
            text.color = new Color(0.95f, 0.93f, 0.9f);
            StretchToRect(text.rectTransform, new Vector2(0.04f, 0.1f), new Vector2(0.96f, 0.9f));

            Text placeholder = CreateText(inputObject.transform, "Placeholder", placeholderText, 20, TextAnchor.MiddleLeft);
            placeholder.color = new Color(0.82f, 0.74f, 0.72f, 0.66f);
            StretchToRect(placeholder.rectTransform, new Vector2(0.04f, 0.1f), new Vector2(0.96f, 0.9f));

            inputField.textComponent = text;
            inputField.placeholder = placeholder;
            inputField.text = initialText;

            return inputField;
        }

        private static Slider CreateSlider(Transform parent, string name)
        {
            GameObject sliderObject = new GameObject(name);
            sliderObject.transform.SetParent(parent, false);
            Image background = sliderObject.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.65f);
            Slider slider = sliderObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(sliderObject.transform, false);
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.78f, 0.12f, 0.12f, 0.92f);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            StretchToRect(fillRect, new Vector2(0.01f, 0.1f), new Vector2(0.99f, 0.9f));
            slider.fillRect = fillRect;
            slider.targetGraphic = fillImage;

            return slider;
        }

        private static Toggle CreateToggle(Transform parent, string name, string label)
        {
            GameObject toggleObject = new GameObject(name);
            toggleObject.transform.SetParent(parent, false);
            Toggle toggle = toggleObject.AddComponent<Toggle>();

            GameObject backgroundObject = new GameObject("Background");
            backgroundObject.transform.SetParent(toggleObject.transform, false);
            Image background = backgroundObject.AddComponent<Image>();
            background.color = new Color(0.11f, 0.11f, 0.12f, 0.94f);
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            StretchToRect(backgroundRect, new Vector2(0f, 0.25f), new Vector2(0.22f, 0.75f));

            GameObject checkmarkObject = new GameObject("Checkmark");
            checkmarkObject.transform.SetParent(backgroundObject.transform, false);
            Image checkmark = checkmarkObject.AddComponent<Image>();
            checkmark.color = new Color(0.8f, 0.12f, 0.12f, 0.96f);
            RectTransform checkmarkRect = checkmark.GetComponent<RectTransform>();
            StretchToRect(checkmarkRect, new Vector2(0.16f, 0.16f), new Vector2(0.84f, 0.84f));

            Text labelText = CreateText(toggleObject.transform, "Label", label, 14, TextAnchor.MiddleLeft);
            labelText.color = new Color(0.92f, 0.88f, 0.82f);
            StretchToRect(labelText.rectTransform, new Vector2(0.28f, 0f), new Vector2(1f, 1f));

            toggle.targetGraphic = background;
            toggle.graphic = checkmark;
            return toggle;
        }

        private static void AddLabelAbove(Transform target, string text)
        {
            RectTransform targetRect = target as RectTransform;
            if (targetRect == null)
            {
                return;
            }

            Text label = CreateText(target, $"{target.name}_Label", text, 14, TextAnchor.LowerLeft);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.pivot = new Vector2(0f, 0f);
            labelRect.sizeDelta = new Vector2(0f, 18f);
            labelRect.anchoredPosition = new Vector2(0f, 4f);
            label.color = new Color(0.86f, 0.84f, 0.82f, 0.9f);
        }

        private static void StretchToRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private static void CreateDirectionalLight(Color color, float intensity, Vector3 eulerAngles)
        {
            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.78f;
            light.shadowBias = 0.03f;
            light.shadowNormalBias = 0.45f;
            lightObject.transform.rotation = Quaternion.Euler(eulerAngles);
        }

        private static void ConfigureHospitalRenderSettings()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.028f, 0.03f, 0.035f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.033f;
            RenderSettings.fogColor = new Color(0.032f, 0.035f, 0.042f);
            RenderSettings.reflectionIntensity = 0.28f;
        }

        private static GameObject CreateCube(
            Transform parent,
            string name,
            Vector3 localPosition,
            Vector3 localScale,
            Color color,
            string style = "default")
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPosition;
            cube.transform.localScale = localScale;
            Renderer renderer = cube.GetComponent<Renderer>();
            renderer.sharedMaterial = CreateScaledMaterial(color, style, localScale);
            return cube;
        }

        private static void CreatePointLight(Transform parent, string name, Vector3 position, float range, float intensity, bool flicker)
        {
            GameObject lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.localPosition = position;

            Light pointLight = lightObject.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.range = range;
            pointLight.intensity = intensity;
            pointLight.color = new Color(0.92f, 0.95f, 1f);
            pointLight.shadows = LightShadows.Soft;

            if (flicker)
            {
                FlickerLight flickerLight = lightObject.AddComponent<FlickerLight>();
                flickerLight.SetBaseIntensity(intensity);
            }
        }

        private static Light CreateColoredPointLight(Transform parent, string name, Vector3 position, float range, float intensity, Color color, bool flicker)
        {
            GameObject lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.localPosition = position;

            Light pointLight = lightObject.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.range = range;
            pointLight.intensity = intensity;
            pointLight.color = color;
            pointLight.shadows = LightShadows.Soft;

            if (flicker)
            {
                FlickerLight flickerLight = lightObject.AddComponent<FlickerLight>();
                flickerLight.SetBaseIntensity(intensity);
            }

            return pointLight;
        }

        private static void CreateWorldWarningText(Transform parent, string name, string text, Vector3 position, Quaternion rotation, Color color)
        {
            // Intentionally disabled.
            // World-space text bled through sight-lines and broke spatial readability.
        }

        private static void CreatePipeRun(Transform parent, string name, Vector3 position, float length, bool alongX)
        {
            GameObject pipe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pipe.name = name;
            pipe.transform.SetParent(parent, false);
            pipe.transform.localPosition = position;
            pipe.transform.localRotation = alongX ? Quaternion.Euler(0f, 0f, 90f) : Quaternion.Euler(90f, 0f, 0f);
            pipe.transform.localScale = alongX
                ? new Vector3(0.18f, length * 0.5f, 0.18f)
                : new Vector3(0.18f, length * 0.5f, 0.18f);
            Object.DestroyImmediate(pipe.GetComponent<Collider>());
            pipe.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.18f, 0.18f, 0.2f), "metal");
        }

        private static void CreateBloodStain(Transform parent, string name, Vector3 position, Vector2 size, float yaw)
        {
            GameObject stain = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stain.name = name;
            stain.transform.SetParent(parent, false);
            stain.transform.localPosition = position;
            stain.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            stain.transform.localScale = new Vector3(Mathf.Max(0.1f, size.x), 0.01f, Mathf.Max(0.1f, size.y));
            Object.DestroyImmediate(stain.GetComponent<Collider>());
            Renderer renderer = stain.GetComponent<Renderer>();
            renderer.sharedMaterial = CreateMaterial(new Color(0.23f, 0.04f, 0.04f), "blood");
        }

        private static void CreateWallBloodStreak(Transform parent, string name, Vector3 position, Vector2 size, float yaw)
        {
            GameObject streak = GameObject.CreatePrimitive(PrimitiveType.Cube);
            streak.name = name;
            streak.transform.SetParent(parent, false);
            streak.transform.localPosition = position;
            streak.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            streak.transform.localScale = new Vector3(Mathf.Max(0.1f, size.x), Mathf.Max(0.1f, size.y), 0.03f);
            Object.DestroyImmediate(streak.GetComponent<Collider>());
            Renderer renderer = streak.GetComponent<Renderer>();
            renderer.sharedMaterial = CreateMaterial(new Color(0.2f, 0.035f, 0.035f), "blood");
        }

        private static void CreateScareTrigger(Transform parent, string name, Vector3 position, Vector3 triggerSize)
        {
            GameObject triggerObject = new GameObject(name);
            triggerObject.transform.SetParent(parent, false);
            triggerObject.transform.localPosition = position;

            BoxCollider triggerCollider = triggerObject.AddComponent<BoxCollider>();
            triggerCollider.isTrigger = true;
            triggerCollider.size = triggerSize;

            AudioSource audioSource = triggerObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.volume = 0.8f;

            ScareTrigger scareTrigger = triggerObject.AddComponent<ScareTrigger>();
            SetReference(scareTrigger, "audioSource", audioSource);
            SetReference(scareTrigger, "triggerClip", LoadAudioClipAsset(ScareStingerClipPath));
        }

        private static void CreateFalsePresenceAnchor(
            Transform parent,
            string name,
            Vector3 position,
            Quaternion rotation,
            FalsePresenceEventType eventType,
            Vector3? secondaryPoint = null)
        {
            GameObject anchorObject = new GameObject(name);
            anchorObject.transform.SetParent(parent, false);
            anchorObject.transform.localPosition = position;
            anchorObject.transform.localRotation = rotation;

            FalsePresenceAnchor anchor = anchorObject.AddComponent<FalsePresenceAnchor>();
            SetEnum(anchor, "eventType", eventType);
            if (secondaryPoint.HasValue)
            {
                Transform point = new GameObject("SecondaryPoint").transform;
                point.SetParent(anchorObject.transform, false);
                point.localPosition = secondaryPoint.Value - position;
                SetReference(anchor, "secondaryPoint", point);
            }
        }

        private static void CreateScriptedMomentFlash(Transform parent, string name, Vector3 position, Vector3 size, Vector3 silhouettePosition)
        {
            GameObject momentObject = CreateScriptedMomentBase(parent, name, position, size, ScriptedHorrorMomentType.FlashSilhouette);
            Transform secondary = new GameObject("SilhouettePoint").transform;
            secondary.SetParent(momentObject.transform, false);
            secondary.localPosition = silhouettePosition - position;
            secondary.localRotation = Quaternion.Euler(0f, 180f, 0f);
            ScriptedHorrorMoment moment = momentObject.GetComponent<ScriptedHorrorMoment>();
            SetReference(moment, "secondaryPoint", secondary);
        }

        private static void CreateScriptedMomentCorpse(Transform parent, string name, Vector3 position, Vector3 size, Vector3 corpsePosition)
        {
            GameObject momentObject = CreateScriptedMomentBase(parent, name, position, size, ScriptedHorrorMomentType.RevealCorpse);

            GameObject corpse = new GameObject("HiddenCorpse");
            corpse.transform.SetParent(momentObject.transform, false);
            corpse.transform.localPosition = corpsePosition - position;
            CreateCube(corpse.transform, "CorpseBody", new Vector3(0f, 0.42f, 0f), new Vector3(1.8f, 0.28f, 0.62f), new Color(0.22f, 0.22f, 0.24f), "fabric");
            CreateCube(corpse.transform, "CorpseHead", new Vector3(0.82f, 0.52f, 0f), new Vector3(0.26f, 0.22f, 0.2f), new Color(0.34f, 0.3f, 0.28f), "default");
            CreateBloodStain(corpse.transform, "CorpseBlood", new Vector3(-0.3f, 0.02f, 0f), new Vector2(1.4f, 0.5f), 0f);
            corpse.SetActive(false);

            Light corpseLight = CreateColoredPointLight(momentObject.transform, "CorpseLight", corpsePosition - position + new Vector3(0f, 1.8f, 0f), 7f, 0.16f, new Color(0.74f, 0.08f, 0.08f), false);
            ScriptedHorrorMoment moment = momentObject.GetComponent<ScriptedHorrorMoment>();
            SetReference(moment, "revealObject", corpse);
            SetReference(moment, "linkedLight", corpseLight);
        }

        private static void CreateScriptedMomentShadow(Transform parent, string name, Vector3 position, Vector3 size, Vector3 startPosition, Vector3 endPosition)
        {
            GameObject momentObject = CreateScriptedMomentBase(parent, name, position, size, ScriptedHorrorMomentType.ShadowPass);
            Transform start = new GameObject("StartPoint").transform;
            start.SetParent(momentObject.transform, false);
            start.localPosition = startPosition - position;
            Transform secondary = new GameObject("SecondaryPoint").transform;
            secondary.SetParent(momentObject.transform, false);
            secondary.localPosition = endPosition - position;
            ScriptedHorrorMoment moment = momentObject.GetComponent<ScriptedHorrorMoment>();
            SetReference(moment, "startPoint", start);
            SetReference(moment, "secondaryPoint", secondary);
        }

        private static void CreateScriptedMomentDoorSlam(Transform parent, string name, Vector3 position, Vector3 size, Vector3 doorPosition)
        {
            GameObject momentObject = CreateScriptedMomentBase(parent, name, position, size, ScriptedHorrorMomentType.DoorSlam);

            GameObject doorPanel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorPanel.name = "SlamDoorPanel";
            doorPanel.transform.SetParent(momentObject.transform, false);
            doorPanel.transform.localPosition = doorPosition - position + new Vector3(0f, 1.55f, 0f);
            doorPanel.transform.localScale = new Vector3(0.12f, 3.1f, 1.6f);
            doorPanel.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(0.2f, 0.18f, 0.16f), "wood");
            Object.DestroyImmediate(doorPanel.GetComponent<Collider>());

            Light slamLight = CreateColoredPointLight(momentObject.transform, "SlamLight", doorPosition - position + new Vector3(0f, 2.2f, 0f), 6f, 0.12f, new Color(0.8f, 0.12f, 0.12f), false);
            ScriptedHorrorMoment moment = momentObject.GetComponent<ScriptedHorrorMoment>();
            SetReference(moment, "doorPanel", doorPanel.transform);
            SetReference(moment, "linkedLight", slamLight);
            SetVector3(moment, "doorClosedEuler", new Vector3(0f, 0f, 0f));
            SetVector3(moment, "doorOpenedEuler", new Vector3(0f, 92f, 0f));
        }

        private static GameObject CreateScriptedMomentBase(Transform parent, string name, Vector3 position, Vector3 size, ScriptedHorrorMomentType type)
        {
            GameObject momentObject = new GameObject(name);
            momentObject.transform.SetParent(parent, false);
            momentObject.transform.localPosition = position;

            BoxCollider triggerCollider = momentObject.AddComponent<BoxCollider>();
            triggerCollider.isTrigger = true;
            triggerCollider.size = size;

            AudioSource audioSource = momentObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.volume = 0.86f;

            ScriptedHorrorMoment moment = momentObject.AddComponent<ScriptedHorrorMoment>();
            SetEnum(moment, "momentType", type);
            SetReference(moment, "audioSource", audioSource);
            SetReference(moment, "triggerClip", LoadAudioClipAsset(ScareStingerClipPath));
            return momentObject;
        }

        private static void CreateObjectiveSpawnPoint(Transform parent, string name, Vector3 position, Quaternion rotation, ObjectiveSpawnType spawnType)
        {
            GameObject point = new GameObject(name);
            point.transform.SetParent(parent, false);
            point.transform.localPosition = position;
            point.transform.localRotation = rotation;
            ObjectiveSpawnPoint spawnPoint = point.AddComponent<ObjectiveSpawnPoint>();
            spawnPoint.SetType(spawnType);
        }

        private static void CreatePlayerSpawn(Transform parent, string name, Vector3 position, Quaternion rotation, int index)
        {
            GameObject spawn = new GameObject(name);
            spawn.transform.SetParent(parent, false);
            spawn.transform.localPosition = position;
            spawn.transform.localRotation = rotation;
            PlayerSpawnPoint spawnPoint = spawn.AddComponent<PlayerSpawnPoint>();
            spawnPoint.SetIndex(index);
        }

        private static void CreateLobbyStartPosition(Transform parent, string name, Vector3 position, Quaternion rotation)
        {
            GameObject spawn = new GameObject(name);
            spawn.transform.SetParent(parent, false);
            spawn.transform.localPosition = position;
            spawn.transform.localRotation = rotation;
            spawn.AddComponent<NetworkStartPosition>();
        }

        private static void CreatePatrolPoint(Transform parent, string name, Vector3 position)
        {
            GameObject point = new GameObject(name);
            point.transform.SetParent(parent, false);
            point.transform.localPosition = position;
            point.AddComponent<PatrolPoint>();
        }

        private static GameObject InstantiatePrefabAt(GameObject prefab, Transform parent, Vector3 localPosition, Quaternion localRotation)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                return null;
            }

            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = localRotation;
            return instance;
        }

        private static GameObject InstantiateStyledModel(
            Transform parent,
            string assetPath,
            string instanceName,
            Vector3 localPosition,
            Quaternion localRotation,
            float targetHeight,
            Material overrideMaterial,
            bool stripColliders,
            bool alignBaseToGround = false)
        {
            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (modelAsset == null)
            {
                return null;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
            if (instance == null)
            {
                instance = Object.Instantiate(modelAsset);
            }

            instance.name = instanceName;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = localRotation;
            instance.transform.localScale = Vector3.one;

            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (targetHeight > 0.05f && renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }

                float currentHeight = Mathf.Max(0.01f, bounds.size.y);
                float scale = targetHeight / currentHeight;
                instance.transform.localScale = Vector3.one * scale;
            }

            if (alignBaseToGround)
            {
                Renderer[] alignedRenderers = instance.GetComponentsInChildren<Renderer>(true);
                if (alignedRenderers.Length > 0)
                {
                    Bounds bounds = alignedRenderers[0].bounds;
                    for (int i = 1; i < alignedRenderers.Length; i++)
                    {
                        bounds.Encapsulate(alignedRenderers[i].bounds);
                    }

                    float desiredBaseY = parent.TransformPoint(localPosition).y;
                    instance.transform.position += Vector3.up * (desiredBaseY - bounds.min.y);
                }
            }

            if (overrideMaterial != null)
            {
                foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.sharedMaterial = overrideMaterial;
                }
            }

            if (stripColliders)
            {
                foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
                {
                    Object.DestroyImmediate(collider);
                }
            }

            return instance;
        }

        private static void PlaceAssetProp(
            Transform parent,
            string assetPath,
            string instanceName,
            Vector3 localPosition,
            Quaternion localRotation,
            float targetHeight,
            string style)
        {
            Material material = CreateMaterial(ResolveVariantTint(instanceName, new Color(0.18f, 0.18f, 0.2f), style), style);
            GameObject prop = InstantiateStyledModel(parent, assetPath, instanceName, localPosition, localRotation, targetHeight, material, true, true);
            if (prop == null)
            {
                return;
            }

            Renderer[] renderers = prop.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return;
            }

            if (style == "blood")
            {
                return;
            }

            if (ShouldValidatePropPlacement(localPosition, targetHeight))
            {
                FinalizeDecorPlacement(prop);
            }
        }

        private static void PlaceRawAssetProp(
            Transform parent,
            string assetPath,
            string instanceName,
            Vector3 localPosition,
            Quaternion localRotation,
            float targetHeight)
        {
            GameObject prop = InstantiateStyledModel(parent, assetPath, instanceName, localPosition, localRotation, targetHeight, null, true, true);
            if (prop == null)
            {
                return;
            }

            foreach (Renderer renderer in prop.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
        }

        private static void PlaceTexturedAssetProp(
            Transform parent,
            string assetPath,
            string textureAssetPath,
            string instanceName,
            Vector3 localPosition,
            Quaternion localRotation,
            float targetHeight,
            string style,
            Color tint)
        {
            Material material = CreateTexturedMaterial(textureAssetPath, ResolveVariantTint(instanceName, tint, style), style);
            GameObject prop = InstantiateStyledModel(parent, assetPath, instanceName, localPosition, localRotation, targetHeight, material, true, true);
            if (prop == null)
            {
                return;
            }

            Renderer[] renderers = prop.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return;
            }

            if (ShouldValidatePropPlacement(localPosition, targetHeight))
            {
                FinalizeDecorPlacement(prop);
            }
        }

        private static bool ShouldValidatePropPlacement(Vector3 localPosition, float targetHeight)
        {
            return localPosition.y <= 0.08f && targetHeight >= 0.8f;
        }

        private static void FinalizeDecorPlacement(GameObject prop)
        {
            if (prop == null || !TryCalculateRendererBounds(prop, out Bounds bounds))
            {
                return;
            }

            Vector3 clearance = new Vector3(
                Mathf.Max(0.12f, bounds.extents.x * 0.92f),
                Mathf.Max(0.26f, bounds.extents.y * 0.94f),
                Mathf.Max(0.12f, bounds.extents.z * 0.92f));

            if (!PlacementSafety.TryResolvePlacement(
                    prop.transform.position,
                    prop.transform.rotation,
                    clearance,
                    false,
                    out Pose safePose,
                    4.5f,
                    10f,
                    1.25f))
            {
                Object.DestroyImmediate(prop);
                return;
            }

            prop.transform.SetPositionAndRotation(safePose.position, safePose.rotation);
        }

        private static bool TryCalculateRendererBounds(GameObject root, out Bounds bounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                bounds = default;
                return false;
            }

            bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return true;
        }

        private static Color ResolveVariantTint(string instanceName, Color baseTint, string style)
        {
            if (string.Equals(style, "blood", StringComparison.OrdinalIgnoreCase))
            {
                return baseTint;
            }

            int hash = string.IsNullOrWhiteSpace(instanceName) ? 0 : Animator.StringToHash(instanceName);
            float seedA = Mathf.Abs(Mathf.Sin(hash * 0.0137f));
            float seedB = Mathf.Abs(Mathf.Sin(hash * 0.0291f + 1.37f));
            float seedC = Mathf.Abs(Mathf.Sin(hash * 0.0517f + 2.41f));

            Color.RGBToHSV(baseTint, out float hue, out float saturation, out float value);
            hue = Mathf.Repeat(hue + Mathf.Lerp(-0.018f, 0.018f, seedA), 1f);
            saturation = Mathf.Clamp01(saturation * Mathf.Lerp(0.92f, 1.08f, seedB));
            value = Mathf.Clamp01(value * Mathf.Lerp(0.82f, 1.06f, seedC));
            return Color.HSVToRGB(hue, saturation, value);
        }

        private static void ResetGeneratedCaches()
        {
            materialCache.Clear();
            textureCache.Clear();
        }

        private static void RepairUnsupportedProjectMaterials()
        {
            string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
            Shader fallback = ResolveLitShader();
            if (fallback == null)
            {
                return;
            }

            foreach (string guid in materialGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    continue;
                }

                Shader shader = material.shader;
                bool unsupported = shader == null ||
                                   shader.name == "Hidden/InternalErrorShader" ||
                                   !shader.isSupported;

                if (!unsupported)
                {
                    continue;
                }

                material.shader = fallback;
                if (material.HasProperty("_Color"))
                {
                    material.SetColor("_Color", new Color(0.2f, 0.2f, 0.22f));
                }

                if (material.HasProperty("_BaseColor"))
                {
                    material.SetColor("_BaseColor", new Color(0.2f, 0.2f, 0.22f));
                }

                EditorUtility.SetDirty(material);
            }
        }

        private static Material CreateMaterial(
            Color color,
            string style = "default",
            bool emissive = false,
            Color emissionColor = default)
        {
            string key = $"{style}|{ToColorKey(color)}|{emissive}|{ToColorKey(emissionColor)}";
            if (materialCache.TryGetValue(key, out Material cached) && cached != null)
            {
                return cached;
            }

            string assetPath = $"{GeneratedMaterialFolderPath}/{SanitizeFileToken(key)}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material != null)
            {
                materialCache[key] = material;
                return material;
            }

            Shader shader = ResolveLitShader();
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader)
            {
                name = $"M_{style}_{ToColorKey(color)}"
            };

            Texture2D texture = ResolveStyleTexture(style, color);
            ApplyMaterialSurface(material, color, texture);
            ApplyStyleTuning(material, style);

            if (emissive)
            {
                Color safeEmission = emissionColor == default ? color : emissionColor;
                if (material.HasProperty("_EmissionColor"))
                {
                    material.SetColor("_EmissionColor", safeEmission * 1.2f);
                }

                material.EnableKeyword("_EMISSION");
            }

            AssetDatabase.CreateAsset(material, assetPath);
            AssetDatabase.ImportAsset(assetPath);
            material = AssetDatabase.LoadAssetAtPath<Material>(assetPath) ?? material;
            materialCache[key] = material;
            return material;
        }

        private static Material CreateTexturedMaterial(
            string textureAssetPath,
            Color colorTint,
            string style = "default",
            bool emissive = false,
            Color emissionColor = default)
        {
            if (string.IsNullOrWhiteSpace(textureAssetPath))
            {
                return CreateMaterial(colorTint, style, emissive, emissionColor);
            }

            string key = $"tex|{textureAssetPath}|{style}|{ToColorKey(colorTint)}|{emissive}|{ToColorKey(emissionColor)}";
            if (materialCache.TryGetValue(key, out Material cached) && cached != null)
            {
                return cached;
            }

            string assetPath = $"{GeneratedMaterialFolderPath}/{SanitizeFileToken(key)}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material != null)
            {
                materialCache[key] = material;
                return material;
            }

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(textureAssetPath);
            if (texture == null)
            {
                return CreateMaterial(colorTint, style, emissive, emissionColor);
            }

            Shader shader = ResolveLitShader();
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader)
            {
                name = $"M_tex_{SanitizeFileToken(Path.GetFileNameWithoutExtension(textureAssetPath))}"
            };

            ApplyMaterialSurface(material, colorTint, texture);
            ApplyStyleTuning(material, style);

            if (emissive)
            {
                Color safeEmission = emissionColor == default ? colorTint : emissionColor;
                if (material.HasProperty("_EmissionColor"))
                {
                    material.SetColor("_EmissionColor", safeEmission * 1.2f);
                }

                material.EnableKeyword("_EMISSION");
            }

            AssetDatabase.CreateAsset(material, assetPath);
            AssetDatabase.ImportAsset(assetPath);
            material = AssetDatabase.LoadAssetAtPath<Material>(assetPath) ?? material;
            materialCache[key] = material;
            return material;
        }

        private static AudioClip LoadAudioClipAsset(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
        }

        private static Object[] LoadAudioClipAssets(params string[] assetPaths)
        {
            List<Object> clips = new List<Object>();
            if (assetPaths == null)
            {
                return clips.ToArray();
            }

            foreach (string assetPath in assetPaths)
            {
                AudioClip clip = LoadAudioClipAsset(assetPath);
                if (clip != null)
                {
                    clips.Add(clip);
                }
            }

            return clips.ToArray();
        }

        private static Shader ResolveLitShader()
        {
            UnityEngine.Rendering.RenderPipelineAsset pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            Shader shader = null;
            if (pipeline != null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null || !shader.isSupported)
                {
                    shader = Shader.Find("HDRP/Lit");
                }
            }

            if (shader == null || !shader.isSupported)
            {
                shader = Shader.Find("Standard");
            }

            if (shader == null || !shader.isSupported)
            {
                shader = Shader.Find("Legacy Shaders/Diffuse");
            }

            return shader;
        }

        private static Texture2D ResolveStyleTexture(string style, Color color)
        {
            string safeStyle = string.IsNullOrWhiteSpace(style) ? "default" : style.Trim().ToLowerInvariant();

            switch (safeStyle)
            {
                case "floor":
                    return GetOrCreateNoiseTexture("floor", new Color(0.05f, 0.05f, 0.055f), new Color(0.14f, 0.14f, 0.15f), 6.8f, 17f, 0.12f, 1.25f);
                case "ceiling":
                    return GetOrCreateNoiseTexture("ceiling", new Color(0.05f, 0.05f, 0.06f), new Color(0.11f, 0.11f, 0.12f), 5.4f, 14f, 0.08f, 1.15f);
                case "wall":
                    return GetOrCreateNoiseTexture("wall", new Color(0.075f, 0.076f, 0.08f), new Color(0.2f, 0.205f, 0.215f), 10.5f, 33f, 0.34f, 1.58f);
                case "metal":
                    return GetOrCreateNoiseTexture("metal", new Color(0.13f, 0.13f, 0.14f), new Color(0.26f, 0.26f, 0.28f), 8f, 21f, 0.22f, 1.45f);
                case "door_frame":
                    return GetOrCreateNoiseTexture("door_frame", new Color(0.1f, 0.1f, 0.11f), new Color(0.23f, 0.23f, 0.25f), 7.2f, 20f, 0.24f, 1.4f);
                case "door_leaf":
                    return GetOrCreateNoiseTexture("door_leaf", new Color(0.09f, 0.1f, 0.11f), new Color(0.2f, 0.21f, 0.23f), 9f, 24f, 0.18f, 1.35f);
                case "door_handle":
                    return GetOrCreateNoiseTexture("door_handle", new Color(0.32f, 0.32f, 0.3f), new Color(0.62f, 0.62f, 0.58f), 10f, 28f, 0.1f, 1.1f);
                case "player_jacket":
                    return GetOrCreateNoiseTexture("player_jacket", new Color(0.17f, 0.22f, 0.25f), new Color(0.4f, 0.46f, 0.5f), 5f, 13f, 0.07f, 1.1f);
                case "player_pants":
                    return GetOrCreateNoiseTexture("player_pants", new Color(0.08f, 0.1f, 0.11f), new Color(0.22f, 0.24f, 0.26f), 5.4f, 14f, 0.07f, 1.15f);
                case "player_skin":
                    return GetOrCreateNoiseTexture("player_skin", new Color(0.3f, 0.24f, 0.2f), new Color(0.48f, 0.4f, 0.35f), 4f, 11f, 0.03f, 1.02f);
                case "player_bag":
                    return GetOrCreateNoiseTexture("player_bag", new Color(0.12f, 0.14f, 0.15f), new Color(0.24f, 0.27f, 0.3f), 6f, 15f, 0.12f, 1.2f);
                case "monster_skin":
                    return GetOrCreateNoiseTexture("monster_skin", new Color(0.07f, 0.07f, 0.08f), new Color(0.22f, 0.06f, 0.06f), 8f, 20f, 0.28f, 1.48f);
                case "monster_bone":
                    return GetOrCreateNoiseTexture("monster_bone", new Color(0.18f, 0.18f, 0.2f), new Color(0.36f, 0.36f, 0.38f), 7f, 18f, 0.12f, 1.12f);
                case "monster_eye":
                    return GetOrCreateNoiseTexture("monster_eye", new Color(0.22f, 0.02f, 0.02f), new Color(0.8f, 0.08f, 0.08f), 12f, 30f, 0.04f, 1.08f);
                case "blood":
                    return GetOrCreateNoiseTexture("blood", new Color(0.1f, 0.01f, 0.01f), new Color(0.34f, 0.04f, 0.04f), 6f, 19f, 0.34f, 1.55f);
                default:
                    return GetOrCreateNoiseTexture($"default_{ToColorKey(color)}", color * 0.72f, color * 1.18f, 5f, 12f, 0.06f, 1.1f);
            }
        }

        private static Texture2D GetOrCreateNoiseTexture(
            string key,
            Color darkColor,
            Color brightColor,
            float baseScale,
            float detailScale,
            float crackStrength,
            float contrast)
        {
            if (textureCache.TryGetValue(key, out Texture2D cached) && cached != null)
            {
                return cached;
            }

            string assetPath = $"{GeneratedTextureFolderPath}/TX_{SanitizeFileToken(key)}.asset";
            Texture2D existingTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (existingTexture != null)
            {
                textureCache[key] = existingTexture;
                return existingTexture;
            }

            int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                name = $"TX_{key}",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };

            int hash = key.GetHashCode();
            float seedA = Mathf.Abs(hash % 997) * 0.013f;
            float seedB = Mathf.Abs((hash / 7) % 991) * 0.017f;
            float seedC = Mathf.Abs((hash / 11) % 983) * 0.019f;

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)(size - 1);
                    float v = y / (float)(size - 1);

                    float coarse = Mathf.PerlinNoise(u * baseScale + seedA, v * baseScale + seedB);
                    float detail = Mathf.PerlinNoise(u * detailScale + seedC, v * detailScale + seedA);
                    float cracks = Mathf.PerlinNoise(u * (detailScale * 1.7f) + seedB, v * (detailScale * 1.7f) + seedC);

                    float value = coarse * 0.62f + detail * 0.28f;
                    value -= Mathf.Clamp01((cracks - 0.65f) * 2.8f) * crackStrength;
                    value = Mathf.Clamp01(Mathf.Pow(Mathf.Max(0.001f, value), contrast));

                    pixels[y * size + x] = Color.Lerp(darkColor, brightColor, value);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(true, false);
            AssetDatabase.CreateAsset(texture, assetPath);
            AssetDatabase.ImportAsset(assetPath);
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath) ?? texture;
            textureCache[key] = texture;
            return texture;
        }

        private static void ApplyStyleTuning(Material material, string style)
        {
            if (material == null)
            {
                return;
            }

            string safeStyle = string.IsNullOrWhiteSpace(style) ? "default" : style.Trim().ToLowerInvariant();
            switch (safeStyle)
            {
                case "metal":
                case "door_frame":
                case "door_leaf":
                case "door_handle":
                    SetMaterialFloat(material, "_Metallic", 0.34f);
                    SetMaterialFloat(material, "_Smoothness", 0.26f);
                    SetMaterialFloat(material, "_Glossiness", 0.26f);
                    break;
                case "blood":
                case "monster_skin":
                    SetMaterialFloat(material, "_Metallic", 0.02f);
                    SetMaterialFloat(material, "_Smoothness", 0.12f);
                    SetMaterialFloat(material, "_Glossiness", 0.12f);
                    break;
                default:
                    SetMaterialFloat(material, "_Metallic", 0.06f);
                    SetMaterialFloat(material, "_Smoothness", 0.2f);
                    SetMaterialFloat(material, "_Glossiness", 0.2f);
                    break;
            }
        }

        private static void ApplyMaterialSurface(Material material, Color color, Texture texture)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (texture != null)
            {
                if (material.HasProperty("_MainTex"))
                {
                    material.SetTexture("_MainTex", texture);
                    material.SetTextureScale("_MainTex", Vector2.one);
                }

                if (material.HasProperty("_BaseMap"))
                {
                    material.SetTexture("_BaseMap", texture);
                    material.SetTextureScale("_BaseMap", Vector2.one);
                }
            }
        }

        private static void SetMaterialFloat(Material material, string property, float value)
        {
            if (material != null && material.HasProperty(property))
            {
                material.SetFloat(property, value);
            }
        }

        private static Material CreateScaledMaterial(Color color, string style, Vector3 localScale)
        {
            Vector2 tiling = ResolveCubeTextureTiling(style, localScale);
            if (Mathf.Abs(tiling.x - 1f) <= 0.01f && Mathf.Abs(tiling.y - 1f) <= 0.01f)
            {
                return CreateMaterial(color, style);
            }

            string tilingKey = $"{style}|{ToColorKey(color)}|tile_{tiling.x:0.00}_{tiling.y:0.00}";
            if (materialCache.TryGetValue(tilingKey, out Material cached) && cached != null)
            {
                return cached;
            }

            string assetPath = $"{GeneratedMaterialFolderPath}/{SanitizeFileToken(tilingKey)}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material != null)
            {
                materialCache[tilingKey] = material;
                return material;
            }

            Material baseMaterial = CreateMaterial(color, style);
            material = new Material(baseMaterial)
            {
                name = $"M_{style}_{ToColorKey(color)}_{tiling.x:0.00}_{tiling.y:0.00}"
            };

            if (material.HasProperty("_MainTex"))
            {
                material.SetTextureScale("_MainTex", tiling);
            }

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTextureScale("_BaseMap", tiling);
            }

            AssetDatabase.CreateAsset(material, assetPath);
            AssetDatabase.ImportAsset(assetPath);
            material = AssetDatabase.LoadAssetAtPath<Material>(assetPath) ?? material;
            materialCache[tilingKey] = material;
            return material;
        }

        private static Vector2 ResolveCubeTextureTiling(string style, Vector3 localScale)
        {
            string safeStyle = string.IsNullOrWhiteSpace(style) ? "default" : style.Trim().ToLowerInvariant();
            float horizontal = Mathf.Max(localScale.x, localScale.z);
            float vertical = Mathf.Max(0.25f, localScale.y);

            switch (safeStyle)
            {
                case "floor":
                    return new Vector2(Mathf.Max(2f, localScale.x * 0.38f), Mathf.Max(2f, localScale.z * 0.38f));
                case "ceiling":
                    return new Vector2(Mathf.Max(2f, localScale.x * 0.32f), Mathf.Max(2f, localScale.z * 0.32f));
                case "wall":
                    return new Vector2(Mathf.Max(1.5f, horizontal * 0.42f), Mathf.Max(1.5f, vertical * 0.86f));
                case "metal":
                case "door_frame":
                case "door_leaf":
                    return new Vector2(Mathf.Max(1.2f, horizontal * 0.62f), Mathf.Max(1.2f, vertical * 0.74f));
                case "blood":
                    return Vector2.one;
                default:
                    return new Vector2(Mathf.Max(1.2f, horizontal * 0.34f), Mathf.Max(1.2f, vertical * 0.7f));
            }
        }

        private static string ToColorKey(Color color)
        {
            return $"{Mathf.RoundToInt(color.r * 255f):X2}{Mathf.RoundToInt(color.g * 255f):X2}{Mathf.RoundToInt(color.b * 255f):X2}{Mathf.RoundToInt(color.a * 255f):X2}";
        }

        private static string SanitizeFileToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "empty";
            }

            char[] invalid = Path.GetInvalidFileNameChars();
            System.Text.StringBuilder builder = new System.Text.StringBuilder(value.Length);
            for (int index = 0; index < value.Length; index++)
            {
                char symbol = value[index];
                if (char.IsWhiteSpace(symbol) || symbol == '|' || symbol == ':' || symbol == ';' || symbol == '/' || symbol == '\\')
                {
                    builder.Append('_');
                    continue;
                }

                bool isInvalid = false;
                for (int invalidIndex = 0; invalidIndex < invalid.Length; invalidIndex++)
                {
                    if (symbol == invalid[invalidIndex])
                    {
                        isInvalid = true;
                        break;
                    }
                }

                builder.Append(isInvalid ? '_' : symbol);
            }

            return builder.ToString();
        }

        private static Font GetDefaultFont()
        {
            if (defaultFont != null)
            {
                return defaultFont;
            }

            defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return defaultFont;
        }

        private static GameObject SavePrefab(GameObject root, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            AssetDatabase.ImportAsset(path);
            return prefab;
        }

        private static void SetReference(Object target, string propertyName, Object value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(target);
            }
        }

        private static void SetReferenceArray(Object target, string propertyName, Object[] values)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null || !property.isArray)
            {
                return;
            }

            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetString(Object target, string propertyName, string value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.stringValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(target);
            }
        }

        private static void SetInt(Object target, string propertyName, int value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.intValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(target);
            }
        }

        private static void SetFloat(Object target, string propertyName, float value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.floatValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(target);
            }
        }

        private static void SetBool(Object target, string propertyName, bool value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.boolValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(target);
            }
        }

        private static void SetEnum(Object target, string propertyName, Enum value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.intValue = Convert.ToInt32(value);
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(target);
            }
        }

        private static void SetVector3(Object target, string propertyName, Vector3 value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.vector3Value = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(target);
            }
        }
    }
}
