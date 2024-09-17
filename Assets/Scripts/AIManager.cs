using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using ProjectGrow.AI;

public class AIManager : MonoBehaviourPunCallbacks
{
    private UniversalCharacterController characterController;
    private NPC_Behavior npcBehavior;
    private NPC_Data npcData;
    private AIDecisionMaker decisionMaker;

    [SerializeField] private float memoryConsolidationInterval = 60f;
    [SerializeField] private float reflectionInterval = 120f;

    private bool isInitialized = false;

    [SerializeField] private float dialogueInitiationCooldown = 300f; // 5 minutes
    private float lastDialogueInitiationTime = 0f;

    private void Start()
    {
        StartCoroutine(PeriodicMemoryConsolidation());
        StartCoroutine(PeriodicReflection());
    }

    private void Awake()
    {
        decisionMaker = gameObject.AddComponent<AIDecisionMaker>();
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
        if (!photonView.IsMine || !isInitialized) return;

        if (!characterController.HasState(UniversalCharacterController.CharacterState.Chatting))
        {
            npcBehavior.UpdateBehavior();
        }
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
                // Use this reflection (e.g., log it, update behavior, etc.)
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

    public void UpdateRelationship(string characterName, float change)
    {
        npcData.UpdateRelationship(characterName, change);
    }

    // Update the MakeDecision method call:
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

    public List<string> GetPersonalGoalTags()
    {
        return characterController.GetPersonalGoalTags();
    }

    public Dictionary<string, bool> GetPersonalGoalCompletion()
    {
        return characterController.GetPersonalGoalCompletion();
    }

    public bool ConsiderCollaboration(LocationManager.LocationAction action)
    {
        if (action == null || characterController == null || characterController.currentLocation == null)
        {
            return false;
        }

        if (characterController.HasState(UniversalCharacterController.CharacterState.Acclimating) ||
            characterController.HasState(UniversalCharacterController.CharacterState.PerformingAction) ||
            characterController.HasState(UniversalCharacterController.CharacterState.Collaborating))
        {
            return false;
        }

        if (!characterController.IsActionAvailable(action.actionName))
        {
            return false;
        }

        if (CollabManager.Instance.CanInitiateCollab(characterController))
        {
            float collaborationChance = CalculateCollaborationChance(action);
            if (Random.value < collaborationChance)
            {
                List<UniversalCharacterController> eligibleCollaborators = CollabManager.Instance.GetEligibleCollaborators(characterController);
                if (eligibleCollaborators.Count > 0)
                {
                    UniversalCharacterController collaborator = eligibleCollaborators[Random.Range(0, eligibleCollaborators.Count)];
                    characterController.InitiateCollab(action.actionName, collaborator);
                    return true;
                }
            }
        }
        return false;
    }

    private float CalculateCollaborationChance(LocationManager.LocationAction action)
    {
        float baseChance = 0.5f;
        if (action.actionName.ToLower().Contains(characterController.aiSettings.characterRole.ToLower()))
        {
            baseChance += 0.2f;
        }
        if (GameManager.Instance.GetCurrentChallenge().title.ToLower().Contains(action.actionName.ToLower()))
        {
            baseChance += 0.2f;
        }
        return Mathf.Clamp01(baseChance);
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

    public async Task<bool> DecideOnCollaboration(string actionName)
    {
        if (characterController.HasState(UniversalCharacterController.CharacterState.Acclimating) ||
            characterController.HasState(UniversalCharacterController.CharacterState.PerformingAction) ||
            characterController.HasState(UniversalCharacterController.CharacterState.Collaborating))
        {
            return false;
        }

        List<string> options = new List<string> { "Collaborate", "Work alone" };
        GameState currentState = GameManager.Instance.GetCurrentGameState();
        string decision = await MakeDecision(options, currentState);

        return decision == "Collaborate";
    }

    public UniversalCharacterController GetCharacterController()
    {
        return characterController;
    }
}