# Phase 1 — PC1-Only Test Suite for Navy (Battleship) Unity Project

You are an expert .NET test engineer and Unity-aware C# developer working on a Windows
corporate PC **without Unity Editor installed**. Your job is to design, generate, run,
and iterate on a comprehensive test suite that validates the AI-generated Unity code
in `unity/Assets/Scripts/` to the maximum extent possible without Unity, and to
**autofix bugs in `unity/` source code** until every test is green.

> **Out of scope for Phase 1:** real Firebase, Firebase Local Emulator Suite, PC2
> (Unity Editor, Android build). A separate Phase 2 prompt will cover Firebase
> integration. Do **not** add any external service dependency.

---

## 1. Project context

```
Navy/
├── req/
│   ├── functional.md       ← functional requirements (FR-*)
│   └── tech.md             ← technical architecture (READ THIS FIRST)
├── unity/
│   └── Assets/Scripts/
│       ├── Core/           ← pure C# (no UnityEngine) — PRIMARY TEST TARGET
│       ├── Data/           ← Firebase + DTOs + PlayerPrefs (mixed)
│       ├── Presentation/   ← MonoBehaviour (Unity-only, NOT testable on PC1)
│       └── Infrastructure/ ← Bootstrap (Unity-only, NOT testable on PC1)
├── tests/                  ← YOU CREATE EVERYTHING HERE
└── prompts/test_phase1.md  ← this file
```

Mandatory reading before writing a single line of code:
- `req/functional.md` — full functional spec (FR-CN, FR-MP, FR-SP, FR-GP, FR-EG, FR-UI, FR-ST, FR-NF).
- `req/tech.md` — architecture, RTDB schema, MVP layering rules, FR→component mapping.
- `unity/Assets/Scripts/Core/**/*.cs` — all Core models, engine, contracts.
- `unity/Assets/Scripts/Data/Firebase/Dto/*.cs` — DTO + DtoMapper.
- `unity/Assets/Scripts/Core/Contracts/ISessionService.cs` — interface for session ops.

---

## 2. Environment & tooling constraints

- **OS:** Windows 11, corporate EPAM machine.
- **Installed:** .NET 8 SDK (confirmed). Git. PowerShell 5+.
- **NOT installed and MUST NOT be installed:** Unity Editor, Firebase Unity SDK, Java JRE,
  Node.js, Docker, Firebase CLI/Emulator, Android SDK.
- **License policy:** only OSS, permissive licenses (MIT / Apache 2.0 / BSD).
  Forbidden: GPL/LGPL, Unity, anything proprietary.
- **Network:** assume corporate proxy may restrict downloads. Use only NuGet packages
  available on nuget.org. Avoid Git submodules and arbitrary downloads.

Approved test stack (all OSS, EPAM-friendly):

| Package | License | Purpose |
|---|---|---|
| `xunit` | Apache 2.0 | Test framework |
| `xunit.runner.visualstudio` | Apache 2.0 | VS / dotnet test integration |
| `Microsoft.NET.Test.Sdk` | MIT | Test SDK |
| `FluentAssertions` | Apache 2.0 | Readable assertions |
| `coverlet.collector` | MIT | Code coverage collection |

No HTML coverage report. **Console summary only.**

---

## 3. Goals & success criteria

1. **Coverage targets** (measured by Coverlet over the test-target assemblies):
   - `Navy.Core.*` — **≥ 95%** line coverage.
   - `Navy.Data.Firebase.Dto.*` (DTOs + DtoMapper) — **≥ 95%**.
   - `ISessionService` contract via `FakeInMemorySessionService` flows — **≥ 90%** of
     branches in the fake itself; full functional flow coverage from FR mapping.
   - **Aggregate ≥ 90%** over the union of the three buckets above.
2. **Every test green.** No skipped, no inconclusive.
3. **All FR scenarios from `req/functional.md` that don't require UI/Unity must have at
   least one test** referencing the FR ID in the test name or `[Trait("FR", "...")]`.
4. **Bugs found in `unity/` code must be fixed in place** (see §6). Do not work around
   them in tests.
5. The user runs **one script** (`RUN_TESTS_SET1.ps1`) and sees a clear summary plus
   coverage percentages, then "Press any key to exit".

---

## 4. Folder layout to create

```
tests/
├── Navy.Tests.sln
├── README.md                                  ← short usage notes for the user
├── Directory.Build.props                      ← shared TFM, nullable, warnings-as-errors
├── src/
│   ├── Navy.Core.TestKit/                     ← shared helpers, builders, UniTask shim
│   │   ├── Navy.Core.TestKit.csproj
│   │   ├── UniTaskShim/                       ← see §5.1
│   │   │   ├── UniTask.cs
│   │   │   └── UniTaskExtensions.cs
│   │   ├── Builders/
│   │   │   ├── BoardBuilder.cs
│   │   │   ├── GameStateBuilder.cs
│   │   │   └── ShipBuilder.cs
│   │   └── Fakes/
│   │       └── FakeInMemorySessionService.cs  ← implements ISessionService
│   └── (Core sources are referenced via <Compile Include="..\..\unity\..."> globs;
│        see §5.2 — DO NOT copy files)
├── unit/
│   ├── Navy.Core.Tests/
│   │   ├── Navy.Core.Tests.csproj
│   │   ├── Models/
│   │   │   ├── CellTests.cs
│   │   │   ├── ShipTests.cs
│   │   │   ├── BoardTests.cs
│   │   │   ├── GameStateTests.cs
│   │   │   └── MapConfigTests.cs
│   │   └── Engine/
│   │       ├── BoardValidatorTests.cs
│   │       ├── ShotResolverTests.cs
│   │       ├── AutoPlacerTests.cs
│   │       ├── GameRulesTests.cs
│   │       └── SessionCodeGeneratorTests.cs
│   └── Navy.Data.Dto.Tests/
│       ├── Navy.Data.Dto.Tests.csproj
│       └── DtoMapperTests.cs (+ per-DTO tests)
├── integration/
│   └── Navy.Session.FlowTests/
│       ├── Navy.Session.FlowTests.csproj
│       └── Flows/
│           ├── LobbyFlowTests.cs
│           ├── MapSelectFlowTests.cs
│           ├── SetupFlowTests.cs
│           ├── GameplayFlowTests.cs
│           ├── EndGameFlowTests.cs
│           └── ConnectionFlowTests.cs
├── data/                                      ← test fixtures (JSON, expected boards, etc.)
│   └── (populate as needed)
└── coverage/                                  ← gitignored output of Coverlet
```

`tests/.gitignore` must exclude `bin/`, `obj/`, `coverage/`, `TestResults/`.

---

## 5. Critical implementation details

### 5.1 UniTask shim (no Unity dependency)

`Navy.Core.*` references `Cysharp.Threading.Tasks.UniTask`. UniTask normally requires
`UnityEngine`. **Do NOT install UniTask via NuGet.** Instead, in
`Navy.Core.TestKit/UniTaskShim/`, write a minimal stub:

- `namespace Cysharp.Threading.Tasks` exposing:
  - `public readonly struct UniTask` and `public readonly struct UniTask<T>` — wrap
    `ValueTask` / `ValueTask<T>`.
  - Implicit conversion from/to `Task` and `ValueTask`.
  - `AsUniTask()` extension on `Task` and `Task<T>`.
  - `AttachExternalCancellation(CancellationToken)` extension — pass-through that
    awaits with the token.
  - `Forget()` extension — fire-and-forget with exception logging to console.
  - `UniTask.Delay(TimeSpan, CancellationToken)` and `UniTask.Delay(int ms, ...)`.
  - `UniTaskVoid` struct.
- The shim must be source-compatible with the Core code such that **no edits to
  `unity/` are required just to compile**.

This shim is **only** for tests; it never ships to the Unity project.

### 5.2 Including Core source in the test build

Do **not copy** files. In each test project's `.csproj`, use globbed `<Compile Include>`
items pointing at `..\..\..\unity\Assets\Scripts\Core\**\*.cs` (and Data/Firebase/Dto
where needed), then add `<Compile Remove>` for anything that drags `UnityEngine`.

If a Core file accidentally `using UnityEngine;` (it shouldn't per `tech.md` §2.2 rule),
**that is a bug — fix it in `unity/` source**, do not exclude it.

For Data DTOs/DtoMapper: same approach. If a DTO file references `UnityEngine`
incidentally (e.g., `[Serializable]` from `System` is fine; `JsonUtility` is not),
flag it as a bug — DTOs should be pure C# data classes.

### 5.3 `FakeInMemorySessionService`

Full `ISessionService` implementation backed by an in-memory dictionary that simulates
the RTDB tree from `tech.md` §5.5. Two test instances share the same backing store
(constructor-injected `SessionStore`) to simulate host + guest on one process.

Must support:
- `CreateSessionAsync` / `JoinSessionAsync` / `LeaveSessionAsync`.
- `SubmitMapChoiceAsync`, `CommitBoardAsync`, `SetMapAndAdvanceToSetupAsync`,
  `StartGameAsync`, `FinishGameAsync`.
- `SubmitAimAsync` → fires `OnAimReceived` on target instance.
- `SubmitShotAsync` → fires `OnShotResolved` on both, increments hits/misses/sunk.
- `TransferTurnAsync` with optimistic-concurrency check (race simulation).
- `SurrenderAsync` → triggers finished phase + winner via `OnGameStateChanged`.
- `OnGameStateChanged`, `OnOpponentConnectionChanged`, `OnShotResolved`,
  `OnAimReceived` events.
- Manual time control (inject `ITimeProvider`); a test `FakeTimeProvider` to simulate
  60s timeouts and 5-min turn timers without sleeping.
- Manual disconnect API (`SimulateDisconnect(uid)`) for reconnect tests.

### 5.4 Test data builders

Provide fluent builders so tests are readable, e.g.:

```csharp
var board = new BoardBuilder(MapType.Medium)
    .WithShip(decks: 4, x: 0, y: 0, ShipOrientation.Horizontal)
    .WithShip(decks: 3, x: 0, y: 2, ShipOrientation.Vertical)
    .Build();
```

### 5.5 Test scenarios — mandatory coverage

For each FR below, write at least one test (more if multiple branches). Tag with
`[Trait("FR", "FR-GP-02")]` etc.

**Core / models:** Cell equality, Ship occupied cells calculation, Board indexing
boundaries, MapConfig totals (Small=6/10, Medium=10/20, Large=15/30), GameState
opponent lookup.

**BoardValidator:** out-of-bounds, overlap, **diagonal touching** (FR-SP),
wrong ship counts, wrong decks, valid case.

**ShotResolver:** Miss, Hit, Sunk, adjacent-misses on Sunk (FR-GP-03), already-shot
cell rejection, no ship-shape revealed (FR-GP-04).

**AutoPlacer:** generates valid placement for Small/Medium/Large; deterministic with
seed; bounded retry count.

**GameRules:** map config lookup, **DetermineWinner** (all ships sunk → opponent wins),
**DetermineWinnerByHits** for early exit (FR-GP-10) including draw, **ResolveMapConflict**
for FR-MP, first-turn random selection.

**SessionCodeGenerator:** 6-digit numeric, no leading zero issues acceptable per spec,
distribution sanity (1000 codes, no obvious bias).

**DtoMapper:** Core ↔ DTO round-trip for every model, null-handling, enum string mapping.

**End-to-end flows (via FakeInMemorySessionService, two clients):**
- **FR-CN:** Lobby create → join, 60s guest timeout, host leaves before join,
  reconnect 60s window, foreground/background = leave.
- **FR-MP:** both choose same → that map; choose different → host's choice wins
  (per `GameRules.ResolveMapConflict`).
- **FR-SP:** both commit boards, partial commit blocks start, AutoPlacer path.
- **FR-GP:** first turn random, Hit continues turn, Miss transfers turn, Sunk +
  adjacent misses, full game to win condition, surrender mid-game, double-tap
  confirmation logic if represented in Core, turn timer expiry transfers turn,
  early-exit winner-by-hits, draw on equal hits.
- **FR-EG:** result computation, rematch resets state.
- **Race conditions:** simultaneous TransferTurn from both clients — exactly one
  succeeds.

### 5.6 Determinism

All randomness (`AutoPlacer`, `SessionCodeGenerator`, first-turn selection) must be
seedable. If current Core code uses `System.Random` without a seed parameter, **add a
constructor overload accepting `Random` or seed** in `unity/` source. This is a
legitimate fix, not a workaround.

### 5.7 Concurrency / timing

Use `FakeTimeProvider` (your own, in TestKit). No `Thread.Sleep`, no `Task.Delay`
beyond a few milliseconds. All "60 seconds" / "5 minutes" assertions advance virtual
time.

---

## 6. Autofix policy (CRITICAL)

When a test fails because of a real bug in the AI-generated Unity code:

1. **Identify the bug.** Cross-reference `req/functional.md` and `req/tech.md` to
   confirm intended behavior.
2. **Fix the file under `unity/Assets/Scripts/`** — minimum viable change, preserve
   coding style, namespaces, and architectural rules from `tech.md` §2.2 (e.g., Core
   may not import UnityEngine).
3. **Re-run tests.** Iterate until green.
4. **Maintain a fix log** at `tests/AUTOFIX_LOG.md` with one bullet per fix:
   `- [file:line] short description (FR-XX-YY)` and the commit-style rationale.
5. **Never weaken a test to make it pass.** If a test seems wrong, document it in
   `AUTOFIX_LOG.md` under "Disputed" and ask the user.
6. **Do not modify** `unity/Assets/Scripts/Presentation/**` or
   `unity/Assets/Scripts/Infrastructure/**` (untestable on PC1). Bugs there are
   recorded in `AUTOFIX_LOG.md` for Phase 2 / PC2 follow-up.
7. **Do not modify** `unity/Assets/Scripts/Data/Firebase/FirebaseSessionService.cs`,
   `FirebaseAuthService.cs`, `FirebaseBootstrap.cs` (Firebase Unity SDK code,
   untestable in Phase 1). Bugs there → Phase 2 log.

Termination rule: stop iterating after **10 consecutive autofix attempts** on the same
test file without progress. Surface the situation to the user with full context.

---

## 7. The runner script: `tests/RUN_TESTS_SET1.ps1`

PowerShell script. Behavior:

1. Print banner: project name, date, .NET version (`dotnet --version`).
2. `dotnet --version` check; abort with red error if not 8.x.
3. `cd` into `tests/`.
4. `dotnet restore Navy.Tests.sln`.
5. `dotnet build Navy.Tests.sln -c Release --nologo` — abort on build error.
6. `dotnet test Navy.Tests.sln -c Release --no-build --nologo --logger "console;verbosity=normal" --collect:"XPlat Code Coverage" --results-directory ./coverage`
7. Parse the Cobertura XML(s) under `./coverage/**/coverage.cobertura.xml` and print
   a console table:
   ```
   Assembly                              Lines   Covered   %
   Navy.Core                              812      791    97.4
   Navy.Data.Firebase.Dto                 145      140    96.6
   Navy.Tests.* (helpers, fakes)          ...
   --------------------------------------------------------
   TOTAL (target assemblies only)        957      931    97.3
   ```
   Use ANSI/PowerShell colors: green ≥90%, yellow 80–89%, red <80%.
8. Print test summary: `Passed: N | Failed: N | Skipped: N | Duration: Xs`.
9. If any test failed or any target assembly is below its threshold, exit code 1
   (but still wait for keypress).
10. **Final line:** `Read-Host -Prompt 'Press ENTER to exit'` (PowerShell's idiomatic
    pause; works without external utilities).

Script must:
- Use `$ErrorActionPreference = 'Stop'` then explicit try/catch around dotnet calls
  so failures still reach the keypress.
- Be safe to run from any CWD (use `$PSScriptRoot`).
- Be idempotent (clean `coverage/` at start).
- Not require admin rights.
- Not require `Set-ExecutionPolicy` changes if user runs via
  `powershell -ExecutionPolicy Bypass -File RUN_TESTS_SET1.ps1`. Mention this in
  `tests/README.md`.

---

## 8. Deliverables checklist

- [ ] `tests/` folder fully populated per §4.
- [ ] All test projects build with zero warnings (warnings-as-errors enabled).
- [ ] `RUN_TESTS_SET1.ps1` working as specified in §7.
- [ ] `tests/README.md` — 1 page: prerequisites, how to run, how to read results.
- [ ] `tests/AUTOFIX_LOG.md` — list of bugs fixed in `unity/` and untestable bugs
      deferred to Phase 2 / PC2.
- [ ] All tests green.
- [ ] Coverage targets met (§3.1).
- [ ] No new dependencies beyond §2's approved stack.
- [ ] No `unity/Assets/Scripts/Presentation/**` or `Infrastructure/**` modifications.

---

## 9. Working method

1. **Read** `req/functional.md`, `req/tech.md`, all of `unity/Assets/Scripts/Core/`
   and `unity/Assets/Scripts/Data/Firebase/Dto/`, plus `ISessionService.cs`.
2. **Plan** test matrix: list every FR-* and map to test class/method names.
   Save to `tests/TEST_PLAN.md`.
3. **Scaffold** solution, projects, TestKit (UniTask shim, builders, fakes).
4. **Verify build** — Core sources compile inside the test project via globbed
   `<Compile Include>`. Fix Core bugs (Unity leakage etc.) per §6.
5. **Write tests** layer by layer: Models → Engine → DTOs → Flows.
6. **Run** via `RUN_TESTS_SET1.ps1`. Iterate: fix `unity/` source on real bugs
   (§6), refine tests on test bugs.
7. **Stop** when §3 success criteria are met.
8. **Final report** in your reply: tests count by category, coverage table,
   bug list summary, anything deferred to Phase 2.

---

## 10. Out of scope (do NOT do)

- Real Firebase calls or Firebase Local Emulator.
- Editing `Presentation/` or `Infrastructure/` Unity code.
- Editing `FirebaseSessionService.cs`, `FirebaseAuthService.cs`,
  `FirebaseBootstrap.cs`.
- Installing Unity, Java, Node.js, Docker, Firebase CLI.
- Generating `.bat` files (PowerShell only).
- HTML coverage reports.
- PC2 / Android build steps.
- Touching `req/` or `prompts/`.

---

## 11. Final notes

- Treat `req/functional.md` and `req/tech.md` as the source of truth. If `unity/`
  code disagrees with them, `unity/` is wrong (autofix per §6).
- Keep tests fast. Whole suite should finish in **under 30 seconds** on a corp laptop.
- Code style: same conventions as `unity/` — PascalCase types, `_camelCase` private
  fields, `sealed` where appropriate.
- When in doubt about user intent or an FR interpretation, **stop and ask** rather
  than guessing.

Now begin with step 9.1 (read the requirements and Core sources) and produce
`tests/TEST_PLAN.md` before writing any code.
