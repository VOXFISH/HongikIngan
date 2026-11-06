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
                ": authored roster including photography in actual Battle scene, metadata, upgrades, clone display, controller resolution, school return and persistent atmosphere.");
            Application.Quit(0);
        }
        IEnumerator CheckScenes()
        {
            while (!UnityEngine.Rendering.SplashScreen.isFinished) yield return null;
            var session = PrototypeSession.Ensure();
            var run = session.Run;
            run.AddMoney(10000);
            run.Settings.bossBaseHp = 1;
            for (int i = 0; i < 4; i++) Check(run.ExpandSlots(), "expand test formation to seven");
            run.Photos["smoke-seed"] = 1;
            for (int group = 0; group < 7; group++)
            {
                run.Students.Clear();
                var roster = group < 2 ? PrototypeCatalog.Students.Skip(group * 6).Take(6) : group == 2 ?
                    PrototypeCatalog.Students.Where(d => d.Club == "사진부").Concat(PrototypeCatalog.Students.Where(d => d.Club != "사진부")).Take(7) :
                    group == 3 ? PrototypeCatalog.Students.Skip(18).Take(6) :
                    group == 4 ? PrototypeCatalog.Students.Skip(24).Take(3) :
                    group == 5 ? PrototypeCatalog.Students.Skip(27).Take(3) : PrototypeCatalog.Students.Skip(30).Take(6);
                run.Students.AddRange(roster.Select(d => new StudentState(d)));
                if (group >= 1) for (int i = 0; i < run.Students.Count; i++) Check(run.Upgrade(i), "upgrade girl " + group + "/" + i);
                session.EnterBattle();
                yield return new WaitForSeconds(1.5f);
                var screen = FindFirstObjectByType<BattleScreen>();
                Check(screen != null && screen.CanReorder && SceneManager.GetActiveScene().name == "Battle", "battle scene ready " + group);
                for (int i = 0; i < run.Students.Count; i++)
                {
                    var d = run.Students[i].Definition; var slot = screen.formation.StudentSlot(i);
                    Check(slot.HasStudent && slot.nameLabel.text == d.Name, "girl name rendered " + d.Id);
                    slot.nameLabel.ForceMeshUpdate();
                    Check(!slot.nameLabel.isTextTruncated, "full girl name fits authored card " + d.Id);
                    Check(slot.roleLabel.text == d.Grade + "학년 - " + d.RoleLabel, "grade and role rendered " + d.Id);
                    Check(slot.synergyLabel.text.Contains(d.Club) && slot.synergyLabel.text.Contains(d.Personality), "synergies rendered " + d.Id);
                    Check(slot.skillLabel.text == d.SkillName, "skill name rendered " + d.Id);
                }
                Screen.SetResolution(group == 0 ? 1440 : 1280, group == 0 ? 900 : 720, FullScreenMode.Windowed);
                yield return new WaitForSeconds(.5f);
                ScreenCapture.CaptureScreenshot(Path.Combine(output, "girls-group-" + group + ".png"));
                yield return new WaitForSeconds(1.5f);
                screen.presentation.stepDuration = .001f;
                screen.hud.actionButton.onClick.Invoke();
                float deadline = Time.realtimeSinceStartup + 90;
                bool cloneSeen = false, cloneDisplayed = false;
                while (screen.IsRunning && Time.realtimeSinceStartup < deadline)
                {
                    if (group == 2 && run.Students.Any(s => s.IsClone))
                    {
                        if (!cloneSeen) ScreenCapture.CaptureScreenshot(Path.Combine(output, "photography-clones-1280x720.png"));
                        cloneSeen = true;
                        cloneDisplayed |= screen.formation.slots.Any(slot => slot.gameObject.activeSelf && slot.nameLabel.text.Contains("(분신)"));
                    }
                    yield return null;
                }
                Check(!screen.IsRunning && run.Battle.Won && !float.IsNaN(run.Battle.Damage), "authored team resolves finite victory " + group);
                if (group == 2) Check(cloneSeen && cloneDisplayed && run.Students.All(s => !s.IsClone), "clones appear in battle slots and expire on victory");
                float atmosphere = run.Atmosphere;
                Check(atmosphere > 0, "mood maker changes persistent atmosphere " + group);
                screen.rewardPopup.claimButton.onClick.Invoke();
                yield return new WaitForSeconds(1.5f);
                Check(SceneManager.GetActiveScene().name == "School" && run.Atmosphere == atmosphere, "victory preserves atmosphere " + group);
                Check(run.Stage == group + 2, "advance stage " + group);
            }
            run.Students.Clear(); run.Students.Add(new StudentState(PrototypeCatalog.Students.Single(d => d.Id == "Girl28")));
            run.Photos.Clear(); run.Settings.bossBaseHp = 1e30f; run.Settings.bossGrowth = 1;
            session.EnterBattle(); yield return new WaitForSeconds(1.5f);
            var rescueScreen = FindFirstObjectByType<BattleScreen>(); rescueScreen.presentation.stepDuration = .001f;
            for (int turn = 0; turn < 3; turn++)
            {
                rescueScreen.PerformAction(); float deadline = Time.realtimeSinceStartup + 15;
                while (rescueScreen.IsRunning && Time.realtimeSinceStartup < deadline) yield return null;
                yield return new WaitForSeconds(1);
            }
            Check(run.Battle.Saved && !run.GameOver && rescueScreen.rewardPopup.IsOpen, "sun rescue opens result reward popup");
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "sun-rescue-result.png")); yield return new WaitForSeconds(.3f);
            rescueScreen.rewardPopup.claimButton.onClick.Invoke(); yield return new WaitForSeconds(1.5f);
            Check(SceneManager.GetActiveScene().name == "School" && run.SunRescueUsed, "sun rescue claims and returns to school");
        }
        void Check(bool condition, string message)
        { assertions++; if (!condition) throw new InvalidOperationException(message); }
    }
}
