using System;
using System.Collections.Generic;
using System.Linq;

namespace HongikIngan
{
    public sealed class KeywordDefinition
    {
        public readonly string Id, Name;
        public readonly bool Consumable, Stackable, Volatile;
        public KeywordDefinition(string id, string name, bool consumable, bool stackable, bool isVolatile)
        {
            Id = id; Name = name; Consumable = consumable; Stackable = stackable; Volatile = isVolatile;
        }
    }

    public sealed class KeywordState
    {
        public readonly KeywordDefinition Definition;
        // Each stack keeps the strength it had when granted; no stack count gameplay limit.
        public readonly List<float> Values = new List<float>();
        public int Stacks => Values.Count;
        public KeywordState(KeywordDefinition definition, float value) { Definition = definition; Values.Add(value); }
    }

    public static class KeywordRules
    {
        public static readonly KeywordDefinition Starlight = new KeywordDefinition("starlight", "별빛", true, true, true);
        public static readonly KeywordDefinition Exposed = new KeywordDefinition("exposed", "노출", false, true, true);

        public static void Add(List<KeywordState> states, KeywordDefinition definition, float value, int stacks = 1)
        {
            if (stacks <= 0) return;
            var current = states.Find(k => k.Definition.Id == definition.Id);
            if (current != null && !definition.Stackable) return;
            int additions = definition.Stackable ? stacks : 1;
            if (current == null)
            {
                current = new KeywordState(definition, value);
                states.Add(current); additions--;
            }
            for (int i = 0; i < additions; i++) current.Values.Add(value);
        }

        public static float Apply(List<KeywordState> states, KeywordState state)
        {
            if (state.Stacks == 0) return 0;
            float amount = state.Definition.Consumable ? state.Values[0] : state.Values.Sum();
            if (state.Definition.Consumable)
            {
                state.Values.RemoveAt(0);
                if (state.Stacks == 0) states.Remove(state);
            }
            return amount;
        }

        public static void EndTurn(List<KeywordState> states) => states.RemoveAll(k => k.Definition.Volatile);
    }

    public interface IStudentTargetStrategy
    {
        IEnumerable<StudentState> SelectTargets(StudentState actor, IReadOnlyList<StudentState> formation);
    }

    public sealed class NextAllyTarget : IStudentTargetStrategy
    {
        public IEnumerable<StudentState> SelectTargets(StudentState actor, IReadOnlyList<StudentState> formation)
        {
            if (formation.Count < 2) yield break;
            for (int i = 0; i < formation.Count; i++)
                if (ReferenceEquals(formation[i], actor)) { yield return formation[(i + 1) % formation.Count]; yield break; }
        }
    }

    public sealed class OtherAlliesTarget : IStudentTargetStrategy
    {
        public IEnumerable<StudentState> SelectTargets(StudentState actor, IReadOnlyList<StudentState> formation)
        {
            foreach (var student in formation) if (!ReferenceEquals(student, actor)) yield return student;
        }
    }

    public sealed class BattleStep
    {
        public readonly string Kind, Message;
        public readonly float Damage;
        public BattleStep(string kind, string message, float damage) { Kind = kind; Message = message; Damage = damage; }
    }

    public sealed class BattleState
    {
        public int Turn { get; internal set; }
        public float BossHp { get; internal set; }
        public float MaxBossHp { get; }
        public float Damage { get; internal set; }
        public bool IsResolving { get; internal set; }
        public bool Won { get; internal set; }
        public bool Lost { get; internal set; }
        public readonly List<string> Log = new List<string>();
        public readonly List<KeywordState> BossKeywords = new List<KeywordState>();
        public BattleState(float hp) { BossHp = hp; MaxBossHp = hp; }
    }

    public static class CombatEngine
    {
        private static readonly IStudentTargetStrategy NextAlly = new NextAllyTarget();
        private static readonly IStudentTargetStrategy OtherAllies = new OtherAlliesTarget();

        public static IEnumerable<BattleStep> ResolveTurn(PrototypeRun run)
        {
            var battle = run.Battle;
            if (battle == null || battle.IsResolving || battle.Won || battle.Lost) yield break;
            battle.IsResolving = true;
            try
            {
                battle.Turn++;
                battle.Damage = 0f;
                run.NotifyDamage();
                yield return Step(battle, "turn", "턴 " + battle.Turn + " · 합동 데미지 0으로 초기화");

                // Relics are processed in acquisition order before character slot order.
                foreach (var relic in run.Relics)
                {
                    if (relic.Kind != RelicKind.OpeningDamage) continue;
                    AddDamage(run, relic.Value);
                    yield return Step(battle, "damage", relic.Name + " +" + PrototypeCatalog.FormatDamage(relic.Value));
                }

                // Temporary content effect: each active club contributes +20 per member.
                foreach (var synergy in run.Students.GroupBy(s => s.Definition.Club).Where(g => g.Count() >= 2).OrderBy(g => g.Key, StringComparer.Ordinal))
                {
                    AddDamage(run, synergy.Count() * 20f);
                    yield return Step(battle, "damage", synergy.Key + " 시너지 +" + synergy.Count() * 20);
                }

                var queue = new LinkedList<StudentState>(run.Students);
                while (queue.Count > 0)
                {
                    var actor = queue.First.Value; queue.RemoveFirst();
                    if (!run.Students.Contains(actor)) continue;
                    yield return Step(battle, "action", actor.Definition.Name + " · " + actor.Definition.SkillDescription);

                    foreach (var keyword in actor.Keywords.ToArray())
                    {
                        float bonus = KeywordRules.Apply(actor.Keywords, keyword);
                        AddDamage(run, bonus);
                        yield return Step(battle, "damage", actor.Definition.Name + "의 " + keyword.Definition.Name + " +" + PrototypeCatalog.FormatDamage(bonus));
                    }

                    switch (actor.Definition.Action)
                    {
                        case ActionKind.AddDamage:
                            AddDamage(run, actor.SkillValue);
                            yield return Step(battle, "damage", "데미지 +" + PrototypeCatalog.FormatDamage(actor.SkillValue));
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
                            KeywordRules.Add(battle.BossKeywords, KeywordRules.Exposed, actor.SkillValue);
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
                                queue.AddFirst(run.Students[Math.Max(a, b)]);
                                queue.AddFirst(run.Students[Math.Min(a, b)]);
                                yield return Step(battle, "move", "자리 교환 후 작은 번호부터 재행동");
                            }
                            break;
                    }

                    // Reaction actions emit the same event; there is deliberately no arbitrary combo cap.
                    // One yield per action allows the presentation loop to remain responsive.
                    var reactions = OtherAllies.SelectTargets(actor, run.Students).Where(s => s.Definition.Reaction).ToArray();
                    for (int i = reactions.Length - 1; i >= 0; i--) queue.AddFirst(reactions[i]);
                }

                if (run.PhotoCount > 0)
                {
                    AddDamage(run, run.PhotoCount * run.Settings.polaroidDamage);
                    yield return Step(battle, "damage", "폴라로이드 " + run.PhotoCount + "장 보정");
                }
                foreach (var relic in run.Relics)
                {
                    if (relic.Kind != RelicKind.FinalMultiplier) continue;
                    battle.Damage *= relic.Value; run.NotifyDamage();
                    yield return Step(battle, "damage", relic.Name + " ×" + relic.Value);
                }
                foreach (var keyword in battle.BossKeywords.ToArray())
                {
                    float percentage = KeywordRules.Apply(battle.BossKeywords, keyword);
                    battle.Damage *= 1f + percentage / 100f; run.NotifyDamage();
                    yield return Step(battle, "damage", "보스 " + keyword.Definition.Name + " 보정 +" + percentage + "%p");
                }

                yield return Step(battle, "chant", BuildChant(run.Students));
                yield return Step(battle, "beam", "합동 필살기! " + PrototypeCatalog.FormatDamage(battle.Damage));
                battle.BossHp = Math.Max(0f, battle.BossHp - battle.Damage);
                battle.Won = battle.BossHp <= 0f;
                battle.Lost = !battle.Won && battle.Turn >= 3;
                foreach (var student in run.Students) KeywordRules.EndTurn(student.Keywords);
                KeywordRules.EndTurn(battle.BossKeywords);
                yield return Step(battle, "result", battle.Won ? "보스 처치! 교실로 돌아가세요." : battle.Lost ? "3턴 종료 · 게임 오버" : "보스 생존 · 배치를 바꾸고 다음 턴에 도전하세요.");
            }
            finally { battle.IsResolving = false; }
        }

        private static void AddDamage(PrototypeRun run, float amount) { run.Battle.Damage += amount; run.NotifyDamage(); }
        private static BattleStep Step(BattleState battle, string kind, string text)
        {
            battle.Log.Add(text);
            return new BattleStep(kind, text, battle.Damage);
        }

        public static string BuildChant(IEnumerable<StudentState> students)
        {
            var entries = new List<Tuple<string, int>>();
            foreach (var student in students)
            {
                entries.Add(Tuple.Create(student.Definition.Club, 0));
                entries.Add(Tuple.Create(student.Definition.Personality, 1));
                if (!string.IsNullOrEmpty(student.Definition.SpecialSynergy)) entries.Add(Tuple.Create(student.Definition.SpecialSynergy, 2));
            }
            var names = entries.GroupBy(e => e.Item1).Where(g => g.Count() >= 2)
                .OrderByDescending(g => g.Count()).ThenBy(g => g.Min(e => e.Item2)).ThenBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => g.Key).ToArray();
            return names.Length == 0 ? "우리의 마음을 담아!" : string.Join("와 ", names) + "를 담아!";
        }
    }
}
