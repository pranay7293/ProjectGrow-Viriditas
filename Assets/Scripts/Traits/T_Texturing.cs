using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class T_Texturing : Trait
{
    public Material material;  // the material to change the Entity to

    private List<MeshRenderer> listOfAllMeshRenderers;  // re-used by GetAllMeshRenderers() below

    public override bool AreExactlyTheSame(Trait trait)
    {
        T_Texturing toCompare = trait as T_Texturing;
        if (toCompare == null)
            return false;  // they aren't the same type, so they aren't the same Trait

        return (base.AreExactlyTheSame(trait) && material == (toCompare.material));
    }

    public override void CopySelf(ref Trait toTrait)
    {
        base.CopySelf(ref toTrait);

        T_Texturing target = null;

        if (toTrait is T_Texturing)
            target = (T_Texturing)toTrait;
        else
        {
            Debug.LogError($"CopySelf passed a Trait that does not match its type.  My type = {this.GetType().FullName}, passed-in type = {toTrait.GetType().FullName}");
            return;
        }

        target.material = material;
    }

    public override string ToString()
    {
        string toReturn = base.ToString() + ", material: " + material.ToString();
        return toReturn;
    }

    protected override void Awake()
    {
        base.Awake();

        listOfAllMeshRenderers = new List<MeshRenderer>();
    }



    public override void Enact()
    {
        // change all materials associated with the Entity to use the class's assigned material instead.
        GetAllMeshRenderers(gameObject);

        foreach (MeshRenderer mr in listOfAllMeshRenderers)
        {
            Material[] currentMaterials = mr.materials;
            for (int i = 0; i < currentMaterials.Length; i++)
                currentMaterials[i] = material;

            mr.materials = currentMaterials;
        }
    }

    public override void Rescind()
    {
        // TODO - do this
        Debug.Log("Rescinding the Texturing Trait isn't currently supported.  It probably wouldn't be hard to do.");
    }



    // returns a list of all mesh renderers on passed-in GameObject and its children
    private void GetAllMeshRenderers(GameObject go)
    {
        listOfAllMeshRenderers.Clear();
        GetMeshRenderersRecursively(go.transform);

        // listOfAllMeshRenderers is now set to the correct list
    }

    private void GetMeshRenderersRecursively(Transform parent)
    {
        MeshRenderer mr = parent.GetComponent<MeshRenderer>();
        if (mr != null)
            listOfAllMeshRenderers.Add(mr);

        foreach (Transform child in parent)
            GetMeshRenderersRecursively(child);
    }


}
