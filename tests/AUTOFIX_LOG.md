# AUTOFIX LOG — Phase 1

Bugs found and fixed in `unity/Assets/Scripts/` during Phase 1 test development.

---

## Fixed

- **[Core/Models/Cell.cs:20]** `Equals(object obj)` signature changed to `Equals(object? obj)` — nullable annotation mismatch caused CS8765 error with `Nullable` enabled. (FR-N/A — nullable correctness)

- **[Core/Models/GameState.cs:10-18]** `SessionId`, `CurrentTurnUid`, `WinnerUid` changed to nullable (`string?`); `Host` and `Guest` changed to `= null!` with non-nullable type — CS8618 errors with `Nullable` enabled. The design intent is that these can be null until populated. (FR-N/A — nullable correctness)

- **[Core/Models/PlayerState.cs:8-10]** `Uid`, `Nickname` changed to `= ""` defaults; `Board` changed to `= null!` — CS8618 errors. (FR-N/A — nullable correctness)

- **[Core/Models/ShotRecord.cs:17,20]** `SunkShipCells` and `AdjacentMissCells` changed to nullable (`IReadOnlyList<Cell>?`) — these are intentionally null for Miss/Hit results. (FR-GP-03 — adjacent miss cells only populated on Sunk)

- **[Core/Models/MapConfig.cs:12]** `Ships` property changed to `= null!` — CS8618 error with Nullable enabled; property is always set in factory methods. (FR-MP — map config factory)

- **[Core/Models/Board.cs:39]** `GetShipAt` return type changed to `Ship?` — the method explicitly returns null when no ship is found; the original `Ship` was not nullable. (FR-N/A — nullable correctness)

- **[Core/Engine/GameRules.cs:108]** `CheckWinCondition` return type changed to `string?` — returns null when game is ongoing (no winner yet). (FR-GP-09)

- **[Core/Engine/GameRules.cs:119]** `DetermineWinnerByHits` return tuple changed to `(string? winnerUid, bool isDraw)` — returns null winnerUid on draw. (FR-GP-10)

- **[Core/Engine/AutoPlacer.cs:25]** `TryPlace` out parameter changed to `out Board?` — set to null on failure path, CS8625 error. (FR-SP-03)

- **[Data/Firebase/Dto/SessionDto.cs]** All string fields changed to nullable (`string?`) — Firebase RTDB fields can be absent/null; proper nullable annotation prevents CS8618. (FR-N/A — nullable correctness)

- **[Data/Firebase/Dto/PlayerDto.cs]** `nickname` and `chosenMapType` changed to `string?` — can be null in RTDB. (FR-N/A — nullable correctness)

- **[Data/Firebase/Dto/ShotDto.cs]** `shooterUid`, `targetUid`, `result` changed to `string?`; `sunkShipCells`, `adjacentMissCells` changed to `List<CellDto>?` — only present on Sunk. (FR-GP-03)

- **[Data/Firebase/Dto/PendingShotDto.cs]** `shooterUid`, `targetUid` changed to `string?`. (FR-N/A — nullable correctness)

- **[Data/Firebase/Dto/BoardDto.cs]** `ships` changed to `List<ShipDto>?`; `orientation` in nested `ShipDto` changed to `string?`. (FR-N/A — nullable correctness)

- **[Data/Firebase/Dto/DtoMapper.cs:19,44,50,64-65]** Method signatures updated for nullable: `MapTypeFromDtoNullable(string? s)`, `ToDtoList(IReadOnlyList<Cell>? cells)` → returns `List<CellDto>?`, `FromDtoList(List<CellDto>? dtos)` — consistent with nullable DTO fields. Null-coalescing added for `dto.shooterUid ?? ""` and `dto.targetUid ?? ""`. (FR-N/A — nullable correctness)

---

## Deferred to Phase 2 / PC2

The following issues exist in untestable layers and cannot be addressed in Phase 1:

- **Presentation/**: All `MonoBehaviour` scripts cannot be compiled or run without Unity Editor.
- **Infrastructure/**: `AppBootstrap.cs`, `ServiceLocator.cs` reference `UnityEngine` and cannot be tested on PC1.
- **Data/Firebase/FirebaseSessionService.cs**: Requires Firebase Unity SDK (not installable on PC1). The implementation of `ISessionService` against real Firebase RTDB is deferred to Phase 2.
- **Data/Firebase/FirebaseAuthService.cs**: Same — requires Firebase Auth SDK.
- **Data/Firebase/FirebaseBootstrap.cs**: Same — requires `FirebaseApp.CheckAndFixDependenciesAsync`.

These will be validated on PC2 with Unity Editor and Firebase Local Emulator in Phase 2.

---

## Disputed

_(None at this time)_
