using System.Collections;
using System.Collections.Generic;
using KinematicCharacterController;
using PlayerData;
using Systems.Toxin;
using Tools;
using UnityEngine;
using UnityEngine.Rendering;

// right now the Player class is only responsible for movement and startPoint management

public class Player : MonoBehaviour, KinematicCharacterController.ICharacterController
{
    private Karyo_GameCore core;
    public static Player Instance;

    public string playerName; // used by NPCs

    [SerializeField] private Camera lookCamera;

    public GameObject startPoint;

    private KinematicCharacterMotor motor;
    private PlayerVisualEffects playerVisualEffects;
    private CapsuleCollider capsuleCollider;
    private ToolHandler toolHandler;
    private PlayerData.PlayerInventoryCircuits inventoryCircuits;

    [Header("References")]
    [SerializeField] private Light playerSpotLight;

    [Header("Locomotion")]
    public float walkSpeed = 5f;  //  units per sec
    public float runSpeed = 15f;  //  units per sec
    public float backwardsSpeedModifier = 0.6f;
    public float turnSpeed = 20f; // degrees per sec
    public float jumpHeight = 1f;  // roughly meters
    public float gravityScale = 1.5f;
    public float airAcceleration = 1f;

    [Header("Mantling")]
    [SerializeField] private LayerMask mantleSurfaceMask;
    [SerializeField] private float mantleScanForward = 0.5f;
    [SerializeField] private float mantleScanHeight = 4f;
    [SerializeField] private float mantleScanDistance = 4f;
    [SerializeField] private float mantleRiseSpeed = 1f;
    [SerializeField] private float mantleSurfaceFlatnessTolerance = 0.95f;
    [SerializeField] private float mantlePushForce = 10f;
    [SerializeField] private float mantlePushDecay = 1f;

    private static float mantleTargetDistanceEpisloon = 0.1f;

    private Vector3 FeetPosition => transform.position;
    private bool IsGrounded => motor.GroundingStatus.IsStableOnGround;

    // The movement state the player is currently in.
    private enum LocomotionState
    {
        PLAYER_CONTROL,  // The player is being fully controlled by input.
        MANTLING         // The player is in a mantling animation.
    }
    private LocomotionState locomotionState = LocomotionState.PLAYER_CONTROL;

    private float normalizedToxinLevel;

    private Vector3 targetMantlePosition;
    private Vector3 mantlePushVector = Vector3.zero;
    private Quaternion targetRotation;
    private bool isTurning;
    private float mantlingTimer;
    private bool jumpHeldLastPhysicsFrame;

    // TODO - NPC stuff that might want to live somewhere else
    private NPC currentNPCSelection;
    private NPC lockedDialogTarget;
    public string currentLocation;


    private void Awake()
    {
        if (Instance != null)
            Debug.LogWarning("More than one Player object exists.");
        else
            Player.Instance = this;

        core = GameObject.FindGameObjectWithTag("GameCore").GetComponent<Karyo_GameCore>();
        if (core == null)
            Debug.LogError(this + " cannot find Game Core.");

        motor = GetComponent<KinematicCharacterMotor>();
        if (motor == null)
            Debug.LogError("Player does not have KinematicCharacterMotor component.");
        else
            motor.CharacterController = this;

        capsuleCollider = GetComponent<CapsuleCollider>();

        toolHandler = GetComponent<ToolHandler>();
        if (toolHandler == null)
            Debug.LogError("Player does not have ToolHandler component.");

        playerVisualEffects = GetComponent<PlayerVisualEffects>();
        if (playerVisualEffects == null)
            Debug.LogError("Player does not have PlayerVisualEffects component.");

        inventoryCircuits = GetComponent<PlayerInventoryCircuits>();
        if (inventoryCircuits == null)
            Debug.LogError("Player does not have PlayerInventoryCircuits component.");

        if (startPoint != null)
            UseStartPoint(startPoint);

        currentLocation = "starting location";  // see NPC comment in the same place, set this to a valid location name if the player starts inside the relevant volume
    }


    // TODO: This is probably not the best location for this method, find a better
    // way to pass this data between system.s
    public CircuitResources GetPlayerCircuitResources()
    {
        return inventoryCircuits.Resources;
    }

    public void UseStartPoint(GameObject thisStartPoint)
    {
        SnapToPoint(thisStartPoint.transform.position, thisStartPoint.transform.rotation);
    }

    private void SnapToPoint(Vector3 pos, Quaternion rot)
    {
        motor.SetPositionAndRotation(pos, rot);
        motor.BaseVelocity = Vector3.zero;
    }

    private void Update()
    {
        // Check if there are toxin effects to apply.
        normalizedToxinLevel = Mathf.Clamp01(ToxinSystem.GetTotalToxinLevel(transform.position));
        playerVisualEffects.SetToxinEffectLevel(normalizedToxinLevel);
        playerSpotLight.enabled = TimeOfDay.Instance?.IsDay == false;

        // TODO - this is very NPC interaction specific and probably should live elsewhere instead
        // cast to find an NPC in front of the player
        if (lockedDialogTarget == null)  // if you have a locked dialog target, you don't switch selections
        {
            Vector3 lookPosition = lookCamera.transform.position;
            Vector3 lookVector = lookCamera.transform.forward;
            float maxDistance = 5f;
            if (Physics.Raycast(new Ray(lookPosition, lookVector), out var hitInfo, maxDistance, Physics.AllLayers, QueryTriggerInteraction.Ignore))
            {
                NPC npc = hitInfo.collider.gameObject.GetComponentInParent<NPC>();

                if (npc)
                {
                    if (npc != currentNPCSelection)
                    {
                        if (currentNPCSelection == null)  // newly found an NPC
                        {
                            npc.StartSelection();
                            currentNPCSelection = npc;
                        }
                        else  // switched immediately from one NPC to another
                        {
                            currentNPCSelection.StopSelection();
                            npc.StartSelection();
                            currentNPCSelection = npc;
                        }
                    }
                    // otherwise we're still on the same NPC, so no change is needed
                }
                else  // found something but not an NPC
                {
                    if (currentNPCSelection != null)
                        currentNPCSelection.StopSelection();
                    currentNPCSelection = null;
                }
            }
            else  // didn't find anything
            {
                if (currentNPCSelection != null)
                    currentNPCSelection.StopSelection();
                currentNPCSelection = null;
            }
        }
        else // you do have a locked dialog target
        {
            // if they are no longer nearby, break off the lock and tell themn they are no longer talking to you if they were
            if (!lockedDialogTarget.IsNearby(lockedDialogTarget.transform.position, transform.position, lockedDialogTarget.currentLocation, currentLocation))
            {
                if (lockedDialogTarget.IsInDialogWithCharacter(playerName))
                    lockedDialogTarget.BeingSpokenTo_Cancelled(playerName);
                lockedDialogTarget.StopSelection();
                lockedDialogTarget = null;
            }
        }


    }


    // TODO - this is very NPC specific and maybe should live elsewhere
    // called by InputManager when player presses the correct key
    public async void InitiatePlayerDialog()
    {
        if (currentNPCSelection == null)
        {
            Debug.Log("Player has no selected NPC, cannot talk.");
            return;
        }

        if (currentNPCSelection.IsAwaitingInstructions)
        {
            Debug.Log("Can't talk to NPCs who are awaiting instructions.");
            return;
        }

        lockedDialogTarget = currentNPCSelection;
        core.uiManager.OpenDialogInputWindow(lockedDialogTarget.name);
        lockedDialogTarget.BeingSpokenTo(playerName);

        // if we are currently supporting AI generated dialog options, then request some from the NPC targeted for dialog
        if (core.uiManager.aiGeneratedDialogOptions)
        {
            string dots = new string("...");
            string[] many_dots = new string[3];
            many_dots[0] = dots;
            many_dots[1] = dots;
            many_dots[2] = dots;
            core.uiManager.PopulateDialogOptionButtons(many_dots, false);

            string[] dialogOptions = await lockedDialogTarget.RequestDialogOptions();

            // control resumes down here when dialog options are returned

            // it always returns an array of exactly 3 strings
            core.uiManager.PopulateDialogOptionButtons(dialogOptions, true);
        }
    }

    // called by UIManager when player cancels dialog submission by pressing esc or clicking outside the window to close the window
    public void PlayerDialogCancelled()
    {
        // the ui manager already closed the ui window
        if (lockedDialogTarget != null)
        {
            if (lockedDialogTarget.IsInDialogWithCharacter(playerName))
                lockedDialogTarget.BeingSpokenTo_Cancelled(playerName);
            lockedDialogTarget.StopSelection();
            lockedDialogTarget = null;
        }
    }

    // called by UIManager when player presses submit button or presses one of the dialog option buttons
    public void PlayerDialogSubmitted (string dialog)
    {
        if (lockedDialogTarget != null)
        {
            // tell the NPC to remember this dialog event in their history
            NPC.DialogEvent dialogEvent = new NPC.DialogEvent(playerName, lockedDialogTarget.name, dialog, lockedDialogTarget.currentLocation);
            lockedDialogTarget.RememberDialogEvent(dialogEvent);

            // remember dialog history first, call WasSpokenTo second, since the latter generates a prompt
            lockedDialogTarget.WasSpokenToBy(playerName, dialog);
        }

        lockedDialogTarget = null;
    }





    private float GetGroundMoveSpeed()
    {
        var speedLimit = Mathf.Lerp(runSpeed, walkSpeed / 2f, normalizedToxinLevel);
        float moveSpeed;
        var canRun = toolHandler.GetEquippedToolConstraints()?.CanPlayerRun ?? true;
        if (core.inputManager.PlayerRunModifier && canRun)
            moveSpeed = runSpeed;
        else
            moveSpeed = walkSpeed;
        return Mathf.Min(moveSpeed, speedLimit);
    }

    private bool CheckMantleSurface(out Vector3 position)
    {
        position = Vector3.zero;
        var ray = GetMantleScanRay();
        if (Physics.Raycast(ray, out var hit, mantleScanDistance, mantleSurfaceMask, QueryTriggerInteraction.Ignore))
        {
            var dot = Vector3.Dot(hit.normal, Vector3.up);
            if (dot >= mantleSurfaceFlatnessTolerance)
            {
                position = hit.point + transform.forward * 0.1f + Vector3.up * 0.1f;
                return true;
            }
        }

        return false;
    }

    private Ray GetMantleScanRay()
    {
        return new Ray(
            transform.position + transform.forward * mantleScanForward + Vector3.up * mantleScanHeight,
            Vector3.down);
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize the mantle scan ray.
        Gizmos.color = Color.green;
        var ray = GetMantleScanRay();
        Gizmos.DrawLine(ray.origin, ray.origin + ray.direction * mantleScanDistance);
    }

    private void UpdatePlayerControlMovement(ref Vector3 currentVelocity, float deltaTime, bool jumpedThisFrame)
    {
        // Determine the move vector.
        var relativeMoveDirection = core.inputManager.PlayerRelativeMoveDirection;
        var moveVector = lookCamera.transform.rotation * relativeMoveDirection;
        var moveSpeed = GetGroundMoveSpeed();
        var jumpVector = Vector3.zero;

        // Check grounded status.
        if (IsGrounded)
        {
            if (jumpedThisFrame)
            {
                // Force the motor to detach itself from the ground before we apply further force.
                motor.ForceUnground(0.1f);
                jumpVector.y = Mathf.Sqrt(jumpHeight * WorldRep.gravity * -3f * gravityScale);
            }
            // For ground movement we assign lateral velocity directly.
            currentVelocity.x = moveVector.x * moveSpeed;
            currentVelocity.z = moveVector.z * moveSpeed;

            // Clamp vertical velocity.
            currentVelocity.y = Mathf.Max(0, currentVelocity.y);
        }
        else
        {
            // For air movement we apply an acceleration, clamped at moveSpeed.
            var accel = moveVector * moveSpeed * airAcceleration * deltaTime;
            var airLateralMoveVector = Vector3.ClampMagnitude(
                new Vector3(currentVelocity.x, 0, currentVelocity.z) + accel, moveSpeed);
            currentVelocity.x = airLateralMoveVector.x;
            currentVelocity.z = airLateralMoveVector.z;
        }

        // Handle vertical velocity.
        currentVelocity.y += jumpVector.y + WorldRep.gravity * gravityScale * deltaTime;

        // Handle mantling push force, decayed over time.
        currentVelocity += mantlePushVector;
        mantlePushVector = Vector3.Lerp(mantlePushVector, Vector3.zero, mantlePushDecay * deltaTime);

        const float minMoveDirectionToTurnToView = .1f;
        const float minMoveDirectionToTurnToViewSq = minMoveDirectionToTurnToView * minMoveDirectionToTurnToView;

        // Rotate to the camera if we are moving at all
        if (relativeMoveDirection.sqrMagnitude > minMoveDirectionToTurnToViewSq)
        {
            // Turn to face towards the movement direction.
            var faceDirection = moveVector;
            faceDirection.y = 0; // TODO: Make relative to player instead of world?
            targetRotation = Quaternion.LookRotation(faceDirection);
            isTurning = true;
        }
        else
        {
            isTurning = false;
        }
    }

    private void UpdateMantlingMovement(ref Vector3 currentVelocity, float deltaTime)
    {
        mantlingTimer -= deltaTime;
        if (mantlingTimer < 0)
        {
            // Escape hatch.
            locomotionState = LocomotionState.PLAYER_CONTROL;
            return;
        }

        var diff = targetMantlePosition.y - FeetPosition.y;
        if (Mathf.Abs(diff) > mantleTargetDistanceEpisloon)
        {
            // Rise action.
            currentVelocity = Vector3.up * mantleRiseSpeed;
            return;
        }


        // End mantling state and queue a forwards push vector.
        locomotionState = LocomotionState.PLAYER_CONTROL;
        var towards = Vector3.Normalize(targetMantlePosition - FeetPosition);
        mantlePushVector = towards * mantlePushForce;
    }



    ////////////////////////////////////////////////////////////////////////////
    // KinematicCharacterController.ICharacterController interface
    ////////////////////////////////////////////////////////////////////////////

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        // Read the jump input. Note that we cannot use GetKeyDown in physics frames, so we 
        // need to wrap this logic ourselves.
        var jumpHeld = core.inputManager.PlayerJumpHeld;
        var jumpedThisFrame = jumpHeld && !jumpHeldLastPhysicsFrame;
        jumpHeldLastPhysicsFrame = jumpHeld;

        // Check for a mantling state change.
        if (locomotionState != LocomotionState.MANTLING && CheckMantleSurface(out var position))
        {
            core.uiManager.ReticleHandler.SetText(UI.TextLocation.BottomPlayer, "Climb (space)");
            if (jumpedThisFrame)
            {
                // Force the motor to detach itself from the ground before we apply further force.
                motor.ForceUnground(0.1f);

                // Start mantling.
                locomotionState = LocomotionState.MANTLING;
                targetMantlePosition = position;
                mantlingTimer = 2f;
                return;
            }
        }
        else
        {
            core.uiManager.ReticleHandler.SetText(UI.TextLocation.BottomPlayer, "");
        }

        switch (locomotionState)
        {
            case LocomotionState.PLAYER_CONTROL:
                UpdatePlayerControlMovement(ref currentVelocity, deltaTime, jumpedThisFrame);
                break;
            case LocomotionState.MANTLING:
                UpdateMantlingMovement(ref currentVelocity, deltaTime);
                break;
        }
    }

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        if (!isTurning)
        {
            return;
        }
        // Smoothly rotate towards the target we've set.
        currentRotation =
            Quaternion.RotateTowards(currentRotation, targetRotation, turnSpeed * deltaTime);
    }

    public bool IsColliderValidForCollisions(Collider coll)
    {
        return true;
    }

    public void BeforeCharacterUpdate(float deltaTime) { }
    public void PostGroundingUpdate(float deltaTime) { }
    public void AfterCharacterUpdate(float deltaTime) { }
    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) { }
    public void OnDiscreteCollisionDetected(Collider hitCollider) { }

}
