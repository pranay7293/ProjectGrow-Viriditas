using System;
using System.Collections.Generic;
using TMPro;
using Tools;
using UnityEngine;

namespace UI
{
    public class CircuitSelectionUI : MonoBehaviour
    {
        [SerializeField] private CircuitButton circuitButtonTemplate;
        [SerializeField] private TMP_Text title;

        private List<CircuitButton> _circuitButtons;

        private Action<Circuit> _onSelect;
        private Action _onCancel;

        private void Awake()
        {
            circuitButtonTemplate.gameObject.SetActive(false);

            _circuitButtons = new List<CircuitButton>();
            EnsureNumButtons(10); // Pre-pool with 10
        }

        private void EnsureNumButtons(int count)
        {
            if (_circuitButtons.Count >= count)
                return;

            var toAdd = count - _circuitButtons.Count;

            for (var i = 0; i < toAdd; ++i)
            {
                var inst = Instantiate(circuitButtonTemplate, circuitButtonTemplate.transform.parent);
                inst.SelectedCircuit += OnCircuitSelected;
                _circuitButtons.Add(inst);
            }
        }

        private void OnCircuitSelected(Circuit circuit)
        {
            if (_onSelect == null)
            {
                Debug.LogError("Tried to select a circuit but nothing was listening. This should not happen!");
                return;
            }

            _onSelect?.Invoke(circuit);

            _onSelect = null;
            _onCancel = null;

            gameObject.SetActive(false);
        }

        public void CloseAndCancelIfOpen()
        {
            _onCancel?.Invoke();

            _onSelect = null;
            _onCancel = null;

            gameObject.SetActive(false);
        }

        public void Show(Circuit currentSelection, Entity currentFocus, Circuit[] circuits, Action<Circuit> onSelect, Action onCancel)
        {
            if (gameObject.activeInHierarchy)
            {
                Debug.LogError("Tried to show selection UI but something was already waiting for input on it. This should not happen!");
                return;
            }

            if (currentFocus && currentFocus.organismDataSheet)
                title.text = $"Select Circuit | {currentFocus.organismDataSheet.nameCommon}";
            else
                title.text = "Select Circuit";

            _onSelect = onSelect;
            _onCancel = onCancel;

            gameObject.SetActive(true);

            EnsureNumButtons(circuits.Length);

            for (var i = 0; i < _circuitButtons.Count; ++i)
            {
                if (i >= circuits.Length)
                {
                    // Disable any buttons outside of size
                    _circuitButtons[i].gameObject.SetActive(false);
                    continue;
                }

                _circuitButtons[i].SetCircuit(circuits[i]);
                _circuitButtons[i].gameObject.SetActive(true);
                _circuitButtons[i].SetIsSelected(circuits[i] == currentSelection);

                if (currentFocus)
                    _circuitButtons[i].SetIsCompatible(circuits[i].IsCompatible(currentFocus));
                else
                    _circuitButtons[i].SetIsCompatible(false);
            }
        }
    }
}
