using System.Linq;
using System.Text;

namespace HongikIngan
{
    public static class SynergyText
    {
        public const string White = "FFFFFF", Yellow = "FFD84D", Gray = "999999";
        public static string Color(string text, string color) => $"<color=#{color}>{BattleHud.DisplayText(text)}</color>";
        public static string Link(string name, string text = null) => $"<link=\"{name}\">{text ?? name}</link>";
        public static string Traits(StudentState student) => string.Join(" - ", new[]{student.Definition.Club,
            student.Definition.Personality, student.Definition.SpecialSynergy}.Where(n => !string.IsNullOrEmpty(n)).Select(n => Link(n)));
        public static string Entry(PrototypeRun run, SynergyDefinition definition)
        {
            int count = BattleSynergies.Count(run, definition.Name);
            bool active = SynergyCatalog.Active(run, definition);
            bool maximum = active && definition.ActiveTier(count) == definition.Thresholds.Length - 1;
            int target = maximum ? definition.Thresholds.Last() : definition.Target(count);
            return Link(definition.Name, Color($"{definition.Name} ({count} / {target})", !active ? Gray : maximum ? Yellow : White));
        }
        public static string List(PrototypeRun run, bool activeOnly)
        {
            var owned = SynergyCatalog.All.Where(d => BattleSynergies.Count(run, d.Name) > 0).ToArray();
            var active = owned.Where(d => SynergyCatalog.Active(run, d)).Select(d => Entry(run, d)).ToArray();
            string result = "활성 시너지\n" + (active.Length == 0 ? Color("없음", Gray) : string.Join("\n", active));
            if (!activeOnly)
            {
                var inactive = owned.Where(d => !SynergyCatalog.Active(run, d)).Select(d => Entry(run, d)).ToArray();
                result += "\n\n비활성 시너지\n" + (inactive.Length == 0 ? Color("없음", Gray) : string.Join("\n", inactive));
            }
            return result;
        }
        public static string Popup(PrototypeRun run, string name)
        {
            var d = SynergyCatalog.Find(name);
            if (d == null) return name;
            var text = new StringBuilder().Append("<b>").Append(d.Name).Append("</b>\n").Append(d.Description).Append("\n\n");
            int count = BattleSynergies.Count(run, name);
            for (int i = 0; i < d.Thresholds.Length; i++)
            {
                bool reached = d.Reached(count, i) && (name != "사랑꾼" || SynergyCatalog.LoverCount(run) >= 2);
                text.AppendLine(Color($"({d.Thresholds[i]}) {d.Effects[i]}", reached ? White : Gray));
            }
            if (name == "분위기메이커")
            {
                text.Append($"\n현재 분위기량: {run.Atmosphere:0.##}\n");
                int[] limits = {1,100,300,500,5000};
                string[] effects = {"1마다 최종 데미지 +1%p", "마지막 소녀 액션 후 데미지 +50%p", "마지막 소녀 추가 액션 (턴당 1회)", "마지막 소녀의 기존 시너지 인원 +1", "위 효과를 모든 소녀에게 적용"};
                for (int i = 0; i < limits.Length; i++) text.AppendLine(Color($"({limits[i]}) {effects[i]}", run.Atmosphere >= limits[i] ? White : Gray));
            }
            if (name == "천문부") text.Append($"\n현재 별의 힘: {run.StarPower}\n별의 힘 1마다 총 데미지 +30%p");
            if (name == "정밀화") text.Append("\n현재 증가량: " + run.Students.Where(s => s.Definition.SpecialSynergy == name)
                .Select(s => 3000f * s.PrecisionCopies).DefaultIfEmpty(0).Max().ToString("0"));
            if (name == "사랑꾼") text.Append($"\n적용 인접 인원: {SynergyCatalog.LoverCount(run)}명");
            return text.ToString().TrimEnd();
        }
    }
}
