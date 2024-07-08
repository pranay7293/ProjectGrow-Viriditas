using System.Collections;
using System.Collections.Generic;
using Systems.Scent;
using UnityEngine;
using ListExtensions;
using System.Linq;
using Systems.Herding;


// Wander behavior involves picking a destination at random and moving to it, waiting at that node for some time, then picking a new destination.
// Nodes are set up in Unity as a folder (GO) full of Transforms (other GOs).
// This class calls InitiateMoveTowardsDestination() on the B_Move derived class which is on this same Entity.  In other words, it is abstracted
// out exactly how the Entity moves between points, but we get callbacks when the Entity arrives at the destination node or if it gives
// up (eg - gets stuck, pathing fails, etc.).

// TODO - at some point I'd like to extend this with SubBehaviors (see RiseInAir for an example) where the new SubBehavior is "wander around point"
// which doesn't need a folder of nodes and instead just picks a random point within a radius of the passed-in point.

public class B_Wander : Behavior, IHerdAgent
{
    public GameObject folderOfNodes;
    public bool snapNodesToTerrain = true;
    public bool visitNodesInOrder = false;
    public bool avoidNodesNearPlayer = true;
    public float minTimeAtNode, maxTimeAtNode;

    private bool waitingAtNode;  // if false, entity is moving between nodes
    private float waitDurationSoFar;  // how long have i been waiting at this node so far
    private float waitDurationTarget;  // how long do i have to wait at this node before moving again

    private List<Transform> nodes;
    private int lastVisitedOrderedNodeIndex; // used when `visitNodesInOrder` is enabled.
    private bool validData;

    private B_Move myMover;
    private AI_Mover myAIMover;
    private bool trackPaused;

    private Transform player;

    // Enables herding behavior if set.
    public IHerdingProfile HerdingProfile { get; set; } = null;
    public Vector3? herdWanderDestination { get; private set; }
    public int herdGroupId { get; set; }

    // For now the herding system is a static field on B_Wander but eventually
    // it should be broken out into a centralized game object.
    private static HerdingSystem herdingSystem = new HerdingSystem();

    protected override void PauseBehavior()
    {
        // keep track of whether my move behavior was paused, and then pause it if it wasn't paused
        trackPaused = myMover.paused;
        myMover.paused = true;

        if (DEBUG_Verbose)
            Debug.Log("Entity " + gameObject + " pausing Wander behavior.");
    }
    protected override void UnpauseBehavior()
    {
        // return the move behavior to the state it was in before Wander behavior was pasued
        myMover.paused = trackPaused;

        if (DEBUG_Verbose)
            Debug.Log("Entity " + gameObject + " unpausing Wander behavior.");
    }

    protected override void Awake()
    {
        base.Awake();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        myMover = GetComponent<B_Move>();
        if (myMover == null)
            Debug.LogWarning("Entity " + gameObject + " has a B_Wander component but no component that derives from B_Move.");

        if (myAI is AI_Mover)
            myAIMover = (AI_Mover)myAI;

        nodes = new List<Transform>();
        if (folderOfNodes != null)
        {
            foreach (Transform child in folderOfNodes.transform)
            {
                nodes.Add(child);
                if (snapNodesToTerrain)
                {
                    // move each node to be above the terrain
                    float height = WorldRep.TerrainYHeightAtPosition(child.position);
                    child.position = new Vector3(child.position.x, height + 0.1f, child.position.z);
                }
            }
        }

        validData = false;

        if (nodes.Count < 2)
        {
            Debug.LogError("Entity " + gameObject + " with wander behavior only has " + nodes.Count + " nodes in its folderOfNodes. It needs at least 2.");
        }
        else
        {
            // make sure all nodes are not identical which could cause an infinite loop
            Transform t = nodes[0];
            foreach (Transform node in nodes)
                if (t != node)
                    validData = true;
            if (!validData)
                Debug.LogError("Enity " + gameObject + " with wander behavior has a list of nodes which are all the same - wander behavior won't happen.");
        }

        waitingAtNode = true;  // all Entities with this behavior start in waitingAtNode mode
        waitDurationTarget = Random.Range(minTimeAtNode, maxTimeAtNode);
        waitDurationSoFar = waitDurationTarget;

        herdingSystem.AssignHerdGroupId(this);
    }

    private void Update()
    {
        if (!behaviorEnabled || !validData)
            return;

        // Try herding if we're configured to do so. If there's no pack neardby
        // to herd with, continue our normal wandering behavior.
        if (HerdingProfile != null && !paused)
        {
            if (Herd(Time.deltaTime))
            {
                return;
            };
        }

        if (waitingAtNode)
        {
            waitDurationSoFar += Time.deltaTime;

            // putting paused check down here instead of up top means that time spent paused DOES count towards waiting at a node.
            // therefore if you pause and then much later unpause a creature who is waiting at a node, it'll immediately start walking to a new node, which seems right
            if (paused)
                return;


            if (waitDurationSoFar > waitDurationTarget)
            {
                waitingAtNode = false;

                // pick a new destination from the list of nodes which is not your current destination and which you can pathfind to
                Vector3 t = myMover.currentDestination;
                bool success = false;

                foreach (var candidate in GetNextNodeCandidates())
                {
                    // Ignore current position.
                    var nodePosition = candidate.position;
                    if (nodePosition == t)
                    {
                        continue;
                    }

                    // Try to start moving to the given node.
                    success = myMover.InitiateMoveTowardsDestination(nodePosition);
                    if (success)
                    {
                        break;
                    }
                    else
                    {
                        Debug.LogError($"B_Wander: {name} - Could not find path to {nodePosition}!");
                    }
                }

                if (success)
                {
                    if (DEBUG_Verbose)
                        Debug.Log($"B_Wander on Entity {name} has finished waiting at node, has picked new desitination: {myMover.currentDestination}");
                }
                else
                    Debug.LogError($"B_Wander on Entity {name} gave up trying to find a destination that was pathable and not its current location.");
            }
        }
    }

    // Starts waiting at the current position, then wanders to the next point.
    public void WaitThenWander()
    {
        waitingAtNode = true;
        waitDurationSoFar = 0f;
        waitDurationTarget = Random.Range(minTimeAtNode, maxTimeAtNode);
        if (DEBUG_Verbose)
            Debug.Log("B_Wander on Entity " + gameObject + " has arrived at destination, waiting for duration: " + waitDurationTarget);

    }

    // Gets the set of nodes to try to visit next, in the provided order.
    private IList<Transform> GetNextNodeCandidates()
    {
        if (visitNodesInOrder)
        {
            var ret = nodes.Rotated(lastVisitedOrderedNodeIndex);
            lastVisitedOrderedNodeIndex = (lastVisitedOrderedNodeIndex + 1) % nodes.Count;
            return ret;
        }

        // Avoid the node closest to the player if enabled.
        IEnumerable<Transform> candidates = nodes;
        if (avoidNodesNearPlayer && player != null && nodes.Count > 1)
        {
            candidates = candidates.OrderBy(t => Vector3.Distance(player.position, t.position));

            // Only avoid the node if its actually close enough to the player.
            if (Vector3.Distance(player.position, candidates.First().position) < 50f)
            {
                candidates = candidates.Skip(1);
            }
        }

        // Randomize the selection.
        return candidates.ToList().Shuffled();
    }

    private void MaybeUpdateWanderDestination()
    {
        bool updated = false;

        // If the herd group doesn't have a destination, pick one.
        if (!herdingSystem.HerdGroupDestinations.ContainsKey(herdGroupId))
        {
            herdWanderDestination = herdingSystem.HerdGroupDestinations[herdGroupId] = GetNextNodeCandidates()[0].position;
            updated = true;
        }
        else if (!herdWanderDestination.HasValue || herdingSystem.HerdGroupDestinations[herdGroupId] != herdWanderDestination.Value)
        {
            herdWanderDestination = herdingSystem.HerdGroupDestinations[herdGroupId];
            updated = true;
        }
        if (updated)
        {
            waitDurationSoFar = 0f;
            waitDurationTarget = Random.Range(minTimeAtNode, maxTimeAtNode);
        }
    }

    private bool Herd(float dt)
    {
        // Scan for neighbors.
        var mask = 1 << gameObject.layer;
        var visibleNeighbors = Physics
            .OverlapSphere(transform.position, HerdingProfile.VisibilityDistance, mask, QueryTriggerInteraction.Ignore)
            .Select(n => n.GetComponent<IHerdAgent>());
        var nearbyNeighbors = visibleNeighbors.Where(agent => Vector3.Distance(agent.transform.position, transform.position) <= HerdingProfile.SeparationDistance);

        MaybeUpdateWanderDestination();
        var wanderVector = Vector3.Normalize(herdWanderDestination.Value - transform.position) * HerdingProfile.PackWanderDestinationStrength;

        // Use the basic boids approach here.
        // @see https://vanhunteradams.com/Pico/Animal_Movement/Boids-algorithm.html

        // Separation.
        var separationVector = nearbyNeighbors.Aggregate(
            Vector3.zero,
            (acc, other) => acc + (transform.position - other.transform.position).normalized
        ) * HerdingProfile.SeparationStrength;

        // Alignment.
        var alignmentVector = visibleNeighbors.Aggregate(
            Vector3.zero,
            (acc, other) => acc + transform.forward
        ) * HerdingProfile.AlignmentStrength;

        // Cohesion.
        var cohesionVector = visibleNeighbors.Aggregate(
            Vector3.zero,
            (acc, other) => acc + (other.transform.position - transform.position).normalized
        ) * HerdingProfile.CohesionStrength;

        var influenceVector = Vector3.Normalize(separationVector + alignmentVector + cohesionVector + wanderVector);
        var projection = transform.position + influenceVector * HerdingProfile.ProjectionDistance;
        var nearWanderPointRadius = 3f;
        var wanderDistanceTheta = Mathf.InverseLerp(1.5f, nearWanderPointRadius, Vector3.Distance(herdWanderDestination.Value, transform.position));
        if (wanderDistanceTheta < 1)
        {
            // Consider this "waiting" at node similar to wander.
            waitingAtNode = true;
            waitDurationSoFar += dt;
            if (waitDurationSoFar > waitDurationTarget)
            {
                herdingSystem.HerdGroupDestinations.Remove(herdGroupId);
            }
        }
        else
        {
            waitingAtNode = false;
        }
        return myMover.InitiateMoveTowardsDestination(projection, B_Move.DefaultRelaxedNavmeshSampleRadius, wanderDistanceTheta);
    }

    private void OnDrawGizmos()
    {
        if (herdWanderDestination.HasValue)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, herdWanderDestination.Value);
            Gizmos.DrawSphere(herdWanderDestination.Value, 0.3f);
        }
    }
}
