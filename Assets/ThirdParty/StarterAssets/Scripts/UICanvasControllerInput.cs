using UnityEngine;

namespace StarterAssets
{
    public class UICanvasControllerInput : MonoBehaviour
    {

        [Header("Output")]
        public StarterAssetsInputs starterAssetsInputs;

        public void VirtualMoveInput(Vector2 virtualMoveDirection)
        {
            if (starterAssetsInputs == null) { return; }
            starterAssetsInputs.MoveInput(virtualMoveDirection);
        }

        public void VirtualLookInput(Vector2 virtualLookDirection)
        {
            if (starterAssetsInputs == null) { return; }
            starterAssetsInputs.LookInput(virtualLookDirection);
        }

        public void VirtualJumpInput(bool virtualJumpState)
        {
            if (starterAssetsInputs == null) { return; }
            starterAssetsInputs.JumpInput(virtualJumpState);
        }

        public void VirtualSprintInput(bool virtualSprintState)
        {
            if (starterAssetsInputs == null) { return; }
            starterAssetsInputs.SprintInput(virtualSprintState);
        }

        public void VirtualViewInput(bool virtualSprintState)
        {
            if (starterAssetsInputs == null) { return; }
            starterAssetsInputs.SwitchView(virtualSprintState);
        }

        public void VirtualInteractInput(bool virtualSprintState)
        {
            if (starterAssetsInputs == null) { return; }
            starterAssetsInputs.InteractInput(virtualSprintState);
        }

    }

}
