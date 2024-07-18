using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using System.Collections.Generic;
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
    [SerializeField] private TextMeshProUGUI dialogueHistoryText;
    [SerializeField] private int maxDialogueHistoryEntries = 5;

    private UniversalCharacterController currentNPC;
    private Queue<string> dialogueHistory = new Queue<string>();

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
        dialoguePanel.SetActive(false);
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
            dialoguePanel.SetActive(true);
            string[] options = await npc.GetComponent<AIManager>().GetGenerativeChoices();

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

            customInputField.text = "";
            UpdateDialogueHistory($"{npc.characterName} is ready to talk.");
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

    private async void ProcessPlayerChoice(string playerChoice)
    {
        UpdateDialogueHistory($"You: {playerChoice}");
        GameManager.Instance.AddPlayerAction(playerChoice);
        string aiResponse = await currentNPC.GetComponent<AIManager>().MakeDecision(playerChoice);
        UpdateDialogueHistory($"{currentNPC.characterName}: {aiResponse}");
        GameManager.Instance.UpdateGameState(currentNPC.characterName, aiResponse);
        CloseDialogue();
    }

    private void CloseDialogue()
    {
        dialoguePanel.SetActive(false);
        currentNPC = null;
    }

    private void UpdateDialogueHistory(string entry)
    {
        dialogueHistory.Enqueue(entry);
        if (dialogueHistory.Count > maxDialogueHistoryEntries)
        {
            dialogueHistory.Dequeue();
        }
        dialogueHistoryText.text = string.Join("\n", dialogueHistory);
    }

    public void TriggerNPCDialogue(UniversalCharacterController initiator, UniversalCharacterController target)
    {
        photonView.RPC("RPC_TriggerNPCDialogue", RpcTarget.All, initiator.photonView.ViewID, target.photonView.ViewID);
    }

    [PunRPC]
    private async void RPC_TriggerNPCDialogue(int initiatorViewID, int targetViewID)
    {
        UniversalCharacterController initiator = PhotonView.Find(initiatorViewID).GetComponent<UniversalCharacterController>();
        UniversalCharacterController target = PhotonView.Find(targetViewID).GetComponent<UniversalCharacterController>();

        if (initiator != null && target != null)
        {
            string initiatorDialogue = await initiator.GetComponent<AIManager>().GetNPCDialogue(target.characterName);
            string targetResponse = await target.GetComponent<AIManager>().GetNPCDialogue(initiator.characterName);

            UpdateDialogueHistory($"{initiator.characterName}: {initiatorDialogue}");
            UpdateDialogueHistory($"{target.characterName}: {targetResponse}");

            GameManager.Instance.UpdateGameState(initiator.characterName, initiatorDialogue);
            GameManager.Instance.UpdateGameState(target.characterName, targetResponse);
        }
    }
}