using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using System.Threading.Tasks;
using ProjectGrow.AI;

public class NPC_Behavior : MonoBehaviourPunCallbacks
{
    private UniversalCharacterController characterController;
    private NPC_Data npcData;
    private NavMeshAgent navMeshAgent;
    private AIManager aiManager;

    [SerializeField] private float decisionInterval = 5f;
    [SerializeField] private float interactionCooldown = 10f;
    [SerializeField] private float interactionDistance = 5f;
    [SerializeField] private float interactionPauseTime = 3f;
    [SerializeField] private float waypointPauseTime = 2f;
    [SerializeField] private float locationChangeCooldown = 5f;
    [SerializeField] private float backgroundThinkingInterval = 5f;
    [SerializeField] private float idleMovementRadius = 2f;
    [SerializeField] private float idleMovementInterval = 15f;
    [SerializeField] private float majorMovementInterval = 30f;

    private float lastDecisionTime;
    private float lastInteractionTime;
    private float lastInteractionPauseTime = 0f;
    private float lastLocationChangeTime = 0f;
    private float lastBackgroundThinkingTime;
    private float lastIdleMovementTime;
    private float lastMajorMovementTime;

    private bool isPausedAtWaypoint = false;
    private bool isAcclimating = false;

    private LocationManager currentLocationManager;

    public void Initialize(UniversalCharacterController controller, NPC_Data data, AIManager manager)
    {
        characterController = controller;
        npcData = data;
        aiManager = manager;
        navMeshAgent = GetComponent<NavMeshAgent>();
        if (navMeshAgent == null)
        {
            navMeshAgent = gameObject.AddComponent<NavMeshAgent>();
        }
        navMeshAgent.speed = characterController.walkSpeed;
        ResetTimers();
    }

    private void ResetTimers()
    {
        lastDecisionTime = Time.time;
        lastInteractionTime = Time.time;
        lastBackgroundThinkingTime = Time.time;
        lastIdleMovementTime = Time.time;
        lastMajorMovementTime = Time.time;
    }

    private void Update()
    {
        if (!photonView.IsMine) return;
        if (characterController == null || aiManager == null) return;

        UpdateBehavior();
        PerformBackgroundThinking();
        ConsiderMajorMovement();
        UpdateIdleMovement();
    }

    public void UpdateBehavior()
    {
        if (isAcclimating) return;

        if (Time.time - lastDecisionTime > decisionInterval)
        {
            StartCoroutine(MakeDecisionCoroutine());
        }

        if (characterController.HasState(CharacterState.Moving))
        {
            CheckForNearbyCharacters();
        }

        if (Time.time - lastInteractionTime > interactionCooldown)
        {
            AttemptInteraction();
        }
    }

    private void CheckForNearbyCharacters()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, aiManager.interactionRadius);
        foreach (var hitCollider in hitColliders)
        {
            UniversalCharacterController otherCharacter = hitCollider.GetComponent<UniversalCharacterController>();
            if (otherCharacter != null && otherCharacter != characterController)
            {
                InitiateInteractionPause(otherCharacter);
                break;
            }
        }
    }

    private void InitiateInteractionPause(UniversalCharacterController otherCharacter)
    {
        if (Time.time - lastInteractionPauseTime < interactionPauseTime) return;

        lastInteractionPauseTime = Time.time;
        characterController.StopMoving();
        StartCoroutine(InteractionPauseCoroutine(otherCharacter));
    }

    private IEnumerator InteractionPauseCoroutine(UniversalCharacterController otherCharacter)
    {
        yield return new WaitForSeconds(interactionPauseTime);
        aiManager.ConsiderCollaboration(otherCharacter);
    }

    private void PerformBackgroundThinking()
    {
        if (Time.time - lastBackgroundThinkingTime < backgroundThinkingInterval) return;

        lastBackgroundThinkingTime = Time.time;
        if (GameManager.Instance == null || npcData == null) return;

        GameState currentState = GameManager.Instance.GetCurrentGameState();
        UpdateMentalModelFromGameState(currentState);
        EvaluateObjectives(currentState);
        ConsiderGroupActions();
    }

    private void UpdateMentalModelFromGameState(GameState currentState)
    {
        if (currentState.CurrentChallenge == null || npcData == null) return;

        CharacterMentalModel mentalModel = npcData.GetMentalModel();

        foreach (var milestone in currentState.CurrentChallenge.milestones)
        {
            string status = currentState.MilestoneCompletion[milestone] ? "Completed" : "In Progress";
            mentalModel.AddMemory($"Milestone '{milestone}' is {status}", 0.7f);
        }

        UpdateEmotionalState(currentState);
    }

    private void EvaluateObjectives(GameState currentState)
    {
        if (npcData == null || characterController == null) return;

        List<string> currentObjectives = GetCurrentObjectives();
        string bestObjective = npcData.GetMentalModel().MakeDecision(currentObjectives, currentState);

        if (bestObjective != characterController.currentObjective)
        {
            SetNewObjective(bestObjective);
            aiManager.RecordSignificantEvent($"Changed objective to: {bestObjective}", 0.8f);
        }
    }

    private List<string> GetCurrentObjectives()
    {
        List<string> objectives = new List<string>();
        if (GameManager.Instance != null && GameManager.Instance.GetCurrentChallenge() != null)
        {
            objectives.AddRange(GameManager.Instance.GetCurrentChallenge().milestones);
        }
        if (aiManager != null)
        {
            objectives.AddRange(aiManager.GetPersonalGoalTags());
        }
        return objectives;
    }

    private void SetNewObjective(string newObjective)
    {
        if (characterController != null && npcData != null)
        {
            characterController.currentObjective = newObjective;
            npcData.AddMemory($"Set new objective: {newObjective}");
        }
    }

    private void UpdateEmotionalState(GameState currentState)
    {
        if (npcData == null) return;

        int completedMilestones = currentState.MilestoneCompletion.Count(m => m.Value);
        int totalMilestones = currentState.MilestoneCompletion.Count;
        float progress = totalMilestones > 0 ? (float)completedMilestones / totalMilestones : 0f;

        EmotionalState newState = progress switch
        {
            > 0.75f => EmotionalState.Excited,
            > 0.5f => EmotionalState.Confident,
            > 0.25f => EmotionalState.Neutral,
            _ => EmotionalState.Anxious
        };

        npcData.UpdateEmotionalState(newState);
    }

    private void ConsiderGroupActions()
    {
        if (characterController.HasState(CharacterState.InGroup))
        {
            EvaluateGroupObjectives();
        }
        else
        {
            ConsiderJoiningGroup();
        }
    }

    private void EvaluateGroupObjectives()
    {
        string groupId = characterController.GetCurrentGroupId();
        if (string.IsNullOrEmpty(groupId)) return;

        List<UniversalCharacterController> groupMembers = GroupManager.Instance.GetGroupMembers(groupId);
        List<string> groupObjectives = GetGroupObjectives(groupMembers);

        if (groupObjectives.Count == 0) return;

        GameState currentState = GameManager.Instance.GetCurrentGameState();
        string bestGroupObjective = npcData.GetMentalModel().MakeDecision(groupObjectives, currentState);

        MoveGroupToObjective(groupId, bestGroupObjective);
    }

    private List<string> GetGroupObjectives(List<UniversalCharacterController> groupMembers)
    {
        HashSet<string> groupObjectives = new HashSet<string>();
        foreach (var member in groupMembers)
        {
            groupObjectives.UnionWith(GetCurrentObjectives());
        }
        return new List<string>(groupObjectives);
    }

    private void MoveGroupToObjective(string groupId, string objective)
    {
        LocationManager targetLocation = FindLocationForObjective(objective);
        if (targetLocation != null)
        {
            GroupManager.Instance.MoveGroup(groupId, targetLocation.transform.position);
        }
    }

    private LocationManager FindLocationForObjective(string objective)
    {
        return LocationManagerMaster.Instance.GetAllLocations()
            .Select(locationName => LocationManagerMaster.Instance.GetLocation(locationName))
            .FirstOrDefault(l => l != null && l.availableActions.Any(a => a.actionName.Contains(objective)));
    }

    private void ConsiderJoiningGroup()
    {
        List<UniversalCharacterController> nearbyCharacters = GetNearbyCharacters();
        UniversalCharacterController potentialGroupMember = nearbyCharacters.FirstOrDefault(c => c.HasState(CharacterState.InGroup) && !c.IsPlayerControlled);

        if (potentialGroupMember != null && ShouldJoinGroup(potentialGroupMember))
        {
            string groupId = potentialGroupMember.GetCurrentGroupId();
            if (!string.IsNullOrEmpty(groupId))
            {
                GroupManager.Instance.AddToGroup(groupId, characterController);
            }
        }
        else if (nearbyCharacters.Count > 0 && ShouldFormGroup(nearbyCharacters))
        {
            List<UniversalCharacterController> groupMembers = new List<UniversalCharacterController> { characterController };
            groupMembers.AddRange(nearbyCharacters.Where(c => !c.IsPlayerControlled).Take(2));
            GroupManager.Instance.FormGroup(groupMembers);
        }
    }

    private bool ShouldJoinGroup(UniversalCharacterController groupMember)
    {
        float relationshipScore = npcData.GetRelationship(groupMember.characterName);
        return relationshipScore > 0.6f && Random.value < 0.5f;
    }

    private bool ShouldFormGroup(List<UniversalCharacterController> nearbyCharacters)
    {
        nearbyCharacters = nearbyCharacters.Where(c => !c.IsPlayerControlled).ToList();
        return nearbyCharacters.Count >= 2 && Random.value < 0.3f;
    }

    private void ConsiderMajorMovement()
    {
        if (Time.time - lastMajorMovementTime < majorMovementInterval) return;

        lastMajorMovementTime = Time.time;
        if (Random.value < 0.5f) // 50% chance to consider movement
        {
            string targetLocation = LocationManagerMaster.Instance.GetTargetLocation(GameManager.Instance.GetCurrentChallenge().milestones);
            MoveToLocation(targetLocation);
        }
    }

    private IEnumerator MakeDecisionCoroutine()
    {
        if (characterController == null || aiManager == null || GameManager.Instance == null) yield break;

        lastDecisionTime = Time.time;

        if (characterController.HasState(CharacterState.Acclimating))
        {
            yield break;
        }

        List<string> options = new List<string>
        {
            "Move to new location",
            "Perform location action",
            "Interact with nearby character",
            "Work on current challenge",
            "Pursue personal goal",
            "Initiate collaboration",
            "Idle"
        };

        GameState currentState = GameManager.Instance.GetCurrentGameState();
        Task<string> decisionTask = aiManager.MakeDecision(options, currentState);
        yield return new WaitUntil(() => decisionTask.IsCompleted);

        string decision = decisionTask.Result;

        switch (decision)
        {
            case "Move to new location":
                MoveToNewLocation();
                break;
            case "Perform location action":
                PerformLocationAction();
                break;
            case "Interact with nearby character":
                AttemptInteraction();
                break;
            case "Work on current challenge":
                WorkOnChallenge();
                break;
            case "Pursue personal goal":
                PursuePersonalGoal();
                break;
            case "Initiate collaboration":
                InitiateCollaboration();
                break;
            case "Idle":
                EnterIdleState();
                break;
        }
    }

    private void MoveToNewLocation()
    {
        if (LocationManagerMaster.Instance == null || GameManager.Instance == null || characterController == null) return;

        string newLocation = LocationManagerMaster.Instance.GetTargetLocation(GameManager.Instance.GetCurrentChallenge().milestones);
        MoveToLocation(newLocation);

        lastLocationChangeTime = Time.time;
        Debug.Log($"{characterController.characterName} moved to {newLocation}");
    }

   public void MoveToPosition(Vector3 position)
{
    if (navMeshAgent != null && navMeshAgent.enabled && !characterController.HasState(CharacterState.PerformingAction))
    {
        navMeshAgent.SetDestination(position);
        characterController.AddState(CharacterState.Moving);
        Debug.Log($"{characterController.characterName}: Setting destination to {position}. NavMeshAgent.hasPath: {navMeshAgent.hasPath}, NavMeshAgent.pathStatus: {navMeshAgent.pathStatus}");
        StartCoroutine(CheckWaypointArrival());
    }
    else
    {
        Debug.LogWarning($"{characterController.characterName}: Cannot move. NavMeshAgent status: {(navMeshAgent == null ? "null" : navMeshAgent.enabled ? "enabled" : "disabled")}. PerformingAction: {characterController.HasState(CharacterState.PerformingAction)}");
    }
}

    private IEnumerator CheckWaypointArrival()
{
    while (characterController.HasState(CharacterState.Moving))
    {
        Debug.Log($"{characterController.characterName}: Checking waypoint arrival. Remaining distance: {navMeshAgent.remainingDistance}, Path pending: {navMeshAgent.pathPending}");
        if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance < 0.1f)
        {
            if (WaypointsManager.Instance.IsNearWaypoint(transform.position) && !isPausedAtWaypoint)
            {
                yield return StartCoroutine(PauseAtWaypoint());
            }
            else
            {
                characterController.RemoveState(CharacterState.Moving);
                characterController.AddState(CharacterState.Idle);
                Debug.Log($"{characterController.characterName}: Reached destination. Switching to Idle state.");
                break;
            }
        }
        yield return null;
    }
}

    private IEnumerator PauseAtWaypoint()
    {
        isPausedAtWaypoint = true;
        navMeshAgent.isStopped = true;
        characterController.RemoveState(CharacterState.Moving);
        characterController.AddState(CharacterState.Idle);

        yield return new WaitForSeconds(waypointPauseTime);

        navMeshAgent.isStopped = false;
        characterController.RemoveState(CharacterState.Idle);
        characterController.AddState(CharacterState.Moving);
        isPausedAtWaypoint = false;
    }

    public void MoveToLocation(string locationName)
    {
        Vector3 destination = WaypointsManager.Instance.GetWaypointNearLocation(locationName);
        if (destination != Vector3.zero)
        {
            MoveToPosition(destination);
            Debug.Log($"{characterController.characterName} is moving to {locationName}");
        }
    }

    private void PerformLocationAction()
    {
        if (currentLocationManager == null || characterController == null || aiManager == null) return;

        List<LocationManager.LocationAction> availableActions = currentLocationManager.GetAvailableActions(characterController.aiSettings.characterRole);
        if (availableActions.Count == 0) return;

        LocationManager.LocationAction selectedAction = ChooseBestAction(availableActions);
        ExecuteAction(selectedAction);
    }

    private void ExecuteAction(LocationManager.LocationAction action)
    {
        if (characterController == null || currentLocationManager == null) return;

        characterController.StartAction(action);
        ActionLogManager.Instance?.LogAction(characterController.characterName, $"performing {action.actionName} at {currentLocationManager.locationName}");
    }

    private LocationManager.LocationAction ChooseBestAction(List<LocationManager.LocationAction> actions)
    {
        if (GameManager.Instance == null || aiManager == null) return null;

        GameState currentState = GameManager.Instance.GetCurrentGameState();
        List<string> personalGoalTags = aiManager.GetPersonalGoalTags();
        string currentChallenge = currentState.CurrentChallenge.title;

        var scoredActions = actions.Select(action => new
        {
            Action = action,
            Score = ScoreAction(action, personalGoalTags, currentChallenge)
        }).ToList();

        return scoredActions.OrderByDescending(sa => sa.Score).FirstOrDefault()?.Action;
    }

    private float ScoreAction(LocationManager.LocationAction action, List<string> personalGoalTags, string currentChallenge)
    {
        float score = 0;

        if (personalGoalTags.Any(goal => action.actionName.ToLower().Contains(goal.ToLower())))
        {
            score += 2;
        }

        if (action.actionName.ToLower().Contains(currentChallenge.ToLower()))
        {
            score += 3;
        }

        score += Random.Range(0f, 0.5f);

        return score;
    }

    private void AttemptInteraction()
    {
        List<UniversalCharacterController> nearbyCharacters = GetNearbyCharacters();
        if (nearbyCharacters.Count > 0)
        {
            UniversalCharacterController target = ChooseInteractionTarget(nearbyCharacters);
            if (target != null)
            {
                if (ShouldAttemptCollaboration(target))
                {
                    InitiateCollaboration(target);
                }
                else
                {
                    InitiateInteraction(target);
                }
            }
        }
    }

    private bool ShouldAttemptCollaboration(UniversalCharacterController target)
    {
        return Random.value > 0.5f || target.currentLocation == characterController.currentLocation;
    }

    private void InitiateCollaboration(UniversalCharacterController target = null)
    {
        if (CollabManager.Instance.CanInitiateCollab(characterController))
        {
            string actionName = ChooseCollaborationAction(target);
            if (!string.IsNullOrEmpty(actionName))
            {
                characterController.InitiateCollab(actionName, target);
            }
        }
    }

    private string ChooseCollaborationAction(UniversalCharacterController target)
    {
        if (currentLocationManager == null) return null;

        List<LocationManager.LocationAction> availableActions = currentLocationManager.GetAvailableActions(characterController.aiSettings.characterRole);
        List<LocationManager.LocationAction> targetActions = target != null 
            ? currentLocationManager.GetAvailableActions(target.aiSettings.characterRole)
            : availableActions;

        var commonActions = availableActions.Intersect(targetActions, new LocationActionComparer());
        return commonActions.Any() ? commonActions.First().actionName : null;
    }

    private List<UniversalCharacterController> GetNearbyCharacters()
    {
        if (GameManager.Instance == null || characterController == null) return new List<UniversalCharacterController>();

        return GameManager.Instance.GetAllCharacters()
            .Where(character => character != null && character != characterController &&
                   Vector3.Distance(transform.position, character.transform.position) <= interactionDistance)
            .ToList();
    }

    private UniversalCharacterController ChooseInteractionTarget(List<UniversalCharacterController> nearbyCharacters)
    {
        if (npcData == null) return null;

        return nearbyCharacters.OrderByDescending(character => npcData.GetRelationship(character.characterName)).FirstOrDefault();
    }

    private void InitiateInteraction(UniversalCharacterController target)
    {
        if (target == null || aiManager == null) return;

        lastInteractionTime = Time.time;
        if (target.IsPlayerControlled)
        {
            aiManager.InitiateDialogueWithPlayer(target);
        }
        else
        {
            DialogueManager.Instance?.TriggerAgentDialogue(characterController, target);
        }
    }

    private void WorkOnChallenge()
    {
        if (GameManager.Instance == null || characterController == null) return;

        string currentChallenge = GameManager.Instance.GetCurrentChallenge().title;
        LocationManager.LocationAction action = new LocationManager.LocationAction
        {
            actionName = $"Working on {currentChallenge}",
            duration = 30,
            baseSuccessRate = 0.7f,
            description = $"Focusing intensely on solving {currentChallenge}"
        };

        characterController.StartAction(action);
    }

    private void PursuePersonalGoal()
    {
        if (aiManager == null || characterController == null) return;

        List<string> personalGoalTags = aiManager.GetPersonalGoalTags();
        Dictionary<string, bool> personalGoalCompletion = aiManager.GetPersonalGoalCompletion();

        string incompleteGoal = personalGoalTags.FirstOrDefault(goal => !personalGoalCompletion[goal]);

        if (incompleteGoal != null)
        {
            LocationManager.LocationAction action = new LocationManager.LocationAction
            {
                actionName = $"Pursuing personal goal: {incompleteGoal}",
                duration = 30,
                baseSuccessRate = 0.8f,
                description = $"Focusing on personal goal: {incompleteGoal}"
            };

            characterController.StartAction(action);
        }
        else
        {
            WorkOnChallenge();
        }
    }

    private void EnterIdleState()
    {
        characterController.AddState(CharacterState.Idle);
        lastIdleMovementTime = Time.time - idleMovementInterval;
    }

    private void UpdateIdleMovement()
    {
        if (!characterController.HasState(CharacterState.Idle)) return;
        if (Time.time - lastIdleMovementTime < idleMovementInterval) return;

        Vector3 randomDirection = Random.insideUnitSphere * idleMovementRadius;
        randomDirection += transform.position;
        NavMeshHit hit;
        NavMesh.SamplePosition(randomDirection, out hit, idleMovementRadius, 1);
        Vector3 finalPosition = hit.position;

        characterController.MoveWhileInState(finalPosition, characterController.walkSpeed * 0.5f);
        lastIdleMovementTime = Time.time;
    }

    public void SetCurrentLocation(LocationManager location)
    {
        if (characterController == null) return;

        currentLocationManager = location;
        if (location != null)
        {
            isAcclimating = true;
            characterController.AddState(CharacterState.Acclimating);
            StartCoroutine(AcclimationCoroutine());
        }
    }

    private IEnumerator AcclimationCoroutine()
    {
        if (characterController == null) yield break;

        yield return new WaitForSeconds(characterController.acclimationTime);
        isAcclimating = false;
        if (characterController.HasState(CharacterState.Acclimating))
        {
            characterController.RemoveState(CharacterState.Acclimating);
            EnterIdleState();
        }
    }
}