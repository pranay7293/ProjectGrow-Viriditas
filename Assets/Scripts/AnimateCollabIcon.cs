using UnityEngine;
using UnityEngine.UI;

public class AnimateCollabIcon : MonoBehaviour
{
    [SerializeField] private float pulseDuration = 1f;
    [SerializeField] private float minScale = 0.9f;
    [SerializeField] private float maxScale = 1.1f;

    private Image iconImage;
    private float pulseTimer;

    private void Awake()
    {
        iconImage = GetComponent<Image>();
    }

    public void StartAnimation()
    {
        enabled = true;
        pulseTimer = 0f;
    }

    public void StopAnimation()
    {
        enabled = false;
        transform.localScale = Vector3.one;
    }


    private void Update()
    {
        pulseTimer += Time.deltaTime;
        if (pulseTimer > pulseDuration)
        {
            pulseTimer -= pulseDuration;
        }

        float t = pulseTimer / pulseDuration;
        float scale = Mathf.Lerp(minScale, maxScale, (Mathf.Sin(t * Mathf.PI * 2) + 1) / 2);
        transform.localScale = Vector3.one * scale;
    }

    public void SetColor(Color color)
    {
        iconImage.color = color;
    }
}