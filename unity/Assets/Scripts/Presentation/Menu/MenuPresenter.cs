// Navy.Presentation.Menu

using Navy.Core.Contracts;
using Navy.Infrastructure;
using Navy.Presentation.Common;
using Navy.Presentation.Lobby;
using UnityEngine;

namespace Navy.Presentation.Menu
{
    /// <summary>
    /// Handles nickname validation (FR-CN-01) and navigation from the main menu.
    /// </summary>
    public sealed class MenuPresenter : MonoBehaviour
    {
        [SerializeField] private MenuView       _view;
        [SerializeField] private LobbyPresenter _lobbyPresenter;   // wired in Inspector

        private ISettingsRepository _settings;
        private PanelRouter         _router;

        private void Start()
        {
            _settings = ServiceLocator.Settings;
            _router   = ServiceLocator.Router;

            // Restore saved nickname
            _view.NicknameInput.text = _settings.Nickname;
            _view.NicknameError.gameObject.SetActive(false);

            _view.SaveNicknameButton.onClick.AddListener(OnSaveNickname);
            _view.HostButton.onClick.AddListener(OnHost);
            _view.JoinButton.onClick.AddListener(OnJoin);
            _view.SettingsButton.onClick.AddListener(OnSettings);
        }

        private void OnSaveNickname()
        {
            string nick = _view.NicknameInput.text?.Trim();
            if (!IsNicknameValid(nick))
            {
                _view.NicknameError.gameObject.SetActive(true);
                return;
            }
            _view.NicknameError.gameObject.SetActive(false);
            _settings.Nickname = nick;
            _settings.Save();
        }

        private void OnHost()
        {
            if (!ValidateNickname()) return;
            if (_lobbyPresenter != null) _lobbyPresenter.IsHost = true;
            _router.Show(AppScreen.Lobby);
        }

        private void OnJoin()
        {
            if (!ValidateNickname()) return;
            if (_lobbyPresenter != null) _lobbyPresenter.IsHost = false;
            _router.Show(AppScreen.Lobby);
        }

        private void OnSettings() => _router.Show(AppScreen.Settings);

        private bool ValidateNickname()
        {
            string nick = _settings.Nickname?.Trim();
            bool valid  = IsNicknameValid(nick);
            _view.NicknameError.gameObject.SetActive(!valid);
            return valid;
        }

        // FR-CN-01: 3–16 chars, unicode, not only spaces
        private static bool IsNicknameValid(string nick)
        {
            if (string.IsNullOrWhiteSpace(nick)) return false;
            if (nick.Length < 3 || nick.Length > 16) return false;
            return true;
        }
    }
}
