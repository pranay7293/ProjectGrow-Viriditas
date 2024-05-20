using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif
#if UPPipeline
using UnityEngine.Rendering.Universal;
#endif

namespace Gaia
{
    /// <summary>
    /// Holds all relevant settings and prefab references for setting up lighting in the built-in render pipeline
    /// </summary>
    /// 
    [CreateAssetMenu(menuName = "Procedural Worlds/Gaia/Lighting Preset HDRP")]
    public class LightingPresetHDRP : ScriptableObject
    {
        public string m_displayName;
        public GameObject m_directionalLightPrefab;
        public GameObject m_environmentPrefab;
        public GameObject m_globalPostProcessingPrefab;
        public GameObject m_worldDensityPrefab;

        public void Apply()
        {
            //Destroy old lighting, if any
            RemoveFromScene();
            GameObject lightingObject = GaiaUtils.GetLightingObject(true);

            //Deactivate any remaining directional lights
            var allLights = GameObject.FindObjectsByType<Light>(FindObjectsSortMode.None);
            for (int i = 0; i < allLights.Length; i++)
            {
                Light light = allLights[i];
                if (light.type == LightType.Directional)
                {
                    light.gameObject.SetActive(false);
                }
            }

            if (m_directionalLightPrefab != null)
            {
                GameObject.Instantiate(m_directionalLightPrefab, lightingObject.transform);
            }
            if (m_environmentPrefab != null)
            { 
                GameObject.Instantiate(m_environmentPrefab, lightingObject.transform);
            }
            if (m_globalPostProcessingPrefab != null)
            {
                GameObject.Instantiate(m_globalPostProcessingPrefab, lightingObject.transform);
            }
            if (m_worldDensityPrefab != null)
            {
                GameObject.Instantiate(m_worldDensityPrefab, lightingObject.transform);
            }
        }

        public void RemoveFromScene()
        {
            GameObject lightingObject = GaiaUtils.GetLightingObject(false);
            if (lightingObject != null)
            {
                if (Application.isPlaying)
                {
                    GameObject.Destroy(lightingObject);
                }
                else
                {
                    GameObject.DestroyImmediate(lightingObject);
                }
            }
        }

        public void IngestFromScene()
        {
#if UNITY_EDITOR
            string currentPath = AssetDatabase.GetAssetPath(this);
            currentPath = currentPath.Substring(0, currentPath.LastIndexOf("/"));
            //Assuming there is a directional light in the scene, we will grab the first occurence and create a prefab from it
            var allLights = GameObject.FindObjectsByType<Light>(FindObjectsSortMode.None);
            for (int i = 0; i < allLights.Length; i++)
            {
                Light light = allLights[i];
                if (light.type == LightType.Directional)
                {

                    string savePath = currentPath + "/" + this.name + " DirectionalLight.prefab";
                    PrefabUtility.SaveAsPrefabAsset(light.gameObject, savePath);
                    m_directionalLightPrefab = (GameObject)AssetDatabase.LoadAssetAtPath(savePath, typeof(GameObject));
                    break;
                }
            }

#if HDPipeline
            //Assuming there is a Density volume controller script in the scene, we will grab the first occurence and create a prefab from it
            HDRPDensityVolumeController dvc = GameObject.FindFirstObjectByType<HDRPDensityVolumeController>();

            if (dvc != null )
            {
                string savePath = currentPath + "/" + this.name + " World Density.prefab";
                PrefabUtility.SaveAsPrefabAsset(dvc.gameObject, savePath);
                m_worldDensityPrefab = (GameObject)AssetDatabase.LoadAssetAtPath(savePath, typeof(GameObject));
            }
            //Assuming there is an environment and a post processing volume in the scene, looking for those to turn them into prefabs
            var allPPVolumes = GameObject.FindObjectsByType<Volume>(FindObjectsSortMode.None);
            for (int i = 0; i < allPPVolumes.Length; i++)
            {
                Volume volume = allPPVolumes[i];
                if (volume.name.Equals(GaiaConstants.HDRPEnvironmentObject))
                {
                    string savePath = currentPath + "/" + this.name + " Environment.prefab";
                    PrefabUtility.SaveAsPrefabAsset(volume.gameObject, savePath);
                    m_environmentPrefab = (GameObject)AssetDatabase.LoadAssetAtPath(savePath, typeof(GameObject));
                }

                if (volume.name.Equals(GaiaConstants.HDRPPostProcessingObject))
                {
                    string savePath = currentPath + "/" + this.name + " Post Processing.prefab";
                    PrefabUtility.SaveAsPrefabAsset(volume.gameObject, savePath);
                    m_globalPostProcessingPrefab = (GameObject)AssetDatabase.LoadAssetAtPath(savePath, typeof(GameObject));
                }
            }
#endif
            EditorUtility.SetDirty(this);
#endif
        }
    }
}
