using UnityEngine;
using System.Collections.Generic;
using ProjectGrow.AI;
using System.Linq;

public class NPC_Data : MonoBehaviour
{
    private UniversalCharacterController characterController;
    private CharacterMentalModel mentalModel;

    public void Initialize(UniversalCharacterController controller)
    {
        characterController = controller;
        mentalModel = new CharacterMentalModel(
            characterController.characterName,
            characterController.aiSettings.characterRole,
            characterController.aiSettings.characterPersonality
        );
    }

    public string GetCharacterName() => characterController.characterName;
    public string GetCharacterRole() => characterController.aiSettings.characterRole;
    public string GetCharacterBackground() => characterController.aiSettings.characterBackground;
    public string GetCharacterPersonality() => characterController.aiSettings.characterPersonality;

    public void AddMemory(string memory, float importance = 0.5f)
    {
        mentalModel.AddMemory(memory, importance);
    }

    public List<string> GetMemories()
    {
        return mentalModel.Memories.Select(m => m.Content).ToList();
    }

    public List<string> GetMemoriesAboutCharacter(string characterName)
    {
        return mentalModel.Memories
            .Where(m => m.Content.Contains(characterName))
            .OrderByDescending(m => m.Timestamp)
            .Select(m => m.Content)
            .ToList();
    }

    public void UpdateRelationship(string characterName, float change)
    {
        mentalModel.UpdateRelationship(characterName, change);
    }

    public float GetRelationship(string characterName)
    {
        return mentalModel.Relationships.TryGetValue(characterName, out float value) ? value : 0f;
    }

    public float GetAverageRelationship()
    {
        if (mentalModel.Relationships.Count == 0)
            return 0f;

        float sum = 0f;
        foreach (var relationship in mentalModel.Relationships.Values)
        {
            sum += relationship;
        }
        return sum / mentalModel.Relationships.Count;
    }

    public void UpdateKnowledge(string key, string value)
    {
        mentalModel.UpdateBelief(key, 0.1f); // Small increase in belief strength
        AddMemory($"Learned: {key} - {value}");
    }

    public string MakeDecision(List<string> options, GameState currentState)
    {
        return mentalModel.MakeDecision(options, currentState);
    }

    public EmotionalState GetCurrentEmotionalState()
    {
        return mentalModel.CurrentEmotionalState;
    }

    public void UpdateEmotionalState(EmotionalState newState)
    {
        mentalModel.UpdateEmotionalState(newState);
    }

    public CharacterMentalModel GetMentalModel()
    {
        return mentalModel;
    }

    public bool HasMetCharacter(string characterName)
    {
        return mentalModel.HasMetCharacter(characterName);
    }
}