using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using PlayerData;

public class OrganismDataSheetUI : MonoBehaviour
{
    [SerializeField] private Button selectChassisButton;
    [SerializeField] private TextMeshProUGUI scientificNameLabel;
    [SerializeField] private TextMeshProUGUI commonNameLabel;
    [SerializeField] private TextMeshProUGUI classLabel;
    [SerializeField] private TextMeshProUGUI traitDescriptions;
    [SerializeField] private Image image;
    [SerializeField] private TextMeshProUGUI percentScanned;

    private OrganismDataSheet currentOds;


    // when this window is active, call this method with an OrganismDataSheet and a ScanDepth to fill the window with that data
    // also with a mode so we know whether the select button should be active
    public void DisplayODS(OrganismDataSheet ods, int scanDepth, TreeOfLifeUI.TreeOfLifeUIMode mode)
    {
        if (ods == null)
        {
            Debug.LogError("DisplayODS called with null ODS");
            return;
        }
        else
            currentOds = ods;

        if ((scanDepth < -1) || (scanDepth > ods.maxScanDepth))
        {
            Debug.LogError($"DisplayODS called with invalid scanDepth of {scanDepth}, setting to the closest valid value.");
            if (scanDepth < -1)
                scanDepth = -1;
            if (scanDepth > ods.maxScanDepth)
                scanDepth = ods.maxScanDepth;
        }

        if (mode == TreeOfLifeUI.TreeOfLifeUIMode.SelectOrganism)
            selectChassisButton.gameObject.SetActive(true);
        else
            selectChassisButton.gameObject.SetActive(false);

        RefreshODSDisplay(currentOds, scanDepth);
    }

    // called when the player clicks the selectChassisButton
    public void ChassisSelected ()
    {
        Karyo_GameCore.Instance.uiManager.BiofoundryChassisSelected(currentOds);
    }


    private void RefreshODSDisplay(OrganismDataSheet ods, int scanDepth)
    {
        if (scanDepth == -1)
        {
            scientificNameLabel.text = new string("Organism not yet scanned");
            commonNameLabel.text = new string("");
            classLabel.text = new string("Class: unknown");
            image.sprite = Karyo_GameCore.Instance.uiManager.unknownODSImage;
            percentScanned.text = new string("Data collected: 0%");
            traitDescriptions.text = new string("Scan organism to collect data.");
            return;
        }

        scientificNameLabel.text = ods.nameScientific;
        commonNameLabel.text = ods.nameCommon;

        string classLabelText = new string("Class: ");
        classLabelText = classLabelText + ods.organismClass;
        classLabel.text = classLabelText;

        if (ods.image != null)
            image.sprite = ods.image;
        else
            image.sprite = Karyo_GameCore.Instance.uiManager.unknownODSImage;

        string scannedLabelText = new string("Data Collected:   <b>");
        float percent = (float)((float)(scanDepth + 1) / ((float)ods.maxScanDepth + 1)) * 100f;
        scannedLabelText = scannedLabelText + percent.ToString("F1") + "%</b>";
        percentScanned.text = scannedLabelText;

        // build the trait descriptions string
        string traitDescriptionsText = new string("");
        List<string> traitDescriptionsList = ods.GetDataEntriesForScanDepth(scanDepth);
        if (traitDescriptionsList != null)
        {
            foreach (string s in traitDescriptionsList)
                if (s != null)  // TODO - could also check for empty strings if desired
                    traitDescriptionsText = traitDescriptionsText + s + '\n';
        }
        // TODO - remove the trailing \n ?

        traitDescriptions.text = traitDescriptionsText;

    }


}
