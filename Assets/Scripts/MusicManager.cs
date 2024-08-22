using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MusicManager : MonoBehaviour
{
    public AudioClip backgroundMusic;
    private AudioSource audioSource;
    public float fadeOutDuration = 1f;

    private void Awake()
    {
        Debug.Log("MusicManager Awake called");
        SetupAudioSource();
    }

    private void Start()
    {
        Debug.Log("MusicManager Start called");
        PlayMusic();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void SetupAudioSource()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.Log("AudioSource not found, adding one");
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.clip = backgroundMusic;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = 0.5f;
        Debug.Log($"AudioSource setup complete. Clip assigned: {audioSource.clip != null}");
    }

    private void PlayMusic()
    {
        if (audioSource != null && !audioSource.isPlaying)
        {
            Debug.Log("Attempting to play music");
            audioSource.Play();
            StartCoroutine(CheckAudioPlaying());
        }
        else
        {
            Debug.Log($"AudioSource is null: {audioSource == null}, Is playing: {audioSource?.isPlaying}");
        }
    }

    private IEnumerator CheckAudioPlaying()
    {
        yield return new WaitForSeconds(1f);
        Debug.Log($"Is audio playing after 1 second: {audioSource.isPlaying}");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex != 0)
        {
            StartCoroutine(FadeOutMusic());
        }
    }

    private IEnumerator FadeOutMusic()
    {
        float startVolume = audioSource.volume;

        while (audioSource.volume > 0)
        {
            audioSource.volume -= startVolume * Time.deltaTime / fadeOutDuration;
            yield return null;
        }

        audioSource.Stop();
        Destroy(gameObject);
    }
}