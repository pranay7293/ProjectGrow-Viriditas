using UnityEngine;
using com.ootii.Input;

public class KaryoUnityInputSource : UnityInputSource
{
    public override bool IsViewingActivated => !lockedCamera;

    private bool lockedCamera = false;

    private void Update()
    {
        // Looking at something specific - lock camera
        if (lockedCamera)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        // TODO: Implement logic to determine when the camera should be locked
        // For example: lockedCamera = InputManager.Instance.IsInMenu;
        lockedCamera = false;
    }
}