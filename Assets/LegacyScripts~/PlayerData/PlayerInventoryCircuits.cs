using System.Collections.Generic;
using Tools;
using UnityEngine;

namespace PlayerData
{
    public class PlayerInventoryCircuits : MonoBehaviour
    {
        [SerializeField] private CircuitResources _resources;
        [SerializeField] private List<Circuit> _circuits;

        public Circuit[] GetCircuits() => _circuits.ToArray(); // Creates a copy so that it won't be edited outside
        public CircuitResources Resources => _resources;

        public void Add(Circuit circuit)
        {
            _circuits.Add(circuit);
        }

        public void Remove(Circuit circuit)
        {
            _circuits.Remove(circuit);
        }
    }

    [System.Serializable]
    public class CircuitResources
    {
        public float Dna;
        public float Media;
        public float Energy;
    }
}
