using TMPro;
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
        public TMP_Text title;
        [Min(0)] public float fadeDuration = .18f;
        [Min(0)] public float holdDuration = .2f;
        public bool IsBusy { get; private set; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic() { Current = null; }
        void Awake()
        {
            if (Current != null && Current != this) { Destroy(gameObject); return; }
            Current = this; DontDestroyOnLoad(gameObject);
            curtain.alpha = 0; curtain.blocksRaycasts = false;
        }
        public void Go(string message, Action changeScene, IEnumerator beforeFade = null)
        {
            if (IsBusy) return;
            StartCoroutine(Transition(message, changeScene, beforeFade));
        }
        IEnumerator Transition(string message, Action changeScene, IEnumerator beforeFade)
        {
            IsBusy = true; curtain.blocksRaycasts = true; title.text = message;
            try
            {
                if (beforeFade != null)
                {
                    // Keep the students visible while the battle-start overlay begins.
                    curtain.alpha = .12f;
                    yield return beforeFade;
                }
                yield return Fade(curtain.alpha, 1);
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
        void OnDestroy() { if (Current == this) Current = null; }
    }
}
