using UnityEngine;
using UnityEngine.AI;

public class NPC_Behavior : MonoBehaviour
{
    private UniversalCharacterController characterController;
    private NPC_Data npcData;
    private NavMeshAgent navMeshAgent;

    private float decisionCooldown = 10f;
    private float lastDecisionTime;
    private bool isInitialized = false;

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
        lastDecisionTime = Time.time;
        // TODO: Implement decision-making logic here
        // You can use npcData and call NPC_openAI methods to generate decisions
    }

    private void UpdateState()
    {
        if (navMeshAgent.remainingDistance < 0.1f)
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
        // TODO: Implement jump logic if needed
        return false;
    }

    // TODO: Add methods for moving to locations, interacting with characters, etc.
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
