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

        activeCollabs[actionName] = new List<UniversalCharacterController> { initiator };
        SetCollabCooldown(initiator.characterName);
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
        }
    }

    public void EndCollab(string actionName)
    {
        if (activeCollabs.ContainsKey(actionName))
        {
            activeCollabs.Remove(actionName);
        }
    }

    public float GetCollabSuccessBonus(string actionName)
    {
        if (activeCollabs.TryGetValue(actionName, out List<UniversalCharacterController> collaborators))
        {
            return (collaborators.Count - 1) * 0.15f;
        }
        return 0f;
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