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
        // Debug.Log($"Loaded {loadedChallenges.Length} challenges");
        foreach (var challenge in loadedChallenges)
        {
            challenges[challenge.title] = challenge;
            // Debug.Log($"Added challenge: {challenge.title}");
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

    public static string[] GetAllChallengeTitles()
    {
        return new List<string>(challenges.Keys).ToArray();
    }
}