using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Challenge", menuName = "Project Grow/Challenge")]
public class ChallengeData : ScriptableObject
{
    public string title;
    [TextArea(3, 10)]
    public string description;
    public List<string> milestones;
    public int goalScore;
    public bool isAvailable;
    public Sprite iconSprite;
}