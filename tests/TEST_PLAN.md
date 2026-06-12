# Test Plan — Navy (Battleship) Phase 1

Generated from `req/functional.md` v1.0 and `req/tech.md` v1.0.

---

## 1. Scope

| Layer | Project | Status |
|---|---|---|
| Core/Models | Navy.Core.Tests / Models/ | In scope |
| Core/Engine | Navy.Core.Tests / Engine/ | In scope |
| Data/Firebase/Dto | Navy.Data.Dto.Tests | In scope |
| ISessionService flows | Navy.Session.FlowTests | In scope (via FakeInMemorySessionService) |
| Presentation / Infrastructure | — | OUT OF SCOPE (Unity-only, Phase 2) |
| FirebaseSessionService / AuthService | — | OUT OF SCOPE (Firebase SDK, Phase 2) |

---

## 2. FR → Test Mapping

### FR-CN (Connection)

| FR | Scenario | Test class | Test method |
|---|---|---|---|
| FR-CN-02 | Host creates session, gets 6-digit code | LobbyFlowTests | CreateSession_ReturnsSessionCode |
| FR-CN-03 | Guest joins by code | LobbyFlowTests | JoinSession_WithValidCode_Succeeds |
| FR-CN-04 | Both see connected status | LobbyFlowTests | BothSeeConnectedStatus |
| FR-CN-05 | Guest 60s timeout | ConnectionFlowTests | GuestTimeout_60s_SessionUnavailable |
| FR-CN-06 | Reconnect 60s window | ConnectionFlowTests | Reconnect_Within60s_Succeeds |
| FR-CN-06 | Reconnect timeout > 60s | ConnectionFlowTests | Reconnect_Timeout_EarlyExit |
| FR-CN-07 | App background = leave | ConnectionFlowTests | BackgroundLeave_TerminatesSession |

### FR-MP (Map Selection)

| FR | Scenario | Test class | Test method |
|---|---|---|---|
| FR-MP | Same choice → that map | MapSelectFlowTests | BothChooseSame_ResolvesToSame |
| FR-MP | Different choice → random one of two | MapSelectFlowTests | DifferentChoices_PicksOneOfTwo |
| FR-MP | GameRules.ResolveMapConflict same | GameRulesTests | ResolveMapConflict_SameChoice |
| FR-MP | GameRules.ResolveMapConflict different | GameRulesTests | ResolveMapConflict_DifferentChoices_RandomlyPicksOne |

### FR-SP (Ship Placement)

| FR | Scenario | Test class | Test method |
|---|---|---|---|
| FR-SP-02 | Out-of-bounds rejection | BoardValidatorTests | ShipOutOfBounds_IsInvalid |
| FR-SP-02 | Overlap rejection | BoardValidatorTests | OverlappingShips_IsInvalid |
| FR-SP-02 | Diagonal touch rejection | BoardValidatorTests | DiagonallyTouchingShips_IsInvalid |
| FR-SP-02 | Adjacent (cardinal) touch rejection | BoardValidatorTests | CardinallyTouchingShips_IsInvalid |
| FR-SP-02 | Valid placement | BoardValidatorTests | ValidPlacement_IsValid |
| FR-SP | Wrong ship count rejected | BoardValidatorTests | WrongShipCount_FullValidation_IsInvalid |
| FR-SP | Wrong decks rejected | BoardValidatorTests | WrongDecks_FullValidation_IsInvalid |
| FR-SP-03 | AutoPlacer generates valid Small board | AutoPlacerTests | TryPlace_Small_ValidBoard |
| FR-SP-03 | AutoPlacer generates valid Medium board | AutoPlacerTests | TryPlace_Medium_ValidBoard |
| FR-SP-03 | AutoPlacer generates valid Large board | AutoPlacerTests | TryPlace_Large_ValidBoard |
| FR-SP-03 | AutoPlacer deterministic with seeded RNG | AutoPlacerTests | TryPlace_SeedRng_Deterministic |
| FR-SP-03 | AutoPlacer bounded retry | AutoPlacerTests | TryPlace_BoundedRetries |
| FR-SP-05 | Both boards committed before start | SetupFlowTests | BothCommitted_GameStarts |
| FR-SP-05 | Only one committed — game does NOT start | SetupFlowTests | OnlyOneCommitted_GameDoesNotStart |

### FR-GP (Gameplay)

| FR | Scenario | Test class | Test method |
|---|---|---|---|
| FR-GP-01 | First turn chosen randomly | GameRulesTests | PickFirstTurnUid_DistributionTest |
| FR-GP-02 | Hit → same player continues | GameplayFlowTests | Hit_SamePlayerContinues |
| FR-GP-02 | Miss → turn transfers | GameplayFlowTests | Miss_TurnTransfers |
| FR-GP-03 | Sunk → adjacent cells auto-marked Miss | ShotResolverTests | Sunk_AdjacentCellsMarkedMiss |
| FR-GP-04 | Sunk shape not revealed (only hits + adjacent) | ShotResolverTests | Sunk_ShipShapeNotExplicitlyRevealed |
| FR-GP-05 | Timer expiry transfers turn | GameplayFlowTests | TurnTimerExpiry_TransfersTurn |
| FR-GP-07 | Surrender → immediate loss for surrenderer | GameplayFlowTests | Surrender_LosesGame |
| FR-GP-09 | All ships sunk → opponent wins | GameplayFlowTests | AllShipsSunk_OpponentWins |
| FR-GP-10 | Early exit → more hits wins | GameplayFlowTests | EarlyExit_MoreHitsWins |
| FR-GP-10 | Early exit → draw on equal hits | GameplayFlowTests | EarlyExit_EqualHits_Draw |
| FR-GP | ShotResolver: Miss | ShotResolverTests | Resolve_Miss |
| FR-GP | ShotResolver: Hit | ShotResolverTests | Resolve_Hit |
| FR-GP | ShotResolver: Sunk | ShotResolverTests | Resolve_Sunk |
| FR-GP | Already-shot cell rejection | ShotResolverTests | AlreadyShot_IsRejected |

### FR-EG (End Game)

| FR | Scenario | Test class | Test method |
|---|---|---|---|
| FR-EG-01 | Win result computed | EndGameFlowTests | WinResult_Computed |
| FR-EG-01 | Draw result computed | EndGameFlowTests | DrawResult_Computed |
| FR-EG-03 | Rematch resets state | EndGameFlowTests | Rematch_ResetsState |

### Race conditions

| Scenario | Test class | Test method |
|---|---|---|
| Simultaneous TransferTurn — only one wins | ConnectionFlowTests | RaceCondition_TransferTurn_OnlyOneSucceeds |

---

## 3. Unit Tests — Core/Models

| Class | Tests |
|---|---|
| CellTests | Equality, GetHashCode, ToString |
| ShipTests | GetCells horizontal/vertical, TryHit, IsSunk, IsHitAt |
| BoardTests | AddShip, GetShipAt, IsInBounds, AliveShipsCount, SunkShipsCount, SetCell/GetCell |
| GameStateTests | GetPlayer, GetOpponent |
| MapConfigTests | Small totals, Medium totals, Large totals |

---

## 4. Unit Tests — Core/Engine

| Class | Tests |
|---|---|
| BoardValidatorTests | IsValidPlacement (bounds, overlap, diagonal), IsFullyValid |
| ShotResolverTests | Resolve Miss/Hit/Sunk, IsCellAlreadyShot, adjacent marking |
| AutoPlacerTests | TryPlace Small/Medium/Large, seeded, bounded |
| GameRulesTests | GetConfig all types, ResolveMapConflict, PickFirstTurnUid, CheckWinCondition, DetermineWinnerByHits |
| SessionCodeGeneratorTests | 6-digit format, distribution |

---

## 5. Unit Tests — Data/DTOs

| Class | Tests |
|---|---|
| DtoMapperTests | MapType ↔ string, GamePhase ↔ string, ShotResult ↔ string |
| DtoMapperTests | Cell ↔ CellDto round-trip |
| DtoMapperTests | ShotRecord round-trip from ShotDto |
| DtoMapperTests | Null handling (empty lists, nullable strings) |

---

## 6. Integration Tests — Session Flows

Uses `FakeInMemorySessionService` (two instances, shared `SessionStore`).

| Flow | Tests |
|---|---|
| LobbyFlowTests | create→join, code validation, host leaves before join |
| MapSelectFlowTests | both same, both different, transition to Setup |
| SetupFlowTests | commit boards, partial commit, auto-placer path |
| GameplayFlowTests | full game to win, hit/miss turns, sunk+adjacent, surrender, timer expiry, early exit |
| EndGameFlowTests | results, rematch cycle |
| ConnectionFlowTests | 60s guest timeout, reconnect window, race condition |

---

## 7. Coverage Targets

| Assembly | Target |
|---|---|
| Navy.Core.* | ≥ 95% line |
| Navy.Data.Firebase.Dto.* | ≥ 95% line |
| FakeInMemorySessionService branches | ≥ 90% |
| Aggregate | ≥ 90% |
