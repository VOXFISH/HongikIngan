using System;
using System.Collections.Generic;
using System.Linq;
using static HongikIngan.CombatEngine;

namespace HongikIngan
{
    /// <summary>Authored art/astronomy skills; scheduling remains with BattleActionResolver.</summary>
    internal static class ExpansionActions
    {
        internal static int ArtCount(StudentState actor) => actor.Keywords.FirstOrDefault(k => k.Definition.Id == KeywordRules.Art.Id)?.Stacks ?? 0;
        internal static bool UsesArt(ActionKind kind) => kind == ActionKind.StickyPaint || kind == ActionKind.ArtBrush ||
            kind == ActionKind.MoonRabbit || kind == ActionKind.ArtEncore || kind == ActionKind.Eros;
        internal static bool IsAttack(ActionKind kind) => kind == ActionKind.ArtBrush || kind == ActionKind.ArtEncore ||
            kind == ActionKind.MarsJealousy || kind == ActionKind.StarGift || kind == ActionKind.GrowingStrike || kind == ActionKind.KeywordKindsDamage;
        internal static int TakeArt(PrototypeRun run, StudentState actor)
        {
            var state = actor.Keywords.FirstOrDefault(k => k.Definition.Id == KeywordRules.Art.Id);
            int count = state?.Stacks ?? 0;
            if (count > 0) KeywordRules.Apply(actor.Keywords, state, BattleSynergies.KeepAllyConsumables(run));
            return count;
        }
        internal static float ArtDamageFactor(PrototypeRun run, StudentState actor)
        {
            if (actor.Definition.Club != "미술부") return 1;
            double factor = 1;
            foreach (var keyword in run.Battle.BossKeywords)
                if (keyword.Definition.Id == KeywordRules.Sticky.Id || keyword.Definition.Id == KeywordRules.StickyPersistent.Id)
                    factor *= Math.Pow(1.3, keyword.StackCount);
            return (float)factor;
        }
        internal static IEnumerable<StudentState> Neighbors(PrototypeRun run, StudentState actor)
        {
            int index = run.Students.IndexOf(actor);
            if (index > 0) yield return run.Students[index - 1];
            if (index >= 0 && index + 1 < run.Students.Count) yield return run.Students[index + 1];
        }
        internal static IEnumerable<BattleStep> Resolve(PrototypeRun run, StudentState actor, bool repeated, int art,
            float factor, int buffKinds, int remainingReplays, Action<int> replay, Action<StudentState> scheduleClone)
        {
            var battle = run.Battle;
            switch (actor.Definition.Action)
            {
                case ActionKind.StickyPaint:
                    var paint = actor.UpgradeCount > 0 ? KeywordRules.StickyPersistent : KeywordRules.Sticky;
                    KeywordRules.Add(battle.BossKeywords, paint, 1.3f, art * BattleSynergies.DebuffApplications(run));
                    yield return Step(battle, "keyword", paint.Name + " " + art + "스택", actor);
                    break;
                case ActionKind.ArtBrush:
                    foreach (var ally in run.Students) KeywordRules.Add(ally.Keywords, KeywordRules.Art, 1, (int)actor.SkillValue);
                    BattleActionResolver.AddAttackDamage(run, art * 300f, factor);
                    yield return Step(battle, "keyword", "모든 아군 미감 +" + actor.SkillValue + ", 자신의 미감 " + art + " x300 피해", actor);
                    foreach (var step in BattleSynergies.LeaderBuff(run, actor)) yield return step;
                    break;
                case ActionKind.MoonRabbit:
                    if (art > 0) actor.LunarRoll = Math.Max(actor.LunarRoll, run.DrawCombatInt(1, (int)actor.SkillValue + 1));
                    KeywordRules.Add(battle.BossKeywords, KeywordRules.BlackClover, 1.2f, actor.LunarRoll * BattleSynergies.DebuffApplications(run));
                    yield return Step(battle, "keyword", "달토끼 - 검은 클로버 " + actor.LunarRoll + "스택", actor);
                    break;
                case ActionKind.ArtEncore:
                    BattleActionResolver.AddAttackDamage(run, battle.Damage * actor.SkillValue / 100f, factor);
                    if (repeated) foreach (var ally in Neighbors(run, actor)) KeywordRules.Add(ally.Keywords, KeywordRules.Art, 1, 2);
                    int remaining = remainingReplays < 0 ? art : remainingReplays;
                    if (remaining > 0) replay(remaining - 1);
                    yield return Step(battle, "damage", "가영가영 +" + actor.SkillValue + "%p" + (repeated ? " - 양옆 미감 +2" : ""), actor);
                    break;
                case ActionKind.WildBrush:
                    if (!repeated) break;
                    var targets = Neighbors(run, actor).Concat(new[] { actor }).ToArray();
                    foreach (var target in targets) KeywordRules.Add(target.Keywords, KeywordRules.Art, 1, 5);
                    yield return Step(battle, "keyword", "광란의 붓질 - 자신과 양옆 미감 +5", actor);
                    foreach (var step in BattleSynergies.LeaderBuff(run, actor)) yield return step;
                    if (targets.Any(s => ArtCount(s) >= actor.SkillValue))
                    {
                        var clone = run.CloneStudent(actor, actor, false);
                        if (clone != null) { scheduleClone(clone); yield return Step(battle, "clone", "광란의 붓질 - 자신 복제", actor, clone); }
                    }
                    break;
                case ActionKind.Eros:
                    if (run.RollCombatChance((actor.SkillValue + art) / 100f))
                    {
                        battle.BossHp = 1;
                        yield return Step(battle, "damage", "에로스 - 보스 HP 1", actor);
                    }
                    else
                    {
                        foreach (var ally in run.Students) KeywordRules.Add(ally.Keywords, KeywordRules.Art, 1, 3);
                        yield return Step(battle, "keyword", "에로스 실패 - 모든 아군 미감 +3", actor);
                        foreach (var step in BattleSynergies.LeaderBuff(run, actor)) yield return step;
                    }
                    break;
                case ActionKind.StarLove:
                    int index = run.Students.IndexOf(actor);
                    for (int i = 1; i <= (int)actor.SkillValue && i < run.Students.Count; i++)
                        KeywordRules.Add(run.Students[(index + i) % run.Students.Count].Keywords, KeywordRules.Love, 1.1f, run.StarPower);
                    yield return Step(battle, "keyword", "하늘의 별만큼 사랑해 - 사랑 " + run.StarPower, actor);
                    if (run.StarPower > 0) foreach (var step in BattleSynergies.LeaderBuff(run, actor)) yield return step;
                    break;
                case ActionKind.SaturnCurse:
                    float percentage = Math.Min(actor.SkillValue, run.StarPower);
                    battle.BossHp *= 1 - percentage / 100f;
                    yield return Step(battle, "damage", "토성의 저주 - 현재 보스 HP -" + percentage + "%", actor);
                    break;
                case ActionKind.MarsJealousy:
                    BattleActionResolver.ReplaceAttackDamage(run, battle.Damage * 5, factor);
                    yield return Step(battle, "damage", "화성의 질투 - 데미지 x5", actor);
                    break;
                case ActionKind.TomorrowSun:
                    // The persistent buff also applies to forced attacks, handled by the scheduler.
                    break;
                case ActionKind.StarGift:
                    run.StarPower++;
                    BattleActionResolver.AddAttackDamage(run, actor.SkillValue, factor);
                    yield return Step(battle, "damage", "별의 힘 +1 - 데미지 +" + actor.SkillValue, actor);
                    foreach (var step in BattleSynergies.LeaderBuff(run, actor)) yield return step;
                    break;
                case ActionKind.Coffee:
                    if (run.StarPower <= 0) break;
                    var coffee = battle.BossKeywords.FirstOrDefault(k => k.Definition.Id == KeywordRules.Coffee.Id);
                    if (coffee == null)
                    {
                        coffee = new KeywordState(KeywordRules.Coffee, 20); coffee.Values.Clear(); battle.BossKeywords.Add(coffee);
                    }
                    coffee.AdditionalStacks += (double)run.StarPower * actor.SkillValue * BattleSynergies.DebuffApplications(run);
                    yield return Step(battle, "keyword", "별의 커피 - 누적 " + coffee.StackCount.ToString("0") + "스택", actor);
                    break;
                case ActionKind.GrowingStrike:
                    BattleActionResolver.AddAttackDamage(run, actor.SkillValue, factor);
                    yield return Step(battle, "damage", "세아isHere - 데미지 +" + PrototypeCatalog.FormatDamage(actor.SkillValue), actor);
                    break;
                case ActionKind.KeywordKindsDamage:
                    BattleActionResolver.ReplaceAttackDamage(run, buffKinds == 0 ? 0 : battle.Damage * buffKinds * actor.SkillValue, factor);
                    yield return Step(battle, "damage", "스파르타!! - 서로 다른 버프 " + buffKinds + "종류", actor);
                    break;
            }
        }
    }
}
