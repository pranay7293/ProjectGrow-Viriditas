using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueRequestUI : MonoBehaviour
{
    public static DialogueRequestUI Instance { get; private set; }

    [SerializeField] private GameObject requestPanel;
    [SerializeField] private TextMeshProUGUI requestText;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button declineButton;

    private UniversalCharacterController requestingNPC;

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
        requestPanel.SetActive(false);
        acceptButton.onClick.AddListener(AcceptRequest);
        declineButton.onClick.AddListener(DeclineRequest);
    }

    public void ShowRequest(UniversalCharacterController npc)
    {
        requestingNPC = npc;
        requestText.text = $"{npc.characterName} wants to talk to you. Do you accept?";
        requestPanel.SetActive(true);
    }

    public void AcceptRequest()
    {
        requestPanel.SetActive(false);
        if (requestingNPC != null)
        {
            requestingNPC.AddState(UniversalCharacterController.CharacterState.Chatting);
            DialogueManager.Instance.InitiateDialogue(requestingNPC);
        }
        requestingNPC = null;
    }

    public void DeclineRequest()
    {
        HideRequest();
        requestingNPC = null;
    }

    public void HideRequest()
    {
        requestPanel.SetActive(false);
    }

    public bool IsRequestActive()
    {
        return requestPanel != null && requestPanel.activeSelf;
    }
}