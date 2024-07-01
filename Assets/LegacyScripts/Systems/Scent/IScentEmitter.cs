
using UnityEngine;

namespace Systems.Scent
{
    public interface IScentEmitter
    {
        float Strength { get; }
        Vector3 Position { get; }
        AnimationCurve Falloff { get; }
        float Radius { get; }
        Color DebugColor { get; }
    }
}
