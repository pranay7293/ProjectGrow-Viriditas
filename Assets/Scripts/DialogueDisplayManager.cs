using UnityEngine;

public class DialogueDisplayManager : MonoBehaviour
{
    public static DialogueDisplayManager Instance { get; private set; }

    [SerializeField] private GameObject dialogueDisplayPrompt;
    [SerializeField] private GameObject playerInputWindow;

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

    private void Start()
    {
        if (dialogueDisplayPrompt == null || playerInputWindow == null)
        {
            Debug.LogError("DialogueDisplayManager: Required UI elements are not assigned!");
        }
    }

    public void ShowDialoguePrompt()
    {
        if (dialogueDisplayPrompt != null && playerInputWindow != null)
        {
            dialogueDisplayPrompt.SetActive(true);
            playerInputWindow.SetActive(false);
        }
    }

    public void ShowPlayerInput()
    {
        if (dialogueDisplayPrompt != null && playerInputWindow != null)
        {
            dialogueDisplayPrompt.SetActive(false);
            playerInputWindow.SetActive(true);
        }
    }

    public bool IsPlayerInputVisible()
    {
        return playerInputWindow != null && playerInputWindow.activeSelf;
    }
}