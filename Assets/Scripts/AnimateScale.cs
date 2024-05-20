using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimateScale : MonoBehaviour
{
    public Vector3 scaleMin;
    public Vector3 scaleMax;
    public float speed = 10f;  

    void Update()
    {
        float scalar = Mathf.Sin(Time.time * speed);         // cycles between -1 and 1
        scalar = (scalar + 1f) / 2f;        // normalize to between 0 and 1

        Vector3 scaleDifference = scaleMax - scaleMin;

        Vector3 newScale = scaleMin + (scaleDifference * scalar);

        transform.localScale = newScale;
        
    }
}
