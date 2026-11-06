using System;
using System.Linq;

namespace HongikIngan
{
    // Display metadata from Docs/홍익인간/Girls/synergy.md. Combat owns effect execution.
    public sealed class SynergyDefinition
    {
        public readonly string Name, Word, Description;
        public readonly int[] Thresholds;
        public readonly string[] Effects;
        public readonly bool Exact;
        public SynergyDefinition(string name, string word, string description, int[] thresholds, string[] effects, bool exact = false)
        { Name = name; Word = word; Description = description; Thresholds = thresholds; Effects = effects; Exact = exact; }
        public bool Reached(int count, int tier) => Exact ? count == Thresholds[tier] : count >= Thresholds[tier];
        public int ActiveTier(int count)
        {
            for (int i = Thresholds.Length - 1; i >= 0; i--) if (Reached(count, i)) return i;
            return -1;
        }
        public int Target(int count) => Thresholds.Where(n => n > count).DefaultIfEmpty(Thresholds.Last()).First();
    }

    public static class SynergyCatalog
    {
        public static readonly SynergyDefinition[] All = {
            new SynergyDefinition("운동부", "활력", "운동부는 데미지를 강화합니다.", new[]{2,3,4,5,6},
                new[]{"데미지 +200", "데미지 +400", "데미지 +800 후 ×2", "데미지 +1600 후 ×10", "데미지 +500000 후 ×10"}),
            new SynergyDefinition("선도부", "정의", "선도부는 미라클 에너지 획득량을 증가시킵니다.", new[]{2,3,4,6},
                new[]{"미라클 에너지 획득량 ×1.5", "미라클 에너지 획득량 ×2", "미라클 에너지 획득량 ×2, 전투 종료 시 +100", "전투 종료 시 보유 미라클 에너지 ×2"}),
            new SynergyDefinition("사진부", "추억", "사진부는 폴라로이드의 수에 따라 데미지를 강화합니다.", new[]{2,4,6},
                new[]{"폴라로이드 한 장마다 현재 데미지 +5%p", "폴라로이드 한 장마다 현재 데미지 +30%p", "폴라로이드 한 장마다 데미지 ×1.2"}),
            new SynergyDefinition("천문부", "별의 힘", "천문부는 별의 힘을 모읍니다.", new[]{2,4,6},
                new[]{"전투 종료마다 별의 힘 +1", "전투 종료마다 별의 힘 +4", "전투 종료마다 별의 힘 +20"}),
            new SynergyDefinition("미술부", "나만의 예술", "미술부는 스킬을 재사용합니다.", new[]{2,3,4,5,6},
                new[]{"미술부 액션마다 5% 확률로 자신의 액션 재발동", "미술부 액션마다 10% 확률로 자신의 액션 재발동", "미술부 액션마다 20% 확률로 자신의 액션 재발동", "미술부 액션마다 30% 확률로 자신의 액션 재발동", "미술부 액션마다 50% 확률로 자신의 액션 재발동"}),
            new SynergyDefinition("연극부", "혼신의 연기", "연극부는 정확히 1명 또는 4명이 있을 때만 활성화됩니다.", new[]{1,4},
                new[]{"발동된 시너지의 종류마다 데미지 +100%", "발동된 시너지의 종류마다 데미지 ×1.5"}, true),
            new SynergyDefinition("리더", "용기", "리더는 버프가 적용될 때마다 팀을 강화합니다.", new[]{1},
                new[]{"리더가 버프 적용 시 현재 합동 데미지 +30%"}),
            new SynergyDefinition("분위기메이커", "우정", "분위기메이커는 분위기 스택을 지닙니다.", new[]{1,3,5,9},
                new[]{"분위기메이커 액션마다 분위기 +30", "분위기메이커 액션마다 분위기 +50", "분위기메이커 액션마다 분위기 +100", "분위기메이커 액션마다 분위기 +1000"}),
            new SynergyDefinition("반항아", "꿈", "반항아는 추가 액션을 갖습니다.", new[]{2,5},
                new[]{"반항아 액션을 두 번 실행, 최종 데미지 ×0.7", "반항아 액션마다 뒤로 한 칸 이동, 최종 데미지 ×0.1"}),
            new SynergyDefinition("사랑꾼", "사랑", "인접한 사랑꾼이 많을수록 데미지가 강화됩니다.", new[]{2},
                new[]{"인접한 사랑꾼 묶음의 실제 인원 1명당 데미지 +100%"}),
            new SynergyDefinition("독설가", "너네 XX", "독설가는 디버프의 효과를 강화합니다.", new[]{3,6},
                new[]{"약화 역할군 액션마다 데미지 ×1.2", "약화 역할군 액션마다 데미지 ×1.2, 모든 디버프 부여 효과 두 번 적용"}),
            new SynergyDefinition("귀염둥이", "매력", "귀염둥이는 귀엽습니다. 더 필요할까요?", new[]{1,3,5,7,9},
                new[]{"귀여움", "조금 더 귀여움", "디버프 관련 유물 효과 두 번 적용", "디버프 관련 유물 효과 두 번 적용, 아군 소모성 키워드 소모 방지", "모든 유물 효과 두 번 적용"}),
            new SynergyDefinition("정밀화", "나만의 그림", "정밀화는 복제에 성공할 때마다 데미지가 3000 증가합니다.", new[]{1},
                new[]{"복제 성공마다 3000, 6000, 9000, … 데미지 추가"})
        };
        public static SynergyDefinition Find(string name) => All.FirstOrDefault(d => d.Name == name);
        public static int LoverCount(PrototypeRun run)
        {
            if (run.InBattle && run.Battle != null && run.Battle.Turn > 0) return (int)(run.Battle.LoverBonusPercent / 100);
            int adjacent = 0;
            for (int i = 0; i < run.Students.Count; i++)
                if (run.Students[i].Definition.Personality == "사랑꾼" &&
                    (i > 0 && run.Students[i - 1].Definition.Personality == "사랑꾼" ||
                     i + 1 < run.Students.Count && run.Students[i + 1].Definition.Personality == "사랑꾼")) adjacent++;
            return adjacent;
        }
        public static bool Active(PrototypeRun run, SynergyDefinition definition) =>
            definition.Name == "사랑꾼" ? LoverCount(run) >= 2 : definition.ActiveTier(BattleSynergies.Count(run, definition.Name)) >= 0;
    }
}
