using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System.Collections;

public class EurekaLogUI : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform contentTransform;
    [SerializeField] private GameObject entryPrefab;
    [SerializeField] private GameObject eurekaLogPanel;
    [SerializeField] private CanvasGroup eurekaLogCanvasGroup;
    [SerializeField] private float fadeDuration = 0.3f;
    private List<EurekaEntryUI> entryUIs = new List<EurekaEntryUI>();

    private void OnEnable()
    {
        EurekaLogManager.Instance.OnEurekaLogEntryAdded += OnEurekaLogEntryAdded;
        RefreshEurekaLog();
    }

    private void OnDisable()
    {
        EurekaLogManager.Instance.OnEurekaLogEntryAdded -= OnEurekaLogEntryAdded;
    }

    private void OnEurekaLogEntryAdded(EurekaLogManager.EurekaLogEntry entry)
    {
        RefreshEurekaLog();
    }

    public void RefreshEurekaLog()
    {
        ClearEntries();
        List<EurekaLogManager.EurekaLogEntry> entries = EurekaLogManager.Instance.GetEurekaLog();
        
        for (int i = 0; i < entries.Count; i++)
        {
            CreateEurekaEntry(entries[i]);
        }

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    private void CreateEurekaEntry(EurekaLogManager.EurekaLogEntry entry)
    {
        GameObject entryObject = Instantiate(entryPrefab, contentTransform);
        EurekaEntryUI entryUI = entryObject.GetComponent<EurekaEntryUI>();
        entryUI.SetEntryData(entry);
        entryUIs.Add(entryUI);

        EventTrigger trigger = entryObject.AddComponent<EventTrigger>();
        AddEventTrigger(trigger, EventTriggerType.PointerEnter, (data) => { entryUI.OnHover(true); });
        AddEventTrigger(trigger, EventTriggerType.PointerExit, (data) => { entryUI.OnHover(false); });
    }

    private void AddEventTrigger(EventTrigger trigger, EventTriggerType eventType, UnityEngine.Events.UnityAction<BaseEventData> action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = eventType;
        entry.callback.AddListener(action);
        trigger.triggers.Add(entry);
    }

    private void ClearEntries()
    {
        foreach (var entryUI in entryUIs)
        {
            Destroy(entryUI.gameObject);
        }
        entryUIs.Clear();
    }

    public void ToggleEurekaLog()
    {
        if (eurekaLogPanel == null)
        {
            Debug.LogError("EurekaLogUI: EurekaLogPanel is not assigned.");
            return;
        }

        bool isVisible = !eurekaLogPanel.activeSelf;
        eurekaLogPanel.SetActive(isVisible);
        InputManager.Instance.SetUIActive(isVisible);

        if (isVisible)
        {
            RefreshEurekaLog();
            StartCoroutine(FadeCanvasGroup(eurekaLogCanvasGroup, eurekaLogCanvasGroup.alpha, 1f, fadeDuration));
        }
        else
        {
            StartCoroutine(FadeCanvasGroup(eurekaLogCanvasGroup, eurekaLogCanvasGroup.alpha, 0f, fadeDuration));
        }
    }

    public bool IsLogVisible()
    {
        return eurekaLogPanel != null && eurekaLogPanel.activeSelf;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float start, float end, float duration)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            cg.alpha = Mathf.Lerp(start, end, elapsedTime / duration);
            yield return null;
        }
        cg.alpha = end;
    }

    private void Update()
    {
        if (IsLogVisible() && scrollRect != null)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                scrollRect.verticalNormalizedPosition += scroll * 0.1f;
            }
        }
    }
}