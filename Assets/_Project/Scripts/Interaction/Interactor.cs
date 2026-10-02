using System;
using Game.Core;
using Game.Player;
using UnityEngine;

namespace Game.Interaction
{
    /// <summary>
    /// Mendeteksi objek <see cref="IInteractable"/> terdekat dan memicunya melalui input pemain.
    /// Query fisika non-allocating dijalankan berkala, bukan setiap frame.
    /// Interaksi bergantung pada <see cref="IPlayerInput"/> dan tetap tidak mengetahui perilaku objek.
    /// Composition root mengunci sistem ini saat puzzle terbuka.
    /// </summary>
    public class Interactor : MonoBehaviour, IInputLockable
    {
        [Header("Deteksi")]
        [Tooltip("Probe sebagai pusat bola deteksi. Default: transform ini.")]
        [SerializeField] private Transform _interactionPoint;

        [SerializeField] private float _interactionRadius = 1.3f;
        [SerializeField] private LayerMask _interactableLayer;

        [Tooltip("Jeda antar query fisika. Lebih kecil = lebih responsif, lebih besar = lebih ringan.")]
        [SerializeField, Range(0.02f, 0.5f)] private float _detectionInterval = 0.1f;

        [Header("Batasan pandangan (mengabaikan objek di belakang pemain)")]
        [SerializeField] private bool _requireInView = true;
        [Tooltip("Sudut bukaan penuh kerucut pandang, derajat (hanya yaw).")]
        [SerializeField, Range(30f, 360f)] private float _viewAngle = 140f;

        [Header("Sumber input (opsional — composition root menyuntik IPlayerInput)")]
        [SerializeField] private PlayerInputSource _inputSource;

        private readonly Collider[] _results = new Collider[8];

        private IPlayerInput _input;
        private IInteractable _current;
        private bool _isSubscribedToInput;
        private bool _isInputLocked;
        private float _elapsedSinceDetection;

        /// <summary>Dipicu saat objek baru menjadi kandidat interaksi.</summary>
        public event Action<IInteractable> OnInteractableFound;

        /// <summary>Dipicu saat kandidat interaksi hilang.</summary>
        public event Action OnInteractableLost;

        private void Awake()
        {
            if (_interactionPoint == null)
            {
                _interactionPoint = transform;
            }

            if (_inputSource == null) _inputSource = PlayerInputSource.Resolve(this);
            if (_input == null && _inputSource != null) _input = _inputSource.Input;
        }

        private void OnEnable()
        {
            if (_input != null) _input.IsEnabled = !_isInputLocked;

            _elapsedSinceDetection = _detectionInterval; // deteksi pada frame pertama
            SubscribeToInput();
        }

        private void OnDisable() => UnsubscribeFromInput();

        private void OnDestroy() => UnsubscribeFromInput();

        private void Update()
        {
            if (_input == null || _isInputLocked)
            {
                ClearCurrent();
                return;
            }

            _elapsedSinceDetection += Time.unscaledDeltaTime;
            if (_elapsedSinceDetection < _detectionInterval)
            {
                return;
            }

            _elapsedSinceDetection = 0f;
            DetectNearest();
        }

        /// <summary>Disuntikkan composition root, sebelum atau sesudah <c>OnEnable</c>.</summary>
        public void Construct(IPlayerInput input)
        {
            UnsubscribeFromInput();

            if (input != null) _input = input;

            SubscribeToInput();
        }

        /// <inheritdoc />
        public void SetInputLocked(bool locked)
        {
            _isInputLocked = locked;

            if (locked) ClearCurrent();
        }

        private void SubscribeToInput()
        {
            if (_isSubscribedToInput || _input == null || !isActiveAndEnabled) return;

            _input.OnInteractRequested += HandleInteractRequested;
            _isSubscribedToInput = true;
        }

        private void UnsubscribeFromInput()
        {
            if (!_isSubscribedToInput || _input == null) return;

            _input.OnInteractRequested -= HandleInteractRequested;
            _isSubscribedToInput = false;
        }

        private void DetectNearest()
        {
            int count = Physics.OverlapSphereNonAlloc(
                _interactionPoint.position,
                _interactionRadius,
                _results,
                _interactableLayer,
                QueryTriggerInteraction.Collide);

            IInteractable nearest = null;
            float nearestSqrDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                Collider candidateCollider = _results[i];
                if (candidateCollider == null) continue;
                if (!candidateCollider.TryGetComponent(out IInteractable candidate)) continue;
                if (!candidate.CanInteract()) continue;
                if (_requireInView && !IsWithinView(candidateCollider.transform.position)) continue;

                float sqrDistance = (candidateCollider.transform.position - _interactionPoint.position).sqrMagnitude;
                if (sqrDistance >= nearestSqrDistance) continue;

                nearestSqrDistance = sqrDistance;
                nearest = candidate;
            }

            SetCurrent(nearest);
        }

        private bool IsWithinView(Vector3 targetPosition)
        {
            Vector3 toTarget = targetPosition - _interactionPoint.position;
            toTarget.y = 0f; // yaw saja: melihat ke bawah tidak boleh menyembunyikan objek

            if (toTarget.sqrMagnitude < 0.0001f) return true;

            return Vector3.Angle(_interactionPoint.forward, toTarget) <= _viewAngle * 0.5f;
        }

        private void SetCurrent(IInteractable next)
        {
            if (ReferenceEquals(next, _current)) return;

            if (_current is UnityEngine.Object previous && previous != null)
            {
                _current.OnFocusExit();
            }

            _current = next;

            if (_current == null)
            {
                OnInteractableLost?.Invoke();
                return;
            }

            _current.OnFocusEnter();
            OnInteractableFound?.Invoke(_current);
        }

        private void ClearCurrent() => SetCurrent(null);

        private void HandleInteractRequested()
        {
            if (_isInputLocked) return;

            IInteractable target = _current;
            if (target == null || !target.CanInteract()) return;

            target.Interact();
        }

        private void OnDrawGizmosSelected()
        {
            Transform probe = _interactionPoint != null ? _interactionPoint : transform;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(probe.position, _interactionRadius);

            if (!_requireInView) return;

            Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
            Gizmos.DrawRay(probe.position, Quaternion.Euler(0f, -_viewAngle * 0.5f, 0f) * probe.forward * _interactionRadius);
            Gizmos.DrawRay(probe.position, Quaternion.Euler(0f, _viewAngle * 0.5f, 0f) * probe.forward * _interactionRadius);
        }
    }
}
