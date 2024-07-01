using UnityEngine;

namespace Systems.Bioluminescence
{
    public abstract class BioluminescentEffect : MonoBehaviour
    {
        public abstract void Activate(Color color, float intensity);
        public abstract void Deactivate();
    }
}
