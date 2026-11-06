using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HongikIngan
{
    // The floating panel follows its subject; its size, offset and typography are authored.
    public sealed class BattleStudentTooltip : MonoBehaviour
    {
        public RectTransform panel, bounds;
        public TMP_Text nameLabel, skillLabel, effectLabel;
        public Vector2 offset = new Vector2(0, 20);
        public StudentCardView card;
        BattleStudentSlot source;
        StudentState shown;
        float exitAt = -1;
        public bool IsOpen => panel != null && panel.gameObject.activeSelf;
        public void Show(BattleStudentSlot slot)
        {
            var student = slot.Student;
            if (student == null) return;
            if (IsOpen && source == slot && shown == student) return;
            source = slot; shown = student; exitAt = -1;
            var point = (Vector2)bounds.InverseTransformPoint(slot.nameLabel.transform.position) + offset;
            panel.localPosition = new Vector3(point.x, point.y, 0);
            panel.gameObject.SetActive(true);
            card.Render(student);
            SynergyPopup.Clamp(panel, bounds);
        }
        public void RequestHide() { exitAt = Time.unscaledTime + .3f; }
        void Update()
        {
            if (!IsOpen) return;
            if (source == null || source.Student != shown || !source.gameObject.activeInHierarchy) { Hide(); return; }
            if (Mouse.current == null) return;
            var canvas = panel.GetComponentInParent<Canvas>();
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 point = Mouse.current.position.ReadValue();
            if (RectTransformUtility.RectangleContainsScreenPoint(panel, point, camera) ||
                RectTransformUtility.RectangleContainsScreenPoint((RectTransform)source.transform, point, camera)) exitAt = -1;
            else if (exitAt < 0) RequestHide();
            else if (Time.unscaledTime >= exitAt) Hide();
        }
        public void Hide() { source = null; shown = null; if (panel != null) panel.gameObject.SetActive(false); }
        void OnDisable() => Hide();
    }
}
