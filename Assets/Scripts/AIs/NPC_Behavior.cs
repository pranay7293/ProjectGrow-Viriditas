using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Linq;

public class NPC_Behavior : MonoBehaviour
{
    private UniversalCharacterController characterController;
    private NPC_Data npcData;
    private NavMeshAgent navMeshAgent;
    private AIManager aiManager;

    private float lastDecisionTime;
    private string currentLocation;
    private string currentAction;
    private float actionCooldown = 5f;
    private float actionDuration = 3f;
    private float actionStartTime;

    private float interactionCooldown = 30f;
    private float lastInteractionTime;

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
        currentLocation = LocationManagerMaster.Instance.GetClosestLocation(transform.position);
        lastDecisionTime = Time.time;
        lastInteractionTime = Time.time;
    }

    public void UpdateBehavior()
    {
        if (Time.time - lastDecisionTime > characterController.aiSettings.decisionInterval)
        {
            MakeDecision();
        }

        if (currentAction != null)
        {
            if (Time.time - actionStartTime > actionDuration)
            {
                CompleteAction();
            }
        }

        UpdateState();
        
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
            currentLocation = location;
        }
    }

    private void PerformLocationAction()
    {
        List<string> actions = LocationManagerMaster.Instance.GetLocationActions(currentLocation);
        if (actions.Count > 0)
        {
            currentAction = ChooseBestAction(actions);
            characterController.PerformAction(currentAction);
            actionStartTime = Time.time;
            Debug.Log($"{characterController.characterName} is {currentAction} at {currentLocation}");

            string detailedAction = GenerateDetailedAction(currentAction);
            DialogueManager.Instance.AddToChatLog(characterController.characterName, $"{characterController.characterName} is {detailedAction} at {currentLocation}");
        }
    }

    private string ChooseBestAction(List<string> actions)
    {
        List<string> milestones = GameManager.Instance.GetCurrentChallenge().milestones;
        foreach (string action in actions)
        {
            if (milestones.Any(milestone => action.ToLower().Contains(milestone.ToLower())))
            {
                return action;
            }
        }
        return actions[Random.Range(0, actions.Count)];
    }

    private string GenerateDetailedAction(string baseAction)
    {
        string role = characterController.aiSettings.characterRole;
        string personality = characterController.aiSettings.characterPersonality;

        Dictionary<string, string> detailedActions = new Dictionary<string, string>
        {
            {"Researching", $"conducting advanced {role.ToLower()} research"},
            {"Experimenting", $"running complex {role.ToLower()} experiments"},
            {"Analyzing", $"performing in-depth {role.ToLower()} analysis"},
            {"Collaborating", $"engaging in {personality.ToLower()} collaboration with colleagues"},
            {"Innovating", $"developing cutting-edge {role.ToLower()} innovations"}
        };

        if (detailedActions.TryGetValue(baseAction, out string detailedAction))
        {
            return detailedAction;
        }

        return $"working on {baseAction.ToLower()} tasks";
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
        characterController.PerformAction($"Working on {currentChallenge}");
        actionStartTime = Time.time;
        Debug.Log($"{characterController.characterName} is working on {currentChallenge}");

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
            characterController.PerformAction($"Pursuing personal goal: {incompleteGoal}");
            actionStartTime = Time.time;
            Debug.Log($"{characterController.characterName} is pursuing personal goal: {incompleteGoal}");

            DialogueManager.Instance.AddToChatLog(characterController.characterName, $"{characterController.characterName} is focusing on their personal goal: {incompleteGoal}");
        }
        else
        {
            WorkOnChallenge(); // If all personal goals are complete, work on the main challenge
        }
    }

    private void CompleteAction()
    {
        if (currentAction != null)
        {
            GameManager.Instance.UpdateGameState(characterController.characterName, currentAction);
            currentAction = null;
            characterController.SetState(UniversalCharacterController.CharacterState.Idle);
        }
    }

    private void UpdateState()
    {
        if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance < 0.1f)
        {
            characterController.SetState(UniversalCharacterController.CharacterState.Idle);
        }
    }
}