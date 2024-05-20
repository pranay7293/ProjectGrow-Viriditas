using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif

namespace Gaia
{
    /// <summary>
    /// Holds all relevant settings and prefab references for setting up lighting in the built-in render pipeline
    /// </summary>
    /// 
    [CreateAssetMenu(menuName = "Procedural Worlds/Gaia/Lighting Preset BuiltIn")]
    public class LightingPresetBuiltIn : ScriptableObject
    {
        public string m_displayName;
        public GameObject m_directionalLightPrefab;
        public GameObject m_globalPostProcessingPrefab;
        public EnvironmentBuiltInURP m_environmentBuiltIn;

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
                GameObject newGO = GameObject.Instantiate(m_directionalLightPrefab, lightingObject.transform);
                newGO.name = newGO.name.Replace("(Clone)", "");
            }
            if (m_globalPostProcessingPrefab!=null)
            {
                GameObject newGO = GameObject.Instantiate(m_globalPostProcessingPrefab, lightingObject.transform);
                newGO.name = newGO.name.Replace("(Clone)", "");
            }
            if (m_environmentBuiltIn != null)
            {
                m_environmentBuiltIn.Apply();
            }
            if (addPPLayerToCam)
            {
#if UNITY_POST_PROCESSING_STACK_V2

                Camera cam = Camera.main;

                GameObject playerObj = GaiaUtils.GetPlayerObject(false);
                if (playerObj != null)
                {
                    Camera playerCam = playerObj.GetComponentInChildren<Camera>();
                    if (playerCam != null)
                    {
                        cam = playerCam;
                    }
                }

                if (cam != null)
                {
                    PostProcessLayer layer = cam.GetComponent<PostProcessLayer>();
                    if (layer == null)
                    {
                        layer = cam.gameObject.AddComponent<PostProcessLayer>();
                    }

                    layer.fog.enabled = true;
                    layer.fog.excludeSkybox = true;
                    layer.antialiasingMode = PostProcessLayer.Antialiasing.TemporalAntialiasing;
                    layer.volumeLayer = 2;
                    layer.volumeTrigger = cam.transform;
                }
                else
                {
                    Debug.LogWarning("Could not find a camera: Post-Processing was NOT added to the camera. You will need to add a post-processing layer component to your camera or re-apply the lighting preset once there is a camera in the scene.");
                }
                
#endif
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

#if UNITY_POST_PROCESSING_STACK_V2
            //Assuming there is a global post processing volume in the scene, we will grab the first occurence and create a prefab from it
            var allPPVolumes = GameObject.FindObjectsByType<PostProcessVolume>(FindObjectsSortMode.None);
            for (int i = 0; i < allPPVolumes.Length; i++)
            {
                PostProcessVolume ppv = allPPVolumes[i];
                if (ppv.isGlobal)
                {
#if UNITY_EDITOR
                    string savePath = currentPath + "/" + this.name + " GlobalPostProcessing.prefab";
                    PrefabUtility.SaveAsPrefabAsset(ppv.gameObject, savePath);
                    m_globalPostProcessingPrefab = (GameObject)AssetDatabase.LoadAssetAtPath(savePath, typeof(GameObject));
#endif
                    break;
                }
            }
#endif

            if (m_environmentBuiltIn == null)
            {
                string savePath = currentPath + "/" + this.name + " Environment.asset";
                EnvironmentBuiltInURP eb = ScriptableObject.CreateInstance<EnvironmentBuiltInURP>();
                AssetDatabase.CreateAsset(eb, currentPath + "/" + this.name + " Environment.asset");
                m_environmentBuiltIn = (EnvironmentBuiltInURP)AssetDatabase.LoadAssetAtPath(savePath, typeof(EnvironmentBuiltInURP));
            }

            m_environmentBuiltIn.IngestFromScene();
#endif
        }
    }
}
