using System.Collections.Generic;
using AsylumHorror.Monster;
using AsylumHorror.Tasks;
using AsylumHorror.World;
using Mirror;
using UnityEngine;

namespace AsylumHorror.Core
{
    public class RoundRandomizer : NetworkBehaviour
    {
        [Header("Doors")]
        [SerializeField] private float lockedDoorChance = 0.4f;
        [SerializeField] private float openDoorChance = 0.2f;

        [Server]
        public void ServerRandomizeLayout()
        {
            Dictionary<ObjectiveSpawnType, List<ObjectiveSpawnPoint>> spawnMap = BuildSpawnMap();
            RandomizeGenerators(spawnMap);
            RandomizeKeycard(spawnMap);
            RandomizePowerConsole(spawnMap);
            RandomizeHooks(spawnMap);
            RandomizeMonster(spawnMap);
            RandomizeBatteries(spawnMap);
            RandomizeDoors();
        }

        [Server]
        private Dictionary<ObjectiveSpawnType, List<ObjectiveSpawnPoint>> BuildSpawnMap()
        {
            Dictionary<ObjectiveSpawnType, List<ObjectiveSpawnPoint>> map = new Dictionary<ObjectiveSpawnType, List<ObjectiveSpawnPoint>>();
            foreach (ObjectiveSpawnPoint point in FindObjectsByType<ObjectiveSpawnPoint>())
            {
                if (!map.TryGetValue(point.SpawnType, out List<ObjectiveSpawnPoint> list))
                {
                    list = new List<ObjectiveSpawnPoint>();
                    map[point.SpawnType] = list;
                }

                list.Add(point);
            }

            foreach (List<ObjectiveSpawnPoint> points in map.Values)
            {
                Shuffle(points);
            }

            return map;
        }

        [Server]
        private void RandomizeGenerators(Dictionary<ObjectiveSpawnType, List<ObjectiveSpawnPoint>> spawnMap)
        {
            if (!spawnMap.TryGetValue(ObjectiveSpawnType.Generator, out List<ObjectiveSpawnPoint> points) || points.Count == 0)
            {
                return;
            }

            GeneratorTask[] generators = FindObjectsByType<GeneratorTask>();
            for (int index = 0; index < generators.Length; index++)
            {
                ObjectiveSpawnPoint spawnPoint = points[index % points.Count];
                generators[index].transform.SetPositionAndRotation(spawnPoint.transform.position, spawnPoint.transform.rotation);
            }
        }

        [Server]
        private void RandomizeKeycard(Dictionary<ObjectiveSpawnType, List<ObjectiveSpawnPoint>> spawnMap)
        {
            if (!spawnMap.TryGetValue(ObjectiveSpawnType.Keycard, out List<ObjectiveSpawnPoint> points) || points.Count == 0)
            {
                return;
            }

            KeycardTask keycard = FindAnyObjectByType<KeycardTask>();
            if (keycard == null)
            {
                return;
            }

            ObjectiveSpawnPoint point = points[Random.Range(0, points.Count)];
            keycard.transform.SetPositionAndRotation(point.transform.position, point.transform.rotation);
        }

        [Server]
        private void RandomizePowerConsole(Dictionary<ObjectiveSpawnType, List<ObjectiveSpawnPoint>> spawnMap)
        {
            if (!spawnMap.TryGetValue(ObjectiveSpawnType.PowerConsole, out List<ObjectiveSpawnPoint> points) || points.Count == 0)
            {
                return;
            }

            PowerRestoreTask powerConsole = FindAnyObjectByType<PowerRestoreTask>();
            if (powerConsole == null)
            {
                return;
            }

            ObjectiveSpawnPoint point = points[Random.Range(0, points.Count)];
            powerConsole.transform.SetPositionAndRotation(point.transform.position, point.transform.rotation);
        }

        [Server]
        private void RandomizeHooks(Dictionary<ObjectiveSpawnType, List<ObjectiveSpawnPoint>> spawnMap)
        {
            if (!spawnMap.TryGetValue(ObjectiveSpawnType.Hook, out List<ObjectiveSpawnPoint> points) || points.Count == 0)
            {
                return;
            }

            HookPoint[] hooks = FindObjectsByType<HookPoint>();
            for (int index = 0; index < hooks.Length; index++)
            {
                ObjectiveSpawnPoint point = points[index % points.Count];
                hooks[index].transform.SetPositionAndRotation(point.transform.position, point.transform.rotation);
            }
        }

        [Server]
        private void RandomizeMonster(Dictionary<ObjectiveSpawnType, List<ObjectiveSpawnPoint>> spawnMap)
        {
            if (!spawnMap.TryGetValue(ObjectiveSpawnType.Monster, out List<ObjectiveSpawnPoint> points) || points.Count == 0)
            {
                return;
            }

            MonsterAI monster = FindAnyObjectByType<MonsterAI>();
            if (monster == null)
            {
                return;
            }

            ObjectiveSpawnPoint point = points[Random.Range(0, points.Count)];
            monster.transform.SetPositionAndRotation(point.transform.position, point.transform.rotation);
        }

        [Server]
        private void RandomizeBatteries(Dictionary<ObjectiveSpawnType, List<ObjectiveSpawnPoint>> spawnMap)
        {
            if (!spawnMap.TryGetValue(ObjectiveSpawnType.Battery, out List<ObjectiveSpawnPoint> points) || points.Count == 0)
            {
                return;
            }

            BatteryPickupTask[] batteries = FindObjectsByType<BatteryPickupTask>();
            for (int index = 0; index < batteries.Length; index++)
            {
                ObjectiveSpawnPoint point = points[index % points.Count];
                batteries[index].transform.SetPositionAndRotation(point.transform.position, point.transform.rotation);
            }
        }

        [Server]
        private void RandomizeDoors()
        {
            NetworkDoor[] doors = FindObjectsByType<NetworkDoor>();
            foreach (NetworkDoor door in doors)
            {
                bool locked = Random.value < lockedDoorChance;
                bool opened = !locked && Random.value < openDoorChance;
                door.ServerSetRandomState(opened, locked);
            }
        }

        private static void Shuffle<T>(IList<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int swapIndex = Random.Range(i, list.Count);
                (list[i], list[swapIndex]) = (list[swapIndex], list[i]);
            }
        }
    }
}
