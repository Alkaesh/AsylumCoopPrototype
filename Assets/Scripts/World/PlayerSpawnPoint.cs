using UnityEngine;

namespace AsylumHorror.World
{
    public class PlayerSpawnPoint : MonoBehaviour
    {
        [SerializeField] private int spawnIndex;

        public int SpawnIndex => spawnIndex;

        public void SetIndex(int index)
        {
            spawnIndex = index;
        }
    }
}
