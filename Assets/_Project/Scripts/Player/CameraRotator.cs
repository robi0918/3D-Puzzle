using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// Menangani rotasi orbit kamera (yaw dan pitch).
    /// </summary>
    public class CameraRotator
    {
        private readonly PlayerController.CameraConfig _config;
        private readonly Transform _target;

        private float _yaw;
        private float _pitch;

        public float Yaw => _yaw;

        public CameraRotator(PlayerController.CameraConfig config, Transform target)
        {
            _config = config;
            _target = target;

            if (_target != null)
            {
                Vector3 euler = _target.eulerAngles;
                _yaw = euler.y;
                _pitch = euler.x;
            }
        }

        public void Rotate(Vector2 look, float rotationSpeed)
        {
            if (look.sqrMagnitude >= PlayerConstants.LookSqrThreshold)
            {
                _yaw += look.x * rotationSpeed;
                _pitch += look.y * rotationSpeed;
            }

            _yaw = ClampAngle(_yaw, float.MinValue, float.MaxValue);
            _pitch = ClampAngle(_pitch, _config.BottomClamp, _config.TopClamp);

            if (_target != null)
            {
                _target.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            }
        }

        private static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360f) angle += 360f;
            if (angle > 360f) angle -= 360f;
            return Mathf.Clamp(angle, min, max);
        }
    }
}
