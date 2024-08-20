using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class FloatingTextManager : MonoBehaviour
{
    public static FloatingTextManager Instance { get; private set; }

    [SerializeField] private GameObject floatingTextPrefab;
    [SerializeField] private Canvas uiCanvas;

    [Header("Colors")]
    public Color pointsColor = new Color(1f, 0.867f, 0f); // FFDD00
    public Color milestoneColor = new Color(0.831f, 0.506f, 0.341f); // OD8157
    public Color collabColor = new Color(0.722f, 0.204f, 0.541f); // B8348A
    public Color eurekaColor = new Color(0.137f, 0.467f, 0.910f); // 2377E8

    private Queue<FloatingText> textPool = new Queue<FloatingText>();
    private int poolSize = 20;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeTextPool();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeTextPool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject textObject = Instantiate(floatingTextPrefab, uiCanvas.transform);
            FloatingText floatingText = textObject.GetComponent<FloatingText>();
            textObject.SetActive(false);
            textPool.Enqueue(floatingText);
        }
    }

    public void ShowFloatingText(string text, Vector3 worldPosition, FloatingTextType type)
    {
        if (textPool.Count == 0) return;

        FloatingText floatingText = textPool.Dequeue();
        floatingText.gameObject.SetActive(true);

        // Convert world position to screen position
        Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);

        // Adjust for canvas scaling
        if (uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            screenPosition.x *= uiCanvas.scaleFactor;
            screenPosition.y *= uiCanvas.scaleFactor;
        }

        floatingText.GetComponent<RectTransform>().position = screenPosition;

        Color color = GetColorForType(type);
        floatingText.Initialize(text, color);

        StartCoroutine(ReturnToPool(floatingText));
    }

    private Color GetColorForType(FloatingTextType type)
    {
        switch (type)
        {
            case FloatingTextType.Points:
                return pointsColor;
            case FloatingTextType.Milestone:
                return milestoneColor;
            case FloatingTextType.Collab:
                return collabColor;
            case FloatingTextType.Eureka:
                return eurekaColor;
            default:
                return Color.white;
        }
    }

    private System.Collections.IEnumerator ReturnToPool(FloatingText floatingText)
    {
        yield return new WaitForSeconds(floatingText.FadeDuration);
        if (floatingText.gameObject.activeSelf)
        {
            floatingText.gameObject.SetActive(false);
        }
        textPool.Enqueue(floatingText);
    }
}

public enum FloatingTextType
{
    Points,
    Milestone,
    Collab,
    Eureka
}