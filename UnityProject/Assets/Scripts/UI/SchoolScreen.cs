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
        public Text[] relicLabels, petLabels;
        public Text energyLabel, rosterLabel, stageLabel, photosLabel, synergyLabel, ownedRelicsLabel;
        public Text detailTitle, detailBody, statusLabel;
        public Button expandButton, rerollButton, battleButton, learnButton, sellButton;
        public Button restartButton, audioButton;
        int selected = 0;
        bool replacement, confirmRestart;
        PrototypeRun observedRun;
        Font schoolFont;
        public bool CanInteract => observedRun != null && !observedRun.PendingPet && !audioOpen && !confirmRestart;

        void Start()
        {
            observedRun = PrototypeSession.Ensure().Run;
            if (schoolCanvas == null)
            {
                Debug.LogError("School canvas is missing. Use HongikIngan/Apply School reference layout in the Editor.");
                enabled = false;
                return;
            }
            // OS font is used only for text; all portraits remain blank editor-authored Images.
            schoolFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 20);
            foreach (var text in schoolCanvas.GetComponentsInChildren<Text>(true)) text.font = schoolFont;
            observedRun.Changed += Refresh;
            for (int i = 0; i < relicButtons.Length; i++)
            {
                int index = i;
                relicButtons[i].onClick.AddListener(() => observedRun.BuyRelic(index));
            }
            for (int i = 0; i < petButtons.Length; i++)
            {
                int index = i;
                petButtons[i].onClick.AddListener(() => { observedRun.OpenPet(index); replacement = false; Refresh(); });
            }
            expandButton.onClick.AddListener(() => observedRun.ExpandSlots());
            rerollButton.onClick.AddListener(() => observedRun.Reroll());
            battleButton.onClick.AddListener(() => PrototypeSession.Instance.EnterBattle());
            learnButton.onClick.AddListener(() => observedRun.Upgrade(selected));
            sellButton.onClick.AddListener(() => { int index = selected; selected = -1; observedRun.Sell(index); });
            restartButton.onClick.AddListener(() => { confirmRestart = true; Refresh(); });
            audioButton.onClick.AddListener(() => { audioOpen = true; Refresh(); });
            var sound = HongikIngan.Audio.SoundManager.EnsureExists();
            if (sound.BackendAvailable) sound.PlayBgm("event:/BGM/School");
            Refresh();
        }
        void OnDestroy()
        {
            if (observedRun != null) observedRun.Changed -= Refresh;
            if (schoolFont != null) Destroy(schoolFont);
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
            bool free = CanInteract;
            schoolCanvas.interactable = free;
            if (!free) foreach (var slot in slots) slot.CancelDrag();
            energyLabel.text = $"미라클 에너지\n{observedRun.Money}";
            rosterLabel.text = $"전대 인원\n{observedRun.Students.Count} / {observedRun.Capacity}";
            stageLabel.text = $"STAGE {observedRun.Stage:00}  ·  방과 전";
            photosLabel.text = $"폴라로이드  {observedRun.PhotoCount}";
            ownedRelicsLabel.text = observedRun.Relics.Count == 0 ? "아직 획득한 유물이 없어요" : string.Join("   /   ", observedRun.Relics.Select(r => r.Name));
            int visible = Mathf.Max(5, observedRun.Capacity);
            float cellWidth = 1080f / visible;
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                slot.gameObject.SetActive(i < visible);
                if (i >= visible) continue;
                var rect = (RectTransform)slot.transform;
                rect.anchoredPosition = new Vector2(i * cellWidth + (cellWidth - 200f * Mathf.Min(1, cellWidth / 210f)) * .5f, 0);
                rect.localScale = Vector3.one * Mathf.Min(1, cellWidth / 210f);
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
            synergyLabel.text = groups.Any() ? string.Join("\n", groups.Select(g => $"{g.Key}   {g.Count()}명")) : "학생을 영입하면\n시너지가 표시됩니다";
            SetButtonText(expandButton, $"책상 확장  +  {observedRun.Settings.slotExpansionPrice}");
            SetButtonText(rerollButton, observedRun.RelicPurchasedThisBattle ? "유물 구매 완료" : $"유물 리롤\n{observedRun.RerollCost} 에너지");
            rerollButton.interactable = observedRun.CanRerollRelic;
            battleButton.interactable = observedRun.Students.Count > 0;
            statusLabel.text = observedRun.LastMessage;
            bool owned = selected >= 0 && selected < observedRun.Students.Count;
            learnButton.gameObject.SetActive(owned);
            sellButton.gameObject.SetActive(owned);
            if (owned)
            {
                var s = observedRun.Students[selected];
                detailTitle.text = $"{s.Definition.Name}   /   {s.Definition.Grade}학년 · {s.Definition.Role}";
                detailBody.text = $"{s.Definition.Club} · {s.Definition.Personality}\n{s.Definition.SkillDescription}\n현재 값 {s.SkillValue:0.##}  ·  강화 {s.UpgradeCount}회";
                SetButtonText(learnButton, $"보충 학습\n{observedRun.Settings.UpgradePrice(s.Definition.Grade)} 에너지");
                SetButtonText(sellButton, $"계약 해제\n+{observedRun.Settings.SalePrice(s.Definition.Grade)}");
            }
            else
            {
                detailTitle.text = "우리들의 교실";
                detailBody.text = "학생을 눌러 자세히 살펴보세요.\n빈 책상에 놓으면 그 자리로 이동합니다.\n다른 학생 위에 놓으면 자리를 바꿉니다.";
            }
        }
        static void SetButtonText(Button target, string value) => target.GetComponentInChildren<Text>().text = value;
        protected override void Face(float x, float y, float size, StudentDefinition student)
        {
            Panel(new Rect(x, y, size, size), new Color(.92f, .89f, .95f));
        }
        void OnGUI()
        {
            if (schoolCanvas == null) return;
            if (!Run.PendingPet && !confirmRestart && !audioOpen) return;
            InitializeUI();
            Panel(new Rect(0, 0, 1440, 900), new Color(.1f, .08f, .2f, .65f));
            if (Run.PendingPet) PetSelection();
            if (confirmRestart) RestartConfirmation();
            AudioUI();
            if (schoolCanvas.interactable != CanInteract) Refresh();
        }
        string PetLabel(int kind)
        {
            string[] labels={"일반 펫","고급 펫","최고급 펫","시너지 펫"};
            return labels[Mathf.Clamp(kind,0,3)]+$" · {Run.Settings.PetPrice((PetKind)kind)} / 후보 {(kind==1||kind==2?5:3)}명";
        }
        void PetSelection()
        {
            Panel(new Rect(70,180,1300,610),new Color(.86f,.84f,.97f));
            Label(100,205,1150,45,"새로운 인연 · 1명 선택 / 남은 후보는 폴라로이드",heading);
            for(int i=0;i<Run.PendingCandidates.Count;i++)
            {
                var d=Run.PendingCandidates[i]; var r=new Rect(105+i*245,275,230,240);
                StudentCard(r,d,d.SkillDescription);
                if(Button(r.x,r.y+245,230,44,"이 학생 영입",Run.Students.Count<Run.Capacity&&!audioOpen)) { Run.ChoosePet(i); replacement=false; break; }
            }
            if(Button(105,605,260,45,"교체 · 기존 학생 판매",!audioOpen)) replacement=!replacement;
            if(Button(1040,700,270,45,"영입하지 않음 (환불 없음)",!audioOpen)) { Run.SkipPet(); replacement=false; }
            Label(105,708,900,45,Run.LastMessage,small);
            if(replacement)
            {
                Panel(new Rect(385,580,920,115),new Color(.95f,.93f,.99f));
                ReplacementStudents(395,590);
            }
        }
        bool ReplacementStudents(float x,float y)
        {
            for(int i=0;i<Run.Students.Count;i++)
            {
                var d=Run.Students[i].Definition; float left=x+i*100;
                // Portrait and name share one click target; no combat details in this panel.
                if(Button(left,y,95,92,"",!audioOpen))
                {
                    bool sold=Run.Sell(i);
                    if(sold) selected=-1;
                    return sold;
                }
                Face(left+30,y+5,35,d);
                Label(left+4,y+43,87,45,d.Name,small);
            }
            return false;
        }
        void RestartConfirmation()
        {
            Panel(new Rect(0,100,1440,800),new Color(.12f,.16f,.24f,.6f));
            Panel(new Rect(420,310,600,260),new Color(.95f,.93f,.99f));
            Label(450,335,540,45,"새 게임을 시작할까요?",heading);
            Label(450,390,540,65,"현재 학생, 재화, 유물, 폴라로이드와 진행 상황이 초기화됩니다.",normal);
            if(Button(450,490,245,45,"취소")) confirmRestart=false;
            if(Button(720,490,270,45,"초기화하고 새 게임"))
            {
                PrototypeSession.Instance.Restart();
                GUIUtility.ExitGUI();
            }
        }
    }
}
