using TMPro;
using UnityEngine;

namespace HongikIngan
{
    // The floating panel follows its subject; its size, offset and typography are authored.
    public sealed class BattleStudentTooltip : MonoBehaviour
    {
        public RectTransform panel, bounds;
        public TMP_Text nameLabel, skillLabel, effectLabel;
        public Vector2 offset = new Vector2(0, 20);
        public bool IsOpen => panel != null && panel.gameObject.activeSelf;
        public void Show(BattleStudentSlot slot)
        {
            var student = slot.Student;
            if (student == null) return;
            nameLabel.text = student.Definition.Name;
            skillLabel.text = student.Definition.SkillName;
            effectLabel.text = BattleHud.DisplayText(student.Definition.SkillDescription) + "\n\n강화 " + student.UpgradeCount +
                "회 / 현재 스킬 수치 " + (Mathf.Abs(student.SkillValue) >= 1e10f ? PrototypeCatalog.FormatDamage(student.SkillValue) :
                    student.SkillValue.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture));
            var point = (Vector2)bounds.InverseTransformPoint(slot.nameLabel.transform.position) + offset;
            var area = bounds.rect;
            point.x = Mathf.Clamp(point.x, area.xMin + panel.rect.width / 2 + 12, area.xMax - panel.rect.width / 2 - 12);
            point.y = Mathf.Clamp(point.y, area.yMin + 12, area.yMax - panel.rect.height - 12);
            panel.localPosition = new Vector3(point.x, point.y, 0);
            panel.gameObject.SetActive(true);
        }
        public void Hide() { if (panel != null) panel.gameObject.SetActive(false); }
    }
}
