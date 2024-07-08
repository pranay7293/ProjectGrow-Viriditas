using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimateRotate : MonoBehaviour
{
    public float speed;

    void Update()
    {
        transform.Rotate(new Vector3(0, speed, 0));        
    }
}
