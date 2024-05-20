using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This is the base class for Traits.
// Traits can actively do something to the Entity they are on.  If so, they override the Enact() method to make that change, and Rescind() to remove the change.
// Traits can map onto Behaviors in Entities, subject to other classes on the Entity, most notably AI.
// Specialized Traits can inherit from this class to provide more data.  Eg - a texturing Trait can contain the Texture the Entity should use.

[SerializeField]
public enum TraitClass
{
    NONE,
    Texturing,
    Antigravity,
    Bioluminescence,
    ScentProduction,
    Elastic,
    ScentAttract,
    ScentRepel,
    ImmuneToxin,
    Photosynthesis,
    ResistantMold,
    ToxinProduction,
    FibersHydrophobic,
    Threatened,
    Herding
}

public class Trait : MonoBehaviour
{
    [SerializeField]
    public TraitClass traitClass;
    public new string name;  // the short, player-facing name
    public string description;  // TODO - not currently displayed anywhere
    public string ODS_text;  // text used by the Organism Data Sheet
    public float strength;  // is interpreted differently by different Traits
    public float duration;  // in seconds
    public float timeRemaining { get; private set; }

    // these are variables that affect a Module this Trait/output is added to
    public float capacityDrain; // should be negative  TODO - make these 3 accessible in the editor but readonly to other classes (by using a backing variable)
    public float fitnessDrain; // should be negative
    public float complexity; // should be positive

    // Specific trait implementations should override this to the specific
    // trait class they're meant for, so its not editable.
    protected virtual TraitClass ForceTraitClass { get; set; } = TraitClass.NONE;

    private Karyo_GameCore core;

    public void StartTimer()
    {
        timeRemaining = duration;

        Karyo_GameCore.Instance.uiManager.CreateTraitTimer(this, Vector3.up * 3f); // TODO: potentially add per entity offset
    }

    public void CancelTimer()
    {
        timeRemaining = 0f;
    }

    public bool UpdateTimer(float timeElapsed)
    {
        timeRemaining -= timeElapsed;
        if (timeRemaining < 0f)
            return true;
        else
            return false;
    }

    // all classes that inherit from Trait should override this
    // copy the data from this Trait to a Trait of the same class
    public virtual void CopySelf(ref Trait toTrait)
    {
        toTrait.traitClass = traitClass;
        toTrait.name = name;
        toTrait.strength = strength;
        toTrait.duration = duration;
    }

    // all classes that inherit from Trait should override this
    // returns true only if the class, name and all parameters are identical
    public virtual bool AreExactlyTheSame(Trait trait)
    {
        if (traitClass != trait.traitClass)
            return false;
        if (name != trait.name)
            return false;
        if (strength != trait.strength)
            return false;
        if (duration != trait.duration)
            return false;

        return true;
    }

    // all classes that inherit from Trait should override this
    public override string ToString()
    {
        string toReturn = new string("");
        toReturn = name + ", class: " + traitClass.ToString() + ", strength: " + strength + ", duration: " + duration.ToString();
        return toReturn;
    }

    // override this if the Trait does something active
    // this is called when the Trait is added and may be called at other times as well
    public virtual void Enact()
    {

    }
    // override this if the Trait does something active
    // this is called when the Trait is removed and may be called at other times as well
    public virtual void Rescind()
    {

    }

    // TODO - is there also a Remove() which is when the Entity defaults to having the Trait and this is what happens when it is removed?  eg - Elastic, or Texturing?
    protected virtual void Awake()
    {
        var coreGameObject = GameObject.FindGameObjectWithTag("GameCore");

        if (coreGameObject == null || !coreGameObject.TryGetComponent(out core))
            Debug.LogError(this + " cannot find Game Core.");
    }

    // This is overridden so we can see the enabled/disabled checkbox in the inspector.
    private void Start() { }

    private void OnValidate()
    {
        if (ForceTraitClass != TraitClass.NONE)
        {
            traitClass = ForceTraitClass;
        }
    }
}
