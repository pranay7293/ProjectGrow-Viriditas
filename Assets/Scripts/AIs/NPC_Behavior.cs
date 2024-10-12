using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using ProjectGrow.AI;

public class NPC_Behavior : MonoBehaviourPunCallbacks
{
    private UniversalCharacterController characterController;
    private NPC_Data npcData;
    private NavMeshAgent navMeshAgent;
    private AIManager aiManager;

    [SerializeField] private float interactionCooldown = 10f;
    [SerializeField] private float interactionDistance = 5f;
    [SerializeField] private float interactionPauseTime = 3f;
    [SerializeField] private float waypointPauseTime = 2f;
    [SerializeField] private float locationChangeCooldown = 5f;
    [SerializeField] private float backgroundThinkingInterval = 5f;

    private float lastInteractionTime;
    private float lastInteractionPauseTime = 0f;
    private float lastLocationChangeTime = 0f;
    private float lastBackgroundThinkingTime;

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
        lastInteractionTime = Time.time;
        lastBackgroundThinkingTime = Time.time;
    }

    private void Update()
    {
        if (!photonView.IsMine) return;
        if (characterController == null || aiManager == null) return;

        UpdateBehavior();
        PerformBackgroundThinking();
    }

    public void UpdateBehavior()
    {
        if (isAcclimating) return;

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
            characterController.AddState(CharacterState.Idle);
        }
    }
}