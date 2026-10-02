using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// Mendeteksi apakah karakter sedang di atas permukaan via sphere check.
    /// </summary>
    public class GroundChecker
    {
        private readonly Transform _transform;
        private readonly PlayerController.GroundConfig _config;

        public bool IsGrounded { get; private set; }

        public GroundChecker(Transform transform, PlayerController.GroundConfig config)
        {
            _transform = transform;
            _config = config;
            IsGrounded = config.IsGrounded;
        }

        public bool Check()
        {
            IsGrounded = Physics.CheckSphere(
                SpherePosition,
                _config.GroundedRadius,
                _config.GroundLayers,
                QueryTriggerInteraction.Ignore
            );

            return IsGrounded;
        }

        public void DrawGizmos()
        {
            Gizmos.color = IsGrounded
                ? new Color(0.0f, 1.0f, 0.0f, 0.35f)
                : new Color(1.0f, 0.0f, 0.0f, 0.35f);

            Gizmos.DrawSphere(SpherePosition, _config.GroundedRadius);
        }

        private Vector3 SpherePosition => new Vector3(
            _transform.position.x,
            _transform.position.y - _config.GroundedOffset,
            _transform.position.z
        );
    }
}
