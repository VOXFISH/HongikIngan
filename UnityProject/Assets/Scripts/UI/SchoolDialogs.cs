using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HongikIngan
{
    /// <summary>Scene-authored recruitment/confirmation controls; never positions or creates UI.</summary>
    public sealed class SchoolDialogs : MonoBehaviour
    {
        public GameObject offers, candidates, restart;
        public Button caller, closeOffers, skip, cancelRestart, acceptRestart, closeStudent;
        public Button[] candidateButtons;
        public TMP_Text[] candidateLabels;
        public TMP_Text candidateStatus;
        public BattleAudioPanel audioPanel;
        SchoolScreen owner;
        public bool IsOpen => offers.activeSelf || candidates.activeSelf || restart.activeSelf || audioPanel.IsOpen;

        public void Bind(SchoolScreen screen)
        {
            owner = screen;
            caller.onClick.AddListener(() => { if (!owner.CanInteract) return; offers.SetActive(true); owner.Refresh(); });
            closeOffers.onClick.AddListener(() => { offers.SetActive(false); owner.Refresh(); });
            skip.onClick.AddListener(() => { owner.CurrentRun.SkipPet(); owner.Refresh(); });
            cancelRestart.onClick.AddListener(() => { restart.SetActive(false); owner.Refresh(); });
            acceptRestart.onClick.AddListener(() => SceneTransitionOverlay.Current.Go("새로운 시작", () => PrototypeSession.Instance.Restart()));
            closeStudent.onClick.AddListener(owner.DeselectStudent);
            for (int i = 0; i < candidateButtons.Length; i++)
            {
                int index = i;
                candidateButtons[i].onClick.AddListener(() => { owner.CurrentRun.ChoosePet(index); owner.Refresh(); });
            }
            audioPanel.VisibilityChanged += owner.Refresh;
        }
        public void ShowRestart() { restart.SetActive(true); owner.Refresh(); }
        public void Render(PrototypeRun run)
        {
            candidates.SetActive(run.PendingPet);
            if (run.PendingPet) offers.SetActive(false);
            candidateStatus.text = run.LastMessage;
            for (int i = 0; i < candidateButtons.Length; i++)
            {
                bool present = i < run.PendingCandidates.Count;
                candidateButtons[i].gameObject.SetActive(present);
                if (!present) continue;
                var d = run.PendingCandidates[i];
                candidateLabels[i].text = $"{d.Name}\n{d.Grade}학년 - {d.RoleLabel}\n\n{d.Club} - {d.Personality}\n\n{d.SkillName}\n{d.SkillDescription}\n\n영입하기";
                candidateButtons[i].interactable = run.PendingPet;
            }
        }
        void OnDestroy() { if (owner != null) audioPanel.VisibilityChanged -= owner.Refresh; }
    }
}
