// Navy.Presentation.Lobby

using System.Threading;
using Cysharp.Threading.Tasks;
using Navy.Core.Contracts;
using Navy.Core.Models;
using Navy.Infrastructure;
using Navy.Presentation.Common;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace Navy.Presentation.Lobby
{
    /// <summary>
    /// Handles session creation (host) and joining (guest).
    /// FR-CN-02..07: session code, 60-sec timeout, reconnect.
    /// </summary>
    public sealed class LobbyPresenter : MonoBehaviour
    {
        [SerializeField] private LobbyView _view;
        [SerializeField] private bool      _isHost;   // Set via MenuPresenter before showing

        private ISessionService   _session;
        private ISettingsRepository _settings;
        private PanelRouter       _router;
        private CancellationTokenSource _cts;
        private bool _guestJoined;

        public bool IsHost { get => _isHost; set => _isHost = value; }

        private void OnEnable()
        {
            _session  = ServiceLocator.Session;
            _settings = ServiceLocator.Settings;
            _router   = ServiceLocator.Router;
            _cts      = new CancellationTokenSource();
            _guestJoined = false;

            _session.OnGameStateChanged       += HandleStateChanged;
            _session.OnOpponentConnectionChanged += HandleOpponentConnection;

            _view.HostPanel.SetActive(_isHost);
            _view.JoinPanel.SetActive(!_isHost);
            _view.JoinError.gameObject.SetActive(false);
            _view.StatusText.text = LocalizationSettings.StringDatabase.GetLocalizedString("Game", "lobby.waiting");

            _view.CopyCodeButton.onClick.AddListener(OnCopyCode);
            _view.JoinButton.onClick.AddListener(OnJoinAsync);
            _view.BackButton.onClick.AddListener(OnBack);

            if (_isHost)
                CreateSessionAsync().Forget();
        }

        private void OnDisable()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _session.OnGameStateChanged       -= HandleStateChanged;
            _session.OnOpponentConnectionChanged -= HandleOpponentConnection;
        }

        // ─── Host ─────────────────────────────────────────────────────────────────

        private async UniTaskVoid CreateSessionAsync()
        {
            try
            {
                string code = await _session.CreateSessionAsync(_settings.Nickname, _cts.Token);
                _view.SessionCodeText.text = code;

                // FR-CN-05: 60-sec timeout for guest to join
                bool guestJoined = await WaitForGuestAsync(_cts.Token);
                if (!guestJoined)
                {
                    await _session.LeaveSessionAsync();
                    _view.StatusText.text = LocalizationSettings.StringDatabase.GetLocalizedString("Game", "lobby.session_unavailable");
                    return;
                }
            }
            catch (System.OperationCanceledException) { }
            catch (System.Exception e)
            {
                Debug.LogError($"[LobbyPresenter] CreateSession error: {e.Message}");
            }
        }

        private async UniTask<bool> WaitForGuestAsync(CancellationToken ct)
        {
            // Poll _guestJoined flag (set by HandleStateChanged) for up to 60 seconds.
            const int timeoutMs = 60_000;
            const int pollMs    = 250;
            int elapsed = 0;
            try
            {
                while (elapsed < timeoutMs && !ct.IsCancellationRequested)
                {
                    if (_guestJoined) return true;
                    await UniTask.Delay(pollMs, cancellationToken: ct);
                    elapsed += pollMs;
                }
            }
            catch (System.OperationCanceledException) { }
            return _guestJoined;
        }

        // ─── Guest ────────────────────────────────────────────────────────────────

        private async void OnJoinAsync()
        {
            string code = _view.CodeInput.text?.Trim();
            if (string.IsNullOrEmpty(code) || code.Length != 6)
            {
                _view.JoinError.gameObject.SetActive(true);
                return;
            }
            _view.JoinError.gameObject.SetActive(false);
            _view.JoinButton.interactable = false;

            try
            {
                await _session.JoinSessionAsync(code, _settings.Nickname, _cts.Token);
            }
            catch (System.Exception e)
            {
                _view.JoinError.gameObject.SetActive(true);
                _view.JoinError.text = e.Message;
                _view.JoinButton.interactable = true;
            }
        }

        // ─── Event handlers ───────────────────────────────────────────────────────

        private void HandleStateChanged(GameState state)
        {
            if (state.Phase == GamePhase.Lobby && state.Host != null && state.Guest != null)
            {
                _guestJoined = true;
                _view.StatusText.text = LocalizationSettings.StringDatabase
                    .GetLocalizedString("Game", "lobby.connected");
                _router.Show(AppScreen.MapSelect);
            }
        }

        private void HandleOpponentConnection(bool connected)
        {
            // Not critical in lobby — only update status text
            _view.StatusText.text = connected
                ? LocalizationSettings.StringDatabase.GetLocalizedString("Game", "lobby.connected")
                : LocalizationSettings.StringDatabase.GetLocalizedString("Game", "lobby.waiting");
        }

        private void OnCopyCode()
        {
            GUIUtility.systemCopyBuffer = _view.SessionCodeText.text;
        }

        private void OnBack()
        {
            _session.LeaveSessionAsync().Forget();
            _router.Show(AppScreen.Menu);
        }
    }
}
