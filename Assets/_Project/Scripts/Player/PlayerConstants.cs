using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// Konstanta bersama untuk sistem pemain.
    /// </summary>
    public static class PlayerConstants
    {
        public const float TerminalVelocity = 53.0f;
        public const float LandingVelocityThreshold = -3.0f;
        public const float GroundedVelocityReset = -2.0f;
        public const float AnimationBlendThreshold = 0.01f;
        public const float SpeedMatchThreshold = 0.1f;
        public const float LookSqrThreshold = 0.01f;
        public const float SpeedRoundFactor = 1000f;
    }
}
