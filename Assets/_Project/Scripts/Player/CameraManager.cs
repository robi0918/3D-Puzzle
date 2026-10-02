using Unity.Cinemachine;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// Mengelola rig Cinemachine dan memilih mana yang aktif untuk <see cref="CameraView"/> saat ini.
    /// </summary>
    /// <remarks>
    /// Tanggung jawab (SRP): hanya pemilihan priority/UI rig. Pitch kamera diterapkan oleh
    /// <see cref="PlayerController"/> pada target kamera Cinemachine, karena pitch adalah state
    /// pemain, bukan state rig kamera. Interaksi kamera dengan wall adalah sifat rig itu sendiri
    /// (misal. <c>CinemachineThirdPersonFollow</c> camera collision).
    /// </remarks>
    public class CameraManager : MonoBehaviour
    {
        [Header("Cinemachine rigs")]
        [SerializeField] private CinemachineCamera _tpsCamera;
        [SerializeField] private CinemachineCamera _topDownCamera;

        [Header("UI rigs (index 0 = TPS, index 1 = TopDown)")]
        [SerializeField] private GameObject[] _uiCameras;

        [Header("Priorities")]
        [SerializeField] private int _activePriority = 10;
        [SerializeField] private int _inactivePriority = 0;

        private PlayerController _playerController;
        private bool _isSubscribed;

        /// <summary>cam rendering aktif, di-cache satu kali saat start.</summary>
        public Camera MainCamera { get; private set; }

        private void Awake()
        {
            MainCamera = Camera.main;
        }

        private void OnEnable() => Subscribe();

        private void OnDisable() => Unsubscribe();

        /// <summary>
        /// Disuntikkan composition root. Aman dipanggil sebelum atau sesudah <c>OnEnable</c>.
        /// </summary>
        public void Construct(PlayerController playerController)
        {
            if (_playerController != playerController)
            {
                Unsubscribe();
                _playerController = playerController;
            }

            Subscribe();
            ApplyView(_playerController != null ? _playerController.CameraSettings.CurrentView : CameraView.TPS);
        }

        private void Subscribe()
        {
            if (_isSubscribed || _playerController == null || !isActiveAndEnabled) return;

            _playerController.OnViewChanged += ApplyView;
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed) return;

            if (_playerController != null)
            {
                _playerController.OnViewChanged -= ApplyView;
            }

            _isSubscribed = false;
        }

        private void OnDestroy() => Unsubscribe();

        private void ApplyView(CameraView view)
        {
            SetPriority(_tpsCamera, view == CameraView.TPS);
            SetPriority(_topDownCamera, view == CameraView.TopDown);

            if (MainCamera != null)
            {
                MainCamera.useOcclusionCulling = false;
            }

            SetUiCamera(0, view == CameraView.TPS);
            SetUiCamera(1, view == CameraView.TopDown);
        }

        private void SetUiCamera(int index, bool state)
        {
            if (_uiCameras == null || index >= _uiCameras.Length) return;

            GameObject uiCamera = _uiCameras[index];
            if (uiCamera != null) uiCamera.SetActive(state);
        }

        private void SetPriority(CinemachineCamera rig, bool isActive)
        {
            if (rig == null) return;

            rig.Priority = isActive ? _activePriority : _inactivePriority;
        }
    }
}
