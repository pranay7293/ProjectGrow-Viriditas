using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AnimateColor : MonoBehaviour
{
    [SerializeField] Image targetImage;
    [SerializeField] private Color startColor;
    [SerializeField] private Color endColor;
    [SerializeField] private float speed;

    float r_diff, g_diff, b_diff, a_diff;

    private void Awake()
    {
        r_diff = endColor.r - startColor.r;
        g_diff = endColor.g - startColor.g;
        b_diff = endColor.b - startColor.b;
        a_diff = endColor.a - startColor.a;
    }

    private void Update()
    {
        float scalar = Mathf.Sin(Time.time * speed);         // cycles between -1 and 1
        scalar = (scalar + 1f) / 2f;        // normalize to between 0 and 1

        float new_r = startColor.r + (r_diff * scalar);
        float new_g = startColor.g + (g_diff * scalar);
        float new_b = startColor.b + (b_diff * scalar);
        float new_a = startColor.a + (a_diff * scalar);

        targetImage.color = new Color(new_r, new_g, new_b, new_a);
    }



}
