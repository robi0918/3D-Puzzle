using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
	public class StarterAssetsInputs : MonoBehaviour
	{
		[Header("Character Input Values")]
		public Vector2 move;
		public Vector2 look;
		public bool jump;
		public bool sprint;
		public bool isSwitchingView;
		public bool interact;

		public event Action<bool> OnInteraction;

        [Header("Movement Settings")]
		public bool analogMovement;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;

        #region Input Actions
        private PlayerInput _playerInput;

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
        }

        private void OnEnable()
        {
            // Subscribe ke event C# (Ini yang bikin kenceng!)
            _playerInput.onActionTriggered += OnActionTriggered;
        }

        private void OnDisable()
        {
            // Unsubscribe agar tidak memory leak
            _playerInput.onActionTriggered -= OnActionTriggered;
        }

        private void OnActionTriggered(InputAction.CallbackContext context)
        {
            // Cek nama action dan panggil fungsi yang sesuai
            switch (context.action.name)
            {
                case "Move":
                    MoveInput(context.ReadValue<Vector2>());
                    break;
                case "Look":
                    if (cursorInputForLook) LookInput(context.ReadValue<Vector2>());
                    break;
                case "Jump":
                    JumpInput(context.ReadValueAsButton());
                    break;
                case "Sprint":
                    SprintInput(context.ReadValueAsButton());
                    break;
                case "View":
                    SwitchView(context.ReadValueAsButton());
                    break;
                case "Interact":
                    InteractInput(context.ReadValueAsButton());
                    break;
            }
        }
        #endregion

        public void MoveInput(Vector2 newMoveDirection)
		{
			move = newMoveDirection;
		} 

		public void LookInput(Vector2 newLookDirection)
		{
			look = newLookDirection;
		}

		public void JumpInput(bool newJumpState)
		{
			jump = newJumpState;
		}

		public void SprintInput(bool newSprintState)
		{
			sprint = newSprintState;
		}

        public void SwitchView(bool switchView)
        {
            isSwitchingView = switchView;

            // Auto-clear flag on release to prevent stuck state.
            // This is necessary because StarterAssetsInputs doesn't do it automatically.
            if (!switchView)
            {
                isSwitchingView = false;
            }
        }

		public void InteractInput(bool newInteractState)
		{
			interact = newInteractState;

            // Null-safe: the scene may run without any subscriber (e.g. no puzzle system yet).
            OnInteraction?.Invoke(newInteractState);
        }

        private void OnApplicationFocus(bool hasFocus)
		{
			SetCursorState(cursorLocked);
		}

		private void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
		}
	}
	
}