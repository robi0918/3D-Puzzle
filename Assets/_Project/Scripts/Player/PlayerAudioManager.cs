using System;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// Mengelola pemutaran audio karakter: irama langkah kaki dan pendaratan.
    /// </summary>
    /// <remarks>
    /// Memakai <see cref="AudioSource.PlayOneShot(AudioClip, float)"/> agar suara yang tumpang tindih
    /// tidak saling memotong, serta <see cref="System.Random"/> untuk variasi klip dan pitch.
    /// </remarks>
    [RequireComponent(typeof(AudioSource))]
    public class PlayerAudioManager : MonoBehaviour, IPlayerAudio
    {
        [Header("Sumber (opsional — gunakan komponen di GameObject ini bila kosong)")]
        [SerializeField] private AudioSource _footstepsSource;
        [SerializeField] private AudioSource _landingSource;

        [Header("Klip")]
        [SerializeField] private AudioClip[] _footstepClips;
        [SerializeField] private AudioClip[] _landingClips;

        [Header("Pengaturan")]
        [Tooltip("Langkah kaki per detik saat berlari dengan kecepatan penuh.")]
        [SerializeField] private float _stepsPerSecondAtFullSpeed = 1.8f;

        [Tooltip("Kecepatan minimum untuk memutar langkah kaki (0-1).")]
        [SerializeField, Range(0f, 0.5f)] private float _walkThreshold = 0.1f;

        [SerializeField, Range(0f, 1f)] private float _footstepVolume = 0.6f;
        [SerializeField, Range(0f, 1f)] private float _landingVolume = 0.8f;
        [SerializeField, Range(0f, 0.4f)] private float _pitchJitter = 0.07f;

        private readonly System.Random _random = new System.Random();

        private float _stepTimer;

        private void Awake()
        {
            AudioSource[] sources = GetComponents<AudioSource>();

            if (_footstepsSource == null && sources.Length > 0) _footstepsSource = sources[0];
            if (_landingSource == null && sources.Length > 1) _landingSource = sources[1];
            _stepTimer = float.MaxValue;
        }

        public void Configure(AudioSource footsteps, AudioSource landing)
        {
            if (footsteps != null) _footstepsSource = footsteps;
            if (landing != null) _landingSource = landing;
        }

        public void UpdateLocomotion(float normalisedSpeed, bool isGrounded)
        {
            if (!isGrounded || normalisedSpeed < _walkThreshold)
            {
                _stepTimer = 0f;
                return;
            }

            _stepTimer += Time.deltaTime;

            float stepsPerSecond = Mathf.Max(0.01f, _stepsPerSecondAtFullSpeed * normalisedSpeed);
            if (_stepTimer < 1f / stepsPerSecond) return;

            _stepTimer = 0f;
            PlayFootstep();
        }

        public void PlayLanding()
        {
            AudioClip clip = PickClip(_landingClips, _landingSource);
            if (clip == null || _landingSource == null) return;

            _landingSource.pitch = NextPitch();
            _landingSource.PlayOneShot(clip, _landingVolume);
        }

        private void PlayFootstep()
        {
            AudioClip clip = PickClip(_footstepClips, _footstepsSource);
            if (clip == null || _footstepsSource == null) return;

            _footstepsSource.pitch = NextPitch();
            _footstepsSource.PlayOneShot(clip, _footstepVolume);
        }

        private AudioClip PickClip(AudioClip[] clips, AudioSource preferredSource)
        {
            if (clips != null && clips.Length > 0) return clips[_random.Next(clips.Length)];

            // Gunakan klip yang sudah terpasang pada source bila daftar klip kosong.
            return preferredSource != null ? preferredSource.clip : null;
        }

        private float NextPitch()
        {
            if (_pitchJitter <= 0f) return 1f;

            double offset = (_random.NextDouble() * 2.0 - 1.0) * _pitchJitter;
            return Mathf.Clamp(1f + (float)offset, 0.5f, 1.5f);
        }
    }
}
