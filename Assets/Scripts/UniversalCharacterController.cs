using UnityEngine;
using Photon.Pun;
using System.Threading.Tasks;

public class UniversalCharacterController : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("Character Settings")]
    public string characterName;
    public Color characterColor = Color.white;
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float rotationSpeed = 120f;
    public float interactionDistance = 3f;

    [Header("AI Settings")]
    public string characterRole;
    public string characterBackground;
    [TextArea(3, 10)]
    public string characterPersonality;

    private AIManager aiManager;
    private CharacterController characterController;
    private UnityEngine.AI.NavMeshAgent navMeshAgent;
    private GameObject cameraRigInstance;
    private Renderer characterRenderer;

    private Vector3 moveDirection;
    private float rotationY;

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
        navMeshAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        characterRenderer = GetComponentInChildren<Renderer>();
        aiManager = GetComponent<AIManager>();
    }

    [PunRPC]
    public void Initialize(string name, bool isPlayerControlled, float r, float g, float b)
    {
        SetCharacterProperties(name, isPlayerControlled, new Color(r, g, b));
        if (photonView.IsMine)
        {
            if (isPlayerControlled)
            {
                SetupPlayerControlled();
            }
            else
            {
                SetupAIControlled();
            }
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

    private void SetupPlayerControlled()
    {
        SetupCamera();
        navMeshAgent.enabled = false;
        characterController.enabled = true;
    }

    private void SetupAIControlled()
    {
        aiManager.Initialize(this);
        navMeshAgent.enabled = true;
        characterController.enabled = false;
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
                MovePlayer();
            }
            else
            {
                HandleAIMovement();
            }
        }
    }

    private void HandlePlayerInput()
    {
        moveDirection = InputManager.Instance.PlayerRelativeMoveDirection;
        rotationY += Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
    }

    private void MovePlayer()
    {
        Vector3 movement = transform.right * moveDirection.x + transform.forward * moveDirection.z;
        float currentSpeed = InputManager.Instance.PlayerRunModifier ? runSpeed : walkSpeed;
        characterController.Move(movement * currentSpeed * Time.deltaTime);

        if (movement.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        Quaternion cameraRotation = Quaternion.Euler(0, rotationY, 0);
        transform.rotation = cameraRotation;
    }

    private void HandleAIMovement()
    {
        // AI movement is handled by the NavMeshAgent component
        if (currentState == CharacterState.Moving)
        {
            navMeshAgent.isStopped = false;
        }
        else
        {
            navMeshAgent.isStopped = true;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (IsPlayerControlled)
        {
            // Ignore collisions with NPCs
            UniversalCharacterController otherCharacter = collision.gameObject.GetComponent<UniversalCharacterController>();
            if (otherCharacter != null && !otherCharacter.IsPlayerControlled)
            {
                Physics.IgnoreCollision(GetComponent<Collider>(), collision.collider);
            }
        }
    }

    public void SetDestination(Vector3 destination)
    {
        if (!IsPlayerControlled && navMeshAgent != null)
        {
            navMeshAgent.SetDestination(destination);
            currentState = CharacterState.Moving;
        }
    }

    public bool IsPlayerInRange(Transform playerTransform)
    {
        return Vector3.Distance(transform.position, playerTransform.position) <= interactionDistance;
    }

    public async Task<string[]> GetDialogueOptions()
    {
        if (!IsPlayerControlled && aiManager != null)
        {
            return await aiManager.GetDialogueOptions();
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