using System.Collections.Generic;
using Game.Core;
using Game.Interaction;
using Game.Player;
using Game.Puzzle;
using Game.UI;
using UnityEngine;

namespace Game.Bootstrap
{
    /// <summary>
    /// Composition Root: satu-satunya tempat seluruh keterhubungan objek disusun.
    /// </summary>
    /// <remarks>
    /// Semua dependensi dihubungkan di sini lewat pemanggilan eksplisit <c>Construct</c> — tanpa singleton,
    /// tanpa service locator, tanpa <c>Find*</c>. Berjalan pada <see cref="DefaultExecutionOrder"/> -1000
    /// agar dependensi siap sebelum <c>Awake</c> atau <c>Start</c> komponen lain dipanggil.<br/>
    /// Komponen ini juga memegang langganan untuk membekukan pemain saat puzzle terbuka, sehingga kode pemain,
    /// kamera, dan interaksi tidak perlu mengetahui sistem puzzle.
    /// </remarks>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameBootstrapper : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField] private PlayerController _playerController;
        [SerializeField] private CameraManager _cameraManager;
        [SerializeField] private Interactor _interactor;

        [Header("UI")]
        [SerializeField] private PuzzleWindow _puzzleWindow;
        [SerializeField] private UIRoot _uiRoot;

        [Header("Puzzle 1 — Memory Match")]
        [SerializeField] private MemoryMatchConfig _memoryMatchConfig;
        [SerializeField] private MemoryMatchView _memoryMatchView;

        [Header("Puzzle 2 — Sliding Puzzle")]
        [SerializeField] private SlidingPuzzleConfig _slidingPuzzleConfig;
        [SerializeField] private SlidingPuzzleView _slidingPuzzleView;

        [Header("Host Puzzle (objek 3D di scene)")]
        [Tooltip("Objek A — Interact Memory Match.")]
        [SerializeField] private PuzzleInteractable _memoryMatchInteractable;
        [Tooltip("Objek B — Interact Sliding Puzzle.")]
        [SerializeField] private PuzzleInteractable _slidingPuzzleInteractable;

        private GameStateService _gameState;
        private PuzzleService _puzzleService;

        private void Awake()
        {
            _gameState = new GameStateService();

            // Satu instance input bersama untuk semua konsumen: dua reader pada perangkat
            // yang sama akan memicu permintaan interaksi dua kali.
            IPlayerInput input = ResolveInput();

            _playerController?.Construct(input, _cameraManager);
            _interactor?.Construct(input);
            _cameraManager?.Construct(_playerController);

            _puzzleService = new PuzzleService(_gameState, _puzzleWindow);
            _uiRoot?.Construct(_gameState);

            WirePuzzleHosts();

            _gameState.OnStateChanged += OnStateChanged;
            OnStateChanged(_gameState.CurrentState);

            ReportUnassignedReferences();
        }

        private void OnDestroy()
        {
            if (_gameState != null)
            {
                _gameState.OnStateChanged -= OnStateChanged;
                _gameState = null;
            }

            _puzzleService?.Dispose();
            _puzzleService = null;
        }

        /// <summary>Membekukan/memulihkan semua konsumen input saat state game berubah.</summary>
        private void OnStateChanged(GameState state)
        {
            bool locked = state == GameState.Puzzle;

            _playerController?.SetInputLocked(locked);
            _interactor?.SetInputLocked(locked);
        }

        private void WirePuzzleHosts()
        {
            if (_memoryMatchConfig != null && _memoryMatchView != null)
            {
                _memoryMatchView.Prewarm(_memoryMatchConfig.CardCount);
            }

            if (_slidingPuzzleConfig != null && _slidingPuzzleView != null)
            {
                _slidingPuzzleView.Prewarm(_slidingPuzzleConfig.Size * _slidingPuzzleConfig.Size - 1);
            }

            if (_memoryMatchInteractable != null && _memoryMatchConfig != null && _memoryMatchView != null)
            {
                _memoryMatchInteractable.Construct(_puzzleService,
                    () => new MemoryMatchPuzzle(_memoryMatchConfig, _memoryMatchView));
            }

            if (_slidingPuzzleInteractable != null && _slidingPuzzleConfig != null && _slidingPuzzleView != null)
            {
                _slidingPuzzleInteractable.Construct(_puzzleService,
                    () => new SlidingPuzzle(_slidingPuzzleConfig, _slidingPuzzleView));
            }
        }

        private IPlayerInput ResolveInput()
        {
            if (_playerController == null) return null;

            PlayerInputSource source = _playerController.GetComponentInChildren<PlayerInputSource>(true);
            return source != null ? source.Input : null;
        }

        /// <summary>
        /// Memberi peringatan satu kali tentang referensi Inspector yang kosong. Scene yang terhubung
        /// dengan benar tidak akan memunculkan log ini.
        /// </summary>
        private void ReportUnassignedReferences()
        {
            List<string> missing = new List<string>();

            if (_playerController == null) missing.Add(nameof(_playerController));
            if (_cameraManager == null) missing.Add(nameof(_cameraManager));
            if (_interactor == null) missing.Add(nameof(_interactor));
            if (_puzzleWindow == null) missing.Add(nameof(_puzzleWindow));
            if (_uiRoot == null) missing.Add(nameof(_uiRoot));
            if (_memoryMatchConfig == null) missing.Add(nameof(_memoryMatchConfig));
            if (_memoryMatchView == null) missing.Add(nameof(_memoryMatchView));
            if (_slidingPuzzleConfig == null) missing.Add(nameof(_slidingPuzzleConfig));
            if (_slidingPuzzleView == null) missing.Add(nameof(_slidingPuzzleView));
            if (_memoryMatchInteractable == null) missing.Add(nameof(_memoryMatchInteractable));
            if (_slidingPuzzleInteractable == null) missing.Add(nameof(_slidingPuzzleInteractable));

            if (missing.Count == 0) return;

            Debug.LogWarning($"[GameBootstrapper] Unassigned references: {string.Join(", ", missing)}.");
        }
    }
}
