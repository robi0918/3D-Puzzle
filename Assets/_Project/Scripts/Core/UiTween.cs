using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Helper tween UniTask minimal (scale + fade) untuk view UI.
    /// Tanpa dependensi DOTween/coroutine; setiap loop dibatalkan lewat <see cref="CancellationToken"/> pemanggil.
    /// </summary>
    public static class UiTween
    {
        /// <summary>Menganimasikan <c>localScale.x</c> dari <paramref name="from"/> ke <paramref name="to"/>.</summary>
        public static async UniTask ScaleXAsync(Transform target, float from, float to, float duration,
            CancellationToken cancellationToken)
        {
            if (target == null) return;

            if (duration <= 0f)
            {
                SetScaleX(target, to);
                return;
            }

            float elapsed = 0f;
            SetScaleX(target, from);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                SetScaleX(target, Mathf.LerpUnclamped(from, to, t));
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            SetScaleX(target, to);
        }

        /// <summary>Menganimasikan skala uniform (x = y = z) dari <paramref name="from"/> ke <paramref name="to"/>.</summary>
        public static async UniTask ScaleUniformAsync(Transform target, float from, float to, float duration,
            CancellationToken cancellationToken)
        {
            if (target == null) return;

            if (duration <= 0f)
            {
                SetUniformScale(target, to);
                return;
            }

            float elapsed = 0f;
            SetUniformScale(target, from);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetUniformScale(target, Mathf.LerpUnclamped(from, to, Mathf.Clamp01(elapsed / duration)));
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            SetUniformScale(target, to);
        }

        /// <summary>Menganimasikan alpha sebuah <see cref="CanvasGroup"/>.</summary>
        public static async UniTask FadeAsync(CanvasGroup group, float from, float to, float duration,
            CancellationToken cancellationToken)
        {
            if (group == null) return;

            group.alpha = from;

            if (duration <= 0f)
            {
                group.alpha = to;
                return;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.LerpUnclamped(from, to, Mathf.Clamp01(elapsed / duration));
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            group.alpha = to;
        }

        /// <summary>Menganimasikan anchored position sebuah <see cref="RectTransform"/>.</summary>
        public static async UniTask MoveAnchoredAsync(RectTransform target, Vector2 from, Vector2 to,
            float duration, CancellationToken cancellationToken)
        {
            if (target == null) return;

            target.anchoredPosition = from;

            if (duration <= 0f)
            {
                target.anchoredPosition = to;
                return;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                target.anchoredPosition = Vector2.LerpUnclamped(from, to, Mathf.Clamp01(elapsed / duration));
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            target.anchoredPosition = to;
        }

        private static void SetScaleX(Transform target, float x)
        {
            Vector3 scale = target.localScale;
            scale.x = x;
            target.localScale = scale;
        }

        private static void SetUniformScale(Transform target, float value)
        {
            target.localScale = new Vector3(value, value, value);
        }
    }
}
