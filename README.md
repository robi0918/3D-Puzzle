# 3D-Puzzle-Explorer

Prototype game eksplorasi **third-person 3D** di Unity: pemain menjelajahi sebuah ruangan,
berinteraksi dengan objek 3D, lalu layar berlanjut ke **puzzle 2D berbasis UI** (Memory Match &
Sliding Puzzle). Selama puzzle berlangsung kontrol karakter **di freeze**, dan setelah selesai atau
dibatalkan kontrol kembali aktif.

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

## 1. Cara Menjalankan

### Dari Editor
1. Buka project dengan Unity **6000.3.7f1** (import pertama akan mengunduh package).
2. `Window > TextMeshPro > Import TMP Essential Resources` (sekali saja — semua UI memakai TMP).
3. Buka scene `Assets/Scenes/Gameplay.unity` — seluruh hierarki UI, prefab, config, karakter, dan
   referensi Inspector sudah tersimpan di scene.
4. Tekan **Play**.
   
---

## 2. Kontrol

| Aksi | Tombol |
|---|---|
| Bergerak | `W A S D` |
| Melihat / memutar kamera | Mouse |
| Lompat | `Space` |
| Lari | `Left Shift` |
| Interaksi | `E` |
| Ganti kamera (Third-Person ↔ Top-Down) | `V` |
| Batalkan puzzle | `ESC` atau tombol **Batal** |

---

## 3. Arsitektur

### 3.1 Assembly (Assembly Definition)

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
Game.Bootstrap ─────────────────────────────┘  (composition root — paling atas, tanpa dependensi masuk)
Game.Puzzle.Tests  ──► EditMode tests (Editor-only)
StarterAssets      ──► kode Starter Assets (dipisah agar tidak bercampur Assembly-CSharp)
```

Dependensi hanya mengarah ke dalam (Game.Core tidak mengetahui apa pun di atasnya) sehingga
**tidak ada dependensi melingkar**.

### 3.2 Alur utama

```
Exploration
  └─ Interactor (OverlapSphereNonAlloc tiap 0.1 s, layer Interactable, cone pandang)
       ├─ OnInteractableFound → InteractionPromptView menampilkan prompt
       └─ tombol E → IInteractable.Interact() → PuzzleInteractable
            └─ PuzzleService.RunAsync(puzzle, ct)          // UniTask<PuzzleResult>
                 ├─ GameStateService.EnterPuzzle()
                 │     └─ GameBootstrapper → IInputLockable.SetInputLocked(true) pada
                 │        PlayerController + Interactor; UIRoot → cursor bebas
                 ├─ PuzzleWindow.OpenAsync("<judul>")
                 ├─ await puzzle.RunAsync(ct)   // MemoryMatch / SlidingPuzzle
                 └─ finally (selalu, termasuk cancel/error):
                      PuzzleWindow.CloseAsync() → EnterExploration() → input aktif + cursor terkunci
Exploration (objek solved → prompt mati + VFX solved)
```

### 3.3 Penerapan SOLID

| Prinsip | Penerapan |
|---|---|
| **S**ingle Responsibility | `PlayerController` hanya motor + orientasi; `Interactor` hanya deteksi & memicu; `PuzzleService` hanya lifecycle puzzle; `MemoryMatchModel`/`SlidingPuzzleModel` hanya aturan main (plain C#); `*View` hanya presentasi; `GameBootstrapper` hanya merakit. Tidak ada God class. |
| **O**pen/Closed | Menambah puzzle baru = membuat class `IPuzzle` baru + view baru; **tidak ada** perubahan pada `PuzzleService`, `Interactor`, `GameStateService`. Menambah objek interaktif baru = menambahkan `PuzzleInteractable` + factory-nya. |
| **L**iskov Substitution | `PuzzleService` memperlakukan semua `IPuzzle` identik, satu-satunya percabangan di seluruh alur puzzle adalah `Completed`/`Cancelled`, tidak ada `if (puzzle is X)`. Semua `IInteractable` saling menggantikan di `Interactor`. |
| **I**nterface Segregation | Interface kecil & fokus: `IInteractable`, `IPlayerInput`, `IPlayerAnimator`, `IPlayerAudio`, `IPuzzle`, `IPuzzleUI`, `IMemoryMatchView`, `ISlidingPuzzleView`, `IGameStateProvider`, `IInputLockable`. Tidak ada interface gemuk. |
| **D**ependency Inversion | Class high-level bergantung pada abstraksi. Semua dependensi disuplai lewat **satu composition root** (`GameBootstrapper`) memakai `Construct(...)`. Tidak ada singleton statis, tidak ada `Find*`, tidak ada service locator. `GameStateService` adalah objek plain C# yang dimiliki composition root. |

### 3.4 Keputusan desain penting

- **Model–View separation**: `MemoryMatchModel` dan `SlidingPuzzleModel` adalah plain C# tanpa
  dependensi Unity → bisa di-unit-test.
- **Cancellation**: satu token per sesi puzzle (`CancellationTokenSource` linked) dari
  `PuzzleService`, diteruskan ke `puzzle.RunAsync`; `destroyCancellationToken` untuk objek yang
  hancur. Tidak ada async yang berjalan setelah objek hilang. `async void` tidak dipakai.
- **Blocking input saat animasi**: view hanya menerima klik ketika puzzle sedang menunggunya
  (`UniTaskCompletionSource`), sehingga klik saat animasi/jeda mismatch otomatis diabaikan.
- **Shuffle Sliding Puzzle selalu solvable**: board dikocok dengan N **langkah legal** dari kondisi
  solved, tanpa membalik langkah terakhir.
- **Mode kamera**: `CameraManager` hanya memindahkan priority rig Cinemachine; pitch kamera adalah
  state pemain sehingga dikelola `PlayerController` pada camera target.

---

## 4. Struktur Folder

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
│   ├── Art/Puzzle/         sprite ikon kartu, tile, punggung kartu
│   ├── Data/               ScriptableObject (MemoryMatchConfig, SlidingPuzzleConfig)
│   └── Tests/EditMode/     Game.Puzzle.Tests
├── Data/                   ScriptableObject (dibuat lewat menu Create)
├── Prefabs/  Scenes/  Settings/  Materials/
├── ThirdParty/             StarterAssets, Cosmic Retro, Anime Girl  (lihat CREDITS.md)
└── 3D/                     model + animasi karakter
Tools/
├── check-compile.sh        kompilasi semua script jadi satu assembly (error Unity = error di sini)
└── check-asmdefs.py        kompilasi tiap asmdef terpisah → memvalidasi graph assembly
```

---

## 5. Optimisasi

### 5.1 Diterapkan (di code)

| Area | Tindakan |
|---|---|
| Physics | `Physics.OverlapSphereNonAlloc` + buffer 8 collider yang di-cache, **layer mask** khusus, deteksi ber-**interval 0.1 s** |
| GC / alokasi | Tidak ada `new`/string interpolation di jalur panas; tidak ada LINQ, `Find*`, atau `GetComponent` di `Update`; `Camera.main` di-cache sekali di `Awake` |
| Animator | `StringToHash` sekali di `Awake`; nilai hanya ditulis saat berubah |
| UI | Label HUD hanya di-`SetText` saat nilainya berubah; `CanvasGroup` + SetActive, bukan Instantiate/Destroy |
| Pooling | `CardView`/`SlidingTileView` di-*reuse* antar sesi puzzle |
| Layout | Grid kartu memakai `GridLayoutGroup` statis; tile sliding diposisikan aritmetik (tidak ada layout rebuild saat animasi) |
| Event | Semua `+=` punya `-=` di `OnDisable`/`OnDestroy` |
| Async | `UniTask.WaitForSeconds`/`Delay`/`Yield` dengan token; tidak ada Coroutine |

---

## 6. Build & Demo

| Item | Tautan |
|---|---|
| Build Windows x86_64 |

### Video demo

<video src="Demo.mp4" controls width="100%"></video>

---

## 7. Credits

Semua aset & plugin pihak ketiga beserta lisensinya ada di **[`CREDITS.md`](CREDITS.md)**.
