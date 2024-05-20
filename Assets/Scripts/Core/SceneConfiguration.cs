using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This class serves as a centralized place to store some shared configuration
// that can be tuned on a per-scene basis.
public class SceneConfiguration : MonoBehaviour
{
    [SerializeField] private LayerMask faunaLayerMask;

    // TODO: Ideally these could be derived programmatically, but for now they
    // need to be configured for the scene based on the camera's exposure
    // behavior and potentially other lighting factors.
    [SerializeField] private float bioluminescentIntensityDay = 100f;
    [SerializeField] private float bioluminescentIntensityNight = 2f;
    [SerializeField] private float daylightAmbientMultiplier = 1;
    [SerializeField] private float nightAmbientMultiplier = 5;

    public LayerMask FaunaLayerMask => faunaLayerMask;
    public float BioluminescentIntensityMultiplierDay => bioluminescentIntensityDay;
    public float BioluminescentIntensityMultiplierNight => bioluminescentIntensityNight;
    public float DaylightAmbientMultiplier => daylightAmbientMultiplier;
    public float NightAmbientMultiplier => nightAmbientMultiplier;
}
