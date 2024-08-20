using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;
using System.Threading.Tasks;
using System.Linq;

public class EurekaManager : MonoBehaviourPunCallbacks
{
    public static EurekaManager Instance { get; private set; }

    [SerializeField] private float baseProbability = 0.1f;
    [SerializeField] private float diversityMultiplier = 0.05f;
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
        else
        {
            Destroy(gameObject);
        }
    }

    public bool CheckForEureka(List<UniversalCharacterController> collaborators)
    {
        float diversity = CalculateDiversity(collaborators);
        float probability = baseProbability + (diversity * diversityMultiplier);
        return Random.value < probability;
    }

    private float CalculateDiversity(List<UniversalCharacterController> collaborators)
    {
        HashSet<string> uniqueRoles = new HashSet<string>();
        foreach (var collaborator in collaborators)
        {
            uniqueRoles.Add(collaborator.aiSettings.characterRole);
        }
        return uniqueRoles.Count;
    }

    [PunRPC]
    public async void TriggerEureka(int[] collaboratorViewIDs)
    {
        List<UniversalCharacterController> collaborators = collaboratorViewIDs
            .Select(id => PhotonView.Find(id).GetComponent<UniversalCharacterController>())
            .ToList();

        string eurekaDescription = await OpenAIService.Instance.GenerateEurekaDescription(collaborators, GameManager.Instance.GetCurrentGameState());
        string completedMilestone = GameManager.Instance.CompleteRandomMilestone(eurekaDescription);

        EurekaUI.Instance.DisplayEurekaNotification(eurekaDescription);

        List<string> involvedCharacters = collaborators.Select(c => c.characterName).ToList();
        EurekaLogManager.Instance.AddEurekaLogEntry("Eureka Moment!", involvedCharacters, eurekaDescription, completedMilestone);

        AddRecentEureka(eurekaDescription);

        foreach (var collaborator in collaborators)
        {
            collaborator.IncrementEurekaCount();
            GameManager.Instance.UpdatePlayerScore(collaborator.characterName, ScoreConstants.EUREKA_BONUS);
            
            Vector3 textPosition = collaborator.transform.position + Vector3.up * 2f;
            FloatingTextManager.Instance.ShowFloatingText($"+{ScoreConstants.EUREKA_BONUS} Eureka!", textPosition, FloatingTextType.Eureka);
        }

        GameManager.Instance.UpdateMilestoneProgress("Eureka", "Eureka Moment");

        // Trigger the Eureka effect at the location
        if (collaborators.Count > 0 && collaborators[0].currentLocation != null)
        {
            collaborators[0].currentLocation.PlayEurekaEffect();
        }
    }

    public void InitiateEureka(List<UniversalCharacterController> collaborators)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            int[] collaboratorViewIDs = collaborators.Select(c => c.photonView.ViewID).ToArray();
            photonView.RPC("TriggerEureka", RpcTarget.All, new object[] { collaboratorViewIDs });
        }
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