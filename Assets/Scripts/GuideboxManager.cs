using UnityEngine;

public class GuideBoxManager : MonoBehaviour
{
    public static GuideBoxManager Instance { get; private set; }

    [SerializeField] private GameObject guideDisplay;

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
        guideDisplay.SetActive(!guideDisplay.activeSelf);
        InputManager.Instance.SetUIActive(guideDisplay.activeSelf);  // Add this line
    }
}