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
                ": no recruitment shop, one relic, reroll isolation, purchase lock, immediate pet refill, paid selection, victory reset and preserved pets.");
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
                yield return new WaitForSeconds(.3f);
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
            Check(screen.relicButtons.Length == 1 && screen.petButtons.Length == 3, "one relic and three pet buttons bound");
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "school-1440x810.png"));
            yield return new WaitForSeconds(.3f);

            var pets = run.PetOffers.ToArray();
            var relic = run.RelicOffers[0];
            int money = run.Money, cost = run.RerollCost;
            screen.rerollButton.onClick.Invoke();
            Check(run.Money == money - cost && run.RelicOffers.Count == 1 && run.RelicOffers[0].Id != relic.Id, "reroll button charges and replaces only relic");
            Check(pets.SequenceEqual(run.PetOffers), "reroll keeps all pets");
            relic = run.RelicOffers[0]; money = run.Money;
            screen.relicButtons[0].onClick.Invoke();
            Check(run.RelicPurchasedThisBattle && run.Relics.Contains(relic) && run.Money == money - run.Settings.relicPrice, "relic button buys once");
            Check(!screen.relicButtons[0].interactable && !screen.rerollButton.interactable, "purchase disables relic and reroll buttons");
            Check(screen.relicLabels[0].text.Contains("구매 완료"), "purchase status displayed");
            money = run.Money;
            screen.rerollButton.onClick.Invoke(); screen.relicButtons[0].onClick.Invoke();
            Check(run.Money == money && run.Relics.Count == 1, "repeated callbacks cannot bypass purchase lock");

            for (int slot = 0; slot < 3; slot++)
            {
                pets = run.PetOffers.ToArray(); money = run.Money;
                cost = run.Settings.PetPrice(pets[slot].Value);
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
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "school-purchased-1280x720.png"));
            yield return new WaitForSeconds(.3f);

            pets = run.PetOffers.ToArray();
            screen.battleButton.onClick.Invoke();
            yield return new WaitForSeconds(.3f);
            Check(SceneManager.GetActiveScene().name == "Battle", "battle button transitions scene");
            run.ResolveNextTurn();
            Check(run.Battle.Won, "victory reached through combat");
            session.BackToSchool();
            yield return new WaitForSeconds(.3f);
            screen = FindFirstObjectByType<SchoolScreen>();
            Check(SceneManager.GetActiveScene().name == "School" && screen.CanInteract && run.Stage == 2, "victory returns to next school");
            Check(!run.RelicPurchasedThisBattle && screen.relicButtons[0].interactable && screen.rerollButton.interactable, "next battle purchase and reroll unlocked");
            Check(pets.SequenceEqual(run.PetOffers), "unused pets persist across scene transitions");
            screen.relicButtons[0].onClick.Invoke();
            Check(run.Relics.Count == 2 && !screen.rerollButton.interactable, "next battle allows exactly one further purchase");
        }

        void Check(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
