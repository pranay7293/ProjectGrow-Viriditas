using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class WanderPointsVisualizer : MonoBehaviour
{
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, 1f);
        Transform first = null;
        Transform prev = null;
        foreach (Transform t in transform)
        {
            if (first == null)
            {
                first = t;
            }
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(t.position, 1f);
            if (prev != null)
            {
                Gizmos.DrawLine(prev.position, t.position);
            }
            prev = t;
        }
        Gizmos.DrawLine(prev.position, first.position);
    }
}
