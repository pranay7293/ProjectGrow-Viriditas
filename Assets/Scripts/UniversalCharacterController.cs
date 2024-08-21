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
    public string currentObjective;
    public float acclimationTime = 10f;
    public float initialDelay = 10f;

    [HideInInspector] public LocationManager currentLocation;

    private List<string> personalGoals = new List<string>();
    private Dictionary<string, bool> personalGoalCompletion = new Dictionary<string, bool>();

    private AIManager aiManager;
    private CharacterController characterController;
    private UnityEngine.AI.NavMeshAgent navMeshAgent;
    private GameObject cameraRigInstance;
    private Renderer characterRenderer;
    private Material characterMaterial;
    private TextMeshPro actionIndicator;

    private LocationManager.LocationAction currentAction;
    private float actionStartTime;
    private Coroutine actionCoroutine;
    public string CurrentActionName => currentAction?.actionName ?? "";

    private Vector3 moveDirection;
    private float rotationY;

    public bool IsPlayerControlled { get; private set; }

    private float lastDialogueAttemptTime = 0f;
    private const float DialogueCooldown = 0.5f;

    public float[] PersonalProgress { get; private set; } = new float[3];
    public int EurekaCount { get; private set; }

    private CharacterProgressBar progressBar;
    private float locationEntryTime;
    private bool isAcclimating = false;

    public enum CharacterState
    {
        Idle,
        Moving,
        Interacting,
        PerformingAction,
        Acclimating
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
            InitializeProgressBar();
        }
        InitializePersonalGoals();
        StartCoroutine(DelayedAcclimation());
    }

    private IEnumerator DelayedAcclimation()
    {
        yield return new WaitForSeconds(initialDelay);
        StartAcclimation();
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

    private void InitializeProgressBar()
    {
        if (progressBar != null)
        {
            Debug.LogWarning($"Progress bar already initialized for {characterName}");
            return;
        }

        GameObject progressBarPrefab = Resources.Load<GameObject>("CharacterProgressBar");
        if (progressBarPrefab == null)
        {
            Debug.LogError($"CharacterProgressBar prefab not found in Resources folder for {characterName}");
            return;
        }

        GameObject progressBarObject = Instantiate(progressBarPrefab, transform);
        progressBar = progressBarObject.GetComponent<CharacterProgressBar>();
        if (progressBar == null)
        {
            Debug.LogError($"CharacterProgressBar component not found on instantiated prefab for {characterName}");
            Destroy(progressBarObject);
            return;
        }

        progressBarObject.transform.localPosition = new Vector3(0, 2.25f, 0);
        progressBarObject.transform.localRotation = Quaternion.identity;
        progressBar.Initialize(this);

        Debug.Log($"Progress bar initialized for {characterName}");
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

            if (isAcclimating)
            {
                UpdateAcclimation();
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

    public void SetState(CharacterState newState)
    {
        currentState = newState;
        if (progressBar != null)
        {
            switch (newState)
            {
                case CharacterState.PerformingAction:
                    progressBar.SetKeyState(KeyState.PerformingAction);
                    break;
                case CharacterState.Interacting:
                    progressBar.SetKeyState(KeyState.Chatting);
                    break;
                case CharacterState.Acclimating:
                    progressBar.SetKeyState(KeyState.Acclimating);
                    break;
                default:
                    progressBar.SetKeyState(KeyState.None);
                    break;
            }
        }
    }

    public CharacterState GetState()
    {
        return currentState;
    }

    public void StartAction(LocationManager.LocationAction action)
    {
        if (currentAction != null)
        {
            StopAction();
        }

        currentAction = action;
        actionStartTime = Time.time;
        SetState(CharacterState.PerformingAction);
        UpdateGoalProgress(action.actionName);

        if (actionCoroutine != null)
        {
            StopCoroutine(actionCoroutine);
        }
        actionCoroutine = StartCoroutine(PerformAction());

        ActionLogManager.Instance.LogAction(characterName, action.actionName);
        GameManager.Instance.UpdateGameState(characterName, action.actionName);

        if (progressBar != null)
        {
            progressBar.SetKeyState(KeyState.PerformingAction);
        }

        Debug.Log($"{characterName} started action: {action.actionName}");
    }

    private IEnumerator PerformAction()
    {
        float elapsedTime = 0f;
        while (elapsedTime < currentAction.duration)
        {
            elapsedTime = Time.time - actionStartTime;
            float progress = elapsedTime / currentAction.duration;
            
            if (photonView.IsMine)
            {
                LocationActionUI.Instance.UpdateActionProgress(currentAction.actionName, progress);
            }

            yield return null;
        }

        CompleteAction();
    }

    private void CompleteAction()
    {
        if (photonView.IsMine)
        {
            RiskRewardManager.Instance.EvaluateActionOutcome(this, currentAction);
        }
        
        CheckPersonalGoalProgress(currentAction.actionName);
        currentAction = null;
        SetState(CharacterState.Idle);
    }

    public void StopAction()
    {
        if (actionCoroutine != null)
        {
            StopCoroutine(actionCoroutine);
        }
        currentAction = null;
        SetState(CharacterState.Idle);
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
        GameManager.Instance.UpdatePlayerScore(characterName, ScoreConstants.PERSONAL_GOAL_COMPLETION_BONUS);
        GameManager.Instance.UpdateGameState(characterName, $"Completed personal goal: {goal}");
    }

    private void UpdateGoalProgress(string actionName)
    {
        for (int i = 0; i < PersonalProgress.Length; i++)
        {
            if (aiSettings.personalGoals[i].ToLower().Contains(actionName.ToLower()))
            {
                PersonalProgress[i] = Mathf.Min(PersonalProgress[i] + 0.25f, 1f);
                GameManager.Instance.UpdatePlayerProgress(this, PersonalProgress);
            }
        }

        GameManager.Instance.UpdateMilestoneProgress(characterName, actionName);
    }

    public List<string> GetPersonalGoals()
    {
        return new List<string>(personalGoals);
    }

    public Dictionary<string, bool> GetPersonalGoalCompletion()
    {
        return new Dictionary<string, bool>(personalGoalCompletion);
    }

    public void UpdateProgress(float[] personalProgress)
    {
        PersonalProgress = personalProgress;
        if (progressBar != null)
        {
            progressBar.UpdatePersonalGoals();
        }
        if (photonView.IsMine)
        {
            GameManager.Instance.UpdatePlayerProgress(this, PersonalProgress);
        }
    }

    public void IncrementEurekaCount()
    {
        EurekaCount++;
        if (photonView.IsMine)
        {
            GameManager.Instance.UpdatePlayerEurekas(this, EurekaCount);
        }
    }

    public void EnterLocation(LocationManager location)
    {
        currentLocation = location;
        StartAcclimation();
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
        yield return new WaitForSeconds(acclimationTime);
        if (LocationActionUI.Instance != null && currentLocation == location)
        {
            LocationActionUI.Instance.ShowActionsForLocation(this, location);
        }
        else
        {
            Debug.LogWarning("LocationActionUI.Instance is null or location has changed");
        }
    }

    public void ExitLocation()
    {
        if (currentLocation != null)
        {
            if (IsPlayerControlled && photonView.IsMine)
            {
                if (LocationActionUI.Instance != null)
                {
                    LocationActionUI.Instance.HideActions();
                }
            }
            else
            {
                NPC_Behavior npcBehavior = GetComponent<NPC_Behavior>();
                if (npcBehavior != null)
                {
                    npcBehavior.SetCurrentLocation(null);
                }
            }
            
            if (progressBar != null)
            {
                progressBar.EndAcclimation();
            }
            
            currentLocation = null;
        }
    }

    public void ResetToSpawnPoint(Vector3 spawnPosition)
    {
        if (photonView.IsMine)
        {
            if (IsPlayerControlled)
            {
                characterController.enabled = false;
                transform.position = spawnPosition;
                characterController.enabled = true;
            }
            else
            {
                navMeshAgent.Warp(spawnPosition);
            }
            SetState(CharacterState.Idle);
            StartAcclimation();
        }
    }

  private void StartAcclimation()
{
    Debug.Log("Starting acclimation");
    locationEntryTime = Time.time;
    isAcclimating = true;
    CharacterProgressBar progressBar = GetComponentInChildren<CharacterProgressBar>();
    if (progressBar != null && currentLocation != null)
    {
        progressBar.StartAcclimation(currentLocation.locationColor);
    }
    else
    {
        Debug.LogWarning("ProgressBar or CurrentLocation is null in StartAcclimation");
    }
}

private void UpdateAcclimation()
{
    if (progressBar == null) return;

    float elapsedTime = Time.time - locationEntryTime;
    if (elapsedTime >= acclimationTime)
    {
        isAcclimating = false;
        SetState(CharacterState.Idle);
        progressBar.EndAcclimation();
    }
    else
    {
        float progress = 1 - (elapsedTime / acclimationTime);
        progressBar.UpdateAcclimationProgress(progress);
    }
}

    public bool IsCollaborating { get; private set; }

    public void InitiateCollab(string actionName)
    {
        if (photonView.IsMine && !IsCollaborating && !string.IsNullOrEmpty(actionName))
        {
            if (CollabManager.Instance.CanInitiateCollab(this))
            {
                IsCollaborating = true;
                photonView.RPC("RPC_InitiateCollab", RpcTarget.All, actionName, photonView.ViewID);
            }
        }
    }

    [PunRPC]
    private void RPC_InitiateCollab(string actionName, int initiatorViewID)
    {
        CollabManager.Instance.InitiateCollab(actionName, initiatorViewID);
        if (progressBar != null)
        {
            progressBar.SetKeyState(KeyState.Collaborating);
        }
    }

    public void JoinCollab(string actionName)
    {
        if (photonView.IsMine && !IsCollaborating && CollabManager.Instance.CanInitiateCollab(this))
        {
            IsCollaborating = true;
            photonView.RPC("RPC_JoinCollab", RpcTarget.All, actionName, photonView.ViewID);
        }
    }

    [PunRPC]
    private void RPC_JoinCollab(string actionName, int joinerViewID)
    {
        CollabManager.Instance.JoinCollab(actionName, joinerViewID);
        if (progressBar != null)
        {
            progressBar.SetKeyState(KeyState.Collaborating);
        }
    }

    public void EndCollab(string actionName)
    {
        if (photonView.IsMine && IsCollaborating)
        {
            IsCollaborating = false;
            photonView.RPC("RPC_EndCollab", RpcTarget.All, actionName);
        }
    }

    [PunRPC]
    private void RPC_EndCollab(string actionName)
    {
        CollabManager.Instance.FinalizeCollaboration(actionName);
        if (progressBar != null)
        {
            progressBar.SetKeyState(KeyState.None);
            progressBar.SetCooldown(CollabManager.Instance.GetCollabCooldown());
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext((int)currentState);
            stream.SendNext(currentObjective);
            stream.SendNext(actionIndicator.text);
            stream.SendNext(characterColor);
            stream.SendNext(PersonalProgress);
            stream.SendNext(EurekaCount);
            stream.SendNext(currentAction != null ? currentAction.actionName : "");
            stream.SendNext(actionStartTime);
        }
        else
        {
            transform.position = (Vector3)stream.ReceiveNext();
            transform.rotation = (Quaternion)stream.ReceiveNext();
            currentState = (CharacterState)stream.ReceiveNext();
            currentObjective = (string)stream.ReceiveNext();
            actionIndicator.text = (string)stream.ReceiveNext();
            Color receivedColor = (Color)stream.ReceiveNext();
            PersonalProgress = (float[])stream.ReceiveNext();
            EurekaCount = (int)stream.ReceiveNext();
            string actionName = (string)stream.ReceiveNext();
            actionStartTime = (float)stream.ReceiveNext();

            if (!string.IsNullOrEmpty(actionName) && currentAction == null && currentLocation != null)
            {
                LocationManager.LocationAction action = currentLocation.availableActions.Find(a => a.actionName == actionName);
                if (action != null)
                {
                    StartAction(action);
                }
            }

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