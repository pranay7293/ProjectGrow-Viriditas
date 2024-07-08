using PlayerData;
using UI;
using UnityEngine;

namespace Tools
{
    public class SamplerTool : ProximityTool
    {
        public override string ToolName => "Sampler";
        public override ReticleType ReticleType => ReticleType.FillBarTop;

        private const float MinFillTime = .1f;

        protected override bool IsFocusable(Entity entity) => entity && entity.organismDataSheet;

        protected override bool CanApply(Entity entity) => entity && entity.organismDataSheet && !PlayerSampledData.HasSampled(entity.organismDataSheet);

        protected override float GetApplyTime(Entity entity)
        {
            if (entity.organismDataSheet == null)
            {
                Debug.LogError("Tried to get fill time for an entity without an organism data sheet. This should not happen!");
                return MinFillTime;
            }

            return Mathf.Max(entity.organismDataSheet.SampleTime, MinFillTime);
        }

        protected override void CompletedApply(Entity entity)
        {
            PlayerSampledData.SampleOrganism(entity.organismDataSheet);
            UpdateGenomeMapReticle(entity);
            Karyo_GameCore.Instance.uiManager.DisplayTreeofLifeWindow(entity.organismDataSheet, TreeOfLifeUI.TreeOfLifeUIMode.ViewOnlyGenomeMap);
        }

        protected override void UpdateApplying(ReticleHandler reticleHandler, float percentage)
        {
            reticleHandler.SetText(TextLocation.Right, "Mapping genome...");
            reticleHandler.SetText(TextLocation.Bottom, $"  {(int)(percentage * 100)}%");
        }

        public override void EnablePrimary(TargetAcquisition targetAcquisition)
        {
            base.EnablePrimary(targetAcquisition);

            // If already sampled and selecting - show genome map window
            var entity = targetAcquisition.CurrentFocus;
            if (entity && entity.organismDataSheet && PlayerSampledData.HasSampled(entity.organismDataSheet))
                Karyo_GameCore.Instance.uiManager.DisplayTreeofLifeWindow(entity.organismDataSheet, TreeOfLifeUI.TreeOfLifeUIMode.ViewOnlyGenomeMap);
        }

        public override void DisablePrimary(TargetAcquisition targetAcquisition)
        {
            base.DisablePrimary(targetAcquisition);
            Karyo_GameCore.Instance.uiManager.ReticleHandler.SetText(TextLocation.Right, "");
        }

        private void UpdateGenomeMapReticle(Entity focus)
        {
            var reticleHandler = Karyo_GameCore.Instance.uiManager.ReticleHandler;

            if (focus && focus.organismDataSheet)
            {
                // Already sampled
                if (PlayerSampledData.HasSampled(focus.organismDataSheet))
                {
                    reticleHandler.SetFillInactive(true);
                    reticleHandler.SetText(TextLocation.Right, "Genome mapped");
                    reticleHandler.SetText(TextLocation.Bottom, "");
                    return;
                }
            }

            reticleHandler.SetFillInactive(false);
            reticleHandler.SetFillValue(0);
            reticleHandler.SetText(TextLocation.Right, "");
            reticleHandler.SetText(TextLocation.Bottom, ""); // Reset
        }

        public override void OnFocusChanged(Entity newFocus)
        {
            base.OnFocusChanged(newFocus);

            UpdateGenomeMapReticle(newFocus);
        }
    }
}
