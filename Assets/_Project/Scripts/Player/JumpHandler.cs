using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// Menangani input lompat, gravitasi, dan cooldown.
    /// </summary>
    public class JumpHandler
    {
        private readonly PlayerController.MovementConfig _config;
        private readonly IPlayerAnimator _animator;
        private readonly IPlayerAudio _audio;
        private readonly CancellationToken _token;

        private float _verticalVelocity;
        private bool _canJump = true;
        private bool _isFalling;
        private bool _groundedLastFrame = true;

        public float VerticalVelocity => _verticalVelocity;

        public JumpHandler(
            PlayerController.MovementConfig config,
            IPlayerAnimator animator,
            IPlayerAudio audio,
            CancellationToken token)
        {
            _config = config;
            _animator = animator;
            _audio = audio;
            _token = token;
        }

        public void Update(bool isGrounded, bool jumpPressed)
        {
            if (isGrounded)
            {
                HandleGrounded(jumpPressed);
            }
            else
            {
                HandleAirborne();
            }

            ApplyGravity();
        }

        private void HandleGrounded(bool jumpPressed)
        {
            if (!_groundedLastFrame && _verticalVelocity < PlayerConstants.LandingVelocityThreshold)
            {
                _audio?.PlayLanding();
            }

            _groundedLastFrame = true;
            _isFalling = false;

            _animator?.SetJump(false);
            _animator?.SetFreeFall(false);

            if (_verticalVelocity < 0f)
            {
                _verticalVelocity = PlayerConstants.GroundedVelocityReset;
            }

            if (jumpPressed && _canJump)
            {
                _verticalVelocity = Mathf.Sqrt(_config.JumpHeight * -2f * _config.Gravity);
                _animator?.SetJump(true);
                HandleJumpCooldownAsync().Forget();
            }
        }

        private void HandleAirborne()
        {
            _groundedLastFrame = false;

            if (!_isFalling)
            {
                HandleFallTimeoutAsync().Forget();
            }
        }

        private void ApplyGravity()
        {
            if (_verticalVelocity < PlayerConstants.TerminalVelocity)
            {
                _verticalVelocity += _config.Gravity * Time.deltaTime;
            }
        }

        private async UniTaskVoid HandleJumpCooldownAsync()
        {
            _canJump = false;

            // SuppressCancellationThrow mengembalikan true jika DIBATALKAN, bukan jika selesai.
            bool isCanceled = await UniTask.WaitForSeconds(_config.JumpTimeout, cancellationToken: _token)
                .SuppressCancellationThrow();

            if (!isCanceled) _canJump = true;
        }

        private async UniTaskVoid HandleFallTimeoutAsync()
        {
            _isFalling = true;

            bool isCanceled = await UniTask.WaitForSeconds(_config.FallTimeout, cancellationToken: _token)
                .SuppressCancellationThrow();

            if (!isCanceled)
            {
                _animator?.SetFreeFall(true);
            }
        }
    }
}
