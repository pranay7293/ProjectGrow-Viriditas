using UnityEngine;

public class DEBUG_TraitEditor : MonoBehaviour
{
    private Entity _currentFocus;

    public void SetFocus(Entity currentFocus)
    {
        _currentFocus = currentFocus;
    }

    public void ButtonPressed_Trait(Trait trait)
    {
        if (!_currentFocus)
        {
            Debug.LogError("Tried to select a trait but did not have a focus.");
            return;
        }

        if (_currentFocus.TryGetComponent<TraitManager>(out var tm))
        {
            if (tm.HasTrait(trait, TraitManager.TraitMatchingType.allDataMatchesExactly) == null)
                tm.AddTrait(trait);
            else
                Debug.Log($"Entity {_currentFocus.name} not adding Trait because it already has it. Trait = {trait.ToString()}");
        }
        else
            Debug.LogError($"Target {_currentFocus.gameObject} does not have a TraitManager component.");
    }

    public void ButtonPressed_RemoveTrait(Trait trait)
    {
        if (!_currentFocus)
        {
            Debug.LogError("Tried to remove a trait but did not have a focus.");
            return;
        }

        if (_currentFocus.TryGetComponent<TraitManager>(out var tm))
        {
            if (tm.HasTrait(trait, TraitManager.TraitMatchingType.sameClass) != null)
                tm.RemoveTrait(trait);
            else
                Debug.Log($"Entity {_currentFocus.name} not removing Trait because it does not have it. Trait = {trait.ToString()}");
        }
        else
            Debug.LogError($"Target {_currentFocus.gameObject} does not have a TraitManager component.");
    }

}
