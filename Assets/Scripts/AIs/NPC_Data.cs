using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[System.Serializable]
public class NPC_Data : MonoBehaviour
{
    [System.Serializable]
    public class CharacterData
    {
        public string name;
        public GameObject spawnLocation;
        [MultiLineProperty(4)]
        public string specificCharacterPrompt_part2;
        [MultiLineProperty(4)]
        public string customActionTextDescription;
        public NPC_PlayerObjective[] playerObjectives;
    }

    [Title("Character Data", bold: true)]
    public CharacterData[] characters;

    [Title("Generic prompt part 1", bold: false)]
    [HideLabel]
    [MultiLineProperty(8)]
    public string genericPrompt_part1;

    [Title("Request prompt part 5", bold: false)]
    [HideLabel]
    [MultiLineProperty(8)]
    public string requestPrompt_part5;

    [Title("Objectives version of Request prompt part 5", bold: false)]
    [HideLabel]
    [MultiLineProperty(8)]
    public string requestPrompt_part5_w_objectives;

    [Title("Dialog options request prompt part 5", bold: false)]
    [HideLabel]
    [MultiLineProperty(8)]
    public string requestPrompt_part5_dialogOptions;

    public float objectiveInclusionPercentChance = 0.25f;
    public float objectiveExclusionDuration = 20f;
    public int requiredConversationDepth = 5;

    public bool DEBUG_PauseOnUnknownResponse;

    public string[] randomDialog;

    public string[] offlinePlayerDialogOptions;

    [HideInInspector] public NPC[] allNPCs;
    [HideInInspector] public NPC_Location[] allLocations;

    public float distanceToUseRandomActions = 50;

    public float duration_SecPerWord = 1f;
    public float minDialogDuration = 10f;
    public float maxDialogDuration = 30f;

    public float idleDurationMin = 60f;
    public float idleDurationMax = 120f;

    public float nearbyThreshold = 3f;

    public static string startingLocationName = "starting location";
    public static string unknownLocationName = "unknown";

    private void Awake()
    {
        allNPCs = FindObjectsOfType<NPC>();
        allLocations = FindObjectsOfType<NPC_Location>();

        ReplacePlayerNameInPrompts();

        if (offlinePlayerDialogOptions.Length != 3)
            Debug.LogError("offlinePlayerDialogOptions must include exactly 3 elements.");
    }

    private void ReplacePlayerNameInPrompts()
    {
        string playerName = Karyo_GameCore.Instance.GetLocalPlayerCharacter()?.name ?? "[PLAYERNAME]";

        genericPrompt_part1 = genericPrompt_part1.Replace("[PLAYERNAME]", playerName);
        requestPrompt_part5 = requestPrompt_part5.Replace("[PLAYERNAME]", playerName);
        requestPrompt_part5_w_objectives = requestPrompt_part5_w_objectives.Replace("[PLAYERNAME]", playerName);
        requestPrompt_part5_dialogOptions = requestPrompt_part5_dialogOptions.Replace("[PLAYERNAME]", playerName);

        foreach (var character in characters)
        {
            character.specificCharacterPrompt_part2 = character.specificCharacterPrompt_part2.Replace("[PLAYERNAME]", playerName);
        }
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

    public List<string> GetAllCharacterNames()
    {
        List<string> names = new List<string>();
        foreach (var character in characters)
        {
            names.Add(character.name);
        }
        return names;
    }

    public Vector3 GetPositionByCharacterName(string characterName)
    {
        NPC npc = GetNPCByName(characterName);
        if (npc != null)
            return npc.transform.position;

        UniversalCharacterController character = FindObjectOfType<UniversalCharacterController>();
        if (character != null && character.name == characterName)
            return character.transform.position;

        return Vector3.zero;
    }

    public CharacterData GetCharacterData(string characterName)
    {
        foreach (var character in characters)
        {
            if (character.name == characterName)
            {
                return character;
            }
        }
        Debug.LogError($"Character data not found for: {characterName}");
        return null;
    }
}