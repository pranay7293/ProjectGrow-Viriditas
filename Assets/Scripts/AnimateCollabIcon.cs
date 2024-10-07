using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class AnimateCollabIcon : MonoBehaviour
{
    [SerializeField] private float pulseDuration = 1f;
    [SerializeField] private float minScale = 0.9f;
    [SerializeField] private float maxScale = 1.1f;

    private Image iconImage;
    private Tween pulseTween;

    private void Awake()
    {
        iconImage = GetComponent<Image>();
    }

    public void StartAnimation()
    {
        if (pulseTween != null)
        {
            pulseTween.Kill();
        }

        pulseTween = transform.DOScale(maxScale, pulseDuration / 2)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    public void StopAnimation()
    {
        if (pulseTween != null)
        {
            pulseTween.Kill();
        }
        transform.localScale = Vector3.one;
    }

    public void SetColor(Color color)
    {
        iconImage.color = color;
    }

    private void OnDisable()
    {
        StopAnimation();
    }
}