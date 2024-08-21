using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;
using System.Linq;

public class CollabManager : MonoBehaviourPunCallbacks
{
    public static CollabManager Instance { get; private set; }

    [SerializeField] private float collabRadius = 5f;
    [SerializeField] private float collabCooldown = 45f;
    [SerializeField] private int maxCollaborators = 3;

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
        return !collabCooldowns.ContainsKey(initiator.characterName);
    }

    public List<UniversalCharacterController> GetEligibleCollaborators(UniversalCharacterController initiator)
    {
        List<UniversalCharacterController> eligibleCollaborators = new List<UniversalCharacterController>();
        if (initiator == null) return eligibleCollaborators;

        Collider[] colliders = Physics.OverlapSphere(initiator.transform.position, collabRadius);

        foreach (Collider collider in colliders)
        {
            UniversalCharacterController character = collider.GetComponent<UniversalCharacterController>();
            if (character != null && character != initiator && !collabCooldowns.ContainsKey(character.characterName))
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
            // Show prompt for local player
            CollabPromptUI.Instance.ShowPrompt(initiator, target, actionName);
        }
        else if (!target.IsPlayerControlled)
        {
            // Automatic decision for AI
            AIManager aiManager = target.GetComponent<AIManager>();
            if (aiManager != null && aiManager.DecideOnCollaboration(actionName))
            {
                InitiateCollab(actionName, initiatorViewID, targetViewID);
            }
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

        if (!activeCollabs.ContainsKey(actionName))
        {
            activeCollabs[actionName] = new List<UniversalCharacterController>();
        }
        activeCollabs[actionName].Add(initiator);
        activeCollabs[actionName].Add(collaborator);
        SetCollabCooldown(initiator.characterName);
        SetCollabCooldown(collaborator.characterName);
        
        GameManager.Instance.UpdatePlayerScore(initiator.characterName, ScoreConstants.COLLABORATION_INITIATION_BONUS);
        GameManager.Instance.UpdatePlayerScore(collaborator.characterName, ScoreConstants.COLLABORATION_JOIN_BONUS);
    }
    

    public void FinalizeCollaboration(string actionName)
{
    if (activeCollabs.TryGetValue(actionName, out List<UniversalCharacterController> collaborators))
    {
        EurekaManager.Instance.CheckForEureka(collaborators, actionName);
        GameManager.Instance.HandleCollabCompletion(actionName, collaborators);
        activeCollabs.Remove(actionName);
    }
}

    private void TriggerEureka(List<UniversalCharacterController> collaborators)
    {
        foreach (var collaborator in collaborators)
        {
            collaborator.IncrementEurekaCount();
            GameManager.Instance.UpdatePlayerScore(collaborator.characterName, ScoreConstants.EUREKA_BONUS);
        }
        
        EurekaManager.Instance.InitiateEureka(collaborators);
    }

    public float GetCollabSuccessBonus(string actionName)
    {
        if (activeCollabs.TryGetValue(actionName, out List<UniversalCharacterController> collaborators))
        {
            return (collaborators.Count - 1) * ScoreConstants.COLLAB_SUCCESS_BONUS_MULTIPLIER;
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
                    progressBar.SetKeyState(KeyState.Cooldown);
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
            }
            else
            {
                collabCooldowns[characterName] = remainingCooldown;
            }
        }
    }
}