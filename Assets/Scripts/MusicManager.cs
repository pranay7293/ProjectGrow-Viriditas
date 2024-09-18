using UnityEngine;

public class MusicManager : MonoBehaviour
{
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private float fadeOutDuration = 1f;
    [SerializeField] private bool playMusicOnStart = false;

    private AudioSource audioSource;

    private void Awake()
    {
        SetupAudioSource();
    }

    private void Start()
    {
        if (playMusicOnStart)
        {
            PlayMusic();
        }
    }

    public void PlayMusic()
    {
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    public void StopMusic()
    {
        StartCoroutine(AudioManager.Instance.FadeOutAudio(audioSource, fadeOutDuration));
    }

    private void SetupAudioSource()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.clip = backgroundMusic;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = 0.5f;
    }
}