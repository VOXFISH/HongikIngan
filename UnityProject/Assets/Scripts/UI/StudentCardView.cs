using TMPro;
using UnityEngine;

namespace HongikIngan
{
    public sealed class StudentCardView : MonoBehaviour
    {
        public TMP_Text title, body;
        public RectTransform bounds;
        StudentState shown;
        void OnEnable() => Roll();
        void Roll()
        {
            transform.localRotation = Quaternion.Euler(0,0,Random.Range(-10f,10f));
            if (bounds != null) SynergyPopup.Clamp((RectTransform)transform, bounds);
        }
        public void Render(StudentState student)
        {
            if (shown != student) { shown = student; Roll(); }
            title.text = $"{student.Definition.Name} / {student.Definition.Grade}학년 - {student.Definition.RoleLabel}";
            body.text = Body(student);
        }
        public static string Body(StudentState s)
        {
            string values = s.Definition.Action == ActionKind.AddThenPercentDamage ?
                $"+{s.SkillValue:0.##} 후 +{s.Definition.PercentAtUpgrade(s.UpgradeCount):0.##}%" : $"{s.SkillValue:0.##}";
            switch (s.Definition.Action)
            {
                case ActionKind.Paparazzi: values = s.UpgradeCount > 0 ? "+500 후 +10%p" : "공격마다 +500"; break;
                case ActionKind.Spotlight: values = $"+{s.SkillValue:0.##}% - 양옆 조명"; break;
                case ActionKind.LightingDamage: values = $"+500 후 조명마다 +{s.SkillValue:0.##}"; break;
                case ActionKind.GrantLove: values = $"사랑 {(int)s.SkillValue}스택 - 스택당 ×1.1"; break;
                case ActionKind.CloneNext: values = (s.UpgradeCount > 0 ? "전투 종료까지 분신" : "턴 종료까지 분신") + $" - 다음 정밀화 +{3000f * (s.PrecisionCopies + 1):0}"; break;
                case ActionKind.BestSubject: values = s.UpgradeCount > 0 ? "세 조건 인원수의 곱" : "세 조건 인원수의 합"; break;
            }
            return BattleHud.DisplayText($"{SynergyText.Traits(s)}\n\n<b>{s.Definition.SkillName}</b>\n{s.Definition.SkillDescription}\n\n현재 효과 {values} - 강화 {s.UpgradeCount}회");
        }
    }
}
