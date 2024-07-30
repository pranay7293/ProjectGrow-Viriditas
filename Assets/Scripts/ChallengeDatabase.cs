using UnityEngine;
using System.Collections.Generic;

public static class ChallengeDatabase
{
    private static Dictionary<string, ChallengeData> challenges;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        challenges = new Dictionary<string, ChallengeData>();
        ChallengeData[] loadedChallenges = Resources.LoadAll<ChallengeData>("Challenges");
        foreach (var challenge in loadedChallenges)
        {
            challenges[challenge.title] = challenge;
        }
    }

    public static ChallengeData GetChallenge(string title)
    {
        if (challenges.TryGetValue(title, out ChallengeData challenge))
        {
            return challenge;
        }
        Debug.LogWarning($"Challenge '{title}' not found.");
        return null;
    }
}