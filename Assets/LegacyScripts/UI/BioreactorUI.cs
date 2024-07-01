using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Tools;
using PlayerData;
using Photon.Pun;

public class BioreactorUI : MonoBehaviour
{
    [Header("Circuit resource settings")]
    [SerializeField] private float dnaConsumed = 1f;
    [SerializeField] private float dnaGrowthProduced = 1f;
    [SerializeField] private float mediaConsumed = 1f;
    [SerializeField] private float mediaGrowthProduced = 1f;
    [SerializeField] private float energyConsumed = 1f;
    [SerializeField] private float energyGrowthProduced = 1f;
    [SerializeField, Range(0, 1)] private float minResourceContributionPercent = 0.2f;

    [Header("References")]
    [SerializeField] private Slider dnaSlider;
    [SerializeField] private TMPro.TMP_Text dnaLabel;
    [SerializeField] private Slider mediaSlider;
    [SerializeField] private TMPro.TMP_Text mediaLabel;
    [SerializeField] private Slider energySlider;
    [SerializeField] private TMPro.TMP_Text energyLabel;
    [SerializeField] private Image progressWheelImage;
    [SerializeField] private TMPro.TMP_Text progressLabel;
    [SerializeField] private Button harvestButton;
    [SerializeField] private GenomeMapColumnUI genomeMapColumn;

    private PlayerData.CircuitResources resources;
    private float complexity = 1;
    private Circuit myCircuit;
    private float growth;

    private void Awake()
    {
        harvestButton.onClick.AddListener(() => HarvestButtonClicked());
    }

    public void HarvestButtonClicked()
    {
        PlayerInventoryCircuits inventory = Karyo_GameCore.Instance.GetLocalPlayerCharacter().GetComponent<PlayerInventoryCircuits>();
        if (inventory == null)
        {
            Debug.LogError("Bioreactor couldn't find PlayerInventoryCircuits component on player.");
            return;
        }
        
        inventory.Add(myCircuit);
        resources = inventory.Resources;

        Karyo_GameCore.Instance.uiManager.CloseOpenWindows();
    }

    private void Update()
    {
        if (resources == null)
        {
            return;
        }

        // Update labels.
        dnaLabel.text = Mathf.Round(resources.Dna).ToString();
        mediaLabel.text = Mathf.Round(resources.Media).ToString();
        energyLabel.text = Mathf.Round(resources.Energy).ToString();
        var donePercent = Mathf.Clamp01(growth / complexity);
        progressWheelImage.fillAmount = donePercent;
        progressLabel.text = $"Growth: {Mathf.Round(donePercent * 100)}%";
        harvestButton.interactable = donePercent >= 1;

        // Abort if we're done producing the circuit.
        if (donePercent >= 1)
        {
            return;
        }

        // Otherwise consume circuit resources to produce growth.
        growth += ConsumeResource(dnaSlider, dnaConsumed, dnaGrowthProduced, () => resources.Dna, v => resources.Dna = v);
        growth += ConsumeResource(mediaSlider, mediaConsumed, mediaGrowthProduced, () => resources.Media, v => resources.Media = v);
        growth += ConsumeResource(energySlider, energyConsumed, energyGrowthProduced, () => resources.Energy, v => resources.Energy = v);
    }

     public void DisplayCircuit(Circuit circuit)
    {
        PlayerInventoryCircuits inventory = Karyo_GameCore.Instance.GetLocalPlayerCharacter().GetComponent<PlayerInventoryCircuits>();
        if (inventory == null)
        {
            Debug.LogError("Bioreactor couldn't find PlayerInventoryCircuits component on player.");
            return;
        }
        
        resources = inventory.Resources;
        
        if (!circuit.IsValid())
            Debug.LogWarning("BioReactor called but passed-in circuit is not valid.");  // TODO - disallow

        myCircuit = circuit;
        this.complexity = circuit.complexity;
        growth = 0;

        genomeMapColumn.InitializeWithCircuit(circuit);

        // Reset all sliders to the minimum when we open the UI.
        dnaSlider.value = dnaSlider.minValue;
        mediaSlider.value = mediaSlider.minValue;
        energySlider.value = energySlider.minValue;
    }

    // Consume a circuit resource and return the amount of growth produced.
    private float ConsumeResource(Slider slider, float consumedFactor, float producedFactor, Func<float> getter, Action<float> setter)
    {
        var resourceValue = getter();

        // TODO: The UI should be updated to show the resource is exhausted.
        if (resourceValue <= 0)
        {
            return 0;
        }

        var t = Mathf.Lerp(minResourceContributionPercent, 1, slider.value);
        var consumed = t * consumedFactor * Time.deltaTime;
        var produced = t * producedFactor * Time.deltaTime;
        setter(resourceValue - consumed);

        return produced;
    }
}
