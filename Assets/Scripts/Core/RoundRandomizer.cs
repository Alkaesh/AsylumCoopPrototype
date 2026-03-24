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
        private static readonly string[] HookPointNames = { "Hook_A", "Hook_B", "Hook_C", "Hook_D", "Hook_E" };

        private sealed class RouteProfile
        {
            public RouteProfile(
                RoundRouteKind routeKind,
                string[] generatorPointNames,
                string[] keycardPointNames,
                string[] powerPointNames,
                string[] monsterPointNames,
                string[] batteryPointNames)
            {
                RouteKind = routeKind;
                GeneratorPointNames = generatorPointNames;
                KeycardPointNames = keycardPointNames;
                PowerPointNames = powerPointNames;
                MonsterPointNames = monsterPointNames;
                BatteryPointNames = batteryPointNames;
            }

            public RoundRouteKind RouteKind { get; }
            public string[] GeneratorPointNames { get; }
            public string[] KeycardPointNames { get; }
            public string[] PowerPointNames { get; }
            public string[] MonsterPointNames { get; }
            public string[] BatteryPointNames { get; }
        }

        private static readonly RouteProfile[] RouteProfiles =
        {
            new RouteProfile(
                RoundRouteKind.WestDescent,
                new[] { "Generator_A", "Generator_C" },
                new[] { "Keycard_A", "Keycard_C" },
                new[] { "Power_A" },
                new[] { "Monster_C", "Monster_D" },
                new[] { "Battery_A", "Battery_C", "Battery_E", "Battery_G", "Battery_H" }),
            new RouteProfile(
                RoundRouteKind.EastDescent,
                new[] { "Generator_B", "Generator_D" },
                new[] { "Keycard_B", "Keycard_D" },
                new[] { "Power_B" },
                new[] { "Monster_B", "Monster_D" },
                new[] { "Battery_B", "Battery_D", "Battery_F", "Battery_H", "Battery_C" }),
            new RouteProfile(
                RoundRouteKind.CrossCurrent,
                new[] { "Generator_C", "Generator_D" },
                new[] { "Keycard_E" },
                new[] { "Power_C" },
                new[] { "Monster_A", "Monster_C" },
                new[] { "Battery_C", "Battery_D", "Battery_G", "Battery_H", "Battery_A" })
        };

        [Server]
        public void ServerRandomizeLayout()
        {
            Dictionary<ObjectiveSpawnType, List<ObjectiveSpawnPoint>> spawnMap = BuildSpawnMap();
            if (spawnMap.Count == 0)
            {
                return;
            }

            RouteProfile profile = RouteProfiles[Random.Range(0, RouteProfiles.Length)];
            GameStateManager.Instance?.ServerSetActiveRoute(profile.RouteKind);

            PositionGenerators(spawnMap, profile);
            PositionKeycard(spawnMap, profile);
            PositionPowerConsole(spawnMap, profile);
            PositionHooks(spawnMap);
            PositionMonster(spawnMap, profile);
            PositionBatteries(spawnMap, profile);
            ResetDoorsToAuthoredState();
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

            return map;
        }

        [Server]
        private void PositionGenerators(Dictionary<ObjectiveSpawnType, List<ObjectiveSpawnPoint>> spawnMap, RouteProfile profile)
        {
            GeneratorTask[] generators = FindObjectsByType<GeneratorTask>();
            if (generators.Length == 0)
            {
                return;
            }

            for (int index = 0; index < generators.Length; index++)
            {
                ObjectiveSpawnPoint spawnPoint = ResolveNamedPoint(
                    spawnMap,
                    ObjectiveSpawnType.Generator,
                    profile.GeneratorPointNames,
                    index);
                if (spawnPoint == null)
                {
                    continue;
                }

                PlaceObjective(generators[index].transform, spawnPoint, ObjectiveSpawnType.Generator);
            }
        }

        [Server]
        private void PositionKeycard(Dictionary<ObjectiveSpawnType, List<ObjectiveSpawnPoint>> spawnMap, RouteProfile profile)
        {
            KeycardTask keycard = FindAnyObjectByType<KeycardTask>();
            if (keycard == null)
            {
                return;
            }

            ObjectiveSpawnPoint point = ResolveRandomNamedPoint(spawnMap, ObjectiveSpawnType.Keycard, profile.KeycardPointNames);
            if (point != null)
            {
                PlaceObjective(keycard.transform, point, ObjectiveSpawnType.Keycard);
            }
        }

        [Server]
        private void PositionPowerConsole(Dictionary<ObjectiveSpawnType, List<ObjectiveSpawnPoint>> spawnMap, RouteProfile profile)
        {
            PowerRestoreTask powerConsole = FindAnyObjectByType<PowerRestoreTask>();
            if (powerConsole == null)
            {
                return;
            }

            ObjectiveSpawnPoint point = ResolveRandomNamedPoint(spawnMap, ObjectiveSpawnType.PowerConsole, profile.PowerPointNames);
            if (point != null)
            {
                PlaceObjective(powerConsole.transform, point, ObjectiveSpawnType.PowerConsole);
            }
        }

        [Server]
        private void PositionHooks(Dictionary<ObjectiveSpawnType, List<ObjectiveSpawnPoint>> spawnMap)
        {
            HookPoint[] hooks = FindObjectsByType<HookPoint>();
            for (int index = 0; index < hooks.Length; index++)
            {
                ObjectiveSpawnPoint point = ResolveNamedPoint(spawnMap, ObjectiveSpawnType.Hook, HookPointNames, index);
                if (point == null)
                {
                    continue;
                }

                PlaceObjective(hooks[index].transform, point, ObjectiveSpawnType.Hook);
            }
        }

        [Server]
        private void PositionMonster(Dictionary<ObjectiveSpawnType, List<ObjectiveSpawnPoint>> spawnMap, RouteProfile profile)
        {
            MonsterAI monster = FindAnyObjectByType<MonsterAI>();
            if (monster == null)
            {
                return;
            }

            ObjectiveSpawnPoint point = ResolveRandomNamedPoint(spawnMap, ObjectiveSpawnType.Monster, profile.MonsterPointNames);
            if (point != null)
            {
                PlaceObjective(monster.transform, point, ObjectiveSpawnType.Monster);
            }
        }

        [Server]
        private void PositionBatteries(Dictionary<ObjectiveSpawnType, List<ObjectiveSpawnPoint>> spawnMap, RouteProfile profile)
        {
            BatteryPickupTask[] batteries = FindObjectsByType<BatteryPickupTask>();
            if (batteries.Length == 0)
            {
                return;
            }

            for (int index = 0; index < batteries.Length; index++)
            {
                bool shouldBeActive = index < profile.BatteryPointNames.Length;
                batteries[index].ServerSetSpawnEnabled(shouldBeActive);
                if (!shouldBeActive)
                {
                    continue;
                }

                ObjectiveSpawnPoint point = ResolveNamedPoint(
                    spawnMap,
                    ObjectiveSpawnType.Battery,
                    profile.BatteryPointNames,
                    index);
                if (point == null)
                {
                    continue;
                }

                PlaceObjective(batteries[index].transform, point, ObjectiveSpawnType.Battery);
            }
        }

        [Server]
        private void ResetDoorsToAuthoredState()
        {
            NetworkDoor[] doors = FindObjectsByType<NetworkDoor>();
            foreach (NetworkDoor door in doors)
            {
                door.ServerResetToInitialState();
            }
        }

        private static ObjectiveSpawnPoint ResolveNamedPoint(
            IReadOnlyDictionary<ObjectiveSpawnType, List<ObjectiveSpawnPoint>> spawnMap,
            ObjectiveSpawnType type,
            IReadOnlyList<string> preferredNames,
            int orderedIndex)
        {
            if (preferredNames != null && preferredNames.Count > 0)
            {
                int clampedIndex = Mathf.Clamp(orderedIndex, 0, preferredNames.Count - 1);
                ObjectiveSpawnPoint namedPoint = FindPointByName(spawnMap, type, preferredNames[clampedIndex]);
                if (namedPoint != null)
                {
                    return namedPoint;
                }
            }

            if (spawnMap.TryGetValue(type, out List<ObjectiveSpawnPoint> fallbacks) && fallbacks.Count > 0)
            {
                return fallbacks[Mathf.Clamp(orderedIndex, 0, fallbacks.Count - 1)];
            }

            return null;
        }

        private static ObjectiveSpawnPoint ResolveRandomNamedPoint(
            IReadOnlyDictionary<ObjectiveSpawnType, List<ObjectiveSpawnPoint>> spawnMap,
            ObjectiveSpawnType type,
            IReadOnlyList<string> preferredNames)
        {
            if (preferredNames != null && preferredNames.Count > 0)
            {
                int startIndex = Random.Range(0, preferredNames.Count);
                for (int offset = 0; offset < preferredNames.Count; offset++)
                {
                    string candidate = preferredNames[(startIndex + offset) % preferredNames.Count];
                    ObjectiveSpawnPoint namedPoint = FindPointByName(spawnMap, type, candidate);
                    if (namedPoint != null)
                    {
                        return namedPoint;
                    }
                }
            }

            if (spawnMap.TryGetValue(type, out List<ObjectiveSpawnPoint> fallbacks) && fallbacks.Count > 0)
            {
                return fallbacks[Random.Range(0, fallbacks.Count)];
            }

            return null;
        }

        private static ObjectiveSpawnPoint FindPointByName(
            IReadOnlyDictionary<ObjectiveSpawnType, List<ObjectiveSpawnPoint>> spawnMap,
            ObjectiveSpawnType type,
            string pointName)
        {
            if (string.IsNullOrWhiteSpace(pointName) ||
                !spawnMap.TryGetValue(type, out List<ObjectiveSpawnPoint> points))
            {
                return null;
            }

            foreach (ObjectiveSpawnPoint point in points)
            {
                if (point != null && point.name == pointName)
                {
                    return point;
                }
            }

            return null;
        }

        [Server]
        private static void PlaceObjective(Transform target, ObjectiveSpawnPoint spawnPoint, ObjectiveSpawnType spawnType)
        {
            if (target == null || spawnPoint == null)
            {
                return;
            }

            PlacementRules rules = PlacementSafety.ResolveRules(spawnType);
            if (!PlacementSafety.TryMoveTransformToSafePlacement(
                    target,
                    spawnPoint.transform.position,
                    spawnPoint.transform.rotation,
                    rules))
            {
                target.SetPositionAndRotation(spawnPoint.transform.position, spawnPoint.transform.rotation);
            }
        }
    }
}
