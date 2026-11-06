using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HongikIngan
{
    /// <summary>Opt-in player checks for the authored school scene and its purchase buttons.</summary>
    public sealed class SchoolEconomySmoke : MonoBehaviour
    {
        string output;
        int assertions;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartIfRequested()
        {
            if (!Environment.GetCommandLineArgs().Contains("--school-economy-smoke")) return;
            var go = new GameObject("SchoolEconomySmoke");
            DontDestroyOnLoad(go);
            go.AddComponent<SchoolEconomySmoke>();
        }

        IEnumerator Start()
        {
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "--smoke-output");
            output = index >= 0 && index + 1 < args.Length ? args[index + 1] : Path.Combine(Application.dataPath, "../Smoke");
            Directory.CreateDirectory(output);
            Application.runInBackground = true;
            var checks = CheckScene();
            while (true)
            {
                object next = null;
                bool more = false;
                Exception failure = null;
                try { more = checks.MoveNext(); if (more) next = checks.Current; }
                catch (Exception e) { failure = e; }
                if (failure != null)
                {
                    File.WriteAllText(Path.Combine(output, "school-economy-result.txt"), "FAIL: " + failure);
                    Application.Quit(1);
                    yield break;
                }
                if (!more) break;
                yield return next;
            }
            File.WriteAllText(Path.Combine(output, "school-economy-result.txt"), "PASS assertions=" + assertions +
                ": fortune animation, input lock, text-only result, immediate effects, daily reset and preserved pets.");
            Application.Quit(0);
        }

        IEnumerator CheckScene()
        {
            while (!UnityEngine.Rendering.SplashScreen.isFinished) yield return null;
            Screen.SetResolution(1440, 810, FullScreenMode.Windowed);
            yield return new WaitForSeconds(.5f);
            if (SceneManager.GetActiveScene().name == "Main")
            {
                var menu = FindFirstObjectByType<MainMenuScreen>();
                Check(menu != null, "main menu is the entry scene");
                menu.startButton.onClick.Invoke();
                yield return new WaitForSeconds(2f);
            }
            var session = PrototypeSession.Ensure();
            var run = session.Run;
            run.AddMoney(3000);
            Check(run.ExpandSlots(), "expand full starting roster for pet selection");
            run.Settings.bossBaseHp = 1;
            var screen = FindFirstObjectByType<SchoolScreen>();
            Check(screen != null && screen.CanInteract, "school scene ready");
            screen.Refresh();
            Check(screen.schoolCanvas.transform.Find("Recruitment") == null &&
                screen.schoolCanvas.transform.Find("StudentDetailBook/Recruit") == null &&
                screen.schoolCanvas.transform.Find("StudentDetailBook/Replace") == null, "recruitment UI removed from scene");
            Check(screen.relicButtons.Length == 1 && screen.petButtons.Length == 3, "one fortune and three pet buttons bound");
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "school-1440x810.png"));
            yield return new WaitForSeconds(2f);

            var pets = run.PetOffers.ToArray();
            int money = run.Money, cost = run.RerollCost;
            float started = Time.unscaledTime;
            screen.rerollButton.onClick.Invoke();
            Check(run.IsFortuneRolling && !screen.rerollButton.interactable && !screen.CanInteract, "roll disables button and school transactions");
            Check(run.Money == money - cost && run.Fortune == null, "payment happens once before result");
            money = run.Money;
            screen.rerollButton.onClick.Invoke(); screen.battleButton.onClick.Invoke();
            Check(run.Money == money && !run.InBattle, "duplicate callbacks cannot charge or leave scene");
            var initialText = screen.relicLabels[0].text;
            yield return new WaitForSecondsRealtime(.15f);
            Check(run.IsFortuneRolling && screen.relicLabels[0].text != initialText, "fortune texts cycle rapidly");
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "fortune-spinning.png"));
            while (run.IsFortuneRolling) yield return null;
            Check(Time.unscaledTime - started >= .8f && Time.unscaledTime - started < 1.1f, "result stops at 0.8 seconds within frame tolerance");
            Check(run.Fortune != null && screen.relicLabels[0].text.Contains(run.Fortune.Description), "selected result displayed as text");
            Check(screen.rerollButton.interactable && !screen.relicButtons[0].interactable && !screen.ownedRelicsLabel.gameObject.activeSelf, "reroll reenabled, no purchase or owned stack");
            Check(pets.SequenceEqual(run.PetOffers), "fortune keeps all pet offers");
            screen.relicLabels[0].ForceMeshUpdate();
            Check(!screen.relicLabels[0].isTextTruncated, "fortune fits existing UI card");
            foreach (var fortune in PrototypeCatalog.Fortunes)
            {
                screen.relicLabels[0].text = fortune.Description + "\n귀염둥이: 효과 2배";
                screen.relicLabels[0].ForceMeshUpdate();
                Check(!screen.relicLabels[0].isTextTruncated, "all fortune descriptions fit: " + fortune.Id);
            }
            screen.Refresh();
            for (int slot = 0; slot < 3; slot++)
            {
                pets = run.PetOffers.ToArray(); money = run.Money;
                cost = run.PetPrice(pets[slot].Value);
                screen.petButtons[slot].onClick.Invoke();
                Check(run.PendingPet && run.Money == money - cost && run.PetOffers[slot].HasValue, "paid pet immediately refills slot " + slot);
                Check(Enumerable.Range(0, 3).Where(i => i != slot).All(i => run.PetOffers[i] == pets[i]), "other pet slots preserved " + slot);
                Check(!screen.CanInteract && !screen.schoolCanvas.interactable, "candidate selection locks school input " + slot);
                money = run.Money;
                screen.petButtons[slot].onClick.Invoke();
                Check(run.Money == money, "pending candidate prevents double purchase " + slot);
                if (slot == 0)
                {
                    var chosen = run.PendingCandidates[0];
                    Check(run.ChoosePet(0) && run.Owns(chosen.Id), "paid candidate recruits student without extra cost");
                }
                else Check(run.SkipPet(), "skip completes candidate selection " + slot);
                Check(run.Money == money && screen.CanInteract && screen.petButtons[slot].interactable, "refilled pet available after selection " + slot);
            }
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
            yield return new WaitForSeconds(.5f);
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "fortune-result-1280x720.png"));
            yield return new WaitForSeconds(2f);

            pets = run.PetOffers.ToArray();
            screen.battleButton.onClick.Invoke();
            yield return new WaitForSeconds(2f);
            Check(SceneManager.GetActiveScene().name == "Battle", "battle button transitions scene");
            run.ResolveNextTurn();
            Check(run.Battle.Won, "victory reached through combat");
            session.BackToSchool();
            yield return new WaitForSeconds(2f);
            screen = FindFirstObjectByType<SchoolScreen>();
            Check(SceneManager.GetActiveScene().name == "School" && screen.CanInteract && run.Stage == 2, "victory returns to next school");
            Check(run.Fortune == null && run.RerollCost == 20 && screen.rerollButton.interactable, "next day resets fortune and reroll price");
            Check(pets.SequenceEqual(run.PetOffers), "unused pets persist across scene transitions");
            screen.rerollButton.onClick.Invoke();
            yield return new WaitForSecondsRealtime(.9f);
            Check(run.Fortune != null && screen.rerollButton.interactable, "next day permits another fortune");
        }
        void Check(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
