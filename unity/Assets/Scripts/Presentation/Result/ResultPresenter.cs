// Navy.Presentation.Result

using System.Text;
using Navy.Core.Contracts;
using Navy.Core.Models;
using Navy.Infrastructure;
using Navy.Presentation.Common;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace Navy.Presentation.Result
{
    /// <summary>
    /// Result screen presenter (FR-EG).
    /// Displays outcome and session statistics.
    /// Handles rematch (FR-EG-03) and session end (FR-EG-04).
    /// </summary>
    public sealed class ResultPresenter : MonoBehaviour
    {
        [SerializeField] private ResultView _view;

        private ISessionService    _session;
        private PanelRouter        _router;
        private GameState          _state;

        private void OnEnable()
        {
            _session = ServiceLocator.Session;
            _router  = ServiceLocator.Router;

            _view.RematchButton.onClick.AddListener(OnRematch);
            _view.MainMenuButton.onClick.AddListener(OnMainMenu);

            _state = _session?.CurrentState;
            RefreshDisplay();
        }

        public void SetState(GameState state)
        {
            _state = state;
            if (gameObject.activeSelf) RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            if (_state == null) return;

            string myUid = _session.LocalUid;
            string outcome;

            if (_state.IsDraw)
                outcome = LocalizationSettings.StringDatabase.GetLocalizedString("Game", "result.draw");
            else if (_state.WinnerUid == myUid)
                outcome = LocalizationSettings.StringDatabase.GetLocalizedString("Game", "result.victory");
            else
                outcome = LocalizationSettings.StringDatabase.GetLocalizedString("Game", "result.defeat");

            _view.OutcomeText.text = outcome;
            _view.StatsText.text   = BuildStatsText();
        }

        private string BuildStatsText()
        {
            if (_state == null) return string.Empty;

            var sb = new StringBuilder();
            AppendPlayerStats(sb, _state.Host);
            sb.AppendLine();
            AppendPlayerStats(sb, _state.Guest);
            return sb.ToString();
        }

        private static void AppendPlayerStats(StringBuilder sb, PlayerState p)
        {
            if (p == null) return;
            sb.AppendLine($"── {p.Nickname} ──");
            sb.AppendLine($"Hits:     {p.Hits}");
            sb.AppendLine($"Misses:   {p.Misses}");
            sb.AppendLine($"Accuracy: {p.Accuracy:F1}%");
            sb.AppendLine($"Sunk:     {p.SunkShips}");
        }

        // ─── Rematch (FR-EG-03) ───────────────────────────────────────────────────

        private void OnRematch()
        {
            // Winner chooses map; at draw → standard flow
            _router.Show(AppScreen.MapSelect);
        }

        // ─── End session (FR-EG-04) ───────────────────────────────────────────────

        private void OnMainMenu()
        {
            _session.LeaveSessionAsync().Forget();
            _router.Show(AppScreen.Menu);
        }
    }
}
