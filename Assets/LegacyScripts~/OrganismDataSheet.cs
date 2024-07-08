using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// TODO - should be a type of scriptable object

// contains all of the player-facing data about a single species of organism.
// includes the ordering that data onlocks per-scan (and therefore the amount of scans required to unlock all the data)

public class OrganismDataSheet : MonoBehaviour
{
    public string nameScientific;
    public string nameCommon;
    public string organismClass;  // TODO - some day this would be an enum instead, most likely
    public Sprite image;
    [SerializeField]
    public ODS_Entry[] dataEntries;    // these should be listed in player-facing display order

    public GenomeMap genomeMap;  // the genomeMap associated with this entity type/species

    [SerializeField] private float sampleTime = 5;
    public float SampleTime => sampleTime;
    [SerializeField] private float injectionTime = 5;
    public float InjectionTime => injectionTime;

    [Serializable]
    public class ODS_Entry
    {
        // exactly 1 of these should be valid / have data at a time
        public string textVersion;
        public Trait trait;
        public Behavior behavior;

        public string GetText()
        {
            if (!string.IsNullOrEmpty(textVersion))
                return textVersion;

            if (trait != null)
                return trait.ODS_text;

            if (behavior != null)
                return behavior.ODS_text;

            Debug.LogError($"ODS_Entry {this} has no valid data assigned.");
            return null;
        }
    }

    [Serializable]
    public class EntriesIndices
    {
        public int[] indices;
    }

    [SerializeField]
    public EntriesIndices[] scanUnlockOrder;

    public int maxScanDepth => scanUnlockOrder.Length-1;

    private void Awake()
    {
        if (genomeMap == null)
            genomeMap = GetComponent<GenomeMap>();
        // TODO - warn if it can't find one
    }

    // can the player see this Trait given the scanDepth?
    public bool TraitIsRevealed(Trait trait)
    {
        return TraitIsRevealed(trait, PlayerData.PlayerScannedData.GetScanLevel(this));
    }

    // can the player see this Trait given the scanDepth?
    public bool TraitIsRevealed(Trait trait, int scanDepth)
    {
        if ((scanDepth < -1) || (scanDepth > maxScanDepth))
        {
            Debug.LogError($"TraitIsRevealed called with invalid scanDepth of {scanDepth}.");
            return false;
        }

        // -1 means it hasn't been scanned
        if (scanDepth == -1)
            return false;

        for (int depth = 0; depth <= scanDepth; depth++)
            for (int i = 0; i < scanUnlockOrder[depth].indices.Length; i++)
            {
                int entry = scanUnlockOrder[depth].indices[i];

                if ((entry < 0) || (entry >= dataEntries.Length))
                    Debug.LogError($"Organism Data Sheet has bad ScanUnlockOrder data.  Invalid entry = {entry}");
                else
                {
                    ODS_Entry entryToTest = dataEntries[entry];
                    Trait traitToTest = entryToTest.trait;
                    if (traitToTest != null)
                        if (TraitManager.DoTraitsMatch(traitToTest, trait, TraitManager.TraitMatchingType.sameClass))
                            return true;
                }
            }

        return false;
    }




    // this returns every ODS_Entry string that is visible to player for the given scanDepth
    // must be checked for null list, null strings, or empty strings
    public List<string> GetDataEntriesForScanDepth (int scanDepth)
    {
        if ((scanDepth < 0) || (scanDepth > maxScanDepth))
        {
            Debug.LogError($"GetDataEntriesForScanDepth called with invalid scanDepth of {scanDepth}");
            return null;
        }

        List<string> toReturn = new List<string>();

        for (int depth = 0; depth <= scanDepth; depth++)
            for (int i = 0; i < scanUnlockOrder[depth].indices.Length; i++)
            {
                int entry = scanUnlockOrder[depth].indices[i];

                if ((entry < 0) || (entry >= dataEntries.Length))
                    Debug.LogError($"Organism Data Sheet {this} has bad ScanUnlockOrder data.  Invalid entry = {entry}");
                else
                    toReturn.Add(dataEntries[entry].GetText());
            }

        return toReturn;
    }


}
