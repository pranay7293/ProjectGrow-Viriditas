using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class NPC_Behavior : MonoBehaviour
{
    private UniversalCharacterController characterController;
    private NPC_Data npcData;
    private NavMeshAgent navMeshAgent;

    private float decisionCooldown = 10f;
    private float lastDecisionTime;
    private string currentLocation;

    private float dialogueTimer = 0f;
    private float dialogueInterval = 60f; // Trigger dialogue every 60 seconds

    private List<string> locationNames = new List<string>
    {
        "Medical Bay", "Think Tank", "Media Center", "Innovation Hub", "Maker Space",
        "Research Lab", "Sound Studio", "Gallery", "Biofoundry", "Space Center"
    };

    public void Initialize(UniversalCharacterController controller, NPC_Data data)
    {
        characterController = controller;
        npcData = data;
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
        if (Time.time - lastDecisionTime > decisionCooldown)
        {
            MakeDecision();
        }

        dialogueTimer += Time.deltaTime;
        if (dialogueTimer >= dialogueInterval)
        {
            dialogueTimer = 0f;
            TriggerRandomNPCDialogue();
        }

        UpdateState();
    }

    private void MakeDecision()
    {
        lastDecisionTime = Time.time;
        string randomLocation = GetRandomUnoccupiedLocation();
        MoveToLocation(randomLocation);
    }

    public void ProcessDecision(string decision)
    {
        Debug.Log($"{characterController.characterName} processed decision: {decision}");
        
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
        else if (decision.Contains("work on"))
        {
            string task = decision.Split("work on ")[1];
            WorkOnTask(task);
        }
        else
        {
            // Default behavior if the decision doesn't match any specific action
            MakeDecision();
        }
    }

    private void MoveToLocation(string location)
    {
        Vector3 destination = LocationManager.GetLocationPosition(location);
        if (destination != Vector3.zero)
        {
            navMeshAgent.SetDestination(destination);
            Debug.Log($"{characterController.characterName} is moving to {location}");
            currentLocation = location;
        }
    }

    private void InteractWithTarget(string target)
    {
        Debug.Log($"{characterController.characterName} is interacting with {target}");
        // Implement interaction logic here
        // For example, find the target NPC and trigger a dialogue
    }

    private void WorkOnTask(string task)
    {
        Debug.Log($"{characterController.characterName} is working on {task}");
        // Implement task-specific behavior here
        // For example, update the game state or trigger a mini-game
    }

    private string GetRandomUnoccupiedLocation()
    {
        List<string> availableLocations = new List<string>(locationNames);
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
            return locationNames[Random.Range(0, locationNames.Count)];
        }
    }

    private void UpdateState()
    {
        if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance < 0.1f)
        {
            characterController.SetState(UniversalCharacterController.CharacterState.Idle);
        }
        else
        {
            characterController.SetState(UniversalCharacterController.CharacterState.Moving);
        }
    }

    public Vector3 GetTargetPosition()
    {
        return navMeshAgent.destination;
    }

    private string GetClosestLocation(Vector3 position)
    {
        string closest = locationNames[0];
        float minDistance = float.MaxValue;

        foreach (string location in locationNames)
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

    private void TriggerRandomNPCDialogue()
    {
        UniversalCharacterController targetNPC = GetRandomNearbyNPC();
        if (targetNPC != null)
        {
            DialogueManager.Instance.TriggerNPCDialogue(characterController, targetNPC);
        }
    }

    private UniversalCharacterController GetRandomNearbyNPC()
    {
        UniversalCharacterController[] allCharacters = FindObjectsOfType<UniversalCharacterController>();
        List<UniversalCharacterController> nearbyNPCs = new List<UniversalCharacterController>();

        foreach (UniversalCharacterController character in allCharacters)
        {
            if (!character.IsPlayerControlled && character != characterController)
            {
                float distance = Vector3.Distance(transform.position, character.transform.position);
                if (distance <= characterController.interactionDistance)
                {
                    nearbyNPCs.Add(character);
                }
            }
        }

        if (nearbyNPCs.Count > 0)
        {
            return nearbyNPCs[Random.Range(0, nearbyNPCs.Count)];
        }

        return null;
    }
}

// // this tracks what the NPC is doing right now
// [System.Serializable]
// public class NPC_Behavior
// {
//     public enum NPC_BehaviorType
//     {
//         Idle,
//         Moving,
//         SpeakingTo,  // it's always assumed the NPC is speaking to currentGoal.targetCharacter
//         BeingSpokenTo,
//         PerformingCustomAction,
//         AwaitingInstructions  // waiting to hear back from openAI
//     }

//     public NPC_BehaviorType behaviorType;
//     public string destination;
//     public string beingSpokenToBy;

//     public static NPC_Behavior Idle()
//     {
//         return new NPC_Behavior { behaviorType = NPC_BehaviorType.Idle };
//     }

//     public static NPC_Behavior Moving(string destination)
//     {
//         return new NPC_Behavior { behaviorType = NPC_BehaviorType.Moving, destination = destination };
//     }

//     public static NPC_Behavior Speaking()
//     {
//         return new NPC_Behavior { behaviorType = NPC_BehaviorType.SpeakingTo };
//     }

//     public static NPC_Behavior BeingSpokenTo(string beingSpokenToBy)
//     {
//         return new NPC_Behavior { behaviorType = NPC_BehaviorType.BeingSpokenTo, beingSpokenToBy = beingSpokenToBy };
//     }

//     public static NPC_Behavior PerformingCustomAction()
//     {
//         return new NPC_Behavior { behaviorType = NPC_BehaviorType.PerformingCustomAction };
//     }

//     public static NPC_Behavior AwaitingInstructions()
//     {
//         return new NPC_Behavior { behaviorType = NPC_BehaviorType.AwaitingInstructions };
//     }

//     public new string ToString()
//     {
//         string toReturn = new string(behaviorType.ToString());

//         if (behaviorType == NPC_BehaviorType.Moving)
//             toReturn = toReturn + " destination: " + destination;
//         if (behaviorType == NPC_BehaviorType.BeingSpokenTo)
//             toReturn = toReturn + " beingSpokenToBy: " + beingSpokenToBy;

//         return toReturn;
//     }
// }
