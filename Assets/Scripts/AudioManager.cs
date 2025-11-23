using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    // Singleton instance
    public static AudioManager Main;

    AudiosDatabase AudiosDatabase
    {
        get
        {
            return Resources.Load<AudiosDatabase>("Audios");
        }
    }

    private readonly Dictionary<string, AudioSource> audioSources = new Dictionary<string, AudioSource>();

    void Awake()
    {
        Main = this;

        // Create and configure AudioSource components
        foreach (var clipInfo in AudiosDatabase.AudioClips)
        {
            if (clipInfo.clip == null)
            {
                Debug.LogWarning("AudioManager: Missing AudioClip in AudioClipInfo.");
                continue;
            }

            GameObject audioObject = new GameObject("Audio_" + clipInfo.clip.name);
            audioObject.transform.SetParent(transform);
            AudioSource audioSource = audioObject.AddComponent<AudioSource>();
            audioSource.clip = clipInfo.clip;
            audioSource.volume = clipInfo.volume;
            audioSource.pitch = clipInfo.pitch;
            audioSource.loop = clipInfo.loop;
            audioSource.playOnAwake = clipInfo.playOnAwake;
            audioSource.outputAudioMixerGroup = clipInfo.mixerGroup != null ? clipInfo.mixerGroup : AudiosDatabase.DefaultAudioMixer;

            audioSources[clipInfo.clip.name] = audioSource;

            if (clipInfo.playOnAwake)
            {
                audioSource.Play();
            }
        }
    }

    private void Start()
    {
        AudioSource[] sources = FindObjectsOfType<AudioSource>();

        foreach (var s in sources)
        {
            if (s.outputAudioMixerGroup is null)
            {
                s.outputAudioMixerGroup = AudiosDatabase.DefaultAudioMixer;
            }
        }
        foreach (var source in audioSources.Values)
        {
            if (source.playOnAwake)
                source.Play();
        }
    }

    public void PlaySound(string clipName, float volume = 1.0f, float pitch = 1.0f, bool loop = false)
    {
        if (audioSources.TryGetValue(clipName, out var audioSource))
        {
            audioSource.volume = volume;
            audioSource.pitch = pitch;
            audioSource.loop = loop;
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning("AudioManager: No clip found with the name " + clipName);
        }
    }

    public void PlaySoundSimple(string clipName)
    {
        Debug.Log(AudiosDatabase.AudioClips[0].clip.name);
        PlaySound(clipName);
    }

    public void PauseOrStop(string clipName, bool pause = true)
    {
        if (audioSources.TryGetValue(clipName, out var audioSource))
        {
            if (pause)
            {
                audioSource.Pause();
            }
            else
            {
                audioSource.Stop();
            }
        }
        else
        {
            Debug.LogWarning("AudioManager: No clip found with the name " + clipName);
        }
    }

    public AudioSource GetAudioSourceFromClipName(string clipName)
    {
        AudioSource output = null;

        if (audioSources.TryGetValue(clipName, out var source))
        {
            output = source;
        }
        else
        {
            Debug.LogWarning("AudioManager: No clip found with the name " + clipName);
        }

        return output;
    }

    public bool IsPlaying(string clipName)
    {
        if (audioSources.TryGetValue(clipName, out var audioSource))
        {
            return audioSource.isPlaying;
        }
        else
        {
            Debug.LogWarning("AudioManager: No clip found with the name " + clipName);
            return false;
        }
    }
}