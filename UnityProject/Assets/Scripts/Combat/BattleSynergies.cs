using System;
using System.Collections.Generic;
using System.Linq;
using static HongikIngan.CombatEngine;

namespace HongikIngan
{
    /// <summary>Shared effects for the synergies belonging to the authored roster.</summary>
    public static class BattleSynergies
    {
        public const string AllCostsFreeDescription = "모든 비용 공짜!";
        public static int Count(PrototypeRun run, string name)
        {
            int count = 0;
            for (int i = 0; i < run.Students.Count; i++)
            {
                var d = run.Students[i].Definition;
                if (d.Club != name && d.Personality != name && d.SpecialSynergy != name) continue;
                count += 1 + (run.Atmosphere >= 5000 || run.Atmosphere >= 500 && i == run.Students.Count - 1 ? 1 : 0);
            }
            return count + run.FortuneSynergyBonus;
        }

        public static void BeginTurn(PrototypeRun run)
        {
            run.Battle.MoodReplays.Clear();
            if (run.Battle.Turn != 1) return;
            int adjacent = 0;
            for (int i = 0; i < run.Students.Count; i++)
            {
                if (run.Students[i].Definition.Personality != "사랑꾼") continue;
                if (i > 0 && run.Students[i - 1].Definition.Personality == "사랑꾼" ||
                    i + 1 < run.Students.Count && run.Students[i + 1].Definition.Personality == "사랑꾼") adjacent++;
            }
            run.Battle.LoverBonusPercent = adjacent * 100;
        }

        public static IEnumerable<BattleStep> AfterAction(PrototypeRun run, StudentState actor)
        {
            if (actor.Definition.Personality == "분위기메이커")
            {
                int count = Count(run, "분위기메이커");
                int amount = count >= 9 ? 1000 : count >= 5 ? 100 : count >= 3 ? 50 : 30;
                run.Atmosphere += amount;
                yield return Step(run.Battle, "synergy", "분위기 +" + amount + " - 누적 " + run.Atmosphere, actor, sourceName: "분위기메이커");
            }
            if (Count(run, "독설가") >= 3 && actor.Definition.Role == StudentRole.Debuff)
            {
                run.Battle.Damage *= 1.2f; run.NotifyDamage();
                yield return Step(run.Battle, "damage", "독설가 - 약화 역할군 액션 ×1.2", actor, sourceName: "독설가", operation: Multiply(1.2));
            }
            if (run.Atmosphere >= 100 && MoodTarget(run, actor))
            {
                AddDamage(run, run.Battle.Damage * .5f, Multiply(1.5));
                yield return Step(run.Battle, "damage", "분위기 - " + actor.Definition.Name + " 액션 시 현재 데미지 +50%p", actor, sourceName: "분위기메이커");
            }
        }

        static bool MoodTarget(PrototypeRun run, StudentState actor) => run.Atmosphere >= 5000 || ReferenceEquals(run.Students.LastOrDefault(), actor);
        public static bool TakeMoodReplay(PrototypeRun run, StudentState actor) =>
            run.Atmosphere >= 300 && MoodTarget(run, actor) &&
            (run.Atmosphere >= 5000 || run.Battle.MoodReplays.Count == 0) && run.Battle.MoodReplays.Add(actor);
        public static bool DoubleRebelAction(PrototypeRun run, StudentState actor) =>
            actor.Definition.Personality == "반항아" && Count(run, "반항아") >= 2 && Count(run, "반항아") < 5;
        public static bool MoveRebelBack(PrototypeRun run, StudentState actor) =>
            actor.Definition.Personality == "반항아" && Count(run, "반항아") >= 5;
        public static int DebuffApplications(PrototypeRun run) => Count(run, "독설가") >= 6 ? 2 : 1;
        public static float ArtReplayChance(PrototypeRun run)
        {
            int n = Count(run, "미술부");
            return n >= 6 ? .5f : n >= 5 ? .3f : n >= 4 ? .2f : n >= 3 ? .1f : n >= 2 ? .05f : 0;
        }
        public static int ActiveKinds(PrototypeRun run)
        {
            int count = 0;
            foreach (string name in new[] { "운동부", "선도부", "사진부", "천문부", "미술부", "반항아" })
                if (Count(run, name) >= 2) count++;
            if (Count(run, "연극부") == 1 || Count(run, "연극부") == 4) count++;
            foreach (string name in new[] { "리더", "분위기메이커", "귀염둥이", "정밀화" }) if (Count(run, name) >= 1) count++;
            if (Count(run, "독설가") >= 3) count++;
            if (run.Battle.LoverBonusPercent > 0) count++;
            return count;
        }
        public static bool KeepAllyConsumables(PrototypeRun run) => Count(run, "귀염둥이") >= 7 && Count(run, "귀염둥이") < 9;
        public static int FortuneStrength(PrototypeRun run) => Count(run, "귀염둥이") >= 5 && Count(run, "귀염둥이") < 9 ? 2 : 1;
        public static bool AllCostsFree(PrototypeRun run) => Count(run, "귀염둥이") >= 9;

        public static IEnumerable<BattleStep> LeaderBuff(PrototypeRun run, StudentState actor)
        {
            if (actor.Definition.Personality != "리더") yield break;
            AddDamage(run, run.Battle.Damage * .3f, Multiply(1.3));
            yield return Step(run.Battle, "damage", "리더의 버프 적용 - 현재 데미지 +30%", actor, sourceName: "리더");
        }

        public static IEnumerable<BattleStep> FinalizeDamage(PrototypeRun run)
        {
            int sports = Count(run, "운동부");
            float flat = sports >= 6 ? 500000 : sports >= 5 ? 1600 : sports >= 4 ? 800 : sports >= 3 ? 400 : sports >= 2 ? 200 : 0;
            float factor = sports >= 5 ? 10 : sports >= 4 ? 2 : 1;
            if (flat > 0)
            {
                AddDamage(run, flat);
                yield return Step(run.Battle, "damage", "운동부 " + sports + " - 데미지 +" + flat, sourceName: "운동부");
                if (factor > 1)
                {
                    run.Battle.Damage *= factor; run.NotifyDamage();
                    yield return Step(run.Battle, "damage", "운동부 - 데미지 ×" + factor, sourceName: "운동부", operation: Multiply(factor));
                }
            }
            int photography = Count(run, "사진부");
            if (photography >= 2 && run.PhotoCount > 0)
            {
                // Each photo applies to the current damage, matching the +%p rule.
                double perPhoto = photography >= 6 ? 1.2 : photography >= 4 ? 1.3 : 1.05;
                if (run.Battle.Damage != 0) run.Battle.Damage = (float)(run.Battle.Damage * Math.Pow(perPhoto, run.PhotoCount));
                run.NotifyDamage();
                yield return Step(run.Battle, "damage", "사진부 - 폴라로이드 " + run.PhotoCount + "장 - 장마다 ×" + perPhoto, sourceName: "사진부", operation: Multiply(Math.Pow(perPhoto, run.PhotoCount)));
            }
            int rebels = Count(run, "반항아");
            if (rebels >= 2)
            {
                float multiplier = rebels >= 5 ? .1f : .7f;
                run.Battle.Damage *= multiplier; run.NotifyDamage();
                yield return Step(run.Battle, "damage", "반항아 - 데미지 ×" + multiplier, sourceName: "반항아", operation: Multiply(multiplier));
            }
            if (run.Atmosphere > 0)
            {
                AddDamage(run, run.Battle.Damage * run.Atmosphere / 100f, Multiply(1 + run.Atmosphere / 100f));
                yield return Step(run.Battle, "damage", "누적 분위기 - 데미지 +" + run.Atmosphere + "%p", sourceName: "분위기메이커");
            }
            if (run.Battle.LoverBonusPercent > 0)
            {
                AddDamage(run, run.Battle.Damage * run.Battle.LoverBonusPercent / 100f, Multiply(1 + run.Battle.LoverBonusPercent / 100f));
                yield return Step(run.Battle, "damage", "전투 시작의 인접 사랑꾼 - 데미지 +" + run.Battle.LoverBonusPercent + "%", sourceName: "사랑꾼");
            }
            if (run.StarPower > 0)
            {
                AddDamage(run, run.Battle.Damage == 0 ? 0 : run.Battle.Damage * run.StarPower * .3f, Multiply(1 + run.StarPower * .3f));
                yield return Step(run.Battle, "damage", "별의 힘 " + run.StarPower + " - 데미지 +" + run.StarPower * 30f + "%p", sourceName: "천문부");
            }
            int theatre = Count(run, "연극부");
            if (theatre == 1 || theatre == 4)
            {
                int active = ActiveKinds(run);
                double multiplier = theatre == 1 ? 1 + active : Math.Pow(1.5, active);
                if (run.Battle.Damage != 0) run.Battle.Damage = (float)(run.Battle.Damage * multiplier);
                run.NotifyDamage();
                yield return Step(run.Battle, "damage", "연극부 - 활성 시너지 " + active + "종류 - x" + multiplier.ToString("0.##"), sourceName: "연극부", operation: Multiply(multiplier));
            }
        }

        public static int Income(PrototypeRun run, int amount)
        {
            return IncomeForCount(amount, Count(run, "선도부"));
        }
        internal static int IncomeForCount(int amount, int discipline)
        {
            double multiplier = discipline >= 6 ? 1 : discipline >= 3 ? 2 : discipline >= 2 ? 1.5 : 1;
            return checked((int)Math.Floor(Math.Max(0, amount) * multiplier));
        }

        public static void FinishBattle(PrototypeRun run)
        {
            if (run.Battle == null || !run.Battle.Ended || run.Battle.SynergyRewardsApplied) return;
            run.Battle.SynergyRewardsApplied = true;
            int discipline = run.Battle.EndDisciplineCount >= 0 ? run.Battle.EndDisciplineCount : Count(run, "선도부");
            if (discipline >= 6) run.AddBattleEndMoney(run.Money, discipline);
            else if (discipline >= 4) run.AddBattleEndMoney(100, discipline);
            int astronomy = run.Battle.EndAstronomyCount >= 0 ? run.Battle.EndAstronomyCount : Count(run, "천문부");
            run.StarPower += astronomy >= 6 ? 20 : astronomy >= 4 ? 4 : astronomy >= 2 ? 1 : 0;
        }
    }
}
