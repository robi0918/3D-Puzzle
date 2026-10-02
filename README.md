# 3D-Puzzle-Explorer

Prototype game eksplorasi **third-person 3D** di Unity 6: pemain menjelajahi sebuah ruangan,
berinteraksi dengan objek 3D, lalu layar berlanjut ke **puzzle 2D berbasis UI** (Memory Match &
Sliding Puzzle). Selama puzzle berlangsung kontrol karakter **dibekukan**, dan setelah selesai atau
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

## 1. Requirement

| ID | Requirement | Status |
|----|-------------|--------|
| R1 | Karakter bergerak menjelajahi area 3D | ✅ |
| R2 | Interaksi dengan objek 3D (prompt + highlight) | ✅ |
| R3 | Interaksi → puzzle/mini game 2D berbasis UI | ✅ |
| R4 | Input karakter terkunci selama puzzle 2D | ✅ |
| R5 | Dua jenis puzzle 2D berbeda | ✅ |
| R5a | Puzzle 1 — Memory Match Card | ✅ |
| R5b | Puzzle 2 — Sliding Puzzle (selalu solvable) | ✅ |
| R6 | Selesai/batal → UI tutup, kontrol aktif kembali | ✅ |
| P1 | Arsitektur SOLID | ✅ |
| P2 | Visual minimalis, bukan greybox | ✅ (aset Cosmic Retro) |
| P3 | Basic visual effect | ✅ (highlight, VFX solved, animasi UI) |

---

## 2. Cara Menjalankan

### 2.1 Dari Editor
1. Buka project dengan Unity **6000.3.7f1** (import pertama akan mengunduh package).
2. `Window > TextMeshPro > Import TMP Essential Resources` (sekali saja — semua UI memakai TMP).
3. Buka scene `Assets/Scenes/Gameplay.unity` — seluruh hierarki UI, prefab, config, karakter, dan
   referensi Inspector sudah tersimpan di scene.
4. Tekan **Play**.

### 2.2 Build
`File > Build Profiles > Windows x86_64 > Build`. Scene `Gameplay.unity` sudah terdaftar di
Build Settings.

### 2.3 Verifikasi tanpa membuka Editor

```bash
bash Tools/check-compile.sh      # 45 source files, exit 0 (0 error, 0 warning)
python Tools/check-asmdefs.py    # 9 assemblies, 0 failed (graph assembly valid & bebas siklus)
bash Tools/run-model-tests.sh    # 17/17 PASS — aturan puzzle dieksekusi nyata (mono)
python Tools/check-references.py # audit GUID menggantung di scene/prefab/asset
```

Unit test EditMode: `Window > General > Test Runner > EditMode > Run All`.

---

## 3. Kontrol

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

## 4. Arsitektur

### 4.1 Assembly (Assembly Definition)

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
**tidak ada dependensi melingkar** — dibuktikan oleh `Tools/check-asmdefs.py`.

### 4.2 Alur utama

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

### 4.3 Penerapan SOLID

| Prinsip | Penerapan |
|---|---|
| **S**ingle Responsibility | `PlayerController` hanya motor + orientasi; `Interactor` hanya deteksi & memicu; `PuzzleService` hanya lifecycle puzzle; `MemoryMatchModel`/`SlidingPuzzleModel` hanya aturan main (plain C#); `*View` hanya presentasi; `GameBootstrapper` hanya merakit. Tidak ada God class (file terbesar: `PlayerController` 300 baris). |
| **O**pen/Closed | Menambah puzzle baru = membuat class `IPuzzle` baru + view baru; **tidak ada** perubahan pada `PuzzleService`, `Interactor`, `GameStateService`. Menambah objek interaktif baru = menambahkan `PuzzleInteractable` + factory-nya. |
| **L**iskov Substitution | `PuzzleService` memperlakukan semua `IPuzzle` identik — satu-satunya percabangan di seluruh alur puzzle adalah `Completed`/`Cancelled`, tidak ada `if (puzzle is X)`. Semua `IInteractable` saling menggantikan di `Interactor`. |
| **I**nterface Segregation | Interface kecil & fokus: `IInteractable` (5 anggota), `IPlayerInput`, `IPlayerAnimator`, `IPlayerAudio`, `IPuzzle`, `IPuzzleUI`, `IMemoryMatchView`, `ISlidingPuzzleView`, `IGameStateProvider`, `IInputLockable`. Tidak ada interface gemuk. |
| **D**ependency Inversion | Kelas high-level bergantung pada abstraksi. Semua dependensi disuplai lewat **satu composition root** (`GameBootstrapper`) memakai `Construct(...)`. Tidak ada singleton statis, tidak ada `Find*`, tidak ada service locator. `GameStateService` adalah objek plain C# yang dimiliki composition root. |

### 4.4 Keputusan desain penting

- **Model–View separation**: `MemoryMatchModel` dan `SlidingPuzzleModel` adalah plain C# tanpa
  dependensi Unity → bisa di-unit-test (17 test) dan dieksekusi di luar Editor.
- **Cancellation**: satu token per sesi puzzle (`CancellationTokenSource` linked) dari
  `PuzzleService`, diteruskan ke `puzzle.RunAsync`; `destroyCancellationToken` untuk objek yang
  hancur. Tidak ada async yang berjalan setelah objek hilang. `async void` tidak dipakai.
- **Blocking input saat animasi**: view hanya menerima klik ketika puzzle sedang menunggunya
  (`UniTaskCompletionSource`), sehingga klik saat animasi/jeda mismatch otomatis diabaikan —
  tanpa flag rumit.
- **Shuffle Sliding Puzzle selalu solvable**: board dikocok dengan N **langkah legal** dari kondisi
  solved, tanpa membalik langkah terakhir. Diverifikasi 500 seed (paritas inversi) dan dicocokkan
  dengan pencarian breadth-first independen.
- **Mode kamera**: `CameraManager` hanya memindahkan priority rig Cinemachine; pitch kamera adalah
  state pemain sehingga dikelola `PlayerController` pada camera target.

---

## 5. Struktur Folder

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
├── check-asmdefs.py        kompilasi tiap asmdef terpisah → memvalidasi graph assembly
├── check-references.py     memindai GUID menggantung di scene/prefab/asset (Missing Script)
├── run-model-tests.sh      menjalankan 17 aturan puzzle di luar Unity
├── ModelTests/ModelTests.cs
├── generate-puzzle-art.py  membuat ulang sprite ikon/tile (Pillow) + pratinjau
└── art-preview.png         contact sheet hasil generate-puzzle-art.py
```

---

## 6. Optimisasi

### 6.1 Diterapkan (dapat diperiksa di kode)

| Area | Tindakan |
|---|---|
| Physics | `Physics.OverlapSphereNonAlloc` + buffer 8 collider yang di-cache, **layer mask** khusus, deteksi ber-**interval 0.1 s** (bukan tiap frame) |
| GC / alokasi | Tidak ada `new`/string interpolation di jalur panas; tidak ada LINQ, `Find*`, atau `GetComponent` di `Update`; `Camera.main` di-cache sekali di `Awake` |
| Animator | `StringToHash` sekali di `Awake`; nilai hanya ditulis saat berubah |
| UI | Label HUD hanya di-`SetText` saat nilainya berubah; `CanvasGroup` + SetActive, bukan Instantiate/Destroy |
| Pooling | `CardView`/`SlidingTileView` di-*reuse* antar sesi puzzle (tidak ada churn GameObject) |
| Layout | Grid kartu memakai `GridLayoutGroup` statis; tile sliding diposisikan aritmetik (tidak ada layout rebuild saat animasi) |
| Event | Semua `+=` punya `-=` di `OnDisable`/`OnDestroy` |
| Async | `UniTask.WaitForSeconds`/`Delay`/`Yield` dengan token; tidak ada Coroutine |

### 6.2 Hasil Profiler

> Profiling memerlukan Unity Profiler di Editor dan Development Build. Setelah profiling,
> isi tabel ini dengan angka nyata:

| Skenario | Sebelum | Sesudah | Metode |
|---|---|---|---|
| GC Alloc/frame — eksplorasi | *(isi)* | *(isi)* | Profiler, 30 s rekaman |
| GC Alloc/frame — puzzle idle | *(isi)* | *(isi)* | Profiler |

---

## 7. Build & Demo

| Item | Tautan |
|---|---|
| Build Windows x86_64 | *(isi: GitHub Releases)* |

### Video demo

<video src="Demo.mp4" controls width="100%"></video>

Alur: eksplorasi → Memory Match (+ bukti freeze input) → Sliding Puzzle → VFX.

---

## 8. Kredit

Semua aset & plugin pihak ketiga beserta lisensinya ada di **[`CREDITS.md`](CREDITS.md)**.
