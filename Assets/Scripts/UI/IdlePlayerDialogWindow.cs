using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdlePlayerDialogWindow : MonoBehaviour
{
    private float originalTimeScale = -1;  // TODO - this is now duplicated in UIManager and maybe should just live in GameCore or something


    private void Awake()
    {
        if (originalTimeScale == -1)
            originalTimeScale = Karyo_GameCore.Instance.DEBUG_TimeScale;
    }


    public void InitializeIdlePlayerDialogWindow()
    {
        Time.timeScale = 0f;
    }


    public void ResumePlay()
    {
        if (originalTimeScale >= 0f)
           Time.timeScale = originalTimeScale;
        gameObject.SetActive(false);
    }

    public void QuitButtonClicked()
    {
        Debug.Log("Quitting...");
        Application.Quit();
    }


}
