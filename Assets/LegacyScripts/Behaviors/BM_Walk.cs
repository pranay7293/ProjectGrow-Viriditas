using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// this is a move behavior where the entity moves along the ground:  walking, running, slithering, etc..
// it requires the entity to have a character controller component.
[RequireComponent(typeof(NavMeshAgent))]
public class BM_Walk : B_Move
{
    public float moveSpeed;
    public float rotationSpeed;
    public float gravityMultiplier = 3f;   // this helps entities remain snapped to the ground more reliably (when not hovering)
    public float hoverHeight = 0f;  // if >0,  instead of snapping to the terrain, hover this height above the terrain

    private NavMeshAgent agent;

    private Vector3? lastInvalidPosition = null;

    protected override void Awake()
    {
        base.Awake();

        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
            Debug.LogWarning($"Entity {gameObject} with wander behavior does not have a NavMeshAgent.");

        paused = true;  // walking movement only happens when paused = false
    }

    public override bool InitiateMoveTowardsDestination(Vector3 destination, float sampleRange = 1f, float speedFactor = 1f, Func<Vector3, Vector3> fallbackPositionGetter = null)
    {
        if (DEBUG_Verbose)
            Debug.Log($"Move_Walk behavior on Entity {gameObject} starting to move towards destination {destination}");

        // Sample the navmesh with the given precision to get a more accurate destination.
        if (NavMesh.SamplePosition(destination, out var hit, sampleRange, NavMesh.AllAreas))
        {
            destination = hit.position;
        }
        else
        {
            if (fallbackPositionGetter != null)
            {
                return InitiateMoveTowardsDestination(fallbackPositionGetter(destination), sampleRange, speedFactor);
            }
            lastInvalidPosition = destination;
            Debug.LogError("No valid sampled navmesh point near destination.");
            return false;
        }

        // Check if the path is valid.
        if (!agent.CalculatePath(destination, new NavMeshPath()))
        {
            if (fallbackPositionGetter != null)
            {
                return InitiateMoveTowardsDestination(fallbackPositionGetter(destination), sampleRange, speedFactor);
            }
            lastInvalidPosition = destination;
            Debug.LogError("No valid path to destination.");
            return false;
        }

        currentDestination = destination;
        travelDuration = 0f;
        paused = false;

        // Configure the agent.
        agent.speed = moveSpeed * speedFactor;
        agent.angularSpeed = rotationSpeed * speedFactor;
        agent.destination = destination;

        return true;
    }

    public override void SetAvoidancePriority(int priority)
    {
        agent.avoidancePriority = priority;
    }

    private void Update()
    {
        if (paused || !behaviorEnabled)
        {
            // Pause the agent.
            agent.updatePosition = false;
            agent.updateRotation = false;
            return;
        }
        else
        {
            // Resume the agent.
            agent.updatePosition = true;
            agent.updateRotation = true;
        }

        bool arrived = ArrivedAtDestination();
        bool stuck = IsEntityStuck();
        if (arrived || stuck)
        {
            paused = true;
            if (arrived)
                MovementComplete(true);
            else if (stuck)
                MovementComplete(false);
            return;
        }

        travelDuration += Time.deltaTime;
    }

    private void OnDrawGizmos()
    {
        if (lastInvalidPosition.HasValue)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, lastInvalidPosition.Value);
            Gizmos.DrawSphere(lastInvalidPosition.Value, 0.5f);
        }
    }
}
