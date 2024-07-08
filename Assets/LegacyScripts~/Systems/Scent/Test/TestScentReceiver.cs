using UnityEngine;
using UnityEngine.AI;

namespace Systems.Scent.Test
{
    public class TestScentReceiver : MonoBehaviour
    {
        [SerializeField] private string scentToFind = "flower";

        private float scentStrength = 0;
        private Vector3? scentPosition;

        private bool hasPath = false;
        private NavMeshPath currentPath;

        private bool TryFindStrongestScentSource(string scent, out float scentStrength, out Vector3 position)
        {
            var found = ScentSystem.TryFindStrongestScent(transform.position, scent, out scentStrength, out var emitter);
            position = emitter?.Position ?? Vector3.zero;
            return found;
        }

        void Awake()
        {
            currentPath = new NavMeshPath();
        }

        private void Update()
        {
            if (string.IsNullOrEmpty(scentToFind))
            {
                scentPosition = null;
                hasPath = false;
                return;
            }

            if (TryFindStrongestScentSource(scentToFind, out var strength, out var position))
            {
                scentPosition = position;
                scentStrength = strength;

                hasPath = NavMesh.CalculatePath(transform.position, position, ~0, currentPath);
            }
            else
            {
                scentStrength = 0;
                scentPosition = null;
                hasPath = false;
            }
        }

        private void OnDrawGizmos()
        {
#if UNITY_EDITOR
            var style = new GUIStyle();
            style.normal.textColor = Color.black;
            UnityEditor.Handles.Label(transform.position, $"strength: {scentStrength}", style);
#endif

            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(transform.position, .5f);

            if (hasPath)
            {
                Gizmos.color = Color.red;

                for (var i = 0; i < currentPath.corners.Length - 1; ++i)
                {
                    var pointA = currentPath.corners[i];
                    var pointB = currentPath.corners[i + 1];

                    Gizmos.DrawLine(pointA, pointB);
                }
            }
        }
    }
}
