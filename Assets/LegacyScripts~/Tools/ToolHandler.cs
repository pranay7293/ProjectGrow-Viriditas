using UI;
using UnityEngine;

namespace Tools
{
    [RequireComponent(typeof(TargetAcquisition))]
    public class ToolHandler : MonoBehaviour, IToolHandler
    {
        [SerializeField] private Tool[] tools;
        [SerializeField] private Camera lookCamera;

        private int _currentToolIndex = -1;
        private bool _primaryActive;
        private bool _secondaryActive;

        private TargetAcquisition _targetAcquisition;

        public IToolConstraints GetEquippedToolConstraints()
        {
            if (TryGetCurrentTool(out var tool))
            {
                return tool;
            }
            return null;
        }

        public void ClearTool()
        {
            SelectTool(-1);
        }

        private bool TryGetCurrentTool(out Tool tool)
        {
            if (_currentToolIndex < 0)
            {
                tool = null;
                return false;
            }

            tool = tools[_currentToolIndex];
            return true;
        }

        private void Awake()
        {
            // Default to main camera if not set.
            if (lookCamera == null)
            {
                lookCamera = Camera.main;
            }

            _targetAcquisition = GetComponent<TargetAcquisition>();
            _targetAcquisition.FocusChanged += TargetAcquisitionOnFocusChanged;

            foreach (var tool in tools)
            {
                tool.gameObject.SetActive(false);
                tool.ToolHandler = this;
            }

            // uncomment the following line of code to start with the first tool selected by default 
            // if (tools.Length > 0) SelectTool(0);
        }

        private void TargetAcquisitionOnFocusChanged(Entity newFocus)
        {
            if (TryGetCurrentTool(out var currentTool))
            {
                currentTool.OnFocusChanged(newFocus);
            }
        }

        private void Update()
        {
            UpdateNumSelectionInput();
            UpdateNextPrevSelectionInput();

            if (Karyo_GameCore.Instance.inputManager.ClearTool)
                ClearTool();

            if (TryGetCurrentTool(out var currentTool))
            {
                UpdateActionActivateInput(currentTool);

                if (_primaryActive) currentTool.UpdatePrimary(_targetAcquisition, Time.deltaTime);
                if (_secondaryActive) currentTool.UpdateSecondary(_targetAcquisition, Time.deltaTime);

                // TODO: This assume all target acquisition will be enitites
                _targetAcquisition.UpdateTargeting(lookCamera.transform.position, lookCamera.transform.forward, transform.position, currentTool);

                currentTool.UpdateTool(_targetAcquisition, Time.deltaTime);
            }
            else
            {
                _targetAcquisition.ClearTargeting();
            }
        }

        private void UpdateActionActivateInput(Tool currentTool)
        {
            if (Karyo_GameCore.Instance.inputManager.PlayerPrimaryHeld)
            {
                if (!_primaryActive)
                {
                    // Started action
                    _primaryActive = true;
                    currentTool.EnablePrimary(_targetAcquisition);
                }
            }
            else if (_primaryActive)
            {
                // Stopped action
                _primaryActive = false;
                currentTool.DisablePrimary(_targetAcquisition);
            }

            if (Karyo_GameCore.Instance.inputManager.PlayerSecondaryHeld)
            {
                if (!_secondaryActive)
                {
                    // Started action
                    _secondaryActive = true;
                    currentTool.EnableSecondary(_targetAcquisition);
                }
            }
            else if (_secondaryActive)
            {
                // Stopped action
                _secondaryActive = false;
                currentTool.DisableSecondary(_targetAcquisition);
            }
        }

        private void UpdateNumSelectionInput()
        {
            if (tools.Length == 0) return; // Nothing to do.

            for (var button = 0; button <= 9; ++button)
            {
                if (!Karyo_GameCore.Instance.inputManager.SelectTool(button)) continue;

                var toolIndex = button == 0 ? 9 : button - 1;
                if (toolIndex >= tools.Length) continue;

                SelectTool(toolIndex);
                break;
            }
        }

        private void UpdateNextPrevSelectionInput()
        {
            if (tools.Length == 0) return; // Nothing to do.

            if (Karyo_GameCore.Instance.inputManager.SelectNextTool)
                SelectTool((_currentToolIndex + 1) % tools.Length);
            if (Karyo_GameCore.Instance.inputManager.SelectPrevTool)
                SelectTool((Mathf.Max(_currentToolIndex, 0) + tools.Length - 1) % tools.Length);
        }

        // TODO: Move this out
        private void UpdateUIText()
        {
            if (TryGetCurrentTool(out var currentTool))
                Karyo_GameCore.Instance.uiManager.SetCurrentToolText(currentTool.ToolName);
            else
            {
                Karyo_GameCore.Instance.uiManager.SetCurrentToolText("");
                Karyo_GameCore.Instance.uiManager.ReticleHandler.SetReticleType(ReticleType.None);
            }
        }

        private void SelectTool(int index)
        {
            if (_currentToolIndex == index)
            {
                // Debug.LogWarning($"Tool already set to index: {index}.");
                return;
            }

            if (TryGetCurrentTool(out var currentTool))
            {
                if (_primaryActive) currentTool.DisablePrimary(_targetAcquisition);
                if (_secondaryActive) currentTool.DisableSecondary(_targetAcquisition);

                currentTool.UnEquip();
                currentTool.gameObject.SetActive(false);
            }

            if (index < 0)
            {
                _currentToolIndex = -1;
                UpdateUIText();
                return;
            }

            if (index >= tools.Length)
            {
                Debug.LogError($"Tried to select a tool ot of range of tools index: {index} only have {tools.Length} tools defined.");
                _currentToolIndex = -1;
                UpdateUIText();
                return;
            }

            _currentToolIndex = index;

            var newTool = tools[_currentToolIndex];
            // Debug.Log($"Equipping tool {newTool.ToolName}");

            Karyo_GameCore.Instance.uiManager.ReticleHandler.SetReticleType(newTool.ReticleType);

            newTool.gameObject.SetActive(true);
            newTool.Equip();
            newTool.OnFocusChanged(_targetAcquisition.CurrentFocus);

            UpdateUIText();
        }
    }
}
