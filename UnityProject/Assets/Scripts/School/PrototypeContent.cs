using System;
using System.Collections.Generic;
using System.Globalization;

namespace HongikIngan
{
    public enum StudentRole { Buff, Debuff, Dealer }
    public enum PetKind { Normal, Advanced, Premium, Synergy }
    public enum ActionKind { AddDamage, MultiplyDamage, EmpowerNext, WeakenBoss, EarnMoney, SwapNext }
    public enum RelicKind { OpeningDamage, FinalMultiplier, ExtraSlots, VictoryMoney }

    [Serializable]
    public sealed class StudentDefinition
    {
        public string Id, Name, Club, Personality, Description, SkillDescription;
        public int Grade;
        // Role is presentation only. Action, Value and Reaction determine behavior.
        public StudentRole Role;
        public ActionKind Action;
        public float InitialValue, UpgradeStep;
        public bool UnlimitedUpgrade, Reaction;
        public string SpecialSynergy;
        public float ValueAtUpgrade(int level)
        {
            // Prototype content-specific replacement rules; these are temporary balance values.
            if (Action == ActionKind.MultiplyDamage) return InitialValue + UpgradeStep * level;
            if (Action == ActionKind.WeakenBoss) return InitialValue + UpgradeStep * (float)Math.Sqrt(level);
            return InitialValue + UpgradeStep * level;
        }
        public int SynergyCount => string.IsNullOrEmpty(SpecialSynergy) ? 2 : 3;
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
            var names = new[] { "아리", "하린", "소라", "예린", "나래", "다온", "유나", "새봄", "별하", "서우", "하늘", "지우", "가온", "노을", "미리", "루나", "시온", "라온" };
            var clubs = new[] { "운동부", "천문부", "선도부", "미술부", "연극부", "사진부" };
            var personalities = new[] { "리더", "분위기메이커", "반항아", "사랑꾼", "독설가", "귀염둥이" };
            var result = new List<StudentDefinition>();
            for (int i = 0; i < names.Length; i++)
            {
                int grade = i / 6 + 1;
                var action = new[] { ActionKind.AddDamage, ActionKind.EmpowerNext, ActionKind.AddDamage, ActionKind.MultiplyDamage, ActionKind.WeakenBoss, ActionKind.EarnMoney }[i % 6];
                float value = action == ActionKind.MultiplyDamage ? 1.3f + grade * .1f : action == ActionKind.WeakenBoss ? 10f + grade * 5 : action == ActionKind.EarnMoney ? 5 + grade * 3 : 60 + grade * 40;
                var role = action == ActionKind.WeakenBoss ? StudentRole.Debuff : action == ActionKind.EmpowerNext || action == ActionKind.EarnMoney ? StudentRole.Buff : StudentRole.Dealer;
                result.Add(new StudentDefinition
                {
                    Id = "student_" + i, Name = names[i], Grade = grade, Role = role,
                    Club = clubs[i % 6], Personality = personalities[(i + 2) % 6],
                    Description = clubs[i % 6] + "에서 활동하는 마법소녀. 프로토타입 임시 콘텐츠입니다.",
                    Action = action, InitialValue = value, UpgradeStep = action == ActionKind.MultiplyDamage ? .2f : 20f + grade * 10f,
                    UnlimitedUpgrade = i % 2 == 0,
                    Reaction = i == 14,
                    SpecialSynergy = i % 5 == 0 ? "별의 약속" : null,
                    SkillDescription = DescriptionFor(action) + (i == 14 ? " · 다른 아군 액션에도 반응" : "")
                });
            }
            return result;
        }

        private static string DescriptionFor(ActionKind action)
        {
            switch (action)
            {
                case ActionKind.AddDamage: return "합동 데미지에 현재 스킬 값을 더합니다";
                case ActionKind.MultiplyDamage: return "합동 데미지에 현재 스킬 값을 곱합니다";
                case ActionKind.EmpowerNext: return "다음 아군에게 소모성 별빛 키워드 부여";
                case ActionKind.WeakenBoss: return "보스에게 이번 턴 받는 피해 %p 증가 키워드 부여";
                case ActionKind.EarnMoney: return "미라클 에너지를 획득합니다";
                default: return "다음 아군과 위치를 교체합니다";
            }
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
