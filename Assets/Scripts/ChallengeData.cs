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
    public List<string> milestoneTags = new List<string>();
    public Dictionary<string, int> tagToSliderIndex = new Dictionary<string, int>();

    public void InitializeTagMapping()
    {
        for (int i = 0; i < milestoneTags.Count; i++)
        {
            tagToSliderIndex[milestoneTags[i]] = i;
        }
    }
}