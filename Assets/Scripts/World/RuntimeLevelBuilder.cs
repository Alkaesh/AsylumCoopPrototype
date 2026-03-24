using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace AsylumHorror.World
{
    public class RuntimeLevelBuilder : MonoBehaviour
    {
        [Header("Build")]
        [SerializeField] private bool buildOnStart = false;
        [SerializeField] private Material floorMaterial;
        [SerializeField] private Material wallMaterial;
        [SerializeField] private PhysicsMaterial floorPhysicsMaterial;

        [Header("Scale")]
        [SerializeField] private float mapSize = 80f;
        [SerializeField] private float wallHeight = 4f;
        [SerializeField] private float wallThickness = 0.4f;

        [Header("NavMesh")]
        [SerializeField] private bool bakeNavMeshAtRuntime = true;
        [SerializeField] private int navMeshAgentTypeId = 0;
        [SerializeField] private Vector3 navMeshBoundsSize = new Vector3(180f, 40f, 180f);

        private Transform generatedRoot;
        private NavMeshDataInstance navMeshInstance;
        private bool built;

        private void Start()
        {
            if (!buildOnStart || built)
            {
                return;
            }

            BuildLevel();
        }

        public void BuildLevel()
        {
            Debug.LogWarning("RuntimeLevelBuilder is blockout-only. Use the authored HospitalLevel scene for the playable experience.");
            built = true;
            generatedRoot = new GameObject("GeneratedLevelRoot").transform;
            generatedRoot.SetParent(transform, false);

            BuildFloor();
            BuildPerimeter();
            BuildInteriorLayout();
            BuildCoverAndDeadEnds();
            BuildAtmosphereLights();
            EnsureSpawnPoints();
            EnsurePatrolPoints();
            SetupPostLook();

            if (bakeNavMeshAtRuntime)
            {
                BuildRuntimeNavMesh();
            }
        }

        private void OnDestroy()
        {
            if (navMeshInstance.valid)
            {
                navMeshInstance.Remove();
            }
        }

        private void BuildFloor()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(generatedRoot, false);
            floor.transform.localScale = new Vector3(mapSize, 1f, mapSize);
            floor.transform.position = new Vector3(0f, -0.5f, 0f);

            if (floorMaterial != null)
            {
                floor.GetComponent<Renderer>().material = floorMaterial;
            }

            if (floorPhysicsMaterial != null)
            {
                floor.GetComponent<Collider>().material = floorPhysicsMaterial;
            }
        }

        private void BuildPerimeter()
        {
            float half = mapSize * 0.5f;
            CreateWall(new Vector3(0f, wallHeight * 0.5f, half), new Vector3(mapSize, wallHeight, wallThickness), "NorthWall");
            CreateWall(new Vector3(0f, wallHeight * 0.5f, -half), new Vector3(mapSize, wallHeight, wallThickness), "SouthWall");
            CreateWall(new Vector3(half, wallHeight * 0.5f, 0f), new Vector3(wallThickness, wallHeight, mapSize), "EastWall");
            CreateWall(new Vector3(-half, wallHeight * 0.5f, 0f), new Vector3(wallThickness, wallHeight, mapSize), "WestWall");
        }

        private void BuildInteriorLayout()
        {
            // Main corridor split.
            CreateWall(new Vector3(0f, wallHeight * 0.5f, 10f), new Vector3(54f, wallHeight, wallThickness), "CorridorDividerA");
            CreateWall(new Vector3(0f, wallHeight * 0.5f, -10f), new Vector3(48f, wallHeight, wallThickness), "CorridorDividerB");

            // Room shells.
            BuildRoom(new Vector3(-24f, 0f, 24f), 18f, 14f, "WardA");
            BuildRoom(new Vector3(22f, 0f, 22f), 16f, 14f, "StorageB");
            BuildRoom(new Vector3(-22f, 0f, -24f), 20f, 12f, "SurgeryC");
            BuildRoom(new Vector3(22f, 0f, -24f), 18f, 12f, "OfficeD");

            // Connector corridors and dead corners.
            CreateWall(new Vector3(-2f, wallHeight * 0.5f, 25f), new Vector3(wallThickness, wallHeight, 12f), "DividerTop");
            CreateWall(new Vector3(4f, wallHeight * 0.5f, -25f), new Vector3(wallThickness, wallHeight, 11f), "DividerBottom");
            CreateWall(new Vector3(-31f, wallHeight * 0.5f, 0f), new Vector3(wallThickness, wallHeight, 20f), "LeftNarrow");
            CreateWall(new Vector3(30f, wallHeight * 0.5f, 2f), new Vector3(wallThickness, wallHeight, 22f), "RightNarrow");
        }

        private void BuildCoverAndDeadEnds()
        {
            CreateCover(new Vector3(-12f, 0.6f, 6f), "LockerA");
            CreateCover(new Vector3(-10f, 0.6f, -6f), "LockerB");
            CreateCover(new Vector3(14f, 0.6f, 4f), "CabinetA");
            CreateCover(new Vector3(13f, 0.6f, -5f), "CabinetB");
            CreateCover(new Vector3(26f, 0.6f, -2f), "DebrisA");
            CreateCover(new Vector3(-26f, 0.6f, 2f), "DebrisB");
        }

        private void BuildAtmosphereLights()
        {
            CreateLight(new Vector3(-18f, 3.2f, 15f), 18f, 0.9f, true);
            CreateLight(new Vector3(18f, 3.2f, 15f), 18f, 1.0f, false);
            CreateLight(new Vector3(-18f, 3.2f, -15f), 18f, 0.8f, true);
            CreateLight(new Vector3(18f, 3.2f, -15f), 18f, 0.85f, true);
            CreateLight(new Vector3(0f, 3.2f, 0f), 22f, 0.6f, false);
        }

        private void EnsureSpawnPoints()
        {
            PlayerSpawnPoint[] existing = FindObjectsByType<PlayerSpawnPoint>();
            if (existing.Length > 0)
            {
                return;
            }

            CreateSpawnPoint(new Vector3(-8f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), 0);
            CreateSpawnPoint(new Vector3(8f, 0f, 0f), Quaternion.Euler(0f, -90f, 0f), 1);
        }

        private void EnsurePatrolPoints()
        {
            PatrolPoint[] existing = FindObjectsByType<PatrolPoint>();
            if (existing.Length > 0)
            {
                return;
            }

            CreatePatrolPoint(new Vector3(-20f, 0f, 0f), "Patrol_A");
            CreatePatrolPoint(new Vector3(-10f, 0f, 20f), "Patrol_B");
            CreatePatrolPoint(new Vector3(12f, 0f, 22f), "Patrol_C");
            CreatePatrolPoint(new Vector3(24f, 0f, 0f), "Patrol_D");
            CreatePatrolPoint(new Vector3(10f, 0f, -22f), "Patrol_E");
            CreatePatrolPoint(new Vector3(-12f, 0f, -20f), "Patrol_F");
        }

        private void SetupPostLook()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.035f;
            RenderSettings.fogColor = new Color(0.06f, 0.07f, 0.08f);
            RenderSettings.ambientIntensity = 0.4f;
        }

        private void BuildRuntimeNavMesh()
        {
            List<NavMeshBuildSource> sources = new List<NavMeshBuildSource>();
            List<NavMeshBuildMarkup> markups = new List<NavMeshBuildMarkup>();
            NavMeshBuilder.CollectSources(null, Physics.DefaultRaycastLayers, NavMeshCollectGeometry.PhysicsColliders, 0, markups, sources);

            NavMeshBuildSettings settings = NavMesh.GetSettingsByID(navMeshAgentTypeId);
            Bounds bounds = new Bounds(Vector3.zero, navMeshBoundsSize);
            NavMeshData data = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
            navMeshInstance = NavMesh.AddNavMeshData(data);
        }

        private void BuildRoom(Vector3 center, float width, float length, string roomName)
        {
            Transform roomRoot = new GameObject(roomName).transform;
            roomRoot.SetParent(generatedRoot, false);
            roomRoot.position = center;

            float halfWidth = width * 0.5f;
            float halfLength = length * 0.5f;

            CreateWall(roomRoot.position + new Vector3(0f, wallHeight * 0.5f, halfLength), new Vector3(width, wallHeight, wallThickness), $"{roomName}_North");
            CreateWall(roomRoot.position + new Vector3(0f, wallHeight * 0.5f, -halfLength), new Vector3(width, wallHeight, wallThickness), $"{roomName}_South");
            CreateWall(roomRoot.position + new Vector3(halfWidth, wallHeight * 0.5f, 0f), new Vector3(wallThickness, wallHeight, length), $"{roomName}_East");
            CreateWall(roomRoot.position + new Vector3(-halfWidth, wallHeight * 0.5f, 0f), new Vector3(wallThickness, wallHeight, length), $"{roomName}_West");
        }

        private void CreateCover(Vector3 position, string objectName)
        {
            GameObject cover = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cover.name = objectName;
            cover.transform.SetParent(generatedRoot, false);
            cover.transform.position = position;
            cover.transform.localScale = new Vector3(1.5f, 1.2f, 1.5f);

            if (wallMaterial != null)
            {
                cover.GetComponent<Renderer>().material = wallMaterial;
            }
        }

        private void CreateLight(Vector3 position, float range, float intensity, bool flicker)
        {
            GameObject lightObject = new GameObject("PointLight");
            lightObject.transform.SetParent(generatedRoot, false);
            lightObject.transform.position = position;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = range;
            light.intensity = intensity;
            light.color = new Color(0.93f, 0.95f, 1f);

            if (flicker)
            {
                FlickerLight flickerComponent = lightObject.AddComponent<FlickerLight>();
                flickerComponent.SetBaseIntensity(intensity);
            }
        }

        private void CreateSpawnPoint(Vector3 position, Quaternion rotation, int index)
        {
            GameObject spawnObject = new GameObject($"SpawnPoint_{index}");
            spawnObject.transform.SetParent(generatedRoot, false);
            spawnObject.transform.SetPositionAndRotation(position, rotation);
            PlayerSpawnPoint spawnPoint = spawnObject.AddComponent<PlayerSpawnPoint>();
            spawnPoint.SetIndex(index);
        }

        private void CreatePatrolPoint(Vector3 position, string pointName)
        {
            GameObject patrolObject = new GameObject(pointName);
            patrolObject.transform.SetParent(generatedRoot, false);
            patrolObject.transform.position = position;
            patrolObject.AddComponent<PatrolPoint>();
        }

        private void CreateWall(Vector3 position, Vector3 scale, string wallName)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = wallName;
            wall.transform.SetParent(generatedRoot, false);
            wall.transform.position = position;
            wall.transform.localScale = scale;

            if (wallMaterial != null)
            {
                wall.GetComponent<Renderer>().material = wallMaterial;
            }
        }
    }
}
