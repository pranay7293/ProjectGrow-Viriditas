using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public enum ReticleType
    {
        None,
        Default,
        FillCircle,
        FillBarTop,
        FillBarBottom,
        FillCircleSmall,
    }

    public enum TextLocation
    {
        Right,
        RightFar,
        Bottom,
        BottomPlayer,
    }

    [Serializable]
    public class ReticleText
    {
        public TextLocation location;
        public TMP_Text text;
    }

    public class ReticleHandler : MonoBehaviour
    {
        [SerializeField] private Image defaultReticle;
        [SerializeField] private FillBar radialReticle;
        [SerializeField] private FillBar fillbarTop;
        [SerializeField] private FillBar fillbarBottom;
        [SerializeField] private FillBar radialReticleSmall;
        [SerializeField] private ReticleText[] textLocations;
        [SerializeField] private CanvasGroup _reticleFadeGroup;

        private FillBar GetFillBar(ReticleType reticleType)
        {
            switch (reticleType)
            {
                case ReticleType.FillCircle: return radialReticle;
                case ReticleType.FillBarTop: return fillbarTop;
                case ReticleType.FillBarBottom: return fillbarBottom;
                case ReticleType.FillCircleSmall: return radialReticleSmall;
            }

            return null;
        }

        [SerializeField] private Color defaultReticleResetColor;

        private ReticleType _currentReticleType;

        private void Awake()
        {
            // _defaultReticleResetColor = defaultReticle.color; // If deactivated this sets to black... Unity is being stupid

            foreach (var text in textLocations)
            {
                text.text.gameObject.SetActive(true);
                text.text.text = "";
            }
        }

        public void SetHasTarget(bool hasTarget)
        {
            // TODO: Animate this
            // TODO: Expose to variables
            _reticleFadeGroup.alpha = hasTarget ? 1 : .25f;
        }

        public void SetReticleType(ReticleType reticleType)
        {
            _currentReticleType = reticleType;

            defaultReticle.gameObject.SetActive(reticleType != ReticleType.None && reticleType != ReticleType.FillCircleSmall); // Center always on, except if no reticle at all
            radialReticle.gameObject.SetActive(reticleType == ReticleType.FillCircle);
            fillbarTop.gameObject.SetActive(reticleType == ReticleType.FillBarTop);
            fillbarBottom.gameObject.SetActive(reticleType == ReticleType.FillBarBottom);
            radialReticleSmall.gameObject.SetActive(reticleType == ReticleType.FillCircleSmall);

            var currentFillBar = GetFillBar(_currentReticleType);
            if (currentFillBar) currentFillBar.SetInactive(false);

            foreach (var text in textLocations)
            {
                text.text.text = "";
            }

            SetFillValue(0);
            SetHasTarget(false);
            ResetColor();
        }

        public void SetColor(Color color)
        {
            defaultReticle.color = color;
            var currentFillBar = GetFillBar(_currentReticleType);
            if (currentFillBar) currentFillBar.SetColor(color);
        }

        public void ResetColor()
        {
            defaultReticle.color = defaultReticleResetColor;
            var currentFillBar = GetFillBar(_currentReticleType);
            if (currentFillBar) currentFillBar.ResetColor();
        }

        public void SetText(TextLocation location, string text)
        {
            foreach (var label in textLocations)
            {
                if (label.location == location)
                {
                    label.text.text = text;
                }
            }
        }

        public void SetFillInactive(bool inactive)
        {
            var currentFillBar = GetFillBar(_currentReticleType);
            if (currentFillBar) currentFillBar.SetInactive(inactive);
        }

        public void SetFillValue(float value)
        {
            var currentFillBar = GetFillBar(_currentReticleType);
            if (currentFillBar) currentFillBar.SetPercentage(value);
        }
    }
}
