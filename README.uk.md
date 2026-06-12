# Navy — Морський бій для Android

Мобільна гра "Морський бій" для двох гравців на Android 13+. Гравці з'єднуються по мережі через 6-значний код сесії, переданий через будь-який месенджер (Viber, Telegram тощо).

## Робочий процес розробки

| Роль | Відповідальність |
|---|---|
| **PC #1** (AI / без Unity) | Всі `.cs`-скрипти, `manifest.json`, `ProjectSettings`, документація |
| **PC #2** (Unity Editor) | Налаштування сцени, prefabs, прив'язка Inspector-референсів, імпорт Firebase, збірка APK |

## Поза обсягом (v1.0)

AI / одиночний режим · глобальний рейтинг · облікові записи · iOS · внутрішньоігровий чат · збереження стану при згортанні

## Налаштування PC #2

Повна інструкція з налаштування Unity Editor — у файлі `dev-pc2-uk.md` (або `dev-pc2.md` англійською).

## Технічний стек

| Категорія | Технологія |
|---|---|
| Рушій | Unity 6000.2.10f1 LTS |
| Мова | C# (.NET Standard 2.1) |
| Архітектура | MVP (Model-View-Presenter) |
| Мережа | Firebase Realtime Database |
| Автентифікація | Firebase Anonymous Authentication |
| Async | UniTask (Cysharp) |
| UI | uGUI (Canvas / RectTransform) |
| Локалізація | Unity Localization Package |
| Збірка | IL2CPP, ARM64, Android 13+ (API 33+) |

## Структура проекту

```
Navy/
├── dev-pc2.md           ← Інструкція налаштування Unity Editor для PC #2 (англійська)
├── dev-pc2-uk.md        ← Інструкція налаштування Unity Editor для PC #2 (українська)
├── spec/
│   ├── functional.md    ← Функціональні вимоги (механіки, правила, UX)
│   └── tech.md          ← Технічна архітектура (шари MVP, схема Firebase, компоненти)
└── unity/               ← Корінь Unity-проекту — відкривати саме цю папку в Unity Hub
    ├── Assets/
    │   ├── Audio/           ← AudioMixer (GameMixer) та ScriptableObject AudioCatalog
    │   ├── Localization/    ← String Tables для української та англійської мов
    │   ├── Prefabs/         ← CellPrefab та інші префаби, що створюються під час гри
    │   ├── Scenes/          ← Main.unity — єдина сцена для всього додатка
    │   ├── Scripts/
    │   │   ├── Core/            ← Чиста ігрова логіка C# (без залежності від UnityEngine)
    │   │   ├── Data/            ← Firebase-сервіси, DTO, репозиторій PlayerPrefs
    │   │   ├── Presentation/    ← MonoBehaviour Views та Presenters для кожного екрана
    │   │   └── Infrastructure/  ← AppBootstrap, ServiceLocator, LocalizationBootstrap
    │   ├── Sprites/         ← Placeholder-спрайти, що генеруються в Unity
    │   └── StreamingAssets/ ← Сюди класти google-services.json (виключено з репо)
    ├── Packages/
    │   └── manifest.json    ← UniTask, Localization, TextMeshPro, Input System
    └── ProjectSettings/     ← Android Player Settings, Quality, Input — комітяться в репо
```
