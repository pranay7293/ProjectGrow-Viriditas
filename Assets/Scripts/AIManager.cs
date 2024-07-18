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
        string prompt = $"Generate 3 dialogue options for {characterController.characterName} related to the current challenge: {GameManager.Instance.GetCurrentChallenge()}";
        string response = await npcOpenAI.GetResponse(prompt);
        return response.Split('\n');
    }

    public async Task<string> MakeDecision(string playerChoice)
    {
        string prompt = $"Generate a response for {characterController.characterName} to the player's choice: '{playerChoice}'. Consider the current challenge: {GameManager.Instance.GetCurrentChallenge()}";
        string aiResponse = await npcOpenAI.GetResponse(prompt);
        npcData.AddMemory($"Player chose: {playerChoice}. AI responded: {aiResponse}");
        npcBehavior.ProcessDecision(aiResponse);
        return aiResponse;
    }

    public async Task<string> GetNPCDialogue(string targetName)
    {
        string prompt = $"Generate a dialogue line for {characterController.characterName} to say to {targetName} about the current challenge: {GameManager.Instance.GetCurrentChallenge()}";
        return await npcOpenAI.GetResponse(prompt);
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