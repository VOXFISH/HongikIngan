using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HongikIngan
{
    public sealed class MainFlowSmoke : MonoBehaviour
    {
        string output;
        int assertions;
        bool awaitingQuit;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (!Environment.GetCommandLineArgs().Contains("--main-flow-smoke")) return;
            var go = new GameObject("MainFlowSmoke"); DontDestroyOnLoad(go);
            go.AddComponent<MainFlowSmoke>();
        }

        IEnumerator Start()
        {
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "--smoke-output");
            output = index >= 0 && index + 1 < args.Length ? args[index + 1] : Path.Combine(Application.dataPath, "../MainSmoke");
            Directory.CreateDirectory(output);
            Application.runInBackground = true;
            Application.quitting += OnQuitting;
            var checks = CheckFlow();
            while (true)
            {
                object next = null; bool more = false; Exception failure = null;
                try { more = checks.MoveNext(); if (more) next = checks.Current; }
                catch (Exception e) { failure = e; }
                if (failure != null)
                {
                    awaitingQuit = false;
                    File.WriteAllText(Path.Combine(output, "main-flow-result.txt"), "FAIL: " + failure);
                    Application.Quit(1); yield break;
                }
                if (!more) break;
                yield return next;
            }
        }

        IEnumerator CheckFlow()
        {
            while (!UnityEngine.Rendering.SplashScreen.isFinished) yield return null;
            Screen.SetResolution(1440, 900, FullScreenMode.Windowed);
            yield return new WaitForSeconds(.4f);
            var menu = FindFirstObjectByType<MainMenuScreen>();
            Check(SceneManager.GetActiveScene().name == "Main" && menu != null, "Main is the first build scene");
            Check(menu.startButton.interactable && menu.exitButton.interactable, "menu controls are enabled");
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "main-1440x900.png"));
            yield return new WaitForSeconds(.3f);
            menu.startButton.onClick.Invoke();
            yield return new WaitForSeconds(.3f);
            var session = PrototypeSession.Ensure(); var run = session.Run;
            Check(SceneManager.GetActiveScene().name == "School", "start enters School");
            Check(run.Money == 100 && run.Students.Count == 3 && run.Stage == 1, "starting resources and roster");
            Check(run.Students.All(s => s.Definition.Grade <= 2) && run.Students.Select(s => s.Definition.Id).Distinct().Count() == 3, "unique first/second-years");
            var school = FindFirstObjectByType<SchoolScreen>();
            Check(school != null && school.CanInteract && school.energyLabel.text.Contains("100"), "initial school UI reflects run");
            foreach (var control in new[] { school.audioButton, school.petButtons[2], school.battleButton })
            {
                var corners = new Vector3[4]; ((RectTransform)control.transform).GetWorldCorners(corners);
                Check(corners.All(p => p.x >= -.5f && p.x <= Screen.width + .5f && p.y >= -.5f && p.y <= Screen.height + .5f), "school controls remain inside the 16:10 screen");
            }
            var roster = run.Students.ToArray();
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "initial-school-1440x900.png"));
            yield return new WaitForSeconds(.3f);
            school.battleButton.onClick.Invoke();
            yield return new WaitForSeconds(.3f);
            Check(SceneManager.GetActiveScene().name == "Battle" && session.Run == run, "School battle control preserves the run");
            Check(run.Students.SequenceEqual(roster) && run.Money == 100, "battle entry does not reroll or recharge starters");
            Check(FindFirstObjectByType<BattleScreen>().CanReorder, "Battle controller is ready");
            SceneManager.LoadScene("Main");
            yield return new WaitForSeconds(.3f);
            menu = FindFirstObjectByType<MainMenuScreen>();
            menu.startButton.onClick.Invoke();
            yield return new WaitForSeconds(.3f);
            Check(session.Run != run && session.Run.Money == 100 && session.Run.Students.Count == 3, "new start replaces prior progress");
            SceneManager.LoadScene("Main");
            yield return new WaitForSeconds(.3f);
            awaitingQuit = true;
            FindFirstObjectByType<MainMenuScreen>().exitButton.onClick.Invoke();
            yield return new WaitForSeconds(3);
            throw new InvalidOperationException("Exit button did not quit the player");
        }

        void OnQuitting()
        {
            if (awaitingQuit)
                File.WriteAllText(Path.Combine(output, "main-flow-result.txt"), "PASS assertions=" + (assertions + 1) + ": Main -> School -> Battle, fresh random roster, 100 energy, restart, actual exit button quits player.");
        }
        void OnDestroy() { Application.quitting -= OnQuitting; }
        void Check(bool value, string message) { assertions++; if (!value) throw new InvalidOperationException(message); }
    }
}
