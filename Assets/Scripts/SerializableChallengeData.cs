using System;
using System.Collections.Generic;

[Serializable]
public class SerializableChallengeData
{
    public string title;
    public string description;
    public List<string> milestones;
    public int goalScore;

    public SerializableChallengeData(ChallengeData challengeData)
    {
        title = challengeData.title;
        description = challengeData.description;
        milestones = new List<string>(challengeData.milestones);
        goalScore = challengeData.goalScore;
    }
}