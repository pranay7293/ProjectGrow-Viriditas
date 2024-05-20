using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Sirenix.OdinInspector;
using UnityEngine;


// should be a scriptable object; this is a way to edit data in the editor.

public class NPC_Data : MonoBehaviour
{
    // these are the same for all NPCs
    [Title("Generic prompt part 1", bold: false)]
    [HideLabel]
    [MultiLineProperty(8)]
    public string genericPrompt_part1;

    // this is the generic request prompt part 5
    [Title("Request prompt part 5", bold: false)]
    [HideLabel]
    [MultiLineProperty(8)]
    public string requestPrompt_part5;

    // this is the request prompt part 5 that includes an option for the NPC to give the player an objective.
    // note the actual list of objectives is appended to this dynamically in NPC_openAI.GeneratePromptPart5()
    [Title("Objectives version of Request prompt part 5", bold: false)]
    [HideLabel]
    [MultiLineProperty(8)]
    public string requestPrompt_part5_w_objectives;

    // this is the request prompt part 5 version that ONLY requests dialog options to offer the player
    [Title("Dialog options request prompt part 5", bold: false)]
    [HideLabel]
    [MultiLineProperty(8)]
    public string requestPrompt_part5_dialogOptions;

    public float objectiveInclusionPercentChance = 0.25f;  // random chance of an NPC to hear about its assigned player objectives and underlying situations in its prompts
    public float objectiveExclusionDuration = 20f;   // how many seconds at the beginning of the game during which the NPC will never think about player objectives or their underlying situations
    public int requiredConversationDepth = 5;  // how many exchanges that involved the player are required in the recent conversation before the AI is given the option to provide the player an objective (NOTE - player must also be nearby)

    public bool DEBUG_PauseOnUnknownResponse;

    public string[] randomDialog;  // intended to be a private set property but needs to be visible in editor

    public string[] offlinePlayerDialogOptions;  // if an NPC is offline when the player requests dialog options, they return these 3. must be exactly 3 options.

    public NPC[] allNPCs; // this is filled during Awake()
    public NPC_Location[] allLocations;  // this is filled during Awake()

    // TODO - a fancier version of this LOD solution would be better, this is silent, and mixing and matching random actions with openAI actions doesn't always show as well
    public float distanceToUseRandomActions = 50; // above this distance in meters away from the player, the NPC will not make calls to openAI and will use random actions instead

    public float duration_SecPerWord = 1f; // how long should dialog stay on screen?
    public float minDialogDuration = 10f;  // even short dialog stays on screen this long
    public float maxDialogDuration = 30f;  // even long dialog only stays on scren this long at most

    public float idleDurationMin = 60f;  // how long NPC idle actions last
    public float idleDurationMax = 120f;

    public float nearbyThreshold = 3f; // if a target is within this range, an NPC can eavesdrop on conversations, doesn't walk to talk to that character, etc..

    public static string startingLocationName = "starting location";  // the location you are in at start (if you don't start in a named location).  used to manage the extent to which NPCs hear about nearby NPC and eavesdrop at the beginning of the sim, so they don't bunch up.
    public static string unknownLocationName = "unknown"; // the location you are when you're not in one of the tracked locations


    private Player player;

    private void Awake()
    {
        allNPCs = GameObject.FindObjectsOfType<NPC>();
        allLocations = GameObject.FindObjectsOfType<NPC_Location>();

        player = Karyo_GameCore.Instance.player;

        if (genericPrompt_part1.Contains("Ash Trotman-Grant"))
            Debug.LogWarning("The string 'Ash Troman-Grant' was found in genericPrompt_part1. Use the string '[PLAYERNAME] instead.");
        if (requestPrompt_part5.Contains("Ash Trotman-Grant"))
            Debug.LogWarning("The string 'Ash Troman-Grant' was found in requestPrompt_part5. Use the string '[PLAYERNAME] instead.");
        if (requestPrompt_part5_w_objectives.Contains("Ash Trotman-Grant"))
            Debug.LogWarning("The string 'Ash Troman-Grant' was found in requestPrompt_part5_w_objectives. Use the string '[PLAYERNAME] instead.");
        if (requestPrompt_part5_dialogOptions.Contains("Ash Trotman-Grant"))
            Debug.LogWarning("The string 'Ash Troman-Grant' was found in requestPrompt_part5_dialogOptions. Use the string '[PLAYERNAME] instead.");

        if (genericPrompt_part1.Contains("[PLAYERNAME]"))
            genericPrompt_part1 = genericPrompt_part1.Replace("[PLAYERNAME]", player.playerName);
        if (requestPrompt_part5.Contains("[PLAYERNAME]"))
            requestPrompt_part5 = requestPrompt_part5.Replace("[PLAYERNAME]", player.playerName);
        if (requestPrompt_part5_w_objectives.Contains("[PLAYERNAME]"))
            requestPrompt_part5_w_objectives = requestPrompt_part5_w_objectives.Replace("[PLAYERNAME]", player.playerName);
        if (requestPrompt_part5_dialogOptions.Contains("[PLAYERNAME]"))
            requestPrompt_part5_dialogOptions = requestPrompt_part5_dialogOptions.Replace("[PLAYERNAME]", player.playerName);

        if (offlinePlayerDialogOptions.Length != 3)
            Debug.LogError("offlinePlayerDialogOptions must include exactly 3 elements.");
    }


    public bool IsOneOfTheNamedLocations(string location)
    {
        foreach (NPC_Location loc in allLocations)
            if (loc.locationName == location)
                return true;

        return false;
    }


    public NPC GetNPCByName(string npc_name)
    {
        foreach (NPC npc in allNPCs)
            if (npc_name == npc.name)
                return npc;

        Debug.LogError($"Can't find NPC with this name: {npc_name}");
        return null;
    }


    // returns a list that includes the player's name and all NPCs' names
    public List<string> GetAllCharacterNames()
    {
        List<string> characterNames = new List<string>();
        characterNames.Add(player.playerName);
        foreach (NPC npc in allNPCs)
            characterNames.Add(npc.name);

        return characterNames;
    }


    public UnityEngine.Vector3 GetPositionByCharacterName(string characterName)
    {
        if (characterName == player.playerName)
            return player.transform.position;

        NPC npc = GetNPCByName(characterName);
        if (npc == null)
            return new UnityEngine.Vector3();

        return npc.transform.position;
    }

}
