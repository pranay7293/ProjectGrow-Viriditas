using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DictionaryExtensions;
using Sirenix.Utilities;
using Photon.Pun;

public class B_Repulsion : Behavior
{
    [SerializeField] private float threatRadius = 5f;
    [SerializeField] private GameObject DEBUG_fallbackFleeNodes;

    public bool ThreatenedByHumanoids { get; set; } = false;

    private List<Transform> fallbackFleeNodes = new List<Transform>();
    private IRepulsionListener aiListener;
    private Transform trackingThreat;
    private Dictionary<Transform, Vector3> nearbyThreatPositions = new Dictionary<Transform, Vector3>();

    protected override void Awake()
{
    base.Awake();

    if (myAI != null && myAI is IRepulsionListener)
    {
        aiListener = (IRepulsionListener)myAI;
    }

    if (DEBUG_fallbackFleeNodes != null)
    {
        foreach (Transform t in DEBUG_fallbackFleeNodes.transform)
        {
            if (t != null)
            {
                fallbackFleeNodes.Add(t);
            }
        }
    }
}

    private void Update()
    {
        if (paused || !behaviorEnabled)
            return;

        var nearbyThreats = FindNearbyThreats();

        nearbyThreatPositions.RemoveWhere(k => !nearbyThreats.Contains(k));

        if (trackingThreat != null && !nearbyThreats.Contains(trackingThreat))
        {
            if (DEBUG_Verbose)
                Debug.Log($"{BehaviorName} forgetting previous threat");
            trackingThreat = null;
            aiListener.NotifyLostReplusion();
        }

        foreach (var threat in nearbyThreats)
        {
            if (trackingThreat == null &&
                nearbyThreatPositions.ContainsKey(threat) &&
                nearbyThreatPositions[threat] != threat.position)
            {
                if (DEBUG_Verbose)
                    Debug.Log($"{BehaviorName} started tracking new threat: {threat.gameObject.name}");
                trackingThreat = threat;
                aiListener.NotifyNewReplusion(threat);
            }

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

        var players = FindObjectsOfType<UniversalCharacterController>()
            .Where(c => c.IsPlayerControlled && Vector3.Distance(c.transform.position, transform.position) <= threatRadius);

        foreach (var player in players)
        {
            ret.Add(player.transform);
        }

        return ret;
    }

    public interface IRepulsionListener
    {
        void NotifyNewReplusion(Transform repulsionTransform);
        void NotifyLostReplusion();
    }
}