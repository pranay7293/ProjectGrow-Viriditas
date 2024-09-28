using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;
using System.Threading.Tasks;
using System.Linq;

public class EurekaManager : MonoBehaviourPunCallbacks
{
    public static EurekaManager Instance { get; private set; }

    [SerializeField] private GameObject eurekaEffectPrefab;

    private List<string> recentEurekas = new List<string>();
    private const int maxRecentEurekas = 5;

    private bool isInitialized = false;

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

    private void Start()
    {
        InitializeManager();
    }

    public override void OnEnable()
    {
        base.OnEnable();
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable()
    {
        base.OnDisable();
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    public override void OnJoinedRoom()
    {
        InitializeManager();
    }

    private void InitializeManager()
    {
        if (!isInitialized)
        {
            isInitialized = true;
            Debug.Log("EurekaManager initialized.");
        }
    }

    public void TriggerEureka(List<UniversalCharacterController> collaborators, string actionName)
    {
        Debug.Log($"Triggering Eureka for action: {actionName} with {collaborators.Count} collaborators");
        
        if (!isInitialized)
        {
            Debug.LogWarning("EurekaManager not initialized. Skipping Eureka trigger.");
            return;
        }

        if (PhotonNetwork.IsMasterClient && collaborators != null && collaborators.Count > 0)
        {
            int[] collaboratorViewIDs = collaborators.Select(c => c.photonView.ViewID).ToArray();
            photonView.RPC("RPC_TriggerEureka", RpcTarget.All, collaboratorViewIDs, actionName);
        }
    }

    [PunRPC]
    private async void RPC_TriggerEureka(int[] collaboratorViewIDs, string actionName)
    {
        List<UniversalCharacterController> collaborators = collaboratorViewIDs
            .Select(id => PhotonView.Find(id)?.GetComponent<UniversalCharacterController>())
            .Where(c => c != null)
            .ToList();

        if (collaborators.Count == 0)
        {
            Debug.LogWarning("No valid collaborators found for Eureka event.");
            return;
        }

        string eurekaDescription = await OpenAIService.Instance.GenerateEurekaDescription(collaborators, GameManager.Instance.GetCurrentGameState(), actionName);

        string completedMilestone = GameManager.Instance.CompleteRandomMilestone(eurekaDescription);

        EurekaUI.Instance.DisplayEurekaNotification(eurekaDescription);

        List<string> involvedCharacters = collaborators.Select(c => c.characterName).ToList();
        EurekaLogManager.Instance.AddEurekaLogEntry(eurekaDescription, involvedCharacters);

        AddRecentEureka(eurekaDescription);

        foreach (var collaborator in collaborators)
        {
            collaborator.IncrementEurekaCount();
            GameManager.Instance.UpdatePlayerScore(collaborator.characterName, ScoreConstants.EUREKA_BONUS, "Eureka Moment", new List<string> { "Eureka" });

            Vector3 textPosition = collaborator.transform.position + Vector3.up * 2f;
            FloatingTextManager.Instance.ShowFloatingText($"+{ScoreConstants.EUREKA_BONUS} Eureka!", textPosition, FloatingTextType.Eureka);
        }

        GameManager.Instance.UpdateMilestoneProgress("Eureka", "Eureka Moment");

        // Trigger the Eureka effect at the location
        if (collaborators.Count > 0 && collaborators[0].currentLocation != null)
        {
            collaborators[0].currentLocation.PlayEurekaEffect();
        }

        Debug.Log($"Eureka triggered: {eurekaDescription}");
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