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
        currentLocation = GetClosestLocation(transform.position);
    }

    public void UpdateBehavior()
    {
        if (Time.time - lastDecisionTime > characterController.aiSettings.decisionInterval)
        {
            MakeDecision();
        }

        if (currentAction != null && Time.time - lastDecisionTime > actionCooldown)
        {
            CompleteAction();
        }

        UpdateState();
    }

    private void MakeDecision()
    {
        lastDecisionTime = Time.time;
        
        if (ShouldMoveToNewLocation())
        {
            string newLocation = GetTargetLocation();
            MoveToLocation(newLocation);
        }
        else
        {
            PerformLocationAction();
        }
    }

    private bool ShouldMoveToNewLocation()
    {
        return Random.value < 0.3f; // 30% chance to move to a new location
    }

    private string GetTargetLocation()
    {
        List<string> subgoals = GameManager.Instance.GetCurrentSubgoals();

        foreach (string location in LocationManager.GetAllLocations())
        {
            List<string> locationActions = LocationManager.GetLocationActions(location);
            if (locationActions.Any(action => subgoals.Any(subgoal => action.ToLower().Contains(subgoal.ToLower()))))
            {
                return location;
            }
        }

        return GetRandomUnoccupiedLocation();
    }

    private void MoveToLocation(string location)
    {
        Vector3 destination = LocationManager.GetLocationPosition(location);
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
        List<string> actions = LocationManager.GetLocationActions(currentLocation);
        if (actions.Count > 0)
        {
            currentAction = ChooseBestAction(actions);
            characterController.PerformAction(currentAction);
            Debug.Log($"{characterController.characterName} is {currentAction} at {currentLocation}");
        }
    }

    private string ChooseBestAction(List<string> actions)
    {
        List<string> subgoals = GameManager.Instance.GetCurrentSubgoals();
        foreach (string action in actions)
        {
            if (subgoals.Any(subgoal => action.ToLower().Contains(subgoal.ToLower())))
            {
                return action;
            }
        }
        return actions[Random.Range(0, actions.Count)];
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

    private string GetRandomUnoccupiedLocation()
    {
        List<string> availableLocations = new List<string>(LocationManager.GetAllLocations());
        availableLocations.Remove(currentLocation);

        foreach (var character in GameManager.Instance.GetAllCharacters())
        {
            if (character != this.characterController)
            {
                NPC_Behavior npcBehavior = character.GetComponent<NPC_Behavior>();
                if (npcBehavior != null)
                {
                    availableLocations.Remove(npcBehavior.currentLocation);
                }
            }
        }

        if (availableLocations.Count > 0)
        {
            return availableLocations[Random.Range(0, availableLocations.Count)];
        }
        else
        {
            return LocationManager.GetAllLocations()[Random.Range(0, LocationManager.GetAllLocations().Count)];
        }
    }

    private void UpdateState()
    {
        if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance < 0.1f)
        {
            characterController.SetState(UniversalCharacterController.CharacterState.Idle);
        }
    }

    private string GetClosestLocation(Vector3 position)
    {
        string closest = LocationManager.GetAllLocations()[0];
        float minDistance = float.MaxValue;

        foreach (string location in LocationManager.GetAllLocations())
        {
            Vector3 locationPosition = LocationManager.GetLocationPosition(location);
            float distance = Vector3.Distance(position, locationPosition);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = location;
            }
        }

        return closest;
    }

    public void ProcessDecision(string decision)
    {
        if (decision.Contains("move to"))
        {
            string location = decision.Split("move to ")[1];
            MoveToLocation(location);
        }
        else if (decision.Contains("interact with"))
        {
            string target = decision.Split("interact with ")[1];
            InteractWithTarget(target);
        }
        else
        {
            PerformLocationAction();
        }

        UpdateEmotionalState(decision);
    }

    private void UpdateEmotionalState(string decision)
    {
        if (decision.Contains("collaborate") || decision.Contains("help"))
        {
            npcData.UpdateEmotionalState(EmotionalState.Happy);
        }
        else if (decision.Contains("compete") || decision.Contains("challenge"))
        {
            npcData.UpdateEmotionalState(EmotionalState.Confident);
        }
        else if (decision.Contains("research") || decision.Contains("study"))
        {
            npcData.UpdateEmotionalState(EmotionalState.Neutral);
        }
    }

    private void InteractWithTarget(string targetName)
    {
        UniversalCharacterController target = GameManager.Instance.GetCharacterByName(targetName);
        if (target != null)
        {
            float relationshipValue = npcData.GetRelationship(targetName);
            string interactionType = relationshipValue > 0.5f ? "collaborate with" :
                                     relationshipValue < -0.5f ? "debate with" : "discuss with";
            
            Debug.Log($"{characterController.characterName} decides to {interactionType} {targetName}");
            DialogueManager.Instance.TriggerNPCDialogue(characterController, target);

            float relationshipChange = Random.Range(-0.1f, 0.1f);
            npcData.UpdateRelationship(targetName, relationshipChange);
        }
    }
}