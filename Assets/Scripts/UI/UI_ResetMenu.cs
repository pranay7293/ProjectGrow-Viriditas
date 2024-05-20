using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UI_ResetMenu : MonoBehaviour
{
    private Karyo_GameCore core;

    public string nameOfSceneToLoad;
    public GameObject startPointDesert;
    public GameObject startPointJungle;

    private void Awake()
    {
        core = GameObject.FindGameObjectWithTag("GameCore").GetComponent<Karyo_GameCore>();
        if (core == null)
            Debug.LogError(this + " cannot find Game Core.");

        if (startPointDesert == null)
            Debug.LogError(this + " does not have startPointDesert defined.");

        if (startPointJungle == null)
            Debug.LogError(this + " does not have startPointJungle defined.");
    }


    public void ResetInJungle()
    {
        core.persistentData.RememberPlayerStartPoint(startPointJungle);
        SceneManager.LoadScene(nameOfSceneToLoad);
    }

    public void ResetInDesert()
    {
        core.persistentData.RememberPlayerStartPoint(startPointDesert);
        SceneManager.LoadScene(nameOfSceneToLoad);
    }

    public void ExitMenu()
    {
        gameObject.SetActive(false);
    }

    public void QuitButtonPressed ()
    {
        Debug.Log("Quitting...");
        Application.Quit();
    }



}
