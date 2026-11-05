using UnityEngine;
using System.Linq;

namespace HongikIngan
{
    [System.Serializable]
    public sealed class BattleGirlArtwork
    {
        public string girlId;
        public Sprite sprite;
    }
    public sealed class BattleFormationView : MonoBehaviour
    {
        [Tooltip("Authored arc positions, ordered left to right. Students occupy the centered contiguous range.")]
        public BattleStudentSlot[] slots;
        public RectTransform dragLayer;
        public BattleStudentTooltip tooltip;
        [Tooltip("Optional per-Girl sprite assignments; empty entries use the placeholder.")]
        public BattleGirlArtwork[] artwork = new BattleGirlArtwork[0];
        BattleScreen screen;
        public bool CanReorder => screen != null && screen.CanReorder;
        public void Bind(BattleScreen owner)
        {
            screen = owner;
            for (int i = 0; i < slots.Length; i++) slots[i].Bind(this, i);
        }
        public void Render(PrototypeRun run)
        {
            int first = FirstOccupiedSlot(run.Students.Count, slots.Length);
            for (int i = 0; i < slots.Length; i++)
            {
                int index = i - first;
                bool occupied = index >= 0 && index < run.Students.Count;
                slots[i].Bind(this, occupied ? index : -1);
                slots[i].gameObject.SetActive(occupied);
                var student = occupied ? run.Students[index] : null;
                var art = student == null ? null : artwork.FirstOrDefault(a => a != null && a.girlId == student.Definition.Id);
                slots[i].Render(student, art?.sprite);
            }
            if (!CanReorder) HideTooltip();
        }
        public static int FirstOccupiedSlot(int count, int slotCount = 9) => Mathf.Max(0, (slotCount - count) / 2);
        public BattleStudentSlot StudentSlot(int index) => slots.FirstOrDefault(s => s.HasStudent && s.Index == index);
        public void ShowTooltip(BattleStudentSlot slot)
        {
            if (CanReorder && slot.HasStudent && !slot.IsDragging && tooltip != null) tooltip.Show(slot);
        }
        public void HideTooltip() { if (tooltip != null) tooltip.Hide(); }
        public void ShowCalculation(StudentState actor, string text)
        {
            foreach (var slot in slots) slot.ShowCalculation(slot.Student == actor && actor != null ? text : "");
        }
        public void Drop(BattleStudentSlot source, BattleStudentSlot target)
        {
            if (source.Owner != this || target.Owner != this) return;
            if (CanReorder && source.HasStudent && target.HasStudent) screen.Swap(source.Index, target.Index);
        }
        public void CancelDrag() { HideTooltip(); foreach (var slot in slots) slot.CancelDrag(); }
    }
}
