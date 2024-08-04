using UnityEngine;
using Photon.Pun;
using System.Collections;
using TMPro;
using System.Collections.Generic;

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
    private List<string> personalGoals = new List<string>();
    private Dictionary<string, bool> personalGoalCompletion = new Dictionary<string, bool>();

    private AIManager aiManager;
    private CharacterController characterController;
    private UnityEngine.AI.NavMeshAgent navMeshAgent;
    private GameObject cameraRigInstance;
    private Renderer characterRenderer;
    private Material characterMaterial;
    private TextMeshPro actionIndicator;

    private Vector3 moveDirection;
    private float rotationY;

    public bool IsPlayerControlled { get; private set; }

    private float lastDialogueAttemptTime = 0f;
    private const float DialogueCooldown = 0.5f;

    public float OverallProgress { get; private set; }
    public float[] PersonalProgress { get; private set; } = new float[3];
    public int InsightCount { get; private set; }

    private LocationManager currentLocation;

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
        if (characterRenderer != null)
        {
            characterMaterial = new Material(characterRenderer.material);
            characterRenderer.material = characterMaterial;
        }
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
        InitializePersonalGoals();
    }

    private void SetCharacterProperties(string name, bool isPlayerControlled, Color color)
    {
        characterName = name;
        IsPlayerControlled = isPlayerControlled;
        characterColor = color;

        if (characterMaterial != null)
        {
            characterMaterial.color = characterColor;
        }
        else
        {
            Debug.LogError($"Character material not found for {characterName}");
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
        if (!photonView.IsMine) return;
        if (Time.time - lastDialogueAttemptTime < DialogueCooldown) return;

        lastDialogueAttemptTime = Time.time;
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
        if (!photonView.IsMine) return;

        SetState(CharacterState.PerformingAction);
        if (actionIndicator != null)
        {
            actionIndicator.text = actionName + "...";
            actionIndicator.gameObject.SetActive(true);
            StartCoroutine(FadeOutActionIndicator());
        }
        CheckPersonalGoalProgress(actionName);

        // Log the action
        ActionLogManager.Instance.LogAction(characterName, actionName);

        // Notify GameManager about the action
        GameManager.Instance.UpdateGameState(characterName, actionName);

        Debug.Log($"{characterName} performed action: {actionName}");

        // Trigger the RiskRewardManager to evaluate the outcome
        RiskRewardManager.Instance.EvaluateActionOutcome(this, new LocationManager.LocationAction { actionName = actionName });
    }

    private IEnumerator FadeOutActionIndicator()
    {
        yield return new WaitForSeconds(3f);
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

    private void InitializePersonalGoals()
    {
        personalGoals = new List<string>(aiSettings.personalGoals);
        foreach (string goal in personalGoals)
        {
            personalGoalCompletion[goal] = false;
        }
    }

    private void CheckPersonalGoalProgress(string action)
    {
        foreach (string goal in personalGoals)
        {
            if (!personalGoalCompletion[goal] && action.ToLower().Contains(goal.ToLower()))
            {
                CompletePersonalGoal(goal);
                break;
            }
        }
    }

    private void CompletePersonalGoal(string goal)
    {
        personalGoalCompletion[goal] = true;
        UpdatePersonalScore(50);
        GameManager.Instance.UpdateGameState(characterName, $"Completed personal goal: {goal}");
    }

    public List<string> GetPersonalGoals()
    {
        return new List<string>(personalGoals);
    }

    public Dictionary<string, bool> GetPersonalGoalCompletion()
    {
        return new Dictionary<string, bool>(personalGoalCompletion);
    }

    public void UpdateProgress(float overallProgress, float[] personalProgress)
    {
        OverallProgress = overallProgress;
        PersonalProgress = personalProgress;
        if (photonView.IsMine)
        {
            PlayerProfileManager.Instance.UpdatePlayerProgress(characterName, OverallProgress, PersonalProgress);
        }
    }

    public void UpdateInsights(int count)
    {
        InsightCount = count;
        if (photonView.IsMine)
        {
            PlayerProfileManager.Instance.UpdatePlayerInsights(characterName, InsightCount);
        }
    }

    public void EnterLocation(LocationManager location)
    {
        currentLocation = location;
        if (IsPlayerControlled && photonView.IsMine)
        {
            StartCoroutine(DelayedShowActions(location));
        }
        else
        {
            NPC_Behavior npcBehavior = GetComponent<NPC_Behavior>();
            if (npcBehavior != null)
            {
                npcBehavior.SetCurrentLocation(location);
            }
        }
    }

    private IEnumerator DelayedShowActions(LocationManager location)
    {
    yield return new WaitForSeconds(2f); // 2 second delay, adjust as needed
    LocationActionUI.Instance.ShowActionsForLocation(this, location);
    }

    public void ExitLocation()
    {
        currentLocation = null;
        if (IsPlayerControlled && photonView.IsMine)
        {
            LocationActionUI.Instance.HideActions();
        }
        else
        {
            NPC_Behavior npcBehavior = GetComponent<NPC_Behavior>();
            if (npcBehavior != null)
            {
                npcBehavior.SetCurrentLocation(null);
            }
        }
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
            stream.SendNext(characterColor);
            stream.SendNext(OverallProgress);
            stream.SendNext(PersonalProgress);
            stream.SendNext(InsightCount);
        }
        else
        {
            transform.position = (Vector3)stream.ReceiveNext();
            transform.rotation = (Quaternion)stream.ReceiveNext();
            currentState = (CharacterState)stream.ReceiveNext();
            personalScore = (int)stream.ReceiveNext();
            currentObjective = (string)stream.ReceiveNext();
            actionIndicator.text = (string)stream.ReceiveNext();
            Color receivedColor = (Color)stream.ReceiveNext();
            OverallProgress = (float)stream.ReceiveNext();
            PersonalProgress = (float[])stream.ReceiveNext();
            InsightCount = (int)stream.ReceiveNext();
            if (characterMaterial != null && receivedColor != characterColor)
            {
                characterColor = receivedColor;
                characterMaterial.color = characterColor;
            }
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