// Navy.Presentation.Game

using Navy.Core.Engine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Navy.Presentation.Game
{
    /// <summary>
    /// Displays the turn countdown timer with color/blink warnings.
    /// FR-GP-05, FR-GP-06.
    /// </summary>
    public sealed class TurnTimerView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _timerText;
        [SerializeField] private Image    _timerBackground;

        [Header("Warning Colors")]
        [SerializeField] private Color _colorNormal = Color.white;
        [SerializeField] private Color _colorYellow = Color.yellow;
        [SerializeField] private Color _colorRed    = Color.red;

        private int     _remainingSeconds;
        private bool    _blinking;
        private float   _blinkTimer;
        private const float BlinkInterval = 0.4f;

        public void UpdateDisplay(int remainingSeconds)
        {
            _remainingSeconds = remainingSeconds;

            int m = remainingSeconds / 60;
            int s = remainingSeconds % 60;
            _timerText.text = $"{m:D2}:{s:D2}";

            UpdateWarningState(remainingSeconds);
        }

        public void UpdateWarningState(int seconds)
        {
            if (seconds <= GameRules.TurnWarningBlinkSec)
            {
                _blinking = true;
                _timerBackground.color = _colorRed;
            }
            else if (seconds <= GameRules.TurnWarningRedSec)
            {
                _blinking = false;
                _timerBackground.color = _colorRed;
            }
            else if (seconds <= GameRules.TurnWarningYellowSec)
            {
                _blinking = false;
                _timerBackground.color = _colorYellow;
            }
            else
            {
                _blinking = false;
                _timerBackground.color = _colorNormal;
            }
        }

        private void Update()
        {
            if (!_blinking) return;
            _blinkTimer += Time.deltaTime;
            if (_blinkTimer >= BlinkInterval)
            {
                _blinkTimer = 0f;
                _timerText.enabled = !_timerText.enabled;
            }
        }

        public void Reset()
        {
            _blinking = false;
            _timerText.enabled = true;
            _timerBackground.color = _colorNormal;
            _timerText.text = "05:00";
        }
    }
}
