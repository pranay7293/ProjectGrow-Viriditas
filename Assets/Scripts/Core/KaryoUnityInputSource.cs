using UnityEngine;
using com.ootii.Input;

// this is our override of ootii's Camera Controller,
// specifically all we changed was when IsViewingActivated is true (ie - when the player can move the mouse to look around)

public class KaryoUnityInputSource : UnityInputSource
{
    public override bool IsViewingActivated => !lockedCamera;

    private Karyo_GameCore core;
    private bool lockedCamera = false;


    private void Awake ()
    {
        core = GameObject.FindGameObjectWithTag("GameCore").GetComponent<Karyo_GameCore>();
        if (core == null)
            Debug.LogError("KaryoInputSource cannot find Game Core.");

    }

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

        // TODO: Clean this up, particularly once we have states like zoomed in etc
        lockedCamera = core.uiManager.InMenu;
    }


}
