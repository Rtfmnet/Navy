// Navy.Presentation.Common

using Navy.Data.Audio;
using UnityEngine;
using UnityEngine.Audio;

namespace Navy.Presentation.Common
{
    /// <summary>
    /// Plays one-shot SFX clips. Volume is controlled via AudioMixer "SFX" group.
    /// </summary>
    public sealed class SoundManager : MonoBehaviour
    {
        [SerializeField] private AudioSource _sfxSource;
        [SerializeField] private AudioMixer  _mixer;
        [SerializeField] private AudioCatalog _catalog;

        private const string MixerParamSfx = "SFXVolume";

        public void PlayShot()    => Play(_catalog.shot);
        public void PlayHit()     => Play(_catalog.hit);
        public void PlaySunk()    => Play(_catalog.sunk);
        public void PlayMiss()    => Play(_catalog.miss);
        public void PlayWarning() => Play(_catalog.timerWarning);
        public void PlayAlert()   => Play(_catalog.timerAlert);
        public void PlayVictory() => Play(_catalog.victory);
        public void PlayDefeat()  => Play(_catalog.defeat);

        /// <summary>volume: 0–1</summary>
        public void SetVolume(float volume)
        {
            // AudioMixer uses decibels; -80 dB = silence
            float db = volume <= 0f ? -80f : Mathf.Log10(volume) * 20f;
            _mixer.SetFloat(MixerParamSfx, db);
        }

        private void Play(AudioClip clip)
        {
            if (clip == null) return;
            _sfxSource.PlayOneShot(clip);
        }
    }
}
