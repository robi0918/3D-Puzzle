using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Game.Puzzle
{
    /// <summary>
    /// UI Sliding Puzzle: membangun papan, melaporkan klik tile, dan memainkan animasi geser.
    /// </summary>
    /// <remarks>
    /// Tile memakai object pool dan selalu dipakai ulang. Posisinya dihitung sekali per build lewat
    /// aritmetika (tanpa LayoutGroup, yang akan rebuild tiap frame saat tile beranimasi). Klik hanya
    /// diteruskan ketika puzzle sedang menunggu satu klik, sehingga input tertahan selama tile bergeser.
    /// </remarks>
    public sealed class SlidingPuzzleView : MonoBehaviour, ISlidingPuzzleView
    {
        [Header("Wiring")]
        [SerializeField] private RectTransform _contentRoot;
        [Tooltip("Induk tempat tile ditempatkan. Default memakai content root.")]
        [SerializeField] private RectTransform _boardRoot;
        [SerializeField] private SlidingTileView _tilePrefab;

        [Header("Layout (dihitung di code, tanpa LayoutGroup)")]
        [SerializeField] private Vector2 _tileSize = new Vector2(140f, 140f);
        [SerializeField] private float _tileSpacing = 8f;

        [Header("Tempo")]
        [SerializeField] private float _moveDuration = 0.14f;

        [Header("HUD")]
        [SerializeField] private TMP_Text _movesLabel;
        [SerializeField] private string _movesFormat = "Moves: {0}";

        private readonly List<SlidingTileView> _tiles = new List<SlidingTileView>();
        private SlidingTileView[] _tileBySlot;

        private UniTaskCompletionSource<int> _pendingClick;
        private int _size;
        private int _lastMoves = -1;

        /// <inheritdoc />
        public async UniTask ShowAsync(CancellationToken cancellationToken)
        {
            if (_contentRoot != null) _contentRoot.gameObject.SetActive(true);

            _pendingClick = null;
            _lastMoves = -1;

            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }

        /// <inheritdoc />
        public UniTask HideAsync(CancellationToken cancellationToken)
        {
            _pendingClick = null;

            if (_contentRoot != null) _contentRoot.gameObject.SetActive(false);

            return UniTask.CompletedTask;
        }

        /// <inheritdoc />
        public void Build(int size, int[] board, Sprite[] spritesByValue)
        {
            _size = Mathf.Max(2, size);
            _tileBySlot = new SlidingTileView[_size * _size];

            int used = 0;

            for (int slot = 0; slot < board.Length; slot++)
            {
                int value = board[slot];

                if (value == 0)
                {
                    _tileBySlot[slot] = null;
                    continue;
                }

                SlidingTileView tile = GetOrCreateTile(used);
                used++;

                tile.gameObject.SetActive(true);
                tile.Bind(slot, value, SpriteFor(spritesByValue, value));
                tile.SetPosition(SlotPosition(slot));

                _tileBySlot[slot] = tile;
            }

            for (int i = used; i < _tiles.Count; i++)
            {
                _tiles[i].gameObject.SetActive(false);
            }
        }

        /// <inheritdoc />
        public void SetMoveCount(int moves)
        {
            if (_lastMoves == moves) return;

            _lastMoves = moves;

            if (_movesLabel != null) _movesLabel.SetText(_movesFormat, moves);
        }

        /// <inheritdoc />
        public async UniTask<int> WaitForTileClickAsync(CancellationToken cancellationToken)
        {
            UniTaskCompletionSource<int> pending = new UniTaskCompletionSource<int>();
            _pendingClick = pending;

            try
            {
                return await pending.Task.AttachExternalCancellation(cancellationToken);
            }
            finally
            {
                if (ReferenceEquals(_pendingClick, pending)) _pendingClick = null;
            }
        }

        /// <inheritdoc />
        public async UniTask AnimateMoveAsync(int fromSlot, int toSlot, CancellationToken cancellationToken)
        {
            SlidingTileView tile = GetTile(fromSlot);
            if (tile == null) return;

            _tileBySlot[fromSlot] = null;
            _tileBySlot[toSlot] = tile;
            tile.SetSlot(toSlot);

            await tile.MoveToAsync(SlotPosition(toSlot), _moveDuration, cancellationToken);
        }

        /// <summary>
        /// Membuat tile sejak awal agar membuka puzzle nanti tidak pernah Instantiate. Satu-satunya
        /// jalur Instantiate: Build tetap menambah tile saat config melewati jumlah prewarm.
        /// </summary>
        public void Prewarm(int tileCount)
        {
            for (int i = 0; i < tileCount; i++)
            {
                GetOrCreateTile(i);
            }

            for (int i = 0; i < _tiles.Count; i++)
            {
                _tiles[i].gameObject.SetActive(false);
            }

            HideAsync(CancellationToken.None).Forget();
        }

        private static Sprite SpriteFor(Sprite[] spritesByValue, int value)
        {
            if (spritesByValue == null) return null;

            int index = value - 1;
            return index >= 0 && index < spritesByValue.Length ? spritesByValue[index] : null;
        }

        private SlidingTileView GetTile(int slot)
        {
            if (_tileBySlot == null || slot < 0 || slot >= _tileBySlot.Length) return null;

            return _tileBySlot[slot];
        }

        private SlidingTileView GetOrCreateTile(int index)
        {
            if (index < _tiles.Count) return _tiles[index];

            if (_tilePrefab == null || _boardRoot == null) return null;

            SlidingTileView tile = Instantiate(_tilePrefab, _boardRoot);
            tile.Clicked += HandleTileClicked;
            _tiles.Add(tile);

            return tile;
        }

        /// <summary>Posisi sebuah slot, dihitung dari geometri grid (anchor di tengah papan).</summary>
        private Vector2 SlotPosition(int slot)
        {
            int column = slot % _size;
            int row = slot / _size;

            float stepX = _tileSize.x + _tileSpacing;
            float stepY = _tileSize.y + _tileSpacing;

            float width = _size * _tileSize.x + (_size - 1) * _tileSpacing;
            float height = _size * _tileSize.y + (_size - 1) * _tileSpacing;

            float x = column * stepX - width * 0.5f + _tileSize.x * 0.5f;
            float y = height * 0.5f - _tileSize.y * 0.5f - row * stepY;

            return new Vector2(x, y);
        }

        private void HandleTileClicked(SlidingTileView tile)
        {
            // Klik dibuang saat tidak ada yang menunggu: inilah yang menahan input selama animasi geser.
            UniTaskCompletionSource<int> pending = _pendingClick;
            _pendingClick = null;

            pending?.TrySetResult(tile.Slot);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _tiles.Count; i++)
            {
                if (_tiles[i] != null) _tiles[i].Clicked -= HandleTileClicked;
            }

            _tiles.Clear();
            _tileBySlot = null;
            _pendingClick = null;
        }
    }
}
