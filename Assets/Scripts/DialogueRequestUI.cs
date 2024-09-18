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
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        promptPanel.SetActive(false);
        acceptButton.onClick.AddListener(AcceptRequest);
        declineButton.onClick.AddListener(DeclineRequest);
    }

    public void ShowRequest(UniversalCharacterController initiator)
    {
        initiatorCharacter = initiator;
        promptText.text = $"{initiator.characterName} wants to talk to you";
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
        DialogueManager.Instance.InitiateDialogue(initiatorCharacter);
        HidePrompt();
    }

    public void DeclineRequest()
    {
        if (timeoutCoroutine != null)
        {
            StopCoroutine(timeoutCoroutine);
        }
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
        return promptPanel.activeSelf;
    }
}