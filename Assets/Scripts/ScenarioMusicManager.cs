using UnityEngine;
using System.Collections;

public class ScenarioMusicManager : MonoBehaviour
{
    [SerializeField] private AudioClip scenarioMusic;
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float fadeOutDuration = 1f;

    private AudioSource audioSource;
    private float gameSessionLength = 900f; // 15 minutes in seconds
    private float firstScenarioTime = 300f; // 5 minutes in seconds
    private float secondScenarioTime = 600f; // 10 minutes in seconds
    private float musicStartOffset = 25f;
    private float votingDuration = 30f;

    private void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = scenarioMusic;
        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.volume = 0f;

        StartCoroutine(ManageScenarioMusic());
    }

    private IEnumerator ManageScenarioMusic()
    {
        yield return new WaitForSeconds(firstScenarioTime - musicStartOffset);
        StartCoroutine(PlayScenarioMusic());

        yield return new WaitForSeconds(secondScenarioTime - firstScenarioTime);
        StartCoroutine(PlayScenarioMusic());
    }

    private IEnumerator PlayScenarioMusic()
    {
        StartCoroutine(FadeInMusic());
        yield return new WaitForSeconds(musicStartOffset + votingDuration);
        StartCoroutine(FadeOutMusic());
    }

    private IEnumerator FadeInMusic()
    {
        audioSource.Play();
        float elapsedTime = 0f;
        while (elapsedTime < fadeInDuration)
        {
            audioSource.volume = Mathf.Lerp(0f, 1f, elapsedTime / fadeInDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        audioSource.volume = 1f;
    }

    private IEnumerator FadeOutMusic()
    {
        float elapsedTime = 0f;
        float startVolume = audioSource.volume;
        while (elapsedTime < fadeOutDuration)
        {
            audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsedTime / fadeOutDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        audioSource.Stop();
        audioSource.volume = 0f;
    }
}