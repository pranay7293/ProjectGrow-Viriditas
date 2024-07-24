using UnityEngine;

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
}