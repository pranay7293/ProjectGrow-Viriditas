using UnityEngine;
using Photon.Pun;
using System.Threading.Tasks;

public class AIManager : MonoBehaviourPunCallbacks
{
    private UniversalCharacterController characterController;
    private NPC_Behavior npcBehavior;
    private NPC_Data npcData;
    private NPC_openAI npcOpenAI;

    private bool isInitialized = false;

    public void Initialize(UniversalCharacterController controller)
    {
        characterController = controller;
        npcBehavior = GetComponent<NPC_Behavior>();
        npcData = GetComponent<NPC_Data>();
        npcOpenAI = GetComponent<NPC_openAI>();

        if (npcBehavior != null && npcData != null && npcOpenAI != null)
        {
            npcBehavior.Initialize(characterController, npcData);
            npcData.Initialize(characterController);
            npcOpenAI.Initialize(npcData);
            isInitialized = true;
        }
        else
        {
            Debug.LogError("AIManager: One or more required components are missing.");
        }
    }

    private void Update()
    {
        if (!photonView.IsMine || !isInitialized) return;

        npcBehavior.UpdateBehavior();
    }

    public Vector3 GetTargetPosition()
    {
        return npcBehavior.GetTargetPosition();
    }

    public async Task<string[]> GetGenerativeChoices()
    {
        return await npcOpenAI.GetGenerativeChoices();
    }

    public async void MakeDecision(string playerChoice)
    {
        string aiResponse = await npcOpenAI.GetResponse(playerChoice);
        npcData.AddMemory($"Player chose: {playerChoice}. AI responded: {aiResponse}");
        npcBehavior.ProcessDecision(aiResponse);
        GameplayManager.Instance.UpdateGameState(characterController.characterName, aiResponse);
    }

    public void UpdateKnowledge(string key, string value)
    {
        npcData.UpdateKnowledge(key, value);
    }

    public void AddMemory(string memory)
    {
        npcData.AddMemory(memory);
    }
}