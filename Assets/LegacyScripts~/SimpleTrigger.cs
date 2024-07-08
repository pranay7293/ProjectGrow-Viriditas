using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleTrigger : MonoBehaviour
{
    [SerializeField] private GameObject[] objectsToActivate;
    [SerializeField] private GameObject[] objectsToDeactivate;

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered || !other.CompareTag("Player"))
        {
            return;
        }

        triggered = true;
        foreach (var obj in objectsToActivate)
        {
            obj.SetActive(true);
        }
        foreach (var obj in objectsToDeactivate)
        {
            obj.SetActive(false);
        }
    }
}
