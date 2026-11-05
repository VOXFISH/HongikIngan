using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HongikIngan
{
    /// <summary>The hit area stays in place while its student visual follows the pointer.</summary>
    public sealed class BattleStudentSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        public RectTransform studentVisual;
        public CanvasGroup visualGroup;
        public Text nameLabel, roleLabel, synergyLabel, skillLabel, emptyLabel;
        public Image portrait;
        public BattleFormationView Owner { get; private set; }
        public int Index { get; private set; }
        public bool HasStudent { get; private set; }
        public bool IsDragging { get; private set; }
        Transform originalParent;
        Vector3 originalPosition;
        int sibling;
        Vector2 pointerOffset;
        public void Bind(BattleFormationView owner, int index) { Owner = owner; Index = index; }
        public void Render(StudentState student)
        {
            HasStudent = student != null;
            studentVisual.gameObject.SetActive(HasStudent);
            emptyLabel.gameObject.SetActive(!HasStudent);
            if (!HasStudent) return;
            var d = student.Definition;
            nameLabel.text = d.Name;
            roleLabel.text = $"{d.Grade}학년 · {d.Role}";
            synergyLabel.text = $"{d.Club} · {d.Personality}";
            skillLabel.text = $"스킬 {student.SkillValue:0.##}";
            int hash = 0;
            foreach (char c in d.Id) hash = (hash * 31 + c) & 0x7fffffff;
            portrait.color = Color.HSVToRGB((hash % 100) / 100f, .28f, .96f);
        }
        public void OnBeginDrag(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left || Owner == null || !Owner.CanReorder || !HasStudent) return;
            originalParent = studentVisual.parent;
            originalPosition = studentVisual.localPosition;
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
        void OnDisable() => CancelDrag();
        public void CancelDrag()
        {
            if (!IsDragging) return;
            IsDragging = false;
            studentVisual.SetParent(originalParent, false);
            studentVisual.SetSiblingIndex(sibling);
            studentVisual.localPosition = originalPosition;
            visualGroup.blocksRaycasts = true;
        }
    }
}
