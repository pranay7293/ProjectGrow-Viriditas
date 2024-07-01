using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    [RequireComponent(typeof(Button))]
    public class CircuitButton : MonoBehaviour
    {
        [SerializeField] private TMP_Text circuitText;
        [SerializeField] private Image currentCircuitBG;
        [SerializeField] private Image compatibleCircuit;

        public delegate void OnSelect(Circuit circuit);
        public event OnSelect SelectedCircuit;

        private Circuit _circuit;

        public void SetCircuit(Circuit circuit)
        {
            _circuit = circuit;
            circuitText.text = circuit.TextDescription;
        }

        public void SetIsSelected(bool selected)
        {
            currentCircuitBG.gameObject.SetActive(selected);
        }

        public void SetIsCompatible(bool compatible)
        {
            compatibleCircuit.gameObject.SetActive(compatible);
        }

        public void Select()
        {
            if (_circuit == null)
            {
                Debug.LogError("Tried to select a button with a circuit that was not set... This should not happen!");
                return;
            }

            SelectedCircuit?.Invoke(_circuit);
        }
    }
}
