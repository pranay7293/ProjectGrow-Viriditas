using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Systems.Scent;


// This is a type of AI that can move. Most obvious example: fauna but not flora.  It is meant to be a base class that handles all standard behaviors,
// and only needs to be derived from if you have some unusual and special design.
// It manages behaviors relating to movement.  So for example, it knows how to pause movement if necessary when antigravity trait is added, and visa-versa.
// It also handles callbacks from B_Move when the AI arrives at its destination and farms out that message to whoever needs to decide what the AI does next.

public class AI_Mover : AI, B_Attraction.IAttractionListener, B_Repulsion.IRepulsionListener
{
    public bool canMoveWhenAntigravity = false;  // when true, make sure the B_RiseInAir subBehavior is not RiseAndWiggle, since that will modify rotation

    // TODO: This is not the appropriate location for this field, it should
    // really be in a behavior or trait instead.
    public float fleeSpeedFactor = 3f;

    private B_Wander myWanderBehavior;
    private B_Move myMoveBehavior;
    private B_Repulsion myRepulsionBehavior;
    private NPC myNPC;

    private Vector3? lastAttractionPosition = null;
    private Transform lastRepulsion = null;
    private float repulsionStrength;
    private Vector3 fallbackFleePosition;

    protected override void Awake()
    {
        base.Awake();
        myWanderBehavior = GetBehavior<B_Wander>();
        myMoveBehavior = GetBehavior<B_Move>();
        myRepulsionBehavior = GetBehavior<B_Repulsion>();
        myNPC = GetComponent<NPC>();
    }

    private void Update()
    {
        if (myWanderBehavior == null)
        {
            return;
        }

        // TODO: Rework this with new herding behavior so that the attraction
        // can be balanced with herd intention, for now we immediately
        // move to the attraction/repulsion.
        if (lastAttractionPosition != null || lastRepulsion != null)
        {
            // TODO: This is a bit messy, but we need to unpause the move behavior
            // so that the wander behavior knows to keep it unpaused, this needs to
            // be cleaned up.
            myMoveBehavior.paused = false;
            // Pause the wander behavior
            myWanderBehavior.paused = true;
            MakeNextMovementDecision();
        }
        else
        {
            // Resume the wander behavior.
            myWanderBehavior.paused = false;
        }
    }

    public override void TraitHasBeenAdded(Trait trait)
    {
        base.TraitHasBeenAdded(trait);

        switch (trait.traitClass)
        {
            case TraitClass.Antigravity:
                // enable the rise in air behavior
                B_RiseInAir b_r = GetBehavior<B_RiseInAir>();
                if (b_r != null)
                {
                    b_r.strength = trait.strength;
                    b_r.behaviorEnabled = true;
                }
                else
                    Debug.LogWarning($"Entity {gameObject} has had Antigravity trait added but does not have the B_RiseInAir behavior");


                if (canMoveWhenAntigravity)
                {
                    BM_Walk bm_w = GetComponent<BM_Walk>();
                    if (bm_w != null)
                        bm_w.hoverHeight = trait.strength;
                }
                else
                {
                    // pause any movement-initiating behaviors (which will pause downstream locomotion behaviors
                    B_Wander b_w = GetBehavior<B_Wander>();
                    if (b_w != null)
                        b_w.paused = true;
                }
                break;
            case TraitClass.Threatened:
                // TODO: Support threats other than humanoids.
                var b_repulsion = GetBehavior<B_Repulsion>();
                if (b_repulsion != null)
                {
                    b_repulsion.behaviorEnabled = true;
                    b_repulsion.ThreatenedByHumanoids = true;
                }
                repulsionStrength = trait.strength;
                break;
            case TraitClass.Herding:
                var b_wander = GetBehavior<B_Wander>();
                if (b_wander != null)
                {
                    b_wander.HerdingProfile = trait as T_Herding;
                }
                break;
        }
    }

    public override void TraitHasBeenRemoved(Trait trait)
    {
        base.TraitHasBeenRemoved(trait);

        switch (trait.traitClass)
        {
            case TraitClass.Antigravity:
                // disable the rise in air behavior
                B_RiseInAir b_r = GetBehavior<B_RiseInAir>();
                if (b_r != null)
                    b_r.behaviorEnabled = false;
                else
                    Debug.LogWarning("Entity " + gameObject + " has had Antigravity trait removed but does not have the B_RiseInAir behavior");

                if (canMoveWhenAntigravity)
                {
                    BM_Walk bm_w = GetComponent<BM_Walk>();
                    if (bm_w != null)
                        bm_w.hoverHeight = 0f;
                }
                else
                {
                    // unpause any movement-initiating behaviors (which will unpause downstream locomotion behaviors
                    B_Wander b_w = GetBehavior<B_Wander>();
                    if (b_w != null)
                        b_w.paused = false;
                }
                break;
            case TraitClass.Threatened:
                var b_repulsion = GetBehavior<B_Repulsion>();
                if (b_repulsion != null)
                {
                    b_repulsion.behaviorEnabled = false;
                }
                break;
            case TraitClass.Herding:
                var b_wander = GetBehavior<B_Wander>();
                if (b_wander != null)
                {
                    b_wander.HerdingProfile = null;
                }
                break;
        }
    }

    // this is called by B_Move when an B_Mover Entity arrives at a destination.
    // the AI has to decide what to do next.
    public void EntityHasArrivedAtDestination()
    {
        if (myNPC != null)
            myNPC.ArrivedAtLocation();

        MakeNextMovementDecision();
    }

    // this is called by B_Move when an B_Mover Entity gives up on getting to a destination.
    // the AI has to decide what to do next.
    public void EntityCantGetToDestination()
    {
        if (myNPC != null)
            myNPC.CantGetToDestination();

        MakeNextMovementDecision();
    }

    public void NotifyNewAttraction(Vector3 attractionPosition)
    {
        lastAttractionPosition = attractionPosition;
    }

    public void NotifyLostAttraction()
    {
        lastAttractionPosition = null;
    }

    public void NotifyNewReplusion(Transform repulsionTransform)
    {
        lastRepulsion = repulsionTransform;
        fallbackFleePosition = myRepulsionBehavior.GetFallbackFleePosition(lastRepulsion.position);
    }

    public void NotifyLostReplusion()
    {
        lastRepulsion = null;
    }

    private void MakeNextMovementDecision()
    {
        // Decision priority goes repulsion, attraction, wander.
        if (lastRepulsion != null)
        {
            // TODO: This won't work well for BM_Fly.

            // Use the repulsion strength to determine movement speed.
            var speedFactor = Mathf.Clamp(repulsionStrength * fleeSpeedFactor, 1, fleeSpeedFactor);
            myMoveBehavior.InitiateMoveTowardsDestination(
                GetProjectedFleePosition(), B_Move.DefaultRelaxedNavmeshSampleRadius, speedFactor);
        }
        else if (lastAttractionPosition != null)
        {
            // If we have an attraction point we're not already near, move to it.
            // TODO: Implement a wait behavior here as well, this works but we're wasting CPU and logspam by
            // repeatedly moving to the same destination.
            myMoveBehavior.InitiateMoveTowardsDestination(lastAttractionPosition.Value, 2f);
        }
        else if (myWanderBehavior != null)
        {
            // Otherwise, continue wandering.
            myWanderBehavior.WaitThenWander();
        }
    }

    private Vector3 GetProjectedFleePosition()
    {
        // HACK for demo, blend and prioritize explicit fallback flee position points.
        var fallbackFleeVector = Vector3.Normalize(fallbackFleePosition - transform.position);
        var threatFleeVector = Vector3.Normalize(transform.position - lastRepulsion.position);
        var theta = Mathf.InverseLerp(0, 15, Vector3.Distance(transform.position, fallbackFleePosition));
        var fleeVector = Vector3.Lerp(threatFleeVector, fallbackFleeVector, theta);
        var fleeRange = B_Move.DefaultRelaxedNavmeshSampleRadius + 1f;
        return transform.position + fleeVector * fleeRange;
    }
}
