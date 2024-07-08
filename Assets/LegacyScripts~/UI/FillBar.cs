using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class FillBar : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private float maxFill = 1f;

        [SerializeField] private Color _defaultColor;
        [SerializeField] private Sprite inactiveSprite;

        private Sprite _defaultSprite;
        private bool _inactive = false;

        private void Awake()
        {
            // TODO: If this GO is deactivated trying to get color from image is all black?! Unity....
            // _initialColor = fillImage.color;
            _defaultSprite = fillImage.sprite;
        }

        public void SetPercentage(float percentage)
        {
            if (_inactive) return;

            fillImage.fillAmount = percentage * maxFill;
        }

        public void SetInactive(bool inactive)
        {
            _inactive = inactive;

            if (inactive)
            {
                fillImage.fillAmount = maxFill;
                fillImage.sprite = inactiveSprite;
            }
            else
            {
                fillImage.sprite = _defaultSprite;
            }
        }

        public void SetColor(Color color)
        {
            fillImage.color = color;
        }

        public void ResetColor()
        {
            fillImage.color = _defaultColor;
        }
    }
}
