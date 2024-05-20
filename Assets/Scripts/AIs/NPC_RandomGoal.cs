using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// class for creating a random goal for an NPC  (used for testing NPCs without going online to openAI)

public class NPC_RandomGoal : MonoBehaviour
{

    public static NPC_Goal GenerateRandomGoal(NPC npc, NPC_Data npc_data, Player player)
    {
        NPC_Goal goal = new NPC_Goal(NPC_Goal.NPC_GoalType.Nothing);

        int random_action = UnityEngine.Random.Range(0, 4);

        switch (random_action)
        {
            case 0:  
                break;

            case 1: 
                goal.goalType = NPC_Goal.NPC_GoalType.MoveTo;
                goal.destination = GenerateRandomDestination(npc_data);
                break;

            case 2: // talk to
                string target = TryGenerateRandomDialogTarget(npc, npc_data, player);
                if (target == null)
                {
                    // no one is available to talk to, idle instead
                }
                else
                {
                    goal.goalType = NPC_Goal.NPC_GoalType.TalkTo;
                    goal.targetCharacter = target;
                    goal.dialog = GenerateRandomDialog(npc_data.randomDialog);
                }
                break;

            case 3: // custom action
                goal.goalType = NPC_Goal.NPC_GoalType.PerformCustomAction;
                break;

        }

        return goal;
    }


    private static string TryGenerateRandomDialogTarget(NPC npc, NPC_Data npc_data, Player player)
    {
        List<string> characterNames = npc_data.GetAllCharacterNames();

        Karyo_GameCore.Shuffle<string>(characterNames); // shuffles the list in-place

        foreach (string charName in characterNames)
            if (NPC.IsValidDialogTargetForNPC(npc, charName, npc_data, player, false))
                return charName;

        return null;
    }


    private static string GenerateRandomDialog(string[] randomDialogOptions)
    {
        int rand;

        if (randomDialogOptions?.Length > 0)
        {
            rand = Random.Range(0, randomDialogOptions.Length);
            return (randomDialogOptions[rand]);
        }


        // if no random dialog strings were defined, return some stock hardcoded random dialog

        rand = UnityEngine.Random.Range(0, 5);

        switch (rand)
        {
            case 0:
                return new string("We should consider the ethical implications of our work here in the Village.");

            case 1:
                return new string("We are all very much involved with the research in the Fringe.");

            case 2:
                return new string("It's a pleasure to work with this group of scientists and creatives.");

            case 3:
                return new string("Let's brainstorm ways we can improve the world with the exciting genetics discovered in the Fringe!");

            case 4:
                return new string("The Fringe is going to change everything. Let's hope we're up for the task.");
        }

        return null;

    }

    
    private static NPC_Location GenerateRandomDestination(NPC_Data npc_data)
    {
        if (npc_data.allLocations?.Length == 0)
            return null;

        int rand = Random.Range(0, npc_data.allLocations.Length);
        return npc_data.allLocations[rand];
    }


}
