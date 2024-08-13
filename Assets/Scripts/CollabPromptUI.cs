using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CollabPromptUI : MonoBehaviour
{
    public static CollabPromptUI Instance { get; private set; }

    [SerializeField] private GameObject promptPanel;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button declineButton;

    private UniversalCharacterController targetCharacter;
    private string currentActionName;

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
        promptPanel.SetActive(false);
        acceptButton.onClick.AddListener(AcceptCollab);
        declineButton.onClick.AddListener(DeclineCollab);
    }

    public void ShowPrompt(UniversalCharacterController initiator, string actionName)
    {
        targetCharacter = initiator;
        currentActionName = actionName;
        promptText.text = $"{initiator.characterName} wants to collab on {actionName}";
        promptPanel.SetActive(true);
    }

    private void AcceptCollab()
    {
        targetCharacter.JoinCollab(currentActionName);
        HidePrompt();
    }

    private void DeclineCollab()
    {
        HidePrompt();
    }

    private void HidePrompt()
    {
        promptPanel.SetActive(false);
        targetCharacter = null;
        currentActionName = null;
    }
}