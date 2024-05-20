using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// attach this to an Entity, assocaite it with a Trait, and it will display a countdown timer for the associated Trait
// then remove itself when that timer expires

[RequireComponent(typeof(RectTransform))]
public class TraitTimerUI : MonoBehaviour
{
    [SerializeField] private Image timerFill;
    [SerializeField] private TMP_Text label;

    private Camera mainCamera;

    private RectTransform _rectTransform;
    private Trait _associatedTrait;

    private Vector3 _offset;

    public void Initialize(Trait associatedTrait, Vector3 offset)
    {
        _offset = offset;
        _associatedTrait = associatedTrait;

        label.text = _associatedTrait.traitClass.ToString();

        _rectTransform = GetComponent<RectTransform>();

        mainCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();

        UpdatePosition();
    }

    private void UpdatePosition()
    {
        var worldPosition = _associatedTrait.transform.position + _offset;

        // If behind - set scale to 0 to hide
        if (Vector3.Dot(worldPosition - mainCamera.transform.position, mainCamera.transform.forward) < 0)
        {
            _rectTransform.transform.localScale = Vector3.zero;
            return;
        }

        var viewportPosition = mainCamera.WorldToViewportPoint(worldPosition);
        _rectTransform.anchorMin = viewportPosition;
        _rectTransform.anchorMax = viewportPosition;

        var diff = worldPosition - mainCamera.transform.position;
        var dist = diff.magnitude;
        var pointSizeAtDistance = 2 * Mathf.Tan(mainCamera.fieldOfView * Mathf.Deg2Rad) * dist;
        const float scalar = 50f;
        var scale = scalar / pointSizeAtDistance;

        // TODO: Clamp scale range?

        _rectTransform.transform.localScale = Vector3.one * scale;

        // TODO: Fade by distance?
    }

    private void Update()
    {
        float percent = _associatedTrait.timeRemaining / _associatedTrait.duration;

        timerFill.fillAmount = percent;

        if (_associatedTrait.timeRemaining <= 0f)
            GameObject.Destroy(gameObject);
    }

    private void LateUpdate()
    {
        UpdatePosition();
    }
}
