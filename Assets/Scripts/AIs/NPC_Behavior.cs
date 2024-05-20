// this tracks what the NPC is doing right now
[System.Serializable]
public class NPC_Behavior
{
    public enum NPC_BehaviorType
    {
        Idle,
        Moving,
        SpeakingTo,  // it's always assumed the NPC is speaking to currentGoal.targetCharacter
        BeingSpokenTo,
        PerformingCustomAction,
        AwaitingInstructions  // waiting to hear back from openAI
    }

    public NPC_BehaviorType behaviorType;
    public string destination;
    public string beingSpokenToBy;

    public static NPC_Behavior Idle()
    {
        return new NPC_Behavior { behaviorType = NPC_BehaviorType.Idle };
    }

    public static NPC_Behavior Moving(string destination)
    {
        return new NPC_Behavior { behaviorType = NPC_BehaviorType.Moving, destination = destination };
    }

    public static NPC_Behavior Speaking()
    {
        return new NPC_Behavior { behaviorType = NPC_BehaviorType.SpeakingTo };
    }

    public static NPC_Behavior BeingSpokenTo(string beingSpokenToBy)
    {
        return new NPC_Behavior { behaviorType = NPC_BehaviorType.BeingSpokenTo, beingSpokenToBy = beingSpokenToBy };
    }

    public static NPC_Behavior PerformingCustomAction()
    {
        return new NPC_Behavior { behaviorType = NPC_BehaviorType.PerformingCustomAction };
    }

    public static NPC_Behavior AwaitingInstructions()
    {
        return new NPC_Behavior { behaviorType = NPC_BehaviorType.AwaitingInstructions };
    }

    public new string ToString()
    {
        string toReturn = new string(behaviorType.ToString());

        if (behaviorType == NPC_BehaviorType.Moving)
            toReturn = toReturn + " destination: " + destination;
        if (behaviorType == NPC_BehaviorType.BeingSpokenTo)
            toReturn = toReturn + " beingSpokenToBy: " + beingSpokenToBy;

        return toReturn;
    }
}
