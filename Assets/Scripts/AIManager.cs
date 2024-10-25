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
    private float lastDecisionTime = 0f;

    public float interactionRadius = 10f;

    private float lastCollabConsiderationTime = 0f;
    private float lastActionTime = 0f;
    private float lastDialogueInitiationTime = 0f;

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

    private IEnumerator DecisionMakingCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(decisionMakingInterval);
            if (!isExecutingAction && Time.time - lastActionTime >= minActionDuration)
            {
                MakeDecision();
            }
        }
    }

    private void Update()
{
    if (!photonView.IsMine || !isInitialized || characterController == null) return;

    if (Time.time - lastDecisionTime >= decisionMakingInterval && !isExecutingAction && Time.time - lastActionTime >= minActionDuration)
    {
        MakeDecision();
        lastDecisionTime = Time.time;
    }
}


    private void MakeDecision()
    {
        if (!photonView.IsMine || !isInitialized || characterController == null) return;

        string chosenAction = ChooseAction();
        // Debug.Log($"{characterController.characterName}: Chose action: {chosenAction}");
        ExecuteAction(chosenAction);
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
        // Debug.Log($"{characterController.characterName}: Action weights: {string.Join(", ", actionWeights.Select(kv => $"{kv.Key}:{kv.Value}"))}");
        return chosenAction;
    }

    private float EvaluateActionWeight(string action)
    {
        float weight = 1f; // Base weight

        switch (action)
        {
            case "MoveToNewLocation":
                if (characterController.HasState(CharacterState.Idle))
                    weight *= 2f;
                break;
            case "PerformLocationAction":
                if (characterController.currentLocation != null)
                    weight *= 1.5f;
                break;
            case "InteractWithNearbyCharacter":
                Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, interactionRadius);
                int nearbyCharacters = nearbyColliders.Count(c => c.GetComponent<UniversalCharacterController>() != null);
                weight *= (1f + (0.1f * nearbyCharacters));
                break;
            case "Idle":
                if (Time.time - lastActionTime < minActionDuration * 2)
                    weight *= 0.5f;
                break;
        }

        return weight;
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
        // Debug.Log($"{characterController.characterName}: Starting MoveToNewLocation");
        string bestLocation = EvaluateBestLocation();
        if (characterController.currentLocation == null || bestLocation != characterController.currentLocation.locationName)
        {
            if (WaypointsManager.Instance != null)
            {
                Vector3 waypointNearLocation = WaypointsManager.Instance.GetWaypointNearLocation(bestLocation);
                npcBehavior.MoveToPosition(waypointNearLocation);
                yield return new WaitUntil(() => !characterController.HasState(CharacterState.Moving));
                // Debug.Log($"{characterController.characterName}: Finished moving to new location");
            }
        }
        isExecutingAction = false;
    }

    private IEnumerator PerformLocationActionCoroutine()
    {
        // Debug.Log($"{characterController.characterName}: Starting PerformLocationAction");
        if (characterController.currentLocation != null)
        {
            List<LocationManager.LocationAction> availableActions = characterController.currentLocation.GetAvailableActions(characterController.aiSettings.characterRole);
            if (availableActions.Count > 0)
            {
                LocationManager.LocationAction selectedAction = ChooseBestAction(availableActions);
                characterController.StartAction(selectedAction);
                yield return new WaitUntil(() => !characterController.HasState(CharacterState.PerformingAction));
                // Debug.Log($"{characterController.characterName}: Finished performing location action");
            }
        }
        isExecutingAction = false;
    }

    private IEnumerator InteractWithNearbyCharacterCoroutine()
    {
        // Debug.Log($"{characterController.characterName}: Starting InteractWithNearbyCharacter");
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, interactionRadius);
        List<UniversalCharacterController> nearbyCharacters = nearbyColliders
            .Select(c => c.GetComponent<UniversalCharacterController>())
            .Where(c => c != null && c != characterController)
            .ToList();

        if (nearbyCharacters.Count > 0)
        {
            UniversalCharacterController target = nearbyCharacters[Random.Range(0, nearbyCharacters.Count)];
            if (target.IsPlayerControlled)
            {
                InitiateDialogueWithPlayer(target);
            }
            else
            {
                // Implement NPC-NPC interaction logic here
                yield return new WaitForSeconds(3f); // Simulated interaction time
            }
            Debug.Log($"{characterController.characterName}: Finished interacting with nearby character");
        }
        isExecutingAction = false;
    }

    private IEnumerator IdleCoroutine()
    {
        // Debug.Log($"{characterController.characterName}: Starting Idle");
        characterController.AddState(CharacterState.Idle);
        yield return new WaitForSeconds(Random.Range(3f, 7f));
        characterController.RemoveState(CharacterState.Idle);
        // Debug.Log($"{characterController.characterName}: Finished Idle");
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
                if (!string.IsNullOrEmpty(characterController.currentObjective) && action.actionName.ToLower().Contains(characterController.currentObjective.ToLower()))
                {
                    score += 2f;
                }
                if (npcData.GetMentalModel().GoalImportance.TryGetValue(action.actionName, out float importance))
                {
                    score += importance;
                }
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
        return actions.OrderByDescending(EvaluateAction).FirstOrDefault();
    }

    private float EvaluateAction(LocationManager.LocationAction action)
    {
        float score = 0f;
        if (GameManager.Instance.GetCurrentChallenge().title.ToLower().Contains(action.actionName.ToLower()))
        {
            score += 2f;
        }
        score += characterController.aiSettings.personalGoalTags.Count(goalTag => action.actionName.ToLower().Contains(goalTag.ToLower())) * 1.5f;
        score += TagSystem.GetTagsForAction(action.actionName)
            .Count(t => t.tag.StartsWith("Challenge") || t.tag.StartsWith("PersonalGoal")) * 0.5f;
        score += Random.Range(0f, 0.5f);
        return score;
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

    return commonActions.Any() ? commonActions.OrderByDescending(EvaluateAction).First().actionName : null;
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