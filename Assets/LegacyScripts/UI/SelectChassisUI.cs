using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerData;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class SelectChassisUI : MonoBehaviour
{
    public GameObject noGenomeMapsYetLabel;
    public Transform contentFolder;
    public Button entitySelectionButtonPrefab;

    private BiofoundryUI toCallBackTo;


    public void RefreshSelectChassisData(BiofoundryUI callback)
    {
        toCallBackTo = callback;

        List<OrganismDataSheet> sampledGenomes = PlayerSampledData.SampledDataSheets.ToList();

        if (sampledGenomes.Count == 0)
            noGenomeMapsYetLabel.gameObject.SetActive(true);
        else
            noGenomeMapsYetLabel.gameObject.SetActive(false);

        // clear all existing buttons out of the contentFolder
        foreach (Transform child in contentFolder)
            if (child.GetComponent<Button>() != null)
                GameObject.Destroy(child.gameObject);

        // add a new button for each sampledGenome
        foreach (OrganismDataSheet ods in sampledGenomes)
        {
            string buttonLabel = new string("");
            buttonLabel = ods.nameScientific + " (" + ods.nameCommon + ")";

            // instantiate a new entitySelectionButton and give it that label
            GameObject buttonGO = GameObject.Instantiate(entitySelectionButtonPrefab.gameObject, contentFolder);
            TextMeshProUGUI label = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = buttonLabel;
            else
                Debug.LogError("entitySelectionButtonPrefab doesn't have a child with a TextMeshProUGUI component (ie - a text label)");

            Button button = buttonGO.GetComponent<Button>();
            if (button == null)
                Debug.LogError("entitySelectionButtonPrefab doesn't have a Button component");
            else
                // assign this ods to the button
                button.onClick.AddListener(() => EntityTypeButtonClicked(ods));
        }
    }

    public void EntityTypeButtonClicked(OrganismDataSheet ods)
    {
        toCallBackTo.GenomeMapSelected(ods);
    }

}
