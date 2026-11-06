using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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
        public Button previousRosterPage, nextRosterPage;
        public TMP_Text rosterPageLabel;
        public GameObject battleSweepPrefab;
        public int RosterPage { get; private set; }
        [Min(.1f)] public float transformationDuration = 1.2f;
        [Tooltip("Fraction of the transformation held fully white, with the outfit swapped halfway through.")]
        [Range(.05f, .8f)] public float transformationWhiteHold = .25f;
        [Min(0)] public float transformedOutfitHoldDuration = .25f;
        public PrototypeRun CurrentRun => observedRun;
        int selected = -1;
        bool transitionWasBusy;
        PrototypeRun observedRun;
        readonly List<RaycastResult> pressHits = new List<RaycastResult>();
        public bool CanInteract => observedRun != null && !observedRun.PendingPet && !dialogs.IsOpen && !(SceneTransitionOverlay.Current != null && SceneTransitionOverlay.Current.IsBusy);

        void Awake() => EnsureDragLayer();

        // This scene-level container must stay outside the desk prefab hierarchy.
        public void EnsureDragLayer()
        {
            if (dragLayer != null || schoolCanvas == null) return;
            var layout = schoolCanvas.transform.Find("Layout") ?? schoolCanvas.transform;
            dragLayer = layout.Find("DragLayer") as RectTransform;
            if (dragLayer != null) return;
            dragLayer = new GameObject("DragLayer", typeof(RectTransform)).GetComponent<RectTransform>();
            dragLayer.gameObject.layer = layout.gameObject.layer;
            dragLayer.SetParent(layout, false);
            dragLayer.anchorMin = Vector2.zero;
            dragLayer.anchorMax = Vector2.one;
            dragLayer.offsetMin = dragLayer.offsetMax = Vector2.zero;
            dragLayer.SetAsLastSibling();
        }

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
            battleButton.onClick.AddListener(() => { if (CanInteract && observedRun.CanStartBattle) SceneTransitionOverlay.Current.Go("전투 시작", () => PrototypeSession.Instance.EnterBattle(), TransformStudents(), battleSweepPrefab); });
            learnButton.onClick.AddListener(() => observedRun.Upgrade(selected));
            sellButton.onClick.AddListener(() => { int index = selected; selected = -1; observedRun.Sell(index); });
            restartButton.onClick.AddListener(dialogs.ShowRestart);
            dialogs.Bind(this);
            if (previousRosterPage != null) previousRosterPage.onClick.AddListener(() => ChangeRosterPage(-1));
            if (nextRosterPage != null) nextRosterPage.onClick.AddListener(() => ChangeRosterPage(1));
            var sound = HongikIngan.Audio.SoundManager.EnsureExists();
            if (sound.BackendAvailable) sound.PlayBgm("event:/BGM/School");
            Refresh();
        }
        IEnumerator TransformStudents()
        {
            foreach (var slot in slots) if (slot != null) slot.CancelDrag();
            float duration = Mathf.Max(.1f, transformationDuration);
            for (float time = 0; time < duration; time += Time.unscaledDeltaTime)
            {
                foreach (var slot in slots) if (slot != null) slot.SetTransformation(time / duration, transformationWhiteHold);
                yield return null;
            }
            foreach (var slot in slots) if (slot != null) slot.SetTransformation(1, transformationWhiteHold);
            yield return new WaitForSecondsRealtime(transformedOutfitHoldDuration);
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
            energyLabel.text = observedRun.Money.ToString();
            rosterLabel.text = $"{observedRun.Students.Count} / {observedRun.Capacity}";
            stageLabel.text = $"STAGE {observedRun.Stage:00}";
            photosLabel.text = $"폴라로이드 {observedRun.PhotoCount}\n분위기 {observedRun.Atmosphere} / 별의 힘 {observedRun.StarPower}";
            ownedRelicsLabel.text = observedRun.Relics.Count == 0 ? "아직 획득한 유물이 없어요" : string.Join("   /   ", observedRun.Relics.Select(r => r.Name));
            int pages = Mathf.Max(1, Mathf.CeilToInt(observedRun.SchoolDeskCount / 9f));
            RosterPage = Mathf.Clamp(RosterPage, 0, pages - 1);
            if (rosterPageLabel != null) rosterPageLabel.text = $"학생 {RosterPage + 1} / {pages}";
            if (previousRosterPage != null) previousRosterPage.interactable = free && RosterPage > 0;
            if (nextRosterPage != null) nextRosterPage.interactable = free && RosterPage + 1 < pages;
            int visible = Mathf.Max(6, Mathf.Max(observedRun.Capacity, observedRun.SchoolDeskCount));
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                int desk = RosterPage * 9 + i;
                slot.gameObject.SetActive(desk < visible);
                if (desk >= visible) continue;
                var seated = observedRun.StudentAtSchoolDesk(desk);
                slot.Bind(this, desk, seated?.Definition, desk < Mathf.Max(observedRun.Capacity, observedRun.SchoolDeskCount),
                    seated != null && observedRun.Students.IndexOf(seated) == selected);
            }
            for (int i = 0; i < relicButtons.Length; i++)
            {
                var relic = i < observedRun.RelicOffers.Count ? observedRun.RelicOffers[i] : null;
                relicLabels[i].text = relic == null ? (observedRun.RelicPurchasedThisBattle ? "구매 완료 - 다음 전투 후 구매 가능" : "남은 유물 없음") :
                    $"{relic.Name}\n{relic.Description}\n{observedRun.Settings.relicPrice} 에너지";
                relicButtons[i].interactable = relic != null && !observedRun.RelicPurchasedThisBattle;
            }
            for (int i = 0; i < petButtons.Length; i++)
            {
                var pet = i < observedRun.PetOffers.Count ? observedRun.PetOffers[i] : null;
                petLabels[i].text = pet.HasValue ? PetLabel((int)pet.Value) : "펫 없음";
                petButtons[i].interactable = pet.HasValue;
            }
            synergyLabel.text = SynergyText.List(observedRun, false);
            SetButtonText(expandButton, $"책상 확장  +  {observedRun.Settings.slotExpansionPrice}");
            expandButton.interactable = observedRun.OpenCapacity < 7;
            SetButtonText(rerollButton, observedRun.RelicPurchasedThisBattle ? "유물 구매 완료" : $"유물 리롤\n{observedRun.RerollCost} 에너지");
            rerollButton.interactable = observedRun.CanRerollRelic;
            battleButton.interactable = observedRun.CanStartBattle;
            statusLabel.text = observedRun.Students.Count > observedRun.Capacity ? observedRun.StartBattleBlockReason : observedRun.LastMessage;
            bool owned = selected >= 0 && selected < observedRun.Students.Count;
            studentCard.SetActive(owned);
            learnButton.gameObject.SetActive(owned);
            sellButton.gameObject.SetActive(owned);
            if (owned)
            {
                var s = observedRun.Students[selected];
                studentCard.GetComponent<StudentCardView>().Render(s);
                SetButtonText(learnButton, $"보충 학습\n{observedRun.Settings.UpgradePrice(s.Definition.Grade)} 에너지");
                SetButtonText(sellButton, $"계약 해제\n+{BattleSynergies.Income(observedRun, observedRun.Settings.SalePrice(s.Definition.Grade))}");
            }
            else
            {
                detailTitle.text = "우리들의 교실";
                detailBody.text = "학생을 눌러 자세히 살펴보세요.\n빈 책상에 놓으면 그 자리로 이동합니다.\n다른 학생 위에 놓으면 자리를 바꿉니다.";
            }
        }
        void ChangeRosterPage(int direction)
        {
            if (!CanInteract) return;
            foreach (var slot in slots) slot.CancelDrag();
            RosterPage += direction;
            DeselectStudent();
        }
        static void SetButtonText(Button target, string value) => target.GetComponentInChildren<TMP_Text>().text = value;
        public void DeselectStudent() { selected = -1; Refresh(); }
        void Update()
        {
            bool busy = SceneTransitionOverlay.Current != null && SceneTransitionOverlay.Current.IsBusy;
            if (busy != transitionWasBusy) { transitionWasBusy = busy; Refresh(); }
            var pointer = Pointer.current;
            if (pointer != null && pointer.press.wasPressedThisFrame && IsOutsideStudentCard(pointer.position.ReadValue()))
                DeselectStudent();
        }
        // Presses on the card or on a student are left to their own click handlers;
        // a student click switches the card to that student instead of closing it.
        public bool IsOutsideStudentCard(Vector2 screenPosition)
        {
            if (!studentCard.activeSelf || !CanInteract || EventSystem.current == null) return false;
            if (RectTransformUtility.RectangleContainsScreenPoint((RectTransform)studentCard.transform, screenPosition, null)) return false;
            pressHits.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = screenPosition }, pressHits);
            var slot = pressHits.Count == 0 ? null : pressHits[0].gameObject.GetComponentInParent<SchoolStudentSlot>();
            return slot == null || !slot.IsOccupied;
        }
        string PetLabel(int kind)
        {
            string[] labels = { "일반 펫", "고급 펫", "최고급 펫", "시너지 펫" };
            return labels[Mathf.Clamp(kind, 0, 3)] + $"\n{Run.Settings.PetPrice((PetKind)kind)} 에너지\n후보 {(kind == 1 || kind == 2 ? 5 : 3)}명";
        }
    }
}
