using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class T_Elastic : Trait
{
    protected override TraitClass ForceTraitClass => TraitClass.Elastic;

    public PhysicMaterial elasticMaterial;
    private PhysicMaterial originalMaterial;


    public override bool AreExactlyTheSame(Trait trait)
    {
        T_Elastic toCompare = trait as T_Elastic;
        if (toCompare == null)
            return false;  // they aren't the same type, so they aren't the same Trait

        return (base.AreExactlyTheSame(trait) && elasticMaterial == (toCompare.elasticMaterial));
    }

    public override void CopySelf(ref Trait toTrait)
    {
        base.CopySelf(ref toTrait);

        T_Elastic target = null;

        if (toTrait is T_Elastic)
            target = (T_Elastic)toTrait;
        else
        {
            Debug.LogError($"CopySelf passed a Trait that does not match its type.  My type = {this.GetType().FullName}, passed-in type = {toTrait.GetType().FullName}");
            return;
        }

        target.elasticMaterial = elasticMaterial;
    }

    public override string ToString()
    {
        string toReturn = base.ToString() + ", elasticMaterial: " + elasticMaterial.ToString();
        return toReturn;
    }

    protected override void Awake()
    {
        base.Awake();

        Collider col = GetMyCollider();

        if (col != null)
            originalMaterial = col.material;
    }

    public override void Enact()
    {
        Collider col = GetMyCollider();

        if (col != null)
            col.material = elasticMaterial;
    }

    public override void Rescind()
    {
        Collider col = GetMyCollider();

        if (col != null)
            col.material = originalMaterial;
    }


    private Collider GetMyCollider()
    {
        // if the Entity itself has a collider, that's the one we're looking for
        Collider col = gameObject.GetComponent<Collider>();
        if (col != null)
            return col;

        // otherwise, do this special case thing
        // TODO - this is specific to cup and ball flora
        Transform ball = transform.Find("Ball");

        if (ball != null)
        {
            col = ball.GetComponent<Collider>();
            if (col != null)
                return col;
        }

        // lastly if you didn't find that collider, return the first one you find on a child
        col = GetComponentInChildren<Collider>();
        if (col != null)
            return col;

        Debug.LogError("Elastic Trait added to an Entity with no Collider - this Entity should be incompatible with this Trait");
        return null;

    }

}
