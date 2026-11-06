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
        // Coffee grows geometrically. Store repeated equal stacks compactly instead of allocating one entry per stack.
        public double AdditionalStacks;
        public double StackCount => Values.Count + AdditionalStacks;
        public int Stacks => (int)Math.Min(int.MaxValue, StackCount);
        public KeywordState(KeywordDefinition definition, float value) { Definition = definition; Values.Add(value); }
    }

    public static class KeywordRules
    {
        public static readonly KeywordDefinition Starlight = new KeywordDefinition("starlight", "별빛", true, true, true);
        public static readonly KeywordDefinition Exposed = new KeywordDefinition("exposed", "노출", false, true, true);
        public static readonly KeywordDefinition Spite = new KeywordDefinition("spite", "표독", false, true, false);
        public static readonly KeywordDefinition BlackClover = new KeywordDefinition("black_clover", "검은 클로버", true, true, false);
        public static readonly KeywordDefinition Mine = new KeywordDefinition("you_are_mine", "넌 내 꺼야", false, false, false);
        public static readonly KeywordDefinition Lighting = new KeywordDefinition("lighting", "조명", false, false, true);
        public static readonly KeywordDefinition Love = new KeywordDefinition("love", "사랑", true, true, true);
        public static readonly KeywordDefinition Bullet = new KeywordDefinition("bullet", "총알", true, true, false);
        public static readonly KeywordDefinition Art = new KeywordDefinition("art", "미감", true, true, false);
        public static readonly KeywordDefinition Sticky = new KeywordDefinition("sticky", "끈적함", false, true, true);
        public static readonly KeywordDefinition StickyPersistent = new KeywordDefinition("sticky_persistent", "끈적끈적함", false, true, false);
        public static readonly KeywordDefinition Sun = new KeywordDefinition("tomorrow_sun", "내일의 태양", false, false, false);
        public static readonly KeywordDefinition Coffee = new KeywordDefinition("coffee", "커피", false, true, false);

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

        public static float Apply(List<KeywordState> states, KeywordState state, bool preserveConsumable = false)
        {
            if (state.Stacks == 0) return 0;
            float amount = state.Definition.Consumable ? state.Values[0] : state.Values.Sum();
            if (state.Definition.Consumable && !preserveConsumable)
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
        public readonly StudentState Actor, Target;
        public BattleStep(string kind, string message, float damage, StudentState actor = null, StudentState target = null)
        { Kind = kind; Message = message; Damage = damage; Actor = actor; Target = target; }
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
        public bool Saved { get; internal set; }
        public bool CanReturnToSchool => Won || Saved;
        public bool Ended => CanReturnToSchool || Lost;
        public int TurnLimit { get; internal set; } = 3;
        public BattleReward Reward { get; internal set; }
        public readonly List<string> Log = new List<string>();
        public readonly List<KeywordState> BossKeywords = new List<KeywordState>();
        internal readonly HashSet<StudentState> MoodReplays = new HashSet<StudentState>();
        public float LoverBonusPercent { get; internal set; }
        internal bool SynergyRewardsApplied;
        internal int EndDisciplineCount = -1, EndAstronomyCount = -1;
        public BattleState(float hp) { BossHp = hp; MaxBossHp = hp; }
    }

    public static class CombatEngine
    {
        public static IEnumerable<BattleStep> ResolveTurn(PrototypeRun run)
        {
            var battle = run.Battle;
            if (battle == null || battle.IsResolving || battle.Ended) yield break;
            battle.IsResolving = true;
            try
            {
                battle.Turn++;
                BattleSynergies.BeginTurn(run);
                battle.Damage = 0f;
                run.NotifyDamage();
                yield return Step(battle, "turn", "턴 " + battle.Turn + " - 합동 데미지 0으로 초기화");

                var coffee = battle.BossKeywords.FirstOrDefault(k => k.Definition.Id == KeywordRules.Coffee.Id);
                if (coffee != null)
                {
                    double reduction = Math.Min(battle.BossHp, coffee.StackCount * 20);
                    battle.BossHp = (float)Math.Max(0, battle.BossHp - reduction);
                    run.RecordDamage((float)reduction);
                    coffee.AdditionalStacks += reduction;
                    yield return Step(battle, "damage", "커피 - 보스 HP -" + reduction.ToString("0") + ", 커피 " + coffee.StackCount.ToString("0"));
                    if (battle.BossHp <= 0)
                    {
                        foreach (var result in FinishTurn(run)) yield return result;
                        yield break;
                    }
                }

                foreach (var step in BattleActionResolver.Resolve(run)) yield return step;

                if (run.PhotoCount > 0)
                {
                    AddDamage(run, run.PhotoCount * run.Settings.polaroidDamage);
                    yield return Step(battle, "damage", "폴라로이드 " + run.PhotoCount + "장 보정");
                }
                foreach (var step in BattleSynergies.FinalizeDamage(run)) yield return step;
                if (run.FortuneDamageMultiplier > 1)
                {
                    battle.Damage *= run.FortuneDamageMultiplier; run.NotifyDamage();
                    yield return Step(battle, "damage", "오늘의 운세 - 공격 완료 후 데미지 ×" + run.FortuneDamageMultiplier);
                }
                foreach (var keyword in battle.BossKeywords.ToArray())
                {
                    if (keyword.Definition.Id == KeywordRules.Mine.Id || keyword.Definition.Id == KeywordRules.Coffee.Id ||
                        keyword.Definition.Id == KeywordRules.Sticky.Id || keyword.Definition.Id == KeywordRules.StickyPersistent.Id) continue;
                    if (keyword.Definition.Id == KeywordRules.BlackClover.Id)
                    {
                        while (keyword.Stacks > 0)
                        {
                            float multiplier = KeywordRules.Apply(battle.BossKeywords, keyword);
                            battle.Damage *= multiplier; run.NotifyDamage();
                            yield return Step(battle, "damage", "검은 클로버 1스택 소모 - 데미지 ×" + multiplier);
                        }
                        continue;
                    }
                    float percentage = KeywordRules.Apply(battle.BossKeywords, keyword);
                    battle.Damage *= 1f + percentage / 100f; run.NotifyDamage();
                    yield return Step(battle, "damage", "보스 " + keyword.Definition.Name + " 보정 +" + percentage + "%p");
                }

                yield return Step(battle, "chant", BuildChant(run.Students));
                yield return Step(battle, "beam", "합동 필살기! " + PrototypeCatalog.FormatDamage(battle.Damage));
                run.RecordDamage(battle.Damage);
                battle.BossHp = Math.Max(0f, battle.BossHp - battle.Damage);
                foreach (var result in FinishTurn(run)) yield return result;
            }
            finally { battle.IsResolving = false; }
        }

        static IEnumerable<BattleStep> FinishTurn(PrototypeRun run)
        {
            var battle = run.Battle;
            battle.Won = battle.BossHp <= 0;
            if (!battle.Won && battle.Turn >= battle.TurnLimit)
                battle.Lost = !run.TrySunRescue();
            if (battle.Ended)
            {
                battle.EndDisciplineCount = BattleSynergies.Count(run, "선도부");
                battle.EndAstronomyCount = BattleSynergies.Count(run, "천문부");
            }
            if (battle.CanReturnToSchool) run.PrepareBattleReward();
            if (battle.Lost) BattleSynergies.FinishBattle(run);
            foreach (var student in run.Students) KeywordRules.EndTurn(student.Keywords);
            KeywordRules.EndTurn(battle.BossKeywords);
            run.RemoveExpiredClones(battle.Ended);
            yield return Step(battle, "result", battle.Won ? "보스 처치! 보상을 수령하세요." :
                battle.Saved ? "내일의 태양이 게임오버를 막았습니다. 보상을 수령하세요." :
                battle.Lost ? battle.TurnLimit + "턴 종료 - 게임 오버" : "보스 생존 - 다음 턴에 도전하세요.");
        }

        internal static void AddDamage(PrototypeRun run, float amount) { run.Battle.Damage += amount; run.NotifyDamage(); }
        internal static BattleStep Step(BattleState battle, string kind, string text, StudentState actor = null, StudentState target = null)
        {
            battle.Log.Add(text);
            return new BattleStep(kind, text, battle.Damage, actor, target);
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
                .Select(g => SynergyCatalog.Find(g.Key)?.Word).Where(word => !string.IsNullOrEmpty(word)).ToArray();
            return names.Length == 0 ? "우리의 마음을 담아!" : string.Join("와 ", names) + "를 담아!";
        }
    }
}
