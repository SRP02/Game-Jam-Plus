using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "audioDatabase", menuName = "Settings/Audio Database")]
public class AudiosDatabase : ScriptableObject
{
    [System.Serializable]
    public class AudioClipInfo
    {
        public AudioClip clip;
        public float volume = 1.0f;
        public float pitch = 1.0f;
        public bool loop = false;
        public bool playOnAwake = false;
        public AudioMixerGroup mixerGroup;
    }

    public List<AudioClipInfo> AudioClips = new List<AudioClipInfo>();
    public AudioMixerGroup DefaultAudioMixer;
}