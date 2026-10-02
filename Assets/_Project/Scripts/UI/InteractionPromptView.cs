using Game.Interaction;
using TMPro;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Menampilkan/menyembunyikan prompt interaksi berdasarkan target dari <see cref="Interactor"/>.
    /// </summary>
    /// <remarks>
    /// View murni: tidak mencari objek interaksi, hanya merespons event detector (pemisahan Model–View).
    /// Detector menghapus target saat input terkunci, sehingga prompt hilang saat puzzle dibuka.
    /// </remarks>
    public sealed class InteractionPromptView : MonoBehaviour
    {
        [SerializeField] private Interactor _interactor;
        [SerializeField] private GameObject _promptRoot;
        [SerializeField] private TMP_Text _promptText;

        private void OnEnable()
        {
            if (_interactor == null) return;

            _interactor.OnInteractableFound += ShowPrompt;
            _interactor.OnInteractableLost += HidePrompt;

            HidePrompt();
        }

        private void OnDisable()
        {
            if (_interactor == null) return;

            _interactor.OnInteractableFound -= ShowPrompt;
            _interactor.OnInteractableLost -= HidePrompt;
        }

        private void ShowPrompt(IInteractable interactable)
        {
            bool hasTarget = interactable != null && interactable.CanInteract();

            if (_promptRoot != null) _promptRoot.SetActive(hasTarget);

            if (hasTarget && _promptText != null)
            {
                _promptText.SetText(interactable.InteractionPrompt);
            }
        }

        private void HidePrompt()
        {
            if (_promptRoot != null) _promptRoot.SetActive(false);
        }
    }
}
