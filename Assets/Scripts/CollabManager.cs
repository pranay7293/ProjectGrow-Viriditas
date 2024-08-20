using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;
using System.Linq;

public class CollabManager : MonoBehaviourPunCallbacks
{
    public static CollabManager Instance { get; private set; }

    [SerializeField] private float collabRadius = 5f;
    [SerializeField] private float collabCooldown = 45f;
    [SerializeField] private int maxCollaborators = 2;

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

        return eligibleCollaborators;
    }

    [PunRPC]
    public void InitiateCollab(string actionName, int initiatorViewID)
    {
        PhotonView initiatorView = PhotonView.Find(initiatorViewID);
        if (initiatorView == null) return;

        UniversalCharacterController initiator = initiatorView.GetComponent<UniversalCharacterController>();
        if (initiator == null) return;

        if (!activeCollabs.ContainsKey(actionName))
        {
            activeCollabs[actionName] = new List<UniversalCharacterController>();
        }
        activeCollabs[actionName].Add(initiator);
        SetCollabCooldown(initiator.characterName);
        
        GameManager.Instance.UpdatePlayerScore(initiator.characterName, ScoreConstants.COLLABORATION_INITIATION_BONUS);
    }

    [PunRPC]
    public void JoinCollab(string actionName, int joinerViewID)
    {
        if (!activeCollabs.ContainsKey(actionName)) return;

        PhotonView joinerView = PhotonView.Find(joinerViewID);
        if (joinerView == null) return;

        UniversalCharacterController joiner = joinerView.GetComponent<UniversalCharacterController>();
        if (joiner == null) return;

        if (activeCollabs[actionName].Count < maxCollaborators)
        {
            activeCollabs[actionName].Add(joiner);
            SetCollabCooldown(joiner.characterName);
            
            GameManager.Instance.UpdatePlayerScore(joiner.characterName, ScoreConstants.COLLABORATION_JOIN_BONUS);
        }
    }

    public void FinalizeCollaboration(string actionName)
    {
        if (activeCollabs.TryGetValue(actionName, out List<UniversalCharacterController> collaborators))
        {
            if (Random.value < ScoreConstants.EUREKA_CHANCE)
            {
                TriggerEureka(collaborators);
            }
            else
            {
                GameManager.Instance.HandleCollabCompletion(actionName, collaborators);
            }
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