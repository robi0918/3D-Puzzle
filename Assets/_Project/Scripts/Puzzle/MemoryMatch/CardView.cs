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
    /// Card Memory Match: mengelola sisi kartu, melaporkan klik, dan memainkan animasi flip.
    /// </summary>
    /// <remarks>
    /// Hanya menangani tampilan; tidak menentukan apakah pilihan valid. Animasi flip memakai dua
    /// tween skala X agar sisi kartu berganti tepat saat kartu terlihat dari samping.
    /// </remarks>
    public sealed class CardView : MonoBehaviour
    {
        [Header("Reference")]
        [SerializeField] private Button _button;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Image _background;
        [Tooltip("Ditampilkan saat kartu terbuka.")]
        [SerializeField] private GameObject _faceRoot;
        [Tooltip("Ditampilkan saat kartu tertutup.")]
        [SerializeField] private GameObject _backRoot;
        [Tooltip("Opsional: menampilkan nomor kartu bila tidak ada sprite ikon.")]
        [SerializeField] private TMP_Text _label;

        [Header("Umpan balik")]
        [SerializeField] private Color _idleColor = new (0.16f, 0.18f, 0.25f, 1f);
        [SerializeField] private Color _matchedColor = new (0.20f, 0.55f, 0.35f, 1f);
        [SerializeField] private float _flipDuration = 0.16f;
        [SerializeField] private float _celebrateScale = 1.12f;

        /// <summary>Dipicu saat pemain mengklik kartu ini.</summary>
        public event Action<CardView> Clicked;

        /// <summary>Indeks papan yang diwakili kartu ini.</summary>
        public int Index { get; private set; }

        /// <summary>Benar saat sisi ikon terlihat.</summary>
        public bool IsFaceUp { get; private set; }

        /// <summary>Benar setelah kartu menemukan pasangannya.</summary>
        public bool IsMatched { get; private set; }

        private RectTransform _rect;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();

            if (_button != null) _button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveListener(HandleClick);
        }

        /// <summary>Mengikat ulang kartu dari pool untuk sesi baru.</summary>
        public void Bind(int index, Sprite icon, bool interactive)
        {
            Index = index;
            IsFaceUp = false;
            IsMatched = false;

            if (_iconImage != null)
            {
                _iconImage.sprite = icon;
                _iconImage.enabled = icon != null;
            }

            if (_label != null)
            {
                _label.SetText((Index + 1).ToString());
                _label.gameObject.SetActive(icon == null);
            }

            if (_background != null) _background.color = _idleColor;

            SetInteractive(interactive);

            if (_rect != null) _rect.localScale = Vector3.one;

            ApplyFaceState(false);
        }

        /// <summary>Mengaktifkan/mematikan klik (kartu yang sudah cocok tetap inert).</summary>
        public void SetInteractive(bool value)
        {
            if (_button != null) _button.interactable = value && !IsMatched;
        }

        /// <summary>Menganimasikan kartu menjadi terbuka / tertutup.</summary>
        public async UniTask FlipAsync(bool faceUp, CancellationToken cancellationToken)
        {
            if (IsFaceUp == faceUp) return;

            float half = Mathf.Max(0.01f, _flipDuration * 0.5f);

            await UiTween.ScaleXAsync(_rect, 1f, 0f, half, cancellationToken);

            IsFaceUp = faceUp;
            ApplyFaceState(faceUp);

            await UiTween.ScaleXAsync(_rect, 0f, 1f, half, cancellationToken);
        }

        /// <summary>Memainkan umpan balik "pasangan ini cocok" dan mengunci kartu.</summary>
        public async UniTask CelebrateAsync(CancellationToken cancellationToken)
        {
            IsMatched = true;
            SetInteractive(false);

            if (_background != null) _background.color = _matchedColor;

            float half = Mathf.Max(0.01f, _flipDuration);

            await UiTween.ScaleUniformAsync(_rect, 1f, _celebrateScale, half, cancellationToken);
            await UiTween.ScaleUniformAsync(_rect, _celebrateScale, 1f, half, cancellationToken);
        }

        private void HandleClick() => Clicked?.Invoke(this);

        private void ApplyFaceState(bool faceUp)
        {
            if (_faceRoot != null) _faceRoot.SetActive(faceUp);
            if (_backRoot != null) _backRoot.SetActive(!faceUp);
        }
    }
}
