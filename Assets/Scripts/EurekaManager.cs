using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;
using System.Threading.Tasks;

public class EurekaManager : MonoBehaviourPunCallbacks
{
    public static EurekaManager Instance { get; private set; }

    [SerializeField] private float baseProbability = 0.1f;
    [SerializeField] private float diversityMultiplier = 0.05f;

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

    public async Task TriggerEureka(List<UniversalCharacterController> collaborators)
    {
        string eurekaDescription = await OpenAIService.Instance.GenerateEurekaDescription(collaborators, GameManager.Instance.GetCurrentGameState());
        GameManager.Instance.CompleteRandomMilestone(eurekaDescription);
        EurekaUI.Instance.DisplayEurekaNotification(eurekaDescription);

        foreach (var collaborator in collaborators)
        {
            collaborator.IncrementEurekaCount();
        }
    }
}