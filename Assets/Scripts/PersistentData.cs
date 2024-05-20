using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PersistentData : MonoBehaviour
{
    private Karyo_GameCore core;
    public static PersistentData Instance;
    public GameObject playerStartPoint;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Subscribe to the sceneLoaded event
            SceneManager.sceneLoaded += OnSceneLoaded;

            core = GameObject.FindGameObjectWithTag("GameCore").GetComponent<Karyo_GameCore>();
            if (core == null)
                Debug.LogError(this + " cannot find Game Core.");

        }
        else
            GameObject.Destroy(gameObject);
    }

    public void RememberPlayerStartPoint (GameObject toRemember)
    {
        playerStartPoint = GameObject.Instantiate(toRemember, transform);
    }


    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(WaitOneFrame());
    }

    private IEnumerator WaitOneFrame()
    {
        yield return null;

        core = GameObject.FindGameObjectWithTag("GameCore").GetComponent<Karyo_GameCore>();
        if (core == null)
            Debug.LogError(this + " cannot find Game Core.");

        if (playerStartPoint != null)
            core.player.UseStartPoint(playerStartPoint);
    }



}
