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
    private float interactionCooldown = 30f;
    private float lastInteractionTime;
    private float interactionDistance = 5f;

    private LocationManager currentLocationManager;
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
            if (navMeshAgent != null && !navMeshAgent.pathPending && navMeshAgent.remainingDistance < 0.1f)
            {
                characterController.RemoveState(UniversalCharacterController.CharacterState.Moving);
                characterController.AddState(UniversalCharacterController.CharacterState.Idle);
            }
        }

        if (Time.time - lastInteractionTime > interactionCooldown)
        {
            AttemptInteraction();
        }
    }

    private void PerformBackgroundThinking()
    {
        if (GameManager.Instance == null || npcData == null) return;

        GameState currentState = GameManager.Instance.GetCurrentGameState();
        
        UpdateMentalModelFromGameState(currentState);
        ConsiderCollaborations();
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

    private void ConsiderCollaborations()
    {
        if (CollabManager.Instance == null || characterController == null) return;

        if (CollabManager.Instance.CanInitiateCollab(characterController))
        {
            List<UniversalCharacterController> potentialCollaborators = CollabManager.Instance.GetEligibleCollaborators(characterController);
            foreach (var collaborator in potentialCollaborators)
            {
                if (collaborator != null && ShouldInitiateCollabWith(collaborator))
                {
                    InitiateCollaboration(collaborator);
                    break;
                }
            }
        }
    }

    private bool ShouldInitiateCollabWith(UniversalCharacterController collaborator)
    {
        if (npcData == null || collaborator == null) return false;

        float relationshipScore = npcData.GetRelationship(collaborator.characterName);
        return relationshipScore > 0.5f && Random.value < 0.3f;
    }

    private void InitiateCollaboration(UniversalCharacterController collaborator)
    {
        if (GameManager.Instance == null || characterController == null || collaborator == null) return;

        string actionName = "Collaborate on " + GameManager.Instance.GetCurrentChallenge().title;
        characterController.InitiateCollab(actionName, collaborator);
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
            // If in a group, consider group-based actions
            EvaluateGroupObjectives();
        }
        else
        {
            // If not in a group, consider joining or forming a group
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

        // Implement group objective here (e.g., move as a group to a new location)
        MoveGroupToObjective(groupId, bestGroupObjective);
    }

    private List<string> GetGroupObjectives(List<UniversalCharacterController> groupMembers)
    {
        // Combine objectives from all group members
        HashSet<string> groupObjectives = new HashSet<string>();
        foreach (var member in groupMembers)
        {
            groupObjectives.UnionWith(GetCurrentObjectives());
        }
        return new List<string>(groupObjectives);
    }

    private void MoveGroupToObjective(string groupId, string objective)
    {
        // Implement logic to move the group towards the objective
        // This could involve finding a suitable location and using GroupManager to move the group
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
        UniversalCharacterController potentialGroupMember = nearbyCharacters.FirstOrDefault(c => c.IsInGroup());

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
            groupMembers.AddRange(nearbyCharacters.Take(2)); // Limit to 3 members total
            GroupManager.Instance.FormGroup(groupMembers);
        }
    }

    private bool ShouldJoinGroup(UniversalCharacterController groupMember)
    {
        // Implement logic to decide whether to join a group
        float relationshipScore = npcData.GetRelationship(groupMember.characterName);
        return relationshipScore > 0.6f && Random.value < 0.5f;
    }

    private bool ShouldFormGroup(List<UniversalCharacterController> nearbyCharacters)
    {
        // Implement logic to decide whether to form a new group
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
    }

    private void MoveToLocation(string location)
    {
        if (LocationManagerMaster.Instance == null || navMeshAgent == null || characterController == null) return;

        Vector3 destination = LocationManagerMaster.Instance.GetLocationPosition(location);
        if (destination != Vector3.zero)
        {
            navMeshAgent.SetDestination(destination);
            characterController.AddState(UniversalCharacterController.CharacterState.Moving);
            characterController.RemoveState(UniversalCharacterController.CharacterState.Idle);
            Debug.Log($"{characterController.characterName} is moving to {location}");
        }
    }

    private void PerformLocationAction()
    {
        if (currentLocationManager == null || characterController == null || aiManager == null) return;

        List<LocationManager.LocationAction> availableActions = currentLocationManager.GetAvailableActions(characterController.aiSettings.characterRole);
        if (availableActions.Count == 0) return;

        LocationManager.LocationAction selectedAction = ChooseBestAction(availableActions);
        aiManager.ConsiderCollaboration(selectedAction);
        ExecuteAction(selectedAction);
    }

    private void ExecuteAction(LocationManager.LocationAction action)
    {
        if (characterController == null || currentLocationManager == null) return;

        characterController.StartAction(action);
        characterController.RemoveState(UniversalCharacterController.CharacterState.Idle);
        
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
                InitiateInteraction(target);
            }
        }
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
        characterController.RemoveState(UniversalCharacterController.CharacterState.Idle);
        if (target.IsPlayerControlled)
        {
            aiManager.InitiateDialogueWithPlayer(target);
        }
        else
        {
            DialogueManager.Instance?.TriggerNPCDialogue(characterController, target);
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
        characterController.RemoveState(UniversalCharacterController.CharacterState.Idle);

        DialogueManager.Instance?.AddToChatLog(characterController.characterName, $"{characterController.characterName} is {action.description}");
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
            characterController.RemoveState(UniversalCharacterController.CharacterState.Idle);

            DialogueManager.Instance?.AddToChatLog(characterController.characterName, $"{characterController.characterName} is {action.description}");
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

            navMeshAgent.SetDestination(finalPosition);
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