using UnityEngine;

public class GuideBoxManager : MonoBehaviour
{
    public static GuideBoxManager Instance { get; private set; }

    [SerializeField] private GameObject guideDisplay;
    [SerializeField] private GameObject guideDisplayPrompt;

    public bool IsGuideDisplayVisible()
    {
        return guideDisplay != null && guideDisplay.activeSelf;
    }

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
        if (guideDisplay != null && guideDisplayPrompt != null)
        {
            guideDisplay.SetActive(false);     
            guideDisplayPrompt.SetActive(true); 
        }
        else
        {
            Debug.LogError("Guide Display or Prompt is not assigned in the GuideBoxManager!");
        }
    }

    public void ToggleGuideDisplay()
    {
        bool newGuideState = !guideDisplay.activeSelf;
        guideDisplay.SetActive(newGuideState);
        guideDisplayPrompt.SetActive(!newGuideState);
        InputManager.Instance.SetUIActive(newGuideState);
    }
}