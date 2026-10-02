using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Puzzle
{
    /// <summary>
    /// UI Memory Match: membangun grid kartu, melaporkan klik, dan memainkan animasi.
    /// </summary>
    /// <remarks>
    /// Kartu di-pool dan dipakai ulang antar sesi. Klik hanya diteruskan saat puzzle menunggu,
    /// sehingga input tertahan selama animasi flip dan jeda pasangan tidak cocok. Label langkah
    /// hanya diperbarui saat nilainya berubah.
    /// </remarks>
    public sealed class MemoryMatchView : MonoBehaviour, IMemoryMatchView
    {
        [Header("Reference")]
        [SerializeField] private RectTransform _contentRoot;
        [SerializeField] private GridLayoutGroup _grid;
        [SerializeField] private CardView _cardPrefab;

        [Header("HUD")]
        [SerializeField] private TMP_Text _movesLabel;
        [SerializeField] private string _movesFormat = "Moves: {0}";

        private readonly List<CardView> _cards = new();

        private UniTaskCompletionSource<int> _pendingClick;
        private int _lastMoves = -1;

        /// <inheritdoc />
        public async UniTask ShowAsync(CancellationToken cancellationToken)
        {
            gameObject.SetActive(true);

            _pendingClick = null;
            _lastMoves = -1;

            // Tunggu satu frame agar grid selesai ditata sebelum raycast pertama mengenai kartu.
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }

        /// <inheritdoc />
        public UniTask HideAsync(CancellationToken cancellationToken)
        {
            _pendingClick = null;

            gameObject.SetActive(false);

            return UniTask.CompletedTask;
        }

        /// <inheritdoc />
        public void Build(int cardCount, int columns, int[] iconIdByCard, Sprite[] iconsByIconId)
        {
            if (_grid != null)
            {
                _grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                _grid.constraintCount = Mathf.Max(1, columns);
            }

            EnsureCardCount(cardCount);

            for (int i = 0; i < _cards.Count; i++)
            {
                CardView card = _cards[i];

                if (i >= cardCount)
                {
                    card.gameObject.SetActive(false);
                    continue;
                }

                int iconId = iconIdByCard != null && i < iconIdByCard.Length ? iconIdByCard[i] : -1;
                Sprite icon = iconsByIconId != null && iconId >= 0 && iconId < iconsByIconId.Length
                    ? iconsByIconId[iconId]
                    : null;

                card.gameObject.SetActive(true);
                card.Bind(i, icon, true);
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
        public async UniTask<int> WaitForCardClickAsync(CancellationToken cancellationToken)
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
        public async UniTask FlipAsync(int cardIndex, bool faceUp, CancellationToken cancellationToken)
        {
            CardView card = GetCard(cardIndex);
            if (card == null) return;

            await card.FlipAsync(faceUp, cancellationToken);
        }

        /// <inheritdoc />
        public async UniTask CelebrateMatchAsync(int firstCard, int secondCard, CancellationToken cancellationToken)
        {
            CardView first = GetCard(firstCard);
            CardView second = GetCard(secondCard);

            if (first != null) await first.CelebrateAsync(cancellationToken);
            if (second != null) await second.CelebrateAsync(cancellationToken);
        }

        /// <summary>
        /// Membuat kartu di awal agar puzzle tidak perlu membuat instance saat dibuka.
        /// </summary>
        public void Prewarm(int cardCount)
        {
            EnsureCardCount(Mathf.Max(0, cardCount));

            for (int i = 0; i < _cards.Count; i++)
            {
                _cards[i].gameObject.SetActive(false);
            }

            HideAsync(CancellationToken.None).Forget();
        }

        private CardView GetCard(int cardIndex)
        {
            if (cardIndex < 0 || cardIndex >= _cards.Count) return null;

            return _cards[cardIndex];
        }

        private void EnsureCardCount(int cardCount)
        {
            if (_cardPrefab == null || _contentRoot == null) return;

            while (_cards.Count < cardCount)
            {
                CardView card = Instantiate(_cardPrefab, _contentRoot);
                card.Clicked += HandleCardClicked;
                _cards.Add(card);
            }
        }

        private void HandleCardClicked(CardView card)
        {
            // Klik tanpa penunggu diabaikan, sehingga input tertahan selama animasi.
            UniTaskCompletionSource<int> pending = _pendingClick;
            _pendingClick = null;

            pending?.TrySetResult(card.Index);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                if (_cards[i] != null) _cards[i].Clicked -= HandleCardClicked;
            }

            _cards.Clear();
            _pendingClick = null;
        }
    }
}
