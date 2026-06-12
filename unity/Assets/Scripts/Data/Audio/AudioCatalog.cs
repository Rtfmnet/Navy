// Navy.Data.Audio

using UnityEngine;

namespace Navy.Data.Audio
{
    /// <summary>
    /// ScriptableObject catalogue mapping named sound events to AudioClips.
    /// Create via Assets > Create > Navy > AudioCatalog.
    /// Assign clips in the Inspector on PC #2.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioCatalog", menuName = "Navy/AudioCatalog")]
    public sealed class AudioCatalog : ScriptableObject
    {
        [Header("SFX")]
        public AudioClip shot;
        public AudioClip hit;
        public AudioClip sunk;
        public AudioClip miss;
        public AudioClip timerWarning;
        public AudioClip timerAlert;
        public AudioClip victory;
        public AudioClip defeat;

        [Header("Music")]
        public AudioClip backgroundMusic;
    }
}
