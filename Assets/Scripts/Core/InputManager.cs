using UnityEngine;
using Photon.Pun;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviourPunCallbacks
{
    public static InputManager Instance { get; private set; }

    public bool PlayerInteractActivate => Input.GetKeyDown(KeyCode.E);
    public bool PlayerRunModifier => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    public bool IsInDialogue { get; private set; }
    public bool IsChatLogOpen { get; private set; }

    [SerializeField] private KeyCode toggleChatLogKey = KeyCode.Tab;
    [SerializeField] private KeyCode endDialogueKey = KeyCode.Escape;
    [SerializeField] private KeyCode toggleCustomInputKey = 
        Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer
        ? KeyCode.LeftCommand
        : KeyCode.LeftControl;
    [SerializeField] private KeyCode toggleActionLogKey = KeyCode.F1;
    [SerializeField] private KeyCode toggleMilestonesKey = KeyCode.F2;
    [SerializeField] private KeyCode togglePersonalGoalsKey = KeyCode.F3;
    [SerializeField] private KeyCode toggleGuideDisplayKey = KeyCode.F4;
    [SerializeField] private KeyCode toggleEurekaLogKey = KeyCode.F5;
    [SerializeField] private KeyCode acceptDialogueRequestKey = KeyCode.Y;
    [SerializeField] private KeyCode declineDialogueRequestKey = KeyCode.N;

    private UniversalCharacterController localPlayer;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public Vector3 PlayerRelativeMoveDirection
    {
        get
        {
            if (IsInDialogue || IsPointerOverUIElement()) return Vector3.zero;

            var moveVector = Vector3.zero;

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) moveVector.z += 1;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) moveVector.z -= 1;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) moveVector.x += 1;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) moveVector.x -= 1;

            return moveVector.normalized;
        }
    }

    private void Update()
    {
        if (localPlayer == null)
        {
            localPlayer = FindLocalPlayer();
        }

        if (localPlayer != null && localPlayer.photonView.IsMine)
        {
            HandlePlayerInput();
        }

        UpdateCursorState();
    }

    private void HandlePlayerInput()
    {
        if (PlayerInteractActivate && !IsInDialogue && !IsPointerOverUIElement())
        {
            localPlayer.TriggerDialogue();
        }

        if (Input.GetKeyDown(endDialogueKey) && IsInDialogue)
        {
            EndDialogue();
        }

        if (Input.GetKeyDown(toggleChatLogKey))
        {
            ToggleChatLog();
        }

        if (Input.GetKeyDown(toggleCustomInputKey))
        {
            ToggleCustomInput();
        }

        if (Input.GetKeyDown(toggleActionLogKey))
        {
            ToggleActionLog();
        }

        if (Input.GetKeyDown(toggleMilestonesKey))
        {
            ToggleMilestones();
        }

        if (Input.GetKeyDown(togglePersonalGoalsKey))
        {
            TogglePersonalGoals();
        }

        if (Input.GetKeyDown(toggleEurekaLogKey))
        {
            ToggleEurekaLog();
        }

        if (Input.GetKeyDown(toggleGuideDisplayKey))
        {
            ToggleGuideDisplay();
        }

        if (IsInDialogue)
        {
            HandleDialogueInput();
        }

        if (DialogueRequestUI.Instance.IsRequestActive())
        {
            if (Input.GetKeyDown(acceptDialogueRequestKey))
            {
                DialogueRequestUI.Instance.AcceptRequest();
            }
            else if (Input.GetKeyDown(declineDialogueRequestKey))
            {
                DialogueRequestUI.Instance.DeclineRequest();
            }
        }
    }

    private void HandleDialogueInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            DialogueManager.Instance.SelectDialogueOption(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            DialogueManager.Instance.SelectDialogueOption(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            DialogueManager.Instance.SelectDialogueOption(2);
        }
    }

    public void StartDialogue()
    {
        IsInDialogue = true;
        UpdateCursorState();
    }

    public void EndDialogue()
    {
        IsInDialogue = false;
        DialogueManager.Instance.EndConversation();
        UpdateCursorState();
    }

    private void ToggleChatLog()
    {
        IsChatLogOpen = !IsChatLogOpen;
        DialogueManager.Instance.ToggleChatLog();
        UpdateCursorState();
    }

    private void ToggleCustomInput()
    {
        DialogueManager.Instance.ToggleCustomInput();
    }

    private void ToggleActionLog()
    {
        ActionLogManager.Instance.ToggleActionLog();
    }

    private void ToggleMilestones()
    {
        GameManager.Instance.ToggleMilestonesDisplay();
    }
    
    private void TogglePersonalGoals()
    {
        // Implement this when personal goals UI is created
        Debug.Log("Toggle Personal Goals - Not yet implemented");
    }

    private void ToggleEurekaLog()
    {
    EurekaLogUI.Instance.ToggleEurekaLog();
    }

    private void ToggleGuideDisplay()
    {
        GuideBoxManager.Instance.ToggleGuideDisplay();
    }

    private UniversalCharacterController FindLocalPlayer()
    {
        UniversalCharacterController[] characters = FindObjectsOfType<UniversalCharacterController>();
        foreach (UniversalCharacterController character in characters)
        {
            if (character.photonView.IsMine && character.IsPlayerControlled)
            {
                return character;
            }
        }
        return null;
    }

    private void UpdateCursorState()
    {
        bool shouldShowCursor = IsInDialogue || IsChatLogOpen || IsPointerOverUIElement();
        Cursor.visible = shouldShowCursor;
        Cursor.lockState = shouldShowCursor ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private bool IsPointerOverUIElement()
    {
        return EventSystem.current.IsPointerOverGameObject();
    }
}