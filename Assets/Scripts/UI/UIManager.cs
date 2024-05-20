using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UI;
using Tools;


public class UIManager : MonoBehaviour
{
    private Karyo_GameCore core;

    [SerializeField] private ReticleHandler reticle;
    [SerializeField] private TMP_Text currentToolText;
    [SerializeField] private float showToolTime = 1f;

    public ReticleHandler ReticleHandler => reticle;

    public GameObject traitEditingWindow;
    public TextMeshProUGUI targetTypeDisplay;

    public GameObject resetMenu;
    public TreeOfLifeUI treeOfLifeWindow;

    [Header("References")]
    [SerializeField] private GameObject biofoundryMainMenu;
    [SerializeField] public GeneticDesignUI geneticDesignWindow;
    [SerializeField] public BioreactorUI bioreactorWindow;
    [SerializeField] private DEBUG_TraitEditor traitEditor;
    [SerializeField] private IdlePlayerDialogWindow idlePlayerDialogWindow;
    [SerializeField] private GameObject genericDialogWindow;  // used for pausing game on parse errors and any other generic message usage
    [SerializeField] private TextMeshProUGUI genericDialogTitle;
    [SerializeField] private TextMeshProUGUI genericDialogBody;
    [SerializeField] private GameObject dialogInputWindow;
    [SerializeField] private TextMeshProUGUI dialogWindowHeader; // the header of the dialog window for player text entry
    [SerializeField] private TMP_InputField dialogWindowInputField; // the input field in the dialog window for player text entry
    [SerializeField] private Button dialogOption1Button;  // the button to select AI-suggested dialog option 1
    [SerializeField] private Button dialogOption2Button;  // the button to select AI-suggested dialog option 2
    [SerializeField] private Button dialogOption3Button;  // the button to select AI-suggested dialog option 3
    [SerializeField] private PlayerObjectiveNotificationPopup playerObjectiveNotificationPopup;

    public bool aiGeneratedDialogOptions => ((dialogOption1Button != null) || (dialogOption2Button != null) || (dialogOption3Button != null));

    public bool textEntryActive => dialogInputWindow.gameObject.activeInHierarchy;

    [SerializeField] public Sprite unknownODSImage;  // TODO - should be readonly but accesible in inspector

    public CircuitSelectionUI circuitSelectionUI;
    public WorldLabelUI worldLabelUI;
    public Image toxinVignetteImage;

    [SerializeField] private TraitTimerUI traitTimerPrefab;  // this is referenced by Traits when they want to add one of these
    private float _timeSetTool;

    private bool treeOfLifeManuallyOpened = false;

    private float originalTimeScale = -1;

    private void Awake()
    {
        core = GameObject.FindGameObjectWithTag("GameCore").GetComponent<Karyo_GameCore>();
        if (core == null)
            Debug.LogError(this + " cannot find Game Core.");

        if (traitEditingWindow == null)
            Debug.LogWarning("traitEditingWindow not assigned in " + this);

        if (targetTypeDisplay == null)
            Debug.LogWarning("targetTypeDisplay not assigned in " + this);

        if (resetMenu == null)
            Debug.LogWarning("resetMenu not assigned in " + this);

        if (treeOfLifeWindow == null)
            Debug.LogWarning("treeOfLifeWindow not assigned in " + this);

        if (circuitSelectionUI == null)
            Debug.LogWarning("circuitSelectionUI not assigned in " + this);

        if (biofoundryMainMenu == null)
            Debug.LogWarning("biofoundryMainMenu not assigned in " + this);

        if (geneticDesignWindow == null)
            Debug.LogWarning("geneticDesignWindow not assigned in " + this);

        if (dialogInputWindow == null)
            Debug.LogWarning("dialogInputWindow not assigned in " + this);

        if (dialogWindowInputField == null)
            Debug.LogWarning("dialogInputField not assigned in " + this);

        if (idlePlayerDialogWindow == null)
            Debug.LogWarning("idlePlayerDialogWindow not assigned in " + this);

        if (genericDialogWindow == null)
            Debug.LogWarning("genericDialogWindow not assigned in " + this);
        if (genericDialogTitle == null)
            Debug.LogWarning("genericDialogTitle not assigned in " + this);
        if (genericDialogBody == null)
            Debug.LogWarning("genericDialogBody not assigned in " + this);

        if (playerObjectiveNotificationPopup == null)
            Debug.LogWarning("playerObjectiveNotificationPopup not assigned in " + this);

        if (originalTimeScale == -1)
            originalTimeScale = Karyo_GameCore.Instance.DEBUG_TimeScale;

        reticle.gameObject.SetActive(true);
        worldLabelUI.gameObject.SetActive(true);

        CloseOpenWindows();
    }

    // TODO: Make more robust... We should have these stack or do something better - this is not great
    public bool InMenu => resetMenu.activeInHierarchy || traitEditingWindow.activeInHierarchy
        || treeOfLifeWindow.gameObject.activeInHierarchy || circuitSelectionUI.gameObject.activeInHierarchy ||
        biofoundryMainMenu.gameObject.activeInHierarchy || geneticDesignWindow.gameObject.activeInHierarchy ||
        bioreactorWindow.gameObject.activeInHierarchy || dialogInputWindow.activeInHierarchy || idlePlayerDialogWindow.gameObject.activeInHierarchy ||
        genericDialogWindow.activeInHierarchy;


    // it's DEBUG only because this window is temp
    public void DEBUG_DisplayTraitEditingWindow(Entity focus)
    {
        if (focus != null)
        {
            string entityType = "Target Type: " + core.targetAcquisition.CurrentFocus.entityType;
            targetTypeDisplay.text = entityType;
            traitEditor.SetFocus(focus);
            traitEditingWindow.SetActive(true);
        }
    }

    public void DEBUG_HideTraitEditingWindow()
    {
        traitEditingWindow.SetActive(false);
    }

    public void SetCurrentToolText(string text)
    {
        currentToolText.text = text;
        _timeSetTool = Time.time;
    }

    public void ClickedOutsideAllOpenWindows()
    {
        // TODO: Sometimes we might not want to allow this... OR if there is a stack of windows, we should POP instead...
        CloseOpenWindows();
    }

    public void CloseOpenWindows()
    {
        resetMenu.SetActive(false);
        traitEditingWindow.SetActive(false);
        treeOfLifeWindow.gameObject.SetActive(false);
        circuitSelectionUI.CloseAndCancelIfOpen();
        biofoundryMainMenu.SetActive(false);
        geneticDesignWindow.gameObject.SetActive(false);
        bioreactorWindow.gameObject.SetActive(false);
        DialogWindowClosedByPlayer();
        dialogInputWindow.SetActive(false);
        treeOfLifeManuallyOpened = false;
        CloseIdlePlayerDialogWindow();
        CloseGenericDialogWindow();
    }

    public void ToggleResetMenu()
    {
        if (resetMenu.activeInHierarchy)
            resetMenu.SetActive(false);
        else if (!InMenu)
            resetMenu.SetActive(true);
    }

    public void HideOrganismDataSheetUIUnlessManuallyOpened()
    {
        if (treeOfLifeManuallyOpened)
        {
            return;
        }
        treeOfLifeWindow.gameObject.SetActive(false);
    }

    public bool IsShowingTreeOfLife => treeOfLifeWindow.gameObject.activeInHierarchy;

    // this one is called by tools and other UI windows
    public void DisplayTreeofLifeWindow(OrganismDataSheet ods, TreeOfLifeUI.TreeOfLifeUIMode mode)
    {
        if (mode == TreeOfLifeUI.TreeOfLifeUIMode.SelectOutput)
            Debug.LogError("DisplayTreeofLifeWindow called in SelectOutput mode without a SelectOutputCallback.");

        DisplayTreeofLifeWindow(ods, mode, null);
    }
    public void DisplayTreeofLifeWindow(OrganismDataSheet ods, TreeOfLifeUI.TreeOfLifeUIMode mode, GenomeMapUI.SelectOutputCallback callback)
    {
        treeOfLifeWindow.gameObject.SetActive(true);
        treeOfLifeWindow.InitializeTreeofLifeUI(ods, mode, callback);
    }

    public void CloseTreeofLifeWindow()
    {
        treeOfLifeWindow.gameObject.SetActive(false);
    }

    // this one is called when the user opens the Tree of Life manually, eg - by pressing T.  Therefore it doens't start with a particular organism selected.
    public void ToggleTreeOfLifeWindow()
    {
        if (treeOfLifeWindow.gameObject.activeInHierarchy)
        {
            treeOfLifeWindow.gameObject.SetActive(false);
            treeOfLifeManuallyOpened = false;
        }
        else if (!InMenu)
        {
            treeOfLifeWindow.gameObject.SetActive(true);
            treeOfLifeWindow.InitializeTreeofLifeUI(null, TreeOfLifeUI.TreeOfLifeUIMode.ViewOnlyODS, null);
            treeOfLifeManuallyOpened = true;
        }
    }

    public void CreateTraitTimer(Trait trait, Vector3 offset)
    {
        var inst = Instantiate(traitTimerPrefab, transform);
        inst.Initialize(trait, offset);
    }

    public void DisplayBiofoundryMainMenu(PlayerData.PlayerInventoryCircuits inventory)
    {
        biofoundryMainMenu.gameObject.SetActive(true);

        // NOTE that the biofoundryMainMenu buttons call the methods below here
        // TODO - make the microorganism factory and bioblock production buttons work some day
    }

    public void BiofoundryMenuButton_CircuitDesign()
    {
        biofoundryMainMenu.SetActive(false);
        // now open the Tree of Life so the player can pick a chassis
        DisplayTreeofLifeWindow(null, TreeOfLifeUI.TreeOfLifeUIMode.SelectOrganism);
    }


    // this method is called by TreeOfLifeUI after the player clicks the Select This Organism button to select an organism/chassis
    public void BiofoundryChassisSelected(OrganismDataSheet selectedOrganism)
    {
        treeOfLifeWindow.gameObject.SetActive(false);
        geneticDesignWindow.gameObject.SetActive(true);
        geneticDesignWindow.InitializeGeneticDesignUI(selectedOrganism);
    }

    public void DisplayBioreactorWindow(Circuit circuit)
    {
        bioreactorWindow.gameObject.SetActive(true);
        bioreactorWindow.DisplayCircuit(circuit);
    }

    public void CloseBioreactorWindow()
    {
        bioreactorWindow.gameObject.SetActive(true);
    }


    public void OpenDialogInputWindow(string name)
    {
        string header = new string("Type your own dialog to ");
        header = header + name + ":";
        dialogWindowHeader.text = header;
        dialogInputWindow.SetActive(true);

        dialogWindowInputField.text = ""; // clear old text
        dialogWindowInputField.Select();
        dialogWindowInputField.ActivateInputField();
    }
    // this one is called by the ui itself to close the window after player makes a dialog decision
    public void CloseDialogInputWindow()
    {
        dialogInputWindow.SetActive(false);
    }
    // this one is called when the player clicks outside the window or presses escape to close the window without submitting any dialog
    private void DialogWindowClosedByPlayer()
    {
        dialogInputWindow.SetActive(false);
        core.player.PlayerDialogCancelled();
    }

    // called by the submit button on the dialog input window
    public void FreeformDialogSubmitted()
    {
        core.player.PlayerDialogSubmitted(dialogWindowInputField.text);
        CloseDialogInputWindow();
    }
    public void DialogOption1ButtonClicked()
    {
        core.player.PlayerDialogSubmitted(dialogOption1Button.GetComponentInChildren<TextMeshProUGUI>().text);
        CloseDialogInputWindow();
    }
    public void DialogOption2ButtonClicked()
    {
        core.player.PlayerDialogSubmitted(dialogOption2Button.GetComponentInChildren<TextMeshProUGUI>().text);
        CloseDialogInputWindow();
    }
    public void DialogOption3ButtonClicked()
    {
        core.player.PlayerDialogSubmitted(dialogOption3Button.GetComponentInChildren<TextMeshProUGUI>().text);
        CloseDialogInputWindow();
    }


    // called by player when it has 3 dialog options to populate the dialog option buttons with.
    // must always pass in an array of exactly 3 strings.
    public void PopulateDialogOptionButtons(string[] dialog_options, bool enable_buttons)
    {
        if (dialog_options?.Length != 3)
        {
            Debug.LogError("PopulateDialogOptionButtons was not passed exactly 3 dialog options.");
            return;
        }

        dialogOption1Button.GetComponentInChildren<TextMeshProUGUI>().text = dialog_options[0];
        dialogOption1Button.enabled = enable_buttons;
        dialogOption2Button.GetComponentInChildren<TextMeshProUGUI>().text = dialog_options[1];
        dialogOption2Button.enabled = enable_buttons;
        dialogOption3Button.GetComponentInChildren<TextMeshProUGUI>().text = dialog_options[2];
        dialogOption3Button.enabled = enable_buttons;
    }


    public void OpenIdlePlayerDialogWindow()
    {
        idlePlayerDialogWindow.gameObject.SetActive(true);
        idlePlayerDialogWindow.InitializeIdlePlayerDialogWindow();
    }
    public  void CloseIdlePlayerDialogWindow()
    {
        idlePlayerDialogWindow.ResumePlay();
        idlePlayerDialogWindow.gameObject.SetActive(false);
    }


    public void LaunchGenericDialogWindow(string title, string body, bool pause_game)
    {
        genericDialogTitle.text = title;
        genericDialogBody.text = body;
        genericDialogWindow.SetActive(true);

        if (pause_game)
            Time.timeScale = 0f;
    }

    public void CloseGenericDialogWindow()
    {
        Time.timeScale = originalTimeScale;
        genericDialogWindow.SetActive(false);
    }


    public void DisplayObjectivePopupNotification (NPC_PlayerObjective objective)
    {
        playerObjectiveNotificationPopup.gameObject.SetActive(true);
        playerObjectiveNotificationPopup.DisplayNotification(objective);
        // the playerObjectiveNotificationPopup will close itself after some seconds
    }

    public void ToggleObjectiveWindow()
    {
        // TODO - this won't work right if the window is already open doing something else
        if (genericDialogWindow.activeInHierarchy)
        {
            CloseGenericDialogWindow();
            return;
        }
        if (core.DoesPlayerCurrentlyHaveAnObjective())
            LaunchGenericDialogWindow("Current Objective", core.currentPlayerObjective.GetObjectiveAsText(), false);
        else
        {
            string fulfilledObjectives = core.CompletedObjectivesAsString();

            if (string.IsNullOrEmpty(fulfilledObjectives))
                LaunchGenericDialogWindow("Current Objective", "You do not currently have any Objectives. Try talking to your colleagues in the Village!", false);
            else
                LaunchGenericDialogWindow("Current Objective", "You do not currently have any Objectives. Try talking to your colleagues in the Village!\n\nCompleted objectives:\n"+fulfilledObjectives, false);

        }
    }


    private void Update()
    {
        var showTool = Time.time < showToolTime + _timeSetTool;
        currentToolText.alpha = Mathf.Lerp(currentToolText.alpha, showTool ? 1 : 0, Time.deltaTime * 10);
        reticle.gameObject.SetActive(!InMenu);
    }
}
