using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using System.Linq;
using System.Threading.Tasks;
using System;

public class CollabManager : MonoBehaviourPunCallbacks
{
    public static CollabManager Instance { get; private set; }

    [SerializeField] private float collabRadius = 5f;
    [SerializeField] private float collabCooldown = 45f;
    [SerializeField] private int maxCollaborators = 3;
    [SerializeField] private float collabBonusMultiplier = 0.5f;

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

        string initiatorGroupId = initiator.GetCurrentGroupId();

        foreach (var character in GameManager.Instance.GetAllCharacters())
        {
            if (character != initiator &&
                character.currentLocation == initiator.currentLocation &&
                Vector3.Distance(initiator.transform.position, character.transform.position) <= collabRadius &&
                !collabCooldowns.ContainsKey(character.characterName) &&
                !character.HasState(UniversalCharacterController.CharacterState.PerformingAction) &&
                (string.IsNullOrEmpty(initiatorGroupId) || character.GetCurrentGroupId() == initiatorGroupId))
            {
                eligibleCollaborators.Add(character);
            }
        }

        return eligibleCollaborators.Take(maxCollaborators - 1).ToList();
    }

    [PunRPC]
    public void RequestCollaboration(int initiatorViewID, int[] targetViewIDs, string actionName)
    {
        PhotonView initiatorView = PhotonView.Find(initiatorViewID);
        if (initiatorView == null) return;

        UniversalCharacterController initiator = initiatorView.GetComponent<UniversalCharacterController>();
        if (initiator == null) return;

        foreach (int targetViewID in targetViewIDs)
        {
            PhotonView targetView = PhotonView.Find(targetViewID);
            if (targetView == null) continue;

            UniversalCharacterController target = targetView.GetComponent<UniversalCharacterController>();
            if (target == null) continue;

            if (target.IsPlayerControlled && target.photonView.IsMine)
            {
                CollabPromptUI.Instance.ShowPrompt(initiator, target, actionName);
            }
            else if (!target.IsPlayerControlled)
            {
                AIManager aiManager = target.GetComponent<AIManager>();
                if (aiManager != null)
                {
                    StartCoroutine(DecideOnCollaborationCoroutine(aiManager, actionName, initiatorViewID, target.photonView.ViewID));
                }
            }
        }
    }

    private IEnumerator DecideOnCollaborationCoroutine(AIManager aiManager, string actionName, int initiatorViewID, int targetViewID)
    {
        Task<bool> decisionTask = aiManager.DecideOnCollaboration(actionName);
        yield return new WaitUntil(() => decisionTask.IsCompleted);

        if (decisionTask.Result)
        {
            string collabID = Guid.NewGuid().ToString();
            photonView.RPC("InitiateCollab", RpcTarget.All, actionName, initiatorViewID, new int[] { targetViewID }, collabID);
        }
    }

    [PunRPC]
    public void InitiateCollab(string actionName, int initiatorViewID, int[] collaboratorViewIDs, string collabID)
    {
        PhotonView initiatorView = PhotonView.Find(initiatorViewID);
        if (initiatorView == null) return;

        UniversalCharacterController initiator = initiatorView.GetComponent<UniversalCharacterController>();
        if (initiator == null) return;

        List<UniversalCharacterController> collaborators = new List<UniversalCharacterController> { initiator };

        foreach (int viewID in collaboratorViewIDs)
        {
            PhotonView collaboratorView = PhotonView.Find(viewID);
            if (collaboratorView != null)
            {
                UniversalCharacterController collaborator = collaboratorView.GetComponent<UniversalCharacterController>();
                if (collaborator != null)
                {
                    collaborators.Add(collaborator);
                }
            }
        }

        if (collaborators.Any(c => c.currentLocation != initiator.currentLocation))
        {
            Debug.LogWarning("Not all collaborators are in the same location");
            return;
        }

        LocationManager.LocationAction action = initiator.currentLocation.GetActionByName(actionName);
        if (action == null)
        {
            Debug.LogWarning($"Action '{actionName}' not found in location '{initiator.currentLocation.locationName}'");
            return;
        }

        activeCollabs[collabID] = collaborators;

        foreach (var collaborator in collaborators)
        {
            SetCollabCooldown(collaborator.characterName);
            collaborator.AddState(UniversalCharacterController.CharacterState.Collaborating);
            collaborator.currentCollabID = collabID;
            collaborator.StartAction(action);
        }

        if (collaborators.Count > 1 && !collaborators[0].IsInGroup())
        {
            if (GroupManager.Instance != null)
            {
                GroupManager.Instance.FormGroup(collaborators);
            }
            else
            {
                Debug.LogWarning("CollabManager: GroupManager instance is null. Cannot form group.");
            }
        }
    }

    public void FinalizeCollaboration(string collabID, int actionDuration)
    {
        if (activeCollabs.TryGetValue(collabID, out List<UniversalCharacterController> collaborators))
        {
            int basePoints = ScoreConstants.GetActionPoints(actionDuration);
            int collabBonus = Mathf.RoundToInt(basePoints * collabBonusMultiplier);

            foreach (var collaborator in collaborators)
            {
                GameManager.Instance.UpdatePlayerScore(collaborator.characterName, basePoints, $"Completed {collaborators.Count}-person collaboration", new List<string> { "Collaboration" });
                GameManager.Instance.UpdatePlayerScore(collaborator.characterName, collabBonus, $"Collaboration bonus", new List<string> { "CollaborationBonus" });

                collaborator.RemoveState(UniversalCharacterController.CharacterState.Collaborating);
                collaborator.AddState(UniversalCharacterController.CharacterState.Cooldown);
                collaborator.currentCollabID = null;
            }

            EurekaManager.Instance.CheckForEureka(collaborators, collabID);
            activeCollabs.Remove(collabID);

            if (collaborators.Count > 1 && collaborators[0].IsInGroup())
            {
                string groupId = collaborators[0].GetCurrentGroupId();
                if (!string.IsNullOrEmpty(groupId))
                {
                    GroupManager.Instance.DisbandGroup(groupId);
                }
            }
        }
    }

    public float GetCollabSuccessBonus(string collabID)
    {
        return activeCollabs.ContainsKey(collabID) ? collabBonusMultiplier : 0f;
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