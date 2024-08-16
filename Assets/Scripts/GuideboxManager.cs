using UnityEngine;

public class GuideBoxManager : MonoBehaviour
{
    public static GuideBoxManager Instance { get; private set; }

    [SerializeField] private GameObject guideDisplay;

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
        if (guideDisplay != null)
        {
            guideDisplay.SetActive(true); // Start with the guide visible
        }
        else
        {
            Debug.LogError("Guide Display is not assigned in the GuideBoxManager!");
        }
    }

    public void ToggleGuideDisplay()
    {
        if (guideDisplay != null)
        {
            guideDisplay.SetActive(!guideDisplay.activeSelf);
        }
        else
        {
            Debug.LogError("Guide Display is not assigned in the GuideBoxManager!");
        }
    }
}