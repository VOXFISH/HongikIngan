using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HongikIngan
{
    /// <summary>Scene-authored recruitment/confirmation controls; never positions or creates UI.</summary>
    public sealed class SchoolDialogs : MonoBehaviour
    {
        public GameObject offers, candidates, replacement, restart;
        public Button caller, closeOffers, skip, replace, closeReplacement, cancelRestart, acceptRestart, closeStudent;
        public Button[] candidateButtons, replacementButtons;
        public TMP_Text[] candidateLabels, replacementLabels;
        public TMP_Text candidateStatus;
        public BattleAudioPanel audioPanel;
        SchoolScreen owner;
        public bool IsOpen => offers.activeSelf || candidates.activeSelf || restart.activeSelf || audioPanel.IsOpen;

        public void Bind(SchoolScreen screen)
        {
            owner = screen;
            caller.onClick.AddListener(() => { if (!owner.CanInteract) return; offers.SetActive(true); owner.Refresh(); });
            closeOffers.onClick.AddListener(() => { offers.SetActive(false); owner.Refresh(); });
            skip.onClick.AddListener(() => { owner.CurrentRun.SkipPet(); replacement.SetActive(false); owner.Refresh(); });
            replace.onClick.AddListener(() => { replacement.SetActive(true); owner.Refresh(); });
            closeReplacement.onClick.AddListener(() => { replacement.SetActive(false); owner.Refresh(); });
            cancelRestart.onClick.AddListener(() => { restart.SetActive(false); owner.Refresh(); });
            acceptRestart.onClick.AddListener(() => SceneTransitionOverlay.Current.Go("새로운 시작", () => PrototypeSession.Instance.Restart()));
            closeStudent.onClick.AddListener(owner.DeselectStudent);
            for (int i = 0; i < candidateButtons.Length; i++)
            {
                int index = i;
                candidateButtons[i].onClick.AddListener(() => { owner.CurrentRun.ChoosePet(index); owner.Refresh(); });
            }
            for (int i = 0; i < replacementButtons.Length; i++)
            {
                int index = i;
                replacementButtons[i].onClick.AddListener(() =>
                {
                    if (owner.CurrentRun.Sell(index)) { replacement.SetActive(false); owner.DeselectStudent(); }
                    owner.Refresh();
                });
            }
            audioPanel.VisibilityChanged += owner.Refresh;
        }
        public void ShowRestart() { restart.SetActive(true); owner.Refresh(); }
        public void Render(PrototypeRun run)
        {
            candidates.SetActive(run.PendingPet);
            if (run.PendingPet) offers.SetActive(false);
            else replacement.SetActive(false);
            candidateStatus.text = run.LastMessage;
            for (int i = 0; i < candidateButtons.Length; i++)
            {
                bool present = i < run.PendingCandidates.Count;
                candidateButtons[i].gameObject.SetActive(present);
                if (!present) continue;
                var d = run.PendingCandidates[i];
                candidateLabels[i].text = $"{d.Name}\n{d.Grade}학년 · {d.RoleLabel}\n\n{d.Club} · {d.Personality}\n\n{d.SkillName}\n{d.SkillDescription}\n\n영입하기";
                candidateButtons[i].interactable = run.PendingPet && run.Students.Count < run.Capacity;
            }
            for (int i = 0; i < replacementButtons.Length; i++)
            {
                bool present = i < run.Students.Count;
                replacementButtons[i].gameObject.SetActive(present);
                if (present) replacementLabels[i].text = run.Students[i].Definition.Name;
            }
        }
        void OnDestroy() { if (owner != null) audioPanel.VisibilityChanged -= owner.Refresh; }
    }
}
