using System;
using StarterAssets;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// Menyesuaikan <see cref="StarterAssetsInputs"/> dengan kontrak <see cref="IPlayerInput"/>.
    /// </summary>
    public sealed class PlayerInputReader : IPlayerInput, IDisposable
    {
        private readonly StarterAssetsInputs _source;

        /// <summary>
        /// Mencegah tombol yang ditahan mengirim permintaan interaksi berulang: flag aktif saat
        /// penekanan pertama dan dihapus saat tombol dilepas, sehingga satu permintaan per tekan.
        /// </summary>
        private bool _interactLatched;

        /// <param name="source">Bridge input yang berada di GameObject pemain.</param>
        public PlayerInputReader(StarterAssetsInputs source)
        {
            _source = source != null
                ? source
                : throw new ArgumentNullException(nameof(source));

            _source.OnInteraction += HandleInteractionChanged;
        }

        /// <inheritdoc />
        public event Action OnInteractRequested;

        /// <inheritdoc />
        public bool IsEnabled { get; set; } = true;

        /// <inheritdoc />
        public Vector2 GetMove() => IsEnabled ? _source.move : Vector2.zero;

        /// <inheritdoc />
        public Vector2 GetLook() => IsEnabled ? _source.look : Vector2.zero;

        /// <inheritdoc />
        public bool GetJump() => IsEnabled && _source.jump;

        /// <inheritdoc />
        public bool GetSprint() => IsEnabled && _source.sprint;

        /// <inheritdoc />
        public bool ConsumeSwitchViewRequest()
        {
            if (!IsEnabled || !_source.isSwitchingView) return false;

            // StarterAssetsInputs mengatur ulang flag setelah satu permintaan dikonsumsi.
            _source.isSwitchingView = false;
            return true;
        }

        /// <inheritdoc />
        public void ResetState()
        {
            _source.MoveInput(Vector2.zero);
            _source.LookInput(Vector2.zero);
            _source.JumpInput(false);
            _source.SprintInput(false);

            _source.isSwitchingView = false;
            _source.interact = false;
            _interactLatched = false;
        }

        /// <summary>Melepas diri dari bridge input. Panggil saat owner dibongkar.</summary>
        public void Dispose()
        {
            _source.OnInteraction -= HandleInteractionChanged;
        }

        private void HandleInteractionChanged(bool pressed)
        {
            if (!pressed)
            {
                _interactLatched = false;
                return;
            }

            if (!IsEnabled || _interactLatched) return;

            _interactLatched = true;
            OnInteractRequested?.Invoke();
        }
    }
}
