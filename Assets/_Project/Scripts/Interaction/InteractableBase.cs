using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Game.Interaction
{
    /// <summary>
    /// Implementasi dasar <see cref="IInteractable"/>: prompt, flag aktif,
    /// highlight fokus, dan hook UnityEvent untuk wiring di Inspector.
    /// Turunkan class ini alih-alih mengimplementasi <see cref="IInteractable"/> langsung,
    /// lalu override <see cref="CanInteract"/> / <see cref="Interact"/> untuk perilaku khusus.
    /// </summary>
    public abstract class InteractableBase : MonoBehaviour, IInteractable
    {
        [Header("Interaction")]
        [SerializeField] private string _prompt = "Press E to interact";
        [SerializeField] private bool _isInteractable = true;

        [Header("Focus feedback")]
        [Tooltip("Objek anak opsional (outline / glow / particle) yang aktif selama pemain memandang objek ini.")]
        [SerializeField] private GameObject _focusHighlight;

        [Header("Events")]
        [FormerlySerializedAs("OnInteraction")]
        [SerializeField] private UnityEvent _onInteraction = new UnityEvent();

        public string InteractionPrompt => _prompt;

        public virtual bool CanInteract() => _isInteractable && isActiveAndEnabled;

        public virtual void Interact() => _onInteraction?.Invoke();

        public virtual void OnFocusEnter()
        {
            if (_focusHighlight != null) _focusHighlight.SetActive(true);
        }

        public virtual void OnFocusExit()
        {
            if (_focusHighlight != null) _focusHighlight.SetActive(false);
        }
    }
}