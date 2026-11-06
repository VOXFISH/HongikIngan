using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HongikIngan
{
    public static class PhotographyChecks
    {
        static int assertions;
        static readonly List<EconomySettings> settings = new List<EconomySettings>();
        public static void Run()
        {
            assertions = 0;
            try
            {
                Roster(); Mark(); Lighting(); Damage(); Love(); Copies(); MovementGuard();
                Debug.Log("PHOTOGRAPHY_CHECKS_PASS assertions=" + assertions);
            }
            finally { foreach (var value in settings) UnityEngine.Object.DestroyImmediate(value); settings.Clear(); }
        }
        static void Check(bool value, string message)
        { assertions++; if (!value) throw new InvalidOperationException("PHOTOGRAPHY_CHECKS_FAIL: " + message); }
        static void Near(float actual, float expected, string message)
        { Check(!float.IsNaN(actual) && Math.Abs(actual - expected) < Math.Max(.02f, Math.Abs(expected) * .00001f), message + " " + actual + " != " + expected); }
        static StudentState Girl(int id, bool upgraded = false, bool synergies = false)
        {
            var original = PrototypeCatalog.Students.Single(s => s.Id == "Girl" + id);
            var definition = new StudentDefinition
            {
                Id = original.Id, Name = original.Name, Grade = original.Grade, Role = original.Role,
                Action = original.Action, InitialValue = original.InitialValue, UpgradeStep = original.UpgradeStep,
                InitialPercent = original.InitialPercent, PercentUpgradeStep = original.PercentUpgradeStep,
                SkillName = original.SkillName, SkillDescription = original.SkillDescription,
                Club = synergies ? original.Club : "club" + id, Personality = synergies ? original.Personality : "personality" + id,
                SpecialSynergy = original.SpecialSynergy
            };
            return new StudentState(definition) { UpgradeCount = upgraded ? 1 : 0, SkillValue = definition.ValueAtUpgrade(upgraded ? 1 : 0) };
        }
        static StudentState Dummy(string id, float amount, ActionKind action = ActionKind.AddDamage)
        { return new StudentState(new StudentDefinition { Id = id, Name = id, Grade = 2, Club = id, Personality = id, InitialValue = amount, Action = action }); }
        static PrototypeRun Battle(params StudentState[] students)
        {
            var economy = ScriptableObject.CreateInstance<EconomySettings>(); settings.Add(economy);
            economy.startingMoney = 10000; economy.bossBaseHp = 1e30f;
            var run = new PrototypeRun(economy, 719);
            for (int i = 0; i < 4; i++) run.ExpandSlots();
            run.Students.Clear(); run.Students.AddRange(students.Take(7));
            Check(run.StartBattle(), "battle fixture starts");
            run.Students.Clear(); run.Students.AddRange(students); return run;
        }
        static void Resolve(PrototypeRun run, Action<BattleStep> inspect = null)
        {
            int steps = 0;
            foreach (var step in run.ResolveTurn())
            {
                Check(++steps < 20000, "documented finite movement/lighting chain finishes");
                inspect?.Invoke(step);
            }
            Check(!run.Battle.IsResolving, "turn releases its resolution lock");
        }
        static void Mark()
        {
            foreach (bool upgraded in new[] { false, true })
            {
                var run = Battle(Girl(12, upgraded), Dummy("a", 10), Dummy("b", 20));
                Resolve(run);
                float expected = 0;
                foreach (float damage in new[] { 10f, 10f, 20f, 20f }) expected = (expected + damage + 500) * (upgraded ? 1.1f : 1);
                Near(run.Battle.Damage, expected, "mark applies to initial and movement-repeat attacks");
                var mark = run.Battle.BossKeywords.Single(k => k.Definition.Id == KeywordRules.Mine.Id);
                Check(mark.Stacks == 1 && !mark.Definition.Consumable && !mark.Definition.Stackable && !mark.Definition.Volatile, "mark persists without consuming or stacking");
                Check(run.Battle.Log.Count(s => s.Contains(" → ")) == 2, "movement reactions cannot add more movement");
                Resolve(run);
                Check(run.Battle.BossKeywords.Count(k => k.Definition.Id == KeywordRules.Mine.Id) == 1, "reapplying mark does not duplicate it");
            }
        }
        static void Roster()
        {
            Check(Enumerable.Range(0, 18).All(id => PrototypeCatalog.Students.Count(s => s.Id == "Girl" + id) == 1), "original Girl0 through Girl17 remain unique");
            string[] names = { "지수", "수지", "지우", "나미", "정윤", "치즈" };
            int[] grades = { 1, 2, 1, 2, 3, 1 };
            StudentRole[] roles = { StudentRole.Debuff, StudentRole.Dealer, StudentRole.Dealer, StudentRole.Buff, StudentRole.Buff, StudentRole.Dealer };
            for (int i = 0; i < names.Length; i++)
            {
                var girl = PrototypeCatalog.Students.Single(s => s.Id == "Girl" + (12 + i));
                Check(girl.Name == names[i] && girl.Grade == grades[i] && girl.Role == roles[i] && girl.Club == "사진부", "natural-language photography identity " + girl.Id);
            }
            Check(PrototypeCatalog.Students.Single(s => s.Id == "Girl16").SpecialSynergy == "정밀화", "Jeongyun has Precision");
        }
        static void Love()
        {
            foreach (bool upgraded in new[] { false, true })
            {
                float factor = upgraded ? 1.331f : 1.21f;
                var flat = Battle(Girl(15, upgraded), Dummy("hit", 100)); Resolve(flat);
                Near(flat.Battle.Damage, 100 * factor, "love multiplies flat attack increase and consumes all stacks");
                Check(flat.Students.All(s => s.Keywords.Count == 0), "used and unused love both gone at turn end");
                var percent = Dummy("percent", 0, ActionKind.AddThenPercentDamage); percent.Definition.InitialPercent = 15;
                var fractional = Battle(Dummy("seed", 100), Girl(15, upgraded), percent); Resolve(fractional);
                Near(fractional.Battle.Damage, 100 + 15 * factor, "love scales the 15% increase, not the accumulated damage");
            }
            var secondNami = Girl(15); secondNami.IsClone = true; secondNami.CloneLastsBattle = true;
            var stacked = Battle(Girl(15), secondNami, Dummy("hit", 100)); Resolve(stacked);
            Near(stacked.Battle.Damage, 146.41f, "two love grants stack to four");
            var moving = Battle(Girl(15), Girl(1)); Resolve(moving);
            Near(moving.Battle.Damage, 363, "love waits for movement damage rather than consuming on a movement-only action");
            var composite = Battle(Girl(15), Girl(11)); Resolve(composite);
            Near(composite.Battle.Damage, 50 * 1.21f * (1 + .1f * 1.21f), "one attack's ordered flat and percentage increases share its consumed-love factor");
            var power = Battle(Dummy("seed", 100), Girl(15), Girl(7)); Resolve(power);
            Near(power.Battle.Damage, 100 + ((float)Math.Pow(100, 1.3f) - 100) * 1.21f, "love scales the increase of a replacement attack");
            var protectedActors = new[] { Girl(15), Dummy("hit", 100) }.Concat(Enumerable.Range(0, 5).Select(i => Dummy("cute" + i, 0))).ToArray();
            foreach (var actor in protectedActors) actor.Definition.Personality = "귀염둥이";
            var protectedRun = Battle(protectedActors); bool kept = false;
            Resolve(protectedRun, step => { if (step.Message == "데미지 +100") kept = protectedActors[1].Keywords.Any(k => k.Definition.Id == KeywordRules.Love.Id && k.Stacks == 2); });
            Near(protectedRun.Battle.Damage, 121, "cute seven preserves consumable love during attacks");
            Check(kept && protectedActors.All(s => s.Keywords.Count == 0), "consumption prevention does not prevent volatile expiry");
        }
        static void Lighting()
        {
            var first = Dummy("a", 100); var second = Dummy("b", 10);
            KeywordRules.Add(first.Keywords, KeywordRules.Lighting, 0);
            KeywordRules.Add(second.Keywords, KeywordRules.Lighting, 0);
            var run = Battle(first, second); Resolve(run);
            Near(run.Battle.Damage, 220, "lighting-caused actions cannot retrigger lighting");
            Check(first.Keywords.Count == 0 && second.Keywords.Count == 0, "lighting expires at turn end");
            foreach (bool upgraded in new[] { false, true })
            {
                var ally = Dummy("seed", 100); var spotlight = Battle(ally, Girl(13, upgraded));
                bool applied = false;
                Resolve(spotlight, step => applied |= ally.Keywords.Any(k => k.Definition.Id == KeywordRules.Lighting.Id));
                Near(spotlight.Battle.Damage, upgraded ? 140 : 115, "spotlight base/upgraded current-damage increase");
                Check(applied && ally.Keywords.Count == 0, "spotlight grants and expires adjacent lighting");
            }
        }
        static void Damage()
        {
            foreach (bool upgraded in new[] { false, true })
            {
                var jiu = Girl(14, upgraded); KeywordRules.Add(jiu.Keywords, KeywordRules.Lighting, 0);
                var run = Battle(jiu); var values = new List<float>(); run.DamageChanged += values.Add;
                Resolve(run);
                Check(values.SequenceEqual(new[] { 0f, 500f, upgraded ? 530f : 520f }), "Jiu adds base then per-light damage in order, without granting a keyword");
                Check(jiu.Definition.Role == StudentRole.Dealer && run.Battle.BossKeywords.Count == 0, "Jiu is an attacker without a debuff");
                var cheese = Battle(Dummy("seed", 100), Girl(17, upgraded, true)); Resolve(cheese);
                Near(cheese.Battle.Damage, upgraded ? 100 : 300, "Cheese counts self in each applicable category, sum versus product");
            }
        }
        static void Copies()
        {
            var artist = Girl(16); var target = Girl(0, true);
            KeywordRules.Add(target.Keywords, KeywordRules.Starlight, 7);
            var run = Battle(artist, target);
            var clone = Copy(run, artist);
            Check(clone != null && run.Students[2] == clone && clone.IsClone, "clone is placed after target");
            Check(clone.Definition == target.Definition && clone.SkillValue == 80 && clone.UpgradeCount == 1, "clone copies traits and upgrade");
            Check(clone.Keywords[0] != target.Keywords[0] && clone.Keywords[0].Values[0] == 7, "clone keywords are independent state");
            clone.Keywords[0].Values[0] = 9;
            Check(target.Keywords[0].Values[0] == 7 && Copy(run, clone) == null, "clone state cannot mutate original or create another clone");
            Resolve(run);
            Check(run.Students.Count == 2 && artist.PrecisionCopies == 2, "base clones expire at turn end and successful copies accumulate");
            Resolve(run);
            Check(artist.PrecisionCopies == 3 && run.Battle.Log.Any(s => s.Contains("정밀화 +9000")), "precision continues across turns");
            Check(new StudentState(artist.Definition).PrecisionCopies == 0, "reacquiring Jeongyun resets precision");
            var upgraded = Girl(16, true); var persistent = Battle(upgraded, Dummy("target", 1));
            Resolve(persistent);
            Check(persistent.Students.Count == 3 && persistent.Students[2].CloneLastsBattle, "upgraded clone survives turn end");
            typeof(BattleState).GetProperty("BossHp").SetValue(persistent.Battle, 1f);
            Resolve(persistent);
            Check(persistent.Battle.Won && persistent.Students.All(s => !s.IsClone), "all clones expire when battle ends");
            int copies = upgraded.PrecisionCopies;
            Check(persistent.ReturnToSchool() && upgraded.PrecisionCopies == copies, "precision survives school return");
            var full = Battle(new[] { Girl(16) }.Concat(Enumerable.Range(0, 6).Select(i => Dummy("member" + i, 0))).ToArray());
            var owner = full.Students[0];
            Check(full.Capacity == 7 && Copy(full, owner) != null && Copy(full, owner) != null, "temporary slots extend seven to nine");
            Check(full.Students.Count == 9 && Copy(full, owner) == null && owner.PrecisionCopies == 2, "failed copy at nine does not advance precision");
        }
        // Editor checks live in a separate assembly; keep this combat-only API internal in the player.
        static StudentState Copy(PrototypeRun run, StudentState actor) => (StudentState)typeof(PrototypeRun)
            .GetMethod("CloneNext", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .Invoke(run, new object[] { actor });
        static void MovementGuard()
        {
            var run = Battle(Dummy("swapA", 0, ActionKind.SwapNext), Dummy("swapB", 0, ActionKind.SwapNext), Dummy("hit", 10));
            Resolve(run);
            Check(run.Battle.Log.Count(s => s.Contains("자리 교환 후")) == 2, "movement-origin repeats never start a new swap chain");
        }
    }
}
