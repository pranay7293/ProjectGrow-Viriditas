using System.Collections.Generic;

public struct GameState
{
    public ChallengeData CurrentChallenge;
    public int CollectiveScore;
    public Dictionary<string, int> PlayerScores;
    public float RemainingTime;

    public GameState(ChallengeData currentChallenge, int collectiveScore, Dictionary<string, int> playerScores, float remainingTime)
    {
        CurrentChallenge = currentChallenge;
        CollectiveScore = collectiveScore;
        PlayerScores = playerScores;
        RemainingTime = remainingTime;
    }
}