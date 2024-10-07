using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "AISettings", menuName = "AI/AISettings", order = 1)]
public class AISettings : ScriptableObject
{
    public string characterRole;
    [TextArea(5, 10)]
    public string characterBackground;
    [TextArea(8, 10)]
    public string characterPersonality;
    public float decisionInterval = 10f;
    public float interactionProbability = 0.5f;
    [TextArea(3, 10)]
    public List<string> personalGoals;
    public List<string> personalGoalTags = new List<string>();
    public Dictionary<string, int> tagToSliderIndex = new Dictionary<string, int>();

    public void InitializeTagMapping()
    {
        for (int i = 0; i < personalGoalTags.Count; i++)
        {
            tagToSliderIndex[personalGoalTags[i]] = i;
        }
    }
}