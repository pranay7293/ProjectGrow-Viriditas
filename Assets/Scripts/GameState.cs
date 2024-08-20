using System.Collections.Generic;
using UnityEngine;

public struct GameState
{
    public ChallengeData CurrentChallenge;
    public int CollectiveProgress;
    public Dictionary<string, int> PlayerScores;
    public float RemainingTime;
    public Dictionary<string, bool> MilestoneCompletion;

    public GameState(ChallengeData currentChallenge, int collectiveProgress, Dictionary<string, int> playerScores, float remainingTime, Dictionary<string, bool> milestoneCompletion)
    {
        CurrentChallenge = currentChallenge;
        CollectiveProgress = collectiveProgress;
        PlayerScores = playerScores;
        RemainingTime = remainingTime;
        MilestoneCompletion = milestoneCompletion;
    }
}