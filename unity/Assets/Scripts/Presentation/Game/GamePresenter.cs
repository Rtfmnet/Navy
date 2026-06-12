// Navy.Presentation.Game

using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Navy.Core.Contracts;
using Navy.Core.Engine;
using Navy.Core.Models;
using Navy.Infrastructure;
using Navy.Presentation.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Settings;

namespace Navy.Presentation.Game
{
    /// <summary>
    /// Core game screen presenter.
    /// Aim-resolve shot model (peer-authoritative, see tech.md §5.2/§5.6):
    ///   1. Shooter taps cell → SubmitAimAsync (writes /pendingShot).
    ///   2. Target client receives OnAimReceived → resolves on its OWN board → SubmitShotAsync.
    ///   3. Both clients receive OnShotResolved → render shot, animate, update stats, transfer turn.
    ///
    /// Handles double-tap (FR-UI-03), turn timer (FR-GP-05/06),
    /// surrender (FR-GP-07), history (FR-GP-08), reconnect overlay (FR-CN-06).
    /// </summary>
    public sealed class GamePresenter : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private GameView _view;

        private ISessionService _session;
        private PanelRouter     _router;
        private GameState       _state;
        private Board           _myBoard;     // local board with my ships (target board for opponent)
        private Board           _mirrorBoard; // empty mirror of opponent's board (shot results only)
        private MapConfig       _config;

        // Double-tap aiming (FR-UI-03)
        private Cell _aimCell;
        private bool _shotInFlight;

        // Turn timer
        private CancellationTokenSource _timerCts;

        // Reconnect
        private CancellationTokenSource _reconnectCts;
        private const int ReconnectTimeoutSec = 60;

        // ─── Enable / Disable ─────────────────────────────────────────────────────

        private void OnEnable()
        {
            _session = ServiceLocator.Session;
            _router  = ServiceLocator.Router;

            _session.OnGameStateChanged          += HandleStateChanged;
            _session.OnOpponentConnectionChanged += HandleOpponentConnection;
            _session.OnAimReceived               += HandleAimReceived;
            _session.OnShotResolved              += HandleShotResolved;

            _view.SurrenderButton.onClick.AddListener(OnSurrenderRequested);
            _view.SurrenderConfirmYes.onClick.AddListener(OnSurrenderConfirmed);
            _view.SurrenderConfirmNo.onClick.AddListener(OnSurrenderCancelled);

            _view.SurrenderConfirmPanel.SetActive(false);
            _view.ReconnectOverlay.SetActive(false);

            // Auto-initialize from session state
            var state = _session.CurrentState;
            if (state != null)
            {
                var config  = GameRules.GetConfig(state.MapType);
                var me      = state.GetPlayer(_session.LocalUid);
                Initialize(state, me?.Board, config);
            }
        }

        private void OnDisable()
        {
            _timerCts?.Cancel();
            _reconnectCts?.Cancel();
            _session.OnGameStateChanged          -= HandleStateChanged;
            _session.OnOpponentConnectionChanged -= HandleOpponentConnection;
            _session.OnAimReceived               -= HandleAimReceived;
            _session.OnShotResolved              -= HandleShotResolved;
        }

        // ─── Initialization ───────────────────────────────────────────────────────

        public void Initialize(GameState state, Board myBoard, MapConfig config)
        {
            _state       = state;
            _myBoard     = myBoard;
            _config      = config;
            _mirrorBoard = new Board(config.BoardSize);

            _view.OpponentBoard.Initialize(config.BoardSize);
            _view.OwnBoardMini.Initialize(config.BoardSize);
            if (myBoard != null) _view.OwnBoardMini.Render(myBoard);
            _view.TimerView.Reset();
        }

        // ─── Double-tap aiming & shooting (FR-UI-03) ─────────────────────────────

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_state == null) return;
            if (_state.CurrentTurnUid != _session.LocalUid) return;
            if (_shotInFlight) return;

            var cell = _view.OpponentBoard.GetCellAt(eventData.position);
            if (cell == null) return;
            if (ShotResolver.IsCellAlreadyShot(_mirrorBoard, cell)) return;

            if (_aimCell != null && _aimCell.Equals(cell))
            {
                // Second tap on same cell → fire
                _view.OpponentBoard.ClearAim();
                _aimCell = null;
                FireAtAsync(cell).Forget();
            }
            else
            {
                // First tap → set aim
                _aimCell = cell;
                _view.OpponentBoard.SetAimCell(cell);
                _view.OpponentBoard.Render(_mirrorBoard);
            }
        }

        // ─── Aim → server → target resolves → both receive ShotRecord ─────────────

        private async UniTaskVoid FireAtAsync(Cell cell)
        {
            _shotInFlight = true;
            var opponent = _state.GetOpponent(_session.LocalUid);
            if (opponent == null) { _shotInFlight = false; return; }

            // Pre-shot animation cue (just a short delay; the visible animation is on result)
            ServiceLocator.Sound?.PlayShot();

            await _session.SubmitAimAsync(opponent.Uid, cell);
            // Resolution arrives via HandleShotResolved on both clients.
        }

        /// <summary>Target side: resolve on own board and report back.</summary>
        private void HandleAimReceived(Cell coord)
        {
            if (_myBoard == null) return;

            var result = ShotResolver.Resolve(_myBoard, coord, out var adjacentCells);
            List<Cell> sunkCells = null;
            if (result == ShotResult.Sunk)
            {
                var ship = _myBoard.GetShipAt(coord);
                if (ship != null) sunkCells = new List<Cell>(ship.GetCells());
            }

            // The shooter UID = opponent of local player.
            string shooterUid = _state.GetOpponent(_session.LocalUid)?.Uid;
            _session.SubmitShotAsync(
                shooterUid,
                _session.LocalUid,
                coord,
                result,
                adjacentCells,
                sunkCells).Forget();

            // Update own mini-map immediately
            _view.OwnBoardMini.Render(_myBoard);
        }

        /// <summary>Both sides: a shot has been finalised; render & advance.</summary>
        private void HandleShotResolved(ShotRecord record)
        {
            _shotInFlight = false;
            bool iAmShooter = record.ShooterUid == _session.LocalUid;

            // Update mirror / own board cell states
            if (iAmShooter)
            {
                ApplyResultToBoard(_mirrorBoard, record);
                _view.OpponentBoard.Render(_mirrorBoard);
            }
            else
            {
                _view.OwnBoardMini.Render(_myBoard);
            }

            // History (newest at top)
            string shooterNick = _state.GetPlayer(record.ShooterUid)?.Nickname ?? "?";
            _view.HistoryPanel.AddEntry(shooterNick, record.Coordinate, record.Result);

            // Sound + vibration
            var sound = ServiceLocator.Sound;
            if (record.Result == ShotResult.Sunk)      sound?.PlaySunk();
            else if (record.Result == ShotResult.Hit)  sound?.PlayHit();
            else                                       sound?.PlayMiss();

            if (record.Result != ShotResult.Miss)
                ServiceLocator.Vibration?.Vibrate();

            // Animation
            var animator = GetComponent<ShotAnimator>();
            if (animator != null) animator.PlayAsync(record.Result, Vector3.zero).Forget();

            // Win check (only the shooter, who has authoritative knowledge of opponent ships
            // via the running mirror, can detect it; but the target also knows from own board.
            // Use the target side as authority since they know their own ships exactly.)
            if (!iAmShooter)
            {
                if (_myBoard != null && _myBoard.AliveShipsCount() == 0)
                {
                    // I'm the loser: target whose board emptied. Host writes Finished.
                    if (_session.IsHost)
                        _session.FinishGameAsync(record.ShooterUid, false).Forget();
                    return;
                }
            }

            // Turn transfer is decided by the SHOOTER (FR-GP-02): continue on Hit/Sunk, transfer on Miss.
            if (iAmShooter)
            {
                if (record.Result == ShotResult.Miss)
                {
                    string opponentUid = _state.GetOpponent(_session.LocalUid)?.Uid;
                    _session.TransferTurnAsync(opponentUid).Forget();
                }
                else
                {
                    RestartTurnTimer();
                }
            }
        }

        private static void ApplyResultToBoard(Board board, ShotRecord record)
        {
            if (board == null || record == null) return;
            var coord = record.Coordinate;
            switch (record.Result)
            {
                case ShotResult.Miss:
                    board.SetCell(coord, CellState.Miss);
                    break;
                case ShotResult.Hit:
                    board.SetCell(coord, CellState.Hit);
                    break;
                case ShotResult.Sunk:
                    if (record.SunkShipCells != null)
                        foreach (var c in record.SunkShipCells)
                            board.SetCell(c, CellState.Hit);
                    if (record.AdjacentMissCells != null)
                        foreach (var c in record.AdjacentMissCells)
                            board.SetCell(c, CellState.Adjacent);
                    break;
            }
        }

        // ─── Turn timer ───────────────────────────────────────────────────────────

        private void RestartTurnTimer()
        {
            _timerCts?.Cancel();
            _timerCts = new CancellationTokenSource();
            RunTurnTimerAsync(_timerCts.Token).Forget();
        }

        private async UniTaskVoid RunTurnTimerAsync(CancellationToken ct)
        {
            bool isMyTurn = _state?.CurrentTurnUid == _session.LocalUid;
            int remaining = GameRules.TurnTimerSeconds;

            while (remaining > 0 && !ct.IsCancellationRequested)
            {
                _view.TimerView.UpdateDisplay(remaining);

                // Warnings
                var sound = ServiceLocator.Sound;
                if (remaining == GameRules.TurnWarningYellowSec) sound?.PlayWarning();
                if (remaining == GameRules.TurnWarningRedSec)    sound?.PlayAlert();
                if (remaining == GameRules.TurnWarningBlinkSec)  ServiceLocator.Vibration?.Vibrate();

                await UniTask.Delay(1000, cancellationToken: ct);
                remaining--;
            }

            if (ct.IsCancellationRequested) return;

            // Timer expired — auto-skip turn if it's ours
            if (isMyTurn && _state?.CurrentTurnUid == _session.LocalUid)
                await _session.TransferTurnAsync(_state.GetOpponent(_session.LocalUid)?.Uid);
        }

        // ─── Surrender (FR-GP-07) ─────────────────────────────────────────────────

        private void OnSurrenderRequested()  => _view.SurrenderConfirmPanel.SetActive(true);
        private void OnSurrenderCancelled()  => _view.SurrenderConfirmPanel.SetActive(false);

        private void OnSurrenderConfirmed()
        {
            _view.SurrenderConfirmPanel.SetActive(false);
            _session.SurrenderAsync().Forget();
        }

        // ─── State / Connection handlers ──────────────────────────────────────────

        private void HandleStateChanged(GameState state)
        {
            _state = state;

            if (state.Phase == GamePhase.Finished)
            {
                _timerCts?.Cancel();
                _router.Show(AppScreen.Result);
                return;
            }

            bool myTurn = state.CurrentTurnUid == _session.LocalUid;

            _view.TurnIndicatorText.text = myTurn
                ? LocalizationSettings.StringDatabase.GetLocalizedString("Game", "game.your_turn")
                : LocalizationSettings.StringDatabase.GetLocalizedString("Game", "game.opponent_turn");

            if (_mirrorBoard != null) _view.OpponentBoard.Render(_mirrorBoard);
            if (_myBoard     != null) _view.OwnBoardMini.Render(_myBoard);

            // Restart timer on new turnStartedAtMs
            RestartTurnTimer();
        }

        private void HandleOpponentConnection(bool connected)
        {
            if (!connected)
            {
                _view.ReconnectOverlay.SetActive(true);
                _reconnectCts?.Cancel();
                _reconnectCts = new CancellationTokenSource();
                RunReconnectTimeoutAsync(_reconnectCts.Token).Forget();
            }
            else
            {
                _reconnectCts?.Cancel();
                _view.ReconnectOverlay.SetActive(false);
            }
        }

        private async UniTaskVoid RunReconnectTimeoutAsync(CancellationToken ct)
        {
            int remaining = ReconnectTimeoutSec;
            while (remaining > 0 && !ct.IsCancellationRequested)
            {
                _view.ReconnectCountdownText.text = remaining.ToString();
                await UniTask.Delay(1000, cancellationToken: ct);
                remaining--;
            }
            if (ct.IsCancellationRequested) return;

            // FR-GP-10: early exit, determine winner by hits.
            var (winnerUid, isDraw) = GameRules.DetermineWinnerByHits(_state);
            await _session.FinishGameAsync(winnerUid, isDraw);
            _state.WinnerUid = winnerUid;
            _state.IsDraw    = isDraw;
            _state.Phase     = GamePhase.Finished;
            _router.Show(AppScreen.Result);
        }
    }
}
