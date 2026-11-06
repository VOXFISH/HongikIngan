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
        StrikeOnMove, Spite, LuckyClover, PowerDamage, RebelPercent,
        Paparazzi, Spotlight, LightingDamage, GrantLove, CloneNext, BestSubject,
        SingleStackBuffDamage, CloverStrike, BulletStrike, LoveAndClone,
        StickyPaint, ArtBrush, MoonRabbit, ArtEncore, WildBrush, Eros,
        StarLove, SaturnCurse, MarsJealousy, TomorrowSun, StarGift, Coffee, GrowingStrike, KeywordKindsDamage
    }
    public enum FortuneKind { Blank, RerollDiscount, RecruitDiscount, FinalDamage, Energy, AllSynergies, SynergyRecruitment, UpgradeAll, BattleReward }

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
            if (Action == ActionKind.GrowingStrike) return InitialValue * (float)Math.Pow(2, level);
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
        public bool IsClone, CloneLastsBattle;
        public int PrecisionCopies;
        public int LunarRoll;
        public readonly List<KeywordState> Keywords = new List<KeywordState>();
        public StudentState(StudentDefinition definition) { Definition = definition; SkillValue = definition.InitialValue; }
    }

    [Serializable]
    public sealed class FortuneDefinition
    {
        public readonly string Id, Description;
        public readonly FortuneKind Kind;
        public readonly float Weight, Value;
        public bool IsBlank => Kind == FortuneKind.Blank;
        public FortuneDefinition(string id, string description, float weight, FortuneKind kind = FortuneKind.Blank, float value = 0)
        { Id = id; Description = description; Weight = weight; Kind = kind; Value = value; }
    }

    public static class PrototypeCatalog
    {
        public static readonly List<StudentDefinition> Students = CreateStudents();
        public static readonly IReadOnlyList<FortuneDefinition> Fortunes = Array.AsReadOnly(new[]
        {
            new FortuneDefinition("reroll", "상점 리롤 비용 50% 감소", 2, FortuneKind.RerollDiscount, .5f),
            new FortuneDefinition("recruit", "모집 비용 50% 감소", 2, FortuneKind.RecruitDiscount, .5f),
            new FortuneDefinition("damage", "공격 완료 후 데미지 2배", 1, FortuneKind.FinalDamage, 2),
            new FortuneDefinition("toe", "엄지발가락을 찧지 않음", 3),
            new FortuneDefinition("dew", "마운틴 듀 1+1", 3),
            new FortuneDefinition("pizza", "급식으로 파인애플 피자가 나옴", 3),
            new FortuneDefinition("malatang", "급식으로 마라탕이 나옴", 3),
            new FortuneDefinition("merge", "머지 컨플릭트 방지", 3),
            new FortuneDefinition("unlucky", "운이 없네요...", 5),
            new FortuneDefinition("energy", "100 미라클 에너지 획득", 1, FortuneKind.Energy, 100),
            new FortuneDefinition("synergies", "모든 시너지 +1 적용", .5f, FortuneKind.AllSynergies, 1),
            new FortuneDefinition("synergy_recruit", "모든 모집이 시너지 모집으로 적용", 1, FortuneKind.SynergyRecruitment),
            new FortuneDefinition("upgrade", "모든 마법소녀 강화", .3f, FortuneKind.UpgradeAll, 1),
            new FortuneDefinition("reward_1_5", "이번 전투 보상 1.5배", 1, FortuneKind.BattleReward, 1.5f),
            new FortuneDefinition("reward_2", "이번 전투 보상 2배", 1, FortuneKind.BattleReward, 2),
            new FortuneDefinition("reward_10", "이번 전투 보상 10배", .3f, FortuneKind.BattleReward, 10)
        });

        public static FortuneDefinition DrawFortune(Random random, bool excludeBlanks)
        {
            double total = 0;
            foreach (var fortune in Fortunes) if (!excludeBlanks || !fortune.IsBlank) total += fortune.Weight;
            double roll = random.NextDouble() * total;
            FortuneDefinition last = null;
            foreach (var fortune in Fortunes)
            {
                if (excludeBlanks && fortune.IsBlank) continue;
                last = fortune;
                roll -= fortune.Weight;
                if (roll < 0) return fortune;
            }
            return last;
        }

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
                    "표독 부여", "보스 키워드 종류마다 표독 1스택 (강화 2스택) - 스택당 받는 피해 +10%p", ActionKind.Spite, 1, 1),
                Girl(5, "클로에", 1, "운동부", "귀염둥이", StudentRole.Debuff,
                    "럭키 클로에", "40% (강화 55%) 확률로 검은 클로버 부여 - 한 스택 소모마다 ×1.2", ActionKind.LuckyClover, 40, 15),
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
                    "독사", "데미지 +50 후 현재 데미지 10% 추가 (강화 +100, 15%)", ActionKind.AddThenPercentDamage, 50, 50, true, 10, 5),
                Girl(12, "지수", 1, "사진부", "리더", StudentRole.Debuff,
                    "파파라치", "넌 내 꺼야: 모든 공격에 +500 및 공격자 전진. 강화: 모든 공격 후 +10%p. 비중첩-비소모-전투 종료까지 유지", ActionKind.Paparazzi, 0, 10),
                Girl(13, "수지", 2, "사진부", "반항아", StudentRole.Dealer,
                    "스포트라이트!", "현재 데미지 +15% (강화 40%) 후 양옆 아군에게 조명 부여", ActionKind.Spotlight, 15, 25),
                Girl(14, "지우", 1, "사진부", "분위기메이커", StudentRole.Dealer,
                    "Lights, Camera, Action!", "데미지 +500 및 조명이 붙은 아군마다 +20 (강화 +30)", ActionKind.LightingDamage, 20, 10),
                Girl(15, "나미", 2, "사진부", "사랑꾼", StudentRole.Buff,
                    "사랑을 담아", "모든 아군에게 사랑 2스택 (강화 3). 다음 공격의 데미지 증가량에 스택당 ×1.1, 모두 소모-턴 종료 삭제", ActionKind.GrantLove, 2, 1),
                Girl(16, "정윤", 3, "사진부", "독설가", StudentRole.Buff,
                    "이거 사진이라니까요", "다음 아군의 분신을 생성 - 턴 종료 시 삭제 (강화: 전투 종료). 정밀화: 복제마다 +3000씩 증가", ActionKind.CloneNext, 0, 1, specialSynergy: "정밀화"),
                Girl(17, "치즈", 1, "사진부", "귀염둥이", StudentRole.Dealer,
                    "최고의 피사체", "귀염둥이-1학년-사진부 아군 수의 합만큼 데미지 배율 (강화: 인원수의 곱)", ActionKind.BestSubject, 0, 1),
                Girl(18, "다나", 1, "미술부", "독설가", StudentRole.Debuff,
                    "끈적이 페인트", "자신의 미감 수만큼 끈적함 부여. 미술부 피해 증가량 x1.3 (강화: 턴 종료 후에도 유지)", ActionKind.StickyPaint, 0, 1),
                Girl(19, "예리", 1, "미술부", "반항아", StudentRole.Buff,
                    "예리한 붓", "아군 모두에게 미감 1 (강화 2). 자신의 미감 수 x300 데미지 추가", ActionKind.ArtBrush, 1, 1),
                Girl(20, "루나", 2, "미술부", "귀염둥이", StudentRole.Debuff,
                    "달토끼", "매 전투 1~10 (강화 1~12) 추첨. 해당 수만큼 검은 클로버. 미감 사용 시 재추첨해 높은 값 유지", ActionKind.MoonRabbit, 10, 2),
                Girl(21, "가영", 2, "미술부", "분위기메이커", StudentRole.Dealer,
                    "가영가영", "데미지 +35%p (강화 50%p). 미감으로 재사용. 재사용마다 양옆에 미감 2", ActionKind.ArtEncore, 35, 15),
                Girl(22, "지나", 2, "미술부", "리더", StudentRole.Dealer,
                    "광란의 붓질", "재행동 시 자신과 양옆에 미감 5. 셋 중 미감 20 이상 (강화 15)이면 자신 복제", ActionKind.WildBrush, 20, -5),
                Girl(23, "민영", 3, "미술부", "사랑꾼", StudentRole.Debuff,
                    "에로스", "5+자신의 미감% (강화 15+미감%) 확률로 보스 HP를 1로 변경. 실패 시 모든 아군 미감 3", ActionKind.Eros, 5, 10),
                Girl(24, "하나", 1, "천문부", "귀염둥이", StudentRole.Dealer,
                    "하나밖에 없는 태양", "자신의 1스택 또는 중첩 불가 버프 종류마다 데미지 x1.11 (강화: 스택 수 조건 제거)", ActionKind.SingleStackBuffDamage, 1.11f, 0),
                Girl(25, "예진", 2, "천문부", "사랑꾼", StudentRole.Buff,
                    "하늘의 별만큼 사랑해", "별의 힘만큼 다음 아군에게 사랑 (강화: 다음 3명)", ActionKind.StarLove, 1, 2),
                Girl(26, "하진", 2, "천문부", "독설가", StudentRole.Debuff,
                    "토성의 저주", "별의 힘%만큼 현재 보스 HP 감소. 최대 50% (강화 60%)", ActionKind.SaturnCurse, 50, 10),
                Girl(27, "혜림", 2, "천문부", "반항아", StudentRole.Dealer,
                    "화성의 질투", "전대 정원 -2, 전투 제한 +1턴 (강화 -3, +2턴). 자신의 액션 데미지 x5", ActionKind.MarsJealousy, 2, 1),
                Girl(28, "수현", 3, "천문부", "리더", StudentRole.Dealer,
                    "내일의 태양", "게임당 한 번 게임오버 방어. 별의 힘 x100 에너지와 영구 버프: 매 행동 데미지 ^1.3 (강화: 재화 없이 ^1.5)", ActionKind.TomorrowSun, 1.3f, .2f),
                Girl(29, "소미", 1, "천문부", "분위기메이커", StudentRole.Buff,
                    "별이 돼 줄게", "공용 별의 힘 +1, 데미지 +500 (강화 1000)", ActionKind.StarGift, 500, 500),
                Girl(30, "미네", 1, "연극부", "리더", StudentRole.Debuff,
                    "별의 커피", "별의 힘만큼 커피 부여 (강화: 2배). 턴 시작에 스택 x20 보스 HP 감소 후 감소량만큼 스택 증가", ActionKind.Coffee, 1, 1),
                Girl(31, "신아", 1, "연극부", "귀염둥이", StudentRole.Debuff,
                    "무대의 꽃", "보스에게 검은 클로버 1스택 부여 후 데미지 +300 (강화 400)", ActionKind.CloverStrike, 300, 100),
                Girl(32, "민희", 2, "연극부", "반항아", StudentRole.Dealer,
                    "군필 여고생", "매 전투 총알 1발 (강화 2발). 액션마다 1발 소모해 데미지 +100000", ActionKind.BulletStrike, 1, 1),
                Girl(33, "세아", 2, "연극부", "분위기메이커", StudentRole.Dealer,
                    "세아isHere", "데미지 +10. 보충 학습 횟수마다 스킬 데미지 x2, 무한 강화 가능", ActionKind.GrowingStrike, 10, 0),
                Girl(34, "마리", 3, "연극부", "사랑꾼", StudentRole.Buff,
                    "돌돌마리", "모든 아군에게 사랑 3스택. 연극부가 정확히 3명 (강화 2명 또는 3명)이면 자신 복제", ActionKind.LoveAndClone, 3, -1),
                Girl(35, "드미트리우스", 2, "연극부", "독설가", StudentRole.Dealer,
                    "스파르타!!", "팀 전체의 서로 다른 버프 종류 수 /2만큼 데미지 배율 (강화: 종류 수 그대로)", ActionKind.KeywordKindsDamage, .5f, .5f)
            };
        }

        private static StudentDefinition Girl(int id, string name, int grade, string club, string personality,
            StudentRole role, string skillName, string description, ActionKind action, float value, float upgrade,
            bool upgradeEnabled = true, float percent = 0, float percentUpgrade = 0, string specialSynergy = null)
        {
            return new StudentDefinition
            {
                Id = "Girl" + id, Name = name, Grade = grade, Club = club, Personality = personality,
                Role = role, SkillName = skillName, SkillDescription = description, Description = "",
                Action = action, InitialValue = value, UpgradeStep = upgrade, UpgradeEnabled = upgradeEnabled,
                UnlimitedUpgrade = action == ActionKind.GrowingStrike,
                InitialPercent = percent, PercentUpgradeStep = percentUpgrade, SpecialSynergy = specialSynergy
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
