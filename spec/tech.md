# Технічна архітектура: Гра "Морський бій"

**Версія:** 1.0
**Базується на:** `requirements/functional.md` v1.0
**Платформа:** Android 13+ (API 33+)
**Engine:** Unity 6000.2.10f1 LTS
**Мова коду:** C# (англійською); архітектурна документація — українською

> Документ описує всі архітектурні та технологічні рішення для генерації Unity-проекту через AI-асистента. Призначений для подальшого автоматизованого створення скелету проекту та реалізації функціоналу.

---

## 1. Огляд

### 1.1. Стек

| Категорія | Технологія | Обґрунтування |
|---|---|---|
| Game engine | Unity 6000.2.10f1 LTS | Стабільна LTS-версія, повна підтримка Android, офіційний Firebase SDK |
| Мова | C# (.NET Standard 2.1) | Стандарт Unity |
| Scripting backend | IL2CPP, ARM64 | Вимога Google Play (64-bit), краща продуктивність |
| UI Framework | uGUI (Canvas + RectTransform) | Перевірене рішення, простіше для AI генерувати |
| Async | UniTask (Cysharp) | async/await без аллокацій, заміна корутинам |
| Анімації | Unity Animator + корутини | Без зовнішніх залежностей, достатньо для 2D MVP |
| Локалізація | Unity Localization Package | Офіційний пакет, підтримка укр/англ |
| Network/State | Firebase Realtime Database (Spark) | Безкоштовно, працює через HTTPS/443, обходить NAT/файрволи |
| Auth | Firebase Anonymous Authentication | Без реєстрації, унікальний UID, дозволяє Security Rules |
| Локальне сховище | UnityEngine.PlayerPrefs | Достатньо для ніку та налаштувань |
| Crash reporting | — | Не передбачено для MVP |
| Аналітика | — | Не передбачено для MVP |

### 1.2. Поза обсягом архітектури

- AI / одиночний режим (FR не передбачає)
- iOS / Web / Desktop (тільки Android)
- Cloud Functions (Spark plan не підтримує)
- Античит / серверна валідація (peer-authoritative)
- CI/CD (додасться пізніше при необхідності)
- Юніт-тести (MVP-прототип; Core-модулі написані як pure C# для майбутнього додавання NUnit-тестів)

---

## 2. Архітектурний патерн

### 2.1. MVP (Model-View-Presenter)

Розділення трьох відповідальностей:

- **Model** — pure C# дані та правила гри. Без залежностей від `UnityEngine`. Тестується ізольовано.
- **View** — `MonoBehaviour`, відображає стан і отримує введення (кліки, drag). Не містить логіки.
- **Presenter** — `MonoBehaviour`, координує View та Model, спілкується з Data-шаром (Firebase, PlayerPrefs).

```
┌─────────────────┐    events     ┌──────────────┐   model ops   ┌─────────────┐
│       View      │──────────────▶│   Presenter  │──────────────▶│    Model    │
│ (MonoBehaviour) │◀──────────────│(MonoBehaviour)│◀──────────────│ (pure C#)   │
└─────────────────┘  state/render └──────────────┘    state      └─────────────┘
                                          │
                                          │ uses
                                          ▼
                                  ┌──────────────┐
                                  │  Data layer  │
                                  │ (Firebase,   │
                                  │ PlayerPrefs) │
                                  └──────────────┘
```

### 2.2. Шари (без Assembly Definitions)

Усі скрипти знаходяться в одній стандартній збірці `Assembly-CSharp`. Розділення — **за namespace та папками**, не за `.asmdef`.

| Шар | Namespace | Залежить від | Залежності |
|---|---|---|---|
| Core | `Navy.Core.*` | — (pure C#) | — |
| Data | `Navy.Data.*` | Core, Firebase SDK, UnityEngine | Зовнішні |
| Presentation | `Navy.Presentation.*` | Core, Data, UnityEngine.UI | Зовнішні |
| Infrastructure | `Navy.Infrastructure.*` | Core, UnityEngine | Зовнішні |

**Правило:** Core НЕ імпортує `UnityEngine`. Перевіряти при code review.

---

## 3. Структура проекту

```
Navy/
├── Assets/
│   ├── Scripts/
│   │   ├── Core/                          ← pure C#, без UnityEngine
│   │   │   ├── Models/
│   │   │   │   ├── Cell.cs                (координата + стан)
│   │   │   │   ├── Ship.cs                (палубність, орієнтація, позиція, стан)
│   │   │   │   ├── Board.cs               (поле + кораблі)
│   │   │   │   ├── GameState.cs           (повний стан партії)
│   │   │   │   ├── PlayerState.cs         (стан одного гравця)
│   │   │   │   ├── ShotResult.cs          (Miss / Hit / Sunk)
│   │   │   │   ├── MapType.cs             (Small / Medium / Large)
│   │   │   │   ├── ShipOrientation.cs     (Horizontal / Vertical)
│   │   │   │   └── GamePhase.cs           (Lobby / Setup / Playing / Finished)
│   │   │   ├── Engine/
│   │   │   │   ├── BoardValidator.cs      (валідація розстановки)
│   │   │   │   ├── ShotResolver.cs        (попадання / промах / потоплення)
│   │   │   │   ├── AutoPlacer.cs          (випадкова валідна розстановка)
│   │   │   │   ├── GameRules.cs           (конфіг карт, переможець)
│   │   │   │   └── SessionCodeGenerator.cs (6-значний код)
│   │   │   └── Contracts/
│   │   │       ├── ISessionService.cs
│   │   │       ├── ISettingsRepository.cs
│   │   │       └── ITimeProvider.cs
│   │   │
│   │   ├── Data/                          ← реалізації + DTO
│   │   │   ├── Firebase/
│   │   │   │   ├── FirebaseSessionService.cs
│   │   │   │   ├── FirebaseAuthService.cs
│   │   │   │   ├── Dto/                   (серіалізація під RTDB)
│   │   │   │   │   ├── SessionDto.cs
│   │   │   │   │   ├── PlayerDto.cs
│   │   │   │   │   ├── BoardDto.cs
│   │   │   │   │   ├── ShotDto.cs
│   │   │   │   │   └── DtoMapper.cs       (Core ↔ DTO)
│   │   │   │   └── FirebaseBootstrap.cs   (ініціалізація SDK)
│   │   │   ├── Settings/
│   │   │   │   └── PlayerPrefsSettingsRepository.cs
│   │   │   └── Audio/
│   │   │       └── AudioCatalog.cs        (ScriptableObject зі звуками)
│   │   │
│   │   ├── Presentation/                  ← MonoBehaviour: View + Presenter
│   │   │   ├── Common/
│   │   │   │   ├── UIPanelBase.cs         (Show/Hide базовий клас панелі)
│   │   │   │   ├── PanelRouter.cs         (перемикає активну панель)
│   │   │   │   ├── SoundManager.cs
│   │   │   │   ├── MusicManager.cs
│   │   │   │   └── VibrationManager.cs
│   │   │   ├── Menu/
│   │   │   │   ├── MenuView.cs
│   │   │   │   └── MenuPresenter.cs
│   │   │   ├── Lobby/
│   │   │   │   ├── LobbyView.cs           (host/join код, статус підключення)
│   │   │   │   └── LobbyPresenter.cs
│   │   │   ├── MapSelect/
│   │   │   │   ├── MapSelectView.cs
│   │   │   │   └── MapSelectPresenter.cs
│   │   │   ├── Setup/
│   │   │   │   ├── SetupView.cs           (drag-and-drop кораблів)
│   │   │   │   ├── SetupPresenter.cs
│   │   │   │   └── ShipDragHandler.cs
│   │   │   ├── Game/
│   │   │   │   ├── GameView.cs            (велике поле + міні-карта + таймер + історія)
│   │   │   │   ├── GamePresenter.cs
│   │   │   │   ├── BoardRenderer.cs
│   │   │   │   ├── ShotAnimator.cs
│   │   │   │   ├── TurnTimerView.cs
│   │   │   │   └── HistoryPanelView.cs
│   │   │   ├── Settings/
│   │   │   │   ├── SettingsView.cs
│   │   │   │   └── SettingsPresenter.cs
│   │   │   └── Result/
│   │   │       ├── ResultView.cs          (статистика, реванш)
│   │   │       └── ResultPresenter.cs
│   │   │
│   │   └── Infrastructure/
│   │       ├── AppBootstrap.cs            (точка входу, ініціалізація сервісів)
│   │       ├── ServiceLocator.cs          (простий статичний реєстр сервісів)
│   │       └── Localization/
│   │           └── LocalizationBootstrap.cs
│   │
│   ├── Scenes/
│   │   └── Main.unity                     (створюється на PC #2 в Unity Editor)
│   │
│   ├── Prefabs/                           (створюються на PC #2)
│   ├── Sprites/                           (placeholder-и, генеруються в Unity)
│   ├── Audio/                             (placeholder .wav)
│   ├── Localization/                      (StringTables: uk, en)
│   └── StreamingAssets/
│       └── google-services.json           (Firebase config; копіюється з Firebase Console)
│
├── Packages/
│   └── manifest.json                      (UniTask, Localization, TextMeshPro, Input System)
│
├── ProjectSettings/
│   └── ProjectVersion.txt                 (m_EditorVersion: 6000.2.10f1)
│
├── docs/
│   └── (місце для додаткових архітектурних документів)
│
├── requirements/
│   ├── functional.md
│   └── tech.md                            (цей файл)
│
├── prompts/
│   └── architecture.md
│
├── .gitignore                             (стандартний Unity .gitignore)
└── README.md
```

### 3.1. Папки, які створюються вручну на PC #2

- `Assets/Scenes/Main.unity` — створюється в Unity Editor
- `Assets/Prefabs/**` — створюються через Unity Editor (drag від UI/панелей)
- Решта `ProjectSettings/*.asset` — Unity згенерує автоматично при першому відкритті проекту

---

## 4. Доменна модель (Core)

### 4.1. Конфігурація карт (FR-MP)

```csharp
public enum MapType { Small, Medium, Large }

public sealed class MapConfig
{
    public MapType Type;
    public int BoardSize;                        // 8 / 10 / 12
    public IReadOnlyList<ShipGroup> Ships;       // палубність × кількість
    public int TotalShips;                       // 6 / 10 / 15
    public int TotalCells;                       // 10 / 20 / 30
}

public sealed class ShipGroup
{
    public int Decks;       // 1..5
    public int Count;
}
```

| MapType | BoardSize | Ships | TotalShips | TotalCells |
|---|---|---|---|---|
| Small | 8 | 1×3, 2×2, 3×1 | 6 | 10 |
| Medium | 10 | 1×4, 2×3, 3×2, 4×1 | 10 | 20 |
| Large | 12 | 1×5, 2×4, 3×3, 4×2, 5×1 | 15 | 30 |

### 4.2. Стан партії

```csharp
public sealed class GameState
{
    public string SessionId;
    public MapType MapType;
    public GamePhase Phase;          // Lobby, Setup, Playing, Finished
    public PlayerState Host;
    public PlayerState Guest;
    public string CurrentTurnUid;    // UID гравця, чий хід
    public long TurnStartedAtMs;     // Firebase ServerValue.TIMESTAMP
    public IReadOnlyList<ShotRecord> History;
    public string WinnerUid;         // null поки не завершено
    public bool IsDraw;
}

public sealed class PlayerState
{
    public string Uid;
    public string Nickname;
    public Board Board;              // власне поле з кораблями
    public bool IsReady;
    public MapType ChosenMapType;
    public int Hits;
    public int Misses;
    public int SunkShips;
}

public sealed class ShotRecord
{
    public string ShooterUid;
    public Cell Coordinate;
    public ShotResult Result;        // Miss, Hit, Sunk
    public long TimestampMs;
}
```

### 4.3. Розстановка та валідація (FR-SP)

`BoardValidator`:
- Кораблі не виходять за межі поля
- Кораблі не перетинаються
- **Кораблі не торкаються один одного** (включно з діагоналями)
- Кількість і палубність відповідають `MapConfig`

`AutoPlacer` — backtracking з обмеженням спроб (≤1000) для гарантованої валідної розстановки.

### 4.4. Логіка пострілу (FR-GP-02..04)

`ShotResolver.Resolve(Board target, Cell coord) → ShotResult`:
1. Якщо клітинка вже стріляна → виключення (Presenter валідує до виклику)
2. Якщо в клітинці частина корабля → `Hit`
3. Якщо це остання клітинка корабля → `Sunk`
4. Інакше → `Miss`

При `Sunk` — Resolver автоматично позначає всі дотичні клітинки як `Miss` для відображення (FR-GP-03), але **форма потопленого не розкривається** (FR-GP-04) — Presenter показує лише окремі hits + дотичні misses, без контуру корабля.

### 4.5. Визначення переможця (FR-GP-09, 10)

- **Нормальне завершення:** усі кораблі одного гравця підбиті → інший переможець.
- **Достроковий вихід** (FR-GP-10): більше попадань → переможець; рівність → нічия.

`GameRules.DetermineWinner(GameState) → (winnerUid?, isDraw)`.

---

## 5. Мережева модель

### 5.1. Загальна схема

```
   ┌──────────────┐                   ┌──────────────────────┐                   ┌──────────────┐
   │   Host App   │   WebSocket/HTTPS │  Firebase Realtime   │   WebSocket/HTTPS │   Guest App  │
   │  (Android)   │◀─────────────────▶│   Database (EU)      │◀─────────────────▶│   (Android)  │
   └──────────────┘                   └──────────────────────┘                   └──────────────┘
        ▲                                                                              ▲
        │ передає 6-значний код                                                        │
        │ через Viber / Telegram / SMS                                                 │
        └──────────────────────────────────────────────────────────────────────────────┘
                                  (поза додатком)
```

### 5.2. Authority

**Peer-authoritative:** кожен клієнт виконує розрахунки локально (валідація розстановки, результат пострілу) і записує результат у RTDB. Інший клієнт читає та довіряє. Античит не передбачено.

**Передача ходу** виконується через Firebase **Transaction** для уникнення race condition при одночасних записах.

### 5.3. Регіон Firebase

`europe-west1` (Belgium) — найнижча латентність для України (~30–60 мс RTT).

### 5.4. Аутентифікація

Firebase Anonymous Authentication: при першому запуску гра викликає `auth.SignInAnonymouslyAsync()` → отримує стабільний UID, прив'язаний до пристрою. UID використовується як ідентифікатор гравця в RTDB.

### 5.5. Schema RTDB

```
/sessions
  /{sessionCode}                       # 6-значний цифровий код, напр. "482917"
    /meta
      hostUid: string
      guestUid: string | null
      mapType: "Small" | "Medium" | "Large" | null  # обраний після узгодження
      hostMapChoice: "Small" | "Medium" | "Large"
      guestMapChoice: "Small" | "Medium" | "Large" | null
      phase: "Lobby" | "Setup" | "Playing" | "Finished"
      currentTurnUid: string | null
      turnStartedAtMs: number          # ServerValue.TIMESTAMP
      winnerUid: string | null
      isDraw: boolean
      createdAtMs: number
    /players
      /{uid}
        nickname: string
        connected: boolean             # підтримується через onDisconnect
        isReady: boolean
        boardCommitted: boolean        # true після "Готовий"
        # board НЕ зберігається у відкритому вигляді в одному вузлі;
        # див. розділ 5.6 — кораблі залишаються локально, бо peer-authoritative
        hits: number
        misses: number
        sunkShipsCount: number
        chosenMapType: "Small" | ...
    /shots
      /{pushId}
        shooterUid: string
        targetUid: string
        x: number
        y: number
        result: "Miss" | "Hit" | "Sunk"
        sunkShipCells: [{x, y}, ...] | null   # лише при Sunk: координати потопленого корабля
        adjacentMissCells: [{x, y}, ...] | null  # дотичні клітинки при Sunk (FR-GP-03)
        timestampMs: number
    /surrender
      uid: string | null               # хто здався (FR-GP-07)

/presence
  /{uid}
    online: boolean                    # onDisconnect → false
    lastSeenMs: number
    sessionCode: string | null         # активна сесія (1 на гравця)
```

### 5.6. Особливості зберігання дошки

Оскільки **peer-authoritative**, повна дошка кожного гравця **не зберігається в RTDB**. Кожен клієнт тримає власну дошку локально та обчислює результат пострілу супротивника, записуючи лише `ShotDto` в `/shots`.

Виняток: при **достроковому виході** (FR-GP-10) переможець визначається за кількістю влучань — це поле `hits` синхронізується в `/players/{uid}/hits` після кожного пострілу.

### 5.7. Security Rules

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

> Згенеровані правила — стартова точка. На PC #2 при налаштуванні Firebase Console їх треба перевірити та доналаштувати.

### 5.8. Управління підключенням

| Сценарій | Реалізація |
|---|---|
| Гість підключається 60с (FR-CN-05) | Хост запускає локальний таймер після створення сесії; якщо `guestUid` не з'являється — видаляє вузол, показує "Сесія недоступна" |
| Перепідключення 60с (FR-CN-06) | `onDisconnect` ставить `connected: false`; інший клієнт показує оверлей "Очікування 60с" + локальний таймер; після спливання → достроковий вихід (FR-GP-13) |
| Згортання додатка = вихід (FR-CN-07) | `Application.focusChanged += OnFocusChanged` → при `false` ініціюємо `LeaveSession()` (видалення з RTDB) |
| Видалення сесії | `onDisconnect().removeValue()` для свого `players/{uid}/connected = false` + явне видалення вузла `/sessions/{code}` при коректному виході |
| Хост вийшов до приєднання гостя | Сесія видаляється негайно (через `onDisconnect`); гість при спробі приєднатися бачить "Сесія недоступна" |

### 5.9. Транзакція передачі ходу

Після `Miss` поточний хід передається супротивнику через `RunTransaction`:

```
transaction(meta):
  if meta.currentTurnUid != myUid:
    abort                           # вже передано (race)
  meta.currentTurnUid = opponentUid
  meta.turnStartedAtMs = ServerValue.TIMESTAMP
  return meta
```

При `Hit` / `Sunk` — `currentTurnUid` залишається без змін (FR-GP-02), але `turnStartedAtMs` оновлюється для перезапуску 5-хвилинного таймера.

### 5.10. Таймер ходу (FR-GP-05, 06)

**Локальний** на кожному клієнті: при отриманні нового `turnStartedAtMs` через listener — кожен клієнт запускає `UniTask`-таймер на 5 хвилин від поточного локального часу. Дрейф годинників ≤ кілька секунд — прийнятно для 5-хвилинного інтервалу.

При спливанні таймера:
- Якщо це **мій хід** — клієнт автоматично виконує транзакцію передачі ходу (як після Miss, але без запису пострілу).
- Якщо це **хід супротивника** — нічого не робиться (його клієнт сам передасть).

Попередження (FR-GP-06):
- T-60с: жовтий колір таймера + звук "warning"
- T-30с: червоний колір + звук "alert"
- T-10с: миготіння + вібрація

---

## 6. Сцени та навігація

### 6.1. Структура сцен

**Одна сцена `Main.unity`**, що містить кореневий Canvas з усіма UI-панелями. Перемикання між екранами — через `SetActive(true/false)` на кореневих GameObject панелей.

```
Main (scene)
├── EventSystem
├── Canvas (Screen Space - Camera)
│   ├── MenuPanel             (active при старті)
│   ├── LobbyPanel
│   ├── MapSelectPanel
│   ├── SetupPanel
│   ├── GamePanel
│   ├── SettingsPanel
│   ├── ResultPanel
│   └── ReconnectOverlay      (модальна, поверх інших)
├── AudioListener
├── AppBootstrap (GameObject)
│   ├── AppBootstrap.cs
│   ├── ServiceLocator (host)
│   ├── SoundManager.cs
│   ├── MusicManager.cs
│   └── VibrationManager.cs
└── Camera (Orthographic, для UI)
```

### 6.2. Маршрутизатор панелей

`PanelRouter` — простий клас, що знає всі панелі та перемикає активну:

```csharp
public enum AppScreen
{
    Menu, Lobby, MapSelect, Setup, Game, Settings, Result
}

public sealed class PanelRouter : MonoBehaviour
{
    [SerializeField] private List<UIPanelBase> panels;
    public void Show(AppScreen screen) { /* активуємо потрібну, деактивуємо інші */ }
}
```

### 6.3. Граф екранів

```
Menu ─┬─▶ Lobby (Host) ─▶ MapSelect ─▶ Setup ─▶ Game ─▶ Result ─┬─▶ MapSelect (rematch)
      │                                                          └─▶ Menu
      ├─▶ Lobby (Join) ─▶ MapSelect ─▶ Setup ─▶ Game ─▶ Result ─...
      └─▶ Settings ─▶ Menu
```

---

## 7. Mapping функціональних вимог на компоненти

| FR | Компоненти |
|---|---|
| FR-CN-01 (нік) | `MenuPresenter` + `PlayerPrefsSettingsRepository` |
| FR-CN-02..04 (host/join, статус) | `LobbyPresenter` + `FirebaseSessionService` |
| FR-CN-05 (тайм-аут гостя 60с) | `LobbyPresenter` + `UniTask.Delay` |
| FR-CN-06 (тайм-аут реконекту 60с) | `GamePresenter` + `ReconnectOverlay` + `UniTask.Delay` |
| FR-CN-07 (згортання = вихід) | `AppBootstrap.OnApplicationFocus` → `FirebaseSessionService.LeaveSession` |
| FR-MP (вибір карти) | `MapSelectPresenter` + `GameRules.ResolveMapConflict` |
| FR-SP-01..04 (drag-and-drop, валідація) | `SetupView` + `ShipDragHandler` + `BoardValidator` + `AutoPlacer` |
| FR-SP-05 (Готовий → блок) | `SetupPresenter` + `boardCommitted` flag |
| FR-GP-01 (перший хід випадково) | Хост: `Random.Range` → запис в `currentTurnUid` |
| FR-GP-02 (продовження при Hit) | `ShotResolver` + транзакція передачі ходу лише при Miss |
| FR-GP-03 (Sunk + дотичні) | `ShotResolver.MarkAdjacentAsMisses` |
| FR-GP-04 (форма не розкривається) | `BoardRenderer` показує лише окремі cells, без контуру корабля |
| FR-GP-05 (таймер 5хв) | `TurnTimerView` + `UniTask` локальний відлік від `turnStartedAtMs` |
| FR-GP-06 (попередження таймера) | `TurnTimerView.UpdateWarningState` |
| FR-GP-07 (Здатися) | `GamePresenter.OnSurrender` → запис у `/surrender` |
| FR-GP-08 (історія ходів) | `HistoryPanelView` + listener на `/shots` |
| FR-GP-09 (перемога) | `GameRules.CheckWinCondition` після кожного пострілу |
| FR-GP-10 (достроковий вихід) | `GameRules.DetermineWinnerByHits` |
| FR-EG-01..04 (результат, реванш) | `ResultPresenter` + `MapSelectPresenter` (для rematch) |
| FR-UI-01..02 (2D, portrait, layout) | Player Settings + Canvas з міні-картою |
| FR-UI-03 (double tap) | `GameView.OnPointerClick` з перевіркою попередньої клітинки |
| FR-UI-04..05 (звуки, анімації) | `SoundManager` + `ShotAnimator` (Animator + корутини) |
| FR-UI-06 (вібрація) | `VibrationManager` (нативний `Handheld.Vibrate()`) |
| FR-UI-07 (індикатор гравця) | `GameView` показує "Ваш хід" / "Хід супротивника" |
| FR-UI-08 (без чату) | Просто не реалізуємо |
| FR-ST (налаштування) | `SettingsPresenter` + `PlayerPrefsSettingsRepository` |
| FR-NF (i18n, latency, persistence) | Unity Localization + Firebase RTDB + PlayerPrefs |

---

## 8. Інфраструктура

### 8.1. AppBootstrap

Точка входу. Виконується на старті сцени `Main`:

1. Ініціалізація Firebase SDK (`FirebaseApp.CheckAndFixDependenciesAsync`)
2. Anonymous Sign-In
3. Завантаження `PlayerPrefs` (нік, налаштування)
    4. Ініціалізація Localization: при першому запуску визначає мову з `Application.systemLanguage` (`uk` якщо Ukrainian, інакше `en`); зберігає вибір у `PlayerPrefs`
5. Реєстрація сервісів у `ServiceLocator`
6. Перехід на `MenuPanel`

### 8.2. ServiceLocator

Простий статичний реєстр (без DI-фреймворку):

```csharp
public static class ServiceLocator
{
    public static ISessionService Session { get; set; }
    public static ISettingsRepository Settings { get; set; }
    public static SoundManager Sound { get; set; }
    public static MusicManager Music { get; set; }
    public static VibrationManager Vibration { get; set; }
    public static PanelRouter Router { get; set; }
}
```

Реєструється в `AppBootstrap.Awake()`. Достатньо для MVP, при потребі легко мігрувати на Zenject/VContainer.

### 8.3. Локалізація

Unity Localization Package. Дві StringTables: `Game (uk)`, `Game (en)`. При першому запуску мова визначається автоматично через `Application.systemLanguage`: Ukrainian → `uk`, будь-яка інша → `en`. Збережений вибір зчитується з `PlayerPrefs` при наступних запусках.

Доступ через `LocalizedString` у Inspector або статично:
```csharp
LocalizationSettings.StringDatabase.GetLocalizedString("Game", "menu.start");
```

Зміна мови (FR-ST) — `LocalizationSettings.SelectedLocale = ...` + збереження вибору в `PlayerPrefs`.

### 8.4. Аудіо

- `AudioMixer` з трьома групами: `Master`, `SFX`, `Music`
- `SoundManager` керує `SFX` гучністю; `MusicManager` — `Music`
- Звуки як `AudioClip` ScriptableObject-каталог (`AudioCatalog`)
- Гучності зберігаються в `PlayerPrefs` (FR-ST)

### 8.5. Вібрація

- Win прапорець `vibrationEnabled` у `PlayerPrefs`
- `Handheld.Vibrate()` — примітивна вібрація (тільки Android)
- Якщо знадобиться якісніша — мігрувати на Lofelt Nice Vibrations

---

## 9. Конфігурація проекту

### 9.1. Player Settings (Android)

| Параметр | Значення |
|---|---|
| Package Name | `com.zotovs.navy` |
| Minimum API Level | 33 (Android 13) |
| Target API Level | Latest (Android 14 / 35) |
| Scripting Backend | IL2CPP |
| Target Architectures | ARM64 (✅), ARMv7 (❌) |
| Default Orientation | Portrait |
| Allowed Orientations | Portrait тільки |
| Internet Access | Required |
| Write Permission | Internal Only |
| Splash Screen | Unity default (можна вимкнути на Personal) |

### 9.2. Quality Settings

- Texture Quality: Full Res
- Anti Aliasing: Disabled (2D, не потрібно)
- VSync: Every V Blank
- Target Frame Rate: 60 fps

### 9.3. Packages (`Packages/manifest.json`)

```json
{
  "dependencies": {
    "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask",
    "com.unity.localization": "1.5.3",
    "com.unity.textmeshpro": "3.0.9",
    "com.unity.inputsystem": "1.7.0",
    "com.unity.ugui": "2.0.0"
  }
}
```

> Firebase Unity SDK додається як `.unitypackage` через Unity Editor на PC #2 (не через Package Manager).

### 9.4. Firebase Unity SDK (імпортується вручну на PC #2)

- `FirebaseAuth.unitypackage`
- `FirebaseDatabase.unitypackage`

Після імпорту: `Assets/StreamingAssets/google-services.json` (з Firebase Console).

---

## 10. Workflow розробки

### 10.1. Розподіл задач

| PC | Задача |
|---|---|
| **PC #1** (без Unity, з AI) | Усі `.cs` файли в `Assets/Scripts/`, `Packages/manifest.json`, `ProjectSettings/ProjectVersion.txt`, `.gitignore`, документація |
| **PC #2** (з Unity Editor) | Створення сцени `Main.unity`, prefabs, ScriptableObjects, налаштування Inspector-референсів, Player Settings (Android, portrait), імпорт Firebase SDK, build APK, тестування на пристрої |

### 10.2. Git workflow

1. PC #2: `git init` → перший commit з мінімальним проектом → push на GitHub
2. PC #1: `git clone` → AI пише код → `git push`
3. PC #2: `git pull` → відкрити Unity → налаштувати сцени/референси → перевірити Console → commit `.meta` файли + сцени → push
4. PC #1: `git pull` → синхронізація `.meta`

### 10.3. .gitignore

Стандартний Unity `.gitignore` (Library/, Temp/, obj/, *.csproj, *.sln, Build/, Logs/, UserSettings/, .vs/, .idea/).

`google-services.json` — **не комітити в публічне репо**; для приватного — можна. Файл знаходиться у `Assets/StreamingAssets/`.

### 10.4. Перший прохід генерації

**Опція A — тільки скелет:**
- Структура папок `Assets/Scripts/...`
- `Packages/manifest.json`
- `ProjectSettings/ProjectVersion.txt`
- `.gitignore`
- `README.md`
- **Без жодних `.cs` файлів з логікою** — лише папки

Подальші проходи додають моделі, рушій, сервіси, presenters інкрементально.

---

## 11. Обмеження та ризики

| # | Ризик | Mitigation |
|---|---|---|
| 1 | Spark plan: 100 одночасних з'єднань → ≈50 партій паралельно | Якщо гра масштабується — перехід на Blaze (pay-as-you-go) |
| 2 | Peer-authoritative → можливість читерства через модифікований клієнт | Прийнятно для гри з друзями; античит поза обсягом |
| 3 | Локальні таймери на двох пристроях → дрейф годинників | Для 5-хвилинного інтервалу прийнятно (~секунди); якщо стане проблемою — перейти на Firebase ServerValue |
| 4 | AI без Unity не може перевірити компіляцію `.cs` | Помилки виявляться на PC #2 при відкритті проекту → виправляти ітеративно |
| 5 | AI не створює сцени/prefabs | Усі сцени та prefabs створюються вручну на PC #2 за інструкціями |
| 6 | Firebase Auth: анонімні UID можуть змінюватися при очищенні даних додатка | Прийнятно — гра не зберігає історію між сесіями |
| 7 | Згортання = вихід (FR-CN-07) — користувачі можуть випадково втратити партію (вхідний дзвінок) | Жорстке правило з FR; UX-попередження перед стартом партії можна додати |

---

## 12. Інструкції для AI на майбутні сесії

При генерації коду наступних модулів:

1. **Core** — суворо без `using UnityEngine;`. Тільки .NET Standard 2.1 + System.*.
2. **Async** — тільки `UniTask`/`UniTaskVoid`, не `Task`/`async void` (крім event handlers).
3. **Стиль:** PascalCase для типів та public методів, camelCase для приватних полів з префіксом `_`. Properties без бекинг-полів де можливо.
4. **MonoBehaviour:** мінімум логіки, делегувати в Presenter / Model.
5. **Серіалізація для RTDB:** окремі DTO в `Data/Firebase/Dto/`, мапер `DtoMapper`. Не серіалізувати Core-моделі напряму.
6. **Логування:** `UnityEngine.Debug.Log` обгорнути в умовний `#if UNITY_EDITOR || DEBUG_BUILD` для production.
7. **Винятки:** не кидати з UI-event handlers (Unity ковтає); ловити в Presenter, показувати модалку.
8. **Localization:** усі user-facing рядки — через `LocalizedString` чи `GetLocalizedString`. Не хардкодити.
9. **Збереження:** `PlayerPrefs.GetString/SetString` через `PlayerPrefsSettingsRepository`, не напряму з Presenter.
10. **Firebase:** всі виклики через `ISessionService` / `IAuthService`, не напряму з Presenter.

---

## 13. Глосарій

| Термін | Значення |
|---|---|
| Хост | Гравець, що створив сесію та згенерував код |
| Гість | Гравець, що приєднався за кодом |
| Сесія | Запис у `/sessions/{code}` у Firebase RTDB; одна партія |
| Дошка / Board | Поле гравця 8×8 / 10×10 / 12×12 з кораблями |
| Клітинка / Cell | Координата (x, y) на дошці |
| Хід / Turn | Серія пострілів одного гравця до першого Miss |
| Постріл / Shot | Один тап-вистріл по клітинці супротивника |
| Peer-authoritative | Кожен клієнт сам розраховує свою логіку, без сервера |
| RTDB | Firebase Realtime Database |
| MVP (тут) | Model-View-Presenter (архітектурний патерн) |
| MVP (продукт) | Minimum Viable Product (мінімальна робоча версія) |
