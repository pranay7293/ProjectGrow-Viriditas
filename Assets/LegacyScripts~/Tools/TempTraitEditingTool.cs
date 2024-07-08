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
        }

        public override void OnFocusChanged(Entity newFocus)
        {
            base.OnFocusChanged(newFocus);
        }

        public override void UnEquip()
        {
            base.UnEquip();
        }

        public override void UpdateTool(TargetAcquisition targetAcquisition, float deltaTime)
        {
        }
    }
}