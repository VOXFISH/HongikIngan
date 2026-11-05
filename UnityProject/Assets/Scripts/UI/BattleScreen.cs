using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HongikIngan
{
    /// <summary>Connects scene views to the existing run. Combat rules remain in CombatEngine.</summary>
    public sealed class BattleScreen : MonoBehaviour
    {
        public BattleHud hud;
        public BattleFormationView formation;
        public BattlePresentation presentation;
        public BattleAudioPanel audioPanel;
        PrototypeRun run;
        IEnumerator<BattleStep> resolution;
        bool running, interrupted, initialized;
        string current = "준비가 되면 합동 공격을 시작하세요.";

        public bool CanReorder => isActiveAndEnabled && initialized && !running && !interrupted &&
            !audioPanel.IsOpen && run.Battle != null && !run.Battle.IsResolving && !run.Battle.Won && !run.Battle.Lost;
        public bool IsRunning => running;

        void Start()
        {
            if (hud == null || formation == null || presentation == null || audioPanel == null)
            {
                Debug.LogError("Battle UI references are missing. Apply the Battle layout in the Editor.", this);
                enabled = false;
                return;
            }
            run = PrototypeSession.Ensure().Run;
            if (!run.InBattle) run.StartBattle();
            formation.Bind(this);
            initialized = true;
            Subscribe();
            presentation.BeginBattle();
            Refresh();
        }
        void OnEnable() { if (initialized) { Subscribe(); Refresh(); } }
        void Subscribe()
        {
            run.Changed += Refresh;
            hud.actionButton.onClick.AddListener(PerformAction);
            hud.practiceReturnButton.onClick.AddListener(ReturnFromPractice);
            audioPanel.VisibilityChanged += ModalChanged;
        }
        void OnDisable()
        {
            if (!initialized) return;
            run.Changed -= Refresh;
            hud.actionButton.onClick.RemoveListener(PerformAction);
            hud.practiceReturnButton.onClick.RemoveListener(ReturnFromPractice);
            audioPanel.VisibilityChanged -= ModalChanged;
            formation.CancelDrag();
            // Partial effects cannot safely be replayed after interruption.
            if (running)
            {
                interrupted = true;
                current = "전투 진행이 중단되었습니다. 새 게임으로 다시 시작해 주세요.";
                StopAllCoroutines();
                resolution?.Dispose();
                resolution = null;
                running = false;
                presentation.Clear();
            }
        }
        void ModalChanged() { formation.CancelDrag(); Refresh(); }
        public void Swap(int first, int second)
        {
            if (CanReorder) run.Swap(first, second);
        }
        public void PerformAction()
        {
            if (!initialized || !isActiveAndEnabled || running || audioPanel.IsOpen) return;
            if (interrupted || run.Battle == null || run.Battle.Lost) PrototypeSession.Instance.Restart();
            else if (run.Battle.Won) PrototypeSession.Instance.BackToSchool();
            else if (!run.Battle.IsResolving && (!run.Battle.IsPracticeBattle || run.Battle.Turn < 3)) StartCoroutine(Attack());
        }
        public void ReturnFromPractice()
        {
            if (!initialized || !isActiveAndEnabled || audioPanel.IsOpen || !run.CanReturnFromPractice) return;
            // Return is available even while the third turn's presentation is playing.
            StopAllCoroutines();
            resolution?.Dispose();
            resolution = null;
            running = false;
            presentation.Clear();
            formation.CancelDrag();
            PrototypeSession.Instance.BackFromPractice();
        }
        IEnumerator Attack()
        {
            running = true;
            formation.CancelDrag();
            Refresh();
            resolution = run.ResolveTurn().GetEnumerator();
            try
            {
                while (resolution.MoveNext())
                {
                    var step = resolution.Current;
                    current = step.Message;
                    presentation.Show(step);
                    Refresh();
                    yield return new WaitForSeconds(presentation.stepDuration);
                    presentation.Clear();
                }
            }
            finally
            {
                resolution?.Dispose();
                resolution = null;
                running = false;
                presentation.Clear();
                Refresh();
            }
        }
        void Refresh()
        {
            if (!initialized) return;
            hud.Render(run, current, running, audioPanel.IsOpen, interrupted);
            formation.Render(run);
        }
        // Kept for existing callers and formatting checks.
        public static string FormatDamage(float value) => PrototypeCatalog.FormatDamage(value);
    }
}
