# Navy — Battleship for Android

A two-player mobile Battleship game for Android 13+. Players connect over the network using a 6-digit session code shared via any messenger (Viber, Telegram, etc.).

## Development Workflow

| Role | Responsibilities |
|---|---|
| **PC #1** (AI / no Unity) | All `.cs` scripts, `manifest.json`, `ProjectSettings`, documentation |
| **PC #2** (Unity Editor) | Scene setup, prefabs, Inspector wiring, Firebase import, APK build |

## Out of Scope (v1.0)

AI / single-player mode · global leaderboard · user accounts · iOS · in-game chat · game state persistence on minimize

## PC #2 Setup

See `dev-pc2.md` for the full Unity Editor setup guide (Firebase, scene, Inspector wiring, APK build).

## Tech Stack

| Category | Technology |
|---|---|
| Engine | Unity 6000.2.10f1 LTS |
| Language | C# (.NET Standard 2.1) |
| Architecture | MVP (Model-View-Presenter) |
| Networking | Firebase Realtime Database |
| Auth | Firebase Anonymous Authentication |
| Async | UniTask (Cysharp) |
| UI | uGUI (Canvas / RectTransform) |
| Localization | Unity Localization Package |
| Build | IL2CPP, ARM64, Android 13+ (API 33+) |

## Project Structure

```
Navy/
├── dev-pc2.md           ← Unity Editor setup guide for PC #2 (English)
├── dev-pc2-uk.md        ← Unity Editor setup guide for PC #2 (Ukrainian)
├── spec/
│   ├── functional.md    ← Functional requirements (features, rules, UX)
│   └── tech.md          ← Technical architecture (MVP layers, Firebase schema, components)
└── unity/               ← Unity project root — open this folder in Unity Hub
    ├── Assets/
    │   ├── Audio/           ← AudioMixer (GameMixer) and AudioCatalog ScriptableObject
    │   ├── Localization/    ← String Tables for Ukrainian and English
    │   ├── Prefabs/         ← CellPrefab and other runtime-instantiated prefabs
    │   ├── Scenes/          ← Main.unity — the single scene for the entire app
    │   ├── Scripts/
    │   │   ├── Core/            ← Pure C# game logic (no UnityEngine dependency)
    │   │   ├── Data/            ← Firebase services, DTOs, PlayerPrefs repository
    │   │   ├── Presentation/    ← MonoBehaviour Views and Presenters for each screen
    │   │   └── Infrastructure/  ← AppBootstrap, ServiceLocator, LocalizationBootstrap
    │   ├── Sprites/         ← Placeholder sprites generated in Unity
    │   └── StreamingAssets/ ← Place google-services.json here (excluded from repo)
    ├── Packages/
    │   └── manifest.json    ← UniTask, Localization, TextMeshPro, Input System
    └── ProjectSettings/     ← Android Player Settings, Quality, Input — committed to repo
```
