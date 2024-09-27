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
    [SerializeField] private float collabCooldown = 10f;
    [SerializeField] private int maxCollaborators = 3;
    [SerializeField] private float collabBonusMultiplier = 0.5f;

    private Dictionary<string, List<UniversalCharacterController>> activeCollabs = new Dictionary<string, List<UniversalCharacterController>>();
    private Dictionary<string, float> collabCooldowns = new Dictionary<string, float>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Ensure this object persists across scenes if necessary
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
                (character.HasState(UniversalCharacterController.CharacterState.Idle) ||
                 character.HasState(UniversalCharacterController.CharacterState.Moving) ||
                 character.HasState(UniversalCharacterController.CharacterState.Chatting)) &&
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
    if (aiManager == null) yield break;

    Task<bool> decisionTask = null;
    try
    {
        decisionTask = aiManager.DecideOnCollaboration(actionName);
    }
    catch (System.Exception)
    {
        yield break;
    }

    if (decisionTask == null) yield break;

    yield return new WaitUntil(() => decisionTask.IsCompleted);

    if (decisionTask.Result)
    {
        string collabID = System.Guid.NewGuid().ToString();
        if (photonView != null)
        {
            photonView.RPC("InitiateCollab", RpcTarget.All, actionName, initiatorViewID, new int[] { targetViewID }, collabID);
        }
    }
}

    [PunRPC]
    public void InitiateCollab(string actionName, int initiatorViewID, int[] collaboratorViewIDs, string collabID)
    {
        if (string.IsNullOrEmpty(actionName) || string.IsNullOrEmpty(collabID)) return;

        List<UniversalCharacterController> collaborators = new List<UniversalCharacterController>();

        PhotonView initiatorView = PhotonView.Find(initiatorViewID);
        if (initiatorView != null)
        {
            UniversalCharacterController initiator = initiatorView.GetComponent<UniversalCharacterController>();
            if (initiator != null)
            {
                collaborators.Add(initiator);
            }
        }

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

        if (collaborators.Count == 0) return;

        UniversalCharacterController initiatorCharacter = collaborators[0];
        if (initiatorCharacter == null || initiatorCharacter.currentLocation == null) return;

        LocationManager location = initiatorCharacter.currentLocation;
        if (location == null) return;

        if (collaborators.Any(c => c.currentLocation != location)) return;

        LocationManager.LocationAction action = location.GetActionByName(actionName);
        if (action == null) return;

        activeCollabs[collabID] = collaborators;

        foreach (var collaborator in collaborators)
        {
            if (collaborator != null)
            {
                SetCollabCooldown(collaborator.characterName);
                collaborator.AddState(UniversalCharacterController.CharacterState.Collaborating);
                collaborator.currentCollabID = collabID;
                collaborator.StartAction(action);

                // Log the collaboration initiation
            ActionLogManager.Instance?.LogAction(collaborator.characterName, $"Started collaboration on {actionName} with {string.Join(", ", collaborators.Where(c => c != collaborator).Select(c => c.characterName))}");
            }
        }

        if (collaborators.Count > 1 && !collaborators[0].IsInGroup())
        {
            if (GroupManager.Instance != null)
            {
                GroupManager.Instance.FormGroup(collaborators);
            }
        }
    }

   public void FinalizeCollaboration(string collabID, float actionDuration)
    {
        if (activeCollabs.TryGetValue(collabID, out List<UniversalCharacterController> collaborators))
        {
            int basePoints = ScoreConstants.GetActionPoints((int)actionDuration);
            int collabBonus = Mathf.RoundToInt(basePoints * collabBonusMultiplier);

            foreach (var collaborator in collaborators)
            {
                GameManager.Instance.UpdatePlayerScore(collaborator.characterName, basePoints, $"Completed {collaborators.Count}-person collaboration", new List<string> { "Collaboration" });
                GameManager.Instance.UpdatePlayerScore(collaborator.characterName, collabBonus, $"Collaboration bonus", new List<string> { "CollaborationBonus" });

                collaborator.RemoveState(UniversalCharacterController.CharacterState.Collaborating);
                collaborator.AddState(UniversalCharacterController.CharacterState.Cooldown);
                collaborator.currentCollabID = null;
            }

            // Trigger Eureka moment
            if (EurekaManager.Instance != null)
            {
                string actionName = collaborators[0].CurrentActionName; // Get the action name from the first collaborator
                EurekaManager.Instance.TriggerEureka(collaborators, actionName);
            }
            else
            {
                Debug.LogWarning("FinalizeCollaboration: EurekaManager.Instance is null.");
            }

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
        else
        {
            Debug.LogWarning($"FinalizeCollaboration: Collaboration ID '{collabID}' not found.");
        }
    }

    public List<UniversalCharacterController> GetCollaborators(string collabID)
    {
        if (activeCollabs.TryGetValue(collabID, out List<UniversalCharacterController> collaborators))
        {
            return collaborators;
        }
        return new List<UniversalCharacterController>();
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
