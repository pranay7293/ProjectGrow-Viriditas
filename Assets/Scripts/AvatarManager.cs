using UnityEngine;
using UnityEngine.UI;

public static class AvatarManager
{
    public static void UpdateAvatar(Image avatarImage, Image ringImage, Color characterColor)
    {
        avatarImage.color = characterColor;
        ringImage.color = characterColor;
    }
}