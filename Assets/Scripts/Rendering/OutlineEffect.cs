using UnityEngine;

namespace Rendering
{
    [ExecuteInEditMode]
    public class OutlineEffect : MonoBehaviour, ITargetAcquisitionListener
    {
        // TODO: Use Odin to autofill
        [SerializeField] private MeshRenderer[] toOutline;
        [SerializeField] private Material outlineMaterial;

        private bool _isSelected = false;

        private void Update()
        {
            // If not selected or no material set, return
            if (!_isSelected) return;

            DrawOutline();
        }

        // TODO: Add editor preview?
        private void DrawOutline()
        {
            if (!outlineMaterial) return;

            // Programatically draw the outlines
            foreach (var meshRenderer in toOutline)
            {
                var meshTransform = meshRenderer.transform;
                var mesh = meshRenderer.GetComponent<MeshFilter>().sharedMesh;
                Graphics.DrawMesh(mesh, meshTransform.localToWorldMatrix, outlineMaterial, 0);
            }
        }

        public void OnTargetAcquired()
        {
            _isSelected = true;
        }

        public void OnTargetLost()
        {
            _isSelected = false;
        }
    }
}
