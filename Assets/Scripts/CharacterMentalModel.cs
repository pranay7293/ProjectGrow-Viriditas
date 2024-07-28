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
    public Dictionary<string, Opinion> Opinions { get; private set; } = new Dictionary<string, Opinion>();

    private const int MaxMemories = 20;
    private const float OpinionUpdateRate = 0.1f;

    public CharacterMentalModel(string characterName, string role, string personality)
    {
        InitializeBeliefs(role, personality);
        InitializeGoals(role);
        CurrentEmotionalState = EmotionalState.Neutral;
    }

    private void InitializeBeliefs(string role, string personality)
    {
        BeliefStrengths["Science is important"] = role == "Scientist" ? 0.9f : 0.5f;
        BeliefStrengths["Creativity is crucial"] = personality.Contains("creative") ? 0.8f : 0.4f;
        BeliefStrengths["Ethics in biotech"] = role == "Bioethicist" ? 0.9f : 0.6f;
        BeliefStrengths["Commercial viability"] = role == "Entrepreneur" ? 0.8f : 0.5f;
    }

    private void InitializeGoals(string role)
    {
        GoalImportance["Advance scientific knowledge"] = role == "Scientist" ? 0.9f : 0.3f;
        GoalImportance["Create innovative solutions"] = role == "Engineer" ? 0.8f : 0.4f;
        GoalImportance["Ensure ethical practices"] = role == "Bioethicist" ? 0.9f : 0.5f;
        GoalImportance["Commercialize breakthroughs"] = role == "Entrepreneur" ? 0.8f : 0.3f;
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

    public void UpdateOpinion(string subject, float sentiment, float confidence)
    {
        if (!Opinions.ContainsKey(subject))
        {
            Opinions[subject] = new Opinion();
        }
        Opinions[subject].UpdateOpinion(sentiment, confidence, OpinionUpdateRate);
    }

    public string MakeDecision(List<string> options, GameState currentState)
    {
        Dictionary<string, float> scores = new Dictionary<string, float>();

        foreach (var option in options)
        {
            float score = EvaluateOption(option, currentState);
            scores[option] = score;
        }
        
        return scores.OrderByDescending(kvp => kvp.Value).First().Key;
    }

    private float EvaluateOption(string option, GameState currentState)
    {
        float score = 0f;

        foreach (var goal in GoalImportance)
        {
            if (option.ToLower().Contains(goal.Key.ToLower()))
            {
                score += goal.Value;
            }
        }

        if (option.Contains("collaborate") && Relationships.Any(r => r.Value > 0.5f))
        {
            score += 0.5f;
        }

        if (currentState.CurrentChallenge.ToLower().Contains(option.ToLower()))
        {
            score += 0.5f;
        }

        if (BeliefStrengths.TryGetValue(option, out float beliefStrength))
        {
            score *= (1 + beliefStrength);
        }

        if (Memories.Any(m => m.Contains(option)))
        {
            score *= 1.2f;
        }

        if (CurrentEmotionalState == EmotionalState.Confident)
        {
            score *= 1.5f;
        }

        return score;
    }
}

public class Opinion
{
    public float Sentiment { get; private set; } = 0f;
    public float Confidence { get; private set; } = 0f;

    public void UpdateOpinion(float newSentiment, float newConfidence, float updateRate)
    {
        Sentiment = Mathf.Lerp(Sentiment, newSentiment, updateRate);
        Confidence = Mathf.Lerp(Confidence, newConfidence, updateRate);
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
}