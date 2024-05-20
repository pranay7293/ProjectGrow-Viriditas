using UnityEngine;
using UnityEngine.VFX;

namespace Systems.Scent
{
    [RequireComponent(typeof(VisualEffect))]
    public class ScentVisual : MonoBehaviour
    {
        private VisualEffect _effect;

        public void UpdateEmitter(IScentEmitter emitter)
        {
            if (_effect == null) _effect = GetComponent<VisualEffect>();

            if (emitter.Strength <= 0)
            {
                _effect.Stop();
                return;
            }

            _effect.SetVector4("Color", emitter.DebugColor);
            _effect.Play();
        }
    }
}
