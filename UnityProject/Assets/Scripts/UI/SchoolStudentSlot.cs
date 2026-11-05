using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HongikIngan
{
    /// <summary>A fixed desk hit area and a separate, cursor-following student visual.</summary>
    public sealed class SchoolStudentSlot : MonoBehaviour, IPointerClickHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        public RectTransform studentVisual;
        public CanvasGroup visualGroup;
        public TMP_Text nameLabel, roleLabel, emptyLabel;
        public Image selectionFrame;
        public Image studentPortrait;
        public StudentPortraitCatalog portraitCatalog;
        public GameObject[] books;
        public TMP_Text[] bookLabels;
        public int Index { get; private set; }
        public bool IsDragging { get; private set; }
        SchoolScreen owner;
        Transform home;
        Vector3 homePosition, homeScale, pointerOffset;
        int homeSibling;
        bool occupied, availableDesk;

        public void Bind(SchoolScreen screen, int index, StudentDefinition student, bool available, bool selected)
        {
            owner = screen; Index = index; occupied = student != null; availableDesk = available;
            studentVisual.gameObject.SetActive(occupied);
            emptyLabel.gameObject.SetActive(!occupied);
            emptyLabel.text = available ? "빈 책상" : "확장 가능한 책상";
            // The old solid selection panel obscures the transparent character art.
            if (selectionFrame != null) selectionFrame.enabled = false;
            // Older scene instances may not yet have the Image reference serialized.
            if (studentPortrait == null)
                studentPortrait = studentVisual.Find("StudentPlaceholder")?.GetComponent<Image>();
            if (studentPortrait != null)
            {
                var sprite = student == null || portraitCatalog == null ? null : portraitCatalog.Find(student.Id);
                studentPortrait.sprite = sprite;
                studentPortrait.color = Color.white;
                studentPortrait.enabled = sprite != null;
                studentPortrait.preserveAspect = true;
            }
            if (occupied)
            {
                nameLabel.text = student.Name;
                roleLabel.text = $"{student.Grade}학년 - {student.RoleLabel}";
            }
            for (int i = 0; i < books.Length; i++)
            {
                books[i].SetActive(occupied && i < student.SynergyCount);
                if (occupied) bookLabels[i].text = i == 0 ? student.Club : i == 1 ? student.Personality : student.SpecialSynergy;
            }
        }
        public void OnPointerClick(PointerEventData e)
        {
            if (e.button == PointerEventData.InputButton.Left && !e.dragging && occupied && owner.CanInteract)
                owner.SelectStudent(Index);
        }
        public void OnBeginDrag(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left || !occupied || !owner.CanInteract) return;
            if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(owner.dragLayer, e.position, e.pressEventCamera, out var pointer)) return;
            home = studentVisual.parent; homePosition = studentVisual.localPosition;
            homeScale = studentVisual.localScale; homeSibling = studentVisual.GetSiblingIndex();
            pointerOffset = studentVisual.position - pointer;
            IsDragging = true;
            studentVisual.SetParent(owner.dragLayer, true);
            visualGroup.blocksRaycasts = false;
            owner.SelectStudent(Index);
        }
        public void OnDrag(PointerEventData e)
        {
            if (!IsDragging) return;
            if (!owner.CanInteract) { CancelDrag(); return; }
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(owner.dragLayer, e.position, e.pressEventCamera, out var pointer))
                studentVisual.position = pointer + pointerOffset;
        }
        public void OnDrop(PointerEventData e)
        {
            var source = e.pointerDrag == null ? null : e.pointerDrag.GetComponent<SchoolStudentSlot>();
            if (source == null || !source.IsDragging || !availableDesk || !owner.CanInteract) return;
            source.CancelDrag();
            owner.MoveStudent(source.Index, Index);
        }
        public void OnEndDrag(PointerEventData e) { CancelDrag(); }
        public void CancelDrag()
        {
            if (!IsDragging) return;
            IsDragging = false;
            studentVisual.SetParent(home, false);
            studentVisual.localPosition = homePosition;
            studentVisual.localScale = homeScale;
            studentVisual.SetSiblingIndex(homeSibling);
            visualGroup.blocksRaycasts = true;
        }
        void OnApplicationFocus(bool focused) { if (!focused) CancelDrag(); }
        void OnDisable() { CancelDrag(); }
    }
}
