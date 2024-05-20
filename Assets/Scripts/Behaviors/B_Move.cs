using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// the base class for moving behaviors
// this class only handles the notion of trying to go to destinations and arriving at them or not, it doesn't know how specifically that movement happens.
// behaviors that inherit from this should be called BM_* and should implement a specific way to move.

public abstract class B_Move : Behavior
{
    // How close we need to be to consider "arrived" at any given destination.
    [SerializeField] protected float EPSILON_CloseEnoughToDestination = 0.6f;

    // revise on a per-entity basis.  if it takes longer than this to move between destinations, you're probably stuck, so we warn
    [SerializeField] private float DEBUG_maxTimeToTravel = 60f;

    protected bool DEBUG_KeepTryingWhenStuck = false;  // should be false when not debugging

    protected AI_Mover myAIMover;

    // The current destination for the entity, determined by the inherited class.
    public Vector3 currentDestination { get; protected set; }

    // The amount of time we've been traveling to `currentDestination`
    protected float travelDuration;

    // The standard radius to used for "relaxed" navigation where we don't care
    // about exact position precision (fleeing, herding).
    public static float DefaultRelaxedNavmeshSampleRadius = 4f;

    protected override void Awake()
    {
        base.Awake();

        if (myAI is AI_Mover)
            myAIMover = (AI_Mover)myAI;
        else
            Debug.LogError("Entity " + gameObject + " has a B_Move behavior with no AI_Mover. It needs both.");
    }

    protected override void PauseBehavior()
    {
        if (DEBUG_Verbose)
            Debug.Log($"Entity {gameObject} pausing {BehaviorName} behavior");
    }

    protected override void UnpauseBehavior()
    {
        if (DEBUG_Verbose)
            Debug.Log($"Entity {gameObject} unpausing {BehaviorName} behavior");
    }

    // returns true if it has started moving towards the destination and false if it cannot (eg - due to pathfinding)
    // all classes that inherit from this should overwrite this method
    public virtual bool InitiateMoveTowardsDestination(Vector3 destination, float sampleRange = 1f, float speedFactor = 1f, Func<Vector3, Vector3> fallbackPositionGetter = null)
    {
        return true;
    }

    // subclasses should override this if they support avoidance.
    public virtual void SetAvoidancePriority(int priority) { }

    // member classes should call this when the entity arrives at its destination or when it gives up trying
    protected virtual void MovementComplete(bool arrivedAtDestination)
    {
        if (arrivedAtDestination)
            myAIMover.EntityHasArrivedAtDestination();
        else
            myAIMover.EntityCantGetToDestination();
    }

    // checks how far away from currentDestination we are, not taking y axis into consideration (so it's independent of how tall the creature is or where the node is vertically)
    protected bool ArrivedAtDestination()
    {
        float distanceToDestination = Vector3.Distance(new Vector3(transform.position.x, 0f, transform.position.z),
            new Vector3(currentDestination.x, 0f, currentDestination.z));

        var arrived = distanceToDestination < EPSILON_CloseEnoughToDestination;

        if (arrived && DEBUG_Verbose)
            Debug.Log($"{BehaviorName} behavior on Entity {gameObject} arrived at destination {currentDestination}.");

        return arrived;
    }

    // Checks if the entity has been traveling long enough to be considered stuck.
    protected bool IsEntityStuck()
    {
        if (travelDuration < DEBUG_maxTimeToTravel)
        {
            return false;
        }

        if (DEBUG_KeepTryingWhenStuck)
        {
            Debug.LogWarning($"Entity {gameObject} has been moving to point {currentDestination} for over {DEBUG_maxTimeToTravel} seconds and may be stuck. He won't give up, though, so come see what he's doing.");
            // if DEBUG_PauseOnWarningOrErrors component exists and is enabled, it'll pause here
            return false;
        }

        Debug.LogWarning($"Entity {gameObject} has been moving to point {currentDestination} for over {DEBUG_maxTimeToTravel} seconds and may be stuck. Reporting failure.");
        return true;
    }
}
