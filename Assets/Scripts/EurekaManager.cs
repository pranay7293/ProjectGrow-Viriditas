using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;
using System.Linq;
using System.Threading.Tasks;

public class EurekaManager : MonoBehaviourPunCallbacks
{
    public static EurekaManager Instance { get; private set; }

    [SerializeField] private GameObject eurekaEffectPrefab;

    private List<string> recentEurekas = new List<string>();
    private const int maxRecentEurekas = 5;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void TriggerEureka(List<UniversalCharacterController> collaborators, string actionName)
    {
        if (collaborators == null || collaborators.Count < 2)
        {
            Debug.LogWarning($"TriggerEureka: Invalid collaborators list for action {actionName}");
            return;
        }

        Debug.Log($"Triggering Eureka for action: {actionName} with {collaborators.Count} collaborators");

        if (PhotonNetwork.IsMasterClient)
        {
            int[] collaboratorViewIDs = collaborators
                .Where(c => c != null && c.photonView != null)
                .Select(c => c.photonView.ViewID)
                .ToArray();

            if (collaboratorViewIDs.Length > 0)
            {
                photonView.RPC("RPC_TriggerEureka", RpcTarget.All, collaboratorViewIDs, actionName);
            }
            else
            {
                Debug.LogWarning("TriggerEureka: No valid collaborator ViewIDs found.");
            }
        }
    }

    [PunRPC]
    private async void RPC_TriggerEureka(int[] collaboratorViewIDs, string actionName)
    {
        List<UniversalCharacterController> collaborators = collaboratorViewIDs
            .Select(id => PhotonView.Find(id)?.GetComponent<UniversalCharacterController>())
            .Where(c => c != null)
            .ToList();

        if (collaborators.Count < 2)
        {
            Debug.LogWarning("Eureka requires at least two collaborators.");
            return;
        }

        var (description, generatedTags) = await OpenAIService.Instance.GenerateEurekaDescriptionAndTags(collaborators, GameManager.Instance.GetCurrentGameState(), actionName);
        List<string> validTags = TagSystem.ValidateEurekaTags(generatedTags);

        // Apply tag effects
        List<(string tag, float weight)> tagsWithWeights = validTags.Select(t => (t, 0.2f)).ToList();
        GameManager.Instance.UpdateMilestoneProgress("Eureka", "EurekaAction", tagsWithWeights);

        EurekaUI.Instance.DisplayEurekaNotification(collaborators, actionName);
        EurekaLogManager.Instance.AddEurekaLogEntry(description, collaborators);

        foreach (var collaborator in collaborators)
        {
            GameManager.Instance.UpdatePlayerScore(collaborator.characterName, ScoreConstants.EUREKA_BONUS, "Eureka Moment", new List<string> { "Eureka" });

            Vector3 textPosition = collaborator.transform.position + Vector3.up * 2f;
            FloatingTextManager.Instance.ShowFloatingText($"+{ScoreConstants.EUREKA_BONUS} Eureka!", textPosition, FloatingTextType.Eureka);
        }

        if (collaborators[0].currentLocation != null)
        {
            collaborators[0].currentLocation.PlayEurekaEffect();
        }

        AddRecentEureka(description);

        Debug.Log($"Eureka triggered: {description}");
    }

    private void AddRecentEureka(string eurekaDescription)
    {
        recentEurekas.Add(eurekaDescription);
        if (recentEurekas.Count > maxRecentEurekas)
        {
            recentEurekas.RemoveAt(0);
        }
    }

    public List<string> GetRecentEurekas()
    {
        return new List<string>(recentEurekas);
    }
}

// using UnityEngine;
// using System.Collections.Generic;
// using Photon.Pun;
// using System.Threading.Tasks;
// using System.Linq;

// public class EurekaManager : MonoBehaviourPunCallbacks
// {
//     public static EurekaManager Instance { get; private set; }

//     [SerializeField] private GameObject eurekaEffectPrefab;

//     private List<string> recentEurekas = new List<string>();
//     private const int maxRecentEurekas = 5;

//     private void Awake()
//     {
//         if (Instance == null)
//         {
//             Instance = this;
//             DontDestroyOnLoad(gameObject);
//         }
//         else if (Instance != this)
//         {
//             Destroy(gameObject);
//         }
//     }

//     private void Start()
//     {
//         Debug.Log("EurekaManager initialized.");
//     }

//     public void TriggerEureka(List<UniversalCharacterController> collaborators, string actionName)
// {
//     if (collaborators == null || collaborators.Count < 2)
//     {
//         Debug.LogWarning($"TriggerEureka: Invalid collaborators list for action {actionName}");
//         return;
//     }

//     Debug.Log($"Triggering Eureka for action: {actionName} with {collaborators.Count} collaborators");

//     if (PhotonNetwork.IsMasterClient)
//     {
//         int[] collaboratorViewIDs = collaborators
//             .Where(c => c != null && c.photonView != null)
//             .Select(c => c.photonView.ViewID)
//             .ToArray();

//         if (collaboratorViewIDs.Length > 0)
//         {
//             photonView.RPC("RPC_TriggerEureka", RpcTarget.All, collaboratorViewIDs, actionName);
//         }
//         else
//         {
//             Debug.LogWarning("TriggerEureka: No valid collaborator ViewIDs found.");
//         }
//     }
// }

// [PunRPC]
// private async void RPC_TriggerEureka(int[] collaboratorViewIDs, string actionName)
// {
//     List<UniversalCharacterController> collaborators = collaboratorViewIDs
//         .Select(id => PhotonView.Find(id)?.GetComponent<UniversalCharacterController>())
//         .Where(c => c != null)
//         .ToList();

//     if (collaborators.Count == 0)
//     {
//         Debug.LogWarning("No valid collaborators found for Eureka event.");
//         return;
//     }

//     string eurekaDescription = await OpenAIService.Instance.GenerateEurekaDescription(collaborators, GameManager.Instance.GetCurrentGameState(), actionName);

//     string completedMilestone = GameManager.Instance.CompleteRandomMilestone(eurekaDescription);

//     EurekaUI.Instance.DisplayEurekaNotification(collaborators, actionName);

//     List<string> involvedCharacters = collaborators.Select(c => c.characterName).ToList();
//     EurekaLogManager.Instance.AddEurekaLogEntry(eurekaDescription, collaborators);

//     AddRecentEureka(eurekaDescription);

//     foreach (var collaborator in collaborators)
//     {
//         collaborator.IncrementEurekaCount();
//         GameManager.Instance.UpdatePlayerScore(collaborator.characterName, ScoreConstants.EUREKA_BONUS, "Eureka Moment", new List<string> { "Eureka" });

//         Vector3 textPosition = collaborator.transform.position + Vector3.up * 2f;
//         FloatingTextManager.Instance.ShowFloatingText($"+{ScoreConstants.EUREKA_BONUS} Eureka!", textPosition, FloatingTextType.Eureka);
//     }

//     GameManager.Instance.UpdateMilestoneProgress("Eureka", "Eureka Moment");

//     if (collaborators[0].currentLocation != null)
//     {
//         collaborators[0].currentLocation.PlayEurekaEffect();
//     }

//     Debug.Log($"Eureka triggered: {eurekaDescription}");
// }

//     private void AddRecentEureka(string eurekaDescription)
//     {
//         recentEurekas.Add(eurekaDescription);
//         if (recentEurekas.Count > maxRecentEurekas)
//         {
//             recentEurekas.RemoveAt(0);
//         }
//     }

//     public List<string> GetRecentEurekas()
//     {
//         return new List<string>(recentEurekas);
//     }
// }