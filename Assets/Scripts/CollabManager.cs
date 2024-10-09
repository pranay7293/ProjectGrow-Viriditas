    using UnityEngine;
    using System.Collections;
    using System.Collections.Generic;
    using Photon.Pun;
    using System.Linq;
    using System.Threading.Tasks;
    using static CharacterState;

    public class CollabManager : MonoBehaviourPunCallbacks
    {
        public static CollabManager Instance { get; private set; }

        [SerializeField] private float collabRadius = 10f;
        [SerializeField] private float collabCooldown = 5f;
        [SerializeField] private int maxCollaborators = 3;
        [SerializeField] private float collabBonusMultiplier = 0.5f;
        [SerializeField] private float collabDuration = 15f;

        private Dictionary<string, List<UniversalCharacterController>> activeCollabs = new Dictionary<string, List<UniversalCharacterController>>();
        private Dictionary<string, float> collabCooldowns = new Dictionary<string, float>();

        private PhotonView photonView;

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
                return;
            }

            photonView = GetComponent<PhotonView>();
            if (photonView == null)
            {
                Debug.LogError("PhotonView component is missing on CollabManager!");
            }
        }

        private void Update()
        {
            UpdateCooldowns();
            CheckAndFinalizeCollaborations();
        }

        public bool CanInitiateCollab(UniversalCharacterController initiator)
        {
            if (initiator == null || string.IsNullOrEmpty(initiator.characterName))
            {
                Debug.LogWarning("Invalid initiator in CanInitiateCollab");
                return false;
            }
            return !collabCooldowns.ContainsKey(initiator.characterName) && !initiator.IsCollaborating;
        }

        public List<UniversalCharacterController> GetEligibleCollaborators(UniversalCharacterController initiator)
        {
            List<UniversalCharacterController> eligibleCollaborators = new List<UniversalCharacterController>();
            if (initiator == null) return eligibleCollaborators;

            foreach (var character in GameManager.Instance.GetAllCharacters())
            {
                if (character != null && character != initiator &&
                    !character.IsCollaborating &&
                    character.currentLocation == initiator.currentLocation &&
                    Vector3.Distance(initiator.transform.position, character.transform.position) <= collabRadius)
                {
                    eligibleCollaborators.Add(character);
                }
            }

            Debug.Log($"Found {eligibleCollaborators.Count} eligible collaborators for {initiator.characterName}");
            return eligibleCollaborators.Take(maxCollaborators - 1).ToList();
        }

        public void RequestCollaboration(int initiatorViewID, int[] targetViewIDs, string actionName)
        {
            if (photonView == null)
            {
                Debug.LogError("PhotonView is null in RequestCollaboration!");
                return;
            }

            photonView.RPC("RPC_RequestCollaboration", RpcTarget.All, initiatorViewID, targetViewIDs, actionName);
        }

        [PunRPC]
        private void RPC_RequestCollaboration(int initiatorViewID, int[] targetViewIDs, string actionName)
        {
            PhotonView initiatorView = PhotonView.Find(initiatorViewID);
            if (initiatorView == null)
            {
                Debug.LogWarning($"Initiator PhotonView not found for ViewID: {initiatorViewID}");
                return;
            }

            UniversalCharacterController initiator = initiatorView.GetComponent<UniversalCharacterController>();
            if (initiator == null)
            {
                Debug.LogWarning($"UniversalCharacterController not found on initiator with ViewID: {initiatorViewID}");
                return;
            }

            foreach (int targetViewID in targetViewIDs)
            {
                PhotonView targetView = PhotonView.Find(targetViewID);
                if (targetView == null)
                {
                    Debug.LogWarning($"Target PhotonView not found for ViewID: {targetViewID}");
                    continue;
                }

                UniversalCharacterController target = targetView.GetComponent<UniversalCharacterController>();
                if (target == null)
                {
                    Debug.LogWarning($"UniversalCharacterController not found on target with ViewID: {targetViewID}");
                    continue;
                }

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
                    else
                    {
                        Debug.LogWarning($"AIManager not found on non-player character: {target.characterName}");
                    }
                }
            }
        }

        private IEnumerator DecideOnCollaborationCoroutine(AIManager aiManager, string actionName, int initiatorViewID, int targetViewID)
        {
            if (aiManager == null) yield break;

            bool decisionResult = aiManager.DecideOnCollaboration(actionName);
            if (decisionResult)
            {
                string collabID = System.Guid.NewGuid().ToString();
                if (photonView != null)
                {
                    photonView.RPC("InitiateCollab", RpcTarget.All, actionName, initiatorViewID, new int[] { targetViewID }, collabID);
                }
                else
                {
                    Debug.LogError("PhotonView is null when trying to initiate collaboration!");
                }
            }
        }

        [PunRPC]
        public void InitiateCollab(string actionName, int initiatorViewID, int[] collaboratorViewIDs, string collabID)
        {
            if (string.IsNullOrEmpty(actionName) || string.IsNullOrEmpty(collabID))
            {
                Debug.LogWarning("Invalid action name or collabID in InitiateCollab");
                return;
            }

            List<UniversalCharacterController> collaborators = new List<UniversalCharacterController>();
            UniversalCharacterController initiatorCharacter = null;

            PhotonView initiatorView = PhotonView.Find(initiatorViewID);
            if (initiatorView != null)
            {
                initiatorCharacter = initiatorView.GetComponent<UniversalCharacterController>();
                if (initiatorCharacter != null)
                {
                    collaborators.Add(initiatorCharacter);
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

            if (collaborators.Count < 2 || initiatorCharacter == null)
            {
                Debug.LogWarning($"InitiateCollab: Not enough collaborators or invalid initiator for collabID {collabID}");
                return;
            }

            activeCollabs[collabID] = collaborators;

            LocationManager location = initiatorCharacter.currentLocation;
            if (location == null)
            {
                Debug.LogWarning($"InitiateCollab: Initiator {initiatorCharacter.characterName} has no current location");
                return;
            }

            LocationManager.LocationAction action = location.GetActionByName(actionName);
            if (action == null)
            {
                Debug.LogWarning($"InitiateCollab: Action {actionName} not found at location {location.locationName}");
                return;
            }

            List<Vector3> collabPositions = WaypointsManager.Instance.GetCollaborationPositions(location.locationName, collaborators.Count);

            for (int i = 0; i < collaborators.Count; i++)
            {
                UniversalCharacterController collaborator = collaborators[i];
                if (collaborator != null)
                {   
                    SetCollabCooldown(collaborator.characterName);
                    collaborator.StartCollaboration(action, collabID);

                    if (collabPositions != null && i < collabPositions.Count)
                    {
                        collaborator.MoveWhileInState(collabPositions[i], collaborator.walkSpeed * 0.5f);
                    }

                    ActionLogManager.Instance?.LogAction(
                        collaborator.characterName,
                        $"Started collaboration on {actionName} with {string.Join(", ", collaborators.Where(c => c != collaborator).Select(c => c.characterName))}"
                    );
                }
            }

            // Group management if necessary
            if (collaborators.Count > 1 && !initiatorCharacter.IsInGroup())
            {
                if (GroupManager.Instance != null)
                {
                    GroupManager.Instance.FormGroup(collaborators);
                }
            }

            StartCoroutine(MoveCollaboratingGroup(collaborators, location));
        }

        private void CheckAndFinalizeCollaborations()
        {
            List<string> collabsToFinalize = new List<string>();

            foreach (var collab in activeCollabs)
            {
                if (collab.Value.All(c => c.CollaborationTimeElapsed >= collabDuration))
                {
                    collabsToFinalize.Add(collab.Key);
                }
            }

            foreach (var collabID in collabsToFinalize)
            {
                FinalizeCollaboration(collabID);
            }
        }

        private IEnumerator MoveCollaboratingGroup(List<UniversalCharacterController> collaborators, LocationManager location)
{
    while (collaborators.Any(c => c.IsCollaborating))
    {
        Vector3 newDestination = WaypointsManager.Instance.GetWaypointNearLocation(location.locationName);
        foreach (var collaborator in collaborators)
        {
            collaborator.MoveWhileInState(newDestination, collaborator.walkSpeed * 0.5f);
        }
        yield return new WaitForSeconds(10f); // Move every 10 seconds
    }
}


        public void FinalizeCollaboration(string collabID)
        {
            if (string.IsNullOrEmpty(collabID) || !activeCollabs.TryGetValue(collabID, out List<UniversalCharacterController> collaborators))
            {
                Debug.LogWarning($"FinalizeCollaboration: Invalid collabID or no active collaboration found for ID: {collabID}");
                return;
            }

            if (collaborators == null || collaborators.Count == 0)
            {
                Debug.LogWarning($"FinalizeCollaboration: No collaborators found for collabID: {collabID}");
                activeCollabs.Remove(collabID);
                return;
            }

            UniversalCharacterController initiator = collaborators[0];
            if (initiator == null) return;

            float actionDuration = initiator.currentAction != null ? initiator.currentAction.duration : collabDuration;

            int basePoints = ScoreConstants.GetActionPoints((int)actionDuration);
            int collabBonus = Mathf.RoundToInt(basePoints * collabBonusMultiplier);

            foreach (var collaborator in collaborators)
            {
                if (collaborator != null)
                {
                    GameManager.Instance.UpdatePlayerScore(collaborator.characterName, basePoints, $"Completed {collaborators.Count}-person collaboration", new List<string> { "Collaboration" });
                    GameManager.Instance.UpdatePlayerScore(collaborator.characterName, collabBonus, $"Collaboration bonus", new List<string> { "CollaborationBonus" });
                    collaborator.EndCollab();
                }
            }

            // Always trigger Eureka for successful collaborations
            if (EurekaManager.Instance != null)
            {
                string actionName = initiator.CurrentActionName;
                EurekaManager.Instance.TriggerEureka(collaborators, actionName);
            }
            else
            {
                Debug.LogError("EurekaManager.Instance is null when trying to trigger Eureka");
            }

            activeCollabs.Remove(collabID);

            // Disband group if necessary
            if (collaborators.Count > 1 && initiator.IsInGroup())
            {
                string groupId = initiator.GetCurrentGroupId();
                if (!string.IsNullOrEmpty(groupId))
                {
                    GroupManager.Instance.DisbandGroup(groupId);
                }
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
                        progressBar.UpdateKeyState(CharacterState.Cooldown);
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
                        character.RemoveState(CharacterState.Cooldown);
                    }
                }
                else
                {
                    collabCooldowns[characterName] = remainingCooldown;
                }
            }
        }
    }