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
            public Command(StudentState actor, CommandKind kind = CommandKind.Action, int remaining = 0,
                int direction = -1, float? forcedDamage = null, bool rebelRepeat = false)
            { Actor = actor; Kind = kind; Remaining = remaining; Direction = direction; ForcedDamage = forcedDamage; RebelRepeat = rebelRepeat; }
        }
        static readonly IStudentTargetStrategy NextAlly = new NextAllyTarget();
        static readonly IStudentTargetStrategy OtherAllies = new OtherAlliesTarget();
        public static IEnumerable<BattleStep> Resolve(PrototypeRun run)
        {
                var battle = run.Battle;
                var queue = new LinkedList<Command>(run.Students.Select(s => new Command(s)));
                var movementStarted = new HashSet<StudentState>();
                while (queue.Count > 0)
                {
                    var command = queue.First.Value; queue.RemoveFirst();
                    var actor = command.Actor;
                    if (!run.Students.Contains(actor)) continue;
                    if (command.Kind != CommandKind.Action)
                    {
                        foreach (var step in ResolveCommand(run, command, queue)) yield return step;
                        continue;
                    }
                    yield return Step(battle, "action", actor.Definition.Name + " · " + actor.Definition.SkillDescription);

                    foreach (var keyword in actor.Keywords.ToArray())
                    {
                        float bonus = KeywordRules.Apply(actor.Keywords, keyword, BattleSynergies.KeepAllyConsumables(run));
                        AddDamage(run, bonus);
                        yield return Step(battle, "damage", actor.Definition.Name + "의 " + keyword.Definition.Name + " +" + PrototypeCatalog.FormatDamage(bonus));
                    }

                    if (command.ForcedDamage.HasValue)
                    {
                        AddDamage(run, command.ForcedDamage.Value);
                        yield return Step(battle, "damage", actor.Definition.Name + " · 잔나비 추가 공격 +" + command.ForcedDamage.Value);
                    }
                    else switch (actor.Definition.Action)
                    {
                        case ActionKind.FourthBatter:
                            float hit = actor.SkillValue * (run.Students.IndexOf(actor) == 3 ? 3 : 1);
                            AddDamage(run, hit);
                            yield return Step(battle, "damage", actor.Definition.Name + " · 4번 타자 +" + hit);
                            break;
                        case ActionKind.Advance:
                        case ActionKind.AdvanceToFront:
                            if (movementStarted.Add(actor))
                                queue.AddFirst(new Command(actor, CommandKind.Move, actor.Definition.Action == ActionKind.Advance ? (int)actor.SkillValue : int.MaxValue));
                            break;
                        case ActionKind.StrikeOnMove:
                            // This skill only reacts to its owner changing position.
                            break;
                        case ActionKind.Spite:
                            int kinds = battle.BossKeywords.Count(k => k.Stacks > 0);
                            int spiteStacks = kinds * (int)actor.SkillValue * BattleSynergies.DebuffApplications(run);
                            KeywordRules.Add(battle.BossKeywords, KeywordRules.Spite, 10, spiteStacks);
                            yield return Step(battle, "keyword", "보스 키워드 " + kinds + "종류 · 표독 " + spiteStacks + "스택 부여");
                            break;
                        case ActionKind.LuckyClover:
                            bool success = run.RollCombatChance(actor.SkillValue / 100f);
                            if (success) KeywordRules.Add(battle.BossKeywords, KeywordRules.BlackClover, 1.2f, BattleSynergies.DebuffApplications(run));
                            yield return Step(battle, "keyword", success ? "검은 클로버 1스택 부여" : "검은 클로버 부여 실패");
                            break;
                        case ActionKind.PowerDamage:
                            battle.Damage = (float)Math.Pow(battle.Damage, actor.SkillValue);
                            run.NotifyDamage();
                            yield return Step(battle, "damage", "현재 데미지 ^" + actor.SkillValue);
                            break;
                        case ActionKind.RebelPercent:
                            int rebels = run.Students.Count(s => s.Definition.Personality == "반항아");
                            if (rebels > 0) AddDamage(run, battle.Damage * actor.SkillValue * rebels / 100f);
                            yield return Step(battle, "damage", "반항아 " + rebels + "명 · 현재 데미지 +" + actor.SkillValue * rebels + "%p");
                            if (rebels > 0) foreach (var step in BattleSynergies.LeaderBuff(run, actor)) yield return step;
                            break;
                        case ActionKind.AddDamage:
                            AddDamage(run, actor.SkillValue);
                            yield return Step(battle, "damage", "데미지 +" + PrototypeCatalog.FormatDamage(actor.SkillValue));
                            break;
                        case ActionKind.TeamSizeDamage:
                            float teamDamage = run.Students.Count * actor.SkillValue;
                            AddDamage(run, teamDamage);
                            yield return Step(battle, "damage", "전대 " + run.Students.Count + "명 · 데미지 +" + PrototypeCatalog.FormatDamage(teamDamage));
                            break;
                        case ActionKind.AddThenPercentDamage:
                            if (actor.SkillValue != 0)
                            {
                                AddDamage(run, actor.SkillValue);
                                yield return Step(battle, "damage", "데미지 +" + PrototypeCatalog.FormatDamage(actor.SkillValue));
                            }
                            float percent = actor.Definition.PercentAtUpgrade(actor.UpgradeCount);
                            AddDamage(run, battle.Damage * percent / 100f);
                            yield return Step(battle, "damage", "현재 데미지의 " + percent + "% 추가");
                            break;
                        case ActionKind.MultiplyDamage:
                            battle.Damage *= actor.SkillValue; run.NotifyDamage();
                            yield return Step(battle, "damage", "데미지 ×" + actor.SkillValue.ToString("0.00"));
                            break;
                        case ActionKind.EmpowerNext:
                            foreach (var target in NextAlly.SelectTargets(actor, run.Students))
                            {
                                KeywordRules.Add(target.Keywords, KeywordRules.Starlight, actor.SkillValue);
                                yield return Step(battle, "keyword", target.Definition.Name + "에게 별빛 1스택");
                            }
                            break;
                        case ActionKind.WeakenBoss:
                            KeywordRules.Add(battle.BossKeywords, KeywordRules.Exposed, actor.SkillValue, BattleSynergies.DebuffApplications(run));
                            yield return Step(battle, "keyword", "보스 받는 피해 +" + actor.SkillValue + "%p");
                            break;
                        case ActionKind.EarnMoney:
                            run.AddMoney((int)Math.Min(int.MaxValue, Math.Truncate(actor.SkillValue)));
                            yield return Step(battle, "income", "미라클 에너지 +" + PrototypeCatalog.FormatDamage(actor.SkillValue));
                            break;
                        case ActionKind.SwapNext:
                            var other = NextAlly.SelectTargets(actor, run.Students).FirstOrDefault();
                            if (other != null)
                            {
                                int a = run.Students.IndexOf(actor), b = run.Students.IndexOf(other);
                                run.Students[a] = other; run.Students[b] = actor;
                                // Swapped pair acts again in NEW ascending slot order, including prior actors.
                                queue.AddFirst(new Command(run.Students[Math.Max(a, b)]));
                                queue.AddFirst(new Command(run.Students[Math.Min(a, b)]));
                                yield return Step(battle, "move", "자리 교환 후 작은 번호부터 재행동");
                            }
                            break;
                    }

                    foreach (var step in BattleSynergies.AfterAction(run, actor)) yield return step;
                    if (BattleSynergies.TakeMoodReplay(run, actor))
                        queue.AddFirst(new Command(actor, forcedDamage: command.ForcedDamage));
                    if (!command.RebelRepeat && BattleSynergies.DoubleRebelAction(run, actor))
                        queue.AddFirst(new Command(actor, forcedDamage: command.ForcedDamage, rebelRepeat: true));
                    if (BattleSynergies.MoveRebelBack(run, actor))
                        queue.AddFirst(new Command(actor, CommandKind.Move, 1, 1));

                    // Reaction actions emit the same event; there is deliberately no arbitrary combo cap.
                    // One yield per action allows the presentation loop to remain responsive.
                    var reactions = OtherAllies.SelectTargets(actor, run.Students).Where(s => s.Definition.Reaction).ToArray();
                    for (int i = reactions.Length - 1; i >= 0; i--) queue.AddFirst(new Command(reactions[i]));
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
                queue.AddFirst(new Command(actor, CommandKind.Move, command.Remaining - 1, command.Direction));
                var left = run.Students[Math.Min(index, next)];
                var right = run.Students[Math.Max(index, next)];
                queue.AddFirst(new Command(right));
                queue.AddFirst(new Command(left));
                queue.AddFirst(new Command(right, CommandKind.Moved));
                queue.AddFirst(new Command(left, CommandKind.Moved));
                yield return Step(battle, "move", actor.Definition.Name + " → " + (next + 1) + "번, " + other.Definition.Name + " → " + (index + 1) + "번");
            }
            else if (command.Kind == CommandKind.Moved)
            {
                if (actor.Definition.Action == ActionKind.Advance)
                {
                    AddDamage(run, 300);
                    yield return Step(battle, "damage", actor.Definition.Name + " 이동 · 데미지 +300");
                }
                else if (actor.Definition.Action == ActionKind.AdvanceToFront)
                {
                    AddDamage(run, battle.Damage * actor.SkillValue / 100f);
                    yield return Step(battle, "damage", actor.Definition.Name + " 이동 · 현재 데미지 +" + actor.SkillValue + "%p");
                    foreach (var step in BattleSynergies.LeaderBuff(run, actor)) yield return step;
                }
                else if (actor.Definition.Action == ActionKind.StrikeOnMove)
                {
                    for (int i = run.Students.Count - 1; i >= 0; i--)
                        queue.AddFirst(new Command(run.Students[i], forcedDamage: actor.SkillValue));
                    yield return Step(battle, "action", actor.Definition.Name + " 이동 · 자신을 포함한 모든 아군 추가 공격");
                }
            }
        }
    }
}
