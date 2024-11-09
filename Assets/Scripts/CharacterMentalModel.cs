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

    public class CharacterMentalModel
    {
        public Dictionary<string, float> Relationships { get; private set; } = new Dictionary<string, float>();
        public Dictionary<string, float> BeliefStrengths { get; private set; } = new Dictionary<string, float>();
        public List<PrioritizedMemory> Memories { get; private set; } = new List<PrioritizedMemory>();
        public EmotionalState CurrentEmotionalState { get; private set; }
        public Dictionary<string, float> GoalImportance { get; private set; } = new Dictionary<string, float>();
        public Dictionary<string, Opinion> Opinions { get; private set; } = new Dictionary<string, Opinion>();
        public MemoryStream MemoryStream { get; private set; }

        private const int MaxMemories = 50;
        private const float OpinionUpdateRate = 0.1f;

        public CharacterMentalModel(string characterName, string role, string personality)
        {
            MemoryStream = new MemoryStream();
            InitializeBasicTraits(role, personality);
            CurrentEmotionalState = EmotionalState.Neutral;
            AddMemory($"I am {characterName}, a {role}. {personality}", 1.0f);
        }

        private void InitializeBasicTraits(string role, string personality)
        {
            BeliefStrengths["Role Understanding"] = 1.0f;
            GoalImportance["Role Excellence"] = 1.0f;
        }

        public void AddMemory(string content, float importance)
        {
            var memory = new PrioritizedMemory(content, importance, DateTime.Now);
            Memories.Add(memory);
            Memories = Memories.OrderByDescending(m => m.Importance).Take(MaxMemories).ToList();

            string[] keywords = ExtractKeywords(content);
            MemoryStream.AddMemory(content, importance, MemoryStream.MemoryType.Observation, keywords);
        }

        public void MemoryConsolidation()
        {
            foreach (var memory in Memories)
            {
                memory.Importance *= 0.95f;
            }

            if (Memories.Count > MaxMemories)
            {
                Memories = Memories.OrderByDescending(m => m.Importance).Take(MaxMemories).ToList();
            }

            // Also consolidate MemoryStream
            MemoryStream.ConsolidateMemories();
        }

        public string MakeDecision(List<string> options, GameState currentState)
        {
            // Debug.Log($"{characterController.characterName}: Making a decision.");
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
            score *= GetEmotionalStateMultiplier();

            return score;
        }

        public void UpdateRelationship(string character, float change)
        {
            if (!Relationships.ContainsKey(character))
            {
                Relationships[character] = 0f;
            }
            Relationships[character] = Mathf.Clamp(Relationships[character] + change, -1f, 1f);
            AddMemory($"Relationship with {character} changed by {change}", Mathf.Abs(change));
        }

        public void UpdateBelief(string belief, float change)
        {
            if (!BeliefStrengths.ContainsKey(belief))
            {
                BeliefStrengths[belief] = 0.5f;
            }
            BeliefStrengths[belief] = Mathf.Clamp(BeliefStrengths[belief] + change, 0f, 1f);
        }

        public void AddConversationMemory(string speaker, string content, string target, float importance)
        {
            AddMemory($"{speaker}: {content}", importance);
            MemoryStream.AddMemory(
                content,
                importance,
                MemoryStream.MemoryType.Conversation,
                ExtractKeywords(content),
                speaker,
                target
            );
        }

        private string[] ExtractKeywords(string text)
        {
            return text.ToLower()
                      .Split(' ')
                      .Where(w => w.Length > 3)
                      .Distinct()
                      .ToArray();
        }

        public List<PrioritizedMemory> RetrieveRelevantMemories(string context, int count = 3)
        {
            var streamMemories = MemoryStream.RetrieveRelevantMemories(context, count);
            var legacyMemories = Memories
                .OrderByDescending(m => CalculateRelevance(m, context))
                .Take(count)
                .ToList();

            return legacyMemories;
        }

        private float CalculateRelevance(PrioritizedMemory memory, string context)
        {
            float score = memory.Importance;
            if (context.ToLower().Contains(memory.Content.ToLower()))
            {
                score += 0.5f;
            }
            return score;
        }

        public string Reflect()
        {
            var recentMemories = Memories.OrderByDescending(m => m.Timestamp).Take(5);
            var strongestBelief = BeliefStrengths.OrderByDescending(b => b.Value).First();
            var mostImportantGoal = GoalImportance.OrderByDescending(g => g.Value).First();

            string reflection = $"Based on recent experiences, I feel {CurrentEmotionalState}. " +
                              $"My strongest belief is in {strongestBelief.Key}, and my most important goal is to {mostImportantGoal.Key}. " +
                              $"Recently, I've been thinking about: {string.Join(", ", recentMemories.Select(m => m.Content))}";

            AddMemory(reflection, 0.6f);
            return reflection;
        }

        public void UpdateEmotionalState(EmotionalState newState)
        {
            if (newState != CurrentEmotionalState)
            {
                AddMemory($"Emotional state changed from {CurrentEmotionalState} to {newState}", 0.5f);
                CurrentEmotionalState = newState;
            }
        }

        public bool HasMetCharacter(string characterName)
        {
            return Memories.Any(m => 
                m.Content.Contains($"Met {characterName}") || 
                m.Content.Contains($"Interacted with {characterName}")
            ) || MemoryStream.HasMetCharacter(characterName);
        }

        public void UpdateOpinion(string subject, float sentiment, float confidence)
        {
            if (!Opinions.ContainsKey(subject))
            {
                Opinions[subject] = new Opinion();
            }
            Opinions[subject].UpdateOpinion(sentiment, confidence, OpinionUpdateRate);
        }

        public float EvaluateScenario(string scenario, GameState gameState)
        {
            float score = 0f;

            var relevantMemories = RetrieveRelevantMemories(scenario, 3);
            score += relevantMemories.Sum(m => m.Importance * 0.2f);

            score *= GetEmotionalStateMultiplier();

            if (scenario.ToLower().Contains(gameState.CurrentChallenge.title.ToLower()))
            {
                score += 1f;
            }

            return score;
        }

        private float GetEmotionalStateMultiplier()
        {
            return CurrentEmotionalState switch
            {
                EmotionalState.Happy or EmotionalState.Excited => 1.2f,
                EmotionalState.Sad or EmotionalState.Anxious => 0.8f,
                EmotionalState.Angry => 1.1f,
                EmotionalState.Confident => 1.3f,
                _ => 1f
            };
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
}