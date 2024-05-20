using Systems.Toxin;
using UnityEngine;

public class T_Toxin : Trait, IToxinEmitter
{
    protected override TraitClass ForceTraitClass => TraitClass.ToxinProduction;

    [SerializeField, Min(0)] public float radius = 50;
    [SerializeField] public Color debugColor = Color.magenta;
    [SerializeField] private AnimationCurve falloff = AnimationCurve.EaseInOut(0, 1, 1, 0);

    public float Strength => strength;
    public float Radius => radius;
    public Color DebugColor => debugColor;
    public AnimationCurve Falloff => falloff;
    public Vector3 Position => transform.position;

    private void OnDrawGizmos()
    {
        if (strength == 0) return;

        var color = debugColor;
        var center = transform.position;

        // Draw full alpha at source
        color.a = 1;
        Gizmos.color = color;
        Gizmos.DrawSphere(center, Mathf.Min(radius, 1f));
    }

    private void OnDrawGizmosSelected()
    {
        if (strength == 0) return;

        var color = debugColor;
        color.a = 1;
        Gizmos.color = color;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
