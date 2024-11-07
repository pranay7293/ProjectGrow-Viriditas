using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectGrow.AI
{
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

    public class PrioritizedMemory
    {
        public string Content { get; private set; }
        public float Importance { get; set; }
        public DateTime Timestamp { get; private set; }

        public PrioritizedMemory(string content, float importance, DateTime timestamp)
        {
            Content = content;
            Importance = importance;
            Timestamp = timestamp;
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

    public class CharacterMentalModel
    {
        public Dictionary<string, float> Relationships { get; private set; } = new Dictionary<string, float>();
        public Dictionary<string, float> BeliefStrengths { get; private set; } = new Dictionary<string, float>();
        public List<PrioritizedMemory> Memories { get; private set; } = new List<PrioritizedMemory>();
        public EmotionalState CurrentEmotionalState { get; private set; }
        public Dictionary<string, float> GoalImportance { get; private set; } = new Dictionary<string, float>();
        public Dictionary<string, Opinion> Opinions { get; private set; } = new Dictionary<string, Opinion>();

        private const int MaxMemories = 50;
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

        public void AddMemory(string content, float importance)
        {
            PrioritizedMemory newMemory = new PrioritizedMemory(content, importance, DateTime.Now);
            Memories.Add(newMemory);
            Memories = Memories.OrderByDescending(m => m.Timestamp).ThenByDescending(m => m.Importance).Take(MaxMemories).ToList();
        }

        public void MemoryConsolidation()
        {
            // Decay old memories
            foreach (var memory in Memories)
            {
                memory.Importance *= 0.95f; // Slight decay over time
            }

            // Remove least important memories if we're over the limit
            if (Memories.Count > MaxMemories)
            {
                Memories = Memories.OrderByDescending(m => m.Importance).Take(MaxMemories).ToList();
            }
        }

        public string Reflect()
        {
            var recentMemories = Memories.OrderByDescending(m => m.Timestamp).Take(5);
            var strongestBelief = BeliefStrengths.OrderByDescending(b => b.Value).First();
            var mostImportantGoal = GoalImportance.OrderByDescending(g => g.Value).First();

            return $"Based on recent experiences, I feel {CurrentEmotionalState}. " +
                   $"My strongest belief is in {strongestBelief.Key}, and my most important goal is to {mostImportantGoal.Key}. " +
                   $"Recently, I've been thinking about: {string.Join(", ", recentMemories.Select(m => m.Content))}";
        }

        public List<PrioritizedMemory> RetrieveRelevantMemories(string context, int count = 3)
        {
            return Memories.OrderByDescending(m => RelevanceScore(m, context)).Take(count).ToList();
        }

        private float RelevanceScore(PrioritizedMemory memory, string context)
        {
            float score = memory.Importance;
            if (context.ToLower().Contains(memory.Content.ToLower()))
            {
                score += 1.0f; // Increase score for direct relevance
            }
            return score;
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

         public bool HasMetCharacter(string characterName)
        {
            return Memories.Any(m => m.Content.Contains($"Met {characterName}") || m.Content.Contains($"Interacted with {characterName}"));
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

        public float EvaluateScenario(string scenario, GameState gameState)
        {
            float score = 0f;

            // Check if the scenario aligns with beliefs
            foreach (var belief in BeliefStrengths)
            {
                if (scenario.ToLower().Contains(belief.Key.ToLower()))
                {
                    score += belief.Value;
                }
            }

            // Check if the scenario aligns with goals
            foreach (var goal in GoalImportance)
            {
                if (scenario.ToLower().Contains(goal.Key.ToLower()))
                {
                    score += goal.Value;
                }
            }

            // Consider the current emotional state
            switch (CurrentEmotionalState)
            {
                case EmotionalState.Happy:
                case EmotionalState.Excited:
                    score *= 1.2f; // More likely to choose positive or exciting scenarios
                    break;
                case EmotionalState.Sad:
                case EmotionalState.Anxious:
                    score *= 0.8f; // Less likely to choose risky or challenging scenarios
                    break;
                case EmotionalState.Angry:
                    score *= 1.1f; // Slightly more likely to choose confrontational scenarios
                    break;
                case EmotionalState.Confident:
                    score *= 1.3f; // More likely to choose ambitious scenarios
                    break;
            }

            // Consider the current challenge
            if (scenario.ToLower().Contains(gameState.CurrentChallenge.title.ToLower()))
            {
                score += 1f;
            }

            // Add a small random factor for variety
            score += UnityEngine.Random.Range(0f, 0.5f);

            return score;
        }

        private float EvaluateOption(string option, GameState currentState)
        {
            float score = 0f;

            // Consider goals
            foreach (var goal in GoalImportance)
            {
                if (option.ToLower().Contains(goal.Key.ToLower()))
                {
                    score += goal.Value;
                }
            }

            // Consider beliefs
            if (BeliefStrengths.TryGetValue(option, out float beliefStrength))
            {
                score *= (1 + beliefStrength);
            }

            // Consider relationships
            if (option.Contains("collaborate") && Relationships.Any(r => r.Value > 0.5f))
            {
                score += 0.5f;
            }

            // Consider current challenge
            if (currentState.CurrentChallenge.title.ToLower().Contains(option.ToLower()))
            {
                score += 0.5f;
            }

            // Consider memories
            var relevantMemories = RetrieveRelevantMemories(option, 2);
            foreach (var memory in relevantMemories)
            {
                score += memory.Importance * 0.2f;
            }

            // Consider emotional state
            score *= EmotionalStateMultiplier();

            return score;
        }

        private float EmotionalStateMultiplier()
        {
            switch (CurrentEmotionalState)
            {
                case EmotionalState.Happy:
                case EmotionalState.Excited:
                    return 1.2f;
                case EmotionalState.Sad:
                case EmotionalState.Anxious:
                    return 0.8f;
                case EmotionalState.Angry:
                    return 1.1f;
                case EmotionalState.Confident:
                    return 1.3f;
                default:
                    return 1f;
            }
        }
    }
}