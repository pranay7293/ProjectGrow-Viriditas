using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using PlayerData;

public class GenomeMapUI : MonoBehaviour
{

    [SerializeField] private GenomeMapColumnUI genomeMapColumn;
    [SerializeField] private Button selectOrganismButton;
    [SerializeField] private Button selectOutputButton;

    [SerializeField] private ModuleDisplayUI moduleDisplay;

    private GenomeMap selectedGenomeMap;
    private TreeOfLifeUI.TreeOfLifeUIMode currentMode;
    private ModuleSummary currentModuleSummarySelected;

    public delegate void SelectOutputCallback(Trait output, OrganismDataSheet sourceOrganism);
    public SelectOutputCallback currentOutputCallback;

    public void DisplayGenomeMap(GenomeMap genomeMap, TreeOfLifeUI.TreeOfLifeUIMode mode)
    {
        if (mode == TreeOfLifeUI.TreeOfLifeUIMode.SelectOutput)
            Debug.LogError("DisplayGenomeMap called in mode SelectOutput but without a SelectOutputCallback passed in.");

        DisplayGenomeMap(genomeMap, mode, null);
    }
    public void DisplayGenomeMap(GenomeMap genomeMap, TreeOfLifeUI.TreeOfLifeUIMode mode, SelectOutputCallback callback)
    {
        if (genomeMap == null)
        {
            Debug.LogError("DisplayGenomeMap called with null genomeMap");
            return;
        }

        selectedGenomeMap = genomeMap;
        currentMode = mode;
        currentOutputCallback = callback;

        if ((mode == TreeOfLifeUI.TreeOfLifeUIMode.ViewOnlyGenomeMap) || (mode == TreeOfLifeUI.TreeOfLifeUIMode.ViewOnlyODS))
        {
            selectOrganismButton.gameObject.SetActive(false);
            selectOutputButton.gameObject.SetActive(false);
        }
        else if (mode == TreeOfLifeUI.TreeOfLifeUIMode.SelectOrganism)
        {
            selectOutputButton.gameObject.SetActive(false);

            if (PlayerSampledData.HasSampled(selectedGenomeMap.ods))
                selectOrganismButton.gameObject.SetActive(true);
            else
                selectOrganismButton.gameObject.SetActive(false);

        }
        else if (mode == TreeOfLifeUI.TreeOfLifeUIMode.SelectOutput)
        {
            selectOrganismButton.gameObject.SetActive(false);
            selectOutputButton.gameObject.SetActive(false);  // this gets activated in ModuleSummarySelected() below
        }

        // turn off any active module display
        moduleDisplay.gameObject.SetActive(false);

        genomeMapColumn.InitializeWithGenomeMap(genomeMap, ModuleSummarySelected);
    }

    // called when the player clicks the selectChassisButton
    public void ChassisSelected()
    {
        Karyo_GameCore.Instance.uiManager.BiofoundryChassisSelected(selectedGenomeMap.ods);
    }

    // called by the Genome Map Column in Tree of Life when the player clicks on one of the module summaries
    public void ModuleSummarySelected(ModuleSummary moduleSummary)
    {
        if (moduleSummary == null)
        {
            Debug.LogError("ModuleSummarySelected called with null moduleSummary");
            return;
        }

        currentModuleSummarySelected = moduleSummary;

        moduleDisplay.AssignModule(moduleSummary.myModuleSlot, selectedGenomeMap.ods);
        moduleDisplay.gameObject.SetActive(true);

        if (currentMode == TreeOfLifeUI.TreeOfLifeUIMode.SelectOutput)
        {
            if (moduleSummary.myModuleSlot.isEmpty)
                selectOutputButton.gameObject.SetActive(false);
            else
                selectOutputButton.gameObject.SetActive(true);
        }
    }

    // called when the player clicks the selectOutputButton
    public void OutputSelected()
    {
        if (currentMode != TreeOfLifeUI.TreeOfLifeUIMode.SelectOutput)
            Debug.LogError("OutputSelected called when GenomeMapUI not in mode SelectOutput");

        currentOutputCallback.Invoke(currentModuleSummarySelected.myModuleSlot.currentModule.output, selectedGenomeMap.ods);
    }


    // returns the name of the currentTrait if this Trait has been scanned, or else its obfuscatedName
    public static string GetTraitName(OrganismDataSheet ods, GenomeMap.ModuleSlot moduleSlot)
    {
        if (ods == null)
        {
            Debug.LogError("GetTraitName called with null ods");
            return new string("");
        }
        if (moduleSlot == null)
        {
            Debug.LogError("GetTraitName called with null moduleSlot");
            return new string("");
        }

        // TODO - actually implement obfuscated name
        string toReturn = new string("Unidentified");

        if (ods.TraitIsRevealed(moduleSlot.currentModule.output))
            return moduleSlot.currentModule.output.name;
        
        return toReturn;
    }

    // the version that takes a Trait instead of a ModuleSlot 
    public static string GetTraitName(OrganismDataSheet ods, Trait trait)
    {
        if (ods == null)
        {
            Debug.LogError("GetTraitName called with null ods");
            return new string("");
        }
        if (trait == null)
        {
            Debug.LogError("GetTraitName called with null trait");
            return new string("");
        }

        // TODO - actually implement obfuscated name
        string toReturn = new string("Unknown Trait");

        if (ods.TraitIsRevealed(trait, PlayerData.PlayerScannedData.GetScanLevel(ods)))
            return trait.name;

        return toReturn;
    }



}
