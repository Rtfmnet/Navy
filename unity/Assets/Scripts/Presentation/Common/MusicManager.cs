// Navy.Presentation.Common

using Navy.Data.Audio;
using UnityEngine;
using UnityEngine.Audio;

namespace Navy.Presentation.Common
{
    /// <summary>
    /// Manages looping background music. Volume via AudioMixer "Music" group.
    /// </summary>
    public sealed class MusicManager : MonoBehaviour
    {
        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private AudioMixer  _mixer;
        [SerializeField] private AudioCatalog _catalog;

        private const string MixerParamMusic = "MusicVolume";

        private void Start()
        {
            if (_catalog.backgroundMusic != null)
            {
                _musicSource.clip = _catalog.backgroundMusic;
                _musicSource.loop = true;
                _musicSource.Play();
            }
        }

        public void SetVolume(float volume)
        {
            float db = volume <= 0f ? -80f : Mathf.Log10(volume) * 20f;
            _mixer.SetFloat(MixerParamMusic, db);
        }
    }
}
