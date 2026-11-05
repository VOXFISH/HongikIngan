using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace HongikIngan
{
    // The Canvas and all geometry are authored in the scene. Only opacity changes at runtime.
    public sealed class SceneTransitionOverlay : MonoBehaviour
    {
        public static SceneTransitionOverlay Current { get; private set; }
        public CanvasGroup curtain;
        public Text title;
        [Min(0)] public float fadeDuration = .18f;
        [Min(0)] public float holdDuration = .2f;
        public bool IsBusy { get; private set; }
        Font font;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic() { Current = null; }
        void Awake()
        {
            if (Current != null && Current != this) { Destroy(gameObject); return; }
            Current = this; DontDestroyOnLoad(gameObject);
            curtain.alpha = 0; curtain.blocksRaycasts = false;
            font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 40);
            title.font = font;
        }
        public void Go(string message, Action changeScene)
        {
            if (IsBusy) return;
            StartCoroutine(Transition(message, changeScene));
        }
        IEnumerator Transition(string message, Action changeScene)
        {
            IsBusy = true; curtain.blocksRaycasts = true; title.text = message;
            try
            {
                yield return Fade(0, 1);
                changeScene();
                yield return null;
                yield return new WaitForSecondsRealtime(holdDuration);
                yield return Fade(1, 0);
            }
            finally { IsBusy = false; curtain.alpha = 0; curtain.blocksRaycasts = false; }
        }
        IEnumerator Fade(float from, float to)
        {
            for (float time = 0; time < fadeDuration; time += Time.unscaledDeltaTime)
            { curtain.alpha = Mathf.Lerp(from, to, time / fadeDuration); yield return null; }
            curtain.alpha = to;
        }
        void OnDestroy() { if (Current == this) Current = null; if (font != null) Destroy(font); }
    }
}
