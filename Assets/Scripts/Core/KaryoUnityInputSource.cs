using UnityEngine;
using com.ootii.Input;

public class KaryoUnityInputSource : UnityInputSource
{
    public override bool IsViewingActivated => !InputManager.Instance.IsInDialogue;

    private void Update()
    {
        if (InputManager.Instance.IsInDialogue)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}