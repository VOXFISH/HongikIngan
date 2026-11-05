using TMPro;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HongikIngan
{
    public sealed class SchoolScreen : PrototypeScreen
    {
        public CanvasGroup schoolCanvas;
        public RectTransform dragLayer;
        public SchoolStudentSlot[] slots;
        public Button[] relicButtons, petButtons;
        public TMP_Text[] relicLabels, petLabels;
        public TMP_Text energyLabel, rosterLabel, stageLabel, photosLabel, synergyLabel, ownedRelicsLabel;
        public TMP_Text detailTitle, detailBody, statusLabel;
        public Button expandButton, rerollButton, battleButton, learnButton, sellButton;
        public Button restartButton, audioButton;
        public SchoolDialogs dialogs;
        public GameObject studentCard;
        public PrototypeRun CurrentRun => observedRun;
        int selected = -1;
        bool transitionWasBusy;
        PrototypeRun observedRun;
        public bool CanInteract => observedRun != null && !observedRun.PendingPet && !dialogs.IsOpen && !(SceneTransitionOverlay.Current != null && SceneTransitionOverlay.Current.IsBusy);

        void Start()
        {
            observedRun = PrototypeSession.Ensure().Run;
            if (schoolCanvas == null)
            {
                Debug.LogError("School canvas is missing. Use HongikIngan/Apply School reference layout in the Editor.");
                enabled = false;
                return;
            }
            observedRun.Changed += Refresh;
            for (int i = 0; i < relicButtons.Length; i++)
            {
                int index = i;
                relicButtons[i].onClick.AddListener(() => observedRun.BuyRelic(index));
            }
            for (int i = 0; i < petButtons.Length; i++)
            {
                int index = i;
                petButtons[i].onClick.AddListener(() => { observedRun.OpenPet(index); Refresh(); });
            }
            expandButton.onClick.AddListener(() => observedRun.ExpandSlots());
            rerollButton.onClick.AddListener(() => observedRun.Reroll());
            battleButton.onClick.AddListener(() => { if (CanInteract && observedRun.CanStartBattle) SceneTransitionOverlay.Current.Go("출동!", () => PrototypeSession.Instance.EnterBattle()); });
            learnButton.onClick.AddListener(() => observedRun.Upgrade(selected));
            sellButton.onClick.AddListener(() => { int index = selected; selected = -1; observedRun.Sell(index); });
            restartButton.onClick.AddListener(dialogs.ShowRestart);
            dialogs.Bind(this);
            var sound = HongikIngan.Audio.SoundManager.EnsureExists();
            if (sound.BackendAvailable) sound.PlayBgm("event:/BGM/School");
            Refresh();
        }
        void OnDestroy()
        {
            if (observedRun != null) observedRun.Changed -= Refresh;
        }
        public void SelectStudent(int desk)
        {
            selected = observedRun.Students.IndexOf(observedRun.StudentAtSchoolDesk(desk));
            Refresh();
        }
        public void MoveStudent(int from, int to)
        {
            if (!CanInteract) return;
            var moving = observedRun.StudentAtSchoolDesk(from);
            if (observedRun.MoveSchoolStudent(from, to))
            {
                selected = observedRun.Students.IndexOf(moving);
            }
            Refresh();
        }
        public void Refresh()
        {
            if (schoolCanvas == null || observedRun == null) return;
            dialogs.Render(observedRun);
            bool free = CanInteract;
            schoolCanvas.interactable = free;
            if (!free) foreach (var slot in slots) slot.CancelDrag();
            energyLabel.text = $"미라클 에너지\n{observedRun.Money}";
            rosterLabel.text = $"전대 인원\n{observedRun.Students.Count} / {observedRun.Capacity}";
            stageLabel.text = $"STAGE {observedRun.Stage:00}  ·  방과 전";
            photosLabel.text = $"폴라로이드 {observedRun.PhotoCount}\n분위기 {observedRun.Atmosphere}";
            ownedRelicsLabel.text = observedRun.Relics.Count == 0 ? "아직 획득한 유물이 없어요" : string.Join("   /   ", observedRun.Relics.Select(r => r.Name));
            int visible = Mathf.Max(6, observedRun.Capacity);
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                slot.gameObject.SetActive(i < visible);
                if (i >= visible) continue;
                var seated = observedRun.StudentAtSchoolDesk(i);
                slot.Bind(this, i, seated?.Definition, i < observedRun.Capacity,
                    seated != null && observedRun.Students.IndexOf(seated) == selected);
            }
            for (int i = 0; i < relicButtons.Length; i++)
            {
                var relic = i < observedRun.RelicOffers.Count ? observedRun.RelicOffers[i] : null;
                relicLabels[i].text = relic == null ? (observedRun.RelicPurchasedThisBattle ? "구매 완료 · 다음 전투 후 구매 가능" : "남은 유물 없음") :
                    $"{relic.Name}\n{relic.Description}\n{observedRun.Settings.relicPrice} 에너지";
                relicButtons[i].interactable = relic != null && !observedRun.RelicPurchasedThisBattle;
            }
            for (int i = 0; i < petButtons.Length; i++)
            {
                var pet = i < observedRun.PetOffers.Count ? observedRun.PetOffers[i] : null;
                petLabels[i].text = pet.HasValue ? PetLabel((int)pet.Value) : "펫 없음";
                petButtons[i].interactable = pet.HasValue;
            }
            var groups = observedRun.Students.SelectMany(s => new[] { s.Definition.Club, s.Definition.Personality, s.Definition.SpecialSynergy })
                .Where(s => !string.IsNullOrEmpty(s)).GroupBy(s => s).OrderByDescending(g => g.Count()).ThenBy(g => g.Key);
            synergyLabel.text = groups.Any() ? string.Join("\n", groups.Select(g => $"{g.Key}   {BattleSynergies.Count(observedRun, g.Key)}명")) : "학생을 영입하면\n시너지가 표시됩니다";
            SetButtonText(expandButton, $"책상 확장  +  {observedRun.Settings.slotExpansionPrice}");
            expandButton.interactable = observedRun.Capacity < 7;
            SetButtonText(rerollButton, observedRun.RelicPurchasedThisBattle ? "유물 구매 완료" : $"유물 리롤\n{observedRun.RerollCost} 에너지");
            rerollButton.interactable = observedRun.CanRerollRelic;
            battleButton.interactable = observedRun.CanStartBattle;
            statusLabel.text = observedRun.LastMessage;
            bool owned = selected >= 0 && selected < observedRun.Students.Count;
            studentCard.SetActive(owned);
            learnButton.gameObject.SetActive(owned);
            sellButton.gameObject.SetActive(owned);
            if (owned)
            {
                var s = observedRun.Students[selected];
                detailTitle.text = $"{s.Definition.Name}   /   {s.Definition.Grade}학년 · {s.Definition.RoleLabel}";
                string values = s.Definition.Action == ActionKind.AddThenPercentDamage ?
                    $"+{s.SkillValue:0.##} 후 +{s.Definition.PercentAtUpgrade(s.UpgradeCount):0.##}%" : $"{s.SkillValue:0.##}";
                switch (s.Definition.Action)
                {
                    case ActionKind.Paparazzi: values = s.UpgradeCount > 0 ? "+500 후 +10%p" : "공격마다 +500"; break;
                    case ActionKind.Spotlight: values = $"+{s.SkillValue:0.##}% · 양옆 조명"; break;
                    case ActionKind.LightingDamage: values = $"+500 후 조명마다 +{s.SkillValue:0.##}"; break;
                    case ActionKind.GrantLove: values = $"사랑 {(int)s.SkillValue}스택 · 스택당 ×1.1"; break;
                    case ActionKind.CloneNext: values = (s.UpgradeCount > 0 ? "전투 종료까지 분신" : "턴 종료까지 분신") + $" · 다음 정밀화 +{3000f * (s.PrecisionCopies + 1):0}"; break;
                    case ActionKind.BestSubject: values = s.UpgradeCount > 0 ? "세 조건 인원수의 곱" : "세 조건 인원수의 합"; break;
                }
                string traits = s.Definition.Club + " · " + s.Definition.Personality + (string.IsNullOrEmpty(s.Definition.SpecialSynergy) ? "" : " · " + s.Definition.SpecialSynergy);
                detailBody.text = $"{traits}\n{s.Definition.SkillDescription}\n현재 효과 {values}  ·  강화 {s.UpgradeCount}회";
                SetButtonText(learnButton, $"보충 학습\n{observedRun.Settings.UpgradePrice(s.Definition.Grade)} 에너지");
                SetButtonText(sellButton, $"계약 해제\n+{BattleSynergies.Income(observedRun, observedRun.Settings.SalePrice(s.Definition.Grade))}");
            }
            else
            {
                detailTitle.text = "우리들의 교실";
                detailBody.text = "학생을 눌러 자세히 살펴보세요.\n빈 책상에 놓으면 그 자리로 이동합니다.\n다른 학생 위에 놓으면 자리를 바꿉니다.";
            }
        }
        static void SetButtonText(Button target, string value) => target.GetComponentInChildren<TMP_Text>().text = value;
        public void DeselectStudent() { selected = -1; Refresh(); }
        void Update()
        {
            bool busy = SceneTransitionOverlay.Current != null && SceneTransitionOverlay.Current.IsBusy;
            if (busy != transitionWasBusy) { transitionWasBusy = busy; Refresh(); }
        }
        string PetLabel(int kind)
        {
            string[] labels = { "일반 펫", "고급 펫", "최고급 펫", "시너지 펫" };
            return labels[Mathf.Clamp(kind, 0, 3)] + $"\n{Run.Settings.PetPrice((PetKind)kind)} 에너지\n후보 {(kind == 1 || kind == 2 ? 5 : 3)}명";
        }
    }
}