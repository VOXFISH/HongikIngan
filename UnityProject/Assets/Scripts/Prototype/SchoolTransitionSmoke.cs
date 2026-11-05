using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HongikIngan
{
    // Opt-in player check: --school-transition-smoke --smoke-output <folder>.
    public sealed class SchoolTransitionSmoke : MonoBehaviour
    {
        string output;
        int assertions;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (!Environment.GetCommandLineArgs().Contains("--school-transition-smoke")) return;
            var go = new GameObject(nameof(SchoolTransitionSmoke));
            DontDestroyOnLoad(go); go.AddComponent<SchoolTransitionSmoke>();
        }
        IEnumerator Start()
        {
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "--smoke-output");
            output = index >= 0 ? args[index + 1] : Path.Combine(Application.persistentDataPath, "SchoolTransitionSmoke");
            Directory.CreateDirectory(output); Application.runInBackground = true;
            var checks = CheckTransition();
            while (true)
            {
                object next = null; bool more = false; Exception failure = null;
                try { more = checks.MoveNext(); if (more) next = checks.Current; }
                catch (Exception e) { failure = e; }
                if (failure != null)
                {
                    File.WriteAllText(Path.Combine(output, "result.txt"), "FAIL " + failure);
                    Application.Quit(1); yield break;
                }
                if (!more) break;
                yield return next;
            }
            File.WriteAllText(Path.Combine(output, "result.txt"), "PASS assertions=" + assertions +
                ": 36 paired portraits, centered full-screen curtain, white hold, outfit reveal, refresh preservation, input lock, School/Battle round trip.");
            Application.Quit();
        }
        IEnumerator CheckTransition()
        {
            while (!UnityEngine.Rendering.SplashScreen.isFinished) yield return null;
            Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
            FindFirstObjectByType<MainMenuScreen>().startButton.onClick.Invoke();
            yield return new WaitForSecondsRealtime(1);
            var school = FindFirstObjectByType<SchoolScreen>();
            var run = school.CurrentRun;
            run.Students.Clear();
            foreach (int id in new[] { 0, 5, 11 }) run.Students.Add(new StudentState(PrototypeCatalog.Students[id]));
            school.Refresh();
            var slots = school.slots.Where(s => s.studentPortrait != null && s.studentPortrait.enabled).ToArray();
            Check(slots.Length == 3, "three visible students");
            foreach (var entry in slots[0].portraitCatalog.entries)
                Check(entry.sprite != null && entry.magicalSprite != null, "both portraits: " + entry.studentId);
            CheckCentered();
            school.transformationDuration = 2;
            school.transformationWhiteHold = .3f;
            school.transformedOutfitHoldDuration = .3f;
            yield return new WaitForEndOfFrame(); Capture("01-school");
            var originals = slots.Select(s => s.studentPortrait.sprite).ToArray();
            school.battleButton.onClick.Invoke();
            Check(SceneTransitionOverlay.Current.IsBusy && !school.CanInteract, "transition locks interaction immediately");
            bool whiteSchool = false, whiteMagic = false, fadingMagic = false, revealed = false;
            float whiteTime = 0, start = Time.realtimeSinceStartup;
            while (school != null && Time.realtimeSinceStartup - start < 6)
            {
                yield return new WaitForEndOfFrame();
                if (school == null) break;
                var portrait = slots[0].studentPortrait;
                Check(portrait.material.shader.isSupported && portrait.material.HasProperty("_Flash"), "working transformation shader");
                float flash = portrait.materialForRendering.GetFloat("_Flash");
                bool changed = portrait.sprite != originals[0];
                if (flash >= .999f)
                {
                    whiteTime += Time.unscaledDeltaTime;
                    Check(SceneTransitionOverlay.Current.curtain.alpha == 0, "white silhouettes are not dimmed by curtain");
                    foreach (var slot in slots)
                        Check(slot.studentPortrait.materialForRendering.GetFloat("_Flash") >= .999f, "all students fully white");
                    if (!changed && !whiteSchool) { Capture("02-white-school"); whiteSchool = true; }
                    if (changed && !whiteMagic)
                    {
                        Capture("03-white-magic"); whiteMagic = true;
                        school.Refresh();
                        Check(portrait.sprite != originals[0] && portrait.materialForRendering.GetFloat("_Flash") >= .999f,
                            "refresh preserves white outfit swap");
                    }
                }
                if (changed && flash > .1f && flash < .9f && !fadingMagic)
                { Capture("04-revealing"); fadingMagic = true; }
                if (changed && flash == 0 && !revealed)
                { Capture("05-outfits"); revealed = true; }
            }
            Check(whiteSchool && whiteMagic && fadingMagic && revealed && whiteTime > .3f, "ordered white hold then smooth outfit reveal");
            yield return new WaitForSecondsRealtime(1);
            Check(SceneManager.GetActiveScene().name == "Battle" && !SceneTransitionOverlay.Current.IsBusy, "battle loaded after reveal");
            CheckCentered();
            SceneTransitionOverlay.Current.Go("학교로", () => SceneManager.LoadScene("School"));
            yield return new WaitForSecondsRealtime(.2f);
            yield return new WaitForEndOfFrame(); Capture("06-centered-curtain");
            yield return new WaitForSecondsRealtime(1);
            Check(SceneManager.GetActiveScene().name == "School", "round trip returns to school");
            Check(FindObjectsByType<SceneTransitionOverlay>(FindObjectsSortMode.None).Length == 1, "single persistent overlay");
            CheckCentered();
        }
        void CheckCentered()
        {
            Canvas.ForceUpdateCanvases();
            Check(Screen.width == 1920 && Screen.height == 1080, "1920x1080 rendering");
            var overlay = SceneTransitionOverlay.Current;
            var rect = (RectTransform)overlay.curtain.transform;
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            Check(Vector2.Distance(corners[0], Vector2.zero) < 1 &&
                Vector2.Distance(corners[2], new Vector2(Screen.width, Screen.height)) < 1, "curtain covers screen exactly");
            var title = overlay.title.rectTransform;
            var center = title.TransformPoint(title.rect.center);
            Check(Vector2.Distance(center, new Vector2(Screen.width * .5f, Screen.height * .5f)) < 1, "transition title centered");
        }
        void Capture(string name)
        {
            var canvas = name == "06-centered-curtain"
                ? SceneTransitionOverlay.Current.GetComponent<Canvas>()
                : FindFirstObjectByType<SchoolScreen>().schoolCanvas.GetComponentInParent<Canvas>();
            BattleUiCapture.Capture(canvas, Path.Combine(output, name + ".png"));
        }
        void Check(bool condition, string message)
        { assertions++; if (!condition) throw new InvalidOperationException(message); }
    }
}
