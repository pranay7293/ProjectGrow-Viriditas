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
        toxinVignetteImage = Karyo_GameCore.Instance.uiManager.toxinVignetteImage;
        foreach (var pass in toxinEffectPassVolume.customPasses)
        {
            if (pass is FullScreenCustomPass)
            {
                toxinEffectFullScreenPass = pass as FullScreenCustomPass;
                toxinEffectFullScreenPass.fullscreenPassMaterial = new Material(toxinEffectFullScreenPass.fullscreenPassMaterial);
            }
        }
    }

    public void SetToxinEffectLevel(float amount)
    {
        amount = Mathf.Clamp01(amount);
        toxinEffectVolume.weight = amount;
        if (toxinVignetteImage != null)
        {
            // TODO: Radial mask.
            var theta = Mathf.Clamp(Mathf.InverseLerp(0.3f, 1, amount), 0, 0.5f);
            toxinVignetteImage.color = new Color(toxinVignetteImage.color.r, toxinVignetteImage.color.g, toxinVignetteImage.color.b, theta);
        }
        if (toxinEffectFullScreenPass != null)
        {
            toxinEffectFullScreenPass.fullscreenPassMaterial.SetFloat("_Amount", amount);
        }
    }
}
