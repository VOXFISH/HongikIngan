using System;
using System.Collections.Generic;
using System.Linq;
using static HongikIngan.CombatEngine;

namespace HongikIngan
{
    /// <summary>Shared effects for the eight synergies belonging to the authored Girl0–Girl11 roster.</summary>
    public static class BattleSynergies
    {
        public static int Count(PrototypeRun run, string name)
        {
            int count = 0;
            for (int i = 0; i < run.Students.Count; i++)
            {
                var d = run.Students[i].Definition;
                if (d.Club != name && d.Personality != name && d.SpecialSynergy != name) continue;
                count += 1 + (run.Atmosphere >= 5000 || run.Atmosphere >= 500 && i == run.Students.Count - 1 ? 1 : 0);
            }
            return count;
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
                yield return Step(run.Battle, "synergy", "분위기 +" + amount + " · 누적 " + run.Atmosphere);
            }
            if (Count(run, "독설가") >= 3 && actor.Definition.Role == StudentRole.Debuff)
            {
                run.Battle.Damage *= 1.2f; run.NotifyDamage();
                yield return Step(run.Battle, "damage", "독설가 · 약화 역할군 액션 ×1.2");
            }
            if (run.Atmosphere >= 100 && MoodTarget(run, actor))
            {
                AddDamage(run, run.Battle.Damage * .5f);
                yield return Step(run.Battle, "damage", "분위기 · " + actor.Definition.Name + " 액션 시 현재 데미지 +50%p");
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
        public static bool KeepAllyConsumables(PrototypeRun run) => Count(run, "귀염둥이") >= 7 && Count(run, "귀염둥이") < 9;
        public static int RelicApplications(PrototypeRun run, RelicDefinition relic) =>
            Count(run, "귀염둥이") >= 9 || Count(run, "귀염둥이") >= 5 && relic.DebuffRelated ? 2 : 1;

        public static IEnumerable<BattleStep> LeaderBuff(PrototypeRun run, StudentState actor)
        {
            if (actor.Definition.Personality != "리더") yield break;
            AddDamage(run, run.Battle.Damage * .3f);
            yield return Step(run.Battle, "damage", "리더의 버프 적용 · 현재 데미지 +30%");
        }

        public static IEnumerable<BattleStep> FinalizeDamage(PrototypeRun run)
        {
            int sports = Count(run, "운동부");
            float flat = sports >= 6 ? 500000 : sports >= 5 ? 1600 : sports >= 4 ? 800 : sports >= 3 ? 400 : sports >= 2 ? 200 : 0;
            float factor = sports >= 5 ? 10 : sports >= 4 ? 2 : 1;
            if (flat > 0)
            {
                AddDamage(run, flat);
                yield return Step(run.Battle, "damage", "운동부 " + sports + " · 데미지 +" + flat);
                if (factor > 1)
                {
                    run.Battle.Damage *= factor; run.NotifyDamage();
                    yield return Step(run.Battle, "damage", "운동부 · 데미지 ×" + factor);
                }
            }
            int rebels = Count(run, "반항아");
            if (rebels >= 2)
            {
                float multiplier = rebels >= 5 ? .1f : .7f;
                run.Battle.Damage *= multiplier; run.NotifyDamage();
                yield return Step(run.Battle, "damage", "반항아 · 데미지 ×" + multiplier);
            }
            if (run.Atmosphere > 0)
            {
                AddDamage(run, run.Battle.Damage * run.Atmosphere / 100f);
                yield return Step(run.Battle, "damage", "누적 분위기 · 데미지 +" + run.Atmosphere + "%p");
            }
            if (run.Battle.LoverBonusPercent > 0)
            {
                AddDamage(run, run.Battle.Damage * run.Battle.LoverBonusPercent / 100f);
                yield return Step(run.Battle, "damage", "전투 시작의 인접 사랑꾼 · 데미지 +" + run.Battle.LoverBonusPercent + "%");
            }
        }

        public static int Income(PrototypeRun run, int amount)
        {
            int discipline = Count(run, "선도부");
            double multiplier = discipline >= 6 ? 1 : discipline >= 3 ? 2 : discipline >= 2 ? 1.5 : 1;
            return checked((int)Math.Floor(Math.Max(0, amount) * multiplier));
        }

        public static void FinishBattle(PrototypeRun run)
        {
            if (run.Battle == null || !run.Battle.Won && !run.Battle.Lost || run.Battle.SynergyRewardsApplied) return;
            run.Battle.SynergyRewardsApplied = true;
            int discipline = Count(run, "선도부");
            if (discipline >= 6) run.AddMoney(run.Money);
            else if (discipline >= 4) run.AddMoney(100);
        }
    }
}
