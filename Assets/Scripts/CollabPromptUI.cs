using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class CollabPromptUI : MonoBehaviour
{
    public static CollabPromptUI Instance { get; private set; }

    [SerializeField] private GameObject promptPanel;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button declineButton;
    [SerializeField] private float timeoutDuration = 5f; // 5 seconds timeout

    private UniversalCharacterController initiatorCharacter;
    private UniversalCharacterController localCharacter;
    private string currentActionName;
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
            Debug.LogError("CollabPromptUI: PromptPanel is not assigned.");
            return;
        }

        promptPanel.SetActive(false);

        if (acceptButton != null)
            acceptButton.onClick.AddListener(AcceptCollab);
        else
            Debug.LogError("CollabPromptUI: AcceptButton is not assigned.");

        if (declineButton != null)
            declineButton.onClick.AddListener(DeclineCollab);
        else
            Debug.LogError("CollabPromptUI: DeclineButton is not assigned.");
    }

    public void ShowPrompt(UniversalCharacterController initiator, UniversalCharacterController localPlayer, string actionName)
    {
        if (promptPanel == null || promptText == null)
        {
            Debug.LogError("CollabPromptUI: UI elements are not assigned.");
            return;
        }

        initiatorCharacter = initiator;
        localCharacter = localPlayer;
        currentActionName = actionName;
        promptText.text = $"{initiator.characterName} wants to collaborate on {actionName}. Do you accept?";
        promptPanel.SetActive(true);

        if (timeoutCoroutine != null)
        {
            StopCoroutine(timeoutCoroutine);
        }
        timeoutCoroutine = StartCoroutine(RequestTimeout());
    }

    private void AcceptCollab()
    {
        if (timeoutCoroutine != null)
        {
            StopCoroutine(timeoutCoroutine);
        }

        if (localCharacter != null && initiatorCharacter != null)
        {
            localCharacter.JoinCollab(currentActionName, initiatorCharacter);
        }
        else
        {
            Debug.LogWarning("CollabPromptUI: Characters are not properly assigned.");
        }

        HidePrompt();
    }

    private void DeclineCollab()
    {
        if (timeoutCoroutine != null)
        {
            StopCoroutine(timeoutCoroutine);
        }
        HidePrompt();
    }

    private void HidePrompt()
    {
        promptPanel.SetActive(false);
        initiatorCharacter = null;
        localCharacter = null;
        currentActionName = null;
    }

    private IEnumerator RequestTimeout()
    {
        yield return new WaitForSeconds(timeoutDuration);
        DeclineCollab();
    }
}
