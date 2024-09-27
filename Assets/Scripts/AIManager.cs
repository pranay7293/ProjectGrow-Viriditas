using UnityEngine;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ProjectGrow.AI;

public class AIManager : MonoBehaviourPunCallbacks
{
    #region Fields
    private UniversalCharacterController characterController;
    private NPC_Behavior npcBehavior;
    private NPC_Data npcData;
    private AIDecisionMaker decisionMaker;

    [SerializeField] private float memoryConsolidationInterval = 60f;
    [SerializeField] private float reflectionInterval = 120f;
    [SerializeField] private float dialogueInitiationCooldown = 300f;
    [SerializeField] private float collabConsiderationInterval = 15f;
    [SerializeField] private float movementConsiderationInterval = 10f;
    [SerializeField] private float explorationProbability = 0.2f;
    private float lastMovementConsiderationTime = 0f;

    private bool isInitialized = false;
    private float lastDialogueInitiationTime = 0f;
    private float lastCollabConsiderationTime = 0f;
    #endregion

    #region Unity Lifecycle Methods
    private void Awake()
    {
        decisionMaker = gameObject.AddComponent<AIDecisionMaker>();
    }

    private void Start()
    {
        StartCoroutine(PeriodicMemoryConsolidation());
        StartCoroutine(PeriodicReflection());
    }

     private void Update()
    {
        if (!photonView.IsMine || !isInitialized) return;

        if (!characterController.HasState(UniversalCharacterController.CharacterState.Chatting) &&
            !characterController.HasState(UniversalCharacterController.CharacterState.Collaborating))
        {
            npcBehavior.UpdateBehavior();
            ConsiderMovement();
            ConsiderCollaboration();
        }
    }
    #endregion

    private void ConsiderMovement()
    {
        if (Time.time - lastMovementConsiderationTime < movementConsiderationInterval) return;

        lastMovementConsiderationTime = Time.time;

        if (characterController.HasState(UniversalCharacterController.CharacterState.Moving) ||
            characterController.HasState(UniversalCharacterController.CharacterState.Acclimating) ||
            characterController.HasState(UniversalCharacterController.CharacterState.PerformingAction))
        {
            return;
        }

        if (Random.value < explorationProbability)
        {
            ExploreRandomWaypoint();
        }
        else
        {
            ConsiderMovingToNewLocation();
        }
    }

    private void ExploreRandomWaypoint()
    {
        Vector3 randomWaypoint = WaypointsManager.Instance.GetRandomWaypoint();
        if (randomWaypoint != Vector3.zero)
        {
            npcBehavior.MoveToPosition(randomWaypoint);
        }
    }

    private void ConsiderMovingToNewLocation()
    {
        string bestLocation = EvaluateBestLocation();
        if (bestLocation != characterController.currentLocation.locationName)
        {
            Vector3 waypointNearLocation = WaypointsManager.Instance.GetWaypointNearLocation(bestLocation);
            npcBehavior.MoveToPosition(waypointNearLocation);
        }
    }

    #region Initialization
    public void Initialize(UniversalCharacterController controller)
    {
        characterController = controller;
        npcBehavior = GetComponent<NPC_Behavior>();
        npcData = GetComponent<NPC_Data>();

        if (npcBehavior != null && npcData != null)
        {
            npcData.Initialize(characterController);
            npcBehavior.Initialize(characterController, npcData, this);
            isInitialized = true;
        }
        else
        {
            Debug.LogError("AIManager: One or more required components are missing.");
        }
    }
    #endregion

    #region Collaboration Logic
   public void ConsiderCollaboration()
    {
        if (Time.time - lastCollabConsiderationTime < collabConsiderationInterval) return;

        lastCollabConsiderationTime = Time.time;

        if (characterController.HasState(UniversalCharacterController.CharacterState.Cooldown) ||
            characterController.HasState(UniversalCharacterController.CharacterState.Acclimating) ||
            characterController.HasState(UniversalCharacterController.CharacterState.PerformingAction) ||
            characterController.HasState(UniversalCharacterController.CharacterState.Collaborating))
        {
            return;
        }

        List<UniversalCharacterController> eligibleCollaborators = CollabManager.Instance.GetEligibleCollaborators(characterController);
        if (eligibleCollaborators.Count > 0)
        {
            StartCoroutine(EvaluateCollaborationOpportunityCoroutine(eligibleCollaborators));
        }
        else
        {
            ConsiderMovingToNewLocation();
        }
    }

    private string EvaluateBestLocation()
    {
        Dictionary<string, float> locationScores = new Dictionary<string, float>();
        List<string> allLocations = LocationManagerMaster.Instance.GetAllLocations();

        foreach (string location in allLocations)
        {
            float score = 0f;

            // Check for relevant actions
            List<LocationManager.LocationAction> actions = LocationManagerMaster.Instance.GetLocationActions(location, characterController.aiSettings.characterRole);
            foreach (var action in actions)
            {
                if (action.actionName.ToLower().Contains(characterController.currentObjective.ToLower()))
                {
                    score += 2f;
                }
                if (npcData.GetMentalModel().GoalImportance.TryGetValue(action.actionName, out float importance))
                {
                    score += importance;
                }
            }

            // Check for potential collaborators
            List<UniversalCharacterController> charactersAtLocation = GameManager.Instance.GetAllCharacters().Where(c => c.currentLocation.locationName == location).ToList();
            foreach (var character in charactersAtLocation)
            {
                if (npcData.GetMentalModel().Relationships.TryGetValue(character.characterName, out float relationship))
                {
                    score += relationship * 0.5f;
                }
            }

            // Add a small random factor
            score += Random.Range(0f, 0.5f);

            locationScores[location] = score;
        }

        return locationScores.OrderByDescending(kvp => kvp.Value).First().Key;
    }

private IEnumerator EvaluateCollaborationOpportunityCoroutine(List<UniversalCharacterController> eligibleCollaborators)
{
    Task task = EvaluateCollaborationOpportunity(eligibleCollaborators);
    yield return new WaitUntil(() => task.IsCompleted);
}

private async Task EvaluateCollaborationOpportunity(List<UniversalCharacterController> eligibleCollaborators)
{
    List<string> options = new List<string> { "Initiate Collaboration", "Continue Current Activity" };
    GameState currentState = GameManager.Instance.GetCurrentGameState();

    string decision = await MakeDecision(options, currentState);

    if (decision == "Initiate Collaboration")
    {
        UniversalCharacterController bestCollaborator = ChooseBestCollaborator(eligibleCollaborators);
        if (bestCollaborator != null)
        {
            string actionName = ChooseCollaborationAction(bestCollaborator);
            if (!string.IsNullOrEmpty(actionName))
            {
                characterController.InitiateCollab(actionName, bestCollaborator);
            }
        }
    }
}

    private UniversalCharacterController ChooseBestCollaborator(List<UniversalCharacterController> eligibleCollaborators)
    {
        return eligibleCollaborators.OrderByDescending(EvaluateCollaborator).FirstOrDefault();
    }

    private float EvaluateCollaborator(UniversalCharacterController collaborator)
    {
        float score = 0f;
        score += npcData.GetRelationship(collaborator.characterName) * 2f;
        score += CountComplementarySkills(collaborator) * 1.5f;
        score += (collaborator.currentLocation == characterController.currentLocation) ? 1f : 0f;
        score += Random.Range(0f, 0.5f);
        return score;
    }

    private int CountComplementarySkills(UniversalCharacterController collaborator)
    {
        var myTags = new HashSet<string>(characterController.aiSettings.personalGoalTags);
        var theirTags = new HashSet<string>(collaborator.aiSettings.personalGoalTags);
        return myTags.Union(theirTags).Count() - myTags.Count;
    }

    private string ChooseCollaborationAction(UniversalCharacterController collaborator)
    {
        if (characterController.currentLocation == null) return null;

        List<LocationManager.LocationAction> availableActions = characterController.currentLocation.GetAvailableActions(characterController.aiSettings.characterRole);
        List<LocationManager.LocationAction> collaboratorActions = characterController.currentLocation.GetAvailableActions(collaborator.aiSettings.characterRole);

        var commonActions = availableActions.Intersect(collaboratorActions, new LocationActionComparer());

        return commonActions.Any() ? EvaluateBestAction(commonActions.ToList()) : null;
    }

    private string EvaluateBestAction(List<LocationManager.LocationAction> actions)
    {
        return actions.OrderByDescending(EvaluateAction).FirstOrDefault()?.actionName;
    }

    private float EvaluateAction(LocationManager.LocationAction action)
    {
        float score = 0f;
        score += GameManager.Instance.GetCurrentChallenge().title.ToLower().Contains(action.actionName.ToLower()) ? 2f : 0f;
        score += characterController.aiSettings.personalGoalTags.Count(goalTag => action.actionName.ToLower().Contains(goalTag.ToLower())) * 1.5f;
        score += TagSystem.GetTagsForAction(action.actionName).Count(tag => tag.StartsWith("Challenge") || tag.StartsWith("PersonalGoal")) * 0.5f;
        score += Random.Range(0f, 0.5f);
        return score;
    }

    public async Task<bool> DecideOnCollaboration(string actionName)
    {
        if (characterController == null || 
            characterController.HasState(UniversalCharacterController.CharacterState.Acclimating) ||
            characterController.HasState(UniversalCharacterController.CharacterState.PerformingAction) ||
            characterController.HasState(UniversalCharacterController.CharacterState.Collaborating))
        {
            return false;
        }

        List<string> options = new List<string> { "Collaborate", "Work alone" };
        GameState currentState = GameManager.Instance != null ? GameManager.Instance.GetCurrentGameState() : default;
        
        if (currentState.Equals(default(GameState))) return false;

        string decision = await MakeDecision(options, currentState);
        return decision == "Collaborate";
    }
    #endregion

    #region Dialogue and Interaction
    public void InitiateDialogueWithPlayer(UniversalCharacterController player)
    {
        if (Time.time - lastDialogueInitiationTime < dialogueInitiationCooldown) return;

        lastDialogueInitiationTime = Time.time;
        photonView.RPC("RPC_RequestDialogueWithPlayer", RpcTarget.All, player.photonView.ViewID);
    }

    [PunRPC]
    private void RPC_RequestDialogueWithPlayer(int playerViewID)
    {
        PhotonView playerView = PhotonView.Find(playerViewID);
        if (playerView != null && playerView.IsMine)
        {
            GameManager.Instance.dialogueRequestUI.ShowRequest(characterController);
        }
    }
    #endregion

    #region Memory and Knowledge Management
    public void UpdateKnowledge(string key, string value)
    {
        npcData.UpdateKnowledge(key, value);
    }

    public void AddMemory(string memory)
    {
        if (npcData != null)
        {
            npcData.AddMemory(memory);
        }
        else
        {
            Debug.LogWarning("Attempted to add memory to a character without NPC_Data.");
        }
    }

    private IEnumerator PeriodicMemoryConsolidation()
    {
        while (true)
        {
            yield return new WaitForSeconds(memoryConsolidationInterval);
            if (npcData != null)
            {
                npcData.GetMentalModel().MemoryConsolidation();
            }
        }
    }

    private IEnumerator PeriodicReflection()
    {
        while (true)
        {
            yield return new WaitForSeconds(reflectionInterval);
            if (npcData != null)
            {
                string reflection = npcData.GetMentalModel().Reflect();
                Debug.Log($"{characterController.characterName} reflects: {reflection}");
            }
        }
    }

    public void RecordSignificantEvent(string eventDescription, float importance)
    {
        if (npcData != null)
        {
            npcData.GetMentalModel().AddMemory(eventDescription, importance);
        }
    }
    #endregion

    #region Decision Making and State Management
    public async Task<string> MakeDecision(List<string> options, GameState currentState)
    {
        string memoryContext = string.Join(", ", npcData.GetMentalModel().RetrieveRelevantMemories(string.Join(" ", options)).Select(m => m.Content));
        string reflection = npcData.GetMentalModel().Reflect();
        return await decisionMaker.MakeDecision(this, options, currentState, memoryContext, reflection);
    }

    public void UpdateEmotionalState(EmotionalState newState)
    {
        npcData.UpdateEmotionalState(newState);
    }

    public void UpdateRelationship(string characterName, float change)
    {
        npcData.UpdateRelationship(characterName, change);
    }
    #endregion

    #region Utility Methods
    public List<string> GetPersonalGoalTags()
    {
        return characterController.GetPersonalGoalTags();
    }

    public Dictionary<string, bool> GetPersonalGoalCompletion()
    {
        return characterController.GetPersonalGoalCompletion();
    }

    public UniversalCharacterController GetCharacterController()
    {
        return characterController;
    }

    public int DecideScenario(List<string> scenarios, GameState gameState)
    {
        Dictionary<int, float> scenarioScores = new Dictionary<int, float>();

        for (int i = 0; i < scenarios.Count; i++)
        {
            float score = npcData.GetMentalModel().EvaluateScenario(scenarios[i], gameState);
            scenarioScores[i] = score;
        }

        float maxScore = scenarioScores.Values.Max();
        var topScenarios = scenarioScores.Where(kvp => Mathf.Approximately(kvp.Value, maxScore)).ToList();
        
        return topScenarios[Random.Range(0, topScenarios.Count)].Key;
    }
    #endregion
}

public class LocationActionComparer : IEqualityComparer<LocationManager.LocationAction>
{
    public bool Equals(LocationManager.LocationAction x, LocationManager.LocationAction y)
    {
        return x.actionName == y.actionName;
    }

    public int GetHashCode(LocationManager.LocationAction obj)
    {
        return obj.actionName.GetHashCode();
    }
}