using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using Photon.Pun;

public class AIController : MonoBehaviourPunCallbacks
{
    private UniversalCharacterController characterController;
    private NavMeshAgent navMeshAgent;
    private LocationManager locationManager;
    private OpenAIService openAIService;

    private Vector3 targetPosition;
    private float decisionCooldown = 5f;
    private float lastDecisionTime;

    private void Awake()
    {
        navMeshAgent = gameObject.AddComponent<NavMeshAgent>();
        locationManager = FindObjectOfType<LocationManager>();
        openAIService = FindObjectOfType<OpenAIService>();
    }

    public void Initialize(UniversalCharacterController controller)
    {
        characterController = controller;
        navMeshAgent.speed = characterController.walkSpeed;
        navMeshAgent.angularSpeed = characterController.rotationSpeed;
        navMeshAgent.acceleration = 8f;
        navMeshAgent.stoppingDistance = 0.1f;
    }

    private void Update()
    {
        if (!photonView.IsMine) return;

        if (Time.time - lastDecisionTime > decisionCooldown)
        {
            MakeDecision();
        }

        UpdateMovement();
    }

    private void MakeDecision()
    {
        lastDecisionTime = Time.time;

        // For now, just choose a random location to move to
        // Location randomLocation = locationManager.GetRandomLocation();
        // SetNewDestination(randomLocation.GetRandomPositionInArea());

        // TODO: Integrate with OpenAIService for more complex decision-making
    }

    private void SetNewDestination(Vector3 destination)
    {
        targetPosition = destination;
        navMeshAgent.SetDestination(targetPosition);
        characterController.SetState(UniversalCharacterController.CharacterState.Moving);
    }

    private void UpdateMovement()
    {
        if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance < 0.1f)
        {
            characterController.SetState(UniversalCharacterController.CharacterState.Idle);
        }
    }

    public Vector3 GetTargetPosition()
    {
        return targetPosition;
    }

    public bool ShouldJump()
    {
        // For now, AI characters don't jump. This can be expanded later if needed.
        return false;
    }

    public string[] GetDialogueOptions()
    {
        // TODO: Integrate with OpenAIService for generating dialogue options
        // For now, return some placeholder options
        return new string[]
        {
            "I'm working on the current challenge.",
            "What do you think about our progress?",
            "I have some ideas to share."
        };
    }
}