using UnityEngine;
using TMPro;
using System.Collections;

public class FloatingText : MonoBehaviour
{
    private TextMeshProUGUI textMesh;
    private RectTransform rectTransform;
    
    [SerializeField] private float moveSpeed = 100f;
    [SerializeField] private float fadeDuration = 2f;

    public float FadeDuration => fadeDuration;

    private void Awake()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
        rectTransform = GetComponent<RectTransform>();
    }

    public void Initialize(string text, Color color)
    {
        textMesh.text = text;
        textMesh.color = color;
        StartCoroutine(FloatAndFade());
    }

    private IEnumerator FloatAndFade()
    {
        float elapsedTime = 0f;
        Color initialColor = textMesh.color;
        Vector2 initialPosition = rectTransform.anchoredPosition;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = 1 - (elapsedTime / fadeDuration);

            textMesh.color = new Color(initialColor.r, initialColor.g, initialColor.b, alpha);
            rectTransform.anchoredPosition = initialPosition + Vector2.up * moveSpeed * elapsedTime;

            yield return null;
        }

        gameObject.SetActive(false);
    }
}