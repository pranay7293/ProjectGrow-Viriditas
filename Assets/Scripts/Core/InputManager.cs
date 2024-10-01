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
    [SerializeField] public float interactionDistance = 5f;

    private UniversalCharacterController localPlayer;
    private UniversalCharacterController currentInteractableCharacter;
    private bool cursorWasVisible;
    private CursorLockMode previousLockState;

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
            if (IsUIActive || IsInDialogue || IsPointerOverUIElement() || IsEurekaLogOpen()) return Vector3.zero;

            Vector3 moveVector = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
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
    }

    private void HandlePlayerInput()
    {
        if (Input.GetKeyDown(interactKey) && !IsUIActive && !IsInDialogue && !IsPointerOverUIElement())
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
        if (localPlayer == null)
        {
            return;
        }

        Vector3 rayOrigin = localPlayer.transform.position + Vector3.up * 1.5f;
        Vector3 forwardDirection = localPlayer.transform.forward;
        
        Ray ray = new Ray(rayOrigin, forwardDirection);
        Debug.DrawRay(ray.origin, ray.direction * interactionDistance, Color.red);

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, interactionDistance))
        {
            UniversalCharacterController character = hit.collider.GetComponentInParent<UniversalCharacterController>();

            if (character != null && character != localPlayer)
            {
                Vector3 directionToCharacter = (character.transform.position - localPlayer.transform.position).normalized;
                float dotProduct = Vector3.Dot(localPlayer.transform.forward, directionToCharacter);

                if (dotProduct > 0.2f)
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
                    return;
                }
            }
        }

        ClearCurrentInteractableCharacter();
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
            if (currentInteractableCharacter.HasState(UniversalCharacterController.CharacterState.Chatting) ||
                currentInteractableCharacter.HasState(UniversalCharacterController.CharacterState.Collaborating) ||
                currentInteractableCharacter.HasState(UniversalCharacterController.CharacterState.PerformingAction))
            {
                Debug.Log($"{currentInteractableCharacter.characterName} is currently busy.");
            }
            else
            {
                DialogueManager.Instance.InitiateDialogue(currentInteractableCharacter);
            }
        }
    }

    public void StartDialogue()
    {
        IsInDialogue = true;
        SetUIActive(true);
    }

    public void EndDialogue()
    {
        IsInDialogue = false;
        SetUIActive(false);
        DialogueManager.Instance.EndConversation();
    }

    private void ToggleChatLog()
    {
        IsChatLogOpen = !IsChatLogOpen;
        SetUIActive(IsChatLogOpen);
        DialogueManager.Instance.ToggleChatLog();
    }

    private void ToggleCustomInput()
    {
        DialogueManager.Instance.ToggleCustomInput();
    }

    private void ToggleActionLog()
    {
        ActionLogManager.Instance.ToggleActionLog();
        SetUIActive(ActionLogManager.Instance.IsLogVisible());
    }

    private void ToggleMilestones()
    {
        GameManager.Instance.ToggleMilestonesDisplay();
        SetUIActive(GameManager.Instance.IsMilestonesDisplayVisible());
    }

    private void TogglePersonalGoals()
    {
        Debug.Log("Toggle Personal Goals - Not yet implemented");
    }

    private void ToggleEurekaLog()
    {
        if (eurekaLogUI != null)
        {
            eurekaLogUI.ToggleEurekaLog();
            SetUIActive(eurekaLogUI.IsLogVisible());
        }
        else
        {
            Debug.LogWarning("EurekaLogUI reference is missing in InputManager");
        }
    }

    private void ToggleGuideDisplay()
    {
        GuideBoxManager.Instance.ToggleGuideDisplay();
        SetUIActive(GuideBoxManager.Instance.IsGuideDisplayVisible());
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

    public void SetUIActive(bool active)
    {
        if (IsUIActive != active)
        {
            IsUIActive = active;
            UpdateCursorState();
        }
    }

    private void UpdateCursorState()
    {
        if (IsUIActive)
        {
            cursorWasVisible = Cursor.visible;
            previousLockState = Cursor.lockState;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.visible = cursorWasVisible;
            Cursor.lockState = previousLockState;
        }

        if (localPlayer != null && localPlayer.PlayerCamera != null)
        {
            localPlayer.PlayerCamera.GetComponent<com.ootii.Cameras.CameraController>().enabled = !IsUIActive;
        }
    }

    public bool IsEurekaLogOpen()
    {
        return eurekaLogUI != null && eurekaLogUI.IsLogVisible();
    }

    private bool IsPointerOverUIElement()
    {
        return EventSystem.current.IsPointerOverGameObject();
    }
}