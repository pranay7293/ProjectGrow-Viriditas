using PlayerData;
using UI;
using UnityEngine;

namespace Tools
{
    public class InjectorTool : ProximityTool
    {
        public override string ToolName => "Injector";
        public override ReticleType ReticleType => ReticleType.FillBarBottom;

        [SerializeField] private GameObject circuitLoadedIndicator;

        private Circuit _currentCircuit;
        private PlayerInventoryCircuits _circuitInventory;

        private const float MIN_FILL_TIME = .1f;

        public override void Equip()
        {
            base.Equip();

            // TODO: This is a little goofy, but should be fine for now
            if (_circuitInventory == null)
                _circuitInventory = GetComponentInParent<PlayerInventoryCircuits>();
        }

        protected override bool CanApply(Entity entity)
        {
            if (_currentCircuit == null) return false;

            return entity && _currentCircuit.IsCompatible(entity);
        }

        protected override bool IsFocusable(Entity entity) => entity && entity.organismDataSheet;

        protected override float GetApplyTime(Entity entity)
        {
            if (entity.organismDataSheet == null)
            {
                Debug.LogError("Tried to get fill time for an entity without an organism data sheet. This should not happen!");
                return MIN_FILL_TIME;
            }

            return Mathf.Max(entity.organismDataSheet.InjectionTime, MIN_FILL_TIME);
        }

        protected override void CompletedApply(Entity entity)
        {
            if (!entity.TryGetComponent<TraitManager>(out var traitManager))
            {
                Debug.LogError($"Tried to apply circuit to entity {entity} that did not have a TraitManager! Will not apply injection.");
                return;
            }

            _currentCircuit.Apply(traitManager);

            _circuitInventory.Remove(_currentCircuit);
            _currentCircuit = null;
            UpdateInjectionText(entity);
            Karyo_GameCore.Instance.uiManager.ReticleHandler.SetText(TextLocation.Bottom, ""); // Clear progress
        }

        protected override void UpdateApplying(ReticleHandler reticleHandler, float percentage)
        {
            reticleHandler.SetText(TextLocation.Bottom, $"  {(int)(percentage * 100)}%");
        }

        private void UpdateInjectionText(Entity entity)
        {
            var reticleHandler = Karyo_GameCore.Instance.uiManager.ReticleHandler;

            if (_currentCircuit != null)
            {
                circuitLoadedIndicator.gameObject.SetActive(true);

                if (entity == null || CanApply(entity))
                {
                    reticleHandler.SetText(TextLocation.Right, _currentCircuit.TextDescription);
                    reticleHandler.SetFillInactive(false);
                    reticleHandler.ResetColor();
                }
                else
                {
                    const string fadedRedColor = "#ccadb6ff"; // TODO: This is a bit silly
                    reticleHandler.SetText(TextLocation.Right, $"<b>Incompatible Target</b>\n\n<color={fadedRedColor}>{_currentCircuit.TextDescription}</color>");
                    reticleHandler.SetColor(new Color(.5f, .5f, .5f, .85f));
                    reticleHandler.SetFillInactive(true);
                }

                reticleHandler.SetText(TextLocation.Bottom, "");

                return;
            }

            circuitLoadedIndicator.gameObject.SetActive(false);

            reticleHandler.SetFillInactive(false);
            reticleHandler.ResetColor();

            if (_circuitInventory.GetCircuits().Length == 0)
                reticleHandler.SetText(TextLocation.Right, "No circuits available");
            else
                reticleHandler.SetText(TextLocation.Right, "No circuit loaded");

            reticleHandler.SetText(TextLocation.Bottom, "");
        }

        public override void EnableSecondary(TargetAcquisition targetAcquisition)
        {
            base.EnableSecondary(targetAcquisition);

            var allCircuits = _circuitInventory.GetCircuits();

            if (allCircuits.Length == 0)
            {
                _currentCircuit = null;
                UpdateInjectionText(targetAcquisition.CurrentFocus);
            }
            else
            {
                Karyo_GameCore.Instance.uiManager.circuitSelectionUI.Show(
                    _currentCircuit,
                    targetAcquisition.CurrentFocus,
                    allCircuits,
                    selection =>
                    {
                        _currentCircuit = selection;
                        UpdateInjectionText(targetAcquisition.CurrentFocus);
                    },
                    () =>
                    {
                        // Do nothing. Keep current if any was set
                    });
            }
        }

        public override void OnFocusChanged(Entity newFocus)
        {
            base.OnFocusChanged(newFocus);
            UpdateInjectionText(newFocus);
        }
    }
}
