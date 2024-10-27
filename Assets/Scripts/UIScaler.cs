using UnityEngine;
using UnityEngine.UI;

public class UIScaler : MonoBehaviour 
{
    [SerializeField] private float referenceWidth4K = 3840f;
    [SerializeField] private float referenceHeight4K = 2160f;
    [SerializeField] private float referenceWidth1080p = 1920f;
    [SerializeField] private float referenceHeight1080p = 1080f;

    void Awake()
    {
        var canvasScaler = GetComponent<CanvasScaler>();
        
        // Keep your current Canvas Scaler settings:
        // UI Scale Mode: Scale With Screen Size
        // Reference Resolution: 3840 x 2160
        // Screen Match Mode: Match Width Or Height
        // Match: 0.5

        if (Screen.width <= referenceWidth1080p)
        {
            float scaleFactor = Screen.width / referenceWidth4K;
            canvasScaler.scaleFactor = scaleFactor;
        }
    }
}