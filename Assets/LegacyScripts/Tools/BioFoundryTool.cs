using PlayerData;
using UI;
using UnityEngine;

namespace Tools
{
    public class BioFoundryTool : Tool
    {
        [SerializeField] private float timeToDeploy = 2f;

        public override string ToolName => "Biofoundry";
        public override ReticleType ReticleType => ReticleType.FillCircleSmall;

        public override bool CanPlayerRun => false;

        public override float MaxTargetAcquisitionCastDistance => 0; // Don't acquire targets
        public override float MaintainTargetDistance(Entity entity) => 0;

        private PlayerInventoryCircuits _playerInventoryCircuits;

        private bool _deployed = false;
        private float _deployTime = 0;

        public override void EnablePrimary(TargetAcquisition targetAcquisition)
        {
            base.EnablePrimary(targetAcquisition);

            _deployed = false;
            _deployTime = 0;
        }

        public override void UpdatePrimary(TargetAcquisition targetAcquisition, float deltaTime)
        {
            base.UpdatePrimary(targetAcquisition, deltaTime);

            if (_deployed) return;

            _deployTime += deltaTime;
            Karyo_GameCore.Instance.uiManager.ReticleHandler.SetText(TextLocation.Right, "Deploying...");
            Karyo_GameCore.Instance.uiManager.ReticleHandler.SetFillValue(_deployTime / timeToDeploy);

            if (_deployTime >= timeToDeploy)
            {
                _deployed = true;
                Karyo_GameCore.Instance.uiManager.ReticleHandler.SetText(TextLocation.Right, "");
                Karyo_GameCore.Instance.uiManager.DisplayBiofoundryMainMenu(_playerInventoryCircuits);
            }
        }

        public override void DisablePrimary(TargetAcquisition targetAcquisition)
        {
            base.DisablePrimary(targetAcquisition);
            Karyo_GameCore.Instance.uiManager.ReticleHandler.SetText(TextLocation.Right, "");
            Karyo_GameCore.Instance.uiManager.ReticleHandler.SetFillValue(0);
        }

        public override void Equip()
        {
            base.Equip();

            if (_playerInventoryCircuits == null)
                _playerInventoryCircuits = GetComponentInParent<PlayerInventoryCircuits>();

            if (_playerInventoryCircuits == null)
                Debug.LogError("Biofoundry tool could not find `PlayerInventoryCircuits` on player. This is required!");

            Karyo_GameCore.Instance.uiManager.ReticleHandler.SetText(TextLocation.Right, "");
            Karyo_GameCore.Instance.uiManager.ReticleHandler.SetFillValue(0);
        }
    }
}
