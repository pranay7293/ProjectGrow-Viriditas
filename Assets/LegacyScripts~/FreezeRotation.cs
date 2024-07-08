using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TODO: Understand why we're having issues with Rigidbody's built-in support for this.
public class FreezeRotation : MonoBehaviour
{
    [SerializeField] private bool x;
    [SerializeField] private bool y;
    [SerializeField] private bool z;

    private Vector3 initialEuler;

    private void Awake()
    {
        initialEuler = transform.rotation.eulerAngles;
    }

    private void LateUpdate()
    {
        transform.rotation = Quaternion.Euler(
            x ? initialEuler.x : transform.rotation.eulerAngles.x,
            y ? initialEuler.y : transform.rotation.eulerAngles.y,
            z ? initialEuler.x : transform.rotation.eulerAngles.z
        );
    }
}
