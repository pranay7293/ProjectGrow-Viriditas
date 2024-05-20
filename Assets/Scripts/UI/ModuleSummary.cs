using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ModuleSummary : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private TextMeshProUGUI body;

    public delegate void ModuleSummaryButtonHandler(ModuleSummary moduleSummary);

    public GenomeMap.ModuleSlot myModuleSlot { get; private set; }

    private Color originalBackgroundColor;
    private Color originalTitleColor;
    private Color originalBodyColor;
    private Color invisibleColor;
    private Image myBackground;

    public override string ToString ()
    {
        string toReturn = new string("");

        if (myModuleSlot.isEmpty)
            toReturn = "Empty Slot";
        else
            toReturn = myModuleSlot.ToString();

        return toReturn;
    }

    private void Awake()
    {
        myBackground = GetComponentInChildren<Image>();
        if (myBackground == null)
            Debug.LogError("ModuleSummary object does not have an Image component");

        originalBackgroundColor = myBackground.color;
        originalTitleColor = title.color;
        originalBodyColor = body.color;
        invisibleColor = new Color(0f, 0f, 0f, 0f);
    }


    // this one is used by the Genome Map column display
    // the passed-in ODS is used to determine whether this module's trait has been identified
    public void InitializeModuleSummary(OrganismDataSheet ods, GenomeMap.ModuleSlot slot, ModuleSummaryButtonHandler callback)
    {
        if ((ods == null) || (slot == null) || (callback == null))
        {
            Debug.LogError($"InitializeModuleSummary called with bad data: ods={ods}, slot={slot}, callback={callback}");
            return;
        }

        myModuleSlot = slot;

        // TITLE
        string labelText = new string("");

        if (slot.isEmpty)
            labelText = "Empty Slot";
        else
            labelText = GenomeMapUI.GetTraitName(ods, slot);

        title.text = labelText;


        // BODY
        string bodyText = new string("");

        if (slot.Unremovable)
            bodyText = bodyText + "Permanent" + '\n';

        if (!slot.isEmpty)
        {
            if (!ods.TraitIsRevealed(slot.currentModule.output))
                bodyText = bodyText + "Scan organism to identify" + '\n';
        }
        else
            bodyText = bodyText + "Capacity " + myModuleSlot.capacity.ToString();

        body.text = bodyText;


        // BUTTON
        Button button = GetComponent<Button>();
        if (button == null)
            Debug.LogError("moduleSummaryPrefab doesn't have a Button component");
        else
            button.onClick.AddListener(() => callback(this));                  // assign this trait module summary to the button
    }

    // null ODS is allowed when the module hasn't been edited yet
    public void InitializeModuleSummary_EditMode(OrganismDataSheet ods, GenomeMap.ModuleSlot slot, Module module, ModuleSummaryButtonHandler callback)
    {
        if ((module == null) || (slot == null) || (callback == null))
        {
            Debug.LogError($"InitializeModuleSummary called with bad data: module={module}, slot={slot}, callback={callback}");
            return;
        }

        myModuleSlot = slot;

        if (!module.IsValid)
        {
            title.text = new string("New Module");
            body.text = new string("Design a valid module");
        }
        else
        {
            // TITLE
            title.text = GenomeMapUI.GetTraitName(ods, module.output);


            // BODY
            string bodyText = new string("");

            if (module.inputs != null)
            {
                if (module.inputs[0] != null)
                {
                    bodyText = bodyText + module.inputs[0].name + '\n';
                    if (module.inputs.Length > 1)
                    {
                        if (module.logicalOperator != null)
                            bodyText = bodyText + module.logicalOperator.name + '\n';
                        bodyText = bodyText + module.inputs[1].name + '\n';
                    }
                }
            }


            body.text = bodyText;

            // BUTTON
            Button button = GetComponent<Button>();
            if (button == null)
                Debug.LogError("moduleSummaryPrefab doesn't have a Button component");
            else
                button.onClick.AddListener(() => callback(this));                  // assign this trait module summary to the button

        }
    }


    // this one is used by the Genome Map column display in the Bioreactor which has a circuit but no slots
    // the passed-in ODS is used to determine whether this module's trait has been identified
    public void InitializeModuleSummary_Circuit(OrganismDataSheet ods, Module module)
    {
        if ((ods == null) || (module == null))
        {
            Debug.LogError($"InitializeModuleSummary called with bad data: ods={ods}, module={module}");
            return;
        }

        title.text = GenomeMapUI.GetTraitName(ods, module.output);

        // BODY
        string bodyText = new string("");

        if (module.output.duration == -1)
            bodyText = bodyText + "Stable Integration";
        else
            bodyText = bodyText + "Transient Integration." + '\n' + "Duration: " + module.output.duration;

        body.text = bodyText;
    }


    public void GoInvisible()
    {
        myBackground.color = invisibleColor;
        title.color = invisibleColor;
        body.color = invisibleColor;
    }

    public void GoVisible()
    {
        myBackground.color = originalBackgroundColor;
        title.color = originalTitleColor;
        body.color = originalBodyColor;
    }


}
