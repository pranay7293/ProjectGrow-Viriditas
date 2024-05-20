using UI;
using UnityEngine;

namespace Tools
{
    public abstract class Tool : MonoBehaviour, IToolConstraints
    {
        public IToolHandler ToolHandler { get; set; }
        public abstract string ToolName { get; }
        public abstract ReticleType ReticleType { get; }


        private bool _primaryActive;
        private bool _secondaryActive;

        public virtual bool CanPlayerRun => true;

        public abstract float MaxTargetAcquisitionCastDistance { get; }

        public abstract float MaintainTargetDistance(Entity entity);

        public virtual void OnFocusChanged(Entity newFocus) { }

        public virtual void Equip() { }
        public virtual void UnEquip() { }

        public virtual void UpdateTool(TargetAcquisition targetAcquisition, float deltaTime) { }

        public virtual void EnablePrimary(TargetAcquisition targetAcquisition) { }
        public virtual void UpdatePrimary(TargetAcquisition targetAcquisition, float deltaTime) { }
        public virtual void DisablePrimary(TargetAcquisition targetAcquisition) { }

        public virtual void EnableSecondary(TargetAcquisition targetAcquisition) { }
        public virtual void UpdateSecondary(TargetAcquisition targetAcquisition, float deltaTime) { }
        public virtual void DisableSecondary(TargetAcquisition targetAcquisition) { }
    }
}
