using UnityEngine;
using Photon.Pun;

public class InputManager : MonoBehaviourPunCallbacks
{
    public static InputManager Instance { get; private set; }

    public bool PlayerInteractActivate => Input.GetKeyDown(KeyCode.E);
    public bool PlayerRunModifier => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    public bool IsInDialogue { get; private set; }
    public bool IsChatLogOpen { get; private set; }

    [SerializeField] private KeyCode toggleChatLogKey = KeyCode.Tab;
    [SerializeField] private KeyCode endDialogueKey = KeyCode.Escape;

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
            if (IsInDialogue) return Vector3.zero;

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
            if (PlayerInteractActivate && !IsInDialogue)
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
        }

        UpdateCursorState();
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

    public void ToggleChatLog()
    {
        IsChatLogOpen = !IsChatLogOpen;
        DialogueManager.Instance.ToggleChatLog();
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
        Cursor.visible = IsInDialogue || IsChatLogOpen;
        Cursor.lockState = (IsInDialogue || IsChatLogOpen) ? CursorLockMode.None : CursorLockMode.Locked;
    }
}