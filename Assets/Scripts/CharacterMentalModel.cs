using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CharacterMentalModel
{
    public Dictionary<string, float> Relationships { get; private set; } = new Dictionary<string, float>();
    public Dictionary<string, float> BeliefStrengths { get; private set; } = new Dictionary<string, float>();
    public List<string> Memories { get; private set; } = new List<string>();
    public EmotionalState CurrentEmotionalState { get; private set; }
    public Dictionary<string, float> GoalImportance { get; private set; } = new Dictionary<string, float>();

    private const int MaxMemories = 20;

    public CharacterMentalModel(string characterName, string role, string personality)
    {
        InitializeBeliefs(role, personality);
        InitializeGoals(role);
        CurrentEmotionalState = EmotionalState.Neutral;
    }

    private void InitializeBeliefs(string role, string personality)
    {
        // Initialize beliefs based on role and personality
        // This is a simplified example, you'd want to expand this based on your game's needs
        BeliefStrengths["Science is important"] = role == "Scientist" ? 0.9f : 0.5f;
        BeliefStrengths["Creativity is crucial"] = personality.Contains("creative") ? 0.8f : 0.4f;
        // Add more beliefs as needed
    }

    private void InitializeGoals(string role)
    {
        // Initialize goals based on role
        // Again, this is a simplified example
        GoalImportance["Advance scientific knowledge"] = role == "Scientist" ? 0.9f : 0.3f;
        GoalImportance["Create innovative solutions"] = role == "Engineer" ? 0.8f : 0.4f;
        // Add more goals as needed
    }

    public void UpdateRelationship(string character, float change)
    {
        if (!Relationships.ContainsKey(character))
        {
            Relationships[character] = 0f;
        }
        Relationships[character] = Mathf.Clamp(Relationships[character] + change, -1f, 1f);
    }

    public void UpdateBelief(string belief, float change)
    {
        if (!BeliefStrengths.ContainsKey(belief))
        {
            BeliefStrengths[belief] = 0.5f;
        }
        BeliefStrengths[belief] = Mathf.Clamp(BeliefStrengths[belief] + change, 0f, 1f);
    }

    public void AddMemory(string memory)
    {
        Memories.Add(memory);
        if (Memories.Count > MaxMemories)
        {
            Memories.RemoveAt(0);
        }
    }

    public void UpdateEmotionalState(EmotionalState newState)
    {
        CurrentEmotionalState = newState;
    }

    public void UpdateGoalImportance(string goal, float importance)
    {
        GoalImportance[goal] = Mathf.Clamp(importance, 0f, 1f);
    }

    public string MakeDecision(List<string> options, GameState currentState)
    {
        Dictionary<string, float> scores = new Dictionary<string, float>();

        foreach (var option in options)
        {
            float score = EvaluateOption(option, currentState);
            
            // Implement confirmation bias
            if (BeliefStrengths.TryGetValue(option, out float beliefStrength))
            {
                score *= (1 + beliefStrength);
            }
            
            // Implement availability heuristic
            if (Memories.Any(m => m.Contains(option)))
            {
                score *= 1.2f;
            }
            
            // Implement overconfidence effect
            if (CurrentEmotionalState == EmotionalState.Confident)
            {
                score *= 1.5f;
            }

            scores[option] = score;
        }
        
        // Return the highest scoring option
        return scores.OrderByDescending(kvp => kvp.Value).First().Key;
    }

    private float EvaluateOption(string option, GameState currentState)
    {
        float score = 0f;

        // Evaluate based on current goals
        foreach (var goal in GoalImportance)
        {
            if (option.ToLower().Contains(goal.Key.ToLower()))
            {
                score += goal.Value;
            }
        }

        // Evaluate based on relationships
        if (option.Contains("collaborate") && Relationships.Any(r => r.Value > 0.5f))
        {
            score += 0.5f;
        }

        // Evaluate based on current game state
        if (currentState.CurrentChallenge.ToLower().Contains(option.ToLower()))
        {
            score += 0.5f;
        }

        return score;
    }
}

public enum EmotionalState
{
    Neutral,
    Happy,
    Sad,
    Angry,
    Excited,
    Anxious,
    Confident
}

public struct GameState
{
    public string CurrentChallenge;
    public List<string> CurrentSubgoals;
    public int CollectiveScore;
    // Add more relevant game state information as needed
}