public static class ScoreConstants
{
    public const int SIMPLE_ACTION_POINTS = 10;
    public const int MEDIUM_ACTION_POINTS = 20;
    public const int COMPLEX_ACTION_POINTS = 30;

    public const int PERSONAL_GOAL_CONTRIBUTION = 10;
    public const int PERSONAL_GOAL_COMPLETION_BONUS = 50;

    public const int CHALLENGE_CONTRIBUTION_BONUS = 10;
    public const int MILESTONE_COMPLETION_BONUS = 50;
    public const int ALL_MILESTONES_BONUS = 500;

    public const int COLLABORATION_INITIATION_BONUS = 5;
    public const int COLLABORATION_JOIN_BONUS = 5;
    public const int COLLABORATION_BONUS = 20;
    public const float COLLAB_SUCCESS_BONUS_MULTIPLIER = 0.15f;

    public const int EUREKA_BONUS = 50;
    public const float EUREKA_CHANCE = 0.3f;

    public const int EMERGENT_SCENARIO_BONUS = 100;

    public const int INAPPROPRIATE_BEHAVIOR_PENALTY = -10;

    public static int GetActionPoints(int duration)
    {
        if (duration <= 15)
            return SIMPLE_ACTION_POINTS;
        else if (duration <= 30)
            return MEDIUM_ACTION_POINTS;
        else
            return COMPLEX_ACTION_POINTS;
    }
}