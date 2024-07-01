using UnityEngine;
using Systems.Scent;

// Attraction behavior allows an entity to be attracted to points of interest
// such as scents or other entities.
// This behavior exposes events and information about attraction, but leaves it
// up to an AI to make a decision on how to act.
public class B_Attraction : Behavior
{
    // How often should we check for scents.
    [SerializeField] private float scentCheckInterval = 1f;

    [SerializeField] private string tempScentToFind;

    private IAttractionListener aiListener;

    private float scentCheckTimer;
    private IScentEmitter trackingScent;

    protected override void Awake()
    {
        base.Awake();

        if (myAI is IAttractionListener)
        {
            aiListener = (IAttractionListener)myAI;
        }
    }

    private void Update()
    {
        if (paused || !behaviorEnabled)
            return;

        // Update timers.
        scentCheckTimer -= Time.deltaTime;

        if (!string.IsNullOrEmpty(tempScentToFind) && scentCheckTimer < 0)
        {
            CheckForScent();
            scentCheckTimer = scentCheckInterval;
        }
    }

    // Check for a nearby scent.
    private void CheckForScent()
    {
        var anyScentFound = ScentSystem.TryFindStrongestScent(transform.position, tempScentToFind, out var _, out var scentEmitter);
        if (!anyScentFound)
        {
            // Check if we have a tracking scent to forget.
            if (trackingScent != null)
            {
                if (DEBUG_Verbose)
                    Debug.Log($"{BehaviorName} forgetting previous scent");
                trackingScent = null;
                aiListener.NotifyLostAttraction();
            }
            return;
        }

        // Check if the found scent is new.
        // TODO: Check that scent is strong enough to care about it...
        if (IsScentNew(scentEmitter))
        {
            if (DEBUG_Verbose)
                Debug.Log($"{BehaviorName} started tracking new scent at {scentEmitter.Position}");
            trackingScent = scentEmitter;
            aiListener.NotifyNewAttraction(scentEmitter.Position);
        }
    }

    private bool IsScentNew(IScentEmitter newScent)
    {
        if (trackingScent == null)
        {
            return true;
        }
        if (trackingScent == newScent)
        {
            return false;
        }

        // Check also that its far enough away from the previous scent.
        const float distanceCheck = .25f;
        var diff = trackingScent.Position - newScent.Position;
        return diff.sqrMagnitude > distanceCheck * distanceCheck;
    }

    public interface IAttractionListener
    {
        void NotifyNewAttraction(Vector3 attractionPosition);
        void NotifyLostAttraction();
    }
}
