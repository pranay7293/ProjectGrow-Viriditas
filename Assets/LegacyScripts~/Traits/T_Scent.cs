using Sirenix.OdinInspector;
using Systems.Scent;
using UnityEngine;

public class T_Scent : Trait, IScentEmitter
{
    protected override TraitClass ForceTraitClass => TraitClass.ScentProduction;

    // TODO: If the scent name ever changes it must be re-registered with the scent system - this is not great...
    [SerializeField, DisableInPlayMode] private string scentName;
    [SerializeField, Min(0)] public float radius = 50;
    [SerializeField] public Color debugColor = Color.green;
    [SerializeField] private AnimationCurve falloff = AnimationCurve.EaseInOut(0, 1, 1, 0);

    public float Strength => strength;
    public float Radius => radius;
    public Color DebugColor => debugColor;
    public AnimationCurve Falloff => falloff;
    public Vector3 Position => transform.position;

    private ScentVisual[] visuals;

    public override void CopySelf(ref Trait toTrait)
    {
        base.CopySelf(ref toTrait);

        if (toTrait is not T_Scent targetScent)
        {
            Debug.LogError($"Tried to copy properties to a trait that was not T_Scent {toTrait.GetType()}, this should not happen.");
            return;
        }

        targetScent.scentName = scentName;
        targetScent.falloff = falloff;
        targetScent.debugColor = debugColor;
        targetScent.radius = radius;
        targetScent.strength = strength;
    }

    protected override void Awake()
    {
        base.Awake();
        visuals = GetComponentsInChildren<ScentVisual>();
    }

    public override void Enact()
    {
        base.Enact();
        ScentSystem.Register(scentName, this);

        // TODO: Optimize
        foreach (var scentVisual in visuals) scentVisual.UpdateEmitter(this);
    }

    public override void Rescind()
    {
        base.Rescind();
        strength = 0;
        ScentSystem.Unregister(scentName, this);

        // TODO: Optimize
        foreach (var scentVisual in visuals) scentVisual.UpdateEmitter(this);
    }

    private void OnDrawGizmos()
    {
        if (strength == 0) return;

        var color = debugColor;
        var center = transform.position;

        // Draw full alpha at source
        color.a = 1;
        Gizmos.color = color;
        Gizmos.DrawSphere(center, Mathf.Min(radius, 1f));
        Gizmos.DrawWireSphere(center, radius);
    }

    // TODO: Explore this if we need to know falloff -- like falloff won't need to change
    // private void OnDrawGizmosSelected()
    // {
    //     if (scents == null) return;
    //
    //     foreach (var scent in scents)
    //     {
    //         var color = scent.Value.debugColor;
    //         var center = transform.position;
    //
    //         const int numSteps = 4;
    //         const float minAlpha = .25f;
    //
    //         // Draw at intervals
    //         for (var i = 0; i < numSteps; ++i)
    //         {
    //             var a = (i + 1f) / numSteps;
    //
    //             // Draw at 50% radius always with some alpha
    //             color.a = Mathf.Max(minAlpha, falloff.Evaluate(a));
    //             Gizmos.color = color;
    //             Gizmos.DrawWireSphere(center, scent.Value.radius * a);
    //         }
    //     }
    // }
}
