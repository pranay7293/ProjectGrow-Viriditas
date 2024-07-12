using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;
using System.Threading.Tasks;

public class UniversalCharacterController : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("Character Settings")]
    public string characterName;
    public Color characterColor = Color.white;
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float rotationSpeed = 120f;
    public float jumpHeight = 1f;
    public float gravity = -20f;
    public float interactionDistance = 3f;
    public float rotationSmoothTime = 0.1f;

    [Header("AI Settings")]
    public string characterRole;
    public string characterBackground;
    [TextArea(3, 10)]
    public string characterPersonality;

    private AIManager aiManager;

    [Header("Component References")]
    private CharacterController characterController;
    private GameObject cameraRigInstance;
    private Renderer characterRenderer;

    [Header("Movement Variables")]
    private Vector3 moveDirection;
    private float rotationY;
    private Vector3 verticalVelocity;
    private bool isGrounded;

    public bool IsPlayerControlled { get; private set; }

    public enum CharacterState
    {
        Idle,
        Moving,
        Interacting,
        PerformingAction
    }

    private CharacterState currentState = CharacterState.Idle;

    private void Awake()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        characterController = GetComponent<CharacterController>();
        characterRenderer = GetComponentInChildren<Renderer>();
        aiManager = GetComponent<AIManager>();
    }

    [PunRPC]
    public void Initialize(string name, bool isPlayerControlled, float r, float g, float b)
    {
        SetCharacterProperties(name, isPlayerControlled, new Color(r, g, b));
        if (photonView.IsMine && isPlayerControlled)
        {
            SetupCamera();
        }
        else if (!isPlayerControlled)
        {
            aiManager.Initialize(this);
        }
    }

    private void SetCharacterProperties(string name, bool isPlayerControlled, Color color)
    {
        characterName = name;
        IsPlayerControlled = isPlayerControlled;
        characterColor = color;

        if (characterRenderer != null)
        {
            characterRenderer.material.color = characterColor;
        }
    }

    private void SetupCamera()
    {
        GameObject cameraRigPrefab = Resources.Load<GameObject>("CameraRig");
        if (cameraRigPrefab != null)
        {
            cameraRigInstance = Instantiate(cameraRigPrefab, Vector3.zero, Quaternion.identity);
            ConfigureCameraController();
        }
    }

    private void ConfigureCameraController()
    {
        if (cameraRigInstance != null)
        {
            com.ootii.Cameras.CameraController cameraController = cameraRigInstance.GetComponent<com.ootii.Cameras.CameraController>();
            if (cameraController != null)
            {
                cameraController.Anchor = this.transform;
                EnsureInputSource(cameraController);
            }
        }
    }

    private void EnsureInputSource(com.ootii.Cameras.CameraController cameraController)
    {
        KaryoUnityInputSource inputSource = cameraRigInstance.GetComponent<KaryoUnityInputSource>();
        if (inputSource == null)
        {
            inputSource = cameraRigInstance.AddComponent<KaryoUnityInputSource>();
        }
        cameraController.InputSource = inputSource;
    }

    private void Update()
    {
        if (photonView.IsMine)
        {
            if (IsPlayerControlled)
            {
                HandlePlayerInput();
            }
            else
            {
                HandleAIInput();
            }
            Move();
            UpdateCameraRotation();
        }
    }

    private void HandlePlayerInput()
    {
        moveDirection = InputManager.Instance.PlayerRelativeMoveDirection;
        rotationY += Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;

        if (InputManager.Instance.PlayerJumpActivate && isGrounded)
        {
            Jump();
        }
    }

    private void HandleAIInput()
    {
        Vector3 targetPosition = aiManager.GetTargetPosition();
        moveDirection = (targetPosition - transform.position).normalized;
        
        if (aiManager.ShouldJump() && isGrounded)
        {
            Jump();
        }
    }

    private void Move()
    {
        UpdateGroundedState();
        ApplyMovement();
        ApplyGravity();
        RotateCharacter();
    }

    private void UpdateGroundedState()
    {
        isGrounded = characterController.isGrounded;
        if (isGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2f;
        }
    }

    private void ApplyMovement()
    {
        Vector3 movement = transform.right * moveDirection.x + transform.forward * moveDirection.z;
        float currentSpeed = IsPlayerControlled && InputManager.Instance.PlayerRunModifier ? runSpeed : walkSpeed;
        characterController.Move(movement * currentSpeed * Time.deltaTime);
    }

    private void ApplyGravity()
    {
        verticalVelocity.y += gravity * Time.deltaTime;
        characterController.Move(verticalVelocity * Time.deltaTime);
    }

    private void RotateCharacter()
    {
        Vector3 movement = transform.right * moveDirection.x + transform.forward * moveDirection.z;
        if (movement.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void UpdateCameraRotation()
    {
        if (IsPlayerControlled)
        {
            Quaternion cameraRotation = Quaternion.Euler(0, rotationY, 0);
            transform.rotation = cameraRotation;
        }
    }

    private void Jump()
    {
        verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
    }

    public bool IsPlayerInRange(Transform playerTransform)
    {
        return Vector3.Distance(transform.position, playerTransform.position) <= interactionDistance;
    }

    public async Task<string[]> GetDialogueOptions()
    {
        if (photonView.IsMine)
        {
            string prompt = $"Generate 3 short dialogue options for {characterName}. Each option should be a single sentence. Separate the options with a newline character.";
            string response = await OpenAIService.Instance.GetChatCompletionAsync(prompt);
            return response.Split('\n');
        }
        return new string[0];
    }

    public void SetState(CharacterState newState)
    {
        currentState = newState;
    }

    public CharacterState GetState()
    {
        return currentState;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext((int)currentState);
        }
        else
        {
            transform.position = (Vector3)stream.ReceiveNext();
            transform.rotation = (Quaternion)stream.ReceiveNext();
            currentState = (CharacterState)stream.ReceiveNext();
        }
    }

    public override void OnDisable()
    {
        base.OnDisable();
        if (cameraRigInstance != null && photonView.IsMine)
        {
            Destroy(cameraRigInstance);
        }
    }
}