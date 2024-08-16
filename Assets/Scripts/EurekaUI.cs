using UnityEngine;
using TMPro;

public class EurekaUI : MonoBehaviour
{
    public static EurekaUI Instance { get; private set; }

    [SerializeField] private GameObject eurekaNotificationPanel;
    [SerializeField] private TextMeshProUGUI eurekaDescriptionText;
    [SerializeField] private float notificationDuration = 5f;

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

    public void DisplayEurekaNotification(string description)
    {
        eurekaDescriptionText.text = "Eureka Moment!\n" + description;
        eurekaNotificationPanel.SetActive(true);
        Invoke(nameof(HideNotification), notificationDuration);
    }

    private void HideNotification()
    {
        eurekaNotificationPanel.SetActive(false);
    }
}