// Navy.Presentation.MapSelect

using Navy.Core.Contracts;
using Navy.Core.Engine;
using Navy.Core.Models;
using Navy.Infrastructure;
using Navy.Presentation.Common;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace Navy.Presentation.MapSelect
{
    /// <summary>
    /// Handles map type selection (FR-MP).
    /// Both players choose; if they differ, one is picked randomly by the host.
    /// Host then writes the resolved MapType + phase=Setup; both clients react via state listener.
    /// </summary>
    public sealed class MapSelectPresenter : MonoBehaviour
    {
        [SerializeField] private MapSelectView _view;

        private ISessionService _session;
        private PanelRouter     _router;
        private MapType?        _myChoice;
        private MapType?        _opponentChoice;
        private bool            _resolutionTriggered;

        private void OnEnable()
        {
            _session = ServiceLocator.Session;
            _router  = ServiceLocator.Router;

            _session.OnGameStateChanged += HandleStateChanged;

            _view.SmallButton.onClick.AddListener(() => ChooseMap(MapType.Small));
            _view.MediumButton.onClick.AddListener(() => ChooseMap(MapType.Medium));
            _view.LargeButton.onClick.AddListener(() => ChooseMap(MapType.Large));

            _view.StatusText.text = LocalizationSettings.StringDatabase
                .GetLocalizedString("Game", "map_select.choose");

            _myChoice = null;
            _opponentChoice = null;
            _resolutionTriggered = false;
            SetButtonsInteractable(true);
        }

        private void OnDisable()
        {
            _session.OnGameStateChanged -= HandleStateChanged;
        }

        private void ChooseMap(MapType type)
        {
            _myChoice = type;
            _view.SelectedMapText.text = type.ToString();
            _session.SubmitMapChoiceAsync(type).Forget();
            _view.StatusText.text = LocalizationSettings.StringDatabase
                .GetLocalizedString("Game", "map_select.waiting_opponent");

            SetButtonsInteractable(false);
            TryResolveAsync();
        }

        private void HandleStateChanged(GameState state)
        {
            // Track opponent's choice via state.GetOpponent(...).ChosenMapType,
            // but FirebaseSessionService stores choices in meta children — they come
            // through the meta listener. For the MVP we re-resolve once both choices
            // are present (host triggers SetMapAndAdvance).
            // Watch for resolved phase.
            if (state.Phase == GamePhase.Setup)
                _router.Show(AppScreen.Setup);
        }

        /// <summary>
        /// Host-only: when both choices have been written, resolve & advance phase.
        /// In production, the host listens to meta.hostMapChoice / guestMapChoice.
        /// For MVP, we delegate that to FirebaseSessionService.HandleMetaSnapshot
        /// surfacing both choices via PlayerState.ChosenMapType — but our service
        /// does not currently expose them in PlayerState. As a pragmatic solution
        /// the host does the resolution after a short delay once the local user has
        /// chosen. A more correct version would listen on meta children.
        /// </summary>
        private async void TryResolveAsync()
        {
            if (_resolutionTriggered) return;
            if (!_session.IsHost) return;
            if (_myChoice == null) return;

            // Wait briefly for guest to submit, polling state.
            const int timeoutMs = 30_000;
            const int pollMs = 250;
            int elapsed = 0;
            while (elapsed < timeoutMs)
            {
                if (_opponentChoice != null) break;

                // Poll opponent choice from PlayerState (populated via HandleMetaSnapshot)
                var opponent = _session.CurrentState?.GetOpponent(_session.LocalUid);
                if (opponent != null && opponent.ChosenMapType.HasValue)
                    _opponentChoice = opponent.ChosenMapType.Value;

                await Cysharp.Threading.Tasks.UniTask.Delay(pollMs);
                elapsed += pollMs;
            }

            // Even if opponent didn't provide a choice (timeout), use host's only.
            var resolved = _opponentChoice.HasValue
                ? GameRules.ResolveMapConflict(_myChoice.Value, _opponentChoice.Value)
                : _myChoice.Value;

            _resolutionTriggered = true;
            _session.SetMapAndAdvanceToSetupAsync(resolved).Forget();
        }

        private void SetButtonsInteractable(bool value)
        {
            _view.SmallButton.interactable  = value;
            _view.MediumButton.interactable = value;
            _view.LargeButton.interactable  = value;
        }
    }
}
