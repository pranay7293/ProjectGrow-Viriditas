using com.ootii.Cameras;
using PlayerData;
using Rendering;
using UI;
using UnityEngine;

namespace Tools
{
    public class ScannerTool : Tool
    {
        [SerializeField] private float distanceToBreakScanNoZoom = 5f;
        [SerializeField] private float distanceToBreakScanZoomed = 50f;

        [SerializeField] private float targetFillSpeedPerSecondClose = 10f;
        [SerializeField] private float targetFillSpeedPerSecondFar = .2f;
        [SerializeField] private float farDistance = 30f;

        [SerializeField] private float minZoomFOV = 45f;
        [SerializeField] private float maxZoomFOV = 10f;

        [SerializeField] private float minTimeToShowODS = 1f;
        [SerializeField] private float distanceFromScanStartPositionToCloseODS = 5f;

        public override string ToolName => "Scanner";
        public override ReticleType ReticleType => ReticleType.FillCircle;

        private float _fill = 0;
        private bool _isZoomed = false;

        private float _currentZoomFov;
        private Vector3 _scanOpenedDisplayStartPosition;
        private float _timeShowedODS;

        private CameraController _cameraController;

        private void OnEnable()
        {
            // TODO: This is not ideal...
            if (_cameraController == null)
                _cameraController = FindObjectOfType<CameraController>();
        }

        public override float MaxTargetAcquisitionCastDistance => distanceToBreakScanZoomed;
        public override float MaintainTargetDistance(Entity entity)
        {
            return _isZoomed ? distanceToBreakScanZoomed : distanceToBreakScanNoZoom;
        }

        public override void UpdatePrimary(TargetAcquisition targetAcquisition, float deltaTime)
        {
            base.UpdatePrimary(targetAcquisition, deltaTime);

            // TODO: This is not great -- ideally shouldn't be dealing with TargetAcquisition at all...
            if (targetAcquisition.HasFocus)
            {
                var diff = targetAcquisition.CurrentFocus.transform.position - transform.position;
                diff.y = 0;
                var distance = diff.magnitude;
                var fillSpeed = Mathf.Lerp(targetFillSpeedPerSecondClose, targetFillSpeedPerSecondFar, distance / farDistance);
                _fill = Mathf.MoveTowards(_fill, 1, fillSpeed * deltaTime);

                if (Mathf.Approximately(_fill, 1))
                {
                    var dataSheet = targetAcquisition.CurrentFocus.organismDataSheet;

                    int level;

                    if (PlayerScannedData.HasScannedEntity(targetAcquisition.CurrentFocus) || !PlayerScannedData.CanScan(dataSheet))
                    {
                        // Not scannable, just get level
                        level = PlayerScannedData.GetScanLevel(dataSheet);
                    }
                    else
                    {
                        level = PlayerScannedData.UnlockScanForEntity(targetAcquisition.CurrentFocus);
                        UpdateReticleStateForFocus(targetAcquisition.CurrentFocus);
                    }

                    Karyo_GameCore.Instance.uiManager.DisplayTreeofLifeWindow(dataSheet, TreeOfLifeUI.TreeOfLifeUIMode.ViewOnlyODS);
                    _scanOpenedDisplayStartPosition = targetAcquisition.transform.position;
                    _timeShowedODS = Time.time;
                }
            }
        }

        public override void DisablePrimary(TargetAcquisition targetAcquisition)
        {
            base.DisablePrimary(targetAcquisition);
            _fill = 0;
        }

        public override void EnableSecondary(TargetAcquisition targetAcquisition)
        {
            base.EnableSecondary(targetAcquisition);

            _currentZoomFov = minZoomFOV;

            if (_cameraController)
            {
                _cameraController.ActivateMotor(2); // TODO: This is silly - clean it up...
            }

            _isZoomed = true;
        }

        public override void DisableSecondary(TargetAcquisition targetAcquisition)
        {
            base.DisableSecondary(targetAcquisition);

            if (_cameraController)
            {
                _cameraController.ActivateMotor(0); // TODO: This is silly - clean it up...

                if (_cameraController.TryGetComponent<CameraZoomController>(out var zoomController))
                    zoomController.ResetTargetFOV();
            }

            _isZoomed = false;
        }

        public override void UpdateTool(TargetAcquisition targetAcquisition, float deltaTime)
        {
            base.UpdateTool(targetAcquisition, deltaTime);

            var core = Karyo_GameCore.Instance;

            // If we're showing the data sheet, enough time has passed, and have moved far away, close it
            if (core.uiManager.IsShowingTreeOfLife &&
                Time.time - _timeShowedODS > minTimeToShowODS &&
                Vector3.Distance(targetAcquisition.transform.position, _scanOpenedDisplayStartPosition) > distanceFromScanStartPositionToCloseODS)
            {
                core.uiManager.HideOrganismDataSheetUIUnlessManuallyOpened();
            }

            var reticleHandler = core.uiManager.ReticleHandler;
            reticleHandler.SetFillValue(_fill);

            if (targetAcquisition.CurrentFocus)
            {
                reticleHandler.SetText(TextLocation.Bottom, $" {Mathf.RoundToInt(targetAcquisition.FocusDistance)}m");
            }

            if (_isZoomed)
            {
                if (core.inputManager.ScrollUp) _currentZoomFov = Mathf.Max(_currentZoomFov - 5, maxZoomFOV);
                if (core.inputManager.ScrollDown) _currentZoomFov = Mathf.Min(_currentZoomFov + 5, minZoomFOV);

                if (_cameraController && _cameraController.TryGetComponent<CameraZoomController>(out var zoomController))
                    zoomController.SetTargetFOV(_currentZoomFov);
            }
        }

        public override void UnEquip()
        {
            base.UnEquip();

            if (_cameraController && _cameraController.TryGetComponent<CameraZoomController>(out var zoomController))
                zoomController.ResetTargetFOV();
        }

        private string GetScanTargetText(OrganismDataSheet dataSheet) => $"<b>{dataSheet.nameScientific.ToUpper()}</b>\n{dataSheet.nameCommon}";

        private void UpdateReticleStateForFocus(Entity focus)
        {
            var core = Karyo_GameCore.Instance;
            var reticleHandler = core.uiManager.ReticleHandler;

            if (focus && focus.organismDataSheet)
            {
                reticleHandler.SetHasTarget(true);

                var isFullyScanned = !PlayerScannedData.CanScan(focus.organismDataSheet);
                var hasPlayerScanned = PlayerScannedData.HasScannedEntity(focus);
                var currentScanLevel = PlayerScannedData.GetScanLevel(focus.organismDataSheet);

                if (isFullyScanned)
                {
                    // Fully scanned - set to filled and white
                    _fill = 1;
                    reticleHandler.ResetColor();
                    reticleHandler.SetFillInactive(true);
                    reticleHandler.SetText(TextLocation.RightFar, GetScanTargetText(focus.organismDataSheet));
                }
                else if (hasPlayerScanned)
                {
                    // Already scanned this instance of the entity - set filled but grayed out
                    _fill = 1;
                    reticleHandler.SetColor(new Color(.5f, .5f, .5f, .85f));
                    reticleHandler.SetFillInactive(true);
                    reticleHandler.SetText(TextLocation.RightFar, GetScanTargetText(focus.organismDataSheet));
                }
                else
                {
                    // Otherwise -- this is scannable
                    _fill = 0;
                    reticleHandler.ResetColor();

                    var text = currentScanLevel >= 0 ? GetScanTargetText(focus.organismDataSheet) : "Unidentified";
                    reticleHandler.SetText(TextLocation.RightFar, text);
                    reticleHandler.SetFillInactive(false);
                }
            }
            else
            {
                reticleHandler.SetHasTarget(false);

                // No target or data sheet - do nothing
                _fill = 0;
                reticleHandler.ResetColor();
                reticleHandler.SetText(TextLocation.RightFar, "");
                reticleHandler.SetText(TextLocation.Bottom, "");
                reticleHandler.SetFillInactive(false);
            }
        }

        public override void OnFocusChanged(Entity newFocus)
        {
            base.OnFocusChanged(newFocus);
            UpdateReticleStateForFocus(newFocus);
        }
    }
}
