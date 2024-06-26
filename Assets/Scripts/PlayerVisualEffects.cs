using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Systems.Toxin;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

// This class is responsible for applying visual effects based on various
// environmental things happening to the player - toxins, injury, etc.
public class PlayerVisualEffects : MonoBehaviour
{
    [SerializeField] private Volume toxinEffectVolume;
    [SerializeField] private CustomPassVolume toxinEffectPassVolume;

    private UnityEngine.UI.Image toxinVignetteImage;
    private FullScreenCustomPass toxinEffectFullScreenPass;

    private void Awake()
    {
        if (Karyo_GameCore.Instance == null || Karyo_GameCore.Instance.uiManager == null)
        {
            Debug.LogWarning("Karyo_GameCore or UIManager not found. Some visual effects may not work.");
            return;
        }

        toxinVignetteImage = Karyo_GameCore.Instance.uiManager.toxinVignetteImage;
        
        if (toxinEffectPassVolume == null)
        {
            Debug.LogWarning("Toxin Effect Pass Volume not assigned. Some visual effects may not work.");
            return;
        }

        foreach (var pass in toxinEffectPassVolume.customPasses)
        {
            if (pass is FullScreenCustomPass)
            {
                toxinEffectFullScreenPass = pass as FullScreenCustomPass;
                if (toxinEffectFullScreenPass.fullscreenPassMaterial != null)
                {
                    toxinEffectFullScreenPass.fullscreenPassMaterial = new Material(toxinEffectFullScreenPass.fullscreenPassMaterial);
                }
                else
                {
                    Debug.LogWarning("Fullscreen Pass Material is null. Some visual effects may not work.");
                }
            }
        }
    }

    public void SetToxinEffectLevel(float amount)
    {
        amount = Mathf.Clamp01(amount);
        
        if (toxinEffectVolume != null)
        {
            toxinEffectVolume.weight = amount;
        }
        
        if (toxinVignetteImage != null)
        {
            var theta = Mathf.Clamp(Mathf.InverseLerp(0.3f, 1, amount), 0, 0.5f);
            toxinVignetteImage.color = new Color(toxinVignetteImage.color.r, toxinVignetteImage.color.g, toxinVignetteImage.color.b, theta);
        }
        
        if (toxinEffectFullScreenPass != null && toxinEffectFullScreenPass.fullscreenPassMaterial != null)
        {
            toxinEffectFullScreenPass.fullscreenPassMaterial.SetFloat("_Amount", amount);
        }
    }
}
