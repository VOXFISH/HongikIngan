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
        [Min(0)] public float sweepInterval = .1f;
        [Min(.01f)] public float sweepDuration = .45f;
        [Min(.01f)] public float battleLogoDuration = .85f;
        public AnimationCurve battleLogoScale = new AnimationCurve(
            new Keyframe(0, 0), new Keyframe(.3f, 1), new Keyframe(.5f, .8f),
            new Keyframe(.72f, 1), new Keyframe(1, 0));
        public AnimationCurve battleLogoRotation = new AnimationCurve(
            new Keyframe(0, 0), new Keyframe(.3f, -12), new Keyframe(.5f, 10),
            new Keyframe(.72f, -7), new Keyframe(1, 0));
        public bool IsBusy { get; private set; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic() { Current = null; }
        void Awake()
        {
            if (Current != null && Current != this) { Destroy(gameObject); return; }
            Current = this; DontDestroyOnLoad(gameObject);
            curtain.alpha = 0; curtain.blocksRaycasts = false;
        }
        public void Go(string message, Action changeScene, IEnumerator beforeFade = null, GameObject sweepPrefab = null)
        {
            if (IsBusy) return;
            StartCoroutine(Transition(message, changeScene, beforeFade, sweepPrefab));
        }
        IEnumerator Transition(string message, Action changeScene, IEnumerator beforeFade, GameObject sweepPrefab)
        {
            IsBusy = true; curtain.blocksRaycasts = true; title.text = message;
            GameObject sweep = null;
            var band = title.transform.parent.gameObject;
            bool bandActive = band.activeSelf;
            try
            {
                if (beforeFade != null)
                {
                    // Do not tint the white silhouettes or hide the outfit reveal.
                    curtain.alpha = 0;
                    yield return beforeFade;
                }
                if (sweepPrefab != null)
                {
                    band.SetActive(false);
                    sweep = Instantiate(sweepPrefab, curtain.transform, false);
                    sweep.SetActive(true);
                    var root = (RectTransform)sweep.transform;
                    Canvas.ForceUpdateCanvases();
                    var tiles = new Image[3];
                    for (int i = 0; i < tiles.Length; i++)
                        tiles[i] = root.Find("Tile" + (i + 1)).GetComponent<Image>();
                    var logo = root.Find("BattleStartLogo") as RectTransform;
                    if (logo != null) logo.localScale = Vector3.zero;
                    // Start every tile completely outside the viewport, ordered bottom to top.
                    float width = root.rect.width;
                    float height = root.rect.height;
                    float tileWidth = Mathf.Max(width * 1.85f, height * 1.85f);
                    float distance = (width + tileWidth) * .5f + 8;
                    foreach (var tile in tiles)
                    {
                        tile.rectTransform.sizeDelta = new Vector2(tileWidth, height * 1.06f);
                        tile.rectTransform.anchoredPosition = new Vector2(-distance, 0);
                    }
                    curtain.alpha = 1;
                    var background = curtain.GetComponent<Image>();
                    bool backgroundEnabled = background != null && background.enabled;
                    if (background != null) background.enabled = false;
                    try
                    {
                        yield return Sweep(tiles, -distance, 0, logo);
                        changeScene();
                        yield return null;
                        if (logo != null)
                        {
                            // Purple arrives first; the logo already began while tiles 2 and 3 entered.
                            float elapsed = Mathf.Max(0, sweepInterval) * (tiles.Length - 1);
                            for (; elapsed < battleLogoDuration; elapsed += Time.unscaledDeltaTime)
                            {
                                SetLogoPose(logo, elapsed);
                                yield return null;
                            }
                            SetLogoPose(logo, battleLogoDuration);
                        }
                        yield return new WaitForSecondsRealtime(holdDuration);
                        yield return Sweep(tiles, 0, distance);
                    }
                    finally { if (background != null) background.enabled = backgroundEnabled; }
                    yield break;
                }
                yield return Fade(curtain.alpha, 1);
                changeScene();
                yield return null;
                yield return new WaitForSecondsRealtime(holdDuration);
                yield return Fade(1, 0);
            }
            finally
            {
                if (sweep != null) Destroy(sweep);
                band.SetActive(bandActive);
                IsBusy = false; curtain.alpha = 0; curtain.blocksRaycasts = false;
            }
        }
        void SetLogoPose(RectTransform logo, float elapsed)
        {
            float progress = Mathf.Clamp01(elapsed / Mathf.Max(.01f, battleLogoDuration));
            logo.localScale = Vector3.one * Mathf.Clamp01(battleLogoScale.Evaluate(progress));
            logo.localRotation = Quaternion.Euler(0, 0, battleLogoRotation.Evaluate(progress));
        }
        IEnumerator Sweep(Image[] tiles, float from, float to, RectTransform logo = null)
        {
            float duration = Mathf.Max(.01f, sweepDuration);
            float interval = Mathf.Max(0, sweepInterval);
            float end = duration + interval * Mathf.Max(0, tiles.Length - 1);
            for (float time = 0; time < end; time += Time.unscaledDeltaTime)
            {
                for (int i = 0; i < tiles.Length; i++)
                {
                    float progress = Mathf.SmoothStep(0, 1, Mathf.Clamp01((time - i * interval) / duration));
                    tiles[i].rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(from, to, progress), 0);
                }
                if (logo != null && time >= duration) SetLogoPose(logo, time - duration);
                yield return null;
            }
            foreach (var tile in tiles) tile.rectTransform.anchoredPosition = new Vector2(to, 0);
            if (logo != null) SetLogoPose(logo, end - duration);
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
