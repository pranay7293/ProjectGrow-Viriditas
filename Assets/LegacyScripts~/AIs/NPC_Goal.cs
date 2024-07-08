// this tracks a goal the NPC has given itself
[System.Serializable]
public class NPC_Goal
{
    public enum NPC_GoalType
    {
        Nothing,
        MoveTo,
        TalkTo,
        PerformCustomAction,  // this supports WORK but could support other custom actions if some NPCs do something besides work
        GivePlayerObjective
    }

    public NPC_GoalType goalType;
    public NPC_Location destination; // for MoveTo
    public string targetCharacter;  // for TalkTo
    public NPC_PlayerObjective playerObjective;  // for GivePlayerObjective
    public string dialog; // for TalkTo and GivePlayerObjective

    public NPC_Goal(NPC_GoalType goalType)
    {
        this.goalType = goalType;
    }

    public new string ToString()
    {
        string toReturn = new string(goalType.ToString());

        if (goalType == NPC_GoalType.MoveTo)
            toReturn = toReturn + " destination: " + destination;
        if (goalType == NPC_GoalType.GivePlayerObjective)
            toReturn = toReturn + " objective: " + playerObjective.ToString() + ", dialog: " + dialog;
        if (goalType == NPC_GoalType.TalkTo) 
            toReturn = toReturn + " target: " + targetCharacter + ", dialog: " + dialog;

        return toReturn;
    }
}
