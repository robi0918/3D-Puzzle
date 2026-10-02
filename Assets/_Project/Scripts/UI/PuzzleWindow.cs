using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Puzzle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Game.UI
{
    /// <summary>
    /// Shell modal yang menampung tampilan puzzle: header, konten, tombol batal, dan penanganan ESC.
    /// </summary>
    public sealed class PuzzleWindow : MonoBehaviour, IPuzzleUI
    {
        [Header("Wiring")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [Tooltip("Root objek yang diaktifkan/dinonaktifkan. Default: anak-anak GameObject ini.")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _titleLabel;
        [SerializeField] private Button _cancelButton;

        [Header("Transisi")]
        [SerializeField] private float _fadeDuration = 0.15f;

        [Header("Batal")]
        [SerializeField] private bool _allowEscapeKey = true;

        public event Action CancelRequested;

        private bool _isVisible;

        private void Awake()
        {
            if (_cancelButton != null) _cancelButton.onClick.AddListener(RequestCancel);

            SetVisibleImmediately(false);
        }

        private void OnDestroy()
        {
            if (_cancelButton != null) _cancelButton.onClick.RemoveListener(RequestCancel);
        }

        private void Update()
        {
            if (!_isVisible || !_allowEscapeKey) return;

#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) RequestCancel();
#else
            if (Input.GetKeyDown(KeyCode.Escape)) RequestCancel();
#endif
        }

        public async UniTask OpenAsync(string title, CancellationToken cancellationToken)
        {
            if (_titleLabel != null) _titleLabel.SetText(title);

            SetVisibleImmediately(true);

            if (_canvasGroup == null) return;

            _canvasGroup.alpha = 0f;
            await UiTween.FadeAsync(_canvasGroup, 0f, 1f, _fadeDuration, cancellationToken);
        }

        public async UniTask CloseAsync(CancellationToken cancellationToken)
        {
            if (!_isVisible) return;

            // Berhenti menerima permintaan batal sebelum fade selesai agar ESC kedua diabaikan.
            _isVisible = false;

            float from = _canvasGroup != null ? _canvasGroup.alpha : 1f;
            await UiTween.FadeAsync(_canvasGroup, from, 0f, _fadeDuration, cancellationToken);

            SetVisibleImmediately(false);
        }

        private void RequestCancel()
        {
            if (!_isVisible) return;

            CancelRequested?.Invoke();
        }

        private void SetVisibleImmediately(bool visible)
        {
            _isVisible = visible;

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = visible ? 1f : 0f;
                _canvasGroup.blocksRaycasts = visible;
                _canvasGroup.interactable = visible;
            }

            if (_panel != null) _panel.SetActive(visible);
        }
    }
}
