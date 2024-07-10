using UnityEngine;
using Photon.Pun;

public class UniversalCharacterController : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("Character Settings")]
    public string characterName;
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float rotationSpeed = 120f;
    public float jumpHeight = 1f;
    public float gravity = -9.81f;
    public float interactionDistance = 3f;

    [Header("Component References")]
    private CharacterController characterController;
    private GameObject cameraRigInstance;

    [Header("Movement Variables")]
    private Vector3 moveDirection;
    private float rotationY;
    private Vector3 verticalVelocity;
    private bool isGrounded;

    public bool IsPlayerControlled { get; private set; }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    [PunRPC]
    public void Initialize(string name, bool isPlayerControlled)
    {
        characterName = name;
        IsPlayerControlled = isPlayerControlled;

        if (photonView.IsMine && isPlayerControlled)
        {
            SetupCamera();
        }

        Debug.Log($"Initialized character: {characterName}, IsPlayerControlled: {IsPlayerControlled}, IsMine: {photonView.IsMine}");
    }

    private void SetupCamera()
    {
        GameObject cameraRigPrefab = Resources.Load<GameObject>("CameraRig");
        if (cameraRigPrefab != null)
        {
            cameraRigInstance = Instantiate(cameraRigPrefab, Vector3.zero, Quaternion.identity);
            
            if (cameraRigInstance != null)
            {
                com.ootii.Cameras.CameraController cameraController = cameraRigInstance.GetComponent<com.ootii.Cameras.CameraController>();
                if (cameraController != null)
                {
                    cameraController.Anchor = this.transform;
                    KaryoUnityInputSource inputSource = cameraRigInstance.GetComponent<KaryoUnityInputSource>();
                    if (inputSource == null)
                    {
                        inputSource = cameraRigInstance.AddComponent<KaryoUnityInputSource>();
                    }
                    cameraController.InputSource = inputSource;
                }
                else
                {
                    Debug.LogError("CameraController component not found on CameraRig prefab.");
                }
            }
            else
            {
                Debug.LogError("Failed to instantiate CameraRig prefab.");
            }
        }
        else
        {
            Debug.LogError("CameraRig prefab not found in Resources folder.");
        }
    }

    private void Update()
    {
        if (photonView.IsMine && IsPlayerControlled)
        {
            HandleInput();
            Move();
            Rotate();
        }
    }

    private void HandleInput()
    {
        moveDirection = InputManager.Instance.PlayerRelativeMoveDirection;
        rotationY += Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;

        if (InputManager.Instance.PlayerJumpActivate && isGrounded)
        {
            Jump();
        }
    }

    private void Move()
    {
        isGrounded = characterController.isGrounded;

        if (isGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2f;
        }

        float currentSpeed = InputManager.Instance.PlayerRunModifier ? runSpeed : walkSpeed;
        Vector3 move = transform.right * moveDirection.x + transform.forward * moveDirection.z;
        characterController.Move(move * currentSpeed * Time.deltaTime);

        verticalVelocity.y += gravity * Time.deltaTime;
        characterController.Move(verticalVelocity * Time.deltaTime);
    }

    private void Jump()
    {
        verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
    }

    private void Rotate()
    {
        transform.rotation = Quaternion.Euler(0, rotationY, 0);
    }

    public bool IsPlayerInRange(Transform playerTransform)
    {
        return Vector3.Distance(transform.position, playerTransform.position) <= interactionDistance;
    }

    public string[] GetDialogueOptions()
    {
        // Placeholder dialogue options
        return new string[]
        {
            "Tell me about your work.",
            "What's your opinion on the current challenge?",
            "How can we collaborate?"
        };
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else
        {
            transform.position = (Vector3)stream.ReceiveNext();
            transform.rotation = (Quaternion)stream.ReceiveNext();
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