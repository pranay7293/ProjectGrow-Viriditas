using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;

public class NPC_Behavior : MonoBehaviourPunCallbacks
{
    private UniversalCharacterController characterController;
    private NPC_Data npcData;
    private NavMeshAgent navMeshAgent;
    private AIManager aiManager;

    private float lastDecisionTime;
    private float interactionCooldown = 30f;
    private float lastInteractionTime;

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
        lastDecisionTime = Time.time;
        lastInteractionTime = Time.time;
    }

    public void UpdateBehavior()
    {
        if (Time.time - lastDecisionTime > characterController.aiSettings.decisionInterval)
        {
            MakeDecision();
        }

        if (characterController.GetState() == UniversalCharacterController.CharacterState.Moving)
        {
            if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance < 0.1f)
            {
                characterController.SetState(UniversalCharacterController.CharacterState.Idle);
            }
        }

        if (Time.time - lastInteractionTime > interactionCooldown)
        {
            AttemptNPCInteraction();
        }
    }

    private void MakeDecision()
    {
        lastDecisionTime = Time.time;
        
        List<string> options = new List<string>
        {
            "Move to new location",
            "Perform location action",
            "Interact with NPC",
            "Work on current challenge",
            "Pursue personal goal"
        };

        GameState currentState = GameManager.Instance.GetCurrentGameState();
        string decision = aiManager.MakeDecision(options, currentState);

        switch (decision)
        {
            case "Move to new location":
                MoveToNewLocation();
                break;
            case "Perform location action":
                PerformLocationAction();
                break;
            case "Interact with NPC":
                AttemptNPCInteraction();
                break;
            case "Work on current challenge":
                WorkOnChallenge();
                break;
            case "Pursue personal goal":
                PursuePersonalGoal();
                break;
        }
    }

    private void MoveToNewLocation()
    {
        string newLocation = LocationManagerMaster.Instance.GetTargetLocation(GameManager.Instance.GetCurrentChallenge().milestones);
        MoveToLocation(newLocation);
    }

    private void MoveToLocation(string location)
    {
        Vector3 destination = LocationManagerMaster.Instance.GetLocationPosition(location);
        if (destination != Vector3.zero)
        {
            navMeshAgent.SetDestination(destination);
            characterController.SetState(UniversalCharacterController.CharacterState.Moving);
            Debug.Log($"{characterController.characterName} is moving to {location}");
        }
    }

    private void PerformLocationAction()
    {
        if (currentLocationManager == null) return;

        List<LocationManager.LocationAction> availableActions = currentLocationManager.GetAvailableActions(characterController.aiSettings.characterRole);
        if (availableActions.Count == 0) return;

        LocationManager.LocationAction selectedAction = ChooseBestAction(availableActions);
        aiManager.ConsiderCollaboration(selectedAction);
        ExecuteAction(selectedAction);
    }

    private void ExecuteAction(LocationManager.LocationAction action)
    {
        characterController.photonView.RPC("StartAction", RpcTarget.All, action.actionName);
        
        ActionLogManager.Instance.LogAction(characterController.characterName, $"performing {action.actionName} at {currentLocationManager.locationName}");
    }

    private LocationManager.LocationAction ChooseBestAction(List<LocationManager.LocationAction> actions)
    {
        GameState currentState = GameManager.Instance.GetCurrentGameState();
        List<string> personalGoals = aiManager.GetPersonalGoals();
        string currentChallenge = currentState.CurrentChallenge.title;

        var scoredActions = actions.Select(action => new
        {
            Action = action,
            Score = ScoreAction(action, personalGoals, currentChallenge)
        }).ToList();

        return scoredActions.OrderByDescending(sa => sa.Score).First().Action;
    }

    private float ScoreAction(LocationManager.LocationAction action, List<string> personalGoals, string currentChallenge)
    {
        float score = 0;

        if (personalGoals.Any(goal => action.actionName.ToLower().Contains(goal.ToLower())))
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

    private void AttemptNPCInteraction()
    {
        List<UniversalCharacterController> nearbyNPCs = GetNearbyNPCs();
        if (nearbyNPCs.Count > 0)
        {
            UniversalCharacterController target = ChooseInteractionTarget(nearbyNPCs);
            InitiateNPCInteraction(target);
        }
    }

    private List<UniversalCharacterController> GetNearbyNPCs()
    {
        return GameManager.Instance.GetAllCharacters()
            .Where(npc => npc != characterController && 
                   Vector3.Distance(transform.position, npc.transform.position) <= characterController.interactionDistance)
            .ToList();
    }

    private UniversalCharacterController ChooseInteractionTarget(List<UniversalCharacterController> nearbyNPCs)
    {
        return nearbyNPCs.OrderBy(npc => npcData.GetRelationship(npc.characterName)).First();
    }

    private void InitiateNPCInteraction(UniversalCharacterController target)
    {
        lastInteractionTime = Time.time;
        DialogueManager.Instance.TriggerNPCDialogue(characterController, target);
    }

    private void WorkOnChallenge()
    {
        string currentChallenge = GameManager.Instance.GetCurrentChallenge().title;
        characterController.photonView.RPC("StartAction", RpcTarget.All, $"Working on {currentChallenge}");

        string detailedAction = $"focusing intensely on solving {currentChallenge}";
        DialogueManager.Instance.AddToChatLog(characterController.characterName, $"{characterController.characterName} is {detailedAction}");
    }

    private void PursuePersonalGoal()
    {
        List<string> personalGoals = aiManager.GetPersonalGoals();
        Dictionary<string, bool> personalGoalCompletion = aiManager.GetPersonalGoalCompletion();
        
        string incompleteGoal = personalGoals.FirstOrDefault(goal => !personalGoalCompletion[goal]);
        
        if (incompleteGoal != null)
        {
            characterController.photonView.RPC("StartAction", RpcTarget.All, $"Pursuing personal goal: {incompleteGoal}");

            DialogueManager.Instance.AddToChatLog(characterController.characterName, $"{characterController.characterName} is focusing on their personal goal: {incompleteGoal}");
        }
        else
        {
            WorkOnChallenge();
        }
    }

    public void SetCurrentLocation(LocationManager location)
    {
        currentLocationManager = location;
    }
}