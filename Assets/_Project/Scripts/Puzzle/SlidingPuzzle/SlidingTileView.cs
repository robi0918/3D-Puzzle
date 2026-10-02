using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Puzzle
{
    /// <summary>
    /// Satu tile sliding: melaporkan klik sendiri dan beranimasi antar slot.
    /// </summary>
    /// <remarks>
    /// Hanya presentasi — aturan kedekatan ada di <see cref="SlidingPuzzleModel"/>. Anchor dinormalkan
    /// ke tengah rect di <c>Awake</c> agar view bisa memposisikan tile lewat aritmetika.
    /// </remarks>
    public sealed class SlidingTileView : MonoBehaviour
    {
        [Header("Wiring")]
        [SerializeField] private Button _button;
        [SerializeField] private Image _background;
        [SerializeField] private Image _iconImage;
        [Tooltip("Opsional: menampilkan nomor tile bila tidak ada sprite.")]
        [SerializeField] private TMP_Text _label;

        [Header("Tampilan")]
        [SerializeField] private Color _tileColor = new Color(0.24f, 0.34f, 0.52f, 1f);

        /// <summary>Dipicu saat pemain mengklik tile ini.</summary>
        public event Action<SlidingTileView> Clicked;

        /// <summary>Slot yang sedang ditempati tile ini.</summary>
        public int Slot { get; private set; }

        /// <summary>Nilai tile (berbasis 1).</summary>
        public int Value { get; private set; }

        /// <summary>Rect yang di-cache, dipakai untuk posisi dan tween.</summary>
        public RectTransform Rect { get; private set; }

        private void Awake()
        {
            Rect = GetComponent<RectTransform>();

            if (Rect != null)
            {
                Rect.anchorMin = new Vector2(0.5f, 0.5f);
                Rect.anchorMax = new Vector2(0.5f, 0.5f);
                Rect.pivot = new Vector2(0.5f, 0.5f);
            }

            if (_button != null) _button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveListener(HandleClick);
        }

        /// <summary>Mengikat ulang tile dari pool untuk sesi baru.</summary>
        public void Bind(int slot, int value, Sprite sprite)
        {
            Slot = slot;
            Value = value;

            if (_background != null) _background.color = _tileColor;

            if (_iconImage != null)
            {
                _iconImage.sprite = sprite;
                _iconImage.enabled = sprite != null;
            }

            if (_label != null)
            {
                _label.SetText("{0}", value);
                _label.gameObject.SetActive(sprite == null);
            }

            if (_button != null) _button.interactable = true;
        }

        /// <summary>Memperbarui slot yang ditempati tile ini (setelah gerakan).</summary>
        public void SetSlot(int slot) => Slot = slot;

        /// <summary>Memindahkan tile langsung ke posisi (dipakai tepat setelah rebuild).</summary>
        public void SetPosition(Vector2 anchoredPosition)
        {
            if (Rect != null) Rect.anchoredPosition = anchoredPosition;
        }

        /// <summary>Menganimasikan tile ke posisi baru.</summary>
        public UniTask MoveToAsync(Vector2 targetPosition, float duration, CancellationToken cancellationToken)
        {
            if (Rect == null) return UniTask.CompletedTask;

            return UiTween.MoveAnchoredAsync(Rect, Rect.anchoredPosition, targetPosition, duration, cancellationToken);
        }

        private void HandleClick() => Clicked?.Invoke(this);
    }
}
