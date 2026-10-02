using StarterAssets;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// Bridge sisi scene yang mengubah komponen pihak ketiga
    /// <see cref="StarterAssetsInputs"/> menjadi <see cref="IPlayerInput"/> dan mengelola umurnya.
    /// </summary>
    [RequireComponent(typeof(StarterAssetsInputs))]
    public sealed class PlayerInputSource : MonoBehaviour
    {
        [SerializeField] private StarterAssetsInputs _source;

        private PlayerInputReader _reader;

        /// <summary>
        /// Abstraksi input untuk pemain ini. Dibuat saat pertama diakses dan dilepas
        /// bersama komponen ini.
        /// </summary>
        public IPlayerInput Input
        {
            get
            {
                if (_reader != null) return _reader;

                if (_source == null) _source = GetComponent<StarterAssetsInputs>();
                if (_source != null) _reader = new PlayerInputReader(_source);

                return _reader;
            }
        }

        /// <summary>
        /// Mengembalikan input bridge untuk <paramref name="owner"/>, menambahkannya bila prefab
        /// dibuat sebelum komponen ini ada.
        /// </summary>
        /// <remarks>
        /// Menjaga prefab pemain tetap mandiri (berjalan tanpa wiring manual) sekaligus memastikan
        /// semua pemanggil berbagi SATU reader: dua reader pada perangkat yang sama akan memicu
        /// permintaan interaksi dua kali.
        /// </remarks>
        public static PlayerInputSource Resolve(Component owner)
        {
            if (owner == null) return null;

            PlayerInputSource existing = owner.GetComponentInParent<PlayerInputSource>();
            if (existing != null) return existing;

            StarterAssetsInputs inputs = owner.GetComponentInChildren<StarterAssetsInputs>(true);
            return inputs != null ? inputs.gameObject.AddComponent<PlayerInputSource>() : null;
        }

        private void Awake()
        {
            if (_source == null) _source = GetComponent<StarterAssetsInputs>();
        }

        private void OnDestroy()
        {
            _reader?.Dispose();
            _reader = null;
        }
    }
}
