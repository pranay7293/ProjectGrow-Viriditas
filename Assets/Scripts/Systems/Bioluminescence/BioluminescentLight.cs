using UnityEngine;

namespace Systems.Bioluminescence
{
    [RequireComponent(typeof(Light))]
    public class BioluminescentLight : BioluminescentEffect
    {
        private Light _light;

        void Awake()
        {
            _light = GetComponent<Light>();
            _light.enabled = false;
        }

        public override void Activate(Color color, float intensity)
        {
            if (_light == null) _light = GetComponent<Light>();

            _light.enabled = true;
            _light.color = color;
            _light.intensity = intensity;
        }

        public override void Deactivate()
        {
            if (_light == null) _light = GetComponent<Light>();

            _light.enabled = false;
        }
    }
}
