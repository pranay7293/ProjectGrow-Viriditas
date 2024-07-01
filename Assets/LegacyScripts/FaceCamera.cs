using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FaceCamera : MonoBehaviour
{

    public Camera camera_target;
    public float rotationOffet; // most commonly this would be 180 if the rotation towards the camera is exatly the opposite of what you want
    public float updateFrequency = 0.5f;
    float accumulatedTime;


    private void Awake()
    {
        camera_target = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        if (camera_target == null)
            Debug.LogError("camera not found by FaceCamera script on object "+gameObject.name);
    }

    void Update()
    {
        accumulatedTime += Time.deltaTime;

        if (accumulatedTime > updateFrequency)
        {
            Vector3 oldRotation = transform.rotation.eulerAngles;

            transform.LookAt(camera_target.transform);

            Vector3 newRotation = transform.rotation.eulerAngles;

            newRotation.y += rotationOffet;

            // only rotate around the y axis
            newRotation.x = 0;
            newRotation.z = 0;

            transform.rotation = Quaternion.Euler(newRotation);
        }
        
    }
}
