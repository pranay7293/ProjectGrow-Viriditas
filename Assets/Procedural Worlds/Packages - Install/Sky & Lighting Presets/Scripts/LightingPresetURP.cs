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
    [CreateAssetMenu(menuName = "Procedural Worlds/Gaia/Lighting Preset URP")]
    public class LightingPresetURP : ScriptableObject
    {
        public string m_displayName;
        public GameObject m_directionalLightPrefab;
        public GameObject m_globalPostProcessingPrefab;
        public EnvironmentBuiltInURP m_environment;

        public void Apply(bool addPPLayerToCam = true)
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
            if (m_globalPostProcessingPrefab!=null)
            { 
                GameObject.Instantiate(m_globalPostProcessingPrefab, lightingObject.transform);
            }
            if (m_environment != null)
            {
                m_environment.Apply();
            }
            if (addPPLayerToCam)
            {
#if UPPipeline
                Camera cam = Camera.main;
                if (cam != null)
                {
                    UniversalAdditionalCameraData uacd = cam.GetComponent<UniversalAdditionalCameraData>();
                    if (uacd == null)
                    {
                        uacd = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
                    }
                    if (uacd != null)
                    {
                        uacd.renderPostProcessing = true;
                    }
                }
#endif
            }

            //add the procedural worlds sky elements / controller if PW sky is selected
            if (name.Contains("Procedural Worlds Sky"))
            { 
                
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

#if UPPipeline
            //Assuming there is a global URP post processing volume in the scene, we will grab the first occurence and create a prefab from it
            var allPPVolumes = GameObject.FindObjectsByType<Volume>(FindObjectsSortMode.None);
            for (int i = 0; i < allPPVolumes.Length; i++)
            {
                Volume ppv = allPPVolumes[i];
                if (ppv.isGlobal)
                {
                    string savePath = currentPath + "/" + this.name + " GlobalPostProcessing.prefab";
                    PrefabUtility.SaveAsPrefabAsset(ppv.gameObject, savePath);
                    m_globalPostProcessingPrefab = (GameObject)AssetDatabase.LoadAssetAtPath(savePath, typeof(GameObject));
                    break;
                }
            }
#endif

            if (m_environment == null)
            {
                string savePath = currentPath + "/" + this.name + " Environment.asset";
                EnvironmentBuiltInURP eb = ScriptableObject.CreateInstance<EnvironmentBuiltInURP>();
                AssetDatabase.CreateAsset(eb, currentPath + "/" + this.name + " Environment.asset");
                m_environment = (EnvironmentBuiltInURP)AssetDatabase.LoadAssetAtPath(savePath, typeof(EnvironmentBuiltInURP));
            }

            m_environment.IngestFromScene();
            EditorUtility.SetDirty(this);
#endif
        }
    }
}
