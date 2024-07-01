using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using PlayerData;
using Tools;

public class GenomeMapColumnUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scientificNameLabel;
    [SerializeField] private TextMeshProUGUI commonNameLabel;
    [SerializeField] private Image image; // organism image
    [SerializeField] private Image ideogram; // ideogram image

    [SerializeField] private GameObject notSequencedLabel;

    [SerializeField] private Transform moduleSummariesFolder;
    [SerializeField] private ModuleSummary moduleSummaryPrefab;

    private GenomeMap myGenomeMap;

    public delegate void ModuleSelectedHandler(ModuleSummary moduleSummary);
    private ModuleSelectedHandler callbackToParent;

    public void InitializeWithGenomeMap (GenomeMap genomeMap, ModuleSelectedHandler callback)
    {
        myGenomeMap = genomeMap;
        callbackToParent = callback;

        OrganismDataSheet ods = genomeMap.ods;

        // copy over the common set of data
            if (PlayerScannedData.GetScanLevel(ods) > -1)
        {
            scientificNameLabel.text = ods.nameScientific;
            commonNameLabel.text = ods.nameCommon;
            if (ods.image != null)
                image.sprite = ods.image;
            else
                image.sprite = Karyo_GameCore.Instance.uiManager.unknownODSImage;
        }
        else
        {
            // TODO - this is in 2 diff places (see also:  RefreshODSDisplay() in OrganismDataSheetUI) would be good if ODSs could return their name and image depending on scanDepth
            scientificNameLabel.text = new string("Organism not yet scanned");
            commonNameLabel.text = new string("");
            image.sprite = Karyo_GameCore.Instance.uiManager.unknownODSImage;
        }

        // clear out old  module summaries
        List<ModuleSummary> toRemove = new List<ModuleSummary>();
        foreach (Transform child in moduleSummariesFolder)
        {
            ModuleSummary ms = child.GetComponent<ModuleSummary>();
            if (ms != null)
                toRemove.Add(ms);
        }
        foreach (ModuleSummary ms in toRemove)
            GameObject.Destroy(ms.gameObject);


        if (!PlayerSampledData.HasSampled(ods))
        {
            notSequencedLabel.SetActive(true);
            ideogram.gameObject.SetActive(false);
            return;
        }

        notSequencedLabel.SetActive(false);
        ideogram.gameObject.SetActive(true);

        // populate the content folder with one Module Summary for each Module Slot in the genomeMap
        foreach (GenomeMap.ModuleSlot slot in genomeMap.moduleSlots)
        {
            // instantiate a new ModuleSummary 
            GameObject moduleSummaryGO = GameObject.Instantiate(moduleSummaryPrefab.gameObject, moduleSummariesFolder);
            ModuleSummary ms = moduleSummaryGO.GetComponent<ModuleSummary>();
            if (ms == null)
                Debug.LogError("moduleSummaryPrefab doesn't have a ModuleSummary component");
            else
                ms.InitializeModuleSummary(ods, slot, ModuleSummaryClicked);
        }

    }

    public void ModuleSummaryClicked(ModuleSummary moduleSummary)
    {
        callbackToParent.Invoke(moduleSummary);
    }

    public int GetIndexOfModuleSummary (ModuleSummary moduleSummary)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            ModuleSummary toTest = moduleSummariesFolder.GetChild(i).GetComponent<ModuleSummary>();
            if (moduleSummary == toTest)
                return i;
        }

        return -1;
    }


    // TODO - collapse this with the other Initialize method
    public void InitializeWithCircuit(Circuit circuit)
    {
        myGenomeMap = null;
        callbackToParent = null;

        OrganismDataSheet chassis = circuit.GetChassis();

        // copy over the common set of data
        if (PlayerScannedData.GetScanLevel(chassis) > -1)
        {
            scientificNameLabel.text = chassis.nameScientific;
            commonNameLabel.text = chassis.nameCommon;
            if (chassis.image != null)
                image.sprite = chassis.image;
            else
                image.sprite = Karyo_GameCore.Instance.uiManager.unknownODSImage;
        }
        else
        {
            // TODO - this is in 2 diff places (see also:  RefreshODSDisplay() in OrganismDataSheetUI) would be good if ODSs could return their name and image depending on scanDepth
            scientificNameLabel.text = new string("Organism not yet scanned");
            commonNameLabel.text = new string("");
            image.sprite = Karyo_GameCore.Instance.uiManager.unknownODSImage;
        }

        // clear out old  module summaries
        List<ModuleSummary> toRemove = new List<ModuleSummary>();
        foreach (Transform child in moduleSummariesFolder)
        {
            ModuleSummary ms = child.GetComponent<ModuleSummary>();
            if (ms != null)
                toRemove.Add(ms);
        }
        foreach (ModuleSummary ms in toRemove)
            GameObject.Destroy(ms.gameObject);


        notSequencedLabel.SetActive(false);
        ideogram.gameObject.SetActive(true);

        // populate the content folder with one Module Summary for each edit in the circuit
        foreach (Circuit.ModuleEdit edit in circuit.moduleEdits)
        {
            if (edit != null)
            {
                // instantiate a new ModuleSummary 
                GameObject moduleSummaryGO = GameObject.Instantiate(moduleSummaryPrefab.gameObject, moduleSummariesFolder);
                ModuleSummary ms = moduleSummaryGO.GetComponent<ModuleSummary>();
                if (ms == null)
                    Debug.LogError("moduleSummaryPrefab doesn't have a ModuleSummary component");
                else
                    ms.InitializeModuleSummary_Circuit(edit.sourceOrganism, edit.module);
            }   
        }

    }


}
