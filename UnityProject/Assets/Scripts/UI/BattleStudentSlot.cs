using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HongikIngan
{
    /// <summary>The hit area stays in place while its student visual follows the pointer.</summary>
    public sealed class BattleStudentSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public RectTransform studentVisual;
        public CanvasGroup visualGroup;
        public TMP_Text nameLabel, roleLabel, synergyLabel, skillLabel, emptyLabel;
        public Image portrait;
        public TMP_Text orderLabel, buffsLabel, calculationLabel;
        public StudentState Student { get; private set; }
        public BattleFormationView Owner { get; private set; }
        public int Index { get; private set; }
        public bool HasStudent { get; private set; }
        public bool IsDragging { get; private set; }
        Transform originalParent;
        Vector3 originalPosition, originalScale;
        int sibling;
        Vector2 pointerOffset;
        public void Bind(BattleFormationView owner, int index) { Owner = owner; Index = index; }
        public void Render(StudentState student, Sprite artwork = null)
        {
            HasStudent = student != null;
            Student = student;
            studentVisual.gameObject.SetActive(HasStudent);
            emptyLabel.gameObject.SetActive(!HasStudent);
            if (!HasStudent) return;
            portrait.sprite = artwork;
            var d = student.Definition;
            nameLabel.text = student.IsClone ? d.Name + " (분신)" : d.Name;
            roleLabel.text = $"{d.Grade}학년 - {d.RoleLabel}";
            synergyLabel.text = $"{d.Club} - {d.Personality}" + (string.IsNullOrEmpty(d.SpecialSynergy) ? "" : " - " + d.SpecialSynergy);
            skillLabel.text = string.IsNullOrEmpty(d.SkillName) ? $"스킬 {student.SkillValue:0.##}" : d.SkillName;
            if (orderLabel != null) orderLabel.text = (Index + 1).ToString();
            if (buffsLabel != null) buffsLabel.text = BattleHud.KeywordText(student.Keywords);
            int hash = 0;
            foreach (char c in d.Id) hash = (hash * 31 + c) & 0x7fffffff;
            portrait.color = portrait.sprite != null ? Color.white : Color.HSVToRGB((hash % 100) / 100f, .28f, .96f);
        }
        public void ShowCalculation(string value)
        {
            if (calculationLabel == null) return;
            calculationLabel.text = value;
            calculationLabel.gameObject.SetActive(!string.IsNullOrEmpty(value));
        }
        public void OnPointerEnter(PointerEventData e) { Owner?.ShowTooltip(this); }
        public void OnPointerExit(PointerEventData e) { Owner?.HideTooltip(); }
        public void OnBeginDrag(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left || Owner == null || !Owner.CanReorder || !HasStudent) return;
            Owner.HideTooltip();
            originalParent = studentVisual.parent;
            originalPosition = studentVisual.localPosition;
            originalScale = studentVisual.localScale;
            sibling = studentVisual.GetSiblingIndex();
            studentVisual.SetParent(Owner.dragLayer, true);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Owner.dragLayer, e.position, e.pressEventCamera, out var pointer);
            pointerOffset = (Vector2)studentVisual.localPosition - pointer;
            visualGroup.blocksRaycasts = false;
            IsDragging = true;
        }
        public void OnDrag(PointerEventData e)
        {
            if (!IsDragging) return;
            if (!Owner.CanReorder) { CancelDrag(); return; }
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(Owner.dragLayer, e.position, e.pressEventCamera, out var point))
                studentVisual.localPosition = point + pointerOffset;
        }
        public void OnDrop(PointerEventData e)
        {
            var source = e.pointerDrag == null ? null : e.pointerDrag.GetComponent<BattleStudentSlot>();
            if (source == null || !source.IsDragging) return;
            source.CancelDrag();
            Owner.Drop(source, this);
        }
        public void OnEndDrag(PointerEventData e) => CancelDrag();
        void OnDisable() { CancelDrag(); Owner?.HideTooltip(); }
        public void CancelDrag()
        {
            if (!IsDragging) return;
            IsDragging = false;
            studentVisual.SetParent(originalParent, false);
            studentVisual.SetSiblingIndex(sibling);
            studentVisual.localPosition = originalPosition;
            // Reparenting to the drag layer adjusts local scale to preserve world size.
            studentVisual.localScale = originalScale;
            visualGroup.blocksRaycasts = true;
        }
    }
}
