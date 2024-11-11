using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;
using System.Collections;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using EPOOutline;

public class UniversalCharacterController : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("Character Settings")]
    public string characterName;
    public Color characterColor = Color.white;
    public float walkSpeed = 2f;
    public float runSpeed = 5f;
    public float rotationSpeed = 10f;
    public float interactionDistance = 5f;

    [Header("Movement Settings")]
    public float accelerationTime = 0.1f;
    public float decelerationTime = 0.1f;
    public float rotationSmoothTime = 0.1f;
    private Vector3 stateMovementDestination;
    private float stateMovementSpeed;

    [Header("AI Settings")]
    public AISettings aiSettings;

    [Header("Gameplay")]
    public string currentObjective;
    public float acclimationTime = 5f;
    public float initialDelay = 10f;
    [SerializeField] private float spawnProtectionTime = 2f;

    [Header("Special Character Settings")]
    public bool hasWhiteLabCoat = false;

    [Header("Character Materials")]
    public Material coatMaterial;
    public Material shirtMaterial;
    public Material pantsMaterial;
    public Material sneakersMaterial;
    public Material logoMaterial;
    public Material baseMaterial;

    [HideInInspector] public LocationManager currentLocation;

    private List<string> personalGoalTags = new List<string>();
    private Dictionary<string, bool> personalGoalCompletion = new Dictionary<string, bool>();

    public AIManager aiManager;
    private CharacterController characterController;
    private NavMeshAgent navMeshAgent;
    private Animator animator;
    private TextMeshPro actionIndicator;

    public LocationManager.LocationAction currentAction;
    private float actionStartTime;
    private Coroutine actionCoroutine;
    public string CurrentActionName => currentAction?.actionName ?? "";

    private Vector3 moveDirection;
    private float currentSpeed;
    private float targetSpeed;
    private Quaternion targetRotation;
    private Vector3 smoothDampVelocity;
    private float currentRotationVelocity;

    public bool IsPlayerControlled { get; private set; }

    private float lastDialogueAttemptTime = 0f;
    private const float DialogueCooldown = 0.5f;

    public float[] PersonalProgress { get; private set; } = new float[3];
    public int EurekaCount { get; private set; }

    private CharacterProgressBar progressBar;
    private float locationEntryTime;
    private bool isAcclimating = false;
    private Coroutine acclimationCoroutine;

    public bool IsCollaborating { get; private set; }
    public string currentCollabID;
    public float CollaborationTimeElapsed { get; private set; }

    private CharacterState activeStates = CharacterState.None;

    private List<LocationManager.LocationAction> availableActions = new List<LocationManager.LocationAction>();

    public GameObject guideTextBoxPrefab;
    private GameObject guideTextBox;
    private TextMeshProUGUI guideText;
    private float guideDisplayDuration = 2f;
    private float guideFadeDuration = 0.5f;

    private Outlinable outlinable;

    private string currentGroupId;
    private GameObject cameraRigInstance;

    private bool hasSpawnProtection = false;
    private Coroutine spawnProtectionCoroutine;

    private const float MinimumActionDuration = 3f;
    private float lastActionTime;

    private void Awake()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        characterController = GetComponent<CharacterController>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
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

        InitializeGuideTextBox();
        InitializeMaterials();
        InitializeOutline();
    }

    private void InitializeMaterials()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer.gameObject.name.Contains("Coat"))
            {
                coatMaterial = renderer.material;
            }
            else if (renderer.gameObject.name.Contains("Shirt"))
            {
                shirtMaterial = renderer.material;
            }
            else if (renderer.gameObject.name.Contains("Pants"))
            {
                pantsMaterial = renderer.material;
            }
            else if (renderer.gameObject.name.Contains("Sneakers"))
            {
                sneakersMaterial = renderer.material;
            }
            else if (renderer.gameObject.name.Contains("Logo"))
            {
                logoMaterial = renderer.material;
            }
            else if (renderer.gameObject.name.Contains("Base"))
            {
                baseMaterial = renderer.material;
            }
        }
    }

    private void InitializeOutline()
    {
        outlinable = GetComponentInChildren<Outlinable>();
        if (outlinable == null)
        {
            Debug.LogWarning($"Outlinable component not found for {characterName}. Adding one.");
            GameObject modelObject = transform.Find("ProjectGrow-CharacterModel (Rigged)")?.gameObject;
            if (modelObject != null)
            {
                outlinable = modelObject.AddComponent<Outlinable>();
            }
            else
            {
                Debug.LogError($"Model object not found for {characterName}. Outline functionality will be disabled.");
            }
        }

        if (outlinable != null)
        {
            outlinable.OutlineParameters.Color = new Color32(13, 134, 248, 255);
            outlinable.OutlineParameters.BlurShift = 0;
            outlinable.OutlineParameters.DilateShift = 0.5f;
            outlinable.OutlineParameters.Enabled = true;
            outlinable.enabled = false;
        }
    }

    private void InitializeGuideTextBox()
    {
        if (guideTextBoxPrefab != null)
        {
            guideTextBox = Instantiate(guideTextBoxPrefab, transform);
            guideTextBox.transform.localPosition = new Vector3(0, 3f, 0);
            guideTextBox.transform.localRotation = Quaternion.identity;
            guideText = guideTextBox.GetComponentInChildren<TextMeshProUGUI>();
            guideTextBox.SetActive(false);
        }
        else
        {
            Debug.LogError("CharacterGuideTextBox prefab not assigned to UniversalCharacterController.");
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
            StartSpawnProtection();
        }
        InitializePersonalGoals();
        StartCoroutine(DelayedAcclimation());

        if (aiSettings == null)
        {
            aiSettings = ScriptableObject.CreateInstance<AISettings>();
            aiSettings.characterRole = isPlayerControlled ? "Player" : "Default AI";
        }

        UpdateCharacterColor();
    }

    private IEnumerator DelayedAcclimation()
    {
        yield return new WaitForSeconds(initialDelay);
        if (currentLocation != null)
        {
            StartAcclimation();
        }
    }

    private void SetCharacterProperties(string name, bool isPlayerControlled, Color color)
    {
        characterName = name;
        IsPlayerControlled = isPlayerControlled;
        characterColor = color;
    }

    private void UpdateCharacterColor()
    {
        Color baseColor = new Color(224f/255f, 224f/255f, 224f/255f);
        Color whiteColor = Color.white;
        Color logoColor = hasWhiteLabCoat ? Color.black : Color.white;

        if (coatMaterial != null) coatMaterial.color = characterColor;
        if (shirtMaterial != null) shirtMaterial.color = whiteColor;
        if (pantsMaterial != null) pantsMaterial.color = whiteColor;
        if (sneakersMaterial != null) sneakersMaterial.color = characterColor;
        if (logoMaterial != null) logoMaterial.color = logoColor;
        if (baseMaterial != null) baseMaterial.color = baseColor;
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

        navMeshAgent.speed = walkSpeed;
        navMeshAgent.acceleration = 8f;
        navMeshAgent.angularSpeed = rotationSpeed * 10;
    }

    private Camera playerCamera;

    public Camera PlayerCamera
    {
        get { return playerCamera; }
    }

    private void SetupCamera()
    {
        GameObject cameraRigPrefab = Resources.Load<GameObject>("CameraRig");
        if (cameraRigPrefab != null)
        {
            cameraRigInstance = Instantiate(cameraRigPrefab, Vector3.zero, Quaternion.identity);
            ConfigureCameraController();

            playerCamera = cameraRigInstance.GetComponentInChildren<Camera>();
            if (playerCamera == null)
            {
                Debug.LogError("Camera component not found in CameraRig");
            }
            else
            {
                Outliner outliner = playerCamera.GetComponent<Outliner>();
                if (outliner == null)
                {
                    outliner = playerCamera.gameObject.AddComponent<Outliner>();
                }

                playerCamera.tag = "MainCamera";
            }
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
        if (!photonView.IsMine) return;

        UpdateMovement();
        UpdateAnimator();
        UpdateRotation();
        UpdateMovementState();


        if (HasState(CharacterState.Chatting) || HasState(CharacterState.Collaborating) || HasState(CharacterState.InGroup))
        {
            HandleStateMovement();
        }

        if (HasState(CharacterState.Moving))
        {
            // Debug.Log($"{characterName}: Current position: {transform.position}, Destination: {stateMovementDestination}, Distance: {Vector3.Distance(transform.position, stateMovementDestination)}");
        }

        if (isAcclimating)
        {
            UpdateAcclimation();
        }

        if (IsCollaborating)
        {
            UpdateCollaboration();
        }
    }

    private void HandleStateMovement()
    {
        if (navMeshAgent != null && navMeshAgent.enabled && stateMovementDestination != Vector3.zero)
        {
            navMeshAgent.speed = stateMovementSpeed;
            navMeshAgent.SetDestination(stateMovementDestination);
        }
    }

    private void UpdateCollaboration()
    {
        CollaborationTimeElapsed += Time.deltaTime;
    }

    public void StartCollaboration(LocationManager.LocationAction action, string collabID)
    {
        IsCollaborating = true;
        AddState(CharacterState.Collaborating);
        currentCollabID = collabID;
        currentAction = action;
        CollaborationTimeElapsed = 0f;
        StartAction(action);
        Debug.Log($"{characterName} started collaboration on {action.actionName} with ID {collabID}");
    }

    private void UpdateMovement()
    {   
        if (IsPlayerControlled)
        {
            HandlePlayerMovement();
        }
        else
        {
            HandleAIMovement();
        }
    }

    private void HandlePlayerMovement()
    {
        Vector3 input = InputManager.Instance.PlayerRelativeMoveDirection;
        bool isRunning = InputManager.Instance.PlayerRunModifier;

        targetSpeed = input.magnitude > 0.1f ? (isRunning ? runSpeed : walkSpeed) : 0f;
        currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref smoothDampVelocity.y, accelerationTime);

        if (input.magnitude > 0.1f)
        {
            moveDirection = PlayerCamera.transform.TransformDirection(input).normalized;
            moveDirection.y = 0;
            targetRotation = Quaternion.LookRotation(moveDirection);
        }

        if (characterController.enabled)
        {
            Vector3 motion = moveDirection * currentSpeed;
            characterController.Move(motion * Time.deltaTime);
        }

        UpdateMovementState();
    }

    private void HandleAIMovement()
    {
        if (HasState(CharacterState.PerformingAction))
        {
            return;
        }

        if (navMeshAgent.enabled && navMeshAgent.hasPath)
        {
            currentSpeed = navMeshAgent.velocity.magnitude;
            moveDirection = navMeshAgent.desiredVelocity.normalized;

            if (moveDirection != Vector3.zero)
            {
                targetRotation = Quaternion.LookRotation(moveDirection);
            }
        }
        else
        {
            currentSpeed = 0f;
        }

        UpdateMovementState();
    }

    private void UpdateMovementState()
    {
        if (currentSpeed > 0.1f)
        {
            AddState(CharacterState.Moving);
            RemoveState(CharacterState.Idle);
        }
        else
        {
            RemoveState(CharacterState.Moving);
            AddState(CharacterState.Idle);
        }
    }

    public void StopMoving()
    {
        if (navMeshAgent != null && navMeshAgent.enabled)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.ResetPath();
        }
        RemoveState(CharacterState.Moving);
        AddState(CharacterState.Idle);

        Debug.Log($"{characterName}: Stopped moving.");
    }

    public void ResumeMoving()
    {
        if (navMeshAgent != null && navMeshAgent.enabled)
        {
            navMeshAgent.isStopped = false;
        }
        RemoveState(CharacterState.Idle);
        AddState(CharacterState.Moving);
    }

    private void UpdateRotation()
    {
        if (targetRotation != Quaternion.identity)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void UpdateAnimator()
    {
        float normalizedSpeed = currentSpeed / runSpeed;
        animator.SetFloat("Speed", normalizedSpeed);
        animator.SetFloat("MotionSpeed", normalizedSpeed);
    }

    private void OnAnimatorMove()
    {
        if (animator.applyRootMotion)
        {
            Vector3 position = animator.rootPosition;
            position.y = transform.position.y;
            transform.position = position;
        }
    }

    public void TriggerDialogue()
    {
        if (!photonView.IsMine) return;
        if (Time.time - lastDialogueAttemptTime < DialogueCooldown) return;

        lastDialogueAttemptTime = Time.time;
        UniversalCharacterController nearestCharacter = FindNearestCharacter();
        if (nearestCharacter != null && IsPlayerInRange(nearestCharacter.transform))
        {
            DialogueManager.Instance.InitiateDialogue(nearestCharacter);
        }
    }

    private UniversalCharacterController FindNearestCharacter()
    {
        UniversalCharacterController[] characters = FindObjectsOfType<UniversalCharacterController>();
        UniversalCharacterController nearest = null;
        float minDistance = float.MaxValue;

        foreach (UniversalCharacterController character in characters)
        {
            if (character != this)
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

    public void MoveTo(Vector3 destination)
    {
        if (navMeshAgent != null && navMeshAgent.enabled)
        {
            navMeshAgent.isStopped = false;
            navMeshAgent.SetDestination(destination);
            AddState(CharacterState.Moving);
            RemoveState(CharacterState.Idle);

            Debug.Log($"{characterName}: Moving to {destination}");
        }
        else
        {
            Debug.LogWarning($"{characterName}: NavMeshAgent is not active or enabled. Cannot move.");
        }
    }

    public bool IsPlayerInRange(Transform targetTransform)
    {
        return Vector3.Distance(transform.position, targetTransform.position) <= interactionDistance;
    }

    [PunRPC]
    public void SetObjective(string objective)
    {
        currentObjective = objective;
    }

    public void AddState(CharacterState state)
    {
        CharacterState previousStates = activeStates;
        if (state == CharacterState.Moving)
        {
            activeStates &= ~CharacterState.Idle;
        }
        else if (state == CharacterState.Idle)
        {
            activeStates &= ~CharacterState.Moving;
        }
        activeStates |= state;
        UpdateProgressBarState();
        
        // Debug.Log($"{characterName}: Added state {state}. Previous states: {previousStates}, New states: {activeStates}");
    }

    public void RemoveState(CharacterState state)
    {
        CharacterState previousStates = activeStates;
        activeStates &= ~state;
        if (state == CharacterState.Moving && !HasState(CharacterState.Idle))
        {
            AddState(CharacterState.Idle);
        }
        UpdateProgressBarState();
        
        // Debug.Log($"{characterName}: Removed state {state}. Previous states: {previousStates}, New states: {activeStates}");
    }

    public bool HasState(CharacterState state)
    {
        return (activeStates & state) != 0;
    }

    public CharacterState GetHighestPriorityState()
    {
        for (int i = 9; i >= 0; i--)
        {
            CharacterState state = (CharacterState)(1 << i);
            if (HasState(state))
            {
                return state;
            }
        }
        return CharacterState.None;
    }

    private void UpdateProgressBarState()
    {
        if (progressBar != null)
        {
            progressBar.UpdateKeyState(GetHighestPriorityState());
        }
    }

    public void MoveWhileInState(Vector3 destination, float speed)
    {
        if (HasState(CharacterState.PerformingAction))
        {
            // Debug.Log($"{characterName}: Cannot move while performing action.");
            return;
        }

        if (Time.time - lastActionTime < MinimumActionDuration)
        {
            // Debug.Log($"{characterName}: Cannot move yet. Minimum action duration not met.");
            return;
        }

        if (IsInGroup())
        {
            // Let GroupManager handle movement
            return;
        }

        stateMovementDestination = destination;
        stateMovementSpeed = speed;

        if (navMeshAgent != null && navMeshAgent.enabled)
        {
            navMeshAgent.speed = speed;
            navMeshAgent.SetDestination(destination);
            // Debug.Log($"{characterName}: Moving to {destination} at speed {speed}. NavMeshAgent.hasPath: {navMeshAgent.hasPath}, NavMeshAgent.pathStatus: {navMeshAgent.pathStatus}");
        }
        else
        {
            Debug.LogWarning($"{characterName}: NavMeshAgent is null or disabled. Cannot move.");
        }

        AddState(CharacterState.Moving);
        lastActionTime = Time.time;
    }

    public void StartAction(LocationManager.LocationAction action)
    {
        if (HasState(CharacterState.PerformingAction))
        {
            ShowGuide("Already performing an action. Please wait.");
            return;
        }

        if (Time.time - lastActionTime < MinimumActionDuration)
        {
            Debug.Log($"{characterName}: Cannot start new action yet. Minimum action duration not met.");
            return;
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

        photonView.RPC("RPC_StartAction", RpcTarget.All, action.actionName);
        lastActionTime = Time.time;
    }

    [PunRPC]
    private void RPC_StartAction(string actionName)
    {
        if (currentLocation != null && !HasState(CharacterState.PerformingAction))
        {
            LocationManager.LocationAction action = currentLocation.GetActionByName(actionName);
            if (action != null)
            {
                StartAction(action);
            }
        }
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

    public void CompleteAction()
    {
        if (photonView.IsMine || !IsPlayerControlled)
        {
            if (CollabManager.Instance != null && IsCollaborating && !string.IsNullOrEmpty(currentCollabID))
            {
                CollabManager.Instance.FinalizeCollaboration(currentCollabID);
                IsCollaborating = false;
                RemoveState(CharacterState.Collaborating);
                currentCollabID = null;
            }
            else if (currentAction != null)
            {
                GameManager.Instance.UpdateGameState(characterName, currentAction.actionName);
            }

            if (currentAction != null)
            {
                CheckPersonalGoalProgress(currentAction.actionName);
            }
            currentAction = null;
            RemoveState(CharacterState.PerformingAction);
            // Debug.Log($"{characterName} completed action: {currentAction?.actionName}");

            if (LocationActionUI.Instance != null)
            {
                LocationActionUI.Instance.HideActions();
            }

            lastActionTime = Time.time;
        }
    }

    private void InitializePersonalGoals()
    {
        personalGoalTags = new List<string>(aiSettings.personalGoalTags);
        foreach (string goalTag in personalGoalTags)
        {
            personalGoalCompletion[goalTag] = false;
        }
    }

    private void CheckPersonalGoalProgress(string action)
    {
        foreach (string goalTag in personalGoalTags)
        {
            if (!personalGoalCompletion[goalTag] && action.ToLower().Contains(goalTag.ToLower()))
            {
                CompletePersonalGoal(goalTag);
                break;
            }
        }
    }

    private void CompletePersonalGoal(string goalTag)
    {
        personalGoalCompletion[goalTag] = true;
        GameManager.Instance.UpdatePlayerScore(characterName, ScoreConstants.PERSONAL_GOAL_COMPLETION, "Personal Goal Completion", new List<string> { "PersonalGoal", goalTag });
        GameManager.Instance.UpdateGameState(characterName, $"Completed personal goal: {goalTag}");
    }

    public void UpdateGoalProgress(string actionName)
    {
        List<(string tag, float weight)> tagsWithWeights = TagSystem.GetTagsForAction(actionName);
        float[] progressUpdate = new float[aiSettings.personalGoalTags.Count];

        foreach (var (tag, weight) in tagsWithWeights)
        {
            if (aiSettings.tagToSliderIndex.TryGetValue(tag, out int sliderIndex))
            {
                progressUpdate[sliderIndex] += weight;
            }
        }

        for (int i = 0; i < progressUpdate.Length; i++)
        {
            PersonalProgress[i] = Mathf.Clamp01(PersonalProgress[i] + progressUpdate[i]);
        }

        if (progressBar != null)
        {
            progressBar.SetPersonalGoalProgress(PersonalProgress);
        }

        GameManager.Instance.UpdateMilestoneProgress(characterName, actionName, tagsWithWeights);
    }

    public List<string> GetPersonalGoalTags()
    {
        return new List<string>(personalGoalTags);
    }

    public Dictionary<string, bool> GetPersonalGoalCompletion()
    {
        return new Dictionary<string, bool>(personalGoalCompletion);
    }

    public void UpdateProgress(Dictionary<string, float> progress)
    {
        if (progressBar != null)
        {
            float[] progressArray = new float[3];
            for (int i = 0; i < 3; i++)
            {
                if (i < aiSettings.personalGoalTags.Count && progress.TryGetValue(aiSettings.personalGoalTags[i], out float value))
                {
                    progressArray[i] = value;
                }
                else
                {
                    progressArray[i] = 0f;
                }
            }
            progressBar.SetPersonalGoalProgress(progressArray);
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
        if (hasSpawnProtection)
        {
            return;
        }

        currentLocation = location;
        StartAcclimation();
        availableActions.Clear();
        location.UpdateCharacterAvailableActions(this);

        if (!IsPlayerControlled)
        {
            NPC_Behavior npcBehavior = GetComponent<NPC_Behavior>();
            if (npcBehavior != null)
            {
                npcBehavior.SetCurrentLocation(location);
            }
        }
    }

    public void UpdateAvailableActions(List<LocationManager.LocationAction> actions)
    {
        availableActions = new List<LocationManager.LocationAction>(actions);
    }

    public bool IsActionAvailable(string actionName)
    {
        return availableActions.Exists(a => a.actionName == actionName);
    }

    public void ExitLocation()
    {
        if (HasState(CharacterState.PerformingAction))
        {
            ShowGuide("Cannot leave while performing an action.");
            return;
        }

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

            StopAcclimation();
            currentLocation = null;
            availableActions.Clear();
        }
    }

    private void ShowGuide(string message)
    {
        if (guideTextBox != null && guideText != null)
        {
            guideText.text = message;
            guideTextBox.SetActive(true);
            StartCoroutine(FadeGuide(true));
        }
    }

    private IEnumerator FadeGuide(bool fadeIn)
    {
        float elapsedTime = 0f;
        Color startColor = guideText.color;
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, fadeIn ? 1f : 0f);

        while (elapsedTime < guideFadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(startColor.a, targetColor.a, elapsedTime / guideFadeDuration);
            guideText.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        guideText.color = targetColor;

        if (fadeIn)
        {
            yield return new WaitForSeconds(guideDisplayDuration);
            StartCoroutine(FadeGuide(false));
        }
        else
        {
            guideTextBox.SetActive(false);
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
            StartSpawnProtection();
        }
    }

    private void StartSpawnProtection()
    {
        hasSpawnProtection = true;
        if (spawnProtectionCoroutine != null)
        {
            StopCoroutine(spawnProtectionCoroutine);
        }
        spawnProtectionCoroutine = StartCoroutine(SpawnProtectionCoroutine());
    }

    private IEnumerator SpawnProtectionCoroutine()
    {
        yield return new WaitForSeconds(spawnProtectionTime);
        hasSpawnProtection = false;
        if (currentLocation != null)
        {
            StartAcclimation();
        }
    }

    private void StartAcclimation()
    {
        if (acclimationCoroutine != null)
        {
            StopCoroutine(acclimationCoroutine);
        }
        acclimationCoroutine = StartCoroutine(AcclimationCoroutine());
    }

    private IEnumerator AcclimationCoroutine()
    {
        isAcclimating = true;
        AddState(CharacterState.Acclimating);
        if (progressBar != null)
        {
            progressBar.StartAcclimation();
        }

        locationEntryTime = Time.time;
        float elapsedTime = 0f;
        while (elapsedTime < acclimationTime)
        {
            if (currentLocation == null)
            {
                break;
            }

            elapsedTime = Time.time - locationEntryTime;
            float progress = elapsedTime / acclimationTime;
            if (progressBar != null)
            {
                progressBar.UpdateAcclimationProgress(1 - progress);
            }
            yield return null;
        }

        FinishAcclimation();
    }

    private void FinishAcclimation()
    {
        isAcclimating = false;
        RemoveState(CharacterState.Acclimating);
        if (progressBar != null)
        {
            progressBar.EndAcclimation();
        }

        if (IsPlayerControlled && photonView.IsMine && currentLocation != null)
        {
            LocationActionUI.Instance.ShowActionsForLocation(this, currentLocation);
        }
    }

    private void StopAcclimation()
    {
        if (acclimationCoroutine != null)
        {
            StopCoroutine(acclimationCoroutine);
        }
        isAcclimating = false;
        RemoveState(CharacterState.Acclimating);
        if (progressBar != null)
        {
            progressBar.EndAcclimation();
        }
    }

    private void UpdateAcclimation()
    {
        if (isAcclimating && currentLocation != null)
        {
            float elapsedTime = Time.time - locationEntryTime;
            float progress = elapsedTime / acclimationTime;
            if (progressBar != null)
            {
                progressBar.UpdateAcclimationProgress(1 - progress);
            }
            if (progress >= 1f)
            {
                FinishAcclimation();
            }
        }
    }

    public Color GetCharacterColor()
    {
        return characterColor;
    }

    public void InitiateCollab(string actionName, UniversalCharacterController collaborator)
    {
        if (!photonView.IsMine || IsCollaborating || currentLocation == null || 
            HasState(CharacterState.Acclimating) || collaborator == null)
        {
            return;
        }

        if (CollabManager.Instance.CanInitiateCollab(this) && IsActionAvailable(actionName))
        {
            LocationManager.LocationAction action = availableActions.Find(a => a.actionName == actionName);
            if (action != null)
            {
                currentAction = action;
                CollabManager.Instance.InitiateCollab(actionName, photonView.ViewID, new int[] { collaborator.photonView.ViewID }, System.Guid.NewGuid().ToString());
            }
        }
    }

    [PunRPC]
    private void RPC_InitiateCollab(string actionName, int initiatorViewID, int[] collaboratorViewIDs, string collabID)
    {
        CollabManager.Instance.InitiateCollab(actionName, initiatorViewID, collaboratorViewIDs, collabID);
        IsCollaborating = true;
        AddState(CharacterState.Collaborating);
        currentCollabID = collabID;
    }

    public void JoinCollab(string actionName, UniversalCharacterController initiator)
    {
        if (photonView.IsMine && !IsCollaborating)
        {
            LocationManager.LocationAction action = availableActions.Find(a => a.actionName == actionName);
            if (action != null)
            {
                currentAction = action;
                string collabID = System.Guid.NewGuid().ToString();
                photonView.RPC("RPC_JoinCollab", RpcTarget.All, actionName, initiator.photonView.ViewID, new int[] { photonView.ViewID }, collabID);
            }
            else
            {
                Debug.LogWarning($"JoinCollab: Action {actionName} not found for {characterName}");
            }
        }
    }

    [PunRPC]
    private void RPC_JoinCollab(string actionName, int initiatorViewID, int[] collaboratorViewIDs, string collabID)
    {
        CollabManager.Instance.InitiateCollab(actionName, initiatorViewID, collaboratorViewIDs, collabID);
        IsCollaborating = true;
        AddState(CharacterState.Collaborating);
        currentCollabID = collabID;
    }

    public void EndCollab()
    {
        if (photonView.IsMine && IsCollaborating)
        {
            IsCollaborating = false;
            if (currentAction != null)
            {
                photonView.RPC("RPC_EndCollab", RpcTarget.All, currentAction.actionName, currentAction.duration);
            }
            else
            {
                Debug.LogWarning($"EndCollab called for {characterName} but currentAction is null");
                photonView.RPC("RPC_EndCollab", RpcTarget.All, "Unknown", 0);
            }
        }
    }

    [PunRPC]
    private void RPC_EndCollab(string actionName, int actionDuration)
    {
        if (!string.IsNullOrEmpty(currentCollabID))
        {
            CollabManager.Instance.FinalizeCollaboration(currentCollabID);
        }
        RemoveState(CharacterState.Collaborating);
        AddState(CharacterState.Cooldown);
        currentCollabID = null;
    }

    public void JoinGroup(string groupId)
    {
        if (currentGroupId != null)
        {
            LeaveGroup(false);
        }
        currentGroupId = groupId;
        AddState(CharacterState.InGroup);
    }

    public void LeaveGroup(bool shouldDisband = true)
    {
        if (currentGroupId != null)
        {
            if (shouldDisband && GroupManager.Instance != null)
            {
                GroupManager.Instance.DisbandGroup(currentGroupId);
            }
            currentGroupId = null;
            RemoveState(CharacterState.InGroup);
        }
    }

    public bool IsInGroup()
    {
        return !string.IsNullOrEmpty(currentGroupId);
    }

    public string GetCurrentGroupId()
    {
        return currentGroupId;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Sending data
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext((int)activeStates);
            stream.SendNext(currentObjective);
            stream.SendNext(actionIndicator.text);
            stream.SendNext(characterColor);
            stream.SendNext(PersonalProgress);
            stream.SendNext(EurekaCount);
            stream.SendNext(currentAction != null ? currentAction.actionName : "");
            stream.SendNext(actionStartTime);
            stream.SendNext(currentCollabID ?? "");
            stream.SendNext(currentGroupId ?? "");
            stream.SendNext(currentSpeed);
            stream.SendNext(moveDirection);
            stream.SendNext(IsCollaborating);
            stream.SendNext(CollaborationTimeElapsed);
            stream.SendNext(isAcclimating);
            stream.SendNext(locationEntryTime);
        }
        else
        {
            // Receiving data
            transform.position = (Vector3)stream.ReceiveNext();
            transform.rotation = (Quaternion)stream.ReceiveNext();
            activeStates = (CharacterState)stream.ReceiveNext();
            currentObjective = (string)stream.ReceiveNext();
            actionIndicator.text = (string)stream.ReceiveNext();
            Color receivedColor = (Color)stream.ReceiveNext();
            PersonalProgress = (float[])stream.ReceiveNext();
            EurekaCount = (int)stream.ReceiveNext();
            string actionName = (string)stream.ReceiveNext();
            actionStartTime = (float)stream.ReceiveNext();
            currentCollabID = (string)stream.ReceiveNext();
            currentGroupId = (string)stream.ReceiveNext();
            currentSpeed = (float)stream.ReceiveNext();
            moveDirection = (Vector3)stream.ReceiveNext();
            IsCollaborating = (bool)stream.ReceiveNext();
            CollaborationTimeElapsed = (float)stream.ReceiveNext();
            isAcclimating = (bool)stream.ReceiveNext();
            locationEntryTime = (float)stream.ReceiveNext();

            if (string.IsNullOrEmpty(currentCollabID))
            {
                currentCollabID = null;
            }

            if (string.IsNullOrEmpty(currentGroupId))
            {
                currentGroupId = null;
            }

            if (!string.IsNullOrEmpty(actionName) && currentAction == null && currentLocation != null)
            {
                LocationManager.LocationAction action = availableActions.Find(a => a.actionName == actionName);
                if (action != null)
                {
                    StartAction(action);
                }
            }

            if (receivedColor != characterColor)
            {
                characterColor = receivedColor;
                UpdateCharacterColor();
            }

            UpdateProgressBarState();
            UpdateAnimator();
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
        if (otherCharacter != null)
        {
            Vector3 pushDirection = (transform.position - collision.transform.position).normalized;
            pushDirection.y = 0; // Ensure the push is horizontal only
            if (navMeshAgent != null && navMeshAgent.enabled)
            {
                navMeshAgent.Move(pushDirection * Time.deltaTime * 2f);
            }
            else if (characterController != null && characterController.enabled)
            {
                characterController.Move(pushDirection * Time.deltaTime * 2f);
            }
        }
    }

    public void ShowOutline()
    {
        if (outlinable != null)
        {
            outlinable.enabled = true;
        }
    }

    public void HideOutline()
    {
        if (outlinable != null)
        {
            outlinable.enabled = false;
        }
    }
}