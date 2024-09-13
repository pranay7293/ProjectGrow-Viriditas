using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using System.Linq;
using System.Threading.Tasks;

public class CollabManager : MonoBehaviourPunCallbacks
{
    public static CollabManager Instance { get; private set; }

    [SerializeField] private float collabRadius = 5f;
    [SerializeField] private float collabCooldown = 45f;
    [SerializeField] private int maxCollaborators = 3;
    [SerializeField] private float collabBonusMultiplier = 0.5f; // 50% bonus for collaborations

    private Dictionary<string, List<UniversalCharacterController>> activeCollabs = new Dictionary<string, List<UniversalCharacterController>>();
    private Dictionary<string, float> collabCooldowns = new Dictionary<string, float>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        UpdateCooldowns();
    }

    public bool CanInitiateCollab(UniversalCharacterController initiator)
    {
        if (initiator == null || string.IsNullOrEmpty(initiator.characterName))
        {
            Debug.LogWarning("Invalid initiator in CanInitiateCollab");
            return false;
        }
        return !collabCooldowns.ContainsKey(initiator.characterName) && !initiator.HasState(UniversalCharacterController.CharacterState.PerformingAction);
    }

    public List<UniversalCharacterController> GetEligibleCollaborators(UniversalCharacterController initiator)
    {
        List<UniversalCharacterController> eligibleCollaborators = new List<UniversalCharacterController>();
        if (initiator == null || initiator.currentLocation == null) return eligibleCollaborators;

        foreach (var character in GameManager.Instance.GetAllCharacters())
        {
            if (character != initiator && 
                character.currentLocation == initiator.currentLocation && 
                Vector3.Distance(initiator.transform.position, character.transform.position) <= collabRadius &&
                !collabCooldowns.ContainsKey(character.characterName) &&
                !character.HasState(UniversalCharacterController.CharacterState.PerformingAction))
            {
                eligibleCollaborators.Add(character);
            }
        }

        return eligibleCollaborators.Take(maxCollaborators).ToList();
    }

        [PunRPC]
    public void RequestCollaboration(int initiatorViewID, int targetViewID, string actionName)
    {
        PhotonView initiatorView = PhotonView.Find(initiatorViewID);
        PhotonView targetView = PhotonView.Find(targetViewID);
        if (initiatorView == null || targetView == null) return;

        UniversalCharacterController initiator = initiatorView.GetComponent<UniversalCharacterController>();
        UniversalCharacterController target = targetView.GetComponent<UniversalCharacterController>();
        if (initiator == null || target == null) return;

        if (target.IsPlayerControlled && target.photonView.IsMine)
        {
            CollabPromptUI.Instance.ShowPrompt(initiator, target, actionName);
        }
        else if (!target.IsPlayerControlled)
        {
            AIManager aiManager = target.GetComponent<AIManager>();
            if (aiManager != null)
            {
                StartCoroutine(DecideOnCollaborationCoroutine(aiManager, actionName, initiatorViewID, targetViewID));
            }
        }
    }

    private IEnumerator DecideOnCollaborationCoroutine(AIManager aiManager, string actionName, int initiatorViewID, int targetViewID)
    {
    Task<bool> decisionTask = aiManager.DecideOnCollaboration(actionName);
    yield return new WaitUntil(() => decisionTask.IsCompleted);

    if (decisionTask.Result)
    {
        InitiateCollab(actionName, initiatorViewID, targetViewID);
    }
    }

    [PunRPC]
    public void InitiateCollab(string actionName, int initiatorViewID, int collaboratorViewID)
{
    PhotonView initiatorView = PhotonView.Find(initiatorViewID);
    PhotonView collaboratorView = PhotonView.Find(collaboratorViewID);
    if (initiatorView == null || collaboratorView == null) return;

    UniversalCharacterController initiator = initiatorView.GetComponent<UniversalCharacterController>();
    UniversalCharacterController collaborator = collaboratorView.GetComponent<UniversalCharacterController>();
    if (initiator == null || collaborator == null) return;

    if (initiator.currentLocation == null || collaborator.currentLocation == null || initiator.currentLocation != collaborator.currentLocation)
    {
        Debug.LogWarning($"Cannot initiate collaboration: characters are not in the same location");
        return;
    }

    LocationManager.LocationAction action = initiator.currentLocation.GetActionByName(actionName);
    if (action == null)
    {
        Debug.LogWarning($"Action '{actionName}' not found for {initiator.characterName} in {initiator.currentLocation.locationName}");
        return;
    }

        if (!activeCollabs.ContainsKey(actionName))
        {
            activeCollabs[actionName] = new List<UniversalCharacterController>();
        }
        activeCollabs[actionName].Add(initiator);
        activeCollabs[actionName].Add(collaborator);
        SetCollabCooldown(initiator.characterName);
        SetCollabCooldown(collaborator.characterName);
        
        initiator.AddState(UniversalCharacterController.CharacterState.Collaborating);
        collaborator.AddState(UniversalCharacterController.CharacterState.Collaborating);

        // Start the collaborative action
        initiator.StartAction(action);
        collaborator.StartAction(action);
    }

    public void FinalizeCollaboration(string actionName, int actionDuration)
    {
        if (activeCollabs.TryGetValue(actionName, out List<UniversalCharacterController> collaborators))
        {
            int basePoints = ScoreConstants.GetActionPoints(actionDuration);
            int collabBonus = Mathf.RoundToInt(basePoints * collabBonusMultiplier);

            foreach (var collaborator in collaborators)
            {
                // Award full points for the action
                GameManager.Instance.UpdatePlayerScore(collaborator.characterName, basePoints, $"Completed {actionName}", new List<string> { actionName, "Collaboration" });
                
                // Award collaboration bonus
                GameManager.Instance.UpdatePlayerScore(collaborator.characterName, collabBonus, $"Collaboration bonus for {actionName}", new List<string> { "CollaborationBonus" });

                collaborator.RemoveState(UniversalCharacterController.CharacterState.Collaborating);
                collaborator.AddState(UniversalCharacterController.CharacterState.Cooldown);
            }

            EurekaManager.Instance.CheckForEureka(collaborators, actionName);
            activeCollabs.Remove(actionName);
        }
    }

    public float GetCollabSuccessBonus(string actionName)
    {
        if (activeCollabs.TryGetValue(actionName, out List<UniversalCharacterController> collaborators))
        {
            return collabBonusMultiplier;
        }
        return 0f;
    }

    public float GetCollabCooldown()
    {
        return collabCooldown;
    }

    private void SetCollabCooldown(string characterName)
    {
        if (!string.IsNullOrEmpty(characterName))
        {
            collabCooldowns[characterName] = collabCooldown;
            UniversalCharacterController character = GameManager.Instance.GetCharacterByName(characterName);
            if (character != null)
            {
                CharacterProgressBar progressBar = character.GetComponentInChildren<CharacterProgressBar>();
                if (progressBar != null)
                {
                    progressBar.UpdateKeyState(UniversalCharacterController.CharacterState.Cooldown);
                    progressBar.SetCooldown(collabCooldown);
                }
            }
        }
    }

    private void UpdateCooldowns()
    {
        var cooldownsCopy = new Dictionary<string, float>(collabCooldowns);

        foreach (var kvp in cooldownsCopy)
        {
            string characterName = kvp.Key;
            float remainingCooldown = kvp.Value - Time.deltaTime;

            if (remainingCooldown <= 0)
            {
                collabCooldowns.Remove(characterName);
                UniversalCharacterController character = GameManager.Instance.GetCharacterByName(characterName);
                if (character != null)
                {
                    character.RemoveState(UniversalCharacterController.CharacterState.Cooldown);
                }
            }
            else
            {
                collabCooldowns[characterName] = remainingCooldown;
            }
        }
    }
}