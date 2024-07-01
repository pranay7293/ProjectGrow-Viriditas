using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// a Genome Map is associated with an entity type / species and describes the Traits of the species (which can be transfered via circuits to entities 
// of other species) and the slots this species has for transferring incoming Traits from other species.

public class GenomeMap : MonoBehaviour
{
    [System.Serializable]
    public class ModuleSlot
    {
        public Module currentModule; // the Module which is in this slot right now
        public Module defaultModule; // the Module normally in this slot for the species (before any edits take place)  TODO - should be read only
        public bool Unremovable;  // TODO - should be readonly
        [SerializeField] private List<TraitClass> incompatibilities;  // a ModuleSlot is compatible with all outputs not in this list (ie - if a module has an output in this list, it is not compatible)
        public float capacity;  // TODO - should be readonly

        public bool isEmpty => ((currentModule == null) || (currentModule.output == null));

        // returns true if the passed-in trait is compatible with this slot
        public bool IsCompatible(Trait trait)
        {
            foreach (TraitClass incompatibility in incompatibilities)
                if (trait.traitClass == incompatibility)
                    return false;

            return true;
        }

        public override string ToString ()
        {
            string toReturn = new string("ModuleSlot ");

            string currentMod = new string("Empty");
            if (currentModule != null)
                currentMod = currentModule.ToString();

            string defaultMod = new string("Empty");
            if (defaultModule != null)
                defaultMod = defaultModule.ToString();

            toReturn = toReturn + currentMod + "/" + defaultMod;

            toReturn = toReturn + ", Unremovable: " + Unremovable.ToString();
            toReturn = toReturn + ", Capacity: " + capacity.ToString();

            return toReturn;
        }
    }

    public ModuleSlot[] moduleSlots;

    private OrganismDataSheet _ods;
    public OrganismDataSheet ods
    {
        get
        {
            if (_ods == null)
            {
                _ods = GetComponent<OrganismDataSheet>();

                if (_ods == null)
                    Debug.LogError($"No OrganismDataSheet component on {gameObject.name} wich has GenomeMap.");
            }

            return _ods;
        }
    }



}
