using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

public class AIManager : MonoBehaviourPunCallbacks
{
    private UniversalCharacterController characterController;
    private NPC_Behavior npcBehavior;
    private NPC_Data npcData;

    private bool isInitialized = false;

    public void Initialize(UniversalCharacterController controller)
    {
        characterController = controller;
        npcBehavior = GetComponent<NPC_Behavior>();
        npcData = GetComponent<NPC_Data>();

        if (npcBehavior != null && npcData != null)
        {
            npcData.Initialize(characterController);
            npcBehavior.Initialize(characterController, npcData, this);
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

        if (characterController.GetState() != UniversalCharacterController.CharacterState.Interacting)
        {
            npcBehavior.UpdateBehavior();
        }
    }

    public void UpdateKnowledge(string key, string value)
    {
        npcData.UpdateKnowledge(key, value);
    }

    public void AddMemory(string memory)
    {
        npcData.AddMemory(memory);
    }

    public void UpdateRelationship(string characterName, float change)
    {
        npcData.UpdateRelationship(characterName, change);
    }

    public string MakeDecision(List<string> options, GameState currentState)
    {
        return npcData.MakeDecision(options, currentState);
    }

    public void UpdateEmotionalState(EmotionalState newState)
    {
        npcData.UpdateEmotionalState(newState);
    }

    public List<string> GetPersonalGoals()
    {
        return characterController.GetPersonalGoals();
    }

    public Dictionary<string, bool> GetPersonalGoalCompletion()
    {
        return characterController.GetPersonalGoalCompletion();
    }
}