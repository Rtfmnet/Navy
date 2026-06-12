// Navy.Presentation.Common

using UnityEngine;

namespace Navy.Presentation.Common
{
    /// <summary>
    /// Wrapper around Handheld.Vibrate() for Android.
    /// Respects vibrationEnabled setting.
    /// </summary>
    public sealed class VibrationManager : MonoBehaviour
    {
        private bool _enabled = true;

        public void SetEnabled(bool enabled) => _enabled = enabled;

        /// <summary>Short tap vibration (hit / sunk).</summary>
        public void Vibrate()
        {
            if (!_enabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }
    }
}
