using UnityEngine;
using Photon.Pun;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviourPunCallbacks
{
    public static InputManager Instance { get; private set; }

    public bool PlayerRunModifier => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    public bool IsInDialogue { get; private set; }
    public bool IsChatLogOpen { get; private set; }
    public bool IsUIActive { get; private set; }

    [SerializeField] private KeyCode interactKey = KeyCode.E;
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
    [SerializeField] private EurekaLogUI eurekaLogUI;
    [SerializeField] private float interactionDistance = 5f;

    private UniversalCharacterController localPlayer;
    private bool wasUIActiveLastFrame;
    private UniversalCharacterController currentInteractableCharacter;

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
            CheckForInteractableCharacter();
        }

        UpdateCursorState();
    }

    private void HandlePlayerInput()
    {
        if (Input.GetKeyDown(interactKey) && !IsInDialogue && !IsPointerOverUIElement())
        {
            TryInteract();
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

        if (DialogueRequestUI.Instance != null && DialogueRequestUI.Instance.IsRequestActive())
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

    private void CheckForInteractableCharacter()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance))
        {
            UniversalCharacterController character = hit.collider.GetComponent<UniversalCharacterController>();
            if (character != null && character.IsInteractable())
            {
                if (currentInteractableCharacter != character)
                {
                    if (currentInteractableCharacter != null)
                    {
                        currentInteractableCharacter.HideOutline();
                    }
                    currentInteractableCharacter = character;
                    currentInteractableCharacter.ShowOutline();
                }
            }
            else
            {
                ClearCurrentInteractableCharacter();
            }
        }
        else
        {
            ClearCurrentInteractableCharacter();
        }
    }

    private void ClearCurrentInteractableCharacter()
    {
        if (currentInteractableCharacter != null)
        {
            currentInteractableCharacter.HideOutline();
            currentInteractableCharacter = null;
        }
    }

    private void TryInteract()
    {
        if (currentInteractableCharacter != null)
        {
            DialogueManager.Instance.InitiateDialogue(currentInteractableCharacter);
        }
    }

    public void StartDialogue()
    {
        IsInDialogue = true;
        IsUIActive = true;
        UpdateCursorState();
    }

    public void EndDialogue()
    {
        IsInDialogue = false;
        IsUIActive = false;
        DialogueManager.Instance.EndConversation();
        UpdateCursorState();
    }

    private void ToggleChatLog()
    {
        IsChatLogOpen = !IsChatLogOpen;
        IsUIActive = IsChatLogOpen;
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
        IsUIActive = ActionLogManager.Instance.IsLogVisible();
        UpdateCursorState();
    }

    private void ToggleMilestones()
    {
        GameManager.Instance.ToggleMilestonesDisplay();
        IsUIActive = GameManager.Instance.IsMilestonesDisplayVisible();
        UpdateCursorState();
    }
    
    private void TogglePersonalGoals()
    {
        // Implement this when personal goals UI is created
        Debug.Log("Toggle Personal Goals - Not yet implemented");
    }

    private void ToggleEurekaLog()
    {
        if (eurekaLogUI != null)
        {
            eurekaLogUI.ToggleEurekaLog();
            IsUIActive = eurekaLogUI.IsLogVisible();
            UpdateCursorState();
        }
        else
        {
            Debug.LogWarning("EurekaLogUI reference is missing in InputManager");
        }
    }

    private void ToggleGuideDisplay()
    {
        GuideBoxManager.Instance.ToggleGuideDisplay();
        IsUIActive = GuideBoxManager.Instance.IsGuideDisplayVisible();
        UpdateCursorState();
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
        bool shouldShowCursor = IsUIActive || IsInDialogue || IsPointerOverUIElement();

        if (shouldShowCursor != wasUIActiveLastFrame)
        {
            Cursor.visible = shouldShowCursor;
            Cursor.lockState = shouldShowCursor ? CursorLockMode.None : CursorLockMode.Locked;
            wasUIActiveLastFrame = shouldShowCursor;
        }
    }

    public void SetUIActive(bool active)
    {
        IsUIActive = active;
        UpdateCursorState();
    }

    private bool IsPointerOverUIElement()
    {
        return EventSystem.current.IsPointerOverGameObject();
    }
}