using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterProfileSimple : MonoBehaviour
{
    [SerializeField] private Image avatarRing;
    [SerializeField] private Image characterBackground;
    [SerializeField] private Image avatarSilhouette;
    [SerializeField] private GameObject agentIcon;
    [SerializeField] private GameObject localPlayerIcon;

    public string CharacterName { get; private set; }

    public void SetProfileInfo(string name, Color color, bool isAI, bool isLocalPlayer)
    {
        CharacterName = name;
        characterBackground.color = color == Color.white ? Color.gray : color;
        avatarSilhouette.color = Color.white;
        agentIcon.SetActive(isAI);
        localPlayerIcon.SetActive(isLocalPlayer && !isAI);
    }
}