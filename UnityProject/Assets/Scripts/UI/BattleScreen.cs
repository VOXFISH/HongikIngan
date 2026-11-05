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
        public TurnTransitionView turnTransition;
        PrototypeRun run;
        IEnumerator<BattleStep> resolution;
        bool running, interrupted, initialized, preparing;
        string current = "준비가 되면 합동 공격을 시작하세요.";

        public bool CanReorder => isActiveAndEnabled && initialized && !running && !preparing && !interrupted &&
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
            StartCoroutine(PrepareTurn());
        }
        void OnEnable() { if (initialized) { Subscribe(); Refresh(); } }
        void Subscribe()
        {
            run.Changed += Refresh;
            hud.actionButton.onClick.AddListener(PerformAction);
            audioPanel.VisibilityChanged += ModalChanged;
        }
        void OnDisable()
        {
            if (!initialized) return;
            run.Changed -= Refresh;
            hud.actionButton.onClick.RemoveListener(PerformAction);
            audioPanel.VisibilityChanged -= ModalChanged;
            formation.CancelDrag();
            preparing = false;
            if (turnTransition != null) turnTransition.Hide();
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
            else StopAllCoroutines();
        }
        void ModalChanged() { formation.CancelDrag(); Refresh(); }
        public void Swap(int first, int second)
        {
            if (CanReorder) run.Swap(first, second);
        }
        public void PerformAction()
        {
            if (!initialized || !isActiveAndEnabled || running || preparing || audioPanel.IsOpen ||
                (SceneTransitionOverlay.Current != null && SceneTransitionOverlay.Current.IsBusy)) return;
            if (interrupted || run.Battle == null || run.Battle.Lost)
                ChangeScene("새로운 시작", () => PrototypeSession.Instance.Restart());
            else if (run.Battle.Won) ChangeScene("학교로!", () => PrototypeSession.Instance.BackToSchool());
            else if (!run.Battle.IsResolving) StartCoroutine(Attack());
        }
        static void ChangeScene(string message, System.Action action)
        {
            if (SceneTransitionOverlay.Current != null) SceneTransitionOverlay.Current.Go(message, action);
            else action();
        }
        IEnumerator PrepareTurn()
        {
            if (run.Battle == null || run.Battle.Won || run.Battle.Lost) yield break;
            preparing = true; formation.CancelDrag(); Refresh();
            while (SceneTransitionOverlay.Current != null && SceneTransitionOverlay.Current.IsBusy) yield return null;
            if (turnTransition != null) yield return turnTransition.Show(run.Battle.Turn + 1);
            preparing = false; Refresh();
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
            if (!run.Battle.Won && !run.Battle.Lost) yield return PrepareTurn();
        }
        void Refresh()
        {
            if (!initialized) return;
            hud.Render(run, current, running || preparing, audioPanel.IsOpen, interrupted);
            formation.Render(run);
        }
        // Kept for existing callers and formatting checks.
        public static string FormatDamage(float value) => PrototypeCatalog.FormatDamage(value);
    }
}
