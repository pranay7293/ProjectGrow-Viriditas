using UnityEngine;
using UnityEngine.VFX;

namespace Systems.Toxin
{
    [RequireComponent(typeof(VisualEffect))]
    public class ToxinVisual : MonoBehaviour
    {
        private VisualEffect _effect;
        private ParticleSystem _particleSys;

        // TODO: Replace this with a more interesting visual effect.
        public void UpdateVisual(IToxinEmitter emitter, bool visualEnabled)
        {
            if (_effect == null) _effect = GetComponent<VisualEffect>();
            if (_particleSys == null) _particleSys = GetComponent<ParticleSystem>();

            if (!visualEnabled)
            {
                _effect.Stop();
                _particleSys.Stop();
                return;
            }

            //_effect.SetVector4("Color", emitter.DebugColor);
            _effect.Play();
            _particleSys.Play();
        }
    }
}
