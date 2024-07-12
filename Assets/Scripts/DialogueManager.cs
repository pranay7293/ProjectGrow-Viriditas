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
            string[] options = await npc.GetDialogueOptions();

            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (i < options.Length)
                {
                    optionTexts[i].text = options[i];
                    optionButtons[i].gameObject.SetActive(true);
                }
                else
                {
                    optionButtons[i].gameObject.SetActive(false);
                }
            }

            dialoguePanel.SetActive(true);
        }
    }

    public void SelectDialogueOption(int optionIndex)
    {
        Debug.Log($"Selected option {optionIndex + 1} for NPC {currentNPC.characterName}");
        CloseDialogue();
    }

    public void CloseDialogue()
    {
        dialoguePanel.SetActive(false);
        currentNPC = null;
    }
}