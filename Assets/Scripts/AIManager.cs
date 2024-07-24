using UnityEngine;
using Photon.Pun;
using System.Threading.Tasks;
using System.Collections.Generic;

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
            npcBehavior.Initialize(characterController, npcData, this);
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

    public async Task<List<string>> GetGenerativeChoices()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return new List<string> { "Waiting for choices...", "Waiting for choices...", "Waiting for choices..." };
        }

        GameState currentState = GameManager.Instance.GetCurrentGameState();
        string prompt = $"Generate 3 dialogue options for {characterController.characterName} related to the current challenge: {currentState.CurrentChallenge}. Consider their emotional state: {npcData.GetCurrentEmotionalState()}";
        return await npcOpenAI.GetGenerativeChoices();
    }

    public async Task<string> MakeDecision(List<string> options)
    {
        GameState currentState = GameManager.Instance.GetCurrentGameState();
        string decision = npcData.MakeDecision(options, currentState);
        
        string prompt = $"Generate a response for {characterController.characterName} who has decided to {decision}. Consider their current emotional state: {npcData.GetCurrentEmotionalState()}";
        return await npcOpenAI.GetResponse(prompt);
    }

    public async Task<string> ProcessCustomInput(string customInput)
    {
        GameState currentState = GameManager.Instance.GetCurrentGameState();
        string prompt = $"Generate a response for {characterController.characterName} to the player's custom input: '{customInput}'. Consider the current challenge: {currentState.CurrentChallenge} and their emotional state: {npcData.GetCurrentEmotionalState()}";
        return await npcOpenAI.GetResponse(prompt);
    }

    public async Task<string> GetNPCDialogue(string targetName)
    {
        GameState currentState = GameManager.Instance.GetCurrentGameState();
        float relationship = npcData.GetRelationship(targetName);
        string emotionalState = npcData.GetCurrentEmotionalState().ToString();

        string prompt = $"Generate a dialogue line for {characterController.characterName} to say to {targetName} about the current challenge: {currentState.CurrentChallenge}. Their relationship is {relationship} and {characterController.characterName}'s current emotional state is {emotionalState}.";
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