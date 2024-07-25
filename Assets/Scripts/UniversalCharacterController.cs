using UnityEngine;
using Photon.Pun;
using System.Collections;
using TMPro;

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
    public AISettings aiSettings;

    [Header("Gameplay")]
    public int personalScore = 0;
    public string currentObjective;

    private AIManager aiManager;
    private CharacterController characterController;
    private UnityEngine.AI.NavMeshAgent navMeshAgent;
    private GameObject cameraRigInstance;
    private Renderer characterRenderer;
    private TextMeshPro actionIndicator;

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

        actionIndicator = GetComponentInChildren<TextMeshPro>();
        if (actionIndicator == null)
        {
            Debug.LogError("ActionIndicator TextMeshPro component not found on character prefab.");
        }
        else
        {
            actionIndicator.text = "";
            actionIndicator.gameObject.SetActive(false);
        }
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
            else if (currentState != CharacterState.Interacting)
            {
                HandleAIMovement();
            }
        }
    }

    private void HandlePlayerInput()
    {
        moveDirection = InputManager.Instance.PlayerRelativeMoveDirection;
        rotationY += Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.E))
        {
            TriggerDialogue();
        }
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
        if (currentState == CharacterState.Moving)
        {
            navMeshAgent.isStopped = false;
        }
        else
        {
            navMeshAgent.isStopped = true;
        }
    }

    public void TriggerDialogue()
    {
        UniversalCharacterController nearestNPC = FindNearestNPC();
        if (nearestNPC != null && IsPlayerInRange(nearestNPC.transform))
        {
            DialogueManager.Instance.InitiateDialogue(nearestNPC);
        } 
    }

    private UniversalCharacterController FindNearestNPC()
    {
        UniversalCharacterController[] characters = FindObjectsOfType<UniversalCharacterController>();
        UniversalCharacterController nearest = null;
        float minDistance = float.MaxValue;

        foreach (UniversalCharacterController character in characters)
        {
            if (!character.IsPlayerControlled && character != this)
            {
                float distance = Vector3.Distance(transform.position, character.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = character;
                }
            }
        }

        return nearest;
    }

    public void SetDestination(Vector3 destination)
    {
        if (!IsPlayerControlled && navMeshAgent != null)
        {
            navMeshAgent.SetDestination(destination);
            SetState(CharacterState.Moving);
        }
    }

    public bool IsPlayerInRange(Transform playerTransform)
    {
        return Vector3.Distance(transform.position, playerTransform.position) <= interactionDistance;
    }

    [PunRPC]
    public void SetObjective(string objective)
    {
        currentObjective = objective;
    }

    public void UpdatePersonalScore(int points)
    {
        if (photonView.IsMine)
        {
            personalScore += points;
            GameManager.Instance.UpdatePlayerScore(photonView.Owner.NickName, personalScore);
        }
    }

    public void SetState(CharacterState newState)
    {
        currentState = newState;
    }

    public CharacterState GetState()
    {
        return currentState;
    }

    public void PerformAction(string actionName)
    {
        SetState(CharacterState.PerformingAction);
        if (actionIndicator != null)
        {
            actionIndicator.text = actionName + "...";
            actionIndicator.gameObject.SetActive(true);
            StartCoroutine(FadeOutActionIndicator());
        }
        // Implement action-specific logic here
    }

    private IEnumerator FadeOutActionIndicator()
    {
        yield return new WaitForSeconds(3f); // Display for 3 seconds
        float duration = 1f;
        float elapsedTime = 0f;
        Color startColor = actionIndicator.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            actionIndicator.color = Color.Lerp(startColor, endColor, elapsedTime / duration);
            yield return null;
        }
        actionIndicator.text = "";
        actionIndicator.gameObject.SetActive(false);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext((int)currentState);
            stream.SendNext(personalScore);
            stream.SendNext(currentObjective);
            stream.SendNext(actionIndicator.text);
        }
        else
        {
            transform.position = (Vector3)stream.ReceiveNext();
            transform.rotation = (Quaternion)stream.ReceiveNext();
            currentState = (CharacterState)stream.ReceiveNext();
            personalScore = (int)stream.ReceiveNext();
            currentObjective = (string)stream.ReceiveNext();
            actionIndicator.text = (string)stream.ReceiveNext();
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