using System;
using System.Collections.Generic;
using System.Globalization;

namespace HongikIngan
{
    public enum StudentRole { Buff, Debuff, Dealer }
    public enum PetKind { Normal, Advanced, Premium, Synergy }
    public enum ActionKind
    {
        AddDamage, MultiplyDamage, EmpowerNext, WeakenBoss, EarnMoney, SwapNext,
        TeamSizeDamage, AddThenPercentDamage, FourthBatter, Advance, AdvanceToFront,
        StrikeOnMove, Spite, LuckyClover, PowerDamage, RebelPercent
    }
    public enum RelicKind { OpeningDamage, FinalMultiplier, ExtraSlots, VictoryMoney }

    [Serializable]
    public sealed class StudentDefinition
    {
        public string Id, Name, Club, Personality, Description, SkillName, SkillDescription;
        public int Grade;
        // Skills determine behavior. The authored harsh-tongue synergy explicitly checks the displayed role.
        public StudentRole Role;
        public ActionKind Action;
        public float InitialValue, UpgradeStep;
        public float InitialPercent, PercentUpgradeStep;
        public bool UnlimitedUpgrade, Reaction;
        public bool UpgradeEnabled = true;
        public string SpecialSynergy;
        public float ValueAtUpgrade(int level)
        {
            // Prototype content-specific replacement rules; these are temporary balance values.
            if (Action == ActionKind.MultiplyDamage) return InitialValue + UpgradeStep * level;
            if (Action == ActionKind.WeakenBoss) return InitialValue + UpgradeStep * (float)Math.Sqrt(level);
            return InitialValue + UpgradeStep * level;
        }
        public int SynergyCount => string.IsNullOrEmpty(SpecialSynergy) ? 2 : 3;
        public float PercentAtUpgrade(int level) => InitialPercent + PercentUpgradeStep * level;
        public string RoleLabel => Role == StudentRole.Buff ? "지원" : Role == StudentRole.Debuff ? "약화" : "공격";
    }

    [Serializable]
    public sealed class StudentState
    {
        public StudentDefinition Definition;
        public int UpgradeCount;
        public float SkillValue;
        public readonly List<KeywordState> Keywords = new List<KeywordState>();
        public StudentState(StudentDefinition definition) { Definition = definition; SkillValue = definition.InitialValue; }
    }

    [Serializable]
    public sealed class RelicDefinition
    {
        public string Id, Name, Description;
        public RelicKind Kind;
        public float Value;
        public bool DebuffRelated;
    }

    public static class PrototypeCatalog
    {
        public static readonly List<StudentDefinition> Students = CreateStudents();
        public static readonly List<RelicDefinition> Relics = new List<RelicDefinition>
        {
            new RelicDefinition { Id="chalk", Name="빛나는 분필", Description="매 턴 시작 데미지 +40", Kind=RelicKind.OpeningDamage, Value=40 },
            new RelicDefinition { Id="ribbon", Name="별의 리본", Description="필살기 데미지 ×1.25", Kind=RelicKind.FinalMultiplier, Value=1.25f },
            new RelicDefinition { Id="desk", Name="접이식 책상", Description="슬롯 +2 (전체 최대 9)", Kind=RelicKind.ExtraSlots, Value=2 },
            new RelicDefinition { Id="lunch", Name="황금 도시락", Description="보스 승리 보상 +30", Kind=RelicKind.VictoryMoney, Value=30 },
            new RelicDefinition { Id="star", Name="작은 별", Description="매 턴 시작 데미지 +70", Kind=RelicKind.OpeningDamage, Value=70 },
            new RelicDefinition { Id="prism", Name="마법 프리즘", Description="필살기 데미지 ×1.15", Kind=RelicKind.FinalMultiplier, Value=1.15f }
        };

        private static List<StudentDefinition> CreateStudents()
        {
            // Names and rules resolve the authored Markdown, not the stale JSON templates.
            return new List<StudentDefinition>
            {
                Girl(0, "아이린", 1, "운동부", "분위기메이커", StudentRole.Dealer,
                    "4번 타자", "데미지 +40 (강화 80), 현재 4번 자리라면 3배", ActionKind.FourthBatter, 40, 40),
                Girl(1, "효빈", 1, "운동부", "사랑꾼", StudentRole.Buff,
                    "매직 스텝", "자기 차례에 앞으로 1칸 (강화 2칸), 이동할 때마다 데미지 +300", ActionKind.Advance, 1, 1),
                Girl(2, "리오네", 3, "운동부", "리더", StudentRole.Dealer,
                    "G.O.A.T", "1번 자리까지 한 칸씩 이동, 이동할 때마다 현재 데미지 +20% (강화 40%)", ActionKind.AdvanceToFront, 20, 20),
                Girl(3, "찬비", 2, "운동부", "반항아", StudentRole.Buff,
                    "잔나비", "이동할 때마다 모든 아군이 1000 피해로 공격 (강화 1500)", ActionKind.StrikeOnMove, 1000, 500),
                Girl(4, "가키", 1, "운동부", "독설가", StudentRole.Debuff,
                    "표독 부여", "보스 키워드 종류마다 표독 1스택 (강화 2스택) · 스택당 받는 피해 +10%p", ActionKind.Spite, 1, 1),
                Girl(5, "클로에", 1, "운동부", "귀염둥이", StudentRole.Debuff,
                    "럭키 클로에", "40% (강화 55%) 확률로 검은 클로버 부여 · 한 스택 소모마다 ×1.2", ActionKind.LuckyClover, 40, 15),
                Girl(6, "민지", 2, "선도부", "분위기메이커", StudentRole.Dealer,
                    "혁명적인 마법", "전대 인원수 ×100 (강화 150) 데미지 추가", ActionKind.TeamSizeDamage, 100, 50),
                Girl(7, "루크레아", 3, "선도부", "사랑꾼", StudentRole.Dealer,
                    "광선검", "현재 데미지를 D^1.3으로 변경 (강화 D^1.4)", ActionKind.PowerDamage, 1.3f, .1f),
                Girl(8, "가람", 3, "선도부", "리더", StudentRole.Dealer,
                    "엄벌의 요술봉", "전대의 반항아 인원마다 현재 데미지 +100%p (강화 150%p)", ActionKind.RebelPercent, 100, 50),
                Girl(9, "한별", 2, "선도부", "반항아", StudentRole.Dealer,
                    "혁명적인 마법", "전대 인원수 ×100 (강화 150) 데미지 추가", ActionKind.TeamSizeDamage, 100, 50),
                Girl(10, "나연", 1, "선도부", "귀염둥이", StudentRole.Dealer,
                    "굿캅", "현재 데미지의 10% (강화 15%) 추가", ActionKind.AddThenPercentDamage, 0, 0, true, 10, 5),
                Girl(11, "다혜", 1, "선도부", "독설가", StudentRole.Dealer,
                    "독사", "데미지 +50 후 현재 데미지 10% 추가 (강화 +100, 15%)", ActionKind.AddThenPercentDamage, 50, 50, true, 10, 5)
            };
        }

        private static StudentDefinition Girl(int id, string name, int grade, string club, string personality,
            StudentRole role, string skillName, string description, ActionKind action, float value, float upgrade,
            bool upgradeEnabled = true, float percent = 0, float percentUpgrade = 0)
        {
            return new StudentDefinition
            {
                Id = "Girl" + id, Name = name, Grade = grade, Club = club, Personality = personality,
                Role = role, SkillName = skillName, SkillDescription = description, Description = "",
                Action = action, InitialValue = value, UpgradeStep = upgrade, UpgradeEnabled = upgradeEnabled,
                InitialPercent = percent, PercentUpgradeStep = percentUpgrade
            };
        }
        public static string FormatDamage(float value)
        {
            if (float.IsNaN(value)) return "0";
            if (float.IsPositiveInfinity(value)) return "∞";
            if (float.IsNegativeInfinity(value)) return "-∞";
            if (Math.Abs(value) >= 1e10f) return value.ToString("0.00e0", CultureInfo.InvariantCulture);
            return Math.Truncate((double)value).ToString("N0", CultureInfo.InvariantCulture).Replace(',', ' ');
        }
    }
}
