using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// A move behavior where the entity can freely fly in 3d space.
// Note that this is currently not very intelligent, works best when B_Wander is
// configured to visit nodes in order so that a "patrol route" can be defined.

public class BM_Fly : B_Move
{
    [SerializeField] private float flySpeed;
    [SerializeField] private float rotationSpeed;
    [SerializeField] private float decelerationRange = 1f;

    private float requestedSpeedFactor = 1f;

    protected override void Awake()
    {
        base.Awake();
        paused = true;  // walking movement only happens when paused = false
    }

    public override bool InitiateMoveTowardsDestination(Vector3 destination, float sampleRange = 1f, float speedFactor = 1f, Func<Vector3, Vector3> fallbackPositionGetter = null)
    {
        if (DEBUG_Verbose)
            Debug.Log($"Move_Fly behavior on Entity {gameObject} starting to move towards destination {destination}");

        // No pathfinding checks for now, assume we can always fly to the destination.

        currentDestination = destination;
        travelDuration = 0f;
        paused = false;
        requestedSpeedFactor = speedFactor;
        return true;
    }

    private void Update()
    {
        if (paused || !behaviorEnabled)
            return;

        if (ArrivedAtDestination() || IsEntityStuck())
        {
            paused = true;
            MovementComplete(true);
            return;
        }

        travelDuration += Time.deltaTime;

        // Calculate the direction to the destination.
        var distanceToTarget = Vector3.Distance(currentDestination, transform.position);
        var directionToTarget = Vector3.Normalize(currentDestination - transform.position);

        // Smoothly rotate the Entity's rotation towards the destination rotation (in all axes).
        transform.rotation = WorldRep.SmoothRotateTowards(transform.rotation, directionToTarget, rotationSpeed * Time.deltaTime);

        // Apply deceleration near target.
        var computedFlySpeed = requestedSpeedFactor * flySpeed;
        var speed = Mathf.Lerp(computedFlySpeed / 3f, computedFlySpeed, distanceToTarget - decelerationRange);

        // Always move in the direction we're currently facing.
        Vector3 currentVelocity = transform.forward * speed * Time.deltaTime;
        transform.position += currentVelocity;
    }
}
