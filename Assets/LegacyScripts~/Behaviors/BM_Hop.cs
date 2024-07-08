using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// comparable to BM_Walk, this one moves by hopping instead of sliding along the ground
// it requires the entity to have a rigidbody

public class BM_Hop : B_Move
{
    public float minTimeBetweenHops;
    public float maxTimeBetweenHops;
    private float currentTimeWaitingToHop;
    private float currentTargetDurationBeforeHopping;

    public float hopForceMin;
    public float hopForceMax;
    public float hopAngleMin;  // this is vertical angle, relative to the flat ground
    public float hopAngleMax;
    public float hopDirectionInaccuracy; // the facing towards the target with be off to the L/R randomly by at most this amount (should be positive)

    public float percentageHighHops;  // between 0f and 1f.  high hops happen this frequently and are good to get up steeper hills.  TODO - it'd be better to detect that you're trying to head up a steep hill and use high hops then (or generally adjust angle for steepness)
    public float highHopAngleMin;
    public float highHopAngleMax;
    public float highHopForceMultiplier = 1f;

    public bool faceTargetWhileHopping = false;

    private PathfindingHelper pathfindingHelper;
    private float requestedSpeedFactor = 1f;

    private Rigidbody myRigidBody;
    private Collider myCollider;

    protected override void Awake()
    {
        base.Awake();

        pathfindingHelper = new PathfindingHelper();

        myRigidBody = GetComponentInChildren<Rigidbody>();
        if (myRigidBody == null)
            Debug.LogError("Entity " + gameObject + " with Move_Hop behavior does not have RigidBody component");

        myCollider = GetComponent<Collider>();

        paused = true;  //  movement only happens when paused = false
    }

    public override bool InitiateMoveTowardsDestination(Vector3 destination, float sampleRange = 1f, float speedFactor = 1f, System.Func<Vector3, Vector3> fallbackPositionGetter = null)
    {
        if (DEBUG_Verbose)
            Debug.Log($"Move_Hop behavior on Entity {gameObject} starting to move towards destination {destination}");

        // Do pathfinding to determine if we can start moving towards destination, return false if not.
        if (!pathfindingHelper.CalculatePath(transform.position, destination))
        {
            Debug.LogError("No valid valid path to destination.");
            return false;
        }

        currentDestination = destination;
        travelDuration = 0f;
        currentTargetDurationBeforeHopping = -1f;  // the first hop can happen as soon as the behavior is initiated
        currentTimeWaitingToHop = 0f;
        paused = false;
        requestedSpeedFactor = speedFactor;
        return true;
    }

    private void FixedUpdate()
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
        currentTimeWaitingToHop += Time.deltaTime;

        if (currentTimeWaitingToHop < currentTargetDurationBeforeHopping)
            return;

        // only hop if you're on the ground, or if it's been a long time since you last hopped
        if (myCollider != null)
            if ((!WorldRep.IsGrounded(myCollider)) && (currentTimeWaitingToHop < 15f))
                return;

        // if you get down here, it's time to hop
        currentTargetDurationBeforeHopping = Random.Range(minTimeBetweenHops, maxTimeBetweenHops) / requestedSpeedFactor;
        currentTimeWaitingToHop = 0f;

        Hop();
    }


    private void Hop()
    {
        // Get the next pathfinding destination.
        var destination = pathfindingHelper.GetNextDestination(transform.position, EPSILON_CloseEnoughToDestination) ?? currentDestination;

        // determine Y rotation to point at target
        GameObject go = new GameObject();  // we only use this to call LookAt() to calculate y_rot
        go.transform.position = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y, gameObject.transform.position.z);
        go.transform.LookAt(destination);
        float y_rot = go.transform.rotation.eulerAngles.y;
        GameObject.Destroy(go);

        y_rot += Random.Range(-hopDirectionInaccuracy, hopDirectionInaccuracy);


        // determine X rotation, which is artillery-style angle reltive to the flat plane of the horizon at which to hop
        // determine hop force at the same time
        float x_rot, hop_force;
        if (Random.Range(0f, 1f) < percentageHighHops)
        {
            // high hop
            x_rot = Random.Range(highHopAngleMin, highHopAngleMax);
            hop_force = Random.Range(hopForceMin, hopForceMax) * highHopForceMultiplier;
        }
        else
        {
            // regular hop
            x_rot = Random.Range(hopAngleMin, hopAngleMax);
            hop_force = Random.Range(hopForceMin, hopForceMax);
        }

        if (faceTargetWhileHopping)
        {
            Vector3 eulerRot = transform.rotation.eulerAngles;
            transform.rotation = Quaternion.Euler(eulerRot.x, y_rot, eulerRot.z);
        }

        // construct the force vector and apply it
        Vector3 forceVector = Vector3.up;
        forceVector = Quaternion.Euler(x_rot, y_rot, 0f) * forceVector * hop_force;

        myRigidBody.AddForce(forceVector);
    }

    private void OnDrawGizmosSelected()
    {
        pathfindingHelper?.DrawDebugGizmos();
    }
}
