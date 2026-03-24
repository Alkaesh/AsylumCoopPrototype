using System.Collections.Generic;
using AsylumHorror.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace AsylumHorror.World
{
    public class RuntimeNavMeshBootstrap : MonoBehaviour
    {
        [SerializeField] private bool bakeOnStart = true;
        [SerializeField] private int navMeshAgentTypeId = 0;
        [SerializeField] private Vector3 navMeshBoundsSize = new Vector3(220f, 50f, 220f);

        private NavMeshDataInstance navMeshInstance;

        private void Start()
        {
            if (bakeOnStart)
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

        public void BuildRuntimeNavMesh()
        {
            if (navMeshInstance.valid)
            {
                navMeshInstance.Remove();
            }

            NetworkDoor[] doors = FindObjectsByType<NetworkDoor>();
            foreach (NetworkDoor door in doors)
            {
                if (door != null)
                {
                    door.SetBakePassThroughMode(true);
                }
            }

            List<NavMeshBuildSource> sources = new List<NavMeshBuildSource>();
            List<NavMeshBuildMarkup> markups = new List<NavMeshBuildMarkup>();
            try
            {
                NavMeshBuilder.CollectSources(null, Physics.DefaultRaycastLayers, NavMeshCollectGeometry.PhysicsColliders, 0, markups, sources);

                NavMeshBuildSettings settings = NavMesh.GetSettingsByID(navMeshAgentTypeId);
                Bounds bounds = new Bounds(Vector3.zero, navMeshBoundsSize);
                NavMeshData navMeshData = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);

                if (navMeshData != null)
                {
                    navMeshInstance = NavMesh.AddNavMeshData(navMeshData);
                }
            }
            finally
            {
                foreach (NetworkDoor door in doors)
                {
                    if (door != null)
                    {
                        door.SetBakePassThroughMode(false);
                    }
                }
            }
        }
    }
}
