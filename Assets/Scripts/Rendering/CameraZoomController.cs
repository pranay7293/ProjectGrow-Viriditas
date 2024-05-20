using UnityEngine;
using UnityEngine.Rendering;

namespace Rendering
{
    public class CameraZoomController : MonoBehaviour
    {
        [SerializeField] private Camera[] cameras;
        [SerializeField] private float fovLerpSpeed = 10;
        [SerializeField] private Volume cameraZoomVignetteEffect;
        [SerializeField] private CanvasGroup zoomReticleGroup;

        private float _currentFOV;
        private float _defaultFOV;

        void Awake()
        {
            _defaultFOV = cameras.Length == 0 ? 60 : cameras[0].fieldOfView;
            _currentFOV = _defaultFOV;
        }

        public void SetTargetFOV(float fov) => _currentFOV = fov;
        public void ResetTargetFOV() => _currentFOV = _defaultFOV;

        private void Update()
        {
            foreach (var cam in cameras)
            {
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, _currentFOV, Time.deltaTime * fovLerpSpeed);
            }

            cameraZoomVignetteEffect.weight = Mathf.Lerp(cameraZoomVignetteEffect.weight, _currentFOV < 50 ? 1 : 0, Time.deltaTime * 20f);
            cameraZoomVignetteEffect.gameObject.SetActive(cameraZoomVignetteEffect.weight > .1f);
            zoomReticleGroup.alpha = Mathf.Lerp(zoomReticleGroup.alpha, _currentFOV < 50 ? 1 : 0, Time.deltaTime * 20f);
        }
    }
}
