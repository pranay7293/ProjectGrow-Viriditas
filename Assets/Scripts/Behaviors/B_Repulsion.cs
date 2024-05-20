using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DictionaryExtensions;
using Sirenix.Utilities;

// Repulsion behavior allows an entity to be repulsed by various things such
// as scents or threats.
// This behavior exposes events and informaton about repulsion, but leaves it
// up to an AI to make a decision on how to act.
public class B_Repulsion : Behavior
{
    [SerializeField] private float threatRadius = 5f;

    // This is a stopgap until we have better automatic navigation.
    // If the entity is unable to flee from a threat due to being stuck, it will
    // pick the furthest node from this set to go to.
    [SerializeField] private GameObject DEBUG_fallbackFleeNodes;

    // If enabled, humanoids are perceived as a threat.
    public bool ThreatenedByHumanoids { get; set; } = false;

    private List<Transform> fallbackFleeNodes = new List<Transform>();

    private IRepulsionListener aiListener;
    private Transform player;
    private Transform trackingThreat;

    private Dictionary<Transform, Vector3> nearbyThreatPositions = new Dictionary<Transform, Vector3>();

    protected override void Awake()
    {
        base.Awake();

        if (myAI is IRepulsionListener)
        {
            aiListener = (IRepulsionListener)myAI;
        }
        player = GameObject.FindGameObjectWithTag("Player").transform;

        if (DEBUG_fallbackFleeNodes != null)
        {
            foreach (Transform t in DEBUG_fallbackFleeNodes.transform)
            {
                fallbackFleeNodes.Add(t);
            }
        }
    }

    private void Update()
    {
        if (paused || !behaviorEnabled)
            return;

        var nearbyThreats = FindNearbyThreats();

        // Clear position cache for threats that are gone.
        nearbyThreatPositions.RemoveWhere(k => !nearbyThreats.Contains(k));

        // Check if we're tracking a threat that is no longer nearby.
        if (trackingThreat != null && !nearbyThreats.Contains(trackingThreat))
        {
            if (DEBUG_Verbose)
                Debug.Log($"{BehaviorName} forgetting previous threat");
            trackingThreat = null;
            aiListener.NotifyLostReplusion();
        }

        foreach (var threat in nearbyThreats)
        {
            // Check if a nearby threat is moving to see if we should acquire a new threat.
            if (trackingThreat == null &&
                nearbyThreatPositions.ContainsKey(threat) &&
                nearbyThreatPositions[threat] != threat.position)
            {
                if (DEBUG_Verbose)
                    Debug.Log($"{BehaviorName} started tracking new threat: {threat.gameObject.name}");
                trackingThreat = threat;
                aiListener.NotifyNewReplusion(threat);
            }

            // Cache the positions of all nearby threats.
            nearbyThreatPositions[threat] = threat.position;
        }
    }

    public Vector3 GetFallbackFleePosition(Vector3 attemptedPosition)
    {
        if (fallbackFleeNodes.Count < 1)
        {
            return attemptedPosition;
        }
        return fallbackFleeNodes.OrderByDescending(n => Vector3.Distance(n.position, transform.position)).First().position;
    }

    private ISet<Transform> FindNearbyThreats()
    {
        var ret = new HashSet<Transform>();
        if (!ThreatenedByHumanoids)
        {
            return ret;
        }

        // TODO: Generalize threat to extend to non-player humanoids.
        if (Vector3.Distance(player.position, transform.position) <= threatRadius)
        {
            ret.Add(player);
        }

        return ret;
    }

    public interface IRepulsionListener
    {
        void NotifyNewReplusion(Transform repulsionTransform);
        void NotifyLostReplusion();
    }
}
