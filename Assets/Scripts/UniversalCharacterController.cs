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

// using UnityEngine;
// using Photon.Pun;
// using KinematicCharacterController;

// public class UniversalCharacterController : MonoBehaviourPunCallbacks, IPunObservable
// {
//     public bool IsPlayerControlled { get; private set; }
//     [SerializeField] private string characterName;
    
//     private KinematicCharacterMotor motor;
//     private NPC npcController;
//     private Player playerController;
//     private PhotonView photonView;
    
//     private void Awake()
//     {
//         motor = GetComponent<KinematicCharacterMotor>();
//         npcController = GetComponent<NPC>();
//         playerController = GetComponent<Player>();
//         photonView = GetComponent<PhotonView>();

//         if (photonView.InstantiationData != null)
//         {
//             Initialize((string)photonView.InstantiationData[0], (bool)photonView.InstantiationData[1]);
//             if (!IsPlayerControlled)
//             {
//                 InitializeNPCData(photonView.InstantiationData);
//             }
//         }
//     }
    
//     [PunRPC]
//     public void Initialize(string name, bool isPlayer)
//     {
//         characterName = name;
//         IsPlayerControlled = isPlayer;

//         if (photonView.IsMine)
//         {
//             if (IsPlayerControlled)
//             {
//                 playerController.enabled = true;
//                 npcController.enabled = false;
//                 SetupCamera();
//             }
//             else
//             {
//                 playerController.enabled = false;
//                 npcController.enabled = true;
//             }
//         }
//         else
//         {
//             playerController.enabled = false;
//             npcController.enabled = !IsPlayerControlled;
//         }
//     }

//     private void InitializeNPCData(object[] instantiationData)
//     {
//         if (npcController != null)
//         {
//             npcController.InitializeNPCData(instantiationData);
//         }
//     }
    
//     private void SetupCamera()
//     {
//         if (Camera.main != null)
//         {
//             Camera.main.gameObject.SetActive(false);
//         }

//         GameObject cameraRigPrefab = Resources.Load<GameObject>("CameraRig");
//         if (cameraRigPrefab != null)
//         {
//             GameObject cameraRig = Instantiate(cameraRigPrefab, transform.position, Quaternion.identity);
//             com.ootii.Cameras.CameraController cameraController = cameraRig.GetComponent<com.ootii.Cameras.CameraController>();
//             if (cameraController != null)
//             {
//                 cameraController.Anchor = this.transform;
//             }
//             else
//             {
//                 Debug.LogError("CameraController component not found on CameraRig prefab");
//             }
//         }
//         else
//         {
//             Debug.LogError("CameraRig prefab not found in Resources folder");
//         }
//     }
    
//     private void Update()
//     {
//         if (!photonView.IsMine) return;
        
//         if (IsPlayerControlled)
//         {
//             playerController.HandleInput();
//         }
//         else
//         {
//             npcController.HandleAI();
//         }
//     }
    
//     public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
//     {
//         if (stream.IsWriting)
//         {
//             stream.SendNext(transform.position);
//             stream.SendNext(transform.rotation);
//         }
//         else
//         {
//             transform.position = (Vector3)stream.ReceiveNext();
//             transform.rotation = (Quaternion)stream.ReceiveNext();
//         }
//     }

//     public void SwitchControlMode(bool toPlayerControl)
//     {
//         if (photonView.IsMine)
//         {
//             IsPlayerControlled = toPlayerControl;
//             playerController.enabled = toPlayerControl;
//             npcController.enabled = !toPlayerControl;

//             if (toPlayerControl)
//             {
//                 SetupCamera();
//             }
//         }
//     }
// }