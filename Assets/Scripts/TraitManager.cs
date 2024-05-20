using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

// This class goes on an Entity and manages its list of Traits.  If you want to add or remove a Trait from an entity, call this class.
// It is responsible for behaviors like automatically removing traits when they time out.
// It also tells the AI when Traits have changed so the AI can determine which behaviors should change.
// Long term it will probably also help with Trait compatibility, ie - which traits are allowed to go on which Entities and similar.

public class TraitManager : MonoBehaviour
{
    // this is a list of Trait components on this gameObject, but only those which are currently active and enabled.  
    // in gameplay terms an Entity is not considered to have a Trait if either: the corresponding component is not on the Entity -or- is not enabled.
    // so this is a list of Traits the entity actually has
    private List<Trait> traits;

    // always call the following 2 methods instead of traits.contains()

    // Does this entity have the matching Trait? (in gameplay terms)
    // returns the matching Trait or null if none
    // TODO - move some of this testing logic to Trait class, not here
    public Trait HasTrait(Trait trait, TraitMatchingType match)
    {
        foreach (Trait t in traits)
        {
            if (t.isActiveAndEnabled)
            {
                if (match == TraitMatchingType.allDataMatchesExactly)
                {
                    if (trait.AreExactlyTheSame(t))
                        return t;
                }
                else if (match == TraitMatchingType.sameClass)
                {
                    if (trait.traitClass == t.traitClass)
                        return t;
                }
                else if (match == TraitMatchingType.sameClassAndName)
                {
                    if ((trait.traitClass == t.traitClass) && (trait.name == t.name))
                        return t;
                }
            }
        }
        return null;
    }

    public enum TraitMatchingType
    {
        sameClass,
        sameClassAndName,
        allDataMatchesExactly
    }

    // Looks for a matching Trait component where that component is not enabled (and therefore 
    // in gameplay terms the entity is not considered to have that Trait)
    // returns the matching Trait or null if none
    // this one is used internally only for reusing Traits by turning components on and off rather than adding and removing them entirely
    private Trait FindDisabledTrait(Trait trait, TraitMatchingType match)
    {
        List<Trait> fullListOfTraits = gameObject.GetComponents<Trait>().ToList();

        foreach (Trait t in fullListOfTraits)
        {
            if (!t.isActiveAndEnabled)
            {
                if (match == TraitMatchingType.allDataMatchesExactly)
                {
                    if (trait.AreExactlyTheSame(t))
                        return t;
                }
                else if (match == TraitMatchingType.sameClass)
                {
                    if (trait.traitClass == t.traitClass)
                        return t;
                }
                else if (match == TraitMatchingType.sameClassAndName)
                {
                    if ((trait.traitClass == t.traitClass) && (trait.name == t.name))
                        return t;
                }
            }
        }
        return null;
    }

    // TODO - work this into the above tests
    public static bool DoTraitsMatch(Trait a, Trait b, TraitMatchingType match)
    {
        if (a == null || b == null)
        {
            Debug.LogError($"DoTraitsMatch() called with null trait.  a = {a.ToString()} b = {b.ToString()}");
            return false;
        }

        if (match == TraitMatchingType.allDataMatchesExactly)
        {
            if (a.AreExactlyTheSame(b))
                return true;
        }
        else if (match == TraitMatchingType.sameClass)
        {
            if (a.traitClass == b.traitClass)
                return true;
        }
        else if (match == TraitMatchingType.sameClassAndName)
        {
            if ((a.traitClass == b.traitClass) && (a.name == b.name))
                return true;
        }

        return false;
    }


    private Karyo_GameCore core;
    private AI myAI;

    public bool DEBUG_verbose;


    private void Awake()
    {
        core = GameObject.FindGameObjectWithTag("GameCore").GetComponent<Karyo_GameCore>();
        if (core == null)
            Debug.LogError(this + " cannot find Game Core.");

        myAI = GetComponent<AI>();

        // Collect traits but don't initialize them yet.
        traits = GatherExistingEnabledTraits();
    }

    private void Start()
    {
        // Initialize traits.
        foreach (var trait in traits)
        {
            HandleTraitAddedSetup(trait);
        }
    }


    // when you add a Trait of the same class as an existing Trait on an Entity, the passed-in data clobbers the existing data (eg - bioluminescence changes from blue to red, both are not active at once)
    // for code rigor purposes, it will complain if you try to add a trait that already exists on the entity (ie - where all data is exactly the same)
    public void AddTrait(Trait toAdd)
    {
        if (DEBUG_verbose)
            Debug.Log($"Entity {name} attempting to add Trait {toAdd.traitClass}, {toAdd.name}.");

        if (toAdd == null)
        {
            Debug.LogWarning("AddTrait called with null Trait");
            return;
        }

        if ((HasTrait(toAdd, TraitMatchingType.allDataMatchesExactly) != null))
        {
            Debug.LogWarning($"AddTrait called with a Trait this Entity already has. Entity = {name}, Trait = {toAdd.ToString()}");
            return;
        }

        // check to see if we have a Trait of the same class and remove it if so  (we want to remove it so that rescind etc gets called)
        Trait trait = HasTrait(toAdd, TraitMatchingType.sameClass);
        if (trait != null)
        {
            if (DEBUG_verbose)
                Debug.Log($"Entity {name} already has Trait of class {trait.traitClass}, removing the old Trait to replace it with the new Trait.");

            RemoveTrait(trait);
        }

        // look for a disabled component that we can reuse
        trait = FindDisabledTrait(toAdd, TraitMatchingType.sameClass);   // TODO - really we want to use a disabled Trait of the same actual class, not traitClass, since we are just reusing Unity components here

        if (trait != null)
        {
            // if the component already exists, we've found an old one that we're going to reuse
            trait.enabled = true;

            if (DEBUG_verbose)
                Debug.Log("Entity " + gameObject + " had a disabled Trait component of class " + trait.traitClass + ", enabling it.");
        }
        else
        {
            // if the component doesn't exist, add the Trait component to the GameObject 
            Type traitType = toAdd.GetType();
            trait = (Trait)gameObject.AddComponent(traitType);

            if (DEBUG_verbose)
                Debug.Log("Entity " + gameObject + " did not have Trait component " + trait.name + ", adding it.");
        }

        // give it the parameters of the passed-in toAdd Trait
        toAdd.CopySelf(ref trait);

        traits.Add(trait);
        HandleTraitAddedSetup(trait);
    }

    // removes the first Trait it finds of the same class as the passed-in Trait
    public void RemoveTrait(Trait toRemove)
    {
        if (DEBUG_verbose)
            Debug.Log($"Entity {name} attempting to remove Trait {toRemove.name}");

        if (toRemove == null)
        {
            Debug.LogWarning("RemoveTrait called with null Trait");
            return;
        }
        Trait target = HasTrait(toRemove, TraitMatchingType.sameClass);
        if (target == null)
        {
            Debug.LogWarning($"RemoveTrait called with a Trait this Entity does not have. Entity = {name}, Trait = {toRemove.ToString()}");
            return;
        }

        HandleTraitRemovedTeardown(target);
        traits.Remove(target);
    }

    // Trait initialization for traits added at startup or during gameplay.
    private void HandleTraitAddedSetup(Trait trait)
    {
        if (trait.duration <= 0)
            trait.duration = -1;  // internally -1 means infinite duration
        else
            trait.StartTimer();

        // inform the AI that the trait has been added
        if (myAI != null)
            myAI.TraitHasBeenAdded(trait);

        trait.Enact();

        if (DEBUG_verbose)
            Debug.Log($"Entity {gameObject}: Trait {trait.name} has been enacted and added. Trait data = {trait}");
    }

    // Trait teardown for traits removed at startup (could happen for collisions) or during gameplay.
    private void HandleTraitRemovedTeardown(Trait trait)
    {
        // Cancel any running timer. No effect for traits with infinite duration.
        trait.CancelTimer();

        // inform the AI that the trait has been removed
        if (myAI != null)
            myAI.TraitHasBeenRemoved(trait);

        trait.Rescind();
        trait.enabled = false;

        if (DEBUG_verbose)
            Debug.Log($"Entity {gameObject}: Trait {trait.name} has been rescinded and removed.");
    }


    // returns the list of traits that are on this Entity and currently enabled
    private List<Trait> GatherExistingEnabledTraits()
    {
        return GetComponents<Trait>().Where(t => t.enabled).ToList();
    }


    private void Update()
    {
        if (traits.Count > 0)
            UpdateTraitTimers();
    }


    private void UpdateTraitTimers()
    {
        List<Trait> toRemove = new List<Trait>();

        foreach (Trait trait in traits)
            if (trait.duration != -1f)
                if (trait.UpdateTimer(Time.deltaTime))
                    toRemove.Add(trait);

        foreach (Trait trait in toRemove)
        {
            if (DEBUG_verbose)
                Debug.Log($"Trait {trait.name} duration has expired, removing. Full trait data = {trait.ToString()}");

            RemoveTrait(trait);
        }

    }

}
