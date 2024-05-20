using Sirenix.OdinInspector;
using Systems.Bioluminescence;
using UnityEngine;

public class T_Bioluminescence : Trait
{
    protected override TraitClass ForceTraitClass => TraitClass.Bioluminescence;

    [SerializeField] private Color currentColor = new Color32(0, 245, 255, 255);

    private BioluminescentEffect[] _effects;

    protected override void Awake()
    {
        base.Awake();

        // TODO: Consider optimizing...
        _effects = GetComponentsInChildren<BioluminescentEffect>();
        Apply();
    }

    [Button("Apply")]
    private void Apply()
    {
        if (!Application.isPlaying)
            _effects = GetComponentsInChildren<BioluminescentEffect>();

        if (strength < Mathf.Epsilon)
        {
            foreach (var effect in _effects)
            {
                effect.Deactivate();
            }
        }
        else
        {
            foreach (var effect in _effects)
            {
                effect.Activate(currentColor, strength);
            }
        }
    }

    public override void CopySelf(ref Trait toTrait)
    {
        base.CopySelf(ref toTrait);

        if (toTrait is not T_Bioluminescence targetBioluminescence)
        {
            Debug.LogError($"Tried to copy properties to a trait that was not T_Bioluminescence {toTrait.GetType()}, this should not happen.");
            return;
        }

        targetBioluminescence.currentColor = currentColor;
        targetBioluminescence.strength = strength;
    }

    public override void Enact()
    {
        base.Enact();
        Apply();
    }

    public override void Rescind()
    {
        base.Rescind();
        strength = 0;
        Apply();
    }
}
