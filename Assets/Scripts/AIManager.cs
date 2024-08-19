using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;
using System.Linq;

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

    public bool ConsiderCollaboration(LocationManager.LocationAction action)
    {
        if (CollabManager.Instance.CanInitiateCollab(characterController))
        {
            float collaborationChance = CalculateCollaborationChance(action);
            if (Random.value < collaborationChance)
            {
                characterController.InitiateCollab(action.actionName);
                return true;
            }
        }
        return false;
    }

    private float CalculateCollaborationChance(LocationManager.LocationAction action)
    {
        float baseChance = 0.5f;
        if (action.actionName.ToLower().Contains(characterController.aiSettings.characterRole.ToLower()))
        {
            baseChance += 0.2f;
        }
        if (GameManager.Instance.GetCurrentChallenge().title.ToLower().Contains(action.actionName.ToLower()))
        {
            baseChance += 0.2f;
        }
        return Mathf.Clamp01(baseChance);
    }

    public int DecideScenario(List<string> scenarios, GameState gameState)
    {
        Dictionary<int, float> scenarioScores = new Dictionary<int, float>();

        for (int i = 0; i < scenarios.Count; i++)
        {
            float score = npcData.GetMentalModel().EvaluateScenario(scenarios[i], gameState);
            scenarioScores[i] = score;
        }

        // If there's a tie, randomly choose among the top-scoring scenarios
        float maxScore = scenarioScores.Values.Max();
        var topScenarios = scenarioScores.Where(kvp => Mathf.Approximately(kvp.Value, maxScore)).ToList();
        
        return topScenarios[Random.Range(0, topScenarios.Count)].Key;
    }

    public bool DecideOnCollaboration(string actionName)
    {
        float collaborationChance = 0.5f; // Base 50% chance

        if (actionName.ToLower().Contains(characterController.aiSettings.characterRole.ToLower()))
        {
            collaborationChance += 0.2f;
        }

        if (GameManager.Instance.GetCurrentChallenge().title.ToLower().Contains(actionName.ToLower()))
        {
            collaborationChance += 0.2f;
        }

        float averageRelationship = npcData.GetAverageRelationship();
        collaborationChance += averageRelationship * 0.1f;

        return Random.value < collaborationChance;
    }
}