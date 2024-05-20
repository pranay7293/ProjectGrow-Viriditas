using Systems.Herding;
using UnityEngine;

public class T_Herding : Trait, IHerdingProfile
{
    protected override TraitClass ForceTraitClass => TraitClass.Herding;

    [SerializeField, Min(0)] private float visibilityDistance;
    [SerializeField, Min(0)] private float separationDistance;
    [SerializeField, Min(0)] private float projectionDistance;
    [SerializeField, Min(0)] private float separationStrength;
    [SerializeField, Min(0)] private float alignmentStrength;
    [SerializeField, Min(0)] private float cohesionStrength;
    [SerializeField, Min(0)] private float packWanderDestinationStrength = 3f;

    public float VisibilityDistance => visibilityDistance;
    public float SeparationDistance => separationDistance;
    public float ProjectionDistance => projectionDistance;
    public float SeparationStrength => separationStrength;
    public float AlignmentStrength => alignmentStrength;
    public float CohesionStrength => cohesionStrength;
    public float PackWanderDestinationStrength => packWanderDestinationStrength;
}
