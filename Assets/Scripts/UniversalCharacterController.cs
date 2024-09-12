using UnityEngine;
using Photon.Pun;
using System.Collections;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class UniversalCharacterController : MonoBehaviourPunCallbacks, IPunObservable
{
    public enum CharacterState
    {
        None = 0,
        Moving = 1,
        Idle = 2,
        Interacting = 3,
        Acclimating = 4,
        PerformingAction = 5,
        Chatting = 6,
        Collaborating = 7,
        Cooldown = 8
    }

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

    public LocationManager.LocationAction currentAction;
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

    public bool IsCollaborating { get; private set; }

    private HashSet<CharacterState> activeStates = new HashSet<CharacterState>();

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

        if (aiSettings == null)
        {
            aiSettings = ScriptableObject.CreateInstance<AISettings>();
            aiSettings.characterRole = isPlayerControlled ? "Player" : "Default AI";
        }
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
            else if (!HasState(CharacterState.Interacting))
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
        if (HasState(CharacterState.Moving))
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
            AddState(CharacterState.Moving);
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

    public void AddState(CharacterState newState)
    {
        activeStates.Add(newState);
        UpdateProgressBarState();
    }

    public void RemoveState(CharacterState state)
    {
        activeStates.Remove(state);
        UpdateProgressBarState();
    }

    public bool HasState(CharacterState state)
    {
        return activeStates.Contains(state);
    }

    public CharacterState GetHighestPriorityState()
    {
        return activeStates.Count > 0 ? activeStates.Max() : CharacterState.None;
    }

    private void UpdateProgressBarState()
    {
        if (progressBar != null)
        {
            progressBar.UpdateKeyState(GetHighestPriorityState());
        }
    }

    public void StartAction(LocationManager.LocationAction action)
    {
        if (currentAction != null)
        {
            StopAction();
        }

        currentAction = action;
        actionStartTime = Time.time;
        AddState(CharacterState.PerformingAction);
        UpdateGoalProgress(action.actionName);

        if (actionCoroutine != null)
        {
            StopCoroutine(actionCoroutine);
        }
        actionCoroutine = StartCoroutine(PerformAction());

        ActionLogManager.Instance.LogAction(characterName, $"Started action: {action.actionName}");
        GameManager.Instance.UpdateGameState(characterName, action.actionName);
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
            if (RiskRewardManager.Instance != null)
            {
                RiskRewardManager.Instance.EvaluateActionOutcome(this, currentAction?.actionName ?? "Unknown Action");
            }
            
            if (CollabManager.Instance != null && IsCollaborating)
            {
                CollabManager.Instance.FinalizeCollaboration(currentAction.actionName, currentAction.duration);
            }
        }
        
        if (currentAction != null)
        {
            CheckPersonalGoalProgress(currentAction.actionName);
        }
        currentAction = null;
        RemoveState(CharacterState.PerformingAction);
    }

    public void StopAction()
    {
        if (actionCoroutine != null)
        {
            StopCoroutine(actionCoroutine);
        }
        currentAction = null;
        RemoveState(CharacterState.PerformingAction);
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
    GameManager.Instance.UpdatePlayerScore(characterName, ScoreConstants.PERSONAL_GOAL_COMPLETION, "Personal Goal Completion", new List<string> { "PersonalGoal", goal });
    GameManager.Instance.UpdateGameState(characterName, $"Completed personal goal: {goal}");
}

    private void UpdateGoalProgress(string actionName)
{
    for (int i = 0; i < PersonalProgress.Length; i++)
    {
        if (aiSettings.personalGoals[i].ToLower().Contains(actionName.ToLower()))
        {
            PersonalProgress[i] = Mathf.Min(PersonalProgress[i] + 0.25f, 1f);
        }
    }

    if (progressBar != null)
    {
        progressBar.SetPersonalGoalProgress(PersonalProgress);
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

    public void UpdateProgress(Dictionary<string, float> progress)
    {
    if (progressBar != null)
    {
        progressBar.SetPersonalGoalProgress(progress.Values.ToArray());
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
            RemoveState(CharacterState.Moving);
            StartAcclimation();
        }
    }

    private void StartAcclimation()
    {
        locationEntryTime = Time.time;
        isAcclimating = true;
        AddState(CharacterState.Acclimating);
        if (progressBar != null)
        {
            progressBar.StartAcclimation();
        }
    }

    private void UpdateAcclimation()
    {
        float elapsedTime = Time.time - locationEntryTime;
        if (elapsedTime >= acclimationTime)
        {
            isAcclimating = false;
            RemoveState(CharacterState.Acclimating);
            if (progressBar != null)
            {
                progressBar.EndAcclimation();
            }
        }
        else
        {
            float progress = 1 - (elapsedTime / acclimationTime);
            if (progressBar != null)
            {
                progressBar.UpdateAcclimationProgress(progress);
            }
        }
    }

    public Color GetCharacterColor()
    {
        return characterColor;
    }

   public void InitiateCollab(string actionName, UniversalCharacterController collaborator)
{
    if (photonView.IsMine && !IsCollaborating)
    {
        LocationManager.LocationAction action = currentLocation.availableActions.Find(a => a.actionName == actionName);
        if (action != null)
        {
            currentAction = action;
            photonView.RPC("RPC_InitiateCollab", RpcTarget.All, actionName, photonView.ViewID, collaborator.photonView.ViewID);
        }
        else
        {
            Debug.LogWarning($"InitiateCollab: Action {actionName} not found for {characterName}");
        }
    }
}

    [PunRPC]
    private void RPC_InitiateCollab(string actionName, int initiatorViewID, int collaboratorViewID)
    {
        CollabManager.Instance.InitiateCollab(actionName, initiatorViewID, collaboratorViewID);
        IsCollaborating = true;
        AddState(CharacterState.Collaborating);
    }

    public void JoinCollab(string actionName, UniversalCharacterController initiator)
{
    if (photonView.IsMine && !IsCollaborating)
    {
        LocationManager.LocationAction action = currentLocation.availableActions.Find(a => a.actionName == actionName);
        if (action != null)
        {
            currentAction = action;
            photonView.RPC("RPC_JoinCollab", RpcTarget.All, actionName, initiator.photonView.ViewID, photonView.ViewID);
        }
        else
        {
            Debug.LogWarning($"JoinCollab: Action {actionName} not found for {characterName}");
        }
    }
}

    [PunRPC]
    private void RPC_JoinCollab(string actionName, int initiatorViewID, int joinerViewID)
    {
        CollabManager.Instance.InitiateCollab(actionName, initiatorViewID, joinerViewID);
        IsCollaborating = true;
        AddState(CharacterState.Collaborating);
    }

    public void EndCollab(string actionName)
{
    if (photonView.IsMine && IsCollaborating)
    {
        IsCollaborating = false;
        if (currentAction != null)
        {
            photonView.RPC("RPC_EndCollab", RpcTarget.All, actionName, currentAction.duration);
        }
        else
        {
            Debug.LogWarning($"EndCollab called for {characterName} but currentAction is null");
            photonView.RPC("RPC_EndCollab", RpcTarget.All, actionName, 0);
        }
    }
}

[PunRPC]
private void RPC_EndCollab(string actionName, int actionDuration)
{
    CollabManager.Instance.FinalizeCollaboration(actionName, actionDuration);
    RemoveState(CharacterState.Collaborating);
    AddState(CharacterState.Cooldown);
}

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(activeStates.ToArray());
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
            activeStates = new HashSet<CharacterState>((CharacterState[])stream.ReceiveNext());
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

            UpdateProgressBarState();
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

    private void OnCollisionStay(Collision collision)
    {
        UniversalCharacterController otherCharacter = collision.gameObject.GetComponent<UniversalCharacterController>();
        if (otherCharacter != null && navMeshAgent != null && navMeshAgent.enabled)
        {
            Vector3 pushDirection = (transform.position - collision.transform.position).normalized;
            pushDirection.y = 0; // Ensure no vertical movement
            navMeshAgent.Move(pushDirection * Time.deltaTime * 2f); // Adjust the multiplier as needed
        }
    }
}