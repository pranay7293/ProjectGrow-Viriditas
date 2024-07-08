using UI;
using UnityEngine;

namespace Tools
{
    public abstract class ProximityTool : Tool
    {
        [SerializeField] private float distanceToBreakSampleNormal = 10f;
        [SerializeField] private float distanceToBreakSampleLarge = 40f;

        private float _fillTime;

        public override float MaxTargetAcquisitionCastDistance => Mathf.Max(distanceToBreakSampleNormal, distanceToBreakSampleLarge) * 10;
        public override float MaintainTargetDistance(Entity entity)
        {
            return entity.IsLarge ? distanceToBreakSampleLarge : distanceToBreakSampleNormal;
        }

        public override void UpdatePrimary(TargetAcquisition targetAcquisition, float deltaTime)
        {
            base.UpdatePrimary(targetAcquisition, deltaTime);

            if (!targetAcquisition.CurrentFocus || !CanApply(targetAcquisition.CurrentFocus)) return;

            var reticleHandler = Karyo_GameCore.Instance.uiManager.ReticleHandler;
            var totalFillTime = GetApplyTime(targetAcquisition.CurrentFocus);

            _fillTime = Mathf.MoveTowards(_fillTime, totalFillTime, deltaTime);
            var a = _fillTime / totalFillTime;
            reticleHandler.SetFillValue(a);
            UpdateApplying(reticleHandler, a);

            if (Mathf.Approximately(_fillTime, totalFillTime))
            {
                CompletedApply(targetAcquisition.CurrentFocus);
            }
        }

        protected abstract bool CanApply(Entity entity);
        protected abstract bool IsFocusable(Entity entity);
        protected abstract float GetApplyTime(Entity entity);
        protected abstract void CompletedApply(Entity entity);
        protected abstract void UpdateApplying(ReticleHandler reticleHandler, float percentage);

        public override void DisablePrimary(TargetAcquisition targetAcquisition)
        {
            base.DisablePrimary(targetAcquisition);

            _fillTime = 0;
            var reticleHandler = Karyo_GameCore.Instance.uiManager.ReticleHandler;

            reticleHandler.SetFillValue(0);
            reticleHandler.SetText(TextLocation.Bottom, "");
        }

        public override void OnFocusChanged(Entity newFocus)
        {
            base.OnFocusChanged(newFocus);

            _fillTime = 0;
            var reticleHandler = Karyo_GameCore.Instance.uiManager.ReticleHandler;
            reticleHandler.SetFillValue(0);
            reticleHandler.SetHasTarget(newFocus && IsFocusable(newFocus));
        }
    }
}
