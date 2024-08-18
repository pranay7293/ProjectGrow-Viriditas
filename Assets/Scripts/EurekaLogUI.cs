using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class EurekaLogUI : MonoBehaviour
{
    public static EurekaLogUI Instance { get; private set; }

    [SerializeField] private GameObject eurekaLogPanel;
    [SerializeField] private TMP_Dropdown characterFilter;
    [SerializeField] private TMP_Dropdown milestoneFilter;
    [SerializeField] private Transform entryContainer;
    [SerializeField] private GameObject entryPrefab;
    [SerializeField] private CanvasGroup canvasGroup;

    private List<EurekaLogManager.EurekaLogEntry> entries = new List<EurekaLogManager.EurekaLogEntry>();

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

        eurekaLogPanel.SetActive(false);
    }

    private void Start()
    {
        InitializeFilters();
    }

    private void InitializeFilters()
    {
        characterFilter.onValueChanged.AddListener(_ => RefreshEurekaLog());
        milestoneFilter.onValueChanged.AddListener(_ => RefreshEurekaLog());

        PopulateFilters();
    }

    private void PopulateFilters()
    {
        characterFilter.ClearOptions();
        characterFilter.AddOptions(new List<string> { "All Characters" });
        characterFilter.AddOptions(CharacterSelectionManager.characterFullNames.ToList());

        milestoneFilter.ClearOptions();
        milestoneFilter.AddOptions(new List<string> { "All Milestones" });
        milestoneFilter.AddOptions(GameManager.Instance.GetCurrentChallenge().milestones);
    }

    public void ToggleEurekaLog()
    {
        if (eurekaLogPanel.activeSelf)
        {
            CloseEurekaLog();
        }
        else
        {
            OpenEurekaLog();
        }
    }

    private void OpenEurekaLog()
    {
        eurekaLogPanel.SetActive(true);
        RefreshEurekaLog();
        StartCoroutine(FadeCanvasGroup(canvasGroup, 0f, 1f, 0.3f));
    }

    private void CloseEurekaLog()
    {
        StartCoroutine(FadeCanvasGroup(canvasGroup, 1f, 0f, 0.3f, () => eurekaLogPanel.SetActive(false)));
    }

    private void RefreshEurekaLog()
    {
        ClearEntries();
        entries = EurekaLogManager.Instance.GetEurekaLog();
        
        foreach (var entry in entries)
        {
            if (PassesFilters(entry))
            {
                CreateEurekaEntry(entry);
            }
        }
    }

    private bool PassesFilters(EurekaLogManager.EurekaLogEntry entry)
    {
        string selectedCharacter = characterFilter.options[characterFilter.value].text;
        string selectedMilestone = milestoneFilter.options[milestoneFilter.value].text;

        bool passesCharacterFilter = selectedCharacter == "All Characters" || entry.involvedCharacters.Contains(selectedCharacter);
        bool passesMilestoneFilter = selectedMilestone == "All Milestones" || entry.completedMilestone == selectedMilestone;

        return passesCharacterFilter && passesMilestoneFilter;
    }

    private void CreateEurekaEntry(EurekaLogManager.EurekaLogEntry entry)
    {
        GameObject entryObject = Instantiate(entryPrefab, entryContainer);
        EurekaEntryUI entryUI = entryObject.GetComponent<EurekaEntryUI>();
        entryUI.SetEntryData(entry);
    }

    private void ClearEntries()
    {
        foreach (Transform child in entryContainer)
        {
            Destroy(child.gameObject);
        }
    }

    private System.Collections.IEnumerator FadeCanvasGroup(CanvasGroup cg, float start, float end, float duration, System.Action onComplete = null)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            cg.alpha = Mathf.Lerp(start, end, elapsedTime / duration);
            yield return null;
        }
        cg.alpha = end;
        onComplete?.Invoke();
    }
}