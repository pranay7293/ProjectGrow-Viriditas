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

    private UniversalCharacterController initiatorCharacter;
    private UniversalCharacterController localCharacter;
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

    public void ShowPrompt(UniversalCharacterController initiator, UniversalCharacterController localPlayer, string actionName)
    {
        initiatorCharacter = initiator;
        localCharacter = localPlayer;
        currentActionName = actionName;
        promptText.text = $"{initiator.characterName} wants to collab on {actionName}";
        promptPanel.SetActive(true);
    }

    private void AcceptCollab()
    {
        localCharacter.JoinCollab(currentActionName, initiatorCharacter);
        HidePrompt();
    }

    private void DeclineCollab()
    {
        HidePrompt();
    }

    private void HidePrompt()
    {
        promptPanel.SetActive(false);
        initiatorCharacter = null;
        localCharacter = null;
        currentActionName = null;
    }
}