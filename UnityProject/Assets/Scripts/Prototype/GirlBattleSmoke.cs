using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HongikIngan
{
    public sealed class GirlBattleSmoke : MonoBehaviour
    {
        string output;
        int assertions;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartIfRequested()
        {
            if (!Environment.GetCommandLineArgs().Contains("--girl-battle-smoke")) return;
            var host = new GameObject("GirlBattleSmoke"); DontDestroyOnLoad(host);
            host.AddComponent<GirlBattleSmoke>();
        }
        IEnumerator Start()
        {
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "--smoke-output");
            output = index >= 0 && index + 1 < args.Length ? args[index + 1] : Path.Combine(Application.dataPath, "../GirlSmoke");
            Directory.CreateDirectory(output);
            Application.runInBackground = true;
            var checks = CheckScenes();
            while (true)
            {
                object next = null; bool more = false; Exception failure = null;
                try { more = checks.MoveNext(); if (more) next = checks.Current; }
                catch (Exception e) { failure = e; }
                if (failure != null)
                {
                    File.WriteAllText(Path.Combine(output, "girl-battle-result.txt"), "FAIL: " + failure);
                    Application.Quit(1); yield break;
                }
                if (!more) break;
                yield return next;
            }
            File.WriteAllText(Path.Combine(output, "girl-battle-result.txt"), "PASS assertions=" + assertions +
                ": all twelve authored girls in actual Battle scene, metadata and skill names, upgrades, controller resolution, victory return, persistent atmosphere.");
            Application.Quit(0);
        }
        IEnumerator CheckScenes()
        {
            while (!UnityEngine.Rendering.SplashScreen.isFinished) yield return null;
            var session = PrototypeSession.Ensure();
            var run = session.Run;
            run.AddMoney(10000);
            run.Settings.bossBaseHp = 1;
            for (int i = 0; i < 3; i++) Check(run.ExpandSlots(), "expand test formation to six");
            for (int group = 0; group < 2; group++)
            {
                run.Students.Clear();
                run.Students.AddRange(PrototypeCatalog.Students.Skip(group * 6).Take(6).Select(d => new StudentState(d)));
                if (group == 1) for (int i = 0; i < 6; i++) Check(run.Upgrade(i), "upgrade discipline girl " + i);
                session.EnterBattle();
                yield return new WaitForSeconds(.3f);
                var screen = FindFirstObjectByType<BattleScreen>();
                Check(screen != null && screen.CanReorder && SceneManager.GetActiveScene().name == "Battle", "battle scene ready " + group);
                for (int i = 0; i < 6; i++)
                {
                    var d = run.Students[i].Definition; var slot = screen.formation.slots[i];
                    Check(slot.HasStudent && slot.nameLabel.text == d.Name, "girl name rendered " + d.Id);
                    Check(slot.roleLabel.text == d.Grade + "학년 · " + d.RoleLabel, "grade and role rendered " + d.Id);
                    Check(slot.synergyLabel.text.Contains(d.Club) && slot.synergyLabel.text.Contains(d.Personality), "synergies rendered " + d.Id);
                    Check(slot.skillLabel.text == d.SkillName, "skill name rendered " + d.Id);
                }
                Screen.SetResolution(group == 0 ? 1440 : 1280, group == 0 ? 900 : 720, FullScreenMode.Windowed);
                yield return new WaitForSeconds(.5f);
                ScreenCapture.CaptureScreenshot(Path.Combine(output, group == 0 ? "girls-sports-1440x900.png" : "girls-discipline-1280x720.png"));
                yield return new WaitForSeconds(.3f);
                screen.presentation.stepDuration = .001f;
                screen.hud.actionButton.onClick.Invoke();
                float deadline = Time.realtimeSinceStartup + 45;
                while (screen.IsRunning && Time.realtimeSinceStartup < deadline) yield return null;
                Check(!screen.IsRunning && run.Battle.Won && !float.IsNaN(run.Battle.Damage), "authored team resolves finite victory " + group);
                float atmosphere = run.Atmosphere;
                Check(atmosphere > 0, "mood maker changes persistent atmosphere " + group);
                screen.hud.actionButton.onClick.Invoke();
                yield return new WaitForSeconds(.3f);
                Check(SceneManager.GetActiveScene().name == "School" && run.Atmosphere == atmosphere, "victory preserves atmosphere " + group);
                Check(run.Stage == group + 2, "advance stage " + group);
            }
        }
        void Check(bool condition, string message)
        { assertions++; if (!condition) throw new InvalidOperationException(message); }
    }
}
