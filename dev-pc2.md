# Navy — PC #2 Developer Instructions

> **Your role:** You work in Unity Editor. All C# scripts are already written by PC #1.  
> Your job: open the project, set up Firebase, build the scene, wire Inspector references, configure Android settings, build the APK.

---

## 0. Prerequisites

Install everything before opening Unity.

### 0.1 Git
Download and install: https://git-scm.com  
Verify: `git --version` in terminal.

### 0.2 Unity Hub + Unity 6000.2.10f1 LTS

1. Download Unity Hub: https://unity.com/download
2. Open Unity Hub → **Installs** → **Install Editor**
3. Find version **6000.2.10f1 LTS** (use the search/archive tab)
4. During installation, in the **Add modules** step, check:
   - **Android Build Support** ← required to build APK
     - Inside it also check: **Android SDK & NDK Tools** ← auto-installs the Android toolchain
     - Inside it also check: **OpenJDK** ← Java runtime needed by the build system
5. Click **Install** and wait (can take 10–30 min)

> **What are modules?** Unity is modular — the base editor builds for PC only. To build for Android you must install the Android module. Without it, the "Switch Platform → Android" option exists but the build will fail.

### 0.3 Verify Android module is installed
Unity Hub → **Installs** → click the gear icon on 6000.2.10f1 → **Add modules** → you should see Android Build Support with a checkmark.

---

## 1. Clone the Repository

```bash
git clone <repo-url>
cd Navy
```

Open **Unity Hub** → **Projects** → **Add** → browse to the `Navy/unity/` folder (not the root `Navy/` folder — the Unity project lives inside `unity/`).

Unity will open and start importing packages automatically. This takes **3–10 minutes** on first open:
- UniTask (downloaded from GitHub)
- Unity Localization
- TextMeshPro
- Input System

Wait until the progress bar in the bottom-right disappears.

**Expected result:** Console shows 0 errors. Some warnings are normal (TMP importer, Input System backend). If you see red errors — do not proceed, fix them first (see §11).

---

## 2. Firebase — Full Setup from Scratch

Firebase is Google's backend service. This game uses it for real-time multiplayer: players connect via a 6-digit code, shots are synced through Firebase Realtime Database.

### 2.1 Create a Firebase Project

1. Go to https://console.firebase.google.com
2. Click **Add project**
3. Name: `Navy` (or any name)
4. Disable Google Analytics (not needed) → **Create project**
5. Wait for project creation → **Continue**

### 2.2 Add an Android App

1. On the project overview page, click the **Android icon** (`</>` Android)
2. **Android package name:** `com.zotovs.navy` ← must match exactly
3. **App nickname:** `Navy` (optional)
4. **Debug signing certificate SHA-1:** leave empty for now
5. Click **Register app**
6. **Download `google-services.json`** → save it somewhere (you'll place it later)
7. Click **Next** through the remaining steps (skip "Add Firebase SDK" — we do that inside Unity)
8. Click **Continue to console**

### 2.3 Enable Anonymous Authentication

Authentication lets Firebase identify each player by a unique ID without requiring login.

1. In Firebase Console → left sidebar → **Build** → **Authentication**
2. Click **Get started**
3. Go to the **Sign-in method** tab
4. Click **Anonymous** → toggle **Enable** → **Save**

### 2.4 Create Realtime Database

The Realtime Database stores the game session: who connected, shots fired, current turn, etc.

1. Left sidebar → **Build** → **Realtime Database**
2. Click **Create database**
3. **Location:** select `europe-west1 (Belgium)` ← important for low latency from Ukraine
4. **Security rules:** choose **Start in locked mode** → **Enable**
5. The database is created. Now paste the correct security rules:
   - Click the **Rules** tab
   - Replace the entire content with:

```json
{
  "rules": {
    "sessions": {
      "$sessionCode": {
        ".read": "auth != null && (data.child('meta/hostUid').val() === auth.uid || data.child('meta/guestUid').val() === auth.uid || !data.exists())",
        ".write": "auth != null",
        "meta": {
          "currentTurnUid": {
            ".validate": "newData.val() === data.parent().child('hostUid').val() || newData.val() === data.parent().child('guestUid').val()"
          }
        },
        "shots": {
          "$shotId": {
            ".validate": "newData.child('shooterUid').val() === auth.uid"
          }
        },
        "players": {
          "$uid": {
            ".write": "$uid === auth.uid"
          }
        }
      }
    },
    "presence": {
      "$uid": {
        ".read": "auth != null",
        ".write": "$uid === auth.uid"
      }
    }
  }
}
```

6. Click **Publish**

### 2.5 Place `google-services.json`

1. Take the `google-services.json` file you downloaded in §2.2
2. Copy it into: `Navy/unity/Assets/StreamingAssets/`
3. There is already a `.keep` placeholder file there — leave it, just add the JSON next to it

> **Do not commit `google-services.json` to a public repository.** It is already listed in `.gitignore`.

---

## 3. Import Firebase Unity SDK

Firebase does not come through Unity's Package Manager. You install it as `.unitypackage` files.

### 3.1 Download the SDK

1. Go to: https://firebase.google.com/docs/unity/setup
2. Scroll to **"Download the Firebase Unity SDK"**
3. Download `firebase_unity_sdk.zip`
4. Unzip it — you'll see many `.unitypackage` files

### 3.2 Import FirebaseAuth

1. In Unity Editor: **Assets** menu → **Import Package** → **Custom Package...**
2. Navigate to the unzipped SDK folder
3. Select `FirebaseAuth.unitypackage` → **Open**
4. An import dialog appears showing all files — click **Import All** → **Import**
5. Wait for Unity to compile

### 3.3 Import FirebaseDatabase

1. **Assets** → **Import Package** → **Custom Package...**
2. Select `FirebaseDatabase.unitypackage` → **Open**
3. Click **Import All** → **Import**
4. Wait for compilation

### 3.4 Run the Dependency Resolver

After import, a window may appear automatically: **"Google Play Services Resolver"** or **"External Dependency Manager"**. If it appears:
- Click **Enable Auto-Resolution** or **Resolve**

If it did not appear:
1. Menu bar → **Assets** → **External Dependency Manager** → **Android Resolver** → **Resolve**

> **What is this?** Firebase depends on Android libraries (`.aar` files). The resolver downloads them into `Assets/Plugins/Android/`. Without this step, the APK will crash at startup.

### 3.5 Verify

Check the Console (Window → General → Console):
- No red errors related to Firebase
- You may see yellow warnings — that is fine

---

## 4. Create the Main Scene

Create `Assets/Scenes/Main.unity` and build this exact hierarchy:

```
Main (scene)
├── Main Camera
│     └── AudioListener (component, already on Camera by default)
├── EventSystem                          (GameObject → UI → Event System)
├── Canvas                               (GameObject → UI → Canvas)
│   ├── MenuPanel                        (empty GameObject, child of Canvas)
│   ├── LobbyPanel
│   ├── MapSelectPanel
│   ├── SetupPanel
│   ├── GamePanel
│   ├── SettingsPanel
│   ├── ResultPanel
│   └── ReconnectOverlay
└── AppBootstrap                         (empty GameObject, child of scene root)
```

**Canvas settings:**
- Render Mode: `Screen Space - Camera` → drag Main Camera into the Render Camera slot
- UI Scale Mode: `Scale With Screen Size`, Reference Resolution `1080 × 1920`

**Camera settings:**
- Projection: `Orthographic`
- Clear Flags: `Solid Color`

Each Panel is an empty `GameObject` with a `RectTransform` — stretch it to fill the full Canvas (`Anchor Presets` → stretch all, Alt+click the bottom-right preset).

Only `MenuPanel` starts active. Set all other panels **inactive** in the Hierarchy (uncheck the checkbox next to the name).

---

## 5. Create the Cell Prefab

`BoardRenderer` instantiates a grid of cells at runtime. It needs a prefab with an `Image` component.

1. In the Project window, right-click `Assets/Prefabs/` → **Create** → **Prefab** — name it `CellPrefab`
2. Double-click `CellPrefab` to open it in Prefab Edit mode
3. Add component: `Image` (UI)
4. Set `Image` color to white
5. The `RectTransform` size will be controlled by `BoardRenderer` at runtime — leave default
6. Save and exit Prefab mode (`<` back arrow in scene header)

---

## 6. AudioMixer Setup

The code in `SoundManager` and `MusicManager` controls volume by setting parameters named **exactly** `SFXVolume` and `MusicVolume` on an AudioMixer. If these names don't match, volume control silently breaks.

### 6.1 Create the AudioMixer

1. Right-click `Assets/Audio/` → **Create** → **Audio Mixer** → name it `GameMixer`
2. Double-click `GameMixer` to open the **Audio Mixer** window
3. By default it has one group called `Master`

### 6.2 Add SFX and Music groups

1. Select `Master` group → click **+** → name the new group `SFX`
2. Select `Master` group → click **+** → name the new group `Music`

### 6.3 Expose the volume parameters

This is the step that connects the mixer to the C# code.

For `SFX` group:
1. Select the `SFX` group in the mixer
2. In the Inspector, right-click the **Volume** knob/slider → **Expose 'Volume (of SFX)' to script**
3. In the top-right of the Audio Mixer window, click **Exposed Parameters**
4. You'll see a new entry with a default name like `MyExposedParam` — **double-click it and rename to exactly:** `SFXVolume`

For `Music` group:
1. Select the `Music` group
2. Right-click **Volume** → **Expose 'Volume (of Music)' to script**
3. In **Exposed Parameters**, rename the new entry to exactly: `MusicVolume`

### 6.4 Create AudioCatalog ScriptableObject

1. **Assets** menu → **Create** → **Navy** → **AudioCatalog** → name it `AudioCatalog`
2. It appears in `Assets/Audio/`
3. Select it — in Inspector you'll see slots for: `shot`, `hit`, `sunk`, `miss`, `timerWarning`, `timerAlert`, `victory`, `defeat`, `backgroundMusic`
4. For now: create minimal placeholder `.wav` files (silent, 1 second) and assign them to each slot so the app runs without null errors. Replace with real audio assets later.

> Brief: import a short `.wav` into `Assets/Audio/`, then drag it into each AudioClip slot on the AudioCatalog. One file can be reused for all slots temporarily.

---

## 7. Unity Localization Setup

The game supports Ukrainian and English. Language is auto-detected on first launch from the device locale.

### 7.1 Open Localization Settings

**Edit** menu → **Project Settings** → **Localization**  
Click **Create** to generate the Localization Settings asset (saved to `Assets/`).

### 7.2 Add Locales

1. Under **Available Locales**, click **+** (Add Locale)
2. Find and select **Ukrainian (uk)** → **Add**
3. Click **+** again → find **English (en)** → **Add**
4. Set **Project Locale Identifier** (default locale) to `English (en)`

### 7.3 Create String Table

1. **Window** menu → **Asset Management** → **Localization Tables**
2. Click **New Table Collection**
3. Type: **String Table Collection**
4. Name: `Game`
5. Select both locales: `uk` and `en` → **Create**
6. The table opens. Add these keys (Key column) with translations for both locales:

| Key | English (en) | Ukrainian (uk) |
|---|---|---|
| `menu.host` | Host Game | Створити гру |
| `menu.join` | Join Game | Приєднатись |
| `menu.settings` | Settings | Налаштування |
| `menu.nickname_placeholder` | Enter nickname | Введіть нік |
| `menu.nickname_error` | 3–16 characters, no spaces only | 3–16 символів, не лише пробіли |
| `lobby.waiting_guest` | Waiting for opponent… | Очікування гравця… |
| `lobby.connected` | Opponent connected | Гравець підключився |
| `lobby.timeout` | Session unavailable | Сесія недоступна |
| `lobby.copy_code` | Copy Code | Копіювати код |
| `lobby.join_error` | Session not found | Сесія не знайдена |
| `mapselect.status_waiting` | Waiting for opponent… | Очікування вибору… |
| `mapselect.selected` | Selected: {0} | Обрано: {0} |
| `setup.status_waiting` | Waiting for opponent… | Очікування гравця… |
| `setup.ready_confirm` | Lock in your fleet? | Підтвердити розстановку? |
| `game.your_turn` | Your turn | Ваш хід |
| `game.opponent_turn` | Opponent's turn | Хід суперника |
| `game.surrender_confirm` | Surrender? You will lose. | Здатися? Ви програєте. |
| `game.reconnecting` | Reconnecting… {0}s | Перепідключення… {0}с |
| `result.victory` | Victory! | Перемога! |
| `result.defeat` | Defeat | Поразка |
| `result.draw` | Draw | Нічия |
| `result.rematch` | Play Again | Зіграти ще раз |
| `result.main_menu` | Main Menu | Головне меню |
| `settings.sfx` | SFX Volume | Гучність звуків |
| `settings.music` | Music Volume | Гучність музики |
| `settings.vibration` | Vibration | Вібрація |
| `settings.language` | Language | Мова |
| `settings.save` | Save | Зберегти |
| `common.back` | Back | Назад |
| `common.yes` | Yes | Так |
| `common.no` | No | Ні |

---

## 8. Wire Inspector References

This is the most important section. Every `[SerializeField]` in the scripts must be filled in the Inspector. A missing reference = `NullReferenceException` crash at runtime.

**How to wire:** select a GameObject in the Hierarchy → in the Inspector, find the component → drag the target object/asset from the Hierarchy or Project window into the slot.

---

### 8.1 AppBootstrap GameObject

Select `AppBootstrap` in the Hierarchy.

**Add components:**
- `AppBootstrap` script
- `PanelRouter` script
- `SoundManager` script
- `MusicManager` script
- `VibrationManager` script

**Also add two AudioSources** (for SFX and Music):
- Add component → `Audio Source` → name the GameObject child `SFXSource`
- Add component → `Audio Source` on a second child → name it `MusicSource`
- On the MusicSource: check **Loop** = true, uncheck **Play On Awake** = false (MusicManager handles playback)

**Wire AppBootstrap fields:**

| Field | Drag from |
|---|---|
| `_router` | The `PanelRouter` component on `AppBootstrap` |
| `_sound` | The `SoundManager` component on `AppBootstrap` |
| `_music` | The `MusicManager` component on `AppBootstrap` |
| `_vibration` | The `VibrationManager` component on `AppBootstrap` |

**Wire PanelRouter fields:**

`_panels` is a list. Click **+** 7 times to add 7 entries. Set each:

| Screen (enum) | Panel (drag from Hierarchy) |
|---|---|
| `Menu` | `MenuPanel` |
| `Lobby` | `LobbyPanel` |
| `MapSelect` | `MapSelectPanel` |
| `Setup` | `SetupPanel` |
| `Game` | `GamePanel` |
| `Settings` | `SettingsPanel` |
| `Result` | `ResultPanel` |

**Wire SoundManager fields:**

| Field | Value |
|---|---|
| `_sfxSource` | `SFXSource` AudioSource |
| `_mixer` | `GameMixer` (from `Assets/Audio/`) |
| `_catalog` | `AudioCatalog` (from `Assets/Audio/`) |

**Wire MusicManager fields:**

| Field | Value |
|---|---|
| `_musicSource` | `MusicSource` AudioSource |
| `_mixer` | `GameMixer` |
| `_catalog` | `AudioCatalog` |

---

### 8.2 MenuPanel

Select `MenuPanel` in the Hierarchy. Build this UI structure inside it:

```
MenuPanel
├── NicknameInput      (TMP_InputField)
├── NicknameError      (TMP_Text)
├── SaveNicknameButton (Button)
├── HostButton         (Button)
├── JoinButton         (Button)
└── SettingsButton     (Button)
```

Add components to `MenuPanel` root:
- `MenuView` script
- `MenuPresenter` script

**Wire MenuView fields:**

| Field | Drag |
|---|---|
| `NicknameInput` | `NicknameInput` TMP_InputField |
| `SaveNicknameButton` | `SaveNicknameButton` Button |
| `NicknameError` | `NicknameError` TMP_Text |
| `HostButton` | `HostButton` Button |
| `JoinButton` | `JoinButton` Button |
| `SettingsButton` | `SettingsButton` Button |

---

### 8.3 LobbyPanel

```
LobbyPanel
├── HostPanel
│   ├── SessionCodeText   (TMP_Text)
│   └── CopyCodeButton    (Button)
├── JoinPanel
│   ├── CodeInput         (TMP_InputField)
│   ├── JoinButton        (Button)
│   └── JoinError         (TMP_Text)
├── StatusText            (TMP_Text)
└── BackButton            (Button)
```

Add `LobbyView` and `LobbyPresenter` scripts to `LobbyPanel` root.

**Wire LobbyView fields:**

| Field | Drag |
|---|---|
| `HostPanel` | `HostPanel` GameObject |
| `SessionCodeText` | `SessionCodeText` TMP_Text |
| `CopyCodeButton` | `CopyCodeButton` Button |
| `JoinPanel` | `JoinPanel` GameObject |
| `CodeInput` | `CodeInput` TMP_InputField |
| `JoinButton` | `JoinButton` Button |
| `JoinError` | `JoinError` TMP_Text |
| `StatusText` | `StatusText` TMP_Text |
| `BackButton` | `BackButton` Button |

---

### 8.4 MapSelectPanel

```
MapSelectPanel
├── SmallButton      (Button)
├── MediumButton     (Button)
├── LargeButton      (Button)
├── SelectedMapText  (TMP_Text)
└── StatusText       (TMP_Text)
```

Add `MapSelectView` and `MapSelectPresenter` to root.

**Wire MapSelectView fields:**

| Field | Drag |
|---|---|
| `SmallButton` | `SmallButton` |
| `MediumButton` | `MediumButton` |
| `LargeButton` | `LargeButton` |
| `StatusText` | `StatusText` |
| `SelectedMapText` | `SelectedMapText` |

---

### 8.5 SetupPanel

```
SetupPanel
├── BoardContainer            (RectTransform — empty panel for the grid)
├── ShipTray                  (empty GameObject — ship icons placed here)
├── AutoPlaceButton           (Button)
├── ClearButton               (Button)
├── RotateButton              (Button)
├── ReadyButton               (Button)
├── StatusText                (TMP_Text)
└── InvalidPlacementIndicator (GameObject — a red Image, starts inactive)
```

Add `SetupView` and `SetupPresenter` to root.

**Wire SetupView fields:**

| Field | Drag |
|---|---|
| `BoardContainer` | `BoardContainer` RectTransform |
| `ShipTray` | `ShipTray` Transform |
| `AutoPlaceButton` | `AutoPlaceButton` |
| `ClearButton` | `ClearButton` |
| `RotateButton` | `RotateButton` |
| `ReadyButton` | `ReadyButton` |
| `StatusText` | `StatusText` |
| `InvalidPlacementIndicator` | `InvalidPlacementIndicator` |
| `CellPrefab` | `CellPrefab` from `Assets/Prefabs/` |

---

### 8.6 GamePanel

This is the most complex panel.

```
GamePanel
├── OpponentBoard             (GameObject with BoardRenderer + RectTransform)
├── OwnBoardMini              (GameObject with BoardRenderer + RectTransform)
├── TurnIndicatorText         (TMP_Text)
├── TimerView                 (GameObject with TurnTimerView)
│   └── TimerText             (TMP_Text, child)
├── HistoryPanel              (GameObject with HistoryPanelView)
│   ├── ToggleButton          (Button)
│   └── EntryContainer        (scroll content Transform)
├── SurrenderButton           (Button)
├── ReconnectOverlay          (GameObject — full-screen dark overlay, starts inactive)
│   └── ReconnectCountdownText (TMP_Text)
└── SurrenderConfirmPanel     (GameObject — confirmation dialog, starts inactive)
    ├── SurrenderConfirmYes   (Button)
    └── SurrenderConfirmNo    (Button)
```

Add `GameView` and `GamePresenter` to `GamePanel` root.

**Add `BoardRenderer` component to `OpponentBoard`:**
- `_container` → its own `RectTransform`
- `_cellPrefab` → `CellPrefab` from `Assets/Prefabs/`
- `_isOpponentBoard` → **checked (true)**

**Add `BoardRenderer` component to `OwnBoardMini`:**
- `_container` → its own `RectTransform`
- `_cellPrefab` → `CellPrefab`
- `_isOpponentBoard` → **unchecked (false)**

**Add `TurnTimerView` to `TimerView`:**

| Field | Drag |
|---|---|
| `_timerText` | `TimerText` TMP_Text |

**Add `ShotAnimator` to `GamePanel` root** (or a dedicated child).

**Wire GameView fields:**

| Field | Drag |
|---|---|
| `OpponentBoard` | `OpponentBoard` BoardRenderer |
| `OwnBoardMini` | `OwnBoardMini` BoardRenderer |
| `TurnIndicatorText` | `TurnIndicatorText` TMP_Text |
| `TimerView` | `TimerView` TurnTimerView |
| `HistoryPanel` | `HistoryPanel` HistoryPanelView |
| `SurrenderButton` | `SurrenderButton` Button |
| `ReconnectOverlay` | `ReconnectOverlay` GameObject |
| `ReconnectCountdownText` | `ReconnectCountdownText` TMP_Text |
| `SurrenderConfirmPanel` | `SurrenderConfirmPanel` GameObject |
| `SurrenderConfirmYes` | `SurrenderConfirmYes` Button |
| `SurrenderConfirmNo` | `SurrenderConfirmNo` Button |

---

### 8.7 SettingsPanel

```
SettingsPanel
├── SfxSlider        (Slider, min 0, max 1)
├── MusicSlider      (Slider, min 0, max 1)
├── VibrationToggle  (Toggle)
├── LanguageDropdown (TMP_Dropdown — options: "Ukrainian", "English")
├── NicknameInput    (TMP_InputField)
├── NicknameError    (TMP_Text)
├── SaveNicknameButton (Button)
└── BackButton       (Button)
```

Add `SettingsView` and `SettingsPresenter` to root.

**Wire SettingsView fields:**

| Field | Drag |
|---|---|
| `SfxSlider` | `SfxSlider` |
| `MusicSlider` | `MusicSlider` |
| `VibrationToggle` | `VibrationToggle` |
| `LanguageDropdown` | `LanguageDropdown` |
| `NicknameInput` | `NicknameInput` |
| `SaveNicknameButton` | `SaveNicknameButton` |
| `NicknameError` | `NicknameError` |
| `BackButton` | `BackButton` |

**LanguageDropdown options** — add exactly in this order (index matters):
- Index 0: `Ukrainian`
- Index 1: `English`

---

### 8.8 ResultPanel

```
ResultPanel
├── OutcomeText    (TMP_Text)
├── StatsText      (TMP_Text — multiline)
├── RematchButton  (Button)
└── MainMenuButton (Button)
```

Add `ResultView` and `ResultPresenter` to root.

**Wire ResultView fields:**

| Field | Drag |
|---|---|
| `OutcomeText` | `OutcomeText` |
| `StatsText` | `StatsText` |
| `RematchButton` | `RematchButton` |
| `MainMenuButton` | `MainMenuButton` |

---

### 8.9 Final check — ReconnectOverlay

`ReconnectOverlay` is a direct child of `Canvas` (not inside GamePanel), so it renders on top of everything. It should be a full-screen dark semi-transparent panel. Make sure it is **inactive** by default.

---

## 9. Android Player Settings

**Edit** → **Project Settings** → **Player** → select the **Android tab** (robot icon).

| Setting | Value |
|---|---|
| **Company Name** | `zotovs` |
| **Product Name** | `Navy` |
| **Package Name** | `com.zotovs.navy` |

Scroll down to **Other Settings:**

| Setting | Value |
|---|---|
| **Minimum API Level** | `Android 13.0 (API level 33)` |
| **Target API Level** | `Automatic (highest installed)` |
| **Scripting Backend** | `IL2CPP` |
| **Target Architectures** | Check `ARM64` only — uncheck `ARMv7` |
| **Internet Access** | `Required` |
| **Write Permission** | `Internal` |

Scroll to **Resolution and Presentation:**

| Setting | Value |
|---|---|
| **Default Orientation** | `Portrait` |
| **Allowed Orientations for Auto Rotation** | Uncheck all except `Portrait` |

> **IL2CPP** compiles C# to native C++ code for better performance and Google Play compatibility (64-bit requirement). **ARM64** is the 64-bit architecture required since Android 2019 policy. If you check ARMv7 as well, the APK size increases significantly for no benefit on modern phones.

---

## 10. Build the APK

### 10.1 Switch Platform to Android

1. **File** → **Build Settings**
2. Select **Android** in the platform list
3. Click **Switch Platform** — Unity will reimport assets for Android. This takes several minutes.

> "Switch Platform" is necessary because Unity compiles assets differently per platform (texture compression, shader variants, etc.). You only need to do it once.

### 10.2 Add the Scene

In the **Build Settings** window:
1. Click **Add Open Scenes** — `Assets/Scenes/Main.unity` should appear in the list
2. Make sure its checkbox is checked and it is listed as index `0`

### 10.3 Build

1. Click **Build** (not "Build and Run" — unless your Android device is connected)
2. A file dialog opens — navigate to `Navy/builds/`
3. Name the file `Navy.apk`
4. Click **Save**

Unity builds the APK. First build takes **5–15 minutes** (IL2CPP compiles all C#).

### 10.4 Install on Device

**Option A — USB:**
1. On your Android device: **Settings** → **Developer Options** → enable **USB Debugging**
2. Connect via USB
3. Run: `adb install Navy/builds/Navy.apk`

**Option B — File transfer:**
1. Copy `Navy.apk` to the phone (via Google Drive, USB storage, Telegram to yourself)
2. On the phone: open the file → allow "Install from unknown sources" if prompted → Install

---

## 11. Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| Red errors on project open | Missing Firebase SDK | Complete §3 |
| `NullReferenceException` in `AppBootstrap` on start | Inspector reference not wired | Check all fields in §8.1 |
| App opens to black screen | `PanelRouter._panels` list empty or wrong | Verify §8.1 PanelRouter wiring |
| Volume control has no effect | AudioMixer params not exposed or wrong name | Re-check §6.3 — names must be exactly `SFXVolume` and `MusicVolume` |
| App crashes immediately on Android | `google-services.json` missing or wrong package name | Check `Assets/StreamingAssets/google-services.json` exists; package name must be `com.zotovs.navy` |
| Firebase sign-in fails | EDM resolver not run | Run **Assets → External Dependency Manager → Android Resolver → Resolve** |
| "No locale available" warning | Localization not set up | Complete §7 |
| Build fails: "Android SDK not found" | Android module not installed | Unity Hub → Installs → gear → Add modules → Android Build Support |
| APK not installing on device | `Install unknown apps` disabled | Device Settings → allow install from the source you're using |
| Localization string shows key name (e.g. `menu.host`) | Key not added to String Table | Open Localization Tables window, add the missing key for both locales |
