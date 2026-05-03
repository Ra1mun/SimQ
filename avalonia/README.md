# SimQ — Avalonia Client

Кроссплатформенный десктопный клиент для системы моделирования СМО (Theory of Queueing Systems / Теория массового обслуживания), переписанный из HTML‑прототипа на **Avalonia 11** + **C# 12** (.NET 8) с использованием паттерна **MVVM** и **CommunityToolkit.Mvvm**.

> Это первая итерация порта HTML‑макета `SimQ.html` в полноценное Avalonia‑приложение.  
> Вся логика моделирования остаётся за HTTP‑сервисами `SimQ.Core` и `SimQ.Problems`.

---

## Структура решения

```
avalonia/
└── SimQ.Client/
    ├── SimQ.Client.csproj          ← .NET 8, Avalonia 11.2.1
    ├── Program.cs                  ← entry point + AppBuilder
    ├── App.axaml(.cs)              ← FluentTheme(Light) + наши стили
    ├── app.manifest                ← Per‑Monitor V2 DPI
    │
    ├── Models/                     ← POCO‑модели предметной области
    │   ├── Enums.cs                  AgentKind / ProblemStatus / RunStatus / DistributionKind
    │   ├── DistributionParams.cs     M / D / Bernoulli / Beta / G + .Format()
    │   ├── Problem.cs                Agent / Edge / Problem (+ RebuildEdgeAnchors)
    │   ├── RunRecord.cs              запуск + строка результата (P(N))
    │   └── SampleData.cs             3 demo‑задачи + история запусков + P(N)
    │
    ├── ViewModels/                 ← MVVM, [ObservableProperty] / [RelayCommand]
    │   ├── AgentViewModel.cs
    │   ├── ProblemViewModel.cs
    │   └── MainViewModel.cs        корневой VM, экраны, симуляция, тосты
    │
    ├── Views/                      ← AXAML + code‑behind
    │   ├── MainWindow.axaml          оконная рамка Win11 + appbar + tabs + status
    │   ├── EditorView.axaml          граф‑редактор (canvas + узлы + рёбра + лог)
    │   ├── SimulateView.axaml        запуск, прогресс, метрики, лог
    │   ├── ResultsView.axaml         таблица P(N) + барчарт
    │   ├── TasksView.axaml           история запусков
    │   └── WizardView.axaml          мастер «Новая задача»
    │
    ├── Styles/
    │   ├── Theme.axaml              цвета (Bg/Panel/Accent/agent‑swatches), типографика
    │   └── Controls.axaml           .primary / .secondary / .toolbar / .tab / .nav / .card
    │
    └── Converters/Converters.cs    AgentKind→Brush, RunStatus→Brush/Label, Progress→Width…
```

---

## Сборка и запуск

```bash
cd avalonia/SimQ.Client
dotnet restore
dotnet run
```

Поддерживается Windows / macOS / Linux. На Windows приложение использует **расширенную клиентскую область** с собственными кнопками управления окном (—, ▢, ✕) — в стиле Win11.

### Зависимости (NuGet)

| Пакет | Версия | Назначение |
|---|---|---|
| `Avalonia` | 11.2.1 | Базовый рантайм |
| `Avalonia.Desktop` | 11.2.1 | Win/macOS/Linux backend |
| `Avalonia.Themes.Fluent` | 11.2.1 | Базовая тема (Light) |
| `Avalonia.Fonts.Inter` | 11.2.1 | Шрифт Inter |
| `Avalonia.ReactiveUI` | 11.2.1 | UI‑schedule для async |
| `CommunityToolkit.Mvvm` | 8.3.2 | `[ObservableProperty]`, `[RelayCommand]` |

---

## Архитектура

* **MVVM, source‑generated.** `MainViewModel` хранит список задач, текущую задачу, выбранного агента, состояние симуляции и режим экрана. Свойства генерируются через `[ObservableProperty]`, команды — через `[RelayCommand]`.
* **Один корневой VM, переключение экранов через флаги.** `IsEditor`/`IsSimulate`/… биндятся на `IsChecked` вкладок и навигационной панели — сохраняем состояние при переключении.
* **Граф моделируется в `Canvas`.** Узлы и рёбра — два `ItemsControl` с `Canvas` в `ItemsPanelTemplate`; позиции узлов заданы через `Canvas.Left`/`Canvas.Top`, точки рёбер пересчитываются `Problem.RebuildEdgeAnchors()` при изменении координат.
* **Темизация — наши ресурсы.** Все цвета и кисти лежат в `Styles/Theme.axaml`; в `Styles/Controls.axaml` — стили кнопок, табов, карточек и т.д. Переключаемый акцентный оттенок (Tweaks) пока хранится в VM как `AccentHue` и применяется к кнопкам палитры.

---

## Что НЕ перенесено (сознательно, для последующих итераций)

1. **HTTP‑клиенты** к `SimQ.Core` / `SimQ.Problems` — сейчас всё на `SampleData`.
2. **Drag‑and‑drop узлов** на канвасе — узлы кликабельны, но не перетаскиваются (нужен behavior на `Canvas` + захват указателя).
3. **Реальный график** P(N) — пока бар‑чарт через `UniformGrid`. Под полноценный график предлагается `LiveCharts2.Avalonia` или `OxyPlot.Avalonia`.
4. **Form‑validation** в мастере и редакторе свойств.
5. **Сохранение/загрузка** задачи (JSON) — есть только тост «Сохранено».
6. **Локализация.** Все строки сейчас зашиты на русском.

---

## Соответствие HTML‑прототипу

| HTML | Avalonia |
|---|---|
| Title‑bar Win11 | `MainWindow` row 0, `OnTitleBarPointerPressed` → `BeginMoveDrag` |
| App‑bar (бренд + крошки + действия) | `MainWindow` row 1 |
| Tabs «Редактор / Моделирование / Результаты / История / Новая» | `MainWindow` row 2 (5× `ToggleButton.tab`) |
| Nav rail слева | колонка 0 главной сетки контента (5× `ToggleButton.nav`) |
| Список задач + canvas + properties | `EditorView` |
| Запуск, прогресс‑бар, метрики, лог | `SimulateView` |
| P(N) таблица + бары + chart | `ResultsView` |
| История запусков | `TasksView` |
| Мастер 1/3 | `WizardView` |
| Tweaks (акцентный цвет) | overlay в `MainWindow` |
| Toast | overlay в `MainWindow` |

---

## Лицензия

Внутренний проект ТГУ. Все права защищены.
