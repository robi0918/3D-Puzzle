# 3D-Puzzle-Explorer

A **third-person 3D** exploration game prototype built in Unity: players explore a room, interact with 3D objects, and transition to **UI-based 2D puzzles** (Memory Match & Sliding Puzzle). Player controls are **frozen** during puzzle gameplay and resume once the puzzle is completed or cancelled.

| | |
|---|---|
| **Unity** | 6000.3.7f1 (Unity 6 LTS) |
| **Render Pipeline** | Universal Render Pipeline 17.3.0 |
| **Kamera** | Cinemachine 3.1.7 (`CinemachineCamera`) |
| **Input** | New Input System 1.18.0 |
| **UI** | uGUI + TextMeshPro |
| **Async** | UniTask 2.5.10 (tanpa Coroutine) |
| **Target** | Windows x86_64 |

---

## 1. Getting Started

### Running from Editor
1. Open the project using Unity **6000.3.7f1** (the initial import will download all required packages).
2. Go to `Window > TextMeshPro > Import TMP Essential Resources` (one-time setup — all UI elements use TMP).
3. Open the scene `Assets/Scenes/Gameplay.unity` — all UI hierarchies, prefabs, configs, character setups, and Inspector references are pre-configured in this scene.
4. Press **Play**.
   
---

## 2. Controls

| Action | Input |
|---|---|
| Movement | `W A S D` |
| Look / Camera Orbit | Mouse |
| Jump | `Space` |
| Sprint | `Left Shift` |
| Interact | `E` |
| Toggle Camera (Third-Person ↔ Top-Down) | `V` |
| Cancel Puzzle | `ESC` or **Cancel** button |

---

## 3. Architecture

### 3.1 Assembly Definitions (Asmdefs)

```
Game.Core ─────────────► IGameStateProvider, GameStateService, IInputLockable, UiTween
    ▲   ▲   ▲   ▲
    │   │   │   └── Game.UI ──────────► PuzzleWindow, UIRoot, InteractionPromptView
    │   │   └────── Game.Puzzle ──────► IPuzzle/PuzzleService, MemoryMatch*, SlidingPuzzle*,
    │   │                               PuzzleInteractable
    │   └────────── Game.Interaction ─► IInteractable, Interactor, InteractableBase
    └────────────── Game.Player ──────► IPlayerInput, PlayerInputReader/Source, PlayerController,
                                        CameraManager, PlayerAnimator, PlayerAudioManager
                                           ▲
Game.Bootstrap ─────────────────────────────┘  (composition root — top-level, no incoming dependencies)
Game.Puzzle.Tests  ──► EditMode tests (Editor-only)
StarterAssets      ──► Starter Assets codebase (isolated from Assembly-CSharp)
```

Dependencies strictly flow inward (`Game.Core` has no knowledge of any module above it), ensuring **zero circular dependencies**.

### 3.2 Main Flow

```
Exploration
  └─ Interactor (OverlapSphereNonAlloc every 0.1s, Interactable layer, view cone filter)
       ├─ OnInteractableFound → InteractionPromptView displays prompt
       └─ tombol E → IInteractable.Interact() → PuzzleInteractable
            └─ PuzzleService.RunAsync(puzzle, ct)          // UniTask<PuzzleResult>
                 ├─ GameStateService.EnterPuzzle()
                 │     └─ GameBootstrapper → IInputLockable.SetInputLocked(true) on
                 │        PlayerController + Interactor; UIRoot → release cursor lock
                 ├─ PuzzleWindow.OpenAsync("<judul>")
                 ├─ await puzzle.RunAsync(ct)   // MemoryMatch / SlidingPuzzle
                 └─ finally (always executes, including cancel/error):
                      PuzzleWindow.CloseAsync() → EnterExploration() → restore input + lock cursor
Exploration (solved object → prompt disabled + play solved VFX)
```

### 3.3 SOLID Principles Implementation

| Principle | Implementation |
|---|---|
| **S**ingle Responsibility | `PlayerController` handles only locomotion & orientation; `Interactor` handles detection & triggering; `PuzzleService` manages puzzle lifecycle; `MemoryMatchModel`/`SlidingPuzzleModel` contain pure C# domain rules; `*View` components handle presentation; `GameBootstrapper` handles assembly. No God classes. |
| **O**pen/Closed | Adding a new puzzle requires creating a new `IPuzzle` class + view — **zero modifications** to `PuzzleService`, `Interactor`, or `GameStateService`. Adding new interactable objects requires adding a `PuzzleInteractable` + its factory. |
| **L**iskov Substitution | `PuzzleService` treats all `IPuzzle` implementations identically. The only branching logic across the puzzle flow is `Completed`/`Cancelled` — no `if (puzzle is X)`. All `IInteractable` instances are interchangeable within `Interactor`. |
| **I**nterface Segregation | Interfaces are granular and focused: `IInteractable`, `IPlayerInput`, `IPlayerAnimator`, `IPlayerAudio`, `IPuzzle`, `IPuzzleUI`, `IMemoryMatchView`, `ISlidingPuzzleView`, `IGameStateProvider`, `IInputLockable`. No fat interfaces. |
| **D**ependency Inversion | High-level modules depend on abstractions. Dependencies are injected via a **single composition root** (`GameBootstrapper`) using `Construct(...)`. No static singletons, `Find*` calls, or service locators. `GameStateService` is a plain C# object managed by the composition root. |

### 3.4 Key Design Decisions

- **Model–View Separation**: `MemoryMatchModel` and `SlidingPuzzleModel` are plain C# classes without Unity dependencies, making them fully unit-testable.
- **Cancellation**: A single linked `CancellationTokenSource` per puzzle session managed by `PuzzleService` is passed to `puzzle.RunAsync`, alongside `destroyCancellationToken` for object destruction. Prevents async leaks after object destruction. `async void` is strictly avoided.
- **Input Blocking During Animations**: Views only accept click inputs while actively awaiting player action (`UniTaskCompletionSource`), automatically ignoring clicks during animations or mismatch delay states.
- **Guaranteed Solvable Sliding Puzzles**: Board shuffling executes N **valid legal moves** from a solved state without reversing the immediately preceding move.
- **Camera System**: `CameraManager` toggles Cinemachine rig priorities, while pitch state is owned by `PlayerController` via the camera target.

---

## 4. Project Structure

```
Assets/
├── _Project/
│   ├── Scripts/
│   │   ├── Core/           Game.Core
│   │   ├── Player/         Game.Player
│   │   ├── Interaction/    Game.Interaction
│   │   ├── Puzzle/         Game.Puzzle
│   │   │   ├── Abstractions/   Services/   MemoryMatch/   SlidingPuzzle/
│   │   ├── UI/             Game.UI
│   │   └── Bootstrap/      Game.Bootstrap  (composition root)
│   ├── Art/Puzzle/         Card icon sprites, tiles, card backs
│   ├── Data/               ScriptableObject (MemoryMatchConfig, SlidingPuzzleConfig)
│   └── Tests/EditMode/     Game.Puzzle.Tests
├── Data/                   ScriptableObjects (created via Create menu)
├── Prefabs/  Scenes/  Settings/  Materials/
├── ThirdParty/             StarterAssets, Cosmic Retro, Anime Girl  (lihat CREDITS.md)
└── 3D/                     Character models + animations
Tools/
├── check-compile.sh        Compiles all scripts into a single assembly (Unity errors = build errors here)
└── check-asmdefs.py        Compiles each asmdef independently to validate the assembly graph
```

---

## 5. Performance & Optimization

### 5.1 Applied Optimizations (In Code)

| Category | Optimization Strategy |
|---|---|
| Physics | `Physics.OverlapSphereNonAlloc` with a cached 8-element buffer, dedicated **layer mask**, evaluated at **0.1s intervals** |
| GC / Allocations | Zero `new` allocations/string interpolations in hot paths; no LINQ, `Find*`, or `GetComponent` in `Update`; `Camera.main` cached in `Awake` |
| Animator | `StringToHash` cached in `Awake`; values updated only on change detection |
| UI | HUD labels use `SetText` only when values change; `CanvasGroup` + `SetActive` toggling instead of Instantiate/Destroy overhead |
| Pooling | `CardView`/`SlidingTileView` instances recycled across puzzle sessions |
| Layout | Card grids utilize static `GridLayoutGroup`; sliding tiles rely on explicit arithmetic positioning (no layout rebuilds during animation) |
| Events | All `+=` subscriptions have matching `-=` unsubscribers in `OnDisable`/`OnDestroy` |
| Async | `UniTask.WaitForSeconds`/`Delay`/`Yield` with cancellation tokens; complete elimination of Coroutines |

---

## 6. Build & Demo

| Asset | Link |
|---|---|
| Windows x86_64 Build | https://drive.google.com/file/d/1dWbiEPVTmdoV4GSdH1QEVToABZ357Em0/view?usp=sharing |

### Demo Video

https://github.com/user-attachments/assets/20d05e70-9b7d-4a69-84cc-2e06d7615dfe





---

## 7. Credits

All third-party assets, libraries, and their respective licenses are documented in **[`CREDITS.md`](CREDITS.md)**.
