# Navy — Інструкції для розробника PC #2

> **Твоя роль:** Ти працюєш в Unity Editor. Усі C# скрипти вже написані на PC #1.  
> Твоя задача: відкрити проект, налаштувати Firebase, побудувати сцену, прив'язати референси в Inspector, налаштувати Android-параметри, зібрати APK.

---

## 0. Що потрібно встановити

Встанови все це до того, як відкриєш Unity.

### 0.1 Git
Завантаж і встанови: https://git-scm.com  
Перевір: виконай `git --version` в терміналі — має показати версію.

### 0.2 Unity Hub + Unity 6000.2.10f1 LTS

1. Завантаж Unity Hub: https://unity.com/download
2. Відкрий Unity Hub → **Installs** → **Install Editor**
3. Знайди версію **6000.2.10f1 LTS** (через вкладку Archive або пошук)
4. Під час встановлення, на кроці **Add modules**, постав галочки:
   - **Android Build Support** ← без цього APK не зберуть
     - Всередині також вибери: **Android SDK & NDK Tools** ← встановлює Android-інструменти автоматично
     - Всередині також вибери: **OpenJDK** ← Java-рантайм, потрібний для збірки
5. Натисни **Install** і чекай (може тривати 10–30 хвилин)

> **Що таке модулі?** Unity встановлюється без підтримки конкретних платформ. Базова версія вміє збирати тільки для PC. Щоб збирати для Android — потрібно встановити Android-модуль окремо. Без нього опція "Switch Platform → Android" є, але збірка падає з помилкою.

### 0.3 Перевір, що Android-модуль встановлено
Unity Hub → **Installs** → натисни іконку шестерні на версії 6000.2.10f1 → **Add modules** → повинна стояти галочка біля Android Build Support.

---

## 1. Клонувати репозиторій

```bash
git clone <посилання-на-репо>
cd Navy
```

Відкрий **Unity Hub** → **Projects** → **Add** → знайди папку `Navy/unity/` (не корінь `Navy/` — Unity-проект знаходиться всередині `unity/`).

Unity відкриється і почне автоматично завантажувати пакети. Перший відкрій займає **3–10 хвилин**:
- UniTask (завантажується з GitHub)
- Unity Localization
- TextMeshPro
- Input System

Чекай, поки прогрес-бар у правому нижньому кутку зникне.

**Очікуваний результат:** Console показує 0 червоних помилок. Жовті попередження (warnings) — це нормально (TMP importer, Input System backend). Якщо є червоні помилки — не продовжуй, спочатку виправ їх (дивись §11).

---

## 2. Firebase — Повне налаштування з нуля

Firebase — це хмарний бекенд від Google. Гра використовує його для мультиплеєру: гравці з'єднуються за 6-значним кодом, постріли синхронізуються через Firebase Realtime Database.

### 2.1 Створи Firebase-проект

1. Перейди на https://console.firebase.google.com
2. Натисни **Add project**
3. Назва: `Navy` (або будь-яка)
4. Вимкни Google Analytics (не потрібна) → **Create project**
5. Зачекай → **Continue**

### 2.2 Додай Android-додаток

1. На головній сторінці проекту натисни іконку **Android** (`</>` Android)
2. **Android package name:** `com.zotovs.navy` ← треба вписати точно так
3. **App nickname:** `Navy` (опційно)
4. **Debug signing certificate SHA-1:** залиш порожнім
5. Натисни **Register app**
6. **Завантаж `google-services.json`** → збережи десь (розмістиш пізніше)
7. Натискай **Next** через решту кроків (крок "Add Firebase SDK" пропускай — робимо це всередині Unity)
8. **Continue to console**

### 2.3 Увімкни Anonymous Authentication

Аутентифікація дозволяє Firebase ідентифікувати кожного гравця унікальним ID без реєстрації.

1. Лівий сайдбар → **Build** → **Authentication**
2. Натисни **Get started**
3. Перейди на вкладку **Sign-in method**
4. Натисни **Anonymous** → увімкни **Enable** → **Save**

### 2.4 Створи Realtime Database

Realtime Database зберігає стан ігрової сесії: хто підключений, постріли, поточний хід тощо.

1. Лівий сайдбар → **Build** → **Realtime Database**
2. Натисни **Create database**
3. **Location:** обери `europe-west1 (Belgium)` ← важливо для низької затримки з України
4. **Security rules:** обери **Start in locked mode** → **Enable**
5. База даних створена. Тепер встав правильні правила безпеки:
   - Натисни вкладку **Rules**
   - Заміни весь вміст на:

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

6. Натисни **Publish**

### 2.5 Розміщення `google-services.json`

1. Візьми файл `google-services.json`, який завантажив у §2.2
2. Скопіюй його в папку: `Navy/unity/Assets/StreamingAssets/`
3. Там вже є файл `.keep` — залиш його, просто поклади JSON поруч

> **Не комітить `google-services.json` у публічне репо.** Він вже є в `.gitignore`.

---

## 3. Імпорт Firebase Unity SDK

Firebase не встановлюється через Package Manager. Його встановлюють як `.unitypackage`-файли.

### 3.1 Завантаж SDK

1. Перейди: https://firebase.google.com/docs/unity/setup
2. Знайди розділ **"Download the Firebase Unity SDK"**
3. Завантаж `firebase_unity_sdk.zip`
4. Розпакуй — побачиш багато `.unitypackage`-файлів

### 3.2 Імпорт FirebaseAuth

1. В Unity Editor: меню **Assets** → **Import Package** → **Custom Package...**
2. Знайди розпакований SDK
3. Обери `FirebaseAuth.unitypackage` → **Open**
4. З'явиться діалог зі списком файлів — натисни **Import All** → **Import**
5. Зачекай компіляцію

### 3.3 Імпорт FirebaseDatabase

1. **Assets** → **Import Package** → **Custom Package...**
2. Обери `FirebaseDatabase.unitypackage` → **Open**
3. **Import All** → **Import**
4. Зачекай компіляцію

### 3.4 Запусти Dependency Resolver

Після імпорту може автоматично з'явитися вікно **"Google Play Services Resolver"** або **"External Dependency Manager"**. Якщо з'явилось:
- Натисни **Enable Auto-Resolution** або **Resolve**

Якщо не з'явилось:
1. Меню **Assets** → **External Dependency Manager** → **Android Resolver** → **Resolve**

> **Що це таке?** Firebase залежить від Android-бібліотек (`.aar`-файли). Resolver завантажує їх в `Assets/Plugins/Android/`. Без цього кроку APK падає при старті.

### 3.5 Перевірка

Відкрий Console (Window → General → Console):
- Жодних червоних помилок, пов'язаних із Firebase
- Жовті попередження — нормально

---

## 4. Створення головної сцени

Створи `Assets/Scenes/Main.unity` і побудуй точно таку ієрархію:

```
Main (scene)
├── Main Camera
│     └── AudioListener (компонент — вже є на камері за замовчуванням)
├── EventSystem                          (GameObject → UI → Event System)
├── Canvas                               (GameObject → UI → Canvas)
│   ├── MenuPanel                        (порожній GameObject, дочірній до Canvas)
│   ├── LobbyPanel
│   ├── MapSelectPanel
│   ├── SetupPanel
│   ├── GamePanel
│   ├── SettingsPanel
│   ├── ResultPanel
│   └── ReconnectOverlay
└── AppBootstrap                         (порожній GameObject, в корені сцени)
```

**Налаштування Canvas:**
- Render Mode: `Screen Space - Camera` → перетягни Main Camera у слот Render Camera
- UI Scale Mode: `Scale With Screen Size`, Reference Resolution `1080 × 1920`

**Налаштування Camera:**
- Projection: `Orthographic`
- Clear Flags: `Solid Color`

Кожен Panel — це порожній `GameObject` із `RectTransform`. Розтягни його на весь Canvas (`Anchor Presets` → stretch all, Alt+клік на правий нижній пресет).

Тільки `MenuPanel` активний при старті. Усі інші панелі — **неактивні** (зніми галочку поруч з назвою у Hierarchy).

---

## 5. Створення Cell Prefab

`BoardRenderer` динамічно створює сітку клітинок при старті. Йому потрібен prefab із компонентом `Image`.

1. У Project window: правий клік на `Assets/Prefabs/` → **Create** → **Prefab** → назви `CellPrefab`
2. Двічі клікни `CellPrefab` — відкриється режим редагування prefab
3. Додай компонент: `Image` (UI)
4. Встанови колір `Image` = білий
5. Розміри `RectTransform` керуватимуться `BoardRenderer` в рантаймі — залиш за замовчуванням
6. Збережи і вийди з режиму prefab (стрілка `<` у заголовку сцени)

---

## 6. Налаштування AudioMixer

Код у `SoundManager` та `MusicManager` керує гучністю через параметри AudioMixer із назвами **точно** `SFXVolume` та `MusicVolume`. Якщо назви не збіглися — керування гучністю тихо зламається.

### 6.1 Створи AudioMixer

1. Правий клік на `Assets/Audio/` → **Create** → **Audio Mixer** → назви `GameMixer`
2. Двічі клікни `GameMixer` — відкриється вікно **Audio Mixer**
3. За замовчуванням є одна група `Master`

### 6.2 Додай групи SFX і Music

1. Обери групу `Master` → натисни **+** → назви нову групу `SFX`
2. Обери `Master` → **+** → назви `Music`

### 6.3 Expose параметри гучності

Це крок, який пов'язує міксер із C#-кодом.

Для групи `SFX`:
1. Вибери групу `SFX` в міксері
2. В Inspector: правий клік на повзунку **Volume** → **Expose 'Volume (of SFX)' to script**
3. У правому верхньому куті вікна Audio Mixer натисни **Exposed Parameters**
4. З'явиться новий запис із назвою типу `MyExposedParam` — **двічі клікни і перейменуй точно на:** `SFXVolume`

Для групи `Music`:
1. Вибери групу `Music`
2. Правий клік на **Volume** → **Expose 'Volume (of Music)' to script**
3. У **Exposed Parameters** перейменуй новий запис точно на: `MusicVolume`

### 6.4 Створи AudioCatalog ScriptableObject

1. Меню **Assets** → **Create** → **Navy** → **AudioCatalog** → назви `AudioCatalog`
2. Він з'явиться в `Assets/Audio/`
3. Вибери його — в Inspector побачиш слоти: `shot`, `hit`, `sunk`, `miss`, `timerWarning`, `timerAlert`, `victory`, `defeat`, `backgroundMusic`
4. Поки що: підготуй мінімальні placeholder `.wav`-файли (тиша, 1 секунда) і призначь їх у кожен слот, щоб додаток запускався без null-помилок. Заміниш на реальні звуки пізніше.

> Коротко: імпортуй короткий `.wav` у `Assets/Audio/`, потім перетягни його в кожен AudioClip-слот AudioCatalog. Один файл можна тимчасово використовувати для всіх слотів.

---

## 7. Налаштування Unity Localization

Гра підтримує українську та англійську. Мова визначається автоматично при першому запуску за мовою пристрою.

### 7.1 Відкрий Localization Settings

Меню **Edit** → **Project Settings** → **Localization**  
Натисни **Create** — буде створено Localization Settings asset (збережеться в `Assets/`).

### 7.2 Додай локалі

1. У розділі **Available Locales** натисни **+** (Add Locale)
2. Знайди та вибери **Ukrainian (uk)** → **Add**
3. Знову **+** → **English (en)** → **Add**
4. Встанови **Project Locale Identifier** (мову за замовчуванням) = `English (en)`

### 7.3 Створи String Table

1. Меню **Window** → **Asset Management** → **Localization Tables**
2. Натисни **New Table Collection**
3. Тип: **String Table Collection**
4. Назва: `Game`
5. Вибери обидві локалі: `uk` та `en` → **Create**
6. Таблиця відкриється. Додай ці ключі (колонка Key) з перекладами для обох локалей:

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

## 8. Прив'язка референсів в Inspector

Це найважливіший розділ. Кожен `[SerializeField]` у скриптах повинен бути заповнений в Inspector. Незаповнений референс = падіння з `NullReferenceException` під час виконання.

**Як прив'язувати:** вибери GameObject у Hierarchy → в Inspector знайди компонент → перетягни потрібний об'єкт/ассет із Hierarchy або Project у слот.

---

### 8.1 AppBootstrap GameObject

Вибери `AppBootstrap` у Hierarchy.

**Додай компоненти:**
- Скрипт `AppBootstrap`
- Скрипт `PanelRouter`
- Скрипт `SoundManager`
- Скрипт `MusicManager`
- Скрипт `VibrationManager`

**Також додай два AudioSource** (для SFX та Music):
- Add component → `Audio Source` → назви дочірній об'єкт `SFXSource`
- Add component → `Audio Source` на другому дочірньому → назви `MusicSource`
- На `MusicSource`: **Loop** = true, **Play On Awake** = false (MusicManager керує програванням)

**Прив'яжи поля AppBootstrap:**

| Поле | Перетягни з |
|---|---|
| `_router` | Компонент `PanelRouter` на `AppBootstrap` |
| `_sound` | Компонент `SoundManager` на `AppBootstrap` |
| `_music` | Компонент `MusicManager` на `AppBootstrap` |
| `_vibration` | Компонент `VibrationManager` на `AppBootstrap` |

**Прив'яжи поля PanelRouter:**

`_panels` — список. Натисни **+** 7 разів, щоб додати 7 записів. Встанови кожен:

| Screen (enum) | Panel (перетягни з Hierarchy) |
|---|---|
| `Menu` | `MenuPanel` |
| `Lobby` | `LobbyPanel` |
| `MapSelect` | `MapSelectPanel` |
| `Setup` | `SetupPanel` |
| `Game` | `GamePanel` |
| `Settings` | `SettingsPanel` |
| `Result` | `ResultPanel` |

**Прив'яжи поля SoundManager:**

| Поле | Значення |
|---|---|
| `_sfxSource` | `SFXSource` AudioSource |
| `_mixer` | `GameMixer` (з `Assets/Audio/`) |
| `_catalog` | `AudioCatalog` (з `Assets/Audio/`) |

**Прив'яжи поля MusicManager:**

| Поле | Значення |
|---|---|
| `_musicSource` | `MusicSource` AudioSource |
| `_mixer` | `GameMixer` |
| `_catalog` | `AudioCatalog` |

---

### 8.2 MenuPanel

Вибери `MenuPanel` у Hierarchy. Побудуй таку структуру UI всередині:

```
MenuPanel
├── NicknameInput      (TMP_InputField)
├── NicknameError      (TMP_Text)
├── SaveNicknameButton (Button)
├── HostButton         (Button)
├── JoinButton         (Button)
└── SettingsButton     (Button)
```

Додай компоненти до кореня `MenuPanel`:
- Скрипт `MenuView`
- Скрипт `MenuPresenter`

**Прив'яжи поля MenuView:**

| Поле | Перетягни |
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

Додай скрипти `LobbyView` та `LobbyPresenter` до кореня `LobbyPanel`.

**Прив'яжи поля LobbyView:**

| Поле | Перетягни |
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

Додай `MapSelectView` та `MapSelectPresenter` до кореня.

**Прив'яжи поля MapSelectView:**

| Поле | Перетягни |
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
├── BoardContainer            (RectTransform — порожня панель для сітки)
├── ShipTray                  (порожній GameObject — іконки кораблів розміщуються тут)
├── AutoPlaceButton           (Button)
├── ClearButton               (Button)
├── RotateButton              (Button)
├── ReadyButton               (Button)
├── StatusText                (TMP_Text)
└── InvalidPlacementIndicator (GameObject — червоний Image, за замовчуванням неактивний)
```

Додай `SetupView` та `SetupPresenter` до кореня.

**Прив'яжи поля SetupView:**

| Поле | Перетягни |
|---|---|
| `BoardContainer` | `BoardContainer` RectTransform |
| `ShipTray` | `ShipTray` Transform |
| `AutoPlaceButton` | `AutoPlaceButton` |
| `ClearButton` | `ClearButton` |
| `RotateButton` | `RotateButton` |
| `ReadyButton` | `ReadyButton` |
| `StatusText` | `StatusText` |
| `InvalidPlacementIndicator` | `InvalidPlacementIndicator` |
| `CellPrefab` | `CellPrefab` з `Assets/Prefabs/` |

---

### 8.6 GamePanel

Це найскладніша панель.

```
GamePanel
├── OpponentBoard             (GameObject з BoardRenderer + RectTransform)
├── OwnBoardMini              (GameObject з BoardRenderer + RectTransform)
├── TurnIndicatorText         (TMP_Text)
├── TimerView                 (GameObject з TurnTimerView)
│   └── TimerText             (TMP_Text, дочірній)
├── HistoryPanel              (GameObject з HistoryPanelView)
│   ├── ToggleButton          (Button)
│   └── EntryContainer        (Transform — контейнер записів)
├── SurrenderButton           (Button)
├── ReconnectOverlay          (GameObject — повноекранний темний оверлей, неактивний)
│   └── ReconnectCountdownText (TMP_Text)
└── SurrenderConfirmPanel     (GameObject — діалог підтвердження, неактивний)
    ├── SurrenderConfirmYes   (Button)
    └── SurrenderConfirmNo    (Button)
```

Додай `GameView` та `GamePresenter` до кореня `GamePanel`.

**Додай компонент `BoardRenderer` до `OpponentBoard`:**
- `_container` → власний `RectTransform`
- `_cellPrefab` → `CellPrefab` з `Assets/Prefabs/`
- `_isOpponentBoard` → **увімкнено (true)**

**Додай компонент `BoardRenderer` до `OwnBoardMini`:**
- `_container` → власний `RectTransform`
- `_cellPrefab` → `CellPrefab`
- `_isOpponentBoard` → **вимкнено (false)**

**Додай `TurnTimerView` до `TimerView`:**

| Поле | Перетягни |
|---|---|
| `_timerText` | `TimerText` TMP_Text |

**Додай `ShotAnimator` до кореня `GamePanel`** (або окремого дочірнього об'єкта).

**Прив'яжи поля GameView:**

| Поле | Перетягни |
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
├── SfxSlider          (Slider, min 0, max 1)
├── MusicSlider        (Slider, min 0, max 1)
├── VibrationToggle    (Toggle)
├── LanguageDropdown   (TMP_Dropdown — варіанти: "Ukrainian", "English")
├── NicknameInput      (TMP_InputField)
├── NicknameError      (TMP_Text)
├── SaveNicknameButton (Button)
└── BackButton         (Button)
```

Додай `SettingsView` та `SettingsPresenter` до кореня.

**Прив'яжи поля SettingsView:**

| Поле | Перетягни |
|---|---|
| `SfxSlider` | `SfxSlider` |
| `MusicSlider` | `MusicSlider` |
| `VibrationToggle` | `VibrationToggle` |
| `LanguageDropdown` | `LanguageDropdown` |
| `NicknameInput` | `NicknameInput` |
| `SaveNicknameButton` | `SaveNicknameButton` |
| `NicknameError` | `NicknameError` |
| `BackButton` | `BackButton` |

**Варіанти LanguageDropdown** — додай точно в такому порядку (індекс важливий):
- Індекс 0: `Ukrainian`
- Індекс 1: `English`

---

### 8.8 ResultPanel

```
ResultPanel
├── OutcomeText    (TMP_Text)
├── StatsText      (TMP_Text — багаторядковий)
├── RematchButton  (Button)
└── MainMenuButton (Button)
```

Додай `ResultView` та `ResultPresenter` до кореня.

**Прив'яжи поля ResultView:**

| Поле | Перетягни |
|---|---|
| `OutcomeText` | `OutcomeText` |
| `StatsText` | `StatsText` |
| `RematchButton` | `RematchButton` |
| `MainMenuButton` | `MainMenuButton` |

---

### 8.9 Фінальна перевірка — ReconnectOverlay

`ReconnectOverlay` є прямим дочірнім елементом `Canvas` (не всередині GamePanel), щоб відображатися поверх усього. Це повноекранна темна напівпрозора панель. Переконайся, що вона **неактивна** за замовчуванням.

---

## 9. Налаштування Android Player Settings

Меню **Edit** → **Project Settings** → **Player** → вибери вкладку **Android** (іконка робота).

| Параметр | Значення |
|---|---|
| **Company Name** | `zotovs` |
| **Product Name** | `Navy` |
| **Package Name** | `com.zotovs.navy` |

Прокрути вниз до **Other Settings:**

| Параметр | Значення |
|---|---|
| **Minimum API Level** | `Android 13.0 (API level 33)` |
| **Target API Level** | `Automatic (highest installed)` |
| **Scripting Backend** | `IL2CPP` |
| **Target Architectures** | Постав галочку лише на `ARM64` — зніми `ARMv7` |
| **Internet Access** | `Required` |
| **Write Permission** | `Internal` |

Прокрути до **Resolution and Presentation:**

| Параметр | Значення |
|---|---|
| **Default Orientation** | `Portrait` |
| **Allowed Orientations for Auto Rotation** | Зніми всі галочки, крім `Portrait` |

> **IL2CPP** компілює C# у нативний C++-код — краща продуктивність і вимога Google Play (64-bit). **ARM64** — 64-бітна архітектура, обов'язкова з 2019 року. Якщо також поставити ARMv7 — APK збільшиться без жодної користі для сучасних телефонів.

---

## 10. Збірка APK

### 10.1 Переключи платформу на Android

1. **File** → **Build Settings**
2. Вибери **Android** у списку платформ
3. Натисни **Switch Platform** — Unity переімпортує ассети для Android. Займе кілька хвилин.

> "Switch Platform" потрібне, бо Unity компілює ассети по-різному для кожної платформи (текстурне стиснення, шейдери тощо). Робиш це один раз.

### 10.2 Додай сцену

У вікні **Build Settings**:
1. Натисни **Add Open Scenes** — `Assets/Scenes/Main.unity` з'явиться у списку
2. Переконайся, що галочка стоїть і сцена стоїть під індексом `0`

### 10.3 Збірка

1. Натисни **Build** (не "Build and Run", якщо Android-пристрій не підключений)
2. Відкриється діалог — знайди папку `Navy/builds/`
3. Назви файл `Navy.apk`
4. Натисни **Save**

Unity збирає APK. Перша збірка займає **5–15 хвилин** (IL2CPP компілює весь C#).

### 10.4 Встановлення на пристрій

**Варіант A — USB:**
1. На телефоні: **Налаштування** → **Для розробників** → увімкни **Налагодження USB**
2. Підключи через USB
3. Виконай: `adb install Navy/builds/Navy.apk`

**Варіант B — Файловий трансфер:**
1. Скопіюй `Navy.apk` на телефон (через Google Drive, USB-сховище, Telegram собі)
2. На телефоні: відкрий файл → дозволи "Встановлення з невідомих джерел" → Встановити

---

## 11. Вирішення проблем

| Симптом | Причина | Рішення |
|---|---|---|
| Червоні помилки при відкритті проекту | Firebase SDK не імпортовано | Виконай §3 |
| `NullReferenceException` в `AppBootstrap` при старті | Референс в Inspector не заповнено | Перевір усі поля з §8.1 |
| Додаток відкривається чорним екраном | Список `_panels` у `PanelRouter` порожній або неправильний | Перевір прив'язку PanelRouter в §8.1 |
| Керування гучністю не працює | Параметри AudioMixer не expose або назва неправильна | Перевір §6.3 — назви мають бути точно `SFXVolume` та `MusicVolume` |
| Додаток падає одразу на Android | `google-services.json` відсутній або неправильний package name | Перевір `Assets/StreamingAssets/google-services.json`; package name має бути `com.zotovs.navy` |
| Firebase sign-in не працює | EDM resolver не запускався | Виконай **Assets → External Dependency Manager → Android Resolver → Resolve** |
| Попередження "No locale available" | Localization не налаштовано | Виконай §7 |
| Збірка падає: "Android SDK not found" | Android-модуль не встановлено | Unity Hub → Installs → шестерня → Add modules → Android Build Support |
| APK не встановлюється на пристрій | "Встановлення невідомих програм" вимкнено | Налаштування пристрою → дозволи для джерела, з якого встановлюєш |
| Локалізація показує ключ замість тексту (наприклад `menu.host`) | Ключ не додано до String Table | Відкрий Localization Tables, додай відсутній ключ для обох локалей |
