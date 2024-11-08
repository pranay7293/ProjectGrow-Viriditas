using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectGrow.AI
{
    public class MemoryStream
    {
        private List<Memory> memories = new List<Memory>();
        private const float RecencyDecayFactor = 0.995f;
        private const int MaxMemories = 100;

        public class Memory
        {
            public string Content { get; set; }
            public DateTime Timestamp { get; set; }
            public float Importance { get; set; }
            public float Recency { get; set; }
            public string[] Keywords { get; set; }
            public MemoryType Type { get; set; }
            public string Speaker { get; set; }
            public string Target { get; set; }
        }

        public enum MemoryType
        {
            Observation,
            Conversation,
            Reflection,
            Plan
        }

        public void AddMemory(string content, float importance, MemoryType type, string[] keywords, 
                            string speaker = "", string target = "")
        {
            var memory = new Memory
            {
                Content = content,
                Timestamp = DateTime.Now,
                Importance = importance,
                Recency = 1.0f,
                Keywords = keywords,
                Type = type,
                Speaker = speaker,
                Target = target
            };

            memories.Add(memory);
            ConsolidateMemories();
        }

        public List<Memory> RetrieveRelevantMemories(string context, int count = 5)
        {
            var relevantMemories = memories
                .Select(m => new 
                { 
                    Memory = m,
                    Score = CalculateRelevance(m, context)
                })
                .OrderByDescending(x => x.Score)
                .Take(count)
                .Select(x => x.Memory)
                .ToList();

            foreach (var memory in relevantMemories)
            {
                memory.Recency = 1.0f;
            }

            return relevantMemories;
        }

        public List<Memory> GetConversationHistory(string participant, int count = 5)
        {
            return memories
                .Where(m => m.Type == MemoryType.Conversation &&
                           (m.Speaker.Equals(participant, StringComparison.OrdinalIgnoreCase) ||
                            m.Target.Equals(participant, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(m => m.Timestamp)
                .Take(count)
                .ToList();
        }

        public bool HasMetCharacter(string characterName)
        {
            return memories.Any(m => 
                (m.Type == MemoryType.Conversation || m.Type == MemoryType.Observation) &&
                (m.Content.Contains($"Met {characterName}") || 
                 m.Content.Contains($"Interacted with {characterName}") ||
                 (m.Speaker == characterName || m.Target == characterName))
            );
        }

        private float CalculateRelevance(Memory memory, string context)
        {
            float recencyScore = memory.Recency;
            float importanceScore = memory.Importance;
            float relevanceScore = CalculateContextSimilarity(memory, context);
            
            return (recencyScore * 0.3f + importanceScore * 0.4f + relevanceScore * 0.3f);
        }

        public void ConsolidateMemories()
        {
            foreach (var memory in memories)
            {
                memory.Recency *= RecencyDecayFactor;
            }

            if (memories.Count > MaxMemories)
            {
                memories = memories
                    .OrderByDescending(m => CalculateRetentionValue(m))
                    .Take(MaxMemories)
                    .ToList();
            }
        }

        private float CalculateRetentionValue(Memory memory)
        {
            return memory.Importance * memory.Recency;
        }

        private float CalculateContextSimilarity(Memory memory, string context)
        {
            var contextKeywords = context.ToLower()
                                       .Split(' ')
                                       .Where(w => w.Length > 3)
                                       .ToHashSet();

            var commonKeywords = memory.Keywords.Intersect(contextKeywords).Count();
            return commonKeywords / (float)Math.Max(memory.Keywords.Length, contextKeywords.Count);
        }
    }
}