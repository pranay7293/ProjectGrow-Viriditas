using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// when given a Module, it will display it correctly in the ModuleDisplay prefab

public class ModuleDisplayUI : MonoBehaviour
{
    private OrganismDataSheet myODS;  // this is the ODS used to check if this module is identified or not
    private GenomeMap.ModuleSlot myModuleSlot;
    private Module myModule;

    [SerializeField] private GameObject inputsGroup;
    [SerializeField] private GameObject input1;
    [SerializeField] private GameObject input2;
    [SerializeField] private GameObject logicalOperator;
    [SerializeField] private GameObject noLogicalOperator;
    [SerializeField] private GameObject output;
    [SerializeField] private TextMeshProUGUI description;

    private bool editMode;

    public delegate void InputButtonPressed();
    private InputButtonPressed callback;

    // shortcut if you're not in edit mode.  it uses the currentModule in the passed-in slot
    public void AssignModule (GenomeMap.ModuleSlot slot, OrganismDataSheet ods)
    {
        AssignModule(slot, slot.currentModule, ods, false, null);
    }
    public void AssignModule(GenomeMap.ModuleSlot slot, Module module, OrganismDataSheet ods, bool editingMode, InputButtonPressed callbackToAssign)
    {
        myODS = ods;
        myModuleSlot = slot;
        myModule = module;
        editMode = editingMode;
        callback = callbackToAssign; 
        UpdateAppearance();
    }

    public void UpdateAppearance()
    {
        if (myModuleSlot == null)
        {
            Debug.LogError("ModuleDisplayUI has a null myModuleSlot");
            return;
        }

        if (!editMode)
        {
            EventTrigger eventTrigger = output.GetComponent<EventTrigger>();
            // clear out any PointerClick event triggers that might be on the component
            eventTrigger.triggers.RemoveAll(trigger => trigger.eventID == EventTriggerType.PointerClick);
            eventTrigger.enabled = false;
        }

        // unrevealed trait
        if ((myODS != null) && (myModule != null))
            if (!myODS.TraitIsRevealed(myModule.output))
            {
                ChangeText(output, "Unidentified");
                inputsGroup.SetActive(false);
                logicalOperator.SetActive(false);
                noLogicalOperator.SetActive(false);
                output.SetActive(true);
                string desc = new string("");
                desc = desc + "Scan organism to identify";
                description.text = desc;
                description.gameObject.SetActive(true);
                return;
            }

        if (myModule == null)
        {
            inputsGroup.SetActive(false);
            input1.SetActive(false);
            input2.SetActive(false);
            logicalOperator.SetActive(false);
            noLogicalOperator.SetActive(false);
        }
        else 
        {
            int numInputs;
            if (myModule.inputs == null)
                numInputs = 0;
            else
                numInputs = myModule.inputs.Length;

            if (numInputs > 2)
                Debug.LogWarning($"Module {myModule} has more than 2 Inputs, which is not currently supported. Ignoring 3rd and beyond Inputs");

            if ((numInputs > 0) && (myModule.inputs[0] != null))
            {
                ChangeText(input1, myModule.inputs[0].name);
                input1.SetActive(true);
            }
            else
                input1.SetActive(false);

            if ((numInputs > 1) && (myModule.inputs[1] != null))
            {
                ChangeText(input2, myModule.inputs[1].name);
                input2.SetActive(true);
            }
            else
                input2.SetActive(false);

            if (numInputs == 0)
                inputsGroup.SetActive(false);
            else
                inputsGroup.SetActive(true);

            if (myModule.logicalOperator != null)
            {
                ChangeText(logicalOperator, myModule.logicalOperator.name);
                ChangeImage(logicalOperator, myModule.logicalOperator.image);
                logicalOperator.SetActive(true);
                noLogicalOperator.SetActive(false);
            }
            else
            {
                if (numInputs >= 1)
                {
                    logicalOperator.SetActive(false);
                    noLogicalOperator.SetActive(true);
                }
                else  // there are no inputs
                {
                    logicalOperator.SetActive(false);
                    noLogicalOperator.SetActive(false);
                }
            }
        }

        // empty module
        if ((myModuleSlot.isEmpty) && (myModule?.output == null))
        {
            if (!editMode)
            {
                ChangeText(output, "Empty");
            }
            else
            {
                ChangeText(output, "Click To Select Output");
                EventTrigger eventTrigger = output.GetComponent<EventTrigger>();
                eventTrigger.enabled = true;
                // assign a PointerClick event such thath the callback is called when the button is clicked on
                EventTrigger.Entry entry = new EventTrigger.Entry();
                entry.eventID = EventTriggerType.PointerClick;
                entry.callback.AddListener((data) => { callback.Invoke(); });
                eventTrigger.triggers.Add(entry);
            }
        }
        else
            ChangeText(output, myModule.output.name);

        output.SetActive(true);

        // update text description area
        string descriptionText = new string("");

        if (myModule != null)
        {
            if (myModule.output != null)
            {
                descriptionText = descriptionText + myModule.output.description + '\n';

                if (myModule.output.strength != 0f)
                    descriptionText = descriptionText + "Strength: " + myModule.output.strength + '\n';

                if (myModule.output.duration == -1f)
                    descriptionText = descriptionText + "Edit Stability:  Stable" + '\n';
                else
                {
                    descriptionText = descriptionText + "Edit Stability:  Transient" + '\n';
                    descriptionText = descriptionText + "Duration: " + myModule.output.duration + "seconds" + '\n';
                }
            }

            /*
            // these are now displayed in progress wheels, so they don't need to be in the module display
            descriptionText = descriptionText + "Total capacity used: " + myModule.totalCapacityUsed + '\n';
            descriptionText = descriptionText + "Complexity: " + myModule.totalComplexity + '\n';
            descriptionText = descriptionText + "Fitness drain: " + myModule.totalFitnessUsed + '\n';
            */
        }

        description.text = descriptionText;
    }


    private void ChangeText (GameObject go, string text)
    {
        TextMeshProUGUI textElement = go.GetComponentInChildren<TextMeshProUGUI>();

        if (textElement != null)
            textElement.text = text;
        else
            Debug.LogError($"ModuleDisplay element {go.name} does not have a child with a TextMeshProUGUI component.");
    }

    private void ChangeImage(GameObject go, Sprite image)
    {
        Image imageElement = go.GetComponentInChildren<Image>();

        if (imageElement != null)
            imageElement.sprite = image;
        else
            Debug.LogError($"ModuleDisplay element {go.name} does not have a child with an Image component.");
    }



}
