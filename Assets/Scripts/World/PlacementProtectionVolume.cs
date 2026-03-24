using UnityEngine;

namespace AsylumHorror.World
{
    [ExecuteAlways]
    public class PlacementProtectionVolume : MonoBehaviour
    {
        [SerializeField] private Vector3 size = new Vector3(4f, 3f, 4f);
        [SerializeField] private float padding = 0.2f;

        public Bounds WorldBounds
        {
            get
            {
                Vector3 scaledSize = Vector3.Scale(size, transform.lossyScale);
                return new Bounds(transform.position, scaledSize);
            }
        }

        public float Padding => padding;

        public void SetSize(Vector3 nextSize)
        {
            size = nextSize;
        }

        public void SetPadding(float nextPadding)
        {
            padding = Mathf.Max(0f, nextPadding);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.85f, 0.15f, 0.12f, 0.22f);
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.DrawCube(Vector3.zero, size);
            Gizmos.color = new Color(0.95f, 0.22f, 0.18f, 0.72f);
            Gizmos.DrawWireCube(Vector3.zero, size + Vector3.one * (padding * 2f));
        }
    }
