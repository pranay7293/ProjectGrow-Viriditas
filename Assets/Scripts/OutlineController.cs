using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class OutlineController : MonoBehaviour
{
    public Color outlineColor = new Color(0.05f, 0.53f, 0.97f, 1f); // 0D86F8 in RGB
    public float outlineWidth = 0.005f;

    private Renderer rend;
    private Material outlineMaterial;
    private bool isOutlineVisible = false;

    void Start()
    {
        rend = GetComponent<Renderer>();
        outlineMaterial = new Material(Shader.Find("Custom/OutlineShader"));
        outlineMaterial.SetColor("_OutlineColor", outlineColor);
        outlineMaterial.SetFloat("_OutlineWidth", outlineWidth);
    }

    public void ShowOutline()
    {
        if (!isOutlineVisible)
        {
            Material[] materials = rend.materials;
            System.Array.Resize(ref materials, materials.Length + 1);
            materials[materials.Length - 1] = outlineMaterial;
            rend.materials = materials;
            isOutlineVisible = true;
        }
    }

    public void HideOutline()
    {
        if (isOutlineVisible)
        {
            Material[] materials = rend.materials;
            System.Array.Resize(ref materials, materials.Length - 1);
            rend.materials = materials;
            isOutlineVisible = false;
        }
    }
}