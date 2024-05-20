using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Tools
{
    [Serializable]
    public class Circuit
    {
        public enum ModuleEditModifier
        {
            Add,
            Remove
        }

        [Serializable]
        public class ModuleEdit
        {
            public Module module;
            public OrganismDataSheet sourceOrganism;  // which organism did this module come from?   null when modifier == remove
            public ModuleEditModifier modifier;

            // TODO - Module should be a ScriptableObject so it can be both edited in the inspector as game data and created at runtime, per here.  Instead of this terrible solution of attaching them to GameObjects.
            public ModuleEdit (Transform folder)
            {
                GameObject moduleHolder = new GameObject();
                moduleHolder.transform.parent = folder;
                module = moduleHolder.AddComponent<Module>();
                modifier = ModuleEditModifier.Add;  // TODO - support remove
            }
        }

        [SerializeField] public OrganismDataSheet targetOrganism;
        [SerializeField] public ModuleEdit[] moduleEdits;  // the size of this array should correspond to the number of ModuleSlots this organism type has, even if some are null (null means "no edit in this position")

        public float complexity
        {
            get
            {
                float toReturn = 0f;

                foreach (ModuleEdit edit in moduleEdits)
                    if (edit != null)
                        if (edit.module != null)
                            toReturn += edit.module.totalComplexity;

                return toReturn;
            }
        }

        private string cachedDesciption = null;

        public string TextDescription
        {
            get
            {
                if (true) //  we want to refresh the descritpion always, since circuits change during design.   cachedDesciption == null)
                {
                    UpdateTextDescription();
                }

                return cachedDesciption;
            }
        }

        public Circuit(OrganismDataSheet targetOrganism, ModuleEdit[] moduleEdits)
        {
            this.targetOrganism = targetOrganism;
            this.moduleEdits = moduleEdits;
            UpdateTextDescription();
        }
        public Circuit (OrganismDataSheet targetOrganism)
        {
            this.targetOrganism = targetOrganism;
            int numSlots = targetOrganism.genomeMap.moduleSlots.Length;
            moduleEdits = new ModuleEdit[numSlots];
            UpdateTextDescription();
        }

        public void CreateEdit (int index, Transform parentFolder)
        {
            if ((index < 0) || (index >= moduleEdits.Length))
            {
                Debug.LogError($"CreateEdit called with bad index = {index}");
                return;
            }
            moduleEdits[index] = new ModuleEdit(parentFolder);
        }

        // TODO - improve this to include other module parts like inputs and logOps?
        private void UpdateTextDescription()
        {
            var builder = new StringBuilder();

            builder.Append($"<i>{targetOrganism.nameScientific}</i>\n{targetOrganism.nameCommon}\n\n");

            foreach (var edit in moduleEdits)
            {
                if (edit != null)
                {
                    switch (edit.modifier)
                    {
                        case ModuleEditModifier.Add:
                            builder.Append("+ ");
                            break;
                        case ModuleEditModifier.Remove:
                            builder.Append("- ");
                            break;
                        default:
                            Debug.LogError($"Injection had unsupported modifier type [{edit.modifier}] for trait [{edit.module.name}]");
                            builder.Append("? ");
                            break;
                    }

                    // we want to know the name of this trait as the player knows it based on how much they've scanned the organism they are taking it from
                    string trait_name = GenomeMapUI.GetTraitName(edit.sourceOrganism, edit.module.output);
                    builder.AppendLine(trait_name);
                }
            }

            cachedDesciption = builder.ToString();
        }

        public override string ToString()
        {
            UpdateTextDescription();
            return cachedDesciption;
        }

        public bool IsCompatible(Entity targetEntity)
        {
            // TODO: Do we want to do any other checks? Specifically on traits it already has or anything?
            return targetEntity && targetEntity.organismDataSheet == targetOrganism;
        }

        public bool IsValid ()
        {
            if (targetOrganism == null)
                return false;

            foreach (var moduleEdit in moduleEdits)
                if (moduleEdit != null)  
                    if (moduleEdit.module.IsValid)
                       return true;

            return false;
        }

        public OrganismDataSheet GetChassis ()
        {
            return targetOrganism;
        }

        // TODO - in the future this should also apply entire modules with inputs and logOps and update the currentModule in each slot.  
        // for now, since those do nothing, it just applies traits/outputs
        public void Apply(TraitManager traitManager)
        {
            foreach (var edit in moduleEdits)
            {
                if (edit != null)
                    switch (edit.modifier)
                    {
                        case ModuleEditModifier.Add:
                            // Only add if trait not present with same properties
                            if (traitManager.HasTrait(edit.module.output, TraitManager.TraitMatchingType.allDataMatchesExactly) == null)
                                traitManager.AddTrait(edit.module.output);
                            else
                            {
                                // TODO: Do we need to do anything player facing if trait was already present?
                                Debug.LogWarning($"Circuit did not add trait [{edit.module.output.name}] as it was already present.");
                            }
                            break;
                        case ModuleEditModifier.Remove:
                            // Only remove if trait already present
                            if (traitManager.HasTrait(edit.module.output, TraitManager.TraitMatchingType.sameClass) != null)
                                traitManager.RemoveTrait(edit.module.output);
                            else
                            {
                                // TODO: Do we need to warn or do anything if trait was not present?
                                Debug.LogWarning($"Circuit did not remove trait [{edit.module.output.name}] as it was not present.");
                            }
                            break;
                        default:
                            Debug.LogError($"Circuit had unsupported modifier type [{edit.modifier}] for trait [{edit.module.output.name}]");
                            break;
                    }
            }
        }
    }
}
