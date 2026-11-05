using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HongikIngan
{
    public sealed class PracticeBattleSmoke : MonoBehaviour
    {
        string output;
        int assertions;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartIfRequested()
        {
            if (!Environment.GetCommandLineArgs().Contains("--practice-smoke")) return;
            var go = new GameObject("PracticeBattleSmoke");
            DontDestroyOnLoad(go); go.AddComponent<PracticeBattleSmoke>();
        }
        IEnumerator Start()
        {
            Application.runInBackground = true;
            var args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, "--smoke-output");
            output = index >= 0 && index + 1 < args.Length ? args[index + 1] : Path.Combine(Application.dataPath, "../PracticeSmoke");
            Directory.CreateDirectory(output);
            var checks = Checks();
            while (true)
            {
                object next = null; bool more = false; Exception error = null;
                try { more = checks.MoveNext(); if (more) next = checks.Current; }
                catch (Exception e) { error = e; }
                if (error != null)
                {
                    File.WriteAllText(Path.Combine(output, "practice-result.txt"), "FAIL: " + error);
                    Application.Quit(1); yield break;
                }
                if (!more) break;
                yield return next;
            }
            File.WriteAllText(Path.Combine(output, "practice-result.txt"), "PASS assertions=" + assertions +
                ": infinite HP, third-turn return during animation and after completion, no fourth turn, no reward/stage advance, reentry.");
            Application.Quit(0);
        }
        IEnumerator Checks()
        {
            while (!UnityEngine.Rendering.SplashScreen.isFinished) yield return null;
            var session = PrototypeSession.Ensure();
            Check(session.Run.PracticeBattles, "test branch defaults to practice");
            session.EnterBattle(); yield return new WaitForSeconds(.3f);
            var screen = FindFirstObjectByType<BattleScreen>();
            Check(screen != null, "battle loaded");
            var battle = session.Run.Battle;
            Check(battle.IsPracticeBattle && float.IsPositiveInfinity(battle.BossHp), "infinite HP");
            Check(screen.hud.bossHpLabel.text.Contains("∞") && screen.hud.bossHpBar.value == 1, "infinite HP display without NaN");
            Check(!screen.hud.practiceReturnButton.interactable, "return initially locked");
            screen.ReturnFromPractice();
            Check(session.Run.InBattle, "early return command rejected");
            session.Run.Students[0].SkillValue = float.PositiveInfinity;
            screen.presentation.stepDuration = .01f;
            for (int i = 1; i <= 2; i++)
            {
                screen.PerformAction(); float deadline = Time.realtimeSinceStartup + 10;
                while (screen.IsRunning && Time.realtimeSinceStartup < deadline) yield return null;
                Check(!screen.IsRunning && battle.Turn == i && float.IsPositiveInfinity(battle.BossHp), "turn " + i);
                Check(!battle.Won && !battle.Lost && !screen.hud.practiceReturnButton.interactable, "no result or early return");
            }
            screen.presentation.stepDuration = .35f;
            screen.PerformAction();
            Check(battle.Turn == 3 && screen.IsRunning && screen.hud.practiceReturnButton.interactable, "return unlocks at third-turn start");
            screen.audioPanel.Open();
            Check(!screen.hud.practiceReturnButton.interactable, "modal blocks return");
            screen.ReturnFromPractice(); Check(session.Run.InBattle, "modal return command rejected");
            screen.audioPanel.Close();
            yield return new WaitForSeconds(.3f);
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "practice-third-turn.png"));
            yield return new WaitForSeconds(.3f);
            Check(screen.IsRunning, "return tested while presentation is running");
            int money = session.Run.Money, stage = session.Run.Stage;
            screen.hud.practiceReturnButton.onClick.Invoke();
            yield return new WaitForSeconds(.3f);
            Check(SceneManager.GetActiveScene().name == "School" && !session.Run.InBattle && session.Run.Battle == null, "return during third turn");
            Check(!battle.IsResolving && session.Run.Money == money && session.Run.Stage == stage, "iterator stopped without victory reward");
            session.Run.Students[0].SkillValue = session.Run.Students[0].Definition.InitialValue;
            session.EnterBattle(); yield return new WaitForSeconds(.3f);
            screen = FindFirstObjectByType<BattleScreen>(); battle = session.Run.Battle;
            Check(battle.Turn == 0 && float.IsPositiveInfinity(battle.BossHp), "new practice battle reset");
            screen.presentation.stepDuration = .01f;
            for (int i = 1; i <= 3; i++)
            {
                screen.PerformAction(); float deadline = Time.realtimeSinceStartup + 10;
                while (screen.IsRunning && Time.realtimeSinceStartup < deadline) yield return null;
                Check(!screen.IsRunning && battle.Turn == i && !battle.Lost && !battle.Won, "completed turn " + i);
            }
            Check(screen.hud.practiceReturnButton.interactable && !screen.hud.actionButton.interactable, "completed third turn can return");
            screen.PerformAction(); Check(battle.Turn == 3 && !screen.IsRunning, "no fourth turn");
            screen.hud.practiceReturnButton.onClick.Invoke(); yield return new WaitForSeconds(.3f);
            Check(SceneManager.GetActiveScene().name == "School" && session.Run.Stage == stage, "completed third-turn return");
        }
        void Check(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
