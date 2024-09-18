using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DialogueRequestUI : MonoBehaviour
{
    public static DialogueRequestUI Instance { get; private set; }

    [SerializeField] private GameObject promptPanel;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button declineButton;
    [SerializeField] private float timeoutDuration = 5f; // 5 seconds timeout

    private UniversalCharacterController initiatorCharacter;
    private Coroutine timeoutCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Ensure the UI is part of the scene hierarchy
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (promptPanel == null)
        {
            Debug.LogError("DialogueRequestUI: PromptPanel is not assigned.");
            return;
        }

        promptPanel.SetActive(false);

        if (acceptButton != null)
            acceptButton.onClick.AddListener(AcceptRequest);
        else
            Debug.LogError("DialogueRequestUI: AcceptButton is not assigned.");

        if (declineButton != null)
            declineButton.onClick.AddListener(DeclineRequest);
        else
            Debug.LogError("DialogueRequestUI: DeclineButton is not assigned.");
    }

    public void ShowRequest(UniversalCharacterController initiator)
    {
        if (promptPanel == null || promptText == null)
        {
            Debug.LogError("DialogueRequestUI: UI elements are not assigned.");
            return;
        }

        initiatorCharacter = initiator;
        promptText.text = $"{initiator.characterName} wants to talk to you. Do you accept?";
        promptPanel.SetActive(true);

        if (timeoutCoroutine != null)
        {
            StopCoroutine(timeoutCoroutine);
        }
        timeoutCoroutine = StartCoroutine(RequestTimeout());
    }

    public void AcceptRequest()
    {
        if (timeoutCoroutine != null)
        {
            StopCoroutine(timeoutCoroutine);
        }

        if (initiatorCharacter != null)
        {
            DialogueManager.Instance.AcceptDialogueRequest(initiatorCharacter);
        }
        else
        {
            Debug.LogWarning("DialogueRequestUI: InitiatorCharacter is not assigned.");
        }

        HidePrompt();
    }

    public void DeclineRequest()
    {
        if (timeoutCoroutine != null)
        {
            StopCoroutine(timeoutCoroutine);
        }

        DialogueManager.Instance.DeclineDialogueRequest();
        HidePrompt();
    }

    public void HideRequest()
    {
        HidePrompt();
    }

    private void HidePrompt()
    {
        promptPanel.SetActive(false);
        initiatorCharacter = null;
    }

    private IEnumerator RequestTimeout()
    {
        yield return new WaitForSeconds(timeoutDuration);
        DeclineRequest();
    }

    public bool IsRequestActive()
    {
        return promptPanel != null && promptPanel.activeSelf;
    }
}
