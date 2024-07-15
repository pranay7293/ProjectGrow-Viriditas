using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class NPC_Behavior : MonoBehaviour
{
    private UniversalCharacterController characterController;
    private NPC_Data npcData;
    private NavMeshAgent navMeshAgent;

    private float decisionCooldown = 10f;
    private float lastDecisionTime;
    private bool isInitialized = false;
    private float stayDuration = 30f; 
    private float stayTimer = 0f;
    private float moveDelay = 0f;

    private List<string> locationNames = new List<string>
    {
        "Medical Bay", "Think Tank", "Media Center", "Innovation Hub", "Maker Space",
        "Research Lab", "Sound Studio", "Gallery", "Biofoundry", "Space Center"
    };

    private string currentLocation;

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
        isInitialized = true;
        currentLocation = GetClosestLocation(transform.position);
    }

    public void UpdateBehavior()
    {
        if (!isInitialized) return;

        if (Time.time - lastDecisionTime > decisionCooldown)
        {
            MakeDecision();
        }

        UpdateState();
    }

    private void MakeDecision()
    {
        if (stayTimer > 0)
        {
            stayTimer -= Time.deltaTime;
            return;
        }

        if (moveDelay > 0)
        {
            moveDelay -= Time.deltaTime;
            return;
        }

        lastDecisionTime = Time.time;
        string randomLocation = GetRandomUnoccupiedLocation();
        Vector3 destination = LocationManager.GetLocationPosition(randomLocation);
        
        if (destination != Vector3.zero)
        {
            navMeshAgent.SetDestination(destination);
            Debug.Log($"{characterController.characterName} is moving to {randomLocation}");
            currentLocation = randomLocation;
            stayTimer = stayDuration;
            moveDelay = Random.Range(1f, 5f); // Random delay before next move
        }
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

    public bool ShouldJump()
    {
        return false;
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
