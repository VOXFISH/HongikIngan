using System;
using System.Collections;
using System.IO;
using System.Linq;
using HongikIngan.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace HongikIngan
{
    /// <summary>Opt-in player integration checks that exercise the real scene and input handlers.</summary>
    public sealed class BattleUiSmoke : MonoBehaviour
    {
        string output;
        int assertions;
        float originalVolume;
        bool originalMute, capturedSettings;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartIfRequested()
        {
            if (!Environment.GetCommandLineArgs().Contains("--battle-ui-smoke")) return;
            var go = new GameObject("BattleUiSmoke");
            DontDestroyOnLoad(go); go.AddComponent<BattleUiSmoke>();
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
                    File.WriteAllText(Path.Combine(output, "battle-ui-result.txt"), "FAIL: " + failure);
                    RestoreSettings(); Application.Quit(1); yield break;
                }
                if (!more) break;
                yield return next;
            }
            RestoreSettings();
            File.WriteAllText(Path.Combine(output, "battle-ui-result.txt"), "PASS assertions=" + assertions +
                ": authored layout, cursor drag/drop/cancel, modal lock, duplicate attack lock, victory return, three-turn defeat, lifecycle interruption, audio persistence.");
            Application.Quit(0);
        }
        IEnumerator CheckScene()
        {
            while (!UnityEngine.Rendering.SplashScreen.isFinished) yield return null;
            var sound = SoundManager.EnsureExists();
            originalVolume = sound.GetVolume(AudioCategory.BGM); originalMute = sound.GetMuted(AudioCategory.Voice); capturedSettings = true;
            var session = PrototypeSession.Ensure();
            session.Run.PracticeBattles = false;
            session.Run.Students.Add(new StudentState(PrototypeCatalog.Students[1]));
            session.Run.Settings.bossBaseHp = 1;
            session.EnterBattle();
            yield return new WaitForSeconds(.3f);
            var screen = FindFirstObjectByType<BattleScreen>();
            Check(screen != null && screen.CanReorder, "battle scene initialized");
            var slots = screen.formation.slots;
            Check(slots.Length == 9 && slots[0].HasStudent && slots[1].HasStudent, "nine serialized slots bound");
            var rect = (RectTransform)slots[0].transform;
            rect.anchoredPosition += new Vector2(7, 3);
            var authored = rect.anchoredPosition;
            var student = session.Run.Students[0];
            var visualParent = slots[0].studentVisual.parent;
            var original = slots[0].studentVisual.position;
            var e = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(null, original), pointerDrag = slots[0].gameObject };
            slots[0].OnBeginDrag(e);
            Check(slots[0].IsDragging && !slots[0].visualGroup.blocksRaycasts, "drag releases raycasts");
            e.position += new Vector2(93, -47);
            slots[0].OnDrag(e);
            Check(Vector3.Distance(slots[0].studentVisual.position - original, new Vector3(93, -47)) < .1f, "cursor movement matches");
            slots[0].OnEndDrag(e);
            Check(slots[0].studentVisual.parent == visualParent && Vector3.Distance(slots[0].studentVisual.position, original) < .1f, "invalid drop restores layout");
            slots[0].OnBeginDrag(e); slots[1].OnDrop(e); slots[0].OnEndDrag(e);
            Check(ReferenceEquals(session.Run.Students[1], student), "drop exchanges model order");
            Check(rect.anchoredPosition == authored, "model refresh preserves authored position");
            screen.audioPanel.openButton.onClick.Invoke();
            Check(screen.audioPanel.IsOpen && !screen.CanReorder && !screen.hud.actionButton.interactable, "modal locks commands");
            screen.PerformAction(); slots[0].OnBeginDrag(e);
            Check(!screen.IsRunning && !slots[0].IsDragging, "modal rejects direct command and drag");
            screen.audioPanel.bgmVolume.value = .23f; screen.audioPanel.voiceMute.isOn = !originalMute;
            Check(Math.Abs(sound.GetVolume(AudioCategory.BGM) - .23f) < .001f, "volume input bound");
            Check(sound.GetMuted(AudioCategory.Voice) == !originalMute, "mute input bound");
            screen.audioPanel.closeButton.onClick.Invoke();
            Check(screen.CanReorder, "close unlocks commands");
            // Render representative UI with all nine slots, then restore the actual run.
            var previewSettings = ScriptableObject.CreateInstance<EconomySettings>();
            var preview = new PrototypeRun(previewSettings);
            preview.Relics.Add(new RelicDefinition { Id = "preview_slots", Kind = RelicKind.ExtraSlots, Value = 6 });
            for (int i = 1; i < 9; i++) preview.Students.Add(new StudentState(PrototypeCatalog.Students[i]));
            screen.formation.Render(preview);
            Screen.SetResolution(1440, 900, false);
            yield return new WaitForSeconds(.3f);
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "battle-nine-slots.png"));
            yield return new WaitForSeconds(.3f);
            Screen.SetResolution(1280, 720, false);
            yield return new WaitForSeconds(.3f);
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "battle-1280x720.png"));
            yield return new WaitForSeconds(.3f);
            Destroy(previewSettings);
            screen.formation.Render(session.Run);
            screen.presentation.stepDuration = .01f;
            screen.hud.actionButton.onClick.Invoke();
            int turn = session.Run.Battle.Turn;
            screen.hud.actionButton.onClick.Invoke();
            slots[0].OnBeginDrag(e);
            Check(screen.IsRunning && !slots[0].IsDragging && session.Run.Battle.Turn == turn, "running input lock");
            float deadline = Time.realtimeSinceStartup + 10;
            while (screen.IsRunning && Time.realtimeSinceStartup < deadline) yield return null;
            Check(!screen.IsRunning && session.Run.Battle.Won, "actual controller reaches victory");
            screen.hud.actionButton.onClick.Invoke();
            yield return new WaitForSeconds(.2f);
            Check(SceneManager.GetActiveScene().name == "School" && session.Run.Stage == 2, "victory returns to school");
            Check(SoundManager.Instance == sound && Math.Abs(sound.GetVolume(AudioCategory.BGM) - .23f) < .001f &&
                sound.GetMuted(AudioCategory.Voice) == !originalMute, "audio survives transition");
            session.ResetRun(); session.Run.Settings.bossBaseHp = 1e30f;
            session.Run.PracticeBattles = false;
            session.EnterBattle(); yield return new WaitForSeconds(.2f);
            screen = FindFirstObjectByType<BattleScreen>(); screen.presentation.stepDuration = .01f;
            for (int i = 1; i <= 3; i++)
            {
                screen.PerformAction(); deadline = Time.realtimeSinceStartup + 10;
                while (screen.IsRunning && Time.realtimeSinceStartup < deadline) yield return null;
                Check(!screen.IsRunning && session.Run.Battle.Turn == i, "turn completion " + i);
                Check(session.Run.Battle.Lost == (i == 3), "three-turn defeat boundary " + i);
            }
            screen.hud.actionButton.onClick.Invoke(); yield return new WaitForSeconds(.2f);
            Check(SceneManager.GetActiveScene().name == "School" && session.Run.Stage == 1, "defeat restarts");
            session.Run.PracticeBattles = false;
            session.EnterBattle(); yield return new WaitForSeconds(.2f);
            screen = FindFirstObjectByType<BattleScreen>(); screen.PerformAction();
            screen.enabled = false;
            Check(!session.Run.Battle.IsResolving && !screen.IsRunning, "interruption disposes iterator");
            screen.enabled = true;
            Check(screen.hud.actionLabel.text.Contains("중단") && !screen.CanReorder, "interruption requires restart");
            screen.PerformAction(); yield return new WaitForSeconds(.2f);
            Check(SceneManager.GetActiveScene().name == "School", "interruption recovery");
        }
        void Check(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new InvalidOperationException(message);
        }
        void RestoreSettings()
        {
            if (!capturedSettings || SoundManager.Instance == null) return;
            SoundManager.Instance.SetVolume(AudioCategory.BGM, originalVolume);
            SoundManager.Instance.SetMuted(AudioCategory.Voice, originalMute);
            SoundManager.Instance.SaveSettings();
        }
    }
}
