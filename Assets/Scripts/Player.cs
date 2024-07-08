using UnityEngine;
using Photon.Pun;
using KinematicCharacterController;

public class Player : MonoBehaviourPunCallbacks
{
    public string playerName;
    public float moveSpeed = 5f;
    public float rotationSpeed = 120f;

    private Vector3 moveDirection;
    private float rotationY;
    private InputManager inputManager;
    private KinematicCharacterMotor motor;

    public void Initialize(string name, KinematicCharacterMotor characterMotor)
    {
        playerName = name;
        motor = characterMotor;
        inputManager = FindObjectOfType<InputManager>();
        if (inputManager == null)
            Debug.LogError("InputManager not found in the scene.");
    }

    private void Update()
    {
        if (photonView.IsMine && inputManager != null)
        {
            moveDirection = inputManager.PlayerRelativeMoveDirection;
            rotationY += Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
        }
    }

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        currentVelocity = moveDirection * moveSpeed;
    }

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        currentRotation = Quaternion.Euler(0, rotationY, 0);
    }
}

// using UnityEngine;
// using KinematicCharacterController;
// using PlayerData;
// using Systems.Toxin;
// using Tools;
// using Photon.Pun;
// using com.ootii.Cameras;

// public class Player : Character, ICharacterController
// {
//     private Karyo_GameCore core;

//     public string playerName;
//     [SerializeField] private Camera lookCamera;
//     public GameObject startPoint;

//     private KinematicCharacterMotor motor;
//     private CapsuleCollider capsuleCollider;
//     private ToolHandler toolHandler;
//     private PlayerData.PlayerInventoryCircuits inventoryCircuits;

//     [Header("Locomotion")]
//     public float walkSpeed = 5f;
//     public float runSpeed = 15f;
//     public float backwardsSpeedModifier = 0.6f;
//     public float turnSpeed = 20f;
//     public float jumpHeight = 1f;
//     public float gravityScale = 1.5f;
//     public float airAcceleration = 1f;

//     [Header("Mantling")]
//     [SerializeField] private LayerMask mantleSurfaceMask;
//     [SerializeField] private float mantleScanForward = 0.5f;
//     [SerializeField] private float mantleScanHeight = 4f;
//     [SerializeField] private float mantleScanDistance = 4f;
//     [SerializeField] private float mantleRiseSpeed = 1f;
//     [SerializeField] private float mantleSurfaceFlatnessTolerance = 0.95f;
//     [SerializeField] private float mantlePushForce = 10f;
//     [SerializeField] private float mantlePushDecay = 1f;

//     private static float mantleTargetDistanceEpisloon = 0.1f;

//     private Vector3 FeetPosition => transform.position;
//     private bool IsGrounded => motor.GroundingStatus.IsStableOnGround;

//     private enum LocomotionState
//     {
//         PLAYER_CONTROL,
//         MANTLING
//     }
//     private LocomotionState locomotionState = LocomotionState.PLAYER_CONTROL;

//     private float normalizedToxinLevel;

//     private Vector3 targetMantlePosition;
//     private Vector3 mantlePushVector = Vector3.zero;
//     private Quaternion targetRotation;
//     private bool isTurning;
//     private float mantlingTimer;
//     private bool jumpHeldLastPhysicsFrame;

//     private NPC currentNPCSelection;
//     private NPC lockedDialogTarget;
//     public string currentLocation;

//     public void InitializePlayerData(string playerName)
// {
//     this.playerName = playerName;
//     // Add any other player-specific initialization here
// }
//     protected override void Awake()
//     {
//         base.Awake();

//         if (photonView.InstantiationData != null && photonView.InstantiationData.Length > 0)
//         {
//         string characterName = (string)photonView.InstantiationData[0];
//         bool isPlayerControlled = (bool)photonView.InstantiationData[1];
        
//         if (isPlayerControlled)
//         {
//             InitializePlayerData(characterName);
//         }
//         }

//         core = GameObject.FindGameObjectWithTag("GameCore").GetComponent<Karyo_GameCore>();
//         if (core == null)
//             Debug.LogError(this + " cannot find Game Core.");

//         motor = GetComponent<KinematicCharacterMotor>();
//         if (motor == null)
//             Debug.LogError("Player does not have KinematicCharacterMotor component.");
//         else
//             motor.CharacterController = this;

//         capsuleCollider = GetComponent<CapsuleCollider>();

//         toolHandler = GetComponent<ToolHandler>();
//         if (toolHandler == null)
//             Debug.LogError("Player does not have ToolHandler component.");

//         inventoryCircuits = GetComponent<PlayerInventoryCircuits>();
//         if (inventoryCircuits == null)
//             Debug.LogError("Player does not have PlayerInventoryCircuits component.");

//         if (startPoint != null)
//             UseStartPoint(startPoint);

//         currentLocation = "starting location";

//         SetupCamera();
//     }

//     public void SetupCamera()
// {
//     if (photonView.IsMine)
//     {
//         GameObject cameraRigPrefab = Resources.Load<GameObject>("CameraRig");
//         if (cameraRigPrefab != null)
//         {
//             GameObject cameraRigInstance = Instantiate(cameraRigPrefab, transform.position, Quaternion.identity);
//             CameraController cameraController = cameraRigInstance.GetComponent<CameraController>();
//             if (cameraController != null)
//             {
//                 cameraController.Anchor = this.transform;
//             }
//             else
//             {
//                 Debug.LogError("CameraController component not found on CameraRig prefab");
//             }
//             lookCamera = cameraRigInstance.GetComponentInChildren<Camera>();
//         }
//         else
//         {
//             Debug.LogError("CameraRig prefab not found in Resources folder");
//         }
//     }
// }

//     public CircuitResources GetPlayerCircuitResources()
//     {
//         return inventoryCircuits.Resources;
//     }

//     public void UseStartPoint(GameObject thisStartPoint)
//     {
//         SnapToPoint(thisStartPoint.transform.position, thisStartPoint.transform.rotation);
//     }

//     private void SnapToPoint(Vector3 pos, Quaternion rot)
//     {
//         motor.SetPositionAndRotation(pos, rot);
//         motor.BaseVelocity = Vector3.zero;
//     }

//     private void Update()
//     {
//         if (!photonView.IsMine) return;

//         normalizedToxinLevel = Mathf.Clamp01(ToxinSystem.GetTotalToxinLevel(transform.position));
//         // playerVisualEffects.SetToxinEffectLevel(normalizedToxinLevel);
//         // playerSpotLight.enabled = TimeOfDay.Instance?.IsDay == false;

//         HandleNPCSelection();
//     }

//     private void HandleNPCSelection()
//     {
//         if (lockedDialogTarget == null)
//         {
//             Vector3 lookPosition = lookCamera.transform.position;
//             Vector3 lookVector = lookCamera.transform.forward;
//             float maxDistance = 5f;
//             if (Physics.Raycast(new Ray(lookPosition, lookVector), out var hitInfo, maxDistance, Physics.AllLayers, QueryTriggerInteraction.Ignore))
//             {
//                 NPC npc = hitInfo.collider.gameObject.GetComponentInParent<NPC>();

//                 if (npc)
//                 {
//                     if (npc != currentNPCSelection)
//                     {
//                         if (currentNPCSelection == null)
//                         {
//                             npc.StartSelection();
//                             currentNPCSelection = npc;
//                         }
//                         else
//                         {
//                             currentNPCSelection.StopSelection();
//                             npc.StartSelection();
//                             currentNPCSelection = npc;
//                         }
//                     }
//                 }
//                 else
//                 {
//                     if (currentNPCSelection != null)
//                         currentNPCSelection.StopSelection();
//                     currentNPCSelection = null;
//                 }
//             }
//             else
//             {
//                 if (currentNPCSelection != null)
//                     currentNPCSelection.StopSelection();
//                 currentNPCSelection = null;
//             }
//         }
//         else
//         {
//             if (!lockedDialogTarget.IsNearby(lockedDialogTarget.transform.position, transform.position, lockedDialogTarget.currentLocation, currentLocation))
//             {
//                 if (lockedDialogTarget.IsInDialogWithCharacter(playerName))
//                     lockedDialogTarget.BeingSpokenTo_Cancelled(playerName);
//                 lockedDialogTarget.StopSelection();
//                 lockedDialogTarget = null;
//             }
//         }
//     }

//     [PunRPC]
//     public async void InitiatePlayerDialog()
//     {
//         if (currentNPCSelection == null)
//         {
//             Debug.Log("Player has no selected NPC, cannot talk.");
//             return;
//         }

//         if (currentNPCSelection.IsAwaitingInstructions)
//         {
//             Debug.Log("Can't talk to NPCs who are awaiting instructions.");
//             return;
//         }

//         lockedDialogTarget = currentNPCSelection;
//         core.uiManager.OpenDialogInputWindow(lockedDialogTarget.name);
//         lockedDialogTarget.BeingSpokenTo(playerName);

//         if (core.uiManager.aiGeneratedDialogOptions)
//         {
//             string dots = new string("...");
//             string[] many_dots = new string[3];
//             many_dots[0] = dots;
//             many_dots[1] = dots;
//             many_dots[2] = dots;
//             core.uiManager.PopulateDialogOptionButtons(many_dots, false);

//             string[] dialogOptions = await lockedDialogTarget.RequestDialogOptions();

//             core.uiManager.PopulateDialogOptionButtons(dialogOptions, true);
//         }
//     }

//     [PunRPC]
//     public void PlayerDialogCancelled()
//     {
//         if (lockedDialogTarget != null)
//         {
//             if (lockedDialogTarget.IsInDialogWithCharacter(playerName))
//                 lockedDialogTarget.BeingSpokenTo_Cancelled(playerName);
//             lockedDialogTarget.StopSelection();
//             lockedDialogTarget = null;
//         }
//     }

//     [PunRPC]
//     public void PlayerDialogSubmitted(string dialog)
//     {
//         if (lockedDialogTarget != null)
//         {
//             NPC.DialogEvent dialogEvent = new NPC.DialogEvent(playerName, lockedDialogTarget.name, dialog, lockedDialogTarget.currentLocation);
//             lockedDialogTarget.RememberDialogEvent(dialogEvent);

//             lockedDialogTarget.WasSpokenToBy(playerName, dialog);
//         }

//         lockedDialogTarget = null;
//     }

//     private float GetGroundMoveSpeed()
//     {
//         var speedLimit = Mathf.Lerp(runSpeed, walkSpeed / 2f, normalizedToxinLevel);
//         float moveSpeed;
//         var canRun = toolHandler.GetEquippedToolConstraints()?.CanPlayerRun ?? true;
//         if (core.inputManager.PlayerRunModifier && canRun)
//             moveSpeed = runSpeed;
//         else
//             moveSpeed = walkSpeed;
//         return Mathf.Min(moveSpeed, speedLimit);
//     }

//     private bool CheckMantleSurface(out Vector3 position)
//     {
//         position = Vector3.zero;
//         var ray = GetMantleScanRay();
//         if (Physics.Raycast(ray, out var hit, mantleScanDistance, mantleSurfaceMask, QueryTriggerInteraction.Ignore))
//         {
//             var dot = Vector3.Dot(hit.normal, Vector3.up);
//             if (dot >= mantleSurfaceFlatnessTolerance)
//             {
//                 position = hit.point + transform.forward * 0.1f + Vector3.up * 0.1f;
//                 return true;
//             }
//         }

//         return false;
//     }

//     private Ray GetMantleScanRay()
//     {
//         return new Ray(
//             transform.position + transform.forward * mantleScanForward + Vector3.up * mantleScanHeight,
//             Vector3.down);
//     }

//     private void OnDrawGizmosSelected()
//     {
//         Gizmos.color = Color.green;
//         var ray = GetMantleScanRay();
//         Gizmos.DrawLine(ray.origin, ray.origin + ray.direction * mantleScanDistance);
//     }

//     private void UpdatePlayerControlMovement(ref Vector3 currentVelocity, float deltaTime, bool jumpedThisFrame)
//     {
//         var relativeMoveDirection = core.inputManager.PlayerRelativeMoveDirection;
//         var moveVector = lookCamera.transform.rotation * relativeMoveDirection;
//         var moveSpeed = GetGroundMoveSpeed();
//         var jumpVector = Vector3.zero;

//         if (IsGrounded)
//         {
//             if (jumpedThisFrame)
//             {
//                 motor.ForceUnground(0.1f);
//                 jumpVector.y = Mathf.Sqrt(jumpHeight * WorldRep.gravity * -3f * gravityScale);
//             }
//             currentVelocity.x = moveVector.x * moveSpeed;
//             currentVelocity.z = moveVector.z * moveSpeed;
//             currentVelocity.y = Mathf.Max(0, currentVelocity.y);
//         }
//         else
//         {
//             var accel = moveVector * moveSpeed * airAcceleration * deltaTime;
//             var airLateralMoveVector = Vector3.ClampMagnitude(
//                 new Vector3(currentVelocity.x, 0, currentVelocity.z) + accel, moveSpeed);
//             currentVelocity.x = airLateralMoveVector.x;
//             currentVelocity.z = airLateralMoveVector.z;
//         }

//         currentVelocity.y += jumpVector.y + WorldRep.gravity * gravityScale * deltaTime;
//         currentVelocity += mantlePushVector;
//         mantlePushVector = Vector3.Lerp(mantlePushVector, Vector3.zero, mantlePushDecay * deltaTime);

//         const float minMoveDirectionToTurnToView = .1f;
//         const float minMoveDirectionToTurnToViewSq = minMoveDirectionToTurnToView * minMoveDirectionToTurnToView;

//         if (relativeMoveDirection.sqrMagnitude > minMoveDirectionToTurnToViewSq)
//         {
//             var faceDirection = moveVector;
//             faceDirection.y = 0;
//             targetRotation = Quaternion.LookRotation(faceDirection);
//             isTurning = true;
//         }
//         else
//         {
//             isTurning = false;
//         }
//     }

//     private void UpdateMantlingMovement(ref Vector3 currentVelocity, float deltaTime)
//     {
//         mantlingTimer -= deltaTime;
//         if (mantlingTimer < 0)
//         {
//             locomotionState = LocomotionState.PLAYER_CONTROL;
//             return;
//         }

//         var diff = targetMantlePosition.y - FeetPosition.y;
//         if (Mathf.Abs(diff) > mantleTargetDistanceEpisloon)
//         {
//             currentVelocity = Vector3.up * mantleRiseSpeed;
//             return;
//         }

//         locomotionState = LocomotionState.PLAYER_CONTROL;
//         var towards = Vector3.Normalize(targetMantlePosition - FeetPosition);
//         mantlePushVector = towards * mantlePushForce;
//     }

//     public override void Move(Vector3 direction)
//     {
//         motor.BaseVelocity = direction * speed;
//     }

//     public override void TakeDamage(float amount)
//     {
//         health -= amount;
//         if (health <= 0)
//         {
//             // Handle player death
//         }
//     }

//     public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
//     {
//         if (!photonView.IsMine) return;

//         var jumpHeld = core.inputManager.PlayerJumpHeld;
//         var jumpedThisFrame = jumpHeld && !jumpHeldLastPhysicsFrame;
//         jumpHeldLastPhysicsFrame = jumpHeld;

//         if (locomotionState != LocomotionState.MANTLING && CheckMantleSurface(out var position))
//         {
//             core.uiManager.ReticleHandler.SetText(UI.TextLocation.BottomPlayer, "Climb (space)");
//             if (jumpedThisFrame)
//             {
//                 motor.ForceUnground(0.1f);
//                 locomotionState = LocomotionState.MANTLING;
//                 targetMantlePosition = position;
//                 mantlingTimer = 2f;
//                 return;
//             }
//         }
//         else
//         {
//             core.uiManager.ReticleHandler.SetText(UI.TextLocation.BottomPlayer, "");
//         }

//         switch (locomotionState)
//         {
//             case LocomotionState.PLAYER_CONTROL:
//                 UpdatePlayerControlMovement(ref currentVelocity, deltaTime, jumpedThisFrame);
//                 break;
//             case LocomotionState.MANTLING:
//                 UpdateMantlingMovement(ref currentVelocity, deltaTime);
//                 break;
//         }
//     }

//     public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
//     {
//         if (!isTurning)
//         {
//             return;
//         }
//         currentRotation = Quaternion.RotateTowards(currentRotation, targetRotation, turnSpeed * deltaTime);
//     }

//     public bool IsColliderValidForCollisions(Collider coll)
//     {
//         return true;
//     }

//     public void BeforeCharacterUpdate(float deltaTime) { }
//     public void PostGroundingUpdate(float deltaTime) { }
//     public void AfterCharacterUpdate(float deltaTime) { }
//     public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
//     public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
//     public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) { }
//     public void OnDiscreteCollisionDetected(Collider hitCollider) { }

//     public void HandleInput()
//     {
//         if (!photonView.IsMine) return;

//         // Add your player input handling code here
//         // This method should be called from Update() or FixedUpdate()
//     }

//     public override void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
//     {
//         if (stream.IsWriting)
//         {
//             // We own this player: send the others our data
//             stream.SendNext(transform.position);
//             stream.SendNext(transform.rotation);
//             stream.SendNext(currentLocation);
//         }
//         else
//         {
//             // Network player, receive data
//             transform.position = (Vector3)stream.ReceiveNext();
//             transform.rotation = (Quaternion)stream.ReceiveNext();
//             currentLocation = (string)stream.ReceiveNext();
//         }
//     }
// }

