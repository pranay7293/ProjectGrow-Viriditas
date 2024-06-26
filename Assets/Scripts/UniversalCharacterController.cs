using UnityEngine;
using Photon.Pun;
using KinematicCharacterController;

public class UniversalCharacterController : MonoBehaviourPunCallbacks, IPunObservable
{
    public bool IsPlayerControlled { get; private set; }
    [SerializeField] private string characterName;
    
    private KinematicCharacterMotor motor;
    private NPC npcController;
    private Player playerController;
    private PhotonView photonView;
    private Camera playerCamera;
    
    private void Awake()
    {
        motor = GetComponent<KinematicCharacterMotor>();
        npcController = GetComponent<NPC>();
        playerController = GetComponent<Player>();
        photonView = GetComponent<PhotonView>();

        if (photonView.IsMine)
        {
            // Initialize local player components
            playerController.enabled = true;
            npcController.enabled = false;
            IsPlayerControlled = true;
            SetupCamera();
        }
        else
        {
            // Initialize AI components for non-local players
            playerController.enabled = false;
            npcController.enabled = true;
            IsPlayerControlled = false;
        }
    }
    
    [PunRPC]
    public void Initialize(string name, bool isPlayer)
    {
        characterName = name;
        IsPlayerControlled = isPlayer;
    }
    
    private void SetupCamera()
    {
        playerCamera = Camera.main;
        if (playerCamera != null)
        {
            playerCamera.transform.SetParent(transform);
            playerCamera.transform.localPosition = new Vector3(0, 1.6f, 0);
        }
    }
    
    private void Update()
    {
        if (!photonView.IsMine) return;
        
        if (IsPlayerControlled)
        {
            playerController.HandleInput();
        }
        else
        {
            npcController.HandleAI();
        }
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
}