using UnityEngine;

namespace HongikIngan
{
    public sealed class BattleFormationView : MonoBehaviour
    {
        [Tooltip("Array order is attack order. Moving a slot in the scene changes its visual position only.")]
        public BattleStudentSlot[] slots;
        public RectTransform dragLayer;
        BattleScreen screen;
        public bool CanReorder => screen != null && screen.CanReorder;
        public void Bind(BattleScreen owner)
        {
            screen = owner;
            for (int i = 0; i < slots.Length; i++) slots[i].Bind(this, i);
        }
        public void Render(PrototypeRun run)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].gameObject.SetActive(i < Mathf.Max(run.Capacity, run.Students.Count));
                slots[i].Render(i < run.Students.Count ? run.Students[i] : null);
            }
        }
        public void Drop(BattleStudentSlot source, BattleStudentSlot target)
        {
            if (source.Owner != this || target.Owner != this) return;
            if (CanReorder && source.HasStudent && target.HasStudent) screen.Swap(source.Index, target.Index);
        }
        public void CancelDrag() { foreach (var slot in slots) slot.CancelDrag(); }
    }
}
