using UI;
using UnityEngine;

namespace Tools
{
    public class TempTraitEditingTool : Tool
    {
        [SerializeField] private float distanceToBreakScanNormal = 10f;
        [SerializeField] private float distanceToBreakScanLarge = 40f;

        public override string ToolName => "DEBUG Trait Editor";
        public override ReticleType ReticleType => ReticleType.Default;

        public override float MaxTargetAcquisitionCastDistance => Mathf.Max(distanceToBreakScanNormal, distanceToBreakScanLarge) * 10;
        public override float MaintainTargetDistance(Entity entity)
        {
            return entity.IsLarge ? distanceToBreakScanLarge : distanceToBreakScanNormal;
        }

        public override void EnablePrimary(TargetAcquisition targetAcquisition)
        {
            base.EnablePrimary(targetAcquisition);

            if (targetAcquisition.CurrentFocus != null)
            {
                Karyo_GameCore.Instance.uiManager.DEBUG_DisplayTraitEditingWindow(targetAcquisition.CurrentFocus);
            }
        }

        public override void OnFocusChanged(Entity newFocus)
        {
            base.OnFocusChanged(newFocus);

            Karyo_GameCore.Instance.uiManager.DEBUG_HideTraitEditingWindow();
        }

        public override void UnEquip()
        {
            base.UnEquip();

            Karyo_GameCore.Instance.uiManager.DEBUG_HideTraitEditingWindow();
        }

        public override void UpdateTool(TargetAcquisition targetAcquisition, float deltaTime)
        {
            // player can press Esc to Deselect the currentTarget
            if (Karyo_GameCore.Instance.inputManager.TargetAcquisitionUnselectTarget)
            {
                Karyo_GameCore.Instance.uiManager.DEBUG_HideTraitEditingWindow();
            }
        }
    }
}
