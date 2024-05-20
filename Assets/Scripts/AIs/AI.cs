using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using Unity.VisualScripting;

// this is the baseAI class which is responsible for determining which Behaviors the Entity should engage in.  
// it could just as easily be called BehaviorManager
// along with an Entity class, it goes on each instance of an Entity.
// Entities that have no behaviors don't need an AI

public class AI : MonoBehaviour
{

    private Entity entity;

    private List<Behavior> _behaviors;
    private List<Behavior> Behaviors
    {
        get
        {
            if (_behaviors == null)
            {
                UpdateListOfBehaviors();
            }
            return _behaviors;
        }
    }

    public bool DEBUG_verbose = false;

    protected virtual void Awake()
    {
        entity = GetComponent<Entity>();
        if (entity == null)
            Debug.LogError("AI " + gameObject + " does not have an Entity component, but it needs one.");
    }


    // all deriving classes should extend this
    public virtual void TraitHasBeenAdded(Trait trait)
    {
        switch (trait.traitClass)
        {
            case TraitClass.ToxinProduction:
                var b_production = GetBehavior<B_Production>();
                if (b_production != null)
                {
                    b_production.behaviorEnabled = true;
                    b_production.ToxinToProduce = trait as T_Toxin;
                }
                break;
        }
    }

    // all deriving classes should extend this
    public virtual void TraitHasBeenRemoved(Trait trait)
    {
        switch (trait.traitClass)
        {
            case TraitClass.ToxinProduction:
                var b_production = GetBehavior<B_Production>();
                if (b_production != null)
                {
                    b_production.behaviorEnabled = false;
                }
                break;
        }
    }


    private void UpdateListOfBehaviors()
    {
        _behaviors = GetComponents<Behavior>().ToList();

        if (DEBUG_verbose)
        {
            string toLog = new string("");
            toLog = toLog + "Entity " + gameObject + " updated list of behaviors. Here is the new list: ";

            foreach (Behavior b in _behaviors)
                toLog = toLog + b.ToString() + ", ";

            Debug.Log(toLog);
        }
    }

    protected T GetBehavior<T>() where T : Behavior
    {
        // search the behaviors list for a behavior of matching type and return it, or return null if not found
        foreach (Behavior behavior in Behaviors)
        {
            if (behavior is T)
                return behavior as T;
        }
        return null;
    }

}
