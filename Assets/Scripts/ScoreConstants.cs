public static class ScoreConstants
{
    public const int SIMPLE_ACTION_POINTS = 5;
    public const int MEDIUM_ACTION_POINTS = 10;
    public const int COMPLEX_ACTION_POINTS = 15;

    public const int PERSONAL_GOAL_CONTRIBUTION = 10;
    public const int PERSONAL_GOAL_COMPLETION = 20;

    public const int KEY_MILESTONE_COMPLETION = 30;

    public const int EUREKA_BONUS = 50;

    public const int SIMPLE_ACTION_FAILURE = -2;
    public const int MEDIUM_ACTION_FAILURE = -5;
    public const int COMPLEX_ACTION_FAILURE = -7;

    public const float PRIMARY_TAG_CONTRIBUTION = 0.1f;
    public const float SECONDARY_TAG_CONTRIBUTION = 0.05f;

    public static int GetActionPoints(int duration)
    {
        if (duration <= 15)
            return SIMPLE_ACTION_POINTS;
        else if (duration <= 30)
            return MEDIUM_ACTION_POINTS;
        else
            return COMPLEX_ACTION_POINTS;
    }

    public static int GetActionFailurePoints(int duration)
    {
        if (duration <= 15)
            return SIMPLE_ACTION_FAILURE;
        else if (duration <= 30)
            return MEDIUM_ACTION_FAILURE;
        else
            return COMPLEX_ACTION_FAILURE;
    }
}