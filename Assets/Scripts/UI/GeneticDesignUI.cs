using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Tools;

public class GeneticDesignUI : MonoBehaviour
{

    [SerializeField] private float traitDuration = 30f;  // TODO - this is temp, curretly all manufactured circuits are transient stability
    [SerializeField] private GenomeMapColumnUI genomeMapColumn;

    // note that these next 3 are parallel arrays that also correspond to the module summaries in the genome map column.
    // ie - the first module summary in the genome map column is horizontally aligned with arrows[0] and moduleSummaries[0] and 
    // corresponds to moduleEdits[0].
    [SerializeField] private Image[] arrows;
    [SerializeField] private ModuleSummary[] moduleSummaries; // the circuit's ModuleEdits are displayed in these
    private Circuit currentCircuit; // the circuit we are currently building.  this includes an array of ModuleEdits the same size as the above parallel arrays (ie - the number of moduleSlots in this genome map)
    private int currentlyEditingModuleIndex;  // an index into currentCircuit's array of ModuleEdits
    private Module currentlyEditingModule
    {
        get { return currentCircuit.moduleEdits[currentlyEditingModuleIndex]?.module; }
        set { currentCircuit.moduleEdits[currentlyEditingModuleIndex].module = value; }
    }
    private GenomeMap.ModuleSlot currentlyEditingModuleSlot
    {
        get { return chassis.genomeMap.moduleSlots[currentlyEditingModuleIndex]; }
    }
    private Circuit.ModuleEdit currentModuleEdit => currentCircuit.moduleEdits[currentlyEditingModuleIndex];
    private ModuleSummary currentlyEditingModuleSummary => moduleSummaries[currentlyEditingModuleIndex];

    [SerializeField] private Button manufactureButton;
    [SerializeField] private GameObject complexityProgressGO;
    [SerializeField] private Image complexityProgressImage;
    [SerializeField] private ModuleDisplayUI moduleDisplay;
    [SerializeField] private GameObject capacityProgressGO;
    [SerializeField] private Image capacityProgressImage;
    [SerializeField] private GameObject fitnessProgressGO;
    [SerializeField] private Image fitnessProgressImage;
    [SerializeField] private GameObject moduleRepoArea;

    [SerializeField] private GameObject titleBarGO;
    [SerializeField] private TextMeshProUGUI titleBarText;

    private Transform folderForModulesCreatedAtRuntime;  // TODO - this is so terrible, would be fixed if we migrate Modules to ScriptableObjects, see Module.cs


    private OrganismDataSheet chassis;
    private int numSlots;  // how many moduleSlots this chassis has in its genome map

    private static float screenPosition_Center = 0f;
    private static float screenPosition_Left = -450f;


    private Color originalArrowColor; // TODO - this isn't intended as the final solution
    private Color invisibleArrowColor; // TODO - this isn't intended as the final solution


    private void Awake()
    {
        originalArrowColor = arrows[0].color;
        invisibleArrowColor = new Color(originalArrowColor.r, originalArrowColor.g, originalArrowColor.b, 0f);

        GameObject folder = new GameObject();
        folder.name = "ModuleGOFolder";
        folderForModulesCreatedAtRuntime = folder.transform;

        // Manufacture circuit click handler.
        manufactureButton.onClick.AddListener(() =>
        {
            Karyo_GameCore.Instance.uiManager.CloseOpenWindows();
            Karyo_GameCore.Instance.uiManager.DisplayBioreactorWindow(currentCircuit);
        });
    }

    // TODO - pass in a mode for if we're designing a circuit, a microorganism factory, or a bioblock (actually bioblock is probably a whole diff interface?)
    public void InitializeGeneticDesignUI(OrganismDataSheet chassisToEdit)
    {
        chassis = chassisToEdit;

        currentlyEditingModuleIndex = -1;  // no module is currently being edited

        // since we're just opening now, put this window in the center of the screen
        RectTransform rectTransform = GetComponent<RectTransform>();
        Vector2 newPosition = new Vector2(screenPosition_Center, rectTransform.anchoredPosition.y);
        rectTransform.anchoredPosition = newPosition;

        genomeMapColumn.InitializeWithGenomeMap(chassis.genomeMap, ChassisModuleSummarySelected);

        numSlots = chassisToEdit.genomeMap.moduleSlots.Length;
        currentCircuit = new Circuit(chassis);  // creates a new circuit where all edits are null

        UpdateAppearance();
    }

    // this one is called when one of the module summaries in the chassis is clicked on
    public void ChassisModuleSummarySelected(ModuleSummary moduleSummary)
    {
        // TODO - for now we're only going to support clicking on an empty slot

        if (!moduleSummary.myModuleSlot.isEmpty)
        {
            Debug.Log("We currently only support editing empty slots!");
            return;
        }

        // find the index corresponding to the module summary that was clicked
        currentlyEditingModuleIndex = genomeMapColumn.GetIndexOfModuleSummary(moduleSummary);

        // if there is not already an edit in this slot, create a new one
        if (currentlyEditingModule == null)
        {
            Debug.Log($"Creating a new module edit for slot {currentlyEditingModuleIndex}");
            currentCircuit.CreateEdit(currentlyEditingModuleIndex, folderForModulesCreatedAtRuntime);
        }

        Debug.Log($"Displaying in-progress edit in slot {currentlyEditingModuleIndex}");
        UpdateAppearance();

    }

    // this one is called when one of the module summaries in the circuit column is clicked on
    public void CircuitModuleSummarySelected(int index)
    {
        currentlyEditingModuleIndex = index;
        Debug.Log($"Displaying in-progress edit in slot {currentlyEditingModuleIndex}");
        UpdateAppearance();
    }

    public void CircuitModuleSummarySelected_ModuleSummary(ModuleSummary moduleSummary)
    {
        for (int i = 0; i < moduleSummaries.Length; i++)
            if (moduleSummaries[i] == moduleSummary)
                CircuitModuleSummarySelected(i);

        Debug.LogError($"Couldn't find moduleSummary {moduleSummary}");
    }

    public void EmptyInputClickedOn()
    {
        Debug.Log("An empty input was clicked on!");

        // move us out of center and to the left position, then open the Tree of Life to select an Input
        RectTransform rectTransform = GetComponent<RectTransform>();
        Vector2 newPosition = new Vector2(screenPosition_Left, rectTransform.anchoredPosition.y);
        rectTransform.anchoredPosition = newPosition;

        Karyo_GameCore.Instance.uiManager.DisplayTreeofLifeWindow(null, TreeOfLifeUI.TreeOfLifeUIMode.SelectOutput, OutputSelected);

    }

    public void OutputSelected(Trait output, OrganismDataSheet sourceOrganism)
    {
        Debug.Log($"Genetic Design UI reports that an Output was selected {output}");

        currentlyEditingModule.AddOutput(output);
        currentModuleEdit.module.output.duration = traitDuration;  
        currentModuleEdit.sourceOrganism = sourceOrganism;
        moduleDisplay.AssignModule(currentlyEditingModuleSlot, currentlyEditingModule, currentModuleEdit.sourceOrganism, false, null);  // don't pass in chassis, pass in the ODS this module came from (so that it can be displayed as identified or not)

        // move us out of the left and to the center position, close Tree of Life
        RectTransform rectTransform = GetComponent<RectTransform>();
        Vector2 newPosition = new Vector2(screenPosition_Center, rectTransform.anchoredPosition.y);
        rectTransform.anchoredPosition = newPosition;
        Karyo_GameCore.Instance.uiManager.CloseTreeofLifeWindow();
        UpdateAppearance();
    }


    // this one is called when a RepoInputButton is clicked on 
    public void InputSelected(ModuleInput input)
    {
        Debug.Log($"Input selected: {input.name}");

        // you can't add an input if you already have 2 inputs
        // TODO - provide feedback
        if (currentlyEditingModule.inputs == null)
        {
            currentlyEditingModule.AddInput(input);
            UpdateAppearance();
        }
        else if (currentlyEditingModule.inputs.Length < 2)
        {
            currentlyEditingModule.AddInput(input);
            UpdateAppearance();
        }
    }

    // this one is called when a RepoLogOpButton is clicked on 
    public void LogOpSelected(ModuleLogicOperator logOp)
    {
        Debug.Log($"Logical Operator selected: {logOp.ToString()}");

        // you can't add a logop unless you have 2 inputs
        // TODO - provide feedback
        if (currentlyEditingModule.inputs != null)
            if (currentlyEditingModule.inputs.Length >= 2)
            {
                currentlyEditingModule.AddOperator(logOp);
                UpdateAppearance();
            }
    }

    // this is a messy way to handle turning things on and off etc for the iGEM demo
    // since I'm pulling some questionable stunts i wanted to compartmentalize it
    private void UpdateAppearance()
    {
        // in order to get the arrows and module summaries in the circuit column to line horizontally up with the genome map column, we make sure
        // the same number of them are enabled as the number of slots in the chassis's genome map

        // first we disable all of them, then we enable the first numSlots of them
        foreach (Image arrow in arrows)
            arrow.gameObject.SetActive(false);
        for (int i = 0; i < numSlots; i++)
            arrows[i].gameObject.SetActive(true);

        foreach (ModuleSummary moduleSummary in moduleSummaries)
            moduleSummary.gameObject.SetActive(false);
        for (int i = 0; i < numSlots; i++)
            moduleSummaries[i].gameObject.SetActive(true);

        // however, we don't want to see any arrows and module summaries unless the corresponding module slot in the genome map has been edited
        // to hide them, we set their alpha to 0 (we can't disable them or that will rearrange them vertically)
        // TODO ? - and if necessary also disable their Button

        for (int i = 0; i < numSlots; i++)
        {
            if (currentCircuit.moduleEdits[i] == null)
            {
                arrows[i].color = invisibleArrowColor;
                moduleSummaries[i].GoInvisible();
            }
            else
            {
                arrows[i].color = originalArrowColor;
                moduleSummaries[i].GoVisible();
            }
        }

        if (currentCircuit.IsValid())
            manufactureButton.gameObject.SetActive(true);
        else
            manufactureButton.gameObject.SetActive(false);

        // for each of the module summaries of the circuit, if that slot has an active edit, set the module summary to the appropriate data
        for (int i=0; i<numSlots; i++)
        {
            if (currentCircuit.moduleEdits[i] != null)
            {
                ModuleSummary moduleSummary = moduleSummaries[i];
                OrganismDataSheet sourceOrganism = currentCircuit.moduleEdits[i].sourceOrganism;
                Module inProgModule = currentCircuit.moduleEdits[i].module;
                GenomeMap.ModuleSlot moduleSlot = chassis.genomeMap.moduleSlots[i];

                moduleSummary.InitializeModuleSummary_EditMode(sourceOrganism, moduleSlot, inProgModule, CircuitModuleSummarySelected_ModuleSummary);
            }
        }

        // if there is a module currently selected for editing, show the whole right hand side, otherwise don't.
        if (currentlyEditingModuleIndex == -1)
        {
            complexityProgressGO.SetActive(false);
            moduleDisplay.gameObject.SetActive(false);
            capacityProgressGO.SetActive(false);
            fitnessProgressGO.SetActive(false);
            moduleRepoArea.SetActive(false);
            fitnessProgressGO.SetActive(false);
            capacityProgressGO.SetActive(false);
        }
        else
        {
            complexityProgressGO.SetActive(true);
            moduleDisplay.gameObject.SetActive(true);
            capacityProgressGO.SetActive(true);
            fitnessProgressGO.SetActive(true);
            moduleRepoArea.SetActive(true);

            moduleDisplay.AssignModule(currentlyEditingModuleSlot, currentlyEditingModule, currentModuleEdit.sourceOrganism, true, EmptyInputClickedOn);

            // fitness and capacity
            // TODO - ugh, fitness should be a rating for the entire circuit, not one module
            float fitness_max = 100f; // get this from the chassis
            float fitness_value = (fitness_max + currentlyEditingModule.totalFitnessUsed) / fitness_max;
            fitness_value = Mathf.Clamp(fitness_value, 0f, 1f);
            fitnessProgressImage.fillAmount = fitness_value;
            fitnessProgressGO.SetActive(true);

            float capacity_max = currentlyEditingModuleSlot.capacity;
            float capacity_value = (capacity_max + currentlyEditingModule.totalCapacityUsed) / capacity_max;
            capacity_value = Mathf.Clamp(capacity_value, 0f, 1f);
            capacityProgressImage.fillAmount = capacity_value;
            capacityProgressGO.SetActive(true);
        }

        // update the complexity display
        float complexity_max = 50f;  // TODO - scrape this from tool data for the biofoundry
        float complexity_value = (complexity_max - currentCircuit.complexity) / complexity_max;
        complexity_value = Mathf.Clamp(complexity_value, 0f, 1f);
        Debug.Log($"Complexity is {currentCircuit.complexity} out of {complexity_max}. Value to display {complexity_value}");
        complexityProgressImage.fillAmount = complexity_value;
        complexityProgressGO.SetActive(true);

        // i guess the title bar is just always on and displaying this text for now
        titleBarGO.SetActive(true);
        titleBarText.text = "Select A Module To Edit";

    }

}
