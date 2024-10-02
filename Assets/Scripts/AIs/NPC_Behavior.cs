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

    private float lastDecisionTime;
    private float interactionCooldown = 10f;
    private float lastInteractionTime;
    private float interactionDistance = 5f;
    [SerializeField] private float interactionPauseTime = 3f;
    private float lastInteractionPauseTime = 0f;

    [SerializeField] private float interactionCheckInterval = 2f;
    private float lastInteractionCheckTime = 0f;
    [SerializeField] private float waypointPauseTime = 2f;
    private bool isPausedAtWaypoint = false;

    private LocationManager currentLocationManager;
    [SerializeField] private float locationChangeCooldown = 60f;
    private float lastLocationChangeTime = 0f;
    private bool isAcclimating = false;

    private float backgroundThinkingInterval = 5f;
    private float lastBackgroundThinkingTime;

    private float idleMovementRadius = 2f;
    private float idleMovementInterval = 5f;
    private float lastIdleMovementTime;

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
        lastDecisionTime = Time.time;
        lastInteractionTime = Time.time;
        lastBackgroundThinkingTime = Time.time;
        lastIdleMovementTime = Time.time;
    }

    private void Update()
    {
        if (!photonView.IsMine) return;
        if (characterController == null || aiManager == null) return;

        if (!characterController.HasState(UniversalCharacterController.CharacterState.Chatting) &&
            !characterController.HasState(UniversalCharacterController.CharacterState.Collaborating))
        {
            UpdateBehavior();
        }

        if (Time.time - lastBackgroundThinkingTime > backgroundThinkingInterval)
        {
            PerformBackgroundThinking();
            lastBackgroundThinkingTime = Time.time;
        }

        if (characterController.HasState(UniversalCharacterController.CharacterState.Idle))
        {
            UpdateIdleMovement();
        }
    }

    public void UpdateBehavior()
    {
        if (isAcclimating || characterController == null || aiManager == null) return;

        if (Time.time - lastDecisionTime > characterController.aiSettings.decisionInterval)
        {
            StartCoroutine(MakeDecisionCoroutine());
        }

        if (characterController.HasState(UniversalCharacterController.CharacterState.Moving))
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

        EmotionalState newState;
        if (progress > 0.75f)
            newState = EmotionalState.Excited;
        else if (progress > 0.5f)
            newState = EmotionalState.Confident;
        else if (progress > 0.25f)
            newState = EmotionalState.Neutral;
        else
            newState = EmotionalState.Anxious;

        npcData.UpdateEmotionalState(newState);
    }

    private void ConsiderGroupActions()
    {
        if (characterController.IsInGroup())
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
        UniversalCharacterController potentialGroupMember = nearbyCharacters.FirstOrDefault(c => c.IsInGroup() && !c.IsPlayerControlled);

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

    private async Task<string> MakeDecisionWithMemory(List<string> options, GameState currentState)
    {
        return await aiManager.MakeDecision(options, currentState);
    }

    private IEnumerator MakeDecisionCoroutine()
    {
        if (characterController == null || aiManager == null || GameManager.Instance == null) yield break;

        lastDecisionTime = Time.time;

        if (characterController.HasState(UniversalCharacterController.CharacterState.Acclimating))
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
        if (navMeshAgent != null && navMeshAgent.enabled)
        {
            navMeshAgent.SetDestination(position);
            characterController.AddState(UniversalCharacterController.CharacterState.Moving);
            StartCoroutine(CheckWaypointArrival());
        }
    }

    private IEnumerator CheckWaypointArrival()
    {
        while (characterController.HasState(UniversalCharacterController.CharacterState.Moving))
        {
            if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance < 0.1f)
            {
                if (WaypointsManager.Instance.IsNearWaypoint(transform.position) && !isPausedAtWaypoint)
                {
                    yield return StartCoroutine(PauseAtWaypoint());
                }
                else
                {
                    characterController.RemoveState(UniversalCharacterController.CharacterState.Moving);
                    characterController.AddState(UniversalCharacterController.CharacterState.Idle);
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
        characterController.RemoveState(UniversalCharacterController.CharacterState.Moving);
        characterController.AddState(UniversalCharacterController.CharacterState.Idle);

        yield return new WaitForSeconds(waypointPauseTime);

        navMeshAgent.isStopped = false;
        characterController.RemoveState(UniversalCharacterController.CharacterState.Idle);
        characterController.AddState(UniversalCharacterController.CharacterState.Moving);
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

    private void InitiateCollaboration(UniversalCharacterController target)
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
        List<LocationManager.LocationAction> targetActions = currentLocationManager.GetAvailableActions(target.aiSettings.characterRole);

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

        return nearbyCharacters.OrderBy(character => npcData.GetRelationship(character.characterName)).FirstOrDefault();
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
        characterController.AddState(UniversalCharacterController.CharacterState.Idle);
        lastIdleMovementTime = Time.time - idleMovementInterval;
    }

    private void UpdateIdleMovement()
    {
        if (Time.time - lastIdleMovementTime >= idleMovementInterval)
        {
            Vector3 randomDirection = Random.insideUnitSphere * idleMovementRadius;
            randomDirection += transform.position;
            NavMeshHit hit;
            NavMesh.SamplePosition(randomDirection, out hit, idleMovementRadius, 1);
            Vector3 finalPosition = hit.position;

            characterController.MoveTo(finalPosition);
            lastIdleMovementTime = Time.time;
        }
    }

    public void SetCurrentLocation(LocationManager location)
    {
        if (characterController == null) return;

        currentLocationManager = location;
        if (location != null)
        {
            isAcclimating = true;
            characterController.AddState(UniversalCharacterController.CharacterState.Acclimating);
            StartCoroutine(AcclimationCoroutine());
        }
    }

    private IEnumerator AcclimationCoroutine()
    {
        if (characterController == null) yield break;

        yield return new WaitForSeconds(characterController.acclimationTime);
        isAcclimating = false;
        if (characterController.HasState(UniversalCharacterController.CharacterState.Acclimating))
        {
            characterController.RemoveState(UniversalCharacterController.CharacterState.Acclimating);
            EnterIdleState();
        }
    }
}