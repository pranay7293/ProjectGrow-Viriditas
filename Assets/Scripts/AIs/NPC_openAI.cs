using UnityEngine;
using System.Threading.Tasks;
using System.Text;

public class NPC_openAI : MonoBehaviour
{
    private NPC_Data npcData;
    private OpenAIService openAIService;

    public void Initialize(NPC_Data data)
    {
        npcData = data;
        openAIService = OpenAIService.Instance;
    }

    public async Task<string[]> GetGenerativeChoices()
    {
        string characterContext = GetCharacterContext();
        string prompt = $"{characterContext}\n\nGenerate 3 short, distinct action choices (max 10 words each) for this character based on their personality and the current game situation. Separate the choices with a newline character.";
        
        string response = await openAIService.GetChatCompletionAsync(prompt);
        if (string.IsNullOrEmpty(response))
        {
            Debug.LogWarning("Failed to get response from OpenAI API, using default choices");
            return new string[] { "Investigate the area", "Talk to a nearby character", "Work on the current objective" };
        }
        return response.Split('\n');
    }

    public async Task<string> GetResponse(string playerInput)
    {
        string characterContext = GetCharacterContext();
        string prompt = $"{characterContext}\n\nPlayer input: {playerInput}\n\nGenerate a short response (max 10 words) for this character based on their personality and the current game situation.";

        string response = await openAIService.GetChatCompletionAsync(prompt);
        return string.IsNullOrEmpty(response) ? "I'm not sure how to respond to that." : response;
    }

    private string GetCharacterContext()
    {
        StringBuilder context = new StringBuilder();
        context.AppendLine($"Character: {npcData.GetCharacterName()}");
        context.AppendLine($"Role: {npcData.GetCharacterRole()}");
        context.AppendLine($"Background: {npcData.GetCharacterBackground()}");
        context.AppendLine($"Personality: {npcData.GetCharacterPersonality()}");
        context.AppendLine("Recent memories:");
        foreach (string memory in npcData.GetMemories())
        {
            context.AppendLine($"- {memory}");
        }
        context.AppendLine($"Current challenge: {GameManager.Instance.GetCurrentChallenge()}");

        return context.ToString();
    }
}

// // TODO: Reimplement OpenAI functionality for advanced NPC behavior in future iterations

// using System.Collections.Generic;
// using UnityEngine;
// using System.Linq;
// using System.Numerics;
// using UnityEngine.Windows;
// using System;


// // class that handles creating prompts for NPCs to send to openAI, and to parse the replies we get from openAI

// public class NPC_openAI
// {

//     // this is the part of prompt part 2 that mentions situations pertaining to any objectives this NPC links to.
//     // this is meant to encourage NPCs to start conversations that will lead into giving the player the objective.
//     // this corresponds to prompt part 2b in this spec - https://docs.google.com/document/d/1CG6k3UbSfHauYwt1o5Cmz4vtxtzR7BG3fm7Gita_IxU/edit?usp=sharing
//     public static string GeneratePart2bObjectivesPrompt(NPC npc)
//     {
//         string objectives_prompt = new string("");

//         foreach (NPC_PlayerObjective objective in npc.playerObjectives)
//             if (!Karyo_GameCore.Instance.HasPlayerAlreadyFulfilledThisObjective(objective))
//                 objectives_prompt = objectives_prompt + objective.prompt_situationDescription + "\n";

//         return objectives_prompt;
//     }


//     // determines what userInput string the NPC should send to the openAI API.
//     // this corresponds to prompts part 3 to 5 in this spec - https://docs.google.com/document/d/1CG6k3UbSfHauYwt1o5Cmz4vtxtzR7BG3fm7Gita_IxU/edit?usp=sharing
//     public static string GenerateUserInputPrompt(NPC npc, NPC_Data npc_data, Player player, List<NPC.DialogEvent> dialogHistory, int max_history)
//     {
//         string prompt = new string("");

//         // generate parts 3 (current context), and part 4 (recent dialog) 
//         prompt = prompt + GeneratePromptPart3(npc, npc_data, player) + "\n";
//         prompt = prompt + GeneratePromptPart4(npc, dialogHistory, max_history) + "\n";

//         // add part 5 (prompt instructions), in this case the usual menu of options which may or may not include player objectives
//         prompt = prompt + GeneratePromptPart5(npc, npc_data);

//         return prompt;
//     }

//     // collects and returns the same prompt to send to openAI as GenerateUserInputPrompt(), above,
//     // except for swaps out a different part 5, the request, which asks open AI to provide 3 dialog options for the player.
//     public static string GenerateUserInputPromptForDialogOptions(NPC npc, NPC_Data npc_data, Player player, List<NPC.DialogEvent> dialogHistory, int max_history)
//     {
//         string prompt = new string("");

//         // generate parts 3 (current context), and part 4 (recent dialog) 
//         prompt = prompt + GeneratePromptPart3(npc, npc_data, player) + "\n";
//         prompt = prompt + GeneratePromptPart4(npc, dialogHistory, max_history) + "\n";

//         // add part 5 (prompt instructions), in this case the version that says "please provide dialog options for the player"
//         prompt = prompt + npc_data.requestPrompt_part5_dialogOptions; 

//         return prompt;
//     }


//     // current context and game state
//     // example return string =
//     // Currently you are in the Medical Bay. Dr. Ishan Kapoor is also here. Dr. Ishan Kapoor is working, conducting research about a medical condition observed among ocuppants of the Village which he is worried about.
//     private static string GeneratePromptPart3(NPC npc, NPC_Data npc_data, Player player)
//     {
//         string toReturn = new string("");

//         if (npc_data.IsOneOfTheNamedLocations(npc.currentLocation))
//             toReturn = toReturn + "Currently you are in the " + npc.currentLocation + ". ";
//         else
//             toReturn = toReturn + "Currently you are in the Village. ";

//         int neighbor_count = 0;
//         foreach (NPC other in npc_data.allNPCs)
//         {
//             if ((other != npc) && (npc.IsNearby(npc.transform.position, other.transform.position, npc.currentLocation, other.currentLocation))
//                 && (npc.currentLocation != NPC_Data.startingLocationName)) // TODO - this behavior is hardcoded (which is bad) and hardcodes the prompt not to mention nearby NPCs at the beginning of the sim, so they don't all just stand around and talk to each other. could be unhardcoded.
//             {
//                 neighbor_count++;

//                 if (npc_data.IsOneOfTheNamedLocations(npc.currentLocation))
//                     toReturn = toReturn + other.name + " is also here. ";
//                 else
//                     toReturn = toReturn + other.name + " is nearby. ";

//                 if (other.currentBehavior.behaviorType == NPC_Behavior.NPC_BehaviorType.PerformingCustomAction)
//                     toReturn = toReturn + other.customActionTextDescription;
//                 else if (other.currentBehavior.behaviorType == NPC_Behavior.NPC_BehaviorType.SpeakingTo)
//                     toReturn = toReturn + other.name + " is in a conversation with " + GetNameInPerspective(npc, other.currentGoal.targetCharacter, false) + ". ";
//                 else if (other.currentBehavior.behaviorType == NPC_Behavior.NPC_BehaviorType.BeingSpokenTo)
//                     toReturn = toReturn + other.name + " is in a conversation with " + GetNameInPerspective(npc, other.currentBehavior.beingSpokenToBy, false) + ". ";
//             }
//         }

//         if ((npc.IsNearby(npc.transform.position, player.transform.position, npc.currentLocation, player.currentLocation))
//              && (npc.currentLocation != NPC_Data.startingLocationName)) // TODO - this behavior is hardcoded (which is bad) and hardcodes the prompt not to mention nearby player at the beginning of the sim, similar to above. could be unhardcoded.
//         {
//             if (npc_data.IsOneOfTheNamedLocations(npc.currentLocation))
//                 toReturn = toReturn + player.playerName + " is here. ";
//             else
//                 toReturn = toReturn + player.playerName + " is nearby. ";

//             neighbor_count++;
//         }

//         if (neighbor_count == 0)
//         {
//             if (npc_data.IsOneOfTheNamedLocations(npc.currentLocation))
//                 toReturn = toReturn + "No one else is here right now. They must be somewhere else in the Village. ";
//             else
//                 toReturn = toReturn + "No one else is nearby right now, but they must be somewhere in the Village. ";
//         }

//         // TODO - if there have been recent story events put them here

//         return toReturn;
//     }


//     // recent dialog history
//     // note this will only return at most the max_history most recent events in the history
//     // example return string =
//     // Recently you said to Dr. Lena Morrow "What's up?"
//     // Dr. Lena Morrow said to you "Not much."
//     // Dr. Ishaan Kapor said to Dr. Lena Morrow "What's your research all about?"
//     private static string GeneratePromptPart4(NPC npc, List<NPC.DialogEvent> dialogHistory, int max_history)
//     {
//         string toReturn = new string("");

//         if ((dialogHistory == null) || (dialogHistory.Count == 0))
//             return toReturn;

//         toReturn = toReturn + "Recently ";

//         // create a new shortList, which is the most recent max_history events, according to each event's timeStamp
//         // step 1 - Sort the list in descending order based on timeStamp
//         List<NPC.DialogEvent> sortedList = dialogHistory.OrderByDescending(item => item.timeStamp).ToList();
//         // step 2 -Take the first maxDialogHistoryToUseInPrompts entries
//         List<NPC.DialogEvent> shortList = sortedList.Take(max_history).ToList();
//         // step 3 - reverse the sort order so smallest timeStamp comes first
//         shortList.Reverse();

//         char doubleQuoteChar = '"';
//         string doubleQuote = doubleQuoteChar.ToString();

//         int entries = 0;

//         foreach (NPC.DialogEvent dialogEvent in shortList)
//             if (IsRecent(dialogEvent))
//             {
//                 toReturn = toReturn + GetNameInPerspective(npc, dialogEvent.speaker, (entries > 0)) + " said to " + GetNameInPerspective(npc, dialogEvent.listener, false) + " " + doubleQuote + dialogEvent.dialog + doubleQuote + " ";
//                 entries++;
//             }

//         return toReturn;
//     }


//     // this is prompt part 5.  normally it's just a copy/paste of npc_data.requestPrompt_part5_dialogOptions
//     // if the conditions are right, then it also includes the option for the NPC to give the player an objective.
//     private static string GeneratePromptPart5(NPC npc, NPC_Data npc_data)
//     {
//         bool include_objectives_in_prompt = npc.UsePrompt5bObjectives();

//         // verion 5, no objectives
//         if (!include_objectives_in_prompt)
//             return npc_data.requestPrompt_part5;


//         // below here = 5b, objectives version 

//         string toReturn = new string("");

//         toReturn = toReturn + npc_data.requestPrompt_part5_w_objectives;

//         foreach (NPC_PlayerObjective objective in npc.playerObjectives)
//             if (!Karyo_GameCore.Instance.HasPlayerAlreadyFulfilledThisObjective(objective))  // NPCs should not give the player an objective they have fulfilled previously
//                 toReturn = toReturn + "\n   " + objective.internalName + " - " + objective.prompt_objectiveDescription;

//         return toReturn;
//     }



//     // helps NPCs not talk about themselves in the third person.
//     // pass in a name and the NPC will return a string which uses it correctly in a prompt, for example "You" instead of this NPC's name.
//     private static string GetNameInPerspective(NPC npc, string name, bool firstWordInSentence)
//     {
//         if (name != npc.name)
//             return name;

//         if (firstWordInSentence)
//             return "You";
//         else
//             return "you";
//     }


//     private static bool IsRecent(NPC.DialogEvent dialogEvent)
//     {
//         // TODO - compare timeStamp against Time.time and return false if it's reasonably long ago
//         return true;
//     }





//     // parse the text response from openAI and return a corresponding NPC_Goal 
//     public static NPC_Goal ProcessTextFromAI(string textFromAI, NPC requestingNPC, NPC_Data npc_data, Player player)
//     {
//         NPC_Goal newGoal = null;

//         // these are variables so they can be changed more easily if the prompt definition changes
//         string string_talk = new string("TALK");
//         string string_go = new string("GO");
//         string string_wait = new string("WAIT");
//         string string_work = new string("WORK");
//         string string_objective = new string("OBJECTIVE");
//         string string_other = new string("OTHER");

//         // support more than just commas, since sometimes GPT uses colons instead.
//         char[] delimiters = { ',', ':' };

//         int talkIndex = textFromAI.IndexOf(string_talk);
//         int goIndex = textFromAI.IndexOf(string_go);
//         int workIndex = textFromAI.IndexOf(string_work);
//         int waitIndex = textFromAI.IndexOf(string_wait);
//         int objectiveIndex = textFromAI.IndexOf(string_objective);
//         int otherIndex = textFromAI.IndexOf(string_other);

//         // note that in cases where openAI returns multiple actions (eg - GO followed by WAIT),
//         // they are processed in the following priority order and only the first is processed

//         //
//         // TALK
//         //
//         if (talkIndex != -1)
//         {
//             // parse the input for both who to talk to and what dialog to say

//             int delimiterIndex = textFromAI.IndexOfAny(delimiters, talkIndex);  // where in the string is the comma or other delimiter

//             if (delimiterIndex != -1)
//             {
//                 // Extract the substring between "TALK" and the comma as the dialogTarget
//                 int end_of_talk = talkIndex + string_talk.Length;
//                 string dialogTarget = textFromAI.Substring(end_of_talk, delimiterIndex - end_of_talk);
//                 dialogTarget = dialogTarget.Trim();

//                 // clean up the data in cases where openAI says stuff like TALK to Ash Trotman-Grant
//                 if (dialogTarget.Contains(player.playerName))
//                     dialogTarget = player.playerName;
//                 foreach (NPC npc in npc_data.allNPCs)
//                     if (dialogTarget.Contains(npc.name))
//                         dialogTarget = npc.name;

//                 // Extact the remainder of the string as the dialog  
//                 string dialog = textFromAI.Substring(delimiterIndex + 1);
//                 dialog = dialog.Trim();

//                 // TODO - the dialog is usually but not always bound within double-quotes. could trim them out here to make it more consistent and readable.
//                 // TODO - also rarely chatGPT adds spurious text after the dialog.  could detect and remove that.


//                 // create a new goal from the parsed data
//                 NPC target = null;
//                 if (dialogTarget != player.playerName)
//                     target = npc_data.GetNPCByName(dialogTarget);
//                 if ((target == null) && (dialogTarget != player.playerName))
//                 {
//                     Debug.LogWarning($"openAI on _{requestingNPC.name} seems to want to talk to a character we can't identify: '{dialogTarget}'");
//                 }
//                 else
//                 {
//                     newGoal = new NPC_Goal(NPC_Goal.NPC_GoalType.TalkTo);
//                     newGoal.targetCharacter = dialogTarget;
//                     newGoal.dialog = dialog;
//                 }
//             }
//             else
//             {
//                 // Handle the case where there is no comma after "TALK"
//                 Debug.LogWarning($"openAI on _{requestingNPC.name} seems to have formatted the output incorrectly, since we can't find a comma after TALK");
//             }
//         }

//         //
//         // GO
//         //
//         else if (goIndex != -1)
//         {
//             // parse the input for where to go

//             // Extract the substring after "GO"
//             int end_of_go = goIndex + string_go.Length;
//             string destination = textFromAI.Substring(end_of_go);
//             destination = destination.Trim();

//             bool destinationFound = false;
//             foreach (NPC_Location loc in npc_data.allLocations)
//                 if (destination.Contains(loc.locationName))
//                 {
//                     newGoal = new NPC_Goal(NPC_Goal.NPC_GoalType.MoveTo);
//                     newGoal.destination = loc;
//                     destinationFound = true;
//                     break;
//                 }

//             if (!destinationFound)
//                 Debug.LogWarning($"openAI on NPC _{requestingNPC.name} seems to want to go to a destination we can't identify: {destination}");

//         }


//         //
//         // OBJECTIVE
//         //
//         else if (objectiveIndex != -1)
//         {
//             // parse the input for both which objective to give and what dialog to say when giving the objective

//             int delimiterIndex = textFromAI.IndexOfAny(delimiters, objectiveIndex);  // where in the string is the comma or other delimiter

//             if (delimiterIndex != -1)
//             {
//                 // Extract the substring between "OBJECTIVE" and the comma as the Objective Name
//                 int end_of_objective = objectiveIndex + string_objective.Length;
//                 string nameOfObjective = textFromAI.Substring(end_of_objective, delimiterIndex - end_of_objective);
//                 nameOfObjective = nameOfObjective.Trim();

//                 // Extact the remainder of the string as the dialog  
//                 string dialog = textFromAI.Substring(delimiterIndex + 1);
//                 dialog = dialog.Trim();

//                 // TODO - the dialog is usually but not always bound within double-quotes. could trim them out here to make it more consistent and readable.
//                 // TODO - also rarely chatGPT adds spurious text after the dialog.  could detect and remove that.

//                 // create a new goal from the parsed data

//                 newGoal = new NPC_Goal(NPC_Goal.NPC_GoalType.GivePlayerObjective);
//                 newGoal.dialog = dialog;
//                 newGoal.targetCharacter = player.playerName;

//                 // find this objective in the NPC's linked objectives, looking it up by internalName
//                 newGoal.playerObjective = null;
//                 foreach (NPC_PlayerObjective o in requestingNPC.playerObjectives)
//                     if (o.internalName == nameOfObjective)
//                         newGoal.playerObjective = o;

//                 if (newGoal.playerObjective == null)
//                     Debug.LogWarning($"openAI on NPC _{requestingNPC.name} wants to give player an objective we can't identify: {nameOfObjective}");

//             }

//             else
//             {
//                 // Handle the case where there is no comma after "OBJECTIVE"
//                 Debug.LogWarning($"openAI on _{requestingNPC.name} seems to have formatted the output incorrectly, since we can't find a comma after OBJECTIVE");
//             }

//         }

//         // WORK
//         else if (workIndex != -1)
//         {
//             newGoal = new NPC_Goal(NPC_Goal.NPC_GoalType.PerformCustomAction);
//         }

//         // OTHER
//         else if (otherIndex != -1)
//         {
//             // parse the input for what this other action is

//             // Extract the substring after "OTHER"
//             int end_of_other = otherIndex + string_other.Length;
//             string otherActivity = textFromAI.Substring(end_of_other);
//             otherActivity = otherActivity.Trim();

//             Debug.LogWarning($"openAI on _{requestingNPC.name} chose to invent an OTHER action for {requestingNPC.name} which is great but which we don't currently support. It says it wants to do: {otherActivity}");
//             // TODO - one idea to support other activities is to use the NPC's custom action label to display this action and inform other NPCs nearby this NPC that the NPC is engaged in the behaviour (mostly just pass in the string)

//             if (npc_data.DEBUG_PauseOnUnknownResponse)
//             {
//                 string body = new string("openAI for NPC ");
//                 body = body + requestingNPC.name + " chose the following OTHER action, which we don't currently support: " + textFromAI;
//                 Karyo_GameCore.Instance.uiManager.LaunchGenericDialogWindow("OTHER NPC action isn't supported", body, true);
//             }
//         }

//         // WAIT
//         else if (waitIndex != -1)
//         {
//             newGoal = new NPC_Goal(NPC_Goal.NPC_GoalType.Nothing);
//         }

//         // UNKNOWN
//         else
//         {
//             if (npc_data.DEBUG_PauseOnUnknownResponse)
//             { 
//                 string body = new string("openAI for NPC ");
//                 body = body + requestingNPC.name + " provided a response we cannot parse for instructions: " + textFromAI;
//                 Karyo_GameCore.Instance.uiManager.LaunchGenericDialogWindow("Unknown AI response", body, true);
//             }
//         }

//         // if we fallthrough to here without hitting any above cases, then the current goal is null
//         return newGoal;
//     }



//     // parse the text response from openAI and return an array of exactly 3 strings which are the dialog options the player will use
//     // or return a null array if the text could not be parsed.
//     public static string[] ProcessTextFromAIForDialogOptions(string textFromAI)
//     {
//         string[] dialog_options = new string[3];

//         // TODO - handle cases where the AI reponds with stuff like "OPTION 1" instead of "OPTION1"

//         string string_option1 = new string("OPTION1 - ");
//         string string_option2 = new string("OPTION2 - ");
//         string string_option3 = new string("OPTION3 - ");

//         int option1_index = textFromAI.IndexOf(string_option1);
//         int option1_start = option1_index + string_option1.Length;
//         int option2_index = textFromAI.IndexOf(string_option2);
//         int option2_start = option2_index + string_option2.Length;
//         int option3_index = textFromAI.IndexOf(string_option3);
//         int option3_start = option3_index + string_option3.Length;

//         dialog_options[0] = textFromAI.Substring(option1_start, (option2_index - 1) - option1_start).Trim();
//         dialog_options[1] = textFromAI.Substring(option2_start, (option3_index - 1) - option2_start).Trim();
//         dialog_options[2] = textFromAI.Substring(option3_start, (textFromAI.Length - 1) - option3_start).Trim();

//         return dialog_options;
//     }

// }