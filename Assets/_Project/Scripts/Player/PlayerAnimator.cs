using System;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// membungkus setiap penulisan animator untuk karakter pemain.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimator : MonoBehaviour, IPlayerAnimator
    {
        /// <summary>Nama parameter animator. Harus cocok dengan Animator Controller.</summary>
        [Serializable]
        public struct Parameters
        {
            public string Speed;
            public string Grounded;
            public string Jump;
            public string FreeFall;
            public string Interact;
        }

        [Header("Parameter Animator")]
        [SerializeField] private Parameters _parameters = new ()
        {
            Speed = "Speed",
            Grounded = "Grounded",
            Jump = "Jump",
            FreeFall = "FreeFall",
            Interact = "Interact",
        };

        private Animator _animator;

        private int _speedId;
        private int _groundedId;
        private int _jumpId;
        private int _freeFallId;
        private int _interactId;

        private void Awake()
        {
            _animator = GetComponent<Animator>();

            _speedId = Animator.StringToHash(_parameters.Speed);
            _groundedId = Animator.StringToHash(_parameters.Grounded);
            _jumpId = Animator.StringToHash(_parameters.Jump);
            _freeFallId = Animator.StringToHash(_parameters.FreeFall);
            _interactId = Animator.StringToHash(_parameters.Interact);
        }

        public void SetGrounded(bool value)
        {
            if (_animator == null) return;

            _animator.SetBool(_groundedId, value);
        }

        public void SetJump(bool value)
        {
            if (_animator == null) return;

            _animator.SetBool(_jumpId, value);
        }

        public void SetFreeFall(bool value)
        {
            if (_animator == null) return;

            _animator.SetBool(_freeFallId, value);
        }

        public void TriggerInteract()
        {
            if (_animator == null) return;

            _animator.SetTrigger(_interactId);
        }

        public void UpdateLocomotion(float speedMetersPerSecond)
        {
            if (_animator == null) return;

            _animator.SetFloat(_speedId, speedMetersPerSecond);
        }
    }
}
