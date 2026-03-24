using AsylumHorror.Monster;
using AsylumHorror.Tasks;
using AsylumHorror.World;
using Mirror;
using UnityEngine;

namespace AsylumHorror.Core
{
    public class GameplayBootstrapper : NetworkBehaviour
    {
        [Header("Network Prefabs")]
        [SerializeField] private MonsterAI monsterPrefab;
        [SerializeField] private GeneratorTask generatorPrefab;
        [SerializeField] private KeycardTask keycardPrefab;
        [SerializeField] private PowerRestoreTask powerRestorePrefab;
        [SerializeField] private ExitDoorTask exitDoorPrefab;
        [SerializeField] private HookPoint hookPrefab;
        [SerializeField] private NetworkDoor doorPrefab;
        [SerializeField] private BatteryPickupTask batteryPrefab;

        [Header("Fallback Spawn Positions")]
        [SerializeField] private Vector3 monsterSpawnPosition = new Vector3(0f, 0f, -18f);
        [SerializeField] private Vector3[] generatorPositions =
        {
            new Vector3(-37.5f, 0.02f, 34f),
            new Vector3(32f, 0.02f, -30f)
        };
        [SerializeField] private Vector3 keycardPosition = new Vector3(-5.2f, 0.82f, 45f);
        [SerializeField] private Vector3 powerPosition = new Vector3(-24.6f, 0.55f, -33f);
        [SerializeField] private Vector3 exitDoorPosition = new Vector3(0f, 0f, 52.2f);
        [SerializeField] private Vector3[] hookPositions =
        {
            new Vector3(-49.2f, 0f, 18f),
            new Vector3(49.2f, 0f, 18f),
            new Vector3(-50f, 0f, -46f),
            new Vector3(50f, 0f, -46f),
            new Vector3(0f, 0f, -18.4f)
        };
        [SerializeField] private Vector3[] doorPositions =
        {
            new Vector3(0f, 0f, 35f),
            new Vector3(0f, 0f, -36f),
            new Vector3(-25f, 0f, 34f),
            new Vector3(25f, 0f, 34f),
            new Vector3(-25f, 0f, 4f),
            new Vector3(25f, 0f, 4f),
            new Vector3(-21f, 0f, -28f),
            new Vector3(21f, 0f, -28f)
        };
        [SerializeField] private Vector3[] batteryPositions =
        {
            new Vector3(-40.4f, 0.01f, 38.4f),
            new Vector3(40.4f, 0.01f, 38.4f),
            new Vector3(-28.2f, 0.01f, 9.8f),
            new Vector3(31.8f, 0.01f, 0f),
            new Vector3(-35.8f, 0.01f, -32.4f),
            new Vector3(38.4f, 0.01f, -31.8f),
            new Vector3(-5.4f, 0.01f, -45f),
            new Vector3(5.2f, 0.01f, 45f)
        };

        [ServerCallback]
        private void Start()
        {
            SpawnMonsterIfMissing();
            SpawnTasksIfMissing();
            SpawnHooksIfMissing();
            SpawnDoorsIfMissing();
            SpawnBatteriesIfMissing();
        }

        [Server]
        private void SpawnMonsterIfMissing()
        {
            MonsterAI[] existing = FindObjectsByType<MonsterAI>();
            if (existing.Length > 0 || monsterPrefab == null)
            {
                return;
            }

            Pose spawnPose = ResolveSafePose(monsterSpawnPosition, Quaternion.identity, ObjectiveSpawnType.Monster);
            GameObject instance = Instantiate(monsterPrefab.gameObject, spawnPose.position, spawnPose.rotation);
            NetworkServer.Spawn(instance);
        }

        [Server]
        private void SpawnTasksIfMissing()
        {
            GeneratorTask[] generators = FindObjectsByType<GeneratorTask>();
            int neededGenerators = Mathf.Max(0, 2 - generators.Length);
            for (int i = 0; i < neededGenerators; i++)
            {
                if (generatorPrefab == null)
                {
                    break;
                }

                Vector3 position = generatorPositions[Mathf.Min(i, generatorPositions.Length - 1)];
                SpawnNetworkObject(generatorPrefab.gameObject, position, Quaternion.identity, ObjectiveSpawnType.Generator);
            }

            if (FindAnyObjectByType<KeycardTask>() == null && keycardPrefab != null)
            {
                SpawnNetworkObject(keycardPrefab.gameObject, keycardPosition, Quaternion.identity, ObjectiveSpawnType.Keycard);
            }

            if (FindAnyObjectByType<PowerRestoreTask>() == null && powerRestorePrefab != null)
            {
                SpawnNetworkObject(powerRestorePrefab.gameObject, powerPosition, Quaternion.identity, ObjectiveSpawnType.PowerConsole);
            }

            if (FindAnyObjectByType<ExitDoorTask>() == null && exitDoorPrefab != null)
            {
                SpawnNetworkObject(exitDoorPrefab.gameObject, exitDoorPosition, Quaternion.identity, BuildDoorRules(true));
            }
        }

        [Server]
        private void SpawnHooksIfMissing()
        {
            HookPoint[] hooks = FindObjectsByType<HookPoint>();
            int neededHooks = Mathf.Max(0, 5 - hooks.Length);
            for (int i = 0; i < neededHooks; i++)
            {
                if (hookPrefab == null)
                {
                    break;
                }

                Vector3 position = hookPositions[Mathf.Min(i, hookPositions.Length - 1)];
                SpawnNetworkObject(hookPrefab.gameObject, position, Quaternion.identity, ObjectiveSpawnType.Hook);
            }
        }

        [Server]
        private void SpawnDoorsIfMissing()
        {
            NetworkDoor[] doors = FindObjectsByType<NetworkDoor>();
            int neededDoors = Mathf.Max(0, 8 - doors.Length);
            for (int i = 0; i < neededDoors; i++)
            {
                if (doorPrefab == null)
                {
                    break;
                }

                Vector3 position = doorPositions[Mathf.Min(i, doorPositions.Length - 1)];
                SpawnNetworkObject(doorPrefab.gameObject, position, Quaternion.identity, BuildDoorRules(false));
            }
        }

        [Server]
        private void SpawnBatteriesIfMissing()
        {
            BatteryPickupTask[] batteries = FindObjectsByType<BatteryPickupTask>();
            int neededBatteries = Mathf.Max(0, 8 - batteries.Length);
            for (int i = 0; i < neededBatteries; i++)
            {
                if (batteryPrefab == null)
                {
                    break;
                }

                Vector3 position = batteryPositions[Mathf.Min(i, batteryPositions.Length - 1)];
                SpawnNetworkObject(batteryPrefab.gameObject, position, Quaternion.identity, ObjectiveSpawnType.Battery);
            }
        }

        [Server]
        private static void SpawnNetworkObject(GameObject prefab, Vector3 position, Quaternion rotation, ObjectiveSpawnType spawnType)
        {
            Pose spawnPose = ResolveSafePose(position, rotation, PlacementSafety.ResolveRules(spawnType));
            GameObject instance = Instantiate(prefab, spawnPose.position, spawnPose.rotation);
            NetworkServer.Spawn(instance);
        }

        [Server]
        private static void SpawnNetworkObject(GameObject prefab, Vector3 position, Quaternion rotation, PlacementRules rules)
        {
            Pose spawnPose = ResolveSafePose(position, rotation, rules);
            GameObject instance = Instantiate(prefab, spawnPose.position, spawnPose.rotation);
            NetworkServer.Spawn(instance);
        }

        [Server]
        private static Pose ResolveSafePose(Vector3 desiredPosition, Quaternion desiredRotation, PlacementRules rules)
        {
            return PlacementSafety.TryResolvePlacement(
                desiredPosition,
                desiredRotation,
                rules,
                out Pose resolvedPose)
                ? resolvedPose
                : new Pose(desiredPosition, desiredRotation);
        }

        private static PlacementRules BuildDoorRules(bool isExitDoor)
        {
            return new PlacementRules(
                isExitDoor ? new Vector3(2.6f, 1.7f, 0.95f) : new Vector3(2.45f, 1.65f, 0.95f),
                PlacementCategory.Structural,
                false,
                protectedPadding: isExitDoor ? 0.45f : 0.35f);
        }
    }
}
