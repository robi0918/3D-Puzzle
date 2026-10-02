using System;
using Game.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Player
{
    /// <summary>
    /// Motor karakter pemain: mendelegasikan ke handler terpisah untuk ground check,
    /// lompat/gravitasi, gerak, dan rotasi kamera. Input dibaca lewat <see cref="IPlayerInput"/>,
    /// <see cref="CharacterController"/> digerakkan, rig kamera dipilih, dan state didorong
    /// ke layanan animasi/audio.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour, IInputLockable
    {
        [Serializable]
        public struct MovementConfig
        {
            [Tooltip("Kecepatan berjalan (m/s).")] public float MoveSpeed;
            [Tooltip("Kecepatan sprint (m/s).")] public float SprintSpeed;
            [Tooltip("Sensitivitas pandangan (derajat per unit input).")] public float RotationSpeed;
            [Range(0f, 0.3f)] public float RotationSmoothTime;
            [Tooltip("Laju percepatan dan perlambatan.")] public float SpeedChangeRate;
            public float JumpHeight;
            public float Gravity;
            public float JumpTimeout;
            public float FallTimeout;
        }

        [Serializable]
        public struct GroundConfig
        {
            public bool IsGrounded;
            public float GroundedOffset;
            public float GroundedRadius;
            public LayerMask GroundLayers;
        }

        [Serializable]
        public struct CameraConfig
        {
            public CameraView CurrentView;
            public GameObject CinemachineCameraTarget;
            public float TopClamp;
            public float BottomClamp;
        }

        [Serializable]
        public struct AudioReferences
        {
            public AudioSource Footsteps;
            public AudioSource Landing;
        }

        [Header("Konfigurasi")]
        [FormerlySerializedAs("Movement")]
        [SerializeField] private MovementConfig _movement = new ()
        {
            MoveSpeed = 2.0f, 
            SprintSpeed = 5.335f, 
            RotationSpeed = 1.0f, 
            RotationSmoothTime = 0.12f,
            SpeedChangeRate = 10.0f, 
            JumpHeight = 1.2f, 
            Gravity = -15.0f, 
            JumpTimeout = 0.50f, 
            FallTimeout = 0.15f,
        };

        [FormerlySerializedAs("Ground")]
        [SerializeField] private GroundConfig _ground = new ()
        {
            IsGrounded = true, 
            GroundedOffset = -0.14f, 
            GroundedRadius = 0.28f,
        };

        [FormerlySerializedAs("CameraSettings")]
        [SerializeField] private CameraConfig _cameraSettings = new ()
        {
            CurrentView = CameraView.TPS, 
            TopClamp = 70.0f, 
            BottomClamp = -30.0f,
        };

        [FormerlySerializedAs("Audio")]
        [SerializeField] private AudioReferences _audio = new ();

        [Header("Dependensi (opsional — diinjeksi GameBootstrapper)")]
        [SerializeField] private PlayerInputSource _inputSource;
        [SerializeField] private CameraManager _cameraManager;

        /// <summary>Dipicu setelah rig kamera diganti, agar view dapat bereaksi tanpa polling.</summary>
        public event Action<CameraView> OnViewChanged;

        // Tampilan read-only atas konfigurasi terserialisasi (tanpa mutasi dari luar).
        public CameraConfig CameraSettings => _cameraSettings;

        private CharacterController _controller;
        private IPlayerAnimator _playerAnimator;
        private IPlayerAudio _playerAudio;

        private IPlayerInput _input;
        private bool _isSubscribedToInput;

        private GroundChecker _groundChecker;
        private JumpHandler _jumpHandler;
        private MovementHandler _movementHandler;
        private CameraRotator _cameraRotator;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();

            // Komponen bisa berada di child (mis. model beranimasi), jadi cari sekali di hierarki.
            _playerAnimator = GetComponentInChildren<IPlayerAnimator>(true);
            _playerAudio = GetComponentInChildren<IPlayerAudio>(true);

            if (_inputSource == null) _inputSource = PlayerInputSource.Resolve(this);
            if (_input == null && _inputSource != null) _input = _inputSource.Input;
        }

        private void Start()
        {
            _groundChecker = new GroundChecker(transform, _ground);
            _jumpHandler = new JumpHandler(_movement, _playerAnimator, _playerAudio, destroyCancellationToken);
            _movementHandler = new MovementHandler(transform, _controller, _movement, _playerAnimator);
            _cameraRotator = new CameraRotator(_cameraSettings, _cameraSettings.CinemachineCameraTarget?.transform);

            _playerAudio?.Configure(_audio.Footsteps, _audio.Landing);
        }

        private void OnEnable() => SubscribeToInput();

        private void OnDisable() => UnsubscribeFromInput();

        private void Update()
        {
            if (_input == null) return;

            bool isGrounded = _groundChecker.Check();
            _jumpHandler.Update(isGrounded, _input.GetJump());
            _movementHandler.Move(_input.GetMove(), _input.GetSprint(), GetCameraYaw(), _jumpHandler.VerticalVelocity);
            CheckSwitchView();
            UpdateServices();
        }

        private void LateUpdate()
        {
            if (_input == null) return;

            _cameraRotator.Rotate(_input.GetLook(), _movement.RotationSpeed);
        }

        private float GetCameraYaw()
        {
            Camera mainCamera = _cameraManager != null ? _cameraManager.MainCamera : null;
            return mainCamera != null ? mainCamera.transform.eulerAngles.y : _cameraRotator.Yaw;
        }

        /// <summary>
        /// Disuntikkan composition root. Aman dipanggil sebelum atau sesudah <c>OnEnable</c>.
        /// </summary>
        public void Construct(IPlayerInput input, CameraManager cameraManager)
        {
            UnsubscribeFromInput();

            _input = input ?? (_inputSource != null ? _inputSource.Input : null);
            _cameraManager = cameraManager;

            SubscribeToInput();
        }

        public void SetInputLocked(bool locked)
        {
            if (_input == null) return;

            _input.IsEnabled = !locked;

            if (!locked) return;

            // Lepas semua tombol yang sedang ditahan agar tidak "nyangkut" saat puzzle ditutup.
            _input.ResetState();
            _movementHandler?.ResetSpeed();
        }

        private void SubscribeToInput()
        {
            if (_isSubscribedToInput || _input == null || !isActiveAndEnabled) return;

            _input.OnInteractRequested += OnInteract;
            _isSubscribedToInput = true;
        }

        private void UnsubscribeFromInput()
        {
            if (!_isSubscribedToInput || _input == null) return;

            _input.OnInteractRequested -= OnInteract;
            _isSubscribedToInput = false;
        }

        private void CheckSwitchView()
        {
            if (!_input.ConsumeSwitchViewRequest()) return;

            _cameraSettings.CurrentView = _cameraSettings.CurrentView == CameraView.TPS
                ? CameraView.TopDown
                : CameraView.TPS;

            OnViewChanged?.Invoke(_cameraSettings.CurrentView);
        }

        /// <summary>Mendorong state lokomosi saat ini ke layanan animasi dan audio.</summary>
        private void UpdateServices()
        {
            bool isGrounded = _groundChecker.IsGrounded;
            _playerAnimator?.SetGrounded(isGrounded);
            _playerAnimator?.UpdateLocomotion(_movementHandler.AnimationBlend);

            float normalisedSpeed = _movement.SprintSpeed > 0f
                ? Mathf.Clamp01(_movementHandler.AnimationBlend / _movement.SprintSpeed)
                : 0f;

            _playerAudio?.UpdateLocomotion(normalisedSpeed, isGrounded);
        }

        private void OnInteract()
        {
            if (_movementHandler.InputMagnitude > 0f) return; // Jangan izinkan interaksi saat bergerak.

            _playerAnimator?.TriggerInteract();
        }

        private void OnDrawGizmosSelected()
        {
            _groundChecker?.DrawGizmos();
        }
    }
}
