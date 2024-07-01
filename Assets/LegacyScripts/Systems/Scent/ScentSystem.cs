using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Systems.Scent
{
    [DefaultExecutionOrder(-1)]
    public class ScentSystem : MonoBehaviour
    {
        private static ScentSystem _instance;
        private Dictionary<string, HashSet<IScentEmitter>> _scentEmitters = new Dictionary<string, HashSet<IScentEmitter>>();

        private void Awake()
        {
            if (_instance != null)
            {
                Debug.LogError("Already had a ScentSystem defined, there should only be one!");
                Destroy(this);
                return;
            }

            _instance = this;
        }

        private static bool VerifyInstance()
        {
            if (_instance == null)
            {
                Debug.LogError("No ScentSystem registered. Make sure to add one to the scene!");
                return false;
            }

            return true;
        }

        // TODO: Consider adding an average of strength positions so multiple things can increase strength in a region

        public static bool TryFindStrongestScent(Vector3 position, string scent, out float scentStrength, out IScentEmitter scentEmitter)
        {
            if (!VerifyInstance())
            {
                scentStrength = 0;
                scentEmitter = null;
                return false;
            }

            if (!_instance._scentEmitters.TryGetValue(scent, out var emittersForScent))
            {
                // No emitters
                scentStrength = 0;
                scentEmitter = null;
                return false;
            }

            IScentEmitter strongestEmitter = null;
            var strongestEmission = 0f;

            // TODO: We could optimize this to have the emissions register which scents in a dictionary to not cycle all of them, but this is fine for now
            foreach (var emitter in emittersForScent)
            {
                var emission = GetScentStrength(position, emitter);

                if (emission > strongestEmission)
                {
                    strongestEmission = emission;
                    strongestEmitter = emitter;
                }
            }

            if (strongestEmitter == null)
            {
                scentStrength = 0;
                scentEmitter = null;
                return false;
            }

            scentEmitter = strongestEmitter;
            scentStrength = strongestEmission;
            return true;
        }

        private static float GetScentStrength(Vector3 position, IScentEmitter emitter)
        {
            // Check if in radius
            var dist = (position - emitter.Position).magnitude;
            var scentRadius = emitter.Radius;

            // Out of range
            if (dist > scentRadius)
            {
                return 0;
            }

            // Evaluate strength at point
            var strengthWithFalloff = emitter.Strength * emitter.Falloff.Evaluate(dist / scentRadius);
            return strengthWithFalloff;
        }

        public static void Register(string scentName, IScentEmitter emitter)
        {
            if (!VerifyInstance()) return;

            if (!_instance._scentEmitters.TryGetValue(scentName, out var emittersForScent))
            {
                emittersForScent = new HashSet<IScentEmitter>();
                _instance._scentEmitters.Add(scentName, emittersForScent);
            }

            emittersForScent.Add(emitter);
        }

        public static void Unregister(string scentName, IScentEmitter emitter)
        {
            if (!VerifyInstance()) return;

            if (_instance._scentEmitters.TryGetValue(scentName, out var emittersByScent))
            {
                emittersByScent.Remove(emitter);
            }
            else
            {
                Debug.LogError($"Could not find {emitter} under {scentName} -- this is bad!");
            }
        }
    }
}
