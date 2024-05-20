using UnityEngine;

public class WorldObjectLabel : MonoBehaviour
{
    [SerializeField] private float range = 15f;
    [SerializeField] private string label;
    [SerializeField] private Sprite icon;

    public string LabelText => label;
    public Sprite IconSprite => icon;

    public void Update()
    {
        var dist = Vector3.Distance(Karyo_GameCore.Instance.player.transform.position, transform.position);
        if (dist < range)
        {
            Karyo_GameCore.Instance.uiManager.worldLabelUI.ShowWorldLabel(this);
        }
        else
        {
            Karyo_GameCore.Instance.uiManager.worldLabelUI.HideWorldLabel(this);
        }
    }
}
