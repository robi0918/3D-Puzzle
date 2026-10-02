using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// Menangani gerak dan rotasi relatif terhadap kamera.
    /// </summary>
    public class MovementHandler
    {
        private readonly Transform _transform;
        private readonly CharacterController _controller;
        private readonly PlayerController.MovementConfig _config;
        private readonly IPlayerAnimator _animator;

        private float _speed;
        private float _animationBlend;
        private float _inputMagnitude;
        private float _targetRotation;
        private float _rotationVelocity;

        public float AnimationBlend => _animationBlend;
        public float InputMagnitude => _inputMagnitude;

        public MovementHandler(
            Transform transform,
            CharacterController controller,
            PlayerController.MovementConfig config,
            IPlayerAnimator animator)
        {
            _transform = transform;
            _controller = controller;
            _config = config;
            _animator = animator;
        }

        public void Move(Vector2 move, bool sprint, float cameraYaw, float verticalVelocity)
        {
            _inputMagnitude = Mathf.Clamp01(move.magnitude);

            float targetSpeed = _inputMagnitude > 0f
                ? (sprint ? _config.SprintSpeed : _config.MoveSpeed)
                : 0f;

            float currentHorizontalSpeed = new Vector3(
                _controller.velocity.x,
                0f,
                _controller.velocity.z
            ).magnitude;

            if (Mathf.Abs(currentHorizontalSpeed - targetSpeed) > PlayerConstants.SpeedMatchThreshold)
            {
                _speed = Mathf.Lerp(
                    currentHorizontalSpeed,
                    targetSpeed * _inputMagnitude,
                    Time.deltaTime * _config.SpeedChangeRate
                );
                _speed = Mathf.Round(_speed * PlayerConstants.SpeedRoundFactor) / PlayerConstants.SpeedRoundFactor;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(
                _animationBlend,
                targetSpeed,
                Time.deltaTime * _config.SpeedChangeRate
            );

            if (_animationBlend < PlayerConstants.AnimationBlendThreshold)
            {
                _animationBlend = 0f;
            }

            ApplyMovement(move, cameraYaw, verticalVelocity);
        }

        private void ApplyMovement(Vector2 move, float cameraYaw, float verticalVelocity)
        {
            if (move != Vector2.zero)
            {
                Vector3 inputDirection = new Vector3(move.x, 0f, move.y).normalized;

                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + cameraYaw;

                float rotation = Mathf.SmoothDampAngle(
                    _transform.eulerAngles.y,
                    _targetRotation,
                    ref _rotationVelocity,
                    _config.RotationSmoothTime
                );

                _transform.rotation = Quaternion.Euler(0f, rotation, 0f);
            }

            Vector3 targetDirection = Quaternion.Euler(0f, _targetRotation, 0f) * Vector3.forward;
            Vector3 motion = targetDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime;
            _controller.Move(motion);
        }

        public void ResetSpeed()
        {
            _speed = 0f;
            _animationBlend = 0f;
            _rotationVelocity = 0f;
        }
    }
}
