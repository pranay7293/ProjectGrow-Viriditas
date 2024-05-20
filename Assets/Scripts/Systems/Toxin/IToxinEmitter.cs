
using UnityEngine;

namespace Systems.Toxin
{
    public interface IToxinEmitter
    {
        float Strength { get; }
        Vector3 Position { get; }
        AnimationCurve Falloff { get; }
        float Radius { get; }
        Color DebugColor { get; }
    }
}
