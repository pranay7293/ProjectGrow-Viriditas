using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Hub", menuName = "Project Grow/Hub")]
public class HubData : ScriptableObject
{
    public string hubName;
    public string description;
    public List<ChallengeData> challenges;
    public Color hubColor;
}