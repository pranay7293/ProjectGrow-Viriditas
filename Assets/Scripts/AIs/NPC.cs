using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using System;
using Photon.Pun;

public class NPC : Character
{
    public new string name;
    [SerializeField] private Color color;

    [Header("--AI prompts and settings--")]
    [Title("Specific character prompt part 2", bold: false)]
    [HideLabel]
    [MultiLineProperty(12)]
    [SerializeField] private string specificCharacterPrompt_part2;

    [Title("Description of this NPC's Custom Action for nearby NPCs", bold: false)]
    [HideLabel]
    [MultiLineProperty(4)]
    [SerializeField] public string customActionTextDescription;

    [SerializeField] public NPC_PlayerObjective[] playerObjectives;

    [Header("--Custom action--")]
    [SerializeField] private NPC_Location customActionLocation;
    [SerializeField] private float durationPerformCustomAction = 120f;

    public enum OpenAIModel
    {
        RandomOffline,
        GPT_3_5,
        GPT_4,
    }
    [SerializeField] private OpenAIModel openAIModel = OpenAIModel.RandomOffline;
    private bool useOpenAI => ((openAIModel != OpenAIModel.RandomOffline) && (Vector3.Distance(transform.position, player.transform.position) < npc_data.distanceToUseRandomActions));
    [SerializeField] private int maxDialogHistoryToUseInPrompts = 3;

    [Header("--Debugging--")]
    public bool DEBUG_NPCActions;
    public bool DEBUG_AIPrompts;
    [SerializeField] private bool DEBUG_overrideRandomOfflineGoal;
    [SerializeField] private NPC_Goal DEBUG_offlineGoal;

    [Header("--References--")]
    [SerializeField] private NPC_Data npc_data;
    [SerializeField] private Canvas dialogCanvas;
    [SerializeField] public TextMeshPro nameLabel;
    [SerializeField] private TextMeshProUGUI dialogText;
    [SerializeField] private GameObject selectionIndicator;
    [SerializeField] private GameObject specialActionIndicator;
    [SerializeField] private GameObject awaitingInstructionsIndicator;
    [SerializeField] public Transform standHereToTalkToMe;

    [Header("--Current state--")]
    [ReadOnly, SerializeField] public NPC_Goal currentGoal;
    [ReadOnly, SerializeField] public NPC_Behavior currentBehavior;
    public bool IsAwaitingInstructions => (currentBehavior.behaviorType == NPC_Behavior.NPC_BehaviorType.AwaitingInstructions);
    [ReadOnly, SerializeField] public string currentLocation;
    [ReadOnly, SerializeField] private float timeBehaviorWillConclude;
    [ReadOnly, SerializeField] private float timeDialogUiShouldClose;

    private BM_Walk myBMWalk;
    private Player player;

    public class DialogEvent
    {
        public string speaker;
        public string listener;
        public string dialog;
        public string location;
        public float timeStamp;

        public DialogEvent(string speaker, string listener, string dialog, string location)
        {
            this.speaker = speaker;
            this.listener = listener;
            this.dialog = dialog;
            this.location = location;
            timeStamp = Time.time;
        }
    }
    private List<DialogEvent> dialogHistory;

    public void InitializeNPCData(object[] npcData)
{

    npc_data.genericPrompt_part1 = (string)npcData[0];
    npc_data.requestPrompt_part5 = (string)npcData[1];
    npc_data.requestPrompt_part5_w_objectives = (string)npcData[2];
    npc_data.requestPrompt_part5_dialogOptions = (string)npcData[3];
    npc_data.objectiveInclusionPercentChance = (float)npcData[4];
    npc_data.objectiveExclusionDuration = (float)npcData[5];
    npc_data.requiredConversationDepth = (int)npcData[6];
    npc_data.distanceToUseRandomActions = (float)npcData[7];
    npc_data.duration_SecPerWord = (float)npcData[8];
    npc_data.minDialogDuration = (float)npcData[9];
    npc_data.maxDialogDuration = (float)npcData[10];
    npc_data.idleDurationMin = (float)npcData[11];
    npc_data.idleDurationMax = (float)npcData[12];
    npc_data.nearbyThreshold = (float)npcData[13];
}
    protected override void Awake()
{
    base.Awake();

    if (photonView.InstantiationData != null && photonView.InstantiationData.Length > 0)
    {
        name = (string)photonView.InstantiationData[0];
        bool isPlayerControlled = (bool)photonView.InstantiationData[1];
        InitializeNPCData((object[])photonView.InstantiationData[2]);
    }

        if (PhotonNetwork.IsMessageQueueRunning)
        {
            player = FindObjectOfType<Player>();
        }

        if (nameLabel != null)
            nameLabel.text = name;

        MeshRenderer mr = GetComponentInChildren<MeshRenderer>();
        if (mr != null)
            mr.material.color = color;

        if (npc_data == null)
            Debug.LogError($"npc_data not assigned in NPC {this.name}, {name}");

        myBMWalk = GetComponent<BM_Walk>();
        if (myBMWalk == null)
            Debug.LogError($"NPC does not have BM_Walk component {this.name}, {name}");

        if (dialogCanvas == null)
            Debug.LogError($"dialogCanvas not defined in {gameObject.name}, {name}");
        if (dialogText == null)
            Debug.LogError($"dialogText not defined in {gameObject.name}, {name}");
        if (selectionIndicator == null)
            Debug.LogError($"selectionIndicator not defined in {gameObject.name}, {name}");
        if (specialActionIndicator == null)
            Debug.LogError($"customActionIndicator not defined in {gameObject.name}, {name}");
        if (awaitingInstructionsIndicator == null)
            Debug.LogError($"awaitingInstructionsIndicator not defined in {gameObject.name}, {name}");

        if (specificCharacterPrompt_part2.Contains("Ash Trotman-Grant"))
            Debug.LogWarning($"The string 'Ash Troman-Grant' was found in specificCharacterPrompt_part2 for NPC {this.name}. Use the string '[PLAYERNAME] instead.");
        if (specificCharacterPrompt_part2.Contains("[PLAYERNAME]"))
            specificCharacterPrompt_part2 = specificCharacterPrompt_part2.Replace("[PLAYERNAME]", player.playerName);

        foreach (NPC_PlayerObjective objective in playerObjectives)
            objective.Validate(player);

        dialogCanvas.gameObject.SetActive(false);
        specialActionIndicator.SetActive(false);
        awaitingInstructionsIndicator.SetActive(false);

        currentGoal = new NPC_Goal(NPC_Goal.NPC_GoalType.Nothing);

        currentBehavior = NPC_Behavior.Idle();
        timeBehaviorWillConclude = UnityEngine.Random.Range(2f, 15f);

        currentLocation = NPC_Data.startingLocationName;
        dialogHistory = new List<DialogEvent>();
    }

    private void Update()
    {
        if (!photonView.IsMine) return;

        if (dialogCanvas.gameObject.activeInHierarchy)
        {
            if (Time.time > timeDialogUiShouldClose)
                dialogCanvas.gameObject.SetActive(false);
        }

        if ((currentBehavior.behaviorType == NPC_Behavior.NPC_BehaviorType.SpeakingTo) && (!DialogTargetIsNearby()))
        {
            if (DEBUG_NPCActions)
                Debug.Log($"NPC _{name} breaking off Speaking behavior with _{currentGoal.targetCharacter} because they moved away.");

            ConcludeBehavior_SpeakingTo();
            return;
        }

        if (Time.time > timeBehaviorWillConclude)
        {
            if (currentBehavior.behaviorType == NPC_Behavior.NPC_BehaviorType.SpeakingTo)
                ConcludeBehavior_SpeakingTo();
            else if (currentBehavior.behaviorType == NPC_Behavior.NPC_BehaviorType.PerformingCustomAction)
                ConcludeBehavior_CustomAction();
            else if (currentBehavior.behaviorType == NPC_Behavior.NPC_BehaviorType.Idle)
                ConcludeBehavior_Idle();
        }
    }

    private async void UpdateCurrentGoal()
    {
        currentGoal = await GetNewGoal();

        if (currentGoal == null)
            currentGoal = new NPC_Goal(NPC_Goal.NPC_GoalType.Nothing);

        PursueGoal(currentGoal);
    }

    private void PursueGoal(NPC_Goal goal)
    {
        if (goal.goalType == NPC_Goal.NPC_GoalType.Nothing)
            InitiateBehavior_Idle();
        else if (goal.goalType == NPC_Goal.NPC_GoalType.MoveTo)
            MoveToTransform(goal.destination.GetAvailableStandPosition(), goal.destination.locationName, false);
        else if (goal.goalType == NPC_Goal.NPC_GoalType.TalkTo)
            InitiateBehavior_Dialog(goal.targetCharacter);
        else if (goal.goalType == NPC_Goal.NPC_GoalType.PerformCustomAction)
            InitiateBehavior_CustomAction();
        else if (goal.goalType == NPC_Goal.NPC_GoalType.GivePlayerObjective)
        {
            Karyo_GameCore.Instance.CreateObjective(goal.playerObjective);
            InitiateBehavior_Dialog(player.playerName);
        }
    }

    private async Task<NPC_Goal> GetNewGoal()
    {
        if (useOpenAI)
        {
            NPC_Goal goal = await GetNewGoalFromOpenAI();
            return goal;
        }

        if (DEBUG_AIPrompts)
        {
            string userInputPrompt = NPC_openAI.GenerateUserInputPrompt(this, npc_data, player, dialogHistory, maxDialogHistoryToUseInPrompts);
            Debug.Log($"PROMPT _{name} generated this userInputPrompt (not sending to openAI because RandomOffline): {userInputPrompt}");
        }

        return NPC_RandomGoal.GenerateRandomGoal(this, npc_data, player);
    }

    private async Task<NPC_Goal> GetNewGoalFromOpenAI()
    {
        string baseOpenAIPrompt = npc_data.genericPrompt_part1 + "\n" + specificCharacterPrompt_part2 + "\n\n";

        if (IncludeObjectivesPromptPart2b())
            baseOpenAIPrompt = baseOpenAIPrompt + NPC_openAI.GeneratePart2bObjectivesPrompt(this) + "\n\n";

        string userInputPrompt = NPC_openAI.GenerateUserInputPrompt(this, npc_data, player, dialogHistory, maxDialogHistoryToUseInPrompts);

        if (DEBUG_AIPrompts)
            Debug.Log($"PROMPT _{name} sending to openAI: {baseOpenAIPrompt}\n   -----\n{userInputPrompt}");
 
        currentBehavior = NPC_Behavior.AwaitingInstructions();
        awaitingInstructionsIndicator.SetActive(true);

        string response = await Karyo_GameCore.Instance.openAiService.GetChatCompletionAsync(openAIModel == OpenAIModel.GPT_3_5 ? OpenAI.Models.Model.GPT3_5_Turbo : OpenAI.Models.Model.GPT4,
                baseOpenAIPrompt, userInputPrompt);

        if (DEBUG_AIPrompts || DEBUG_NPCActions)
            Debug.Log($"PROMPT _{name} received from openAI: {response}");

        awaitingInstructionsIndicator.SetActive(false);

        NPC_Goal parsed_goal = NPC_openAI.ProcessTextFromAI(response, this, npc_data, player);

        if (DEBUG_AIPrompts || DEBUG_NPCActions)
        {
            if (parsed_goal != null)
                Debug.Log($"PROMPT: _{name} parsed AI response, GOAL = {parsed_goal.ToString()}");
            else
                Debug.Log($"PROMPT: _{name} could not parse AI response for a valid goal.");
        }

        return parsed_goal;
    }

    private bool IncludeObjectivesPromptPart2b()
    {
        if (Time.time < npc_data.objectiveExclusionDuration)
            return false;

        if (Karyo_GameCore.Instance.DoesPlayerCurrentlyHaveAnObjective(PhotonNetwork.LocalPlayer.ActorNumber))
            return false;

        float rand = UnityEngine.Random.Range(0f, 1f);
        if (rand < npc_data.objectiveInclusionPercentChance)
            return true;
        else
            return false;
    }

    public bool UsePrompt5bObjectives()
    {
        if (Karyo_GameCore.Instance.DoesPlayerCurrentlyHaveAnObjective(PhotonNetwork.LocalPlayer.ActorNumber))
            return false;

        bool has_at_least_one_unfulfilled_objective = false;
        foreach (NPC_PlayerObjective objective in playerObjectives)
            if (!Karyo_GameCore.Instance.HasPlayerAlreadyFulfilledThisObjective(objective))
                has_at_least_one_unfulfilled_objective = true;
        if (!has_at_least_one_unfulfilled_objective)
            return false;

        return (HasBeenTalkingToPlayer() && IsNearby(transform.position, player.transform.position, currentLocation, player.currentLocation));
    }

    public bool HasBeenTalkingToPlayer()
    {
        int player_exchanges = 0;

        foreach (DialogEvent d_event in dialogHistory)
            if ((d_event.speaker == player.playerName) || (d_event.listener == player.playerName))
                player_exchanges++;

        return (player_exchanges >= npc_data.requiredConversationDepth);
    }

    private void InitiateBehavior_Idle()
    {
        if (DEBUG_NPCActions)
            Debug.Log($"NPC _{name} entering Idle behavior.");

        currentGoal = new NPC_Goal(NPC_Goal.NPC_GoalType.Nothing);
        currentBehavior = NPC_Behavior.Idle();
        timeBehaviorWillConclude = Time.time + UnityEngine.Random.Range(npc_data.idleDurationMin, npc_data.idleDurationMax);
    }

    private void InitiateBehavior_CustomAction()
    {
        MoveToTransform(customActionLocation.interactPoint.position, customActionLocation.locationName, true);
    }

    private void InitiateBehavior_Dialog(string targetCharacter)
    {
        Transform targetTransform;
        UnityEngine.Vector3 destination;

        if (!IsValidDialogTargetForNPC(this, targetCharacter, npc_data, player, true))
        {
            if ((DEBUG_AIPrompts) || (DEBUG_NPCActions))  
                Debug.Log($"NPC _{name} can't talk to _{targetCharacter} because they are not a valid dialog target right now.");

            InitiateBehavior_Idle();
            return;
        }

        if (targetCharacter == player.playerName)
        {
            targetTransform = player.transform;
            destination = new UnityEngine.Vector3(targetTransform.position.x, targetTransform.position.y, targetTransform.position.z - 1.3f);

            MoveToTransform(destination, player.playerName);
            return;
        }

        NPC target = npc_data.GetNPCByName(targetCharacter);
        target.TargetedForSpeaking(this.name);
        destination = new UnityEngine.Vector3(target.standHereToTalkToMe.position.x, target.standHereToTalkToMe.position.y, target.standHereToTalkToMe.position.z);

        MoveToTransform(destination, targetCharacter);
    }

    private void ConcludeBehavior_SpeakingTo()
    {
        if (DEBUG_NPCActions)
            Debug.Log($"NPC _{name} concluding speaking to _{currentGoal.targetCharacter}.");

        dialogCanvas.gameObject.SetActive(false);

        if (currentGoal.targetCharacter != player.playerName)
        {
            NPC target = npc_data.GetNPCByName(currentGoal.targetCharacter);
            target.WasSpokenToBy(this.name, currentGoal.dialog);
        }

        InitiateBehavior_Idle();
    }

    private void ConcludeBehavior_CustomAction()
    {
        if (DEBUG_NPCActions)
            Debug.Log($"NPC _{name} concluding custom action.");
        specialActionIndicator.SetActive(false);
        UpdateCurrentGoal();
    }

    private void ConcludeBehavior_Idle()
    {
        if (DEBUG_NPCActions)
            Debug.Log($"NPC _{name} concluding idle behavior.");
        UpdateCurrentGoal();
    }

    public void MoveToTransform(UnityEngine.Vector3 destination, string destinationName)
    {
        MoveToTransform(destination, destinationName, false);
    }

    public void MoveToTransform(UnityEngine.Vector3 destination, string destinationName, bool alwaysMove)
    {
        if (currentBehavior.behaviorType == NPC_Behavior.NPC_BehaviorType.SpeakingTo)
        {
            Debug.LogWarning($"NPC _{name} who was in SpeakingTo behavior has decided to move. Randy didn't think this case was possible, since NPCs who are spoken to should change to Behavior = BeingSpokenTo.");
            dialogCanvas.gameObject.SetActive(false);
        }

        currentBehavior = NPC_Behavior.Moving(destinationName);

        if ((!alwaysMove) && (IsNearby(transform.position, destination, currentLocation, destinationName)))
        {
            if (DEBUG_NPCActions)
                Debug.Log($"NPC _{name} wanted to move to {destinationName} but is already there, so no need.");
            ArrivedAtLocation(false);
        }
        else
        {
            if (DEBUG_NPCActions)
                Debug.Log($"NPC _{name} starting move to location {destinationName}");

            bool success = myBMWalk.InitiateMoveTowardsDestination(destination);

            if (!success)
            {
                if (currentGoal.goalType == NPC_Goal.NPC_GoalType.TalkTo)
                {
                    NPC NPCtarget = npc_data.GetNPCByName(currentGoal.targetCharacter);
                    if (NPCtarget != null)
                        NPCtarget.BeingSpokenTo_Cancelled(name);
                }
                InitiateBehavior_Idle();
            }
        }
    }

    public void ArrivedAtLocation()
    {
        ArrivedAtLocation(true);
    }

    public void ArrivedAtLocation(bool spewWhenArrived)
    {
        if (spewWhenArrived && DEBUG_NPCActions)
            Debug.Log($"NPC _{name} has arrived at location {currentBehavior.destination}.");

        if (currentGoal.goalType == NPC_Goal.NPC_GoalType.MoveTo)
        {
            if (useOpenAI)
                InitiateBehavior_Idle();
            else
                InitiateBehavior_Idle();
        }
        else if ((currentGoal.goalType == NPC_Goal.NPC_GoalType.TalkTo) || (currentGoal.goalType == NPC_Goal.NPC_GoalType.GivePlayerObjective))
        {
            if (!DialogTargetIsNearby())
            {
                if (DEBUG_NPCActions)
                    Debug.Log($"NPC _{name} arrived at location where they expected to find _{currentGoal.targetCharacter} but they are not here. Attempting to move to their new location.");
                InitiateBehavior_Dialog(currentGoal.targetCharacter);
                return;
            }

            if (currentGoal.targetCharacter == player.playerName)
            {
                FaceTarget(player.transform);
            }
            else
            {
                NPC target = npc_data.GetNPCByName(currentGoal.targetCharacter);

                if ((target.currentBehavior.behaviorType != NPC_Behavior.NPC_BehaviorType.BeingSpokenTo) || (target.currentBehavior.beingSpokenToBy != name))
                {
                    if (DEBUG_NPCActions)
                        Debug.Log($"NPC _{name} arrived at _{currentGoal.targetCharacter} but they were no longer being spoked to by me. Probably the player got here first.");
                    UpdateCurrentGoal();
                    return;
                }

                target.BeingSpokenTo(name);
                FaceTarget(target.transform);
            }

            Speak(currentGoal.targetCharacter, currentGoal.dialog);
        }
        else if (currentGoal.goalType == NPC_Goal.NPC_GoalType.PerformCustomAction)
        {
            if (DEBUG_NPCActions)
                Debug.Log($"NPC _{name} entering custom action behavior.");
            specialActionIndicator.SetActive(true);
            FaceTarget(customActionLocation.faceMe);
            timeBehaviorWillConclude = Time.time + durationPerformCustomAction;
            currentBehavior = NPC_Behavior.PerformingCustomAction();
        }
        else
        {
            Debug.LogError($"NPC _{name} can't process current action type of {currentGoal.goalType.ToString()}");
            InitiateBehavior_Idle();
        }
    }

    public void CantGetToDestination()
    {
        if (DEBUG_NPCActions)
            Debug.Log($"NPC _{name} gave up on attempting to pathfind to current goal location: {currentGoal.goalType.ToString()}, switching to Idle behavior.");

        UpdateCurrentGoal();
    }

    [PunRPC]
    public void TargetedForSpeaking(string fromCharacter)
    {
        if (DEBUG_NPCActions)
            Debug.Log($"NPC _{name} is being targeted for speaking by _{fromCharacter}");

        if (fromCharacter == player.playerName)
            Debug.LogWarning("Player should call BeingSpokenTo(), not TargetedForSpeaking()");

        currentBehavior = NPC_Behavior.BeingSpokenTo(fromCharacter);
    }

    [PunRPC]
    public void BeingSpokenTo(string fromCharacter)
    {
        if (DEBUG_NPCActions)
            Debug.Log($"NPC _{name} is being spoken to by _{fromCharacter}");

        if (currentGoal.goalType == NPC_Goal.NPC_GoalType.TalkTo)
            BeingSpokenTo_Cancelled(currentGoal.targetCharacter);

        specialActionIndicator.SetActive(false);

        if (fromCharacter == player.playerName)
        {
            FaceTarget(player.transform);

            currentGoal = new NPC_Goal(NPC_Goal.NPC_GoalType.Nothing);

            foreach (NPC npc in npc_data.allNPCs)
                if (npc != this)
                    npc.IsBeingSpokenToByPlayer(this);
        }
        else
        {
            FaceTarget(npc_data.GetNPCByName(fromCharacter).transform);
        }

        currentBehavior = NPC_Behavior.BeingSpokenTo(fromCharacter);
    }

    [PunRPC]
    public void BeingSpokenTo_Cancelled(string fromCharacter)
    {
        if (DEBUG_NPCActions)
            Debug.Log($"NPC _{name} is no longer being spoken to by _{fromCharacter}");

        InitiateBehavior_Idle();
    }

    public void IsBeingSpokenToByPlayer(NPC speaker)
    {
        if (!IsInDialogWithCharacter(player.playerName))
            return;

        if (DEBUG_NPCActions)
        {
            if (currentBehavior.behaviorType == NPC_Behavior.NPC_BehaviorType.SpeakingTo)
                Debug.Log($"NPC _{name} had been speaking to _{player.playerName} but is breaking off because _{speaker.name} is now being spoken to by _{player.playerName}.");
            else if (currentBehavior.behaviorType == NPC_Behavior.NPC_BehaviorType.BeingSpokenTo)
                Debug.Log($"NPC _{name} was being spoken to by _{player.playerName} but is breaking off because _{speaker.name} is now being spoken to by _{player.playerName}.");
        }

        InitiateBehavior_Idle();
    }

    public void Speak(string toCharacter, string dialog)
    {
        if (DEBUG_NPCActions)
            Debug.Log($"NPC _{name} speaking to _{toCharacter} and saying: {dialog}");

        DialogEvent dialogEvent = new DialogEvent(name, toCharacter, dialog, currentLocation);
        RememberDialogEvent(dialogEvent);

        if (toCharacter != player.playerName)
        {
            NPC target = npc_data.GetNPCByName(toCharacter);
            if (target != null)
                target.RememberDialogEvent(dialogEvent);
        }

        CheckForEavesdropping(dialogEvent);

        string text = new string("Speaking to: ");
        text = text + toCharacter + '\n' + '\n';
        text = text + dialog;

        dialogText.text = text;
        dialogCanvas.gameObject.SetActive(true);

        int wordCount = GetWordCount(dialog);
        float durationOfDialog = (npc_data.duration_SecPerWord * wordCount);
        durationOfDialog = Mathf.Clamp(durationOfDialog, npc_data.minDialogDuration, npc_data.maxDialogDuration);

        timeBehaviorWillConclude = Time.time + durationOfDialog;
        timeDialogUiShouldClose = timeBehaviorWillConclude;

        currentBehavior = NPC_Behavior.Speaking();
    }

    [PunRPC]
    public void WasSpokenToBy(string fromCharacter, string dialog)
    {
        if (DEBUG_NPCActions)
            Debug.Log($"NPC _{name} was spoken to by _{fromCharacter} who said: {dialog}");

        if ((currentBehavior.behaviorType == NPC_Behavior.NPC_BehaviorType.BeingSpokenTo) && (currentBehavior.beingSpokenToBy == fromCharacter))
        {
            UpdateCurrentGoal();
        }
        else
        {
            if (DEBUG_NPCActions)
                Debug.Log($"NPC _{name} will not respond to _{fromCharacter} because {name} is already doing this behavior: {currentBehavior.ToString()}");
        }
    }

    public static bool IsValidDialogTargetForNPC(NPC speaker, string target, NPC_Data npc_data, Player player, bool allowDebugSpew)
    {
        bool spew = allowDebugSpew && (speaker.DEBUG_AIPrompts || speaker.DEBUG_NPCActions);

        if (speaker.name == target)
        {
            if (spew)
                Debug.Log($"_{target} is not a valid dialog target, because this NPC is {target}");
            return false;
        }

        if (target == player.playerName)
        {
            foreach (NPC npc in npc_data.allNPCs)
                if (npc != speaker)
                    if (npc.IsInDialogWithCharacter((player.playerName)))  
                        if (npc.IsNearby(npc.transform.position, player.transform.position, npc.currentLocation, player.currentLocation))
                        {
                            if (spew)
                                Debug.Log($"_{target} is not a valid dialog target for _{speaker.name}, because {target} is already talking to _{npc.name}");
                            return false;
                        }

            return true;
        }

        NPC targetNPC = npc_data.GetNPCByName(target);
        if (targetNPC == null)
        {
            if (spew)
                Debug.Log($"_{target} is not a valid dialog target for _{speaker.name}, because there is no known NPC with that name.");
            return false;
        }

        if (targetNPC.currentBehavior.behaviorType == NPC_Behavior.NPC_BehaviorType.AwaitingInstructions)
        {
            if (spew)
                Debug.Log($"_{target} is not a valid dialog target for _{speaker.name}, because they are waiting to hear back from openAI.");
            return false;
        }

        if (targetNPC.currentBehavior.behaviorType == NPC_Behavior.NPC_BehaviorType.Moving)
            if (targetNPC.currentBehavior.destination != targetNPC.currentLocation)
            {
                if (spew)
                    Debug.Log($"_{target} is a valid dialog target for _{speaker.name}, but note they are moving to {targetNPC.currentBehavior.destination} from {targetNPC.currentLocation}");
            }

        if ((targetNPC.currentGoal.goalType == NPC_Goal.NPC_GoalType.TalkTo) && (targetNPC.currentGoal.targetCharacter != speaker.name))
        {
            if (spew)
                Debug.Log($"_{target} is not a valid dialog target for _{speaker.name}, because they are speaking to _{targetNPC.currentGoal.targetCharacter}.");
            return false;
        }
        if ((targetNPC.currentBehavior.behaviorType == NPC_Behavior.NPC_BehaviorType.BeingSpokenTo) && (targetNPC.currentBehavior.beingSpokenToBy != speaker.name))
        {
            if (spew)
                Debug.Log($"_{target} is not a valid dialog target for _{speaker.name}, because they are being spoken to by _{targetNPC.currentBehavior.beingSpokenToBy}.");
            return false;
        }

        return true;
    }

    public bool IsInDialogWithCharacter(string characterName)
    {
        if (currentGoal != null)
            if (currentGoal.goalType == NPC_Goal.NPC_GoalType.TalkTo)
                if (currentGoal.targetCharacter == characterName)
                    return true;

        if (currentBehavior.behaviorType == NPC_Behavior.NPC_BehaviorType.BeingSpokenTo)
            if (currentBehavior.beingSpokenToBy == characterName)
                return true;

        return false;
    }

    private static int GetWordCount(string input)
    {
        string[] words = input.Split(new char[] { ' ', '\t', '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        return words.Length;
    }

    public void RememberDialogEvent(DialogEvent dialogEvent)
    {
        if (dialogEvent != null)
            dialogHistory.Add(dialogEvent);
    }

 private void CheckForEavesdropping(DialogEvent dialogEvent)
    {
        foreach (NPC npc in npc_data.allNPCs)
            if (currentLocation != NPC_Data.startingLocationName)
                if ((npc.name != dialogEvent.speaker) && (npc.name != dialogEvent.listener))
                    if (IsNearby(npc.transform.position, transform.position, npc.currentLocation, currentLocation))
                        npc.RememberDialogEvent(dialogEvent);
    }

    private bool DialogTargetIsNearby()
    {
        if ((currentGoal.goalType != NPC_Goal.NPC_GoalType.TalkTo) && (currentGoal.goalType != NPC_Goal.NPC_GoalType.GivePlayerObjective))
        {
            Debug.LogError($"DialogTargetIsNearby() called on _{name} when their goal type is not TalkTo nor GivePlayerObjective. Their goal is {currentGoal.ToString()}");
            return false;
        }

        if (currentGoal.targetCharacter == player.playerName)
            return IsNearby(transform.position, player.transform.position, currentLocation, player.currentLocation);
        else
            return IsNearby(transform.position, npc_data.GetPositionByCharacterName(currentGoal.targetCharacter), currentLocation, npc_data.GetNPCByName(currentGoal.targetCharacter).currentLocation);
    }

    public bool IsNearby(Vector3 positionOfNPC, Vector3 positionOfTarget, string nameOfNPCLocation, string nameOfTargetLocation)
    {
        if (npc_data.IsOneOfTheNamedLocations(nameOfNPCLocation))
            if (nameOfNPCLocation == nameOfTargetLocation)
                return true;

        if (Vector3.Distance(positionOfNPC, positionOfTarget) < npc_data.nearbyThreshold)
            return true;

        return false;
    }

    public void StartSelection()
    {
        selectionIndicator.SetActive(true);
    }

    public void StopSelection()
    {
        selectionIndicator.SetActive(false);
    }

    public void FaceTarget(Transform target)
    {
        FaceTarget(target, 0f);
    }

    public void FaceTarget(Transform target, float rotationOffset)
    {
        UnityEngine.Vector3 oldRotation = transform.rotation.eulerAngles;

        transform.LookAt(target);

        UnityEngine.Vector3 newRotation = transform.rotation.eulerAngles;

        newRotation.y += rotationOffset;

        newRotation.x = 0;
        newRotation.z = 0;

        transform.rotation = UnityEngine.Quaternion.Euler(newRotation);
    }

    public async Task<string[]> RequestDialogOptions()
    {
        string baseOpenAIPrompt = npc_data.genericPrompt_part1 + "\n" + specificCharacterPrompt_part2 + "\n\n";
        string userInputPrompt = NPC_openAI.GenerateUserInputPromptForDialogOptions(this, npc_data, player, dialogHistory, maxDialogHistoryToUseInPrompts);

        if (!useOpenAI)
        {
            if (DEBUG_AIPrompts)
                Debug.Log($"PROMPT _{name} generated this userInputPrompt for player dialog options (not sending to openAI because RandomOffline): {userInputPrompt}");

            return npc_data.offlinePlayerDialogOptions;
        }

        if (DEBUG_AIPrompts)
            Debug.Log($"PROMPT _{name} sending player dialog request to openAI: {baseOpenAIPrompt}\n   -----\n{userInputPrompt}");

        string response = await Karyo_GameCore.Instance.openAiService.GetChatCompletionAsync(openAIModel == OpenAIModel.GPT_3_5 ? OpenAI.Models.Model.GPT3_5_Turbo : OpenAI.Models.Model.GPT4,
                baseOpenAIPrompt, userInputPrompt);

        if (DEBUG_AIPrompts || DEBUG_NPCActions)
            Debug.Log($"PROMPT _{name} received player dialog response from openAI: {response}");

        string[] dialog_options = NPC_openAI.ProcessTextFromAIForDialogOptions(response);

        if (DEBUG_AIPrompts || DEBUG_NPCActions)
        {
            if (dialog_options != null)
                Debug.Log($"PROMPT: _{name} parsed AI response for these dialog options: 1-{dialog_options[0]}, 2-{dialog_options[1]}, 3-{dialog_options[2]}");
            else
                Debug.Log($"PROMPT: _{name} could not parse AI response for dialog options.");
        }

        return dialog_options;
    }

    public override void TakeDamage(float damage)
    {
        if (!photonView.IsMine) return;
        // Implement your damage logic here for NPCs
        health -= damage;
        if (health <= 0)
        {
            // Handle NPC death
        }
    }

    public override void Move(Vector3 direction)
    {
        if (!photonView.IsMine) return;
        // Implement your movement logic here for NPCs
        transform.position += direction * speed * Time.deltaTime;
    }

    public void HandleAI()
    {
        if (!photonView.IsMine) return;
        // Add your AI handling code here
        // This method should be called from Update() or FixedUpdate()
    }

    public override void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // We own this NPC: send the others our data
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(currentLocation);
            stream.SendNext((int)currentBehavior.behaviorType);
            stream.SendNext(health);
        }
        else
        {
            // Network NPC, receive data
            transform.position = (Vector3)stream.ReceiveNext();
            transform.rotation = (Quaternion)stream.ReceiveNext();
            currentLocation = (string)stream.ReceiveNext();
            currentBehavior.behaviorType = (NPC_Behavior.NPC_BehaviorType)stream.ReceiveNext();
            health = (float)stream.ReceiveNext();
        }
    }
}