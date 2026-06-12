// Navy.Data.Firebase
// Full ISessionService implementation using Firebase Realtime Database

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Firebase.Database;
using Navy.Core.Contracts;
using Navy.Core.Engine;
using Navy.Core.Models;
using Navy.Data.Firebase.Dto;
using UnityEngine;

namespace Navy.Data.Firebase
{
    /// <summary>
    /// Implements all multiplayer session logic using Firebase RTDB.
    /// Region: europe-west1 is set in Firebase Console, not in code.
    /// </summary>
    public sealed class FirebaseSessionService : ISessionService
    {
        // ─── Fields ───────────────────────────────────────────────────────────────

        private readonly FirebaseAuthService _auth;
        private DatabaseReference _db;
        private string _sessionCode;
        private GameState _localState;

        // Listeners (kept to unsubscribe on leave)
        private DatabaseReference _sessionRef;
        private EventHandler<ValueChangedEventArgs> _metaListener;
        private EventHandler<ValueChangedEventArgs> _playersListener;
        private EventHandler<ChildChangedEventArgs> _shotListener;
        private EventHandler<ValueChangedEventArgs> _surrenderListener;
        private EventHandler<ValueChangedEventArgs> _pendingShotListener;

        public string LocalUid => _auth.Uid;

        // ─── Events ───────────────────────────────────────────────────────────────

        public event Action<GameState> OnGameStateChanged;
        public event Action<bool> OnOpponentConnectionChanged;
        public event Action<ShotRecord> OnShotResolved;
        public event Action<Cell> OnAimReceived;

        // ─── Constructor ──────────────────────────────────────────────────────────

        public FirebaseSessionService(FirebaseAuthService auth)
        {
            _auth = auth;
        }

        // ─── Auth ─────────────────────────────────────────────────────────────────

        public async UniTask<string> SignInAnonymouslyAsync(CancellationToken ct = default)
        {
            return await _auth.SignInAnonymouslyAsync(ct);
        }

        // ─── Session lifecycle ────────────────────────────────────────────────────

        public async UniTask<string> CreateSessionAsync(string nickname, CancellationToken ct = default)
        {
            _db = FirebaseDatabase.DefaultInstance.RootReference;
            _sessionCode = SessionCodeGenerator.Generate();

            var meta = new SessionDto
            {
                hostUid      = LocalUid,
                guestUid     = null,
                phase        = DtoMapper.ToDto(GamePhase.Lobby),
                mapType      = null,
                isDraw       = false,
                winnerUid    = null,
                createdAtMs  = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            var playerDto = new PlayerDto
            {
                nickname  = nickname,
                connected = true,
                isReady   = false
            };

            // Write meta + own player node
            await _db.Child("sessions").Child(_sessionCode).Child("meta")
                .SetRawJsonValueAsync(JsonUtility.ToJson(meta)).AsUniTask().AttachExternalCancellation(ct);

            await _db.Child("sessions").Child(_sessionCode).Child("players").Child(LocalUid)
                .SetRawJsonValueAsync(JsonUtility.ToJson(playerDto)).AsUniTask().AttachExternalCancellation(ct);

            // onDisconnect: mark connected = false
            await _db.Child("sessions").Child(_sessionCode).Child("players").Child(LocalUid)
                .Child("connected").OnDisconnect().SetValueAsync(false).AsUniTask();

            // Update presence
            await UpdatePresenceAsync(true, ct);

            _localState = new GameState
            {
                SessionId = _sessionCode,
                Phase     = GamePhase.Lobby,
                Host      = new PlayerState { Uid = LocalUid, Nickname = nickname }
            };

            StartListeners();
            return _sessionCode;
        }

        public async UniTask JoinSessionAsync(string sessionCode, string nickname, CancellationToken ct = default)
        {
            _db = FirebaseDatabase.DefaultInstance.RootReference;
            _sessionCode = sessionCode;

            // Verify session exists
            var snap = await _db.Child("sessions").Child(sessionCode).Child("meta").GetValueAsync().AsUniTask().AttachExternalCancellation(ct);
            if (!snap.Exists)
                throw new InvalidOperationException("Session not found.");

            string hostUid = snap.Child("hostUid").Value?.ToString();

            var playerDto = new PlayerDto
            {
                nickname  = nickname,
                connected = true,
                isReady   = false
            };

            // Write guest UID into meta + own player node
            await _db.Child("sessions").Child(sessionCode).Child("meta").Child("guestUid")
                .SetValueAsync(LocalUid).AsUniTask().AttachExternalCancellation(ct);

            await _db.Child("sessions").Child(sessionCode).Child("players").Child(LocalUid)
                .SetRawJsonValueAsync(JsonUtility.ToJson(playerDto)).AsUniTask().AttachExternalCancellation(ct);

            await _db.Child("sessions").Child(sessionCode).Child("players").Child(LocalUid)
                .Child("connected").OnDisconnect().SetValueAsync(false).AsUniTask();

            await UpdatePresenceAsync(true, ct);

            _localState = new GameState
            {
                SessionId = sessionCode,
                Phase     = GamePhase.Lobby,
                Host      = new PlayerState { Uid = hostUid },
                Guest     = new PlayerState { Uid = LocalUid, Nickname = nickname }
            };

            StartListeners();
        }

        public async UniTask LeaveSessionAsync(CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(_sessionCode)) return;
            StopListeners();

            try
            {
                await _db.Child("sessions").Child(_sessionCode).Child("players").Child(LocalUid)
                    .Child("connected").SetValueAsync(false).AsUniTask();
                await UpdatePresenceAsync(false, ct);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[FirebaseSessionService] LeaveSession error: {e.Message}");
            }

            _sessionCode = null;
            _localState  = null;
        }

        // ─── Setup phase ──────────────────────────────────────────────────────────

        public async UniTask SubmitMapChoiceAsync(MapType choice, CancellationToken ct = default)
        {
            bool isHost = _localState?.Host?.Uid == LocalUid;
            string key  = isHost ? "hostMapChoice" : "guestMapChoice";
            await _db.Child("sessions").Child(_sessionCode).Child("meta").Child(key)
                .SetValueAsync(DtoMapper.ToDto(choice)).AsUniTask().AttachExternalCancellation(ct);
        }

        public async UniTask CommitBoardAsync(CancellationToken ct = default)
        {
            await _db.Child("sessions").Child(_sessionCode).Child("players").Child(LocalUid)
                .Child("boardCommitted").SetValueAsync(true).AsUniTask().AttachExternalCancellation(ct);
        }

        public async UniTask SetMapAndAdvanceToSetupAsync(MapType resolved, CancellationToken ct = default)
        {
            if (!IsHost) return;
            var meta = _db.Child("sessions").Child(_sessionCode).Child("meta");
            await meta.Child("mapType").SetValueAsync(DtoMapper.ToDto(resolved)).AsUniTask().AttachExternalCancellation(ct);
            await meta.Child("phase").SetValueAsync(DtoMapper.ToDto(GamePhase.Setup)).AsUniTask().AttachExternalCancellation(ct);
        }

        public async UniTask StartGameAsync(string firstTurnUid, CancellationToken ct = default)
        {
            if (!IsHost) return;
            var meta = _db.Child("sessions").Child(_sessionCode).Child("meta");
            await meta.Child("currentTurnUid").SetValueAsync(firstTurnUid).AsUniTask().AttachExternalCancellation(ct);
            await meta.Child("turnStartedAtMs").SetValueAsync(ServerValue.Timestamp).AsUniTask().AttachExternalCancellation(ct);
            await meta.Child("phase").SetValueAsync(DtoMapper.ToDto(GamePhase.Playing)).AsUniTask().AttachExternalCancellation(ct);
        }

        public async UniTask FinishGameAsync(string winnerUid, bool isDraw, CancellationToken ct = default)
        {
            var meta = _db.Child("sessions").Child(_sessionCode).Child("meta");
            await meta.Child("winnerUid").SetValueAsync(winnerUid).AsUniTask().AttachExternalCancellation(ct);
            await meta.Child("isDraw").SetValueAsync(isDraw).AsUniTask().AttachExternalCancellation(ct);
            await meta.Child("phase").SetValueAsync(DtoMapper.ToDto(GamePhase.Finished)).AsUniTask().AttachExternalCancellation(ct);
        }

        public GameState CurrentState => _localState;
        public bool IsHost => _localState?.Host?.Uid == LocalUid;

        // ─── Game phase ───────────────────────────────────────────────────────────

        public async UniTask SubmitAimAsync(string targetUid, Cell coord, CancellationToken ct = default)
        {
            // Write { targetUid, x, y, shooterUid } to /pendingShot.
            // Target client listens, resolves, and calls SubmitShotAsync.
            var pendingRef = _db.Child("sessions").Child(_sessionCode).Child("pendingShot");
            var json = JsonUtility.ToJson(new PendingShotDto
            {
                shooterUid = LocalUid,
                targetUid  = targetUid,
                x = coord.X,
                y = coord.Y
            });
            await pendingRef.SetRawJsonValueAsync(json).AsUniTask().AttachExternalCancellation(ct);
        }

        // ─── Game phase ───────────────────────────────────────────────────────────

        public async UniTask SubmitShotAsync(
            string shooterUid, string targetUid, Cell coord,
            ShotResult result,
            IReadOnlyList<Cell> adjacentMissCells,
            IReadOnlyList<Cell> sunkShipCells,
            CancellationToken ct = default)
        {
            var shotDto = new ShotDto
            {
                shooterUid       = shooterUid,
                targetUid        = targetUid,
                x                = coord.X,
                y                = coord.Y,
                result           = DtoMapper.ToDto(result),
                adjacentMissCells = DtoMapper.ToDtoList(adjacentMissCells),
                sunkShipCells    = DtoMapper.ToDtoList(sunkShipCells),
                timestampMs      = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            var shotsRef = _db.Child("sessions").Child(_sessionCode).Child("shots").Push();
            await shotsRef.SetRawJsonValueAsync(JsonUtility.ToJson(shotDto)).AsUniTask().AttachExternalCancellation(ct);

            // Clear pendingShot so the listener doesn't re-fire.
            await _db.Child("sessions").Child(_sessionCode).Child("pendingShot")
                .RemoveValueAsync().AsUniTask();

            // Atomically increment hit/miss counter for the SHOOTER (not the local writer).
            string statKey = result == ShotResult.Miss ? "misses" : "hits";
            var statRef = _db.Child("sessions").Child(_sessionCode).Child("players").Child(shooterUid).Child(statKey);
            await statRef.RunTransactionAsync(data =>
            {
                int current = 0;
                if (data?.Value != null && int.TryParse(data.Value.ToString(), out var parsed)) current = parsed;
                data.Value = current + 1;
                return TransactionResult.Success(data);
            }).AsUniTask().AttachExternalCancellation(ct);

            if (result == ShotResult.Sunk)
            {
                var sunkRef = _db.Child("sessions").Child(_sessionCode).Child("players").Child(shooterUid).Child("sunkShipsCount");
                await sunkRef.RunTransactionAsync(data =>
                {
                    int current = 0;
                    if (data?.Value != null && int.TryParse(data.Value.ToString(), out var parsed)) current = parsed;
                    data.Value = current + 1;
                    return TransactionResult.Success(data);
                }).AsUniTask().AttachExternalCancellation(ct);
            }
        }

        public async UniTask TransferTurnAsync(string nextTurnUid, CancellationToken ct = default)
        {
            var metaRef = _db.Child("sessions").Child(_sessionCode).Child("meta");
            await metaRef.RunTransactionAsync(data =>
            {
                if (data == null || !data.HasChildren) return TransactionResult.Abort();
                if (data.Child("currentTurnUid").Value?.ToString() != LocalUid)
                    return TransactionResult.Abort();   // already transferred (race)
                data.Child("currentTurnUid").Value = nextTurnUid;
                data.Child("turnStartedAtMs").Value = ServerValue.Timestamp;
                return TransactionResult.Success(data);
            }).AsUniTask().AttachExternalCancellation(ct);
        }

        public async UniTask SurrenderAsync(CancellationToken ct = default)
        {
            await _db.Child("sessions").Child(_sessionCode).Child("surrender")
                .Child("uid").SetValueAsync(LocalUid).AsUniTask().AttachExternalCancellation(ct);
        }

        // ─── Listeners ────────────────────────────────────────────────────────────

        private void StartListeners()
        {
            _sessionRef = _db.Child("sessions").Child(_sessionCode);

            _metaListener = (_, args) =>
            {
                if (args.DatabaseError != null) return;
                HandleMetaSnapshot(args.Snapshot);
            };

            _playersListener = (_, args) =>
            {
                if (args.DatabaseError != null) return;
                HandlePlayersSnapshot(args.Snapshot);
            };

            _shotListener = (_, args) =>
            {
                if (args.DatabaseError != null) return;
                HandleNewShot(args.Snapshot);
            };

            _surrenderListener = (_, args) =>
            {
                if (args.DatabaseError != null) return;
                HandleSurrender(args.Snapshot);
            };

            _pendingShotListener = (_, args) =>
            {
                if (args.DatabaseError != null) return;
                HandlePendingShot(args.Snapshot);
            };

            _sessionRef.Child("meta").ValueChanged          += _metaListener;
            _sessionRef.Child("players").ValueChanged       += _playersListener;
            _sessionRef.Child("shots").ChildAdded           += _shotListener;
            _sessionRef.Child("surrender").ValueChanged     += _surrenderListener;
            _sessionRef.Child("pendingShot").ValueChanged   += _pendingShotListener;
        }

        private void StopListeners()
        {
            if (_sessionRef == null) return;
            if (_metaListener        != null) _sessionRef.Child("meta").ValueChanged        -= _metaListener;
            if (_playersListener     != null) _sessionRef.Child("players").ValueChanged     -= _playersListener;
            if (_shotListener        != null) _sessionRef.Child("shots").ChildAdded         -= _shotListener;
            if (_surrenderListener   != null) _sessionRef.Child("surrender").ValueChanged   -= _surrenderListener;
            if (_pendingShotListener != null) _sessionRef.Child("pendingShot").ValueChanged -= _pendingShotListener;
        }

        private void HandleSurrender(DataSnapshot snap)
        {
            if (_localState == null) return;
            string surrenderUid = snap.Child("uid").Value?.ToString();
            if (string.IsNullOrEmpty(surrenderUid)) return;

            // The player who surrendered always loses (FR-GP-07).
            string winnerUid = _localState.GetOpponent(surrenderUid)?.Uid;
            _localState.WinnerUid = winnerUid;
            _localState.IsDraw    = false;
            _localState.Phase     = GamePhase.Finished;

            // Only one client should write Finished to RTDB; let the host do it.
            if (IsHost)
                FinishGameAsync(winnerUid, false).Forget();

            OnGameStateChanged?.Invoke(_localState);
        }

        private void HandleMetaSnapshot(DataSnapshot snap)
        {
            if (_localState == null) return;
            try
            {
                _localState.Phase          = DtoMapper.GamePhaseFromDto(snap.Child("phase").Value?.ToString() ?? "Lobby");
                _localState.MapType        = DtoMapper.MapTypeFromDtoNullable(snap.Child("mapType").Value?.ToString()) ?? _localState.MapType;
                _localState.CurrentTurnUid = snap.Child("currentTurnUid").Value?.ToString();
                _localState.WinnerUid      = snap.Child("winnerUid").Value?.ToString();

                // Map choices (per-player)
                var hostMapChoice  = DtoMapper.MapTypeFromDtoNullable(snap.Child("hostMapChoice").Value?.ToString());
                var guestMapChoice = DtoMapper.MapTypeFromDtoNullable(snap.Child("guestMapChoice").Value?.ToString());
                if (_localState.Host != null  && hostMapChoice.HasValue)  _localState.Host.ChosenMapType  = hostMapChoice.Value;
                if (_localState.Guest != null && guestMapChoice.HasValue) _localState.Guest.ChosenMapType = guestMapChoice.Value;

                if (bool.TryParse(snap.Child("isDraw").Value?.ToString(), out bool isDraw))
                    _localState.IsDraw = isDraw;

                if (long.TryParse(snap.Child("turnStartedAtMs").Value?.ToString(), out long ts))
                    _localState.TurnStartedAtMs = ts;

                OnGameStateChanged?.Invoke(_localState);
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseSessionService] HandleMetaSnapshot error: {e.Message}");
            }
        }

        private void HandlePlayersSnapshot(DataSnapshot snap)
        {
            if (_localState == null) return;
            try
            {
                foreach (DataSnapshot playerSnap in snap.Children)
                {
                    string uid = playerSnap.Key;

                    // Auto-create opponent player slot when host first sees the guest
                    var player = _localState.GetPlayer(uid);
                    if (player == null)
                    {
                        player = new PlayerState { Uid = uid };
                        if (_localState.Host == null)       _localState.Host  = player;
                        else if (_localState.Guest == null) _localState.Guest = player;
                    }

                    bool connected = playerSnap.Child("connected").Value as bool? ?? false;
                    if (uid != LocalUid)
                        OnOpponentConnectionChanged?.Invoke(connected);

                    player.Nickname       = playerSnap.Child("nickname").Value?.ToString() ?? player.Nickname;
                    player.IsReady        = playerSnap.Child("isReady").Value as bool? ?? false;
                    player.BoardCommitted = playerSnap.Child("boardCommitted").Value as bool? ?? false;
                    if (int.TryParse(playerSnap.Child("hits").Value?.ToString(), out int h))   player.Hits      = h;
                    if (int.TryParse(playerSnap.Child("misses").Value?.ToString(), out int m)) player.Misses    = m;
                    if (int.TryParse(playerSnap.Child("sunkShipsCount").Value?.ToString(), out int s)) player.SunkShips = s;
                }
                OnGameStateChanged?.Invoke(_localState);
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseSessionService] HandlePlayersSnapshot error: {e.Message}");
            }
        }

        private void HandleNewShot(DataSnapshot snap)
        {
            if (_localState == null) return;
            try
            {
                var dto = JsonUtility.FromJson<ShotDto>(snap.GetRawJsonValue());
                var record = DtoMapper.FromDto(dto);
                _localState.History.Add(record);
                OnShotResolved?.Invoke(record);
                OnGameStateChanged?.Invoke(_localState);
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseSessionService] HandleNewShot error: {e.Message}");
            }
        }

        private void HandlePendingShot(DataSnapshot snap)
        {
            if (_localState == null) return;
            if (!snap.Exists) return;
            try
            {
                var dto = JsonUtility.FromJson<PendingShotDto>(snap.GetRawJsonValue());
                if (dto == null || dto.targetUid != LocalUid) return;   // not aimed at us
                OnAimReceived?.Invoke(new Cell(dto.x, dto.y));
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseSessionService] HandlePendingShot error: {e.Message}");
            }
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private async UniTask UpdatePresenceAsync(bool online, CancellationToken ct)
        {
            var presenceRef = _db.Child("presence").Child(LocalUid);
            await presenceRef.Child("online").SetValueAsync(online).AsUniTask().AttachExternalCancellation(ct);
            await presenceRef.Child("sessionCode").SetValueAsync(online ? _sessionCode : null).AsUniTask().AttachExternalCancellation(ct);

            if (online)
                await presenceRef.Child("online").OnDisconnect().SetValueAsync(false).AsUniTask();
        }
    }
}
