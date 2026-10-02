using Game.Core;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Mengatur kursor mouse: terkunci dan tersembunyi saat eksplorasi, bebas dan terlihat saat
    /// window puzzle terbuka.
    /// </summary>
    /// <remarks>
    /// Dikendalikan oleh <see cref="IGameStateProvider"/>, bukan oleh code puzzle, sehingga layar modal
    /// berikutnya otomatis mendapat perilaku kursor yang benar. <c>LateUpdate</c> menerapkan ulang state
    /// setiap frame karena OS melepaskan kursor saat window kehilangan fokus; menulis nilai yang sama
    /// adalah no-op di Unity, sehingga cara ini murah dan kursor tidak bisa tersangkut di mode salah.
    /// </remarks>
    public sealed class UIRoot : MonoBehaviour
    {
        [SerializeField] private bool _lockCursorInExploration = true;

        private IGameStateProvider _gameState;

        /// <summary>Disuntikkan oleh Composition Root.</summary>
        public void Construct(IGameStateProvider gameState)
        {
            if (_gameState != null) _gameState.OnStateChanged -= OnStateChanged;

            _gameState = gameState;

            if (_gameState == null) return;

            _gameState.OnStateChanged += OnStateChanged;
            Apply(_gameState.CurrentState);
        }

        private void LateUpdate()
        {
            if (_gameState == null) return;

            Apply(_gameState.CurrentState);
        }

        private void OnDestroy()
        {
            if (_gameState != null) _gameState.OnStateChanged -= OnStateChanged;

            _gameState = null;
        }

        private void OnStateChanged(GameState state) => Apply(state);

        private void Apply(GameState state)
        {
            bool locked = state == GameState.Exploration && _lockCursorInExploration;

            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
