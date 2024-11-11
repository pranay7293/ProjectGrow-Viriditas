using UnityEngine;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ProjectGrow.AI;

public class AIManager : MonoBehaviourPunCallbacks
{
    private UniversalCharacterController characterController;
    private NPC_Behavior npcBehavior;
    public NPC_Data npcData;
    private AIDecisionMaker decisionMaker;

    [SerializeField] private float memoryConsolidationInterval = 30f;
    [SerializeField] private float reflectionInterval = 60f;
    [SerializeField] private float dialogueInitiationCooldown = 30f;
    [SerializeField] private float collabConsiderationInterval = 15f;
    [SerializeField] private float decisionMakingInterval = 5f;
    [SerializeField] private float explorationProbability = 0.25f;
    [SerializeField] private float minActionDuration = 5f;
    [SerializeField] private float maxIdleDuration = 10f; // New variable to force exit from Idle state

    public float interactionRadius = 10f;

    private float lastDecisionTime = 0f;
    private float lastCollabConsiderationTime = 0f;
    private float lastActionTime = 0f;
    private float lastDialogueInitiationTime = 0f;
    private float idleStartTime = 0f; // To track how long the agent has been idle

    private bool isInitialized = false;
    private bool isExecutingAction = false;

    private void Awake()
    {
        decisionMaker = gameObject.AddComponent<AIDecisionMaker>();
    }

    private void Start()
    {
        StartCoroutine(PeriodicMemoryConsolidation());
        StartCoroutine(PeriodicReflection());
    }

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

    private void Update()
    {
        if (!photonView.IsMine || !isInitialized || characterController == null) return;

        float currentDecisionInterval = decisionMakingInterval;
        if (characterController.HasState(CharacterState.Idle))
        {
            currentDecisionInterval = decisionMakingInterval / 2f; // Make decisions more frequently when idle
            if (Time.time - idleStartTime > maxIdleDuration)
            {
                // Force the agent to make a decision to exit Idle state
                isExecutingAction = false;
                idleStartTime = Time.time;
                Debug.Log($"{characterController.characterName}: Forced exit from Idle state.");
            }
        }

        // Add debug logs to monitor the agent's state
        // Debug.Log($"{characterController.characterName}: isExecutingAction={isExecutingAction}, Time since last decision={Time.time - lastDecisionTime}, Time since last action={Time.time - lastActionTime}");

        if (Time.time - lastDecisionTime >= currentDecisionInterval && !isExecutingAction && Time.time - lastActionTime >= minActionDuration)
        {
            MakeDecision();
            lastDecisionTime = Time.time;
        }
    }

    private void MakeDecision()
    {
        Debug.Log($"{characterController.characterName}: Making a decision.");
        if (!photonView.IsMine || !isInitialized || characterController == null) return;

        string chosenAction = ChooseAction();
        ExecuteAction(chosenAction);
    }

    public void ForceMoveToNewLocation()
    {
        if (!photonView.IsMine || !isInitialized || characterController == null) return;

        // Stop any current actions (except PerformingAction)
        if (characterController.HasState(CharacterState.Moving))
        {
            characterController.StopMoving();
        }

        // Get a new random destination
        Vector3 newDestination = WaypointsManager.Instance.GetRandomWaypoint();

        // Command the character to move to the new destination
        characterController.MoveTo(newDestination);

        // Update states
        isExecutingAction = false;
        lastActionTime = Time.time;

        Debug.Log($"{characterController.characterName}: Forced to move to a new location.");
    }

    private string ChooseAction()
    {
        List<string> possibleActions = new List<string>
        {
            "MoveToNewLocation",
            "PerformLocationAction",
            "InteractWithNearbyCharacter",
            "Idle"
        };

        Dictionary<string, float> actionWeights = new Dictionary<string, float>();
        foreach (string action in possibleActions)
        {
            actionWeights[action] = EvaluateActionWeight(action);
        }

        string chosenAction = WeightedRandomSelection(actionWeights);
        return chosenAction;
    }

    private float EvaluateActionWeight(string action)
    {
        float weight = 1f; // Base weight

        // Get personal goals and completion status
        var personalGoals = characterController.GetPersonalGoalTags();
        var personalGoalCompletion = characterController.GetPersonalGoalCompletion();

        // Get game state and current challenge
        var gameState = GameManager.Instance.GetCurrentGameState();
        var currentChallenge = gameState.CurrentChallenge;

        // Determine goal completion ratio
        int goalsAchieved = personalGoalCompletion.Count(kv => kv.Value);
        int totalGoals = personalGoalCompletion.Count;
        float goalCompletionRatio = totalGoals > 0 ? (float)goalsAchieved / totalGoals : 0f;

        switch (action)
        {
            case "MoveToNewLocation":
                // Encourage moving to new locations
                weight *= 3f; // Increase base weight
                if (characterController.HasState(CharacterState.Idle))
                    weight *= 2f;
                if (characterController.currentLocation == null)
                    weight *= 2f;
                break;
            case "PerformLocationAction":
                if (characterController.currentLocation != null)
                {
                    weight *= 5f; // Increase weight significantly
                    // Adjust weight based on actions available at the location
                    var availableActions = characterController.currentLocation.GetAvailableActions(characterController.aiSettings.characterRole);
                    foreach (var actionOption in availableActions)
                    {
                        float actionScore = EvaluateLocationAction(actionOption);
                        weight += actionScore;
                    }
                }
                else
                {
                    weight *= 2f;
                }
                break;
            case "InteractWithNearbyCharacter":
                Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, interactionRadius);
                int nearbyCharacters = nearbyColliders.Count(c => c.GetComponent<UniversalCharacterController>() != null);
                weight *= (1f + (0.2f * nearbyCharacters)); // Increase the weight
                break;
            case "Idle":
                // Penalize idle action if agent hasn't achieved goals
                if (goalCompletionRatio < 1f)
                {
                    // Reduce weight of Idle more aggressively
                    weight *= 0.1f;
                }
                else
                {
                    weight *= 0.5f;
                }

                if (Time.time - lastActionTime < minActionDuration * 2)
                    weight *= 0.5f;
                break;
        }

        return weight;
    }

    private float EvaluateLocationAction(LocationManager.LocationAction action)
    {
        float score = 0f;

        // Increase score if action contributes to current challenge
        if (GameManager.Instance.GetCurrentChallenge().title.ToLower().Contains(action.actionName.ToLower()))
        {
            score += 5f;
        }

        // Increase score if action helps achieve personal goals
        int personalGoalMatches = characterController.aiSettings.personalGoalTags.Count(goalTag => action.actionName.ToLower().Contains(goalTag.ToLower()));
        score += personalGoalMatches * 3f;

        // Increase score based on tags
        var tagsWithWeights = TagSystem.GetTagsForAction(action.actionName);
        foreach (var (tag, weight) in tagsWithWeights)
        {
            if (characterController.aiSettings.personalGoalTags.Contains(tag))
            {
                score += weight * 2f;
            }
            else if (tag.StartsWith("Challenge"))
            {
                score += weight * 1f;
            }
        }

        score += Random.Range(0f, 1f);

        return score;
    }

    private string WeightedRandomSelection(Dictionary<string, float> weights)
    {
        float totalWeight = weights.Values.Sum();
        float randomValue = Random.Range(0f, totalWeight);
        float cumulativeWeight = 0f;

        foreach (var kvp in weights)
        {
            cumulativeWeight += kvp.Value;
            if (randomValue <= cumulativeWeight)
            {
                return kvp.Key;
            }
        }

        return weights.Keys.Last(); // Fallback
    }

    private void ExecuteAction(string action)
    {
        if (isExecutingAction)
        {
            Debug.Log($"{characterController.characterName}: Attempted to execute {action} while already executing an action.");
            return;
        }

        isExecutingAction = true;
        lastActionTime = Time.time;

        switch (action)
        {
            case "MoveToNewLocation":
                StartCoroutine(MoveToNewLocationCoroutine());
                break;
            case "PerformLocationAction":
                StartCoroutine(PerformLocationActionCoroutine());
                break;
            case "InteractWithNearbyCharacter":
                StartCoroutine(InteractWithNearbyCharacterCoroutine());
                break;
            case "Idle":
                StartCoroutine(IdleCoroutine());
                break;
        }
    }

    private IEnumerator MoveToNewLocationCoroutine()
    {
        string bestLocation = EvaluateBestLocation();
        if (characterController.currentLocation == null || bestLocation != characterController.currentLocation.locationName)
        {
            if (WaypointsManager.Instance != null)
            {
                Vector3 waypointNearLocation = WaypointsManager.Instance.GetWaypointNearLocation(bestLocation);
                npcBehavior.MoveToPosition(waypointNearLocation);
                yield return new WaitUntil(() => !characterController.HasState(CharacterState.Moving));
            }
        }
        isExecutingAction = false;
    }

    private IEnumerator PerformLocationActionCoroutine()
    {
        if (characterController.currentLocation != null)
        {
            List<LocationManager.LocationAction> availableActions = characterController.currentLocation.GetAvailableActions(characterController.aiSettings.characterRole);
            if (availableActions.Count > 0)
            {
                LocationManager.LocationAction selectedAction = ChooseBestAction(availableActions);
                characterController.StartAction(selectedAction);
                yield return new WaitUntil(() => !characterController.HasState(CharacterState.PerformingAction));
            }
        }
        else
        {
            Debug.LogWarning($"{characterController.characterName}: No current location to perform action.");
        }
        isExecutingAction = false;
    }

    private IEnumerator InteractWithNearbyCharacterCoroutine()
    {
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, interactionRadius);
        List<UniversalCharacterController> nearbyCharacters = nearbyColliders
            .Select(c => c.GetComponent<UniversalCharacterController>())
            .Where(c => c != null && c != characterController)
            .ToList();

        if (nearbyCharacters.Count > 0)
        {
            UniversalCharacterController target = nearbyCharacters.OrderByDescending(EvaluateCollaborator).FirstOrDefault();
            if (target != null)
            {
                if (target.IsPlayerControlled)
                {
                    InitiateDialogueWithPlayer(target);
                    yield return new WaitForSeconds(3f); // Simulate dialogue duration
                }
                else
                {
                    // Implement NPC-NPC interaction logic here
                    yield return new WaitForSeconds(3f); // Simulate interaction duration
                }
            }
        }
        else
        {
            Debug.Log($"{characterController.characterName}: No nearby characters to interact with.");
        }

        isExecutingAction = false;
    }

    private IEnumerator IdleCoroutine()
    {
        characterController.AddState(CharacterState.Idle);
        idleStartTime = Time.time; // Start tracking idle time
        yield return new WaitForSeconds(Random.Range(3f, 7f));
        characterController.RemoveState(CharacterState.Idle);
        isExecutingAction = false;
    }

    private string EvaluateBestLocation()
    {
        Dictionary<string, float> locationScores = new Dictionary<string, float>();
        List<string> allLocations = LocationManagerMaster.Instance.GetAllLocations();

        foreach (string location in allLocations)
        {
            float score = 0f;

            List<LocationManager.LocationAction> actions = LocationManagerMaster.Instance.GetLocationActions(location, characterController.aiSettings.characterRole);
            foreach (var action in actions)
            {
                score += EvaluateLocationAction(action);
            }

            List<UniversalCharacterController> charactersAtLocation = GameManager.Instance.GetAllCharacters()
                .Where(c => c != null && c.currentLocation != null && c.currentLocation.locationName == location)
                .ToList();

            foreach (var character in charactersAtLocation)
            {
                if (npcData.GetMentalModel().Relationships.TryGetValue(character.characterName, out float relationship))
                {
                    score += relationship * 0.5f;
                }
            }

            score += Random.Range(0f, 0.5f);

            locationScores[location] = score;
        }

        return locationScores.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key;
    }

    private LocationManager.LocationAction ChooseBestAction(List<LocationManager.LocationAction> actions)
    {
        return actions.OrderByDescending(EvaluateLocationAction).FirstOrDefault();
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

    public void ConsiderCollaboration(UniversalCharacterController potentialCollaborator = null)
    {
        if (Time.time - lastCollabConsiderationTime < collabConsiderationInterval) return;

        lastCollabConsiderationTime = Time.time;

        if (characterController.HasState(CharacterState.Collaborating))
        {
            return;
        }

        List<UniversalCharacterController> eligibleCollaborators;
        if (potentialCollaborator != null)
        {
            eligibleCollaborators = new List<UniversalCharacterController> { potentialCollaborator };
        }
        else
        {
            eligibleCollaborators = CollabManager.Instance.GetEligibleCollaborators(characterController);
        }

        if (eligibleCollaborators.Count > 0)
        {
            EvaluateCollaborationOpportunity(eligibleCollaborators);
        }
    }

    private bool EvaluateCollaborationOpportunity(List<UniversalCharacterController> eligibleCollaborators)
    {
        UniversalCharacterController bestCollaborator = ChooseBestCollaborator(eligibleCollaborators);
        if (bestCollaborator != null)
        {
            string actionName = ChooseCollaborationAction(bestCollaborator);
            if (!string.IsNullOrEmpty(actionName))
            {
                bool decided = DecideOnCollaboration(actionName);
                if (decided)
                {
                    CollabManager.Instance.RequestCollaboration(characterController.photonView.ViewID, new int[] { bestCollaborator.photonView.ViewID }, actionName);
                    return true;
                }
            }
        }
        return false;
    }

    public bool DecideOnCollaboration(string actionName)
    {
        if (characterController == null)
        {
            Debug.LogError("AIManager.DecideOnCollaboration: characterController is null.");
            return false;
        }
        if (characterController.HasState(CharacterState.Acclimating) ||
            characterController.HasState(CharacterState.PerformingAction) ||
            characterController.HasState(CharacterState.Collaborating))
        {
            return false;
        }

        if (!characterController.IsPlayerControlled)
        {
            return Random.value < 0.9f;
        }

        return false; // Player-controlled characters don't auto-decide
    }

    private UniversalCharacterController ChooseBestCollaborator(List<UniversalCharacterController> eligibleCollaborators)
    {
        return eligibleCollaborators.OrderByDescending(EvaluateCollaborator).FirstOrDefault();
    }

    private string ChooseCollaborationAction(UniversalCharacterController collaborator)
    {
        if (characterController.currentLocation == null) return null;

        List<LocationManager.LocationAction> availableActions = characterController.currentLocation.GetAvailableActions(characterController.aiSettings.characterRole);
        List<LocationManager.LocationAction> collaboratorActions = characterController.currentLocation.GetAvailableActions(collaborator.aiSettings.characterRole);

        var commonActions = availableActions.Intersect(collaboratorActions, new LocationActionComparer());

        return commonActions.Any() ? commonActions.OrderByDescending(EvaluateLocationAction).First().actionName : null;
    }

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
                // Debug.Log($"{characterController.characterName} reflects: {reflection}");
            }
        }
    }

    public void OnInteractionWithCharacter(string otherCharacterName, string interactionSummary)
    {
        // Record the interaction
        npcData.AddMemory($"Interacted with {otherCharacterName}: {interactionSummary}", importance: 0.8f);

        // Update relationship
        npcData.UpdateRelationship(otherCharacterName, 0.1f); // Adjust change as needed
    }

    public void RecordSignificantEvent(string eventDescription, float importance)
    {
        if (npcData != null)
        {
            npcData.GetMentalModel().AddMemory(eventDescription, importance);
        }
    }

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

    public void OnCollaborationStart(string actionName, List<UniversalCharacterController> collaborators)
    {
        characterController.AddState(CharacterState.Collaborating);
        string collaboratorNames = string.Join(", ", collaborators.Select(c => c.characterName));
        AddMemory($"Started collaboration on {actionName} with {collaboratorNames}");
    }

    public void OnCollaborationEnd(string actionName, bool success)
    {
        characterController.RemoveState(CharacterState.Collaborating);
        string outcome = success ? "successfully" : "unsuccessfully";
        AddMemory($"Ended collaboration on {actionName} {outcome}");

        if (success)
        {
            List<UniversalCharacterController> collaborators = CollabManager.Instance.GetCollaborators(characterController.currentCollabID);
            foreach (var collaborator in collaborators)
            {
                if (collaborator != characterController)
                {
                    UpdateRelationship(collaborator.characterName, 0.1f);
                }
            }
        }
    }
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