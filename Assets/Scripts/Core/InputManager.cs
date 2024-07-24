using UnityEngine;
using Photon.Pun;

public class InputManager : MonoBehaviourPunCallbacks
{
    public static InputManager Instance { get; private set; }

    public bool PlayerInteractActivate => Input.GetKeyDown(KeyCode.E);
    public bool PlayerRunModifier => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    public bool IsInDialogue { get; set; }
    public bool IsChatLogOpen { get; set; }

    [SerializeField] private KeyCode toggleChatLogKey = KeyCode.Tab;

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
        UniversalCharacterController localPlayer = FindLocalPlayer();
        if (localPlayer != null && localPlayer.photonView.IsMine)
        {
            if (PlayerInteractActivate)
            {
                if (IsInDialogue)
                {
                    CloseDialogue();
                }
                else
                {
                    localPlayer.TriggerDialogue();
                }
            }

            if (Input.GetKeyDown(toggleChatLogKey))
            {
                ToggleChatLog();
            }
        }

        UpdateCursorState();
    }

    private void CloseDialogue()
    {
        IsInDialogue = false;
        DialogueManager.Instance.CloseDialogue();
    }

    public void ToggleChatLog()
    {
        IsChatLogOpen = !IsChatLogOpen;
        DialogueManager.Instance.ToggleChatLog();
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
        if (IsInDialogue || IsChatLogOpen)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}