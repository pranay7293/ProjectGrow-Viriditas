using UnityEngine;

namespace Systems.Herding
{
    public interface IHerdingProfile
    {
        float VisibilityDistance { get; }
        float SeparationDistance { get; }
        float ProjectionDistance { get; }
        float SeparationStrength { get; }
        float AlignmentStrength { get; }
        float CohesionStrength { get; }
        float PackWanderDestinationStrength { get; }
    }
}
