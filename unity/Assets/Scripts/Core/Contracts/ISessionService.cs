// Navy.Core.Contracts
// Pure C# - NO UnityEngine dependency

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Navy.Core.Models;

namespace Navy.Core.Contracts
{
    /// <summary>
    /// Abstraction for all Firebase session operations.
    /// Presenters use this interface; the actual Firebase implementation is in Data layer.
    /// </summary>
    public interface ISessionService
    {
        // ─── Auth ─────────────────────────────────────────────────────────────────
        string LocalUid { get; }

        UniTask<string> SignInAnonymouslyAsync(CancellationToken ct = default);

        // ─── Session lifecycle ────────────────────────────────────────────────────

        /// <summary>Creates a new session and returns the 6-digit code.</summary>
        UniTask<string> CreateSessionAsync(string nickname, CancellationToken ct = default);

        /// <summary>Joins an existing session by code.</summary>
        UniTask JoinSessionAsync(string sessionCode, string nickname, CancellationToken ct = default);

        /// <summary>Leaves (or dissolves) the current session gracefully.</summary>
        UniTask LeaveSessionAsync(CancellationToken ct = default);

        // ─── Setup phase ──────────────────────────────────────────────────────────

        UniTask SubmitMapChoiceAsync(MapType choice, CancellationToken ct = default);
        UniTask CommitBoardAsync(CancellationToken ct = default);

        /// <summary>Host-only: writes resolved map type and advances phase to Setup.</summary>
        UniTask SetMapAndAdvanceToSetupAsync(MapType resolved, CancellationToken ct = default);

        /// <summary>Host-only: when both boards committed, set first turn UID and advance to Playing.</summary>
        UniTask StartGameAsync(string firstTurnUid, CancellationToken ct = default);

        /// <summary>Host-only: writes Finished phase + winner/draw to meta.</summary>
        UniTask FinishGameAsync(string winnerUid, bool isDraw, CancellationToken ct = default);

        // ─── Local state access ──────────────────────────────────────────────────
        GameState CurrentState { get; }
        bool IsHost { get; }

        // ─── Game phase ───────────────────────────────────────────────────────────

        /// <summary>
        /// Shooter writes target cell to /pendingShot. Target client resolves on its own
        /// board and calls <see cref="SubmitShotAsync"/> with the full result.
        /// </summary>
        UniTask SubmitAimAsync(string targetUid, Cell coord, CancellationToken ct = default);

        UniTask SubmitShotAsync(string shooterUid, string targetUid, Cell coord,
            ShotResult result,
            IReadOnlyList<Cell> adjacentMissCells,
            IReadOnlyList<Cell> sunkShipCells,
            CancellationToken ct = default);

        UniTask TransferTurnAsync(string nextTurnUid, CancellationToken ct = default);
        UniTask SurrenderAsync(CancellationToken ct = default);

        // ─── Observables / listeners ──────────────────────────────────────────────

        /// <summary>Fires whenever remote game state changes.</summary>
        event Action<GameState> OnGameStateChanged;

        /// <summary>Fires when the opponent disconnects or reconnects.</summary>
        event Action<bool> OnOpponentConnectionChanged;

        /// <summary>Fires when a new shot has been recorded (for renderers + animations).</summary>
        event Action<ShotRecord> OnShotResolved;

        /// <summary>
        /// Fires on the target client when the shooter submits an aim coord at us.
        /// Target client must resolve the shot on its own Board and call <see cref="SubmitShotAsync"/>.
        /// </summary>
        event Action<Cell> OnAimReceived;
    }
}
