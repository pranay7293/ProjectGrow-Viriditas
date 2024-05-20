using UnityEngine;

namespace Systems.Bioluminescence
{
    [RequireComponent(typeof(MeshRenderer))]
    public class BioluminescentMesh : BioluminescentEffect
    {
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissiveColor");

        private MeshRenderer _meshRenderer;
        private MaterialPropertyBlock _materialPropertyBlock;

        private Color _color;
        private float _intensity;

        void Awake()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            _materialPropertyBlock = new MaterialPropertyBlock();

            if (_intensity <= 0) this.enabled = false; // Deactivate update if no intensity
        }

        void Update()
        {
            UpdateColor();
        }

        private void UpdateColor()
        {
            if (TimeOfDay.Instance == null)
                return;

            // TODO: Consider lerping or something - hard to control with exposure adjusting for drastic day/night difference
            var intensityMultiplier = TimeOfDay.Instance.LightLevel > .45f ?
                Karyo_GameCore.Instance.sceneConfiguration.BioluminescentIntensityMultiplierDay :
                Karyo_GameCore.Instance.sceneConfiguration.BioluminescentIntensityMultiplierNight;

            var color = _color * _intensity * intensityMultiplier;

            _materialPropertyBlock.SetColor(EmissionColor, color);
            _meshRenderer.SetPropertyBlock(_materialPropertyBlock);
        }

        public override void Activate(Color color, float intensity)
        {
            if (_meshRenderer == null) _meshRenderer = GetComponent<MeshRenderer>();
            if (_materialPropertyBlock == null) _materialPropertyBlock = new MaterialPropertyBlock();

            _meshRenderer.sharedMaterial.EnableKeyword("_EMISSION");

            _color = color;
            _intensity = intensity;
            UpdateColor();

            this.enabled = true; // Turn on update
        }

        public override void Deactivate()
        {
            if (_meshRenderer == null) _meshRenderer = GetComponent<MeshRenderer>();
            if (_materialPropertyBlock == null) _materialPropertyBlock = new MaterialPropertyBlock();

            _materialPropertyBlock.SetColor(EmissionColor, Color.black);
            _meshRenderer.SetPropertyBlock(_materialPropertyBlock);

            this.enabled = false; // Turn off update
        }
    }
}
