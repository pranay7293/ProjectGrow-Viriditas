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
    
    private void Awake()
    {
        motor = GetComponent<KinematicCharacterMotor>();
        npcController = GetComponent<NPC>();
        playerController = GetComponent<Player>();
        photonView = GetComponent<PhotonView>();

        if (photonView.InstantiationData != null)
        {
            Initialize((string)photonView.InstantiationData[0], (bool)photonView.InstantiationData[1]);
            if (!IsPlayerControlled)
            {
                InitializeNPCData(photonView.InstantiationData);
            }
        }
    }
    
    [PunRPC]
    public void Initialize(string name, bool isPlayer)
    {
        characterName = name;
        IsPlayerControlled = isPlayer;

        if (photonView.IsMine)
        {
            if (IsPlayerControlled)
            {
                playerController.enabled = true;
                npcController.enabled = false;
                SetupCamera();
            }
            else
            {
                playerController.enabled = false;
                npcController.enabled = true;
            }
        }
        else
        {
            playerController.enabled = false;
            npcController.enabled = !IsPlayerControlled;
        }
    }

    private void InitializeNPCData(object[] instantiationData)
    {
        if (npcController != null)
        {
            npcController.InitializeNPCData(instantiationData);
        }
    }
    
    private void SetupCamera()
    {
        if (Camera.main != null)
        {
            Camera.main.gameObject.SetActive(false);
        }

        GameObject cameraRigPrefab = Resources.Load<GameObject>("CameraRig");
        if (cameraRigPrefab != null)
        {
            GameObject cameraRig = Instantiate(cameraRigPrefab, transform.position, Quaternion.identity);
            com.ootii.Cameras.CameraController cameraController = cameraRig.GetComponent<com.ootii.Cameras.CameraController>();
            if (cameraController != null)
            {
                cameraController.Anchor = this.transform;
            }
            else
            {
                Debug.LogError("CameraController component not found on CameraRig prefab");
            }
        }
        else
        {
            Debug.LogError("CameraRig prefab not found in Resources folder");
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

    public void SwitchControlMode(bool toPlayerControl)
    {
        if (photonView.IsMine)
        {
            IsPlayerControlled = toPlayerControl;
            playerController.enabled = toPlayerControl;
            npcController.enabled = !toPlayerControl;

            if (toPlayerControl)
            {
                SetupCamera();
            }
        }
    }
}