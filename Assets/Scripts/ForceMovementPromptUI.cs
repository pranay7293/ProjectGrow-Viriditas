using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class ForceMovementPromptUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image background;
    [SerializeField] private TextMeshProUGUI descriptionText;
    
    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.2f;
    [SerializeField] private float scaleAmount = 0.95f;
    
    private Color defaultColor;
    private Color pressedColor;
    private Vector3 defaultScale;
    private Sequence currentAnimation;

    private void Awake()
    {
        // Initialize colors
        defaultColor = new Color(0.05f, 0.53f, 0.97f); // 0D86F8
        pressedColor = new Color(0.72f, 0.20f, 0.54f); // B8348A
        defaultScale = transform.localScale;
    }

    public void PlayPressEffect()
    {
        // Kill any running animation
        if (currentAnimation != null && currentAnimation.IsPlaying())
        {
            currentAnimation.Kill();
        }

        // Create new animation sequence
        currentAnimation = DOTween.Sequence();

        // Add animations to sequence
        currentAnimation.Join(background.DOColor(pressedColor, animationDuration))
                      .Join(transform.DOScale(defaultScale * scaleAmount, animationDuration))
                      .AppendInterval(animationDuration)
                      .Append(background.DOColor(defaultColor, animationDuration))
                      .Join(transform.DOScale(defaultScale, animationDuration));
    }

    // Call this when dialogue/UI is active to ensure visual consistency
    public void ResetToDefault()
    {
        if (currentAnimation != null && currentAnimation.IsPlaying())
        {
            currentAnimation.Kill();
        }
        
        background.color = defaultColor;
        transform.localScale = defaultScale;
    }
}