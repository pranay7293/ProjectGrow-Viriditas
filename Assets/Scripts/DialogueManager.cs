using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using System.Threading.Tasks;

public class DialogueManager : MonoBehaviourPunCallbacks
{
    public static DialogueManager Instance { get; private set; }

    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI npcNameText;
    [SerializeField] private Button[] optionButtons;
    [SerializeField] private TextMeshProUGUI[] optionTexts;
    [SerializeField] private TMP_InputField customInputField;
    [SerializeField] private Button submitCustomInputButton;

    private UniversalCharacterController currentNPC;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        submitCustomInputButton.onClick.AddListener(SubmitCustomInput);
    }

    public void InitiateDialogue(UniversalCharacterController npc)
    {
        if (npc == null)
        {
            Debug.LogError("Attempted to initiate dialogue with null NPC");
            return;
        }
        currentNPC = npc;
        photonView.RPC("RPC_OpenDialogue", RpcTarget.All, npc.photonView.ViewID);
    }

    [PunRPC]
    private async void RPC_OpenDialogue(int npcViewID)
    {
        UniversalCharacterController npc = PhotonView.Find(npcViewID).GetComponent<UniversalCharacterController>();
        if (npc != null)
        {
            currentNPC = npc;
            npcNameText.text = npc.characterName;
            string[] options = await npc.GetGenerativeChoices();

            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (i < options.Length)
                {
                    optionTexts[i].text = options[i];
                    int index = i;
                    optionButtons[i].onClick.RemoveAllListeners();
                    optionButtons[i].onClick.AddListener(() => SelectDialogueOption(index));
                    optionButtons[i].gameObject.SetActive(true);
                }
                else
                {
                    optionButtons[i].gameObject.SetActive(false);
                }
            }

            dialoguePanel.SetActive(true);
            customInputField.text = "";
        }
    }

    private void SelectDialogueOption(int optionIndex)
    {
        if (currentNPC != null)
        {
            string selectedOption = optionTexts[optionIndex].text;
            ProcessPlayerChoice(selectedOption);
        }
    }

    private void SubmitCustomInput()
    {
        if (currentNPC != null && !string.IsNullOrEmpty(customInputField.text))
        {
            ProcessPlayerChoice(customInputField.text);
        }
    }

    private void ProcessPlayerChoice(string playerChoice)
    {
        GameplayManager.Instance.AddPlayerAction(playerChoice);
        currentNPC.GetComponent<AIManager>().MakeDecision(playerChoice);
        CloseDialogue();
    }

    private void CloseDialogue()
    {
        dialoguePanel.SetActive(false);
        currentNPC = null;
    }
}