using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Systems.Toxin
{
    [DefaultExecutionOrder(-1)]
    public class ToxinSystem : MonoBehaviour
    {
        private static ToxinSystem _instance;
        private HashSet<IToxinEmitter> activeEmitters = new HashSet<IToxinEmitter>();

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

        // Gets the overall toxin level at a given position.
        public static float GetTotalToxinLevel(Vector3 position)
        {
            if (!VerifyInstance()) return 0;

            return _instance.activeEmitters
                // Get the distance for all emitters.
                .Select(emitter => (emitter, dist: Vector3.Distance(position, emitter.Position)))
                // Filter emitters within range.
                .Where(e => e.dist < e.emitter.Radius)
                // Evaluate strength using falloff.
                .Select(e => e.emitter.Strength * e.emitter.Falloff.Evaluate(e.dist / e.emitter.Radius))
                // Additively combine.
                .Sum();
        }

        public static void Register(IToxinEmitter emitter)
        {
            if (!VerifyInstance()) return;
            _instance.activeEmitters.Add(emitter);
        }

        public static void Unregister(IToxinEmitter emitter)
        {
            if (!VerifyInstance()) return;
            _instance.activeEmitters.Remove(emitter);
        }
    }
}
