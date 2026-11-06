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
        public Material transformationMaterial;
        Material portraitMaterial;
        string studentId;
        float transformationProgress = -1;
        float whiteHoldFraction = .25f;
        public GameObject[] books;
        public TMP_Text[] bookLabels;
        public int Index { get; private set; }
        public bool IsDragging { get; private set; }
        public bool IsOccupied => occupied;
        SchoolScreen owner;
        Transform home;
        Vector3 homePosition, homeScale, pointerOffset;
        int homeSibling;
        bool occupied, availableDesk;

        public void Bind(SchoolScreen screen, int index, StudentDefinition student, bool available, bool selected)
        {
            owner = screen; Index = index; occupied = student != null; availableDesk = available;
            if (studentId != student?.Id || SceneTransitionOverlay.Current == null || !SceneTransitionOverlay.Current.IsBusy)
                transformationProgress = -1;
            studentId = student?.Id;
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
                if (portraitMaterial != null) portraitMaterial.SetFloat("_Flash", 0);
                // The input-lock refresh must not reset a transformation already in progress.
                if (transformationProgress >= 0) SetTransformation(transformationProgress, whiteHoldFraction);
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
        public void SetTransformation(float progress, float whiteHold = .25f)
        {
            if (!occupied || studentPortrait == null || portraitCatalog == null) return;
            transformationProgress = progress = Mathf.Clamp01(progress);
            whiteHoldFraction = Mathf.Clamp(whiteHold, .05f, .8f);
            if (portraitMaterial == null && transformationMaterial != null)
            {
                portraitMaterial = new Material(transformationMaterial);
                studentPortrait.material = portraitMaterial;
            }
            var sprite = progress >= .5f ? portraitCatalog.Find(studentId, true) : null;
            studentPortrait.sprite = sprite != null ? sprite : portraitCatalog.Find(studentId);
            studentPortrait.enabled = studentPortrait.sprite != null;
            if (portraitMaterial != null)
            {
                float fadeFraction = (1 - whiteHoldFraction) * .5f;
                float flash = progress < .5f
                    ? Mathf.SmoothStep(0, 1, Mathf.Clamp01(progress / fadeFraction))
                    : Mathf.SmoothStep(0, 1, Mathf.Clamp01((1 - progress) / fadeFraction));
                portraitMaterial.SetFloat("_Flash", flash);
                // uGUI may render a stencil copy when the desk is inside a Mask.
                studentPortrait.materialForRendering.SetFloat("_Flash", flash);
            }
        }
        void OnDestroy() { if (portraitMaterial != null) Destroy(portraitMaterial); }
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
