using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;

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
        return !collabCooldowns.ContainsKey(initiator.characterName);
    }

    public List<UniversalCharacterController> GetEligibleCollaborators(UniversalCharacterController initiator)
    {
        List<UniversalCharacterController> eligibleCollaborators = new List<UniversalCharacterController>();
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
        
        // Award points for initiating a collaboration
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
            
            // Award points for joining a collaboration
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
        
        // Trigger Eureka event in EurekaManager
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
        collabCooldowns[characterName] = collabCooldown;
    }

    private void UpdateCooldowns()
    {
        List<string> expiredCooldowns = new List<string>();

        foreach (var cooldown in collabCooldowns)
        {
            collabCooldowns[cooldown.Key] -= Time.deltaTime;
            if (collabCooldowns[cooldown.Key] <= 0)
            {
                expiredCooldowns.Add(cooldown.Key);
            }
        }

        foreach (string character in expiredCooldowns)
        {
            collabCooldowns.Remove(character);
        }
    }
}