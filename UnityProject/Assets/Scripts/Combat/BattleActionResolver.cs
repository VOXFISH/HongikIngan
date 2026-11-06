using System;
using System.Collections.Generic;
using System.Linq;
using static HongikIngan.CombatEngine;

namespace HongikIngan
{
    /// <summary>Schedules actions, movement reactions and movement continuations in their actual order.</summary>
    internal static class BattleActionResolver
    {
        enum CommandKind { Action, Move, Moved }
        sealed class Command
        {
            public readonly StudentState Actor;
            public readonly CommandKind Kind;
            public readonly int Remaining;
            public readonly int Direction;
            public readonly float? ForcedDamage;
            public readonly bool RebelRepeat;
            public readonly bool LightingRepeat, MovementChain;
            public readonly bool IsRepeat;
            public readonly int ArtReplayRemaining;
            public Command(StudentState actor, CommandKind kind = CommandKind.Action, int remaining = 0,
                int direction = -1, float? forcedDamage = null, bool rebelRepeat = false, bool lightingRepeat = false, bool movementChain = false,
                bool isRepeat = true, int artReplayRemaining = -1)
            { Actor = actor; Kind = kind; Remaining = remaining; Direction = direction; ForcedDamage = forcedDamage; RebelRepeat = rebelRepeat; LightingRepeat = lightingRepeat; MovementChain = movementChain; IsRepeat = isRepeat; ArtReplayRemaining = artReplayRemaining; }
        }
        static readonly IStudentTargetStrategy NextAlly = new NextAllyTarget();
        static readonly IStudentTargetStrategy OtherAllies = new OtherAlliesTarget();
        public static IEnumerable<BattleStep> Resolve(PrototypeRun run)
        {
                var battle = run.Battle;
                var queue = new LinkedList<Command>(run.Students.Select(s => new Command(s, isRepeat: false)));
                var movementStarted = new HashSet<StudentState>();
                while (queue.Count > 0 && battle.BossHp > 0)
                {
                    var command = queue.First.Value; queue.RemoveFirst();
                    var actor = command.Actor;
                    if (!run.Students.Contains(actor)) continue;
                    if (command.Kind != CommandKind.Action)
                    {
                        foreach (var step in ResolveCommand(run, command, queue)) yield return step;
                        continue;
                    }
                    yield return Step(battle, "action", actor.Definition.Name + " - " + actor.Definition.SkillDescription, actor);
                    // Count single-stack buffs before a consumable such as love is spent.
                    int singleBuffs = actor.Keywords.Count(k => k.Stacks > 0 && (actor.UpgradeCount > 0 || k.Stacks == 1 || !k.Definition.Stackable));
                    int teamBuffs = run.Students.SelectMany(s => s.Keywords).Where(k => k.Stacks > 0).Select(k => k.Definition.Id).Distinct().Count();
                    int art = !command.ForcedDamage.HasValue && ExpansionActions.UsesArt(actor.Definition.Action) ? ExpansionActions.TakeArt(run, actor) : 0;
                    bool attacks = command.ForcedDamage.HasValue || IsAttack(actor.Definition.Action) &&
                        (actor.Definition.Action != ActionKind.BulletStrike || actor.Keywords.Any(k => k.Definition.Id == KeywordRules.Bullet.Id && k.Stacks > 0)) &&
                        (actor.Definition.Action != ActionKind.TomorrowSun || actor.Keywords.Any(k => k.Definition.Id == KeywordRules.Sun.Id));
                    float attackFactor = attacks ? TakeLove(run, actor) * ExpansionActions.ArtDamageFactor(run, actor) : 1;

                    foreach (var keyword in actor.Keywords.ToArray())
                    {
                        if (keyword.Definition.Id == KeywordRules.Lighting.Id || keyword.Definition.Id == KeywordRules.Love.Id ||
                            keyword.Definition.Id == KeywordRules.Bullet.Id || keyword.Definition.Id == KeywordRules.Art.Id ||
                            keyword.Definition.Id == KeywordRules.Sun.Id) continue;
                        float bonus = KeywordRules.Apply(actor.Keywords, keyword, BattleSynergies.KeepAllyConsumables(run));
                        AddDamage(run, bonus);
                        yield return Step(battle, "damage", actor.Definition.Name + "의 " + keyword.Definition.Name + " +" + PrototypeCatalog.FormatDamage(bonus), actor, sourceName: keyword.Definition.Name);
                    }

                    if (command.ForcedDamage.HasValue)
                    {
                        AddAttackDamage(run, command.ForcedDamage.Value, attackFactor);
                        yield return Step(battle, "damage", actor.Definition.Name + " - 잔나비 추가 공격 +" + command.ForcedDamage.Value, actor, sourceName: "잔나비");
                    }
                    else switch (actor.Definition.Action)
                    {
                        case ActionKind.SingleStackBuffDamage:
                            ReplaceAttackDamage(run, battle.Damage == 0 ? 0 : (float)(battle.Damage * Math.Pow(actor.SkillValue, singleBuffs)), attackFactor, Multiply(Math.Pow(actor.SkillValue, singleBuffs)));
                            yield return Step(battle, "damage", "하나밖에 없는 태양 - 버프 " + singleBuffs + "종류", actor);
                            break;
                        case ActionKind.CloverStrike:
                            KeywordRules.Add(battle.BossKeywords, KeywordRules.BlackClover, 1.2f, BattleSynergies.DebuffApplications(run));
                            yield return Step(battle, "keyword", "무대의 꽃 - 검은 클로버 부여", actor);
                            AddAttackDamage(run, actor.SkillValue, attackFactor);
                            yield return Step(battle, "damage", "무대의 꽃 - 데미지 +" + actor.SkillValue, actor);
                            break;
                        case ActionKind.BulletStrike:
                            var bullet = actor.Keywords.FirstOrDefault(k => k.Definition.Id == KeywordRules.Bullet.Id && k.Stacks > 0);
                            if (bullet != null)
                            {
                                KeywordRules.Apply(actor.Keywords, bullet, BattleSynergies.KeepAllyConsumables(run));
                                AddAttackDamage(run, 100000, attackFactor);
                                yield return Step(battle, "damage", "총알 발사 - 데미지 +100000", actor);
                            }
                            break;
                        case ActionKind.LoveAndClone:
                            foreach (var ally in run.Students) KeywordRules.Add(ally.Keywords, KeywordRules.Love, 1.1f, 3);
                            yield return Step(battle, "keyword", "돌돌마리 - 모든 아군에게 사랑 3스택", actor);
                            foreach (var step in BattleSynergies.LeaderBuff(run, actor)) yield return step;
                            int theatre = run.Students.Count(s => s.Definition.Club == "연극부");
                            if (theatre == 3 || actor.UpgradeCount > 0 && theatre == 2)
                            {
                                var selfClone = run.CloneStudent(actor, actor, false);
                                if (selfClone != null)
                                {
                                    queue.AddFirst(new Command(selfClone, lightingRepeat: command.LightingRepeat, movementChain: command.MovementChain, isRepeat: false));
                                    yield return Step(battle, "clone", "돌돌마리 - 분신 생성", actor, selfClone);
                                }
                            }
                            break;
                        case ActionKind.FourthBatter:
                            float hit = actor.SkillValue * (run.Students.IndexOf(actor) == 3 ? 3 : 1);
                            AddAttackDamage(run, hit, attackFactor);
                            yield return Step(battle, "damage", actor.Definition.Name + " - 4번 타자 +" + hit, actor);
                            break;
                        case ActionKind.Advance:
                        case ActionKind.AdvanceToFront:
                            if (!command.MovementChain && movementStarted.Add(actor))
                                queue.AddFirst(new Command(actor, CommandKind.Move, actor.Definition.Action == ActionKind.Advance ? (int)actor.SkillValue : int.MaxValue, lightingRepeat: command.LightingRepeat, movementChain: command.MovementChain));
                            break;
                        case ActionKind.StrikeOnMove:
                            // This skill only reacts to its owner changing position.
                            break;
                        case ActionKind.Spite:
                            int kinds = battle.BossKeywords.Count(k => k.Stacks > 0);
                            int spiteStacks = kinds * (int)actor.SkillValue * BattleSynergies.DebuffApplications(run);
                            KeywordRules.Add(battle.BossKeywords, KeywordRules.Spite, 10, spiteStacks);
                            yield return Step(battle, "keyword", "보스 키워드 " + kinds + "종류 - 표독 " + spiteStacks + "스택 부여", actor);
                            break;
                        case ActionKind.LuckyClover:
                            bool success = run.RollCombatChance(actor.SkillValue / 100f);
                            if (success) KeywordRules.Add(battle.BossKeywords, KeywordRules.BlackClover, 1.2f, BattleSynergies.DebuffApplications(run));
                            yield return Step(battle, "keyword", success ? "검은 클로버 1스택 부여" : "검은 클로버 부여 실패", actor);
                            break;
                        case ActionKind.PowerDamage:
                            ReplaceAttackDamage(run, (float)Math.Pow(battle.Damage, actor.SkillValue), attackFactor, "^" + Operand(actor.SkillValue));
                            yield return Step(battle, "damage", "현재 데미지 ^" + actor.SkillValue, actor);
                            break;
                        case ActionKind.RebelPercent:
                            int rebels = run.Students.Count(s => s.Definition.Personality == "반항아");
                            if (rebels > 0) AddAttackDamage(run, battle.Damage * actor.SkillValue * rebels / 100f, attackFactor, actor.SkillValue * rebels);
                            yield return Step(battle, "damage", "반항아 " + rebels + "명 - 현재 데미지 +" + actor.SkillValue * rebels + "%p", actor);
                            if (rebels > 0) foreach (var step in BattleSynergies.LeaderBuff(run, actor)) yield return step;
                            break;
                        case ActionKind.AddDamage:
                            AddAttackDamage(run, actor.SkillValue, attackFactor);
                            yield return Step(battle, "damage", "데미지 +" + PrototypeCatalog.FormatDamage(actor.SkillValue), actor);
                            break;
                        case ActionKind.TeamSizeDamage:
                            float teamDamage = run.Students.Count * actor.SkillValue;
                            AddAttackDamage(run, teamDamage, attackFactor);
                            yield return Step(battle, "damage", "전대 " + run.Students.Count + "명 - 데미지 +" + PrototypeCatalog.FormatDamage(teamDamage), actor);
                            break;
                        case ActionKind.AddThenPercentDamage:
                            if (actor.SkillValue != 0)
                            {
                                AddAttackDamage(run, actor.SkillValue, attackFactor);
                                yield return Step(battle, "damage", "데미지 +" + PrototypeCatalog.FormatDamage(actor.SkillValue), actor);
                            }
                            float percent = actor.Definition.PercentAtUpgrade(actor.UpgradeCount);
                            AddAttackDamage(run, battle.Damage * percent / 100f, attackFactor, percent);
                            yield return Step(battle, "damage", "현재 데미지의 " + percent + "% 추가", actor);
                            break;
                        case ActionKind.MultiplyDamage:
                            ReplaceAttackDamage(run, battle.Damage * actor.SkillValue, attackFactor, Multiply(actor.SkillValue));
                            yield return Step(battle, "damage", "데미지 ×" + actor.SkillValue.ToString("0.00"), actor);
                            break;
                        case ActionKind.EmpowerNext:
                            foreach (var target in NextAlly.SelectTargets(actor, run.Students))
                            {
                                KeywordRules.Add(target.Keywords, KeywordRules.Starlight, actor.SkillValue);
                                yield return Step(battle, "keyword", target.Definition.Name + "에게 별빛 1스택", actor);
                            }
                            break;
                        case ActionKind.WeakenBoss:
                            KeywordRules.Add(battle.BossKeywords, KeywordRules.Exposed, actor.SkillValue, BattleSynergies.DebuffApplications(run));
                            yield return Step(battle, "keyword", "보스 받는 피해 +" + actor.SkillValue + "%p", actor);
                            break;
                        case ActionKind.EarnMoney:
                            run.AddMoney((int)Math.Min(int.MaxValue, Math.Truncate(actor.SkillValue)));
                            yield return Step(battle, "income", "미라클 에너지 +" + PrototypeCatalog.FormatDamage(actor.SkillValue), actor);
                            break;
                        case ActionKind.Paparazzi:
                            KeywordRules.Add(battle.BossKeywords, KeywordRules.Mine, actor.SkillValue);
                            yield return Step(battle, "keyword", "보스에게 넌 내 꺼야 부여 - 전투 종료까지 유지", actor);
                            break;
                        case ActionKind.Spotlight:
                            AddAttackDamage(run, battle.Damage * actor.SkillValue / 100f, attackFactor, actor.SkillValue);
                            foreach (var neighbor in Neighbors(run, actor))
                                KeywordRules.Add(neighbor.Keywords, KeywordRules.Lighting, 0);
                            yield return Step(battle, "keyword", "현재 데미지 +" + actor.SkillValue + "% - 양옆 아군에게 조명", actor);
                            break;
                        case ActionKind.LightingDamage:
                            int lights = run.Students.Count(s => HasLighting(s));
                            AddAttackDamage(run, 500, attackFactor);
                            yield return Step(battle, "damage", "Lights, Camera, Action! - 데미지 +500", actor);
                            for (int i = 0; i < lights; i++)
                            {
                                AddAttackDamage(run, actor.SkillValue, attackFactor);
                                yield return Step(battle, "damage", "조명 " + (i + 1) + "/" + lights + " - 데미지 +" + actor.SkillValue, actor);
                            }
                            break;
                        case ActionKind.BestSubject:
                            int cute = run.Students.Count(s => s.Definition.Personality == "귀염둥이");
                            int freshmen = run.Students.Count(s => s.Definition.Grade == 1);
                            int photographers = run.Students.Count(s => s.Definition.Club == "사진부");
                            int factor = actor.UpgradeCount > 0 ? cute * freshmen * photographers : cute + freshmen + photographers;
                            ReplaceAttackDamage(run, factor == 0 ? 0 : battle.Damage * factor, attackFactor, Multiply(factor));
                            yield return Step(battle, "damage", "최고의 피사체 - 데미지 ×" + factor, actor);
                            break;
                        case ActionKind.GrantLove:
                            foreach (var ally in run.Students)
                                KeywordRules.Add(ally.Keywords, KeywordRules.Love, 1.1f, (int)actor.SkillValue);
                            yield return Step(battle, "keyword", "모든 아군에게 사랑 " + (int)actor.SkillValue + "스택 - 다음 공격 증가량에 스택당 ×1.1", actor);
                            foreach (var step in BattleSynergies.LeaderBuff(run, actor)) yield return step;
                            break;
                        case ActionKind.CloneNext:
                            var cloneTarget = NextAlly.SelectTargets(actor, run.Students).FirstOrDefault();
                            var clone = run.CloneNext(actor);
                            if (clone == null)
                            {
                                yield return Step(battle, "action", "복제 불가 - 대상 없음, 9명 도달 또는 분신", actor);
                                break;
                            }
                            var pending = queue.First;
                            while (pending != null && (pending.Value.Kind != CommandKind.Action || pending.Value.Actor != cloneTarget)) pending = pending.Next;
                            var copiedAction = new Command(clone, lightingRepeat: command.LightingRepeat, movementChain: command.MovementChain, isRepeat: false);
                            if (pending != null) queue.AddAfter(pending, copiedAction); else queue.AddFirst(copiedAction);
                            AddDamage(run, 3000f * actor.PrecisionCopies);
                            yield return Step(battle, "clone", cloneTarget.Definition.Name + " 분신 생성 - 정밀화 +" + (3000f * actor.PrecisionCopies), actor,
                                sourceName: actor.PrecisionCopies > 0 ? "정밀화" : actor.Definition.SkillName,
                                operation: actor.PrecisionCopies > 0 ? null : "");
                            break;
                        case ActionKind.SwapNext:
                            var other = NextAlly.SelectTargets(actor, run.Students).FirstOrDefault();
                            if (other != null && !command.MovementChain)
                            {
                                int a = run.Students.IndexOf(actor), b = run.Students.IndexOf(other);
                                run.Students[a] = other; run.Students[b] = actor;
                                // Swapped pair acts again in NEW ascending slot order, including prior actors.
                                queue.AddFirst(new Command(run.Students[Math.Max(a, b)], lightingRepeat: command.LightingRepeat, movementChain: true));
                                queue.AddFirst(new Command(run.Students[Math.Min(a, b)], lightingRepeat: command.LightingRepeat, movementChain: true));
                                yield return Step(battle, "move", "자리 교환 후 작은 번호부터 재행동", actor, other);
                            }
                            break;
                        default:
                            foreach (var step in ExpansionActions.Resolve(run, actor, command.IsRepeat, art, attackFactor, teamBuffs,
                                command.ArtReplayRemaining,
                                remaining => queue.AddFirst(new Command(actor, lightingRepeat: command.LightingRepeat, movementChain: command.MovementChain, artReplayRemaining: remaining)),
                                created => queue.AddFirst(new Command(created, lightingRepeat: command.LightingRepeat, movementChain: command.MovementChain, isRepeat: false))))
                                yield return step;
                            break;
                    }

                    if (actor.Definition.Action == ActionKind.TomorrowSun && actor.Keywords.Any(k => k.Definition.Id == KeywordRules.Sun.Id))
                    {
                        ReplaceAttackDamage(run, (float)Math.Pow(battle.Damage, actor.SkillValue), attackFactor, "^" + Operand(actor.SkillValue));
                        yield return Step(battle, "damage", "내일의 태양 - 데미지 ^" + actor.SkillValue, actor);
                    }
                    if (attacks)
                        foreach (var step in AfterAttack(run, command, queue)) yield return step;
                    foreach (var step in BattleSynergies.AfterAction(run, actor)) yield return step;
                    if (actor.Definition.Club == "미술부" && run.RollCombatChance(BattleSynergies.ArtReplayChance(run)))
                    {
                        queue.AddFirst(new Command(actor, lightingRepeat: command.LightingRepeat, movementChain: command.MovementChain));
                        yield return Step(battle, "synergy", "미술부 - 자신 재행동", actor, sourceName: "미술부");
                    }
                    if (BattleSynergies.TakeMoodReplay(run, actor))
                        queue.AddFirst(new Command(actor, forcedDamage: command.ForcedDamage, lightingRepeat: command.LightingRepeat, movementChain: command.MovementChain));
                    if (!command.RebelRepeat && BattleSynergies.DoubleRebelAction(run, actor))
                        queue.AddFirst(new Command(actor, forcedDamage: command.ForcedDamage, rebelRepeat: true, lightingRepeat: command.LightingRepeat, movementChain: command.MovementChain));
                    if (!command.MovementChain && BattleSynergies.MoveRebelBack(run, actor))
                        queue.AddFirst(new Command(actor, CommandKind.Move, 1, 1, lightingRepeat: command.LightingRepeat, movementChain: command.MovementChain));
                    if (!command.LightingRepeat && HasLighting(actor))
                    {
                        var neighbors = Neighbors(run, actor).ToArray();
                        for (int i = neighbors.Length - 1; i >= 0; i--)
                            queue.AddFirst(new Command(neighbors[i], lightingRepeat: true, movementChain: command.MovementChain));
                        yield return Step(battle, "keyword", actor.Definition.Name + " 조명 - 양옆 재행동", actor, sourceName: "조명");
                    }

                    // Reaction actions emit the same event; there is deliberately no arbitrary combo cap.
                    // One yield per action allows the presentation loop to remain responsive.
                    var reactions = OtherAllies.SelectTargets(actor, run.Students).Where(s => s.Definition.Reaction).ToArray();
                    for (int i = reactions.Length - 1; i >= 0; i--) queue.AddFirst(new Command(reactions[i], lightingRepeat: command.LightingRepeat, movementChain: command.MovementChain));
                }

        }

        static IEnumerable<BattleStep> ResolveCommand(PrototypeRun run, Command command, LinkedList<Command> queue)
        {
            var actor = command.Actor;
            var battle = run.Battle;
            if (command.Kind == CommandKind.Move)
            {
                int index = run.Students.IndexOf(actor);
                int next = index + command.Direction;
                if (index < 0 || next < 0 || next >= run.Students.Count || command.Remaining <= 0) yield break;
                var other = run.Students[next];
                run.Students[next] = actor;
                run.Students[index] = other;
                // All movement hooks and pair re-actions finish before the next step forward.
                queue.AddFirst(new Command(actor, CommandKind.Move, command.Remaining - 1, command.Direction, lightingRepeat: command.LightingRepeat, movementChain: command.MovementChain));
                var left = run.Students[Math.Min(index, next)];
                var right = run.Students[Math.Max(index, next)];
                queue.AddFirst(new Command(right, lightingRepeat: command.LightingRepeat, movementChain: true));
                queue.AddFirst(new Command(left, lightingRepeat: command.LightingRepeat, movementChain: true));
                queue.AddFirst(new Command(right, CommandKind.Moved, lightingRepeat: command.LightingRepeat, movementChain: true));
                queue.AddFirst(new Command(left, CommandKind.Moved, lightingRepeat: command.LightingRepeat, movementChain: true));
                yield return Step(battle, "move", actor.Definition.Name + " → " + (next + 1) + "번, " + other.Definition.Name + " → " + (index + 1) + "번", actor, other);
            }
            else if (command.Kind == CommandKind.Moved)
            {
                if (actor.Definition.Action == ActionKind.Advance)
                {
                    AddAttackDamage(run, 300, TakeLove(run, actor));
                    yield return Step(battle, "damage", actor.Definition.Name + " 이동 - 데미지 +300", actor);
                    foreach (var step in AfterAttack(run, command, queue)) yield return step;
                }
                else if (actor.Definition.Action == ActionKind.AdvanceToFront)
                {
                    AddAttackDamage(run, battle.Damage * actor.SkillValue / 100f, TakeLove(run, actor), actor.SkillValue);
                    yield return Step(battle, "damage", actor.Definition.Name + " 이동 - 현재 데미지 +" + actor.SkillValue + "%p", actor);
                    foreach (var step in BattleSynergies.LeaderBuff(run, actor)) yield return step;
                    foreach (var step in AfterAttack(run, command, queue)) yield return step;
                }
                else if (actor.Definition.Action == ActionKind.StrikeOnMove)
                {
                    for (int i = run.Students.Count - 1; i >= 0; i--)
                        queue.AddFirst(new Command(run.Students[i], forcedDamage: actor.SkillValue, lightingRepeat: command.LightingRepeat, movementChain: command.MovementChain));
                    yield return Step(battle, "action", actor.Definition.Name + " 이동 - 자신을 포함한 모든 아군 추가 공격", actor);
                }
            }
        }

        static bool IsAttack(ActionKind action)
        {
            return action == ActionKind.FourthBatter || action == ActionKind.AddDamage ||
                action == ActionKind.TeamSizeDamage || action == ActionKind.AddThenPercentDamage ||
                action == ActionKind.PowerDamage || action == ActionKind.MultiplyDamage ||
                action == ActionKind.RebelPercent || action == ActionKind.Spotlight ||
                action == ActionKind.LightingDamage || action == ActionKind.BestSubject ||
                action == ActionKind.SingleStackBuffDamage || action == ActionKind.CloverStrike || action == ActionKind.BulletStrike ||
                action == ActionKind.TomorrowSun || ExpansionActions.IsAttack(action);
        }

        static float TakeLove(PrototypeRun run, StudentState actor)
        {
            var love = actor.Keywords.FirstOrDefault(k => k.Definition.Id == KeywordRules.Love.Id);
            if (love == null || love.Stacks == 0) return 1;
            double factor = 1;
            foreach (float value in love.Values) factor *= value;
            int stacks = love.Stacks;
            if (!BattleSynergies.KeepAllyConsumables(run)) actor.Keywords.Remove(love);
            run.Battle.Log.Add(actor.Definition.Name + " 사랑 " + stacks + "스택 - 이번 공격 증가량 ×" + factor.ToString("0.###"));
            return (float)factor;
        }

        internal static void AddAttackDamage(PrototypeRun run, float amount, float factor, float? percent = null)
        {
            // A zero-valued attack remains zero even if an exceptionally large stack count overflows the multiplier.
            AddDamage(run, amount == 0 ? 0 : amount * factor, percent.HasValue ? Multiply(1 + percent.Value * factor / 100f) : null);
        }

        internal static void ReplaceAttackDamage(PrototypeRun run, float replacement, float factor, string operation = null)
        {
            float previous = run.Battle.Damage;
            if (factor != 1 && !float.IsInfinity(replacement) && replacement > previous)
            {
                replacement = previous + (replacement - previous) * factor;
                operation = previous == 0 ? "+" + Operand(replacement) : Multiply(replacement / previous);
            }
            RecordDamageOperation(run.Battle, operation ?? (previous == 0 ? "+" + Operand(replacement) : Multiply(replacement / previous)));
            run.Battle.Damage = replacement; run.NotifyDamage();
        }

        static IEnumerable<BattleStep> AfterAttack(PrototypeRun run, Command command, LinkedList<Command> queue)
        {
            var mark = run.Battle.BossKeywords.FirstOrDefault(k => k.Definition.Id == KeywordRules.Mine.Id);
            if (mark == null || mark.Stacks == 0) yield break;
            AddDamage(run, 500);
            float percent = mark.Values[0];
            if (percent != 0) AddDamage(run, run.Battle.Damage * percent / 100f, Multiply(1 + percent / 100f));
            yield return Step(run.Battle, "damage", "넌 내 꺼야 - 추가 피해 500" + (percent > 0 ? " 후 현재 데미지 +" + percent + "%p" : ""), sourceName: "넌 내 꺼야");
            if (!command.MovementChain)
                queue.AddFirst(new Command(command.Actor, CommandKind.Move, 1, lightingRepeat: command.LightingRepeat));
        }

        static bool HasLighting(StudentState actor) => actor.Keywords.Any(k => k.Definition.Id == KeywordRules.Lighting.Id && k.Stacks > 0);
        static IEnumerable<StudentState> Neighbors(PrototypeRun run, StudentState actor)
        {
            int index = run.Students.IndexOf(actor);
            if (index > 0) yield return run.Students[index - 1];
            if (index >= 0 && index + 1 < run.Students.Count) yield return run.Students[index + 1];
        }
    }
}
