using UnityEngine;

namespace AsylumHorror.World
{
    public enum ObjectiveSpawnType
    {
        Generator = 0,
        Keycard = 1,
        PowerConsole = 2,
        Hook = 3,
        Monster = 4,
        Battery = 5
    }

    public class ObjectiveSpawnPoint : MonoBehaviour
    {
        [SerializeField] private ObjectiveSpawnType spawnType = ObjectiveSpawnType.Generator;

        public ObjectiveSpawnType SpawnType => spawnType;

        public void SetType(ObjectiveSpawnType type)
        {
            spawnType = type;
        }
    }
}
