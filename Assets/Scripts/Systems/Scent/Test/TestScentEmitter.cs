using Sirenix.OdinInspector;
using UnityEngine;

namespace Systems.Scent.Test
{
    public class TestScentEmitter : MonoBehaviour, IScentEmitter
    {
        [SerializeField, Range(0, 1)] private float strength = 1;
        [SerializeField, DisableInPlayMode] private string scentName;
        [SerializeField, Min(0)] public float radius = 50;
        [SerializeField] public Color debugColor = Color.green;
        [SerializeField] private AnimationCurve falloff = AnimationCurve.EaseInOut(0, 1, 1, 0);

        public float Strength => strength;
        public float Radius => radius;
        public Color DebugColor => debugColor;
        public AnimationCurve Falloff => falloff;
        public Vector3 Position => transform.position;

        void OnEnable()
        {
            ScentSystem.Register(scentName, this);

            // TODO: Optimize
            foreach (var scentVisual in GetComponentsInChildren<ScentVisual>()) scentVisual.UpdateEmitter(this);
        }

        void OnDisable()
        {
            ScentSystem.Unregister(scentName, this);

            // TODO: Optimize
            foreach (var scentVisual in GetComponentsInChildren<ScentVisual>()) scentVisual.UpdateEmitter(this);
        }

        private void OnDrawGizmos()
        {
            if (strength == 0) return;

            var color = debugColor;
            var center = transform.position;

            // Draw full alpha at source
            color.a = 1;
            Gizmos.color = color;
            Gizmos.DrawSphere(center, Mathf.Min(radius, 1f));
            Gizmos.DrawWireSphere(center, radius);
        }
    }
}
