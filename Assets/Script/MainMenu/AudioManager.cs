using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioSource uiSource;
    public AudioSource ambientSource;
    [Header("Default Music")]
    public AudioClip defaultBGM;
    public bool playMusicOnStart = true;

    [Header("Volume")]
    [Range(0, 1)] public float masterVolume = 1f;
    [Range(0, 1)] public float musicVolume = 1f;
    [Range(0, 1)] public float sfxVolume = 1f;
    [Range(0, 1)] public float uiVolume = 1f;
    [Range(0, 1)] public float ambientVolume = 1f;

    Dictionary<string, AudioClip> sfxClips = new();

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        ApplyVolumes();

        if (playMusicOnStart && defaultBGM != null)
        {
            PlayMusic(defaultBGM);
        }
    }


    void ApplyVolumes()
    {
        musicSource.volume = musicVolume * masterVolume;
        sfxSource.volume = sfxVolume * masterVolume;
        uiSource.volume = uiVolume * masterVolume;
        ambientSource.volume = ambientVolume * masterVolume;
    }

    // ===== SET VOLUME =====
    public void SetMaster(float v) { masterVolume = v; ApplyVolumes(); }
    public void SetMusic(float v) { musicVolume = v; ApplyVolumes(); }
    public void SetSFX(float v) { sfxVolume = v; ApplyVolumes(); }
    public void SetUI(float v) { uiVolume = v; ApplyVolumes(); }
    public void SetAmbient(float v) { ambientVolume = v; ApplyVolumes(); }

    // ===== MUSIC =====
    public void PlayMusic(AudioClip clip)
    {
        if (!clip) return;
        musicSource.clip = clip;
        musicSource.Play();
    }

    // ===== UI =====
    public void PlayUIById(string id)
    {
        if (!sfxClips.TryGetValue(id, out var clip)) return;
        uiSource.PlayOneShot(clip, uiVolume * masterVolume);
    }


    // ===== SFX =====
    public void RegisterSFX(string id, AudioClip clip)
    {
        if (!string.IsNullOrEmpty(id) && clip)
            sfxClips[id] = clip;
    }

    public void PlaySFX(string id)
    {
        if (!sfxClips.TryGetValue(id, out var clip)) return;
        sfxSource.PlayOneShot(clip, sfxVolume * masterVolume);
    }
    public void PlaySFX(AudioClip clip, Vector3 pos)
    {
        if (clip == null) return;

        AudioSource.PlayClipAtPoint(
            clip,
            pos,
            sfxVolume * masterVolume
        );
    }

    public void PlayAmbient(AudioClip clip)
    {
        if (!clip) return;
        ambientSource.clip = clip;
        ambientSource.Play();
    }

}
