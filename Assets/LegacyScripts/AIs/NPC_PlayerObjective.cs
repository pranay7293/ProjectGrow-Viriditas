using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;


// this is a class that helps NPCs decide when to give players objectives.  right now these objectives are not actually tracked
// (and when that time comes, probably player objectives shouldn't be as tightly coupled with NPCs and should live elsewhere)

public class NPC_PlayerObjective : MonoBehaviour
{
    public string internalName; // the name used for AI prompts 
    public string prompt_situationDescription;  // for an NPC linked to this objective, this text is always included at the end of their prompt part 1 in order to encourage them to start conversations relevant to the objective
    public string prompt_objectiveDescription;  // for an NPC linked to this objecigve, this text is included in prompt part 5 as part of a menu
    public string title; // the player-facing name, the AI never sees this
    public string[] subtasks;  // this is the player-facing list of tasks, the AI never sees this and it is currently just for show (not tracked by game mechanics)

    private bool alreadyTested = false;

    public void Validate(Player player)
    {
        if (alreadyTested)
            return;

        if (prompt_situationDescription.Contains("Ash Trotman-Grant"))
            Debug.LogWarning($"The string 'Ash Troman-Grant' was found in {this.name}. Use the string '[PLAYERNAME] instead.");
        if (prompt_objectiveDescription.Contains("Ash Trotman-Grant"))
            Debug.LogWarning($"The string 'Ash Troman-Grant' was found in {this.name}. Use the string '[PLAYERNAME] instead.");

        if (prompt_situationDescription.Contains("[PLAYERNAME]"))
            prompt_situationDescription = prompt_situationDescription.Replace("[PLAYERNAME]", player.playerName);
        if (prompt_objectiveDescription.Contains("[PLAYERNAME]"))
            prompt_objectiveDescription = prompt_objectiveDescription.Replace("[PLAYERNAME]", player.playerName);

        alreadyTested = true;
    }


    // called by UI manager when the player presses O to view their objectives
    public string GetObjectiveAsText()
    {
        string toReturn = new string("");

        toReturn = toReturn + title + "\n\n";

        foreach (string subtask in subtasks)
            toReturn = toReturn + "* " + subtask + "\n";

        return toReturn;
    }

}
