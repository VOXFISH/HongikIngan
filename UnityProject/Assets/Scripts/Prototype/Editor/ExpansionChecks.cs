using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HongikIngan
{
    public static class ExpansionChecks
    {
        static int assertions;
        static readonly List<EconomySettings> settings = new List<EconomySettings>();
        static void Check(bool condition, string message)
        { assertions++; if (!condition) throw new InvalidOperationException("EXPANSION_CHECKS_FAIL: " + message); }
        static void Near(float actual, float expected, string message)
        { Check(!float.IsNaN(actual) && Math.Abs(actual - expected) < Math.Max(.02f, Math.Abs(expected) * .00001f), message + ": " + actual + " != " + expected); }
        static StudentState Girl(int id, bool upgraded = false)
        {
            var d = PrototypeCatalog.Students.Single(s => s.Id == "Girl" + id);
            var copy = new StudentDefinition {
                Id = d.Id, Name = d.Name, Grade = d.Grade, Role = d.Role, Action = d.Action,
                Club = "test-" + id, Personality = "test-" + id, InitialValue = d.InitialValue,
                UpgradeStep = d.UpgradeStep, SkillName = d.SkillName, SkillDescription = d.SkillDescription,
                UnlimitedUpgrade = d.UnlimitedUpgrade, UpgradeEnabled = d.UpgradeEnabled
            };
            return new StudentState(copy) { UpgradeCount = upgraded ? 1 : 0, SkillValue = copy.ValueAtUpgrade(upgraded ? 1 : 0) };
        }
        static StudentState Dummy(float amount = 0, string club = "test") => new StudentState(new StudentDefinition {
            Id = "dummy", Name = "dummy", Grade = 1, Club = club, Personality = "test",
            Action = ActionKind.AddDamage, InitialValue = amount
        });
        static PrototypeRun Battle(params StudentState[] actors)
        {
            var e = ScriptableObject.CreateInstance<EconomySettings>(); settings.Add(e);
            e.bossBaseHp = 1e30f;
            var run = new PrototypeRun(e, 312);
            run.Students.Clear(); run.Students.AddRange(actors);
            Check(run.StartBattle(), "fixture enters battle");
            return run;
        }
        static void Resolve(PrototypeRun run)
        {
            int count = 0;
            foreach (var step in run.ResolveTurn()) Check(++count < 10000, "finite scenario terminates");
        }
        static void Set(object target, string property, object value) => target.GetType().GetProperty(property).SetValue(target, value);
        static void Actions(PrototypeRun run)
        {
            var resolver = typeof(PrototypeRun).Assembly.GetType("HongikIngan.BattleActionResolver");
            var steps = (IEnumerable<BattleStep>)resolver.GetMethod("Resolve").Invoke(null, new object[] { run });
            int count = 0;
            foreach (var step in steps) Check(++count < 10000, "finite skill chain");
        }
        static void ArtSkills()
        {
            foreach (bool upgraded in new[] { false, true })
            {
                var dana = Girl(18, upgraded); var run = Battle(dana);
                KeywordRules.Add(dana.Keywords, KeywordRules.Art, 1, 3); Actions(run);
                Check(dana.Keywords.Single().Stacks == 2, "art consumes one, not all stacks");
                var paint = run.Battle.BossKeywords.Single();
                Check(paint.Stacks == 3 && paint.Definition.Volatile == !upgraded, "paint snapshots own stacks and upgrades lifetime");
                var yeri = Girl(19, upgraded); run = Battle(yeri, Dummy());
                KeywordRules.Add(yeri.Keywords, KeywordRules.Art, 1, 3); Actions(run);
                Near(run.Battle.Damage, 900, "brush snapshots stacks before consuming and granting");
                Check(yeri.Keywords.Single().Stacks == (upgraded ? 4 : 3), "brush grant strength");
                var luna = Girl(20, upgraded); run = Battle(luna);
                int first = luna.LunarRoll; Actions(run); Actions(run);
                Check(first >= 1 && first <= (upgraded ? 12 : 10) && luna.LunarRoll == first, "lunar value fixed for battle without art");
                KeywordRules.Add(luna.Keywords, KeywordRules.Art, 1, 2); Actions(run);
                Check(luna.LunarRoll >= first && luna.LunarRoll <= (upgraded ? 12 : 10) && luna.Keywords.Single().Stacks == 1, "lunar reroll keeps larger value and consumes one art");
                Near(run.Battle.Damage, 0, "lunar skill grants clover without multiplying damage");
                var gayoung = Girl(21, upgraded); run = Battle(Dummy(100), gayoung, Dummy());
                KeywordRules.Add(gayoung.Keywords, KeywordRules.Art, 1, 2); Actions(run);
                Near(run.Battle.Damage, 100 * (float)Math.Pow(upgraded ? 1.5 : 1.35, 3), "art stack replay budget");
                Check(run.Students[0].Keywords.Single().Stacks == 4 && run.Students[2].Keywords.Single().Stacks == 4, "encore grants art to neighbors only");
                var jina = Girl(22, upgraded); run = Battle(Dummy(), jina, Dummy());
                KeywordRules.Add(jina.Keywords, KeywordRules.Art, 1, upgraded ? 10 : 15);
                KeywordRules.Add(run.Students[0].Keywords, KeywordRules.Lighting, 0); Actions(run);
                Check(run.Students.Count(s => s.IsClone) == 1, "only repeated brush can clone and clones cannot clone");
                var minyoung = Girl(23, upgraded); run = Battle(minyoung);
                KeywordRules.Add(minyoung.Keywords, KeywordRules.Art, 1, 100); Actions(run);
                Near(run.Battle.BossHp, 1, "Eros success sets absolute boss HP one");
                Check(minyoung.Keywords.Single().Stacks == 99, "Eros consumes one art");
            }
        }
        static void AstronomyAndTheatre()
        {
            foreach (bool upgraded in new[] { false, true })
            {
                var yejin = Girl(25, upgraded); var run = Battle(yejin, Dummy(), Dummy()); Set(run, "StarPower", 4); Actions(run);
                Check(run.Students[1].Keywords.Count == 0, "next attacker consumes love");
                var hajin = Girl(26, upgraded); run = Battle(hajin); Set(run, "StarPower", 80); Set(run.Battle, "BossHp", 100f);
                Actions(run); Actions(run); Near(run.Battle.BossHp, upgraded ? 16 : 25, "Saturn percentage uses remaining HP and upgrade cap");
                var somi = Girl(29, upgraded); run = Battle(somi); Set(run, "StarPower", 7); Actions(run);
                Check(run.StarPower == 8, "Somi adds shared persistent star power"); Near(run.Battle.Damage, upgraded ? 1000 : 500, "Somi flat damage");
                var mine = Girl(30, upgraded); run = Battle(mine); Set(run, "StarPower", 2); Set(run.Battle, "BossHp", 1000f);
                Resolve(run); double first = upgraded ? 4 : 2;
                Check(run.Battle.BossKeywords.Single().StackCount == first, "coffee grant counts shared star power with upgrade");
                Resolve(run); Near(run.Battle.BossHp, (float)(1000 - first * 20), "coffee ticks only at following turn start");
                Check(run.Battle.BossKeywords.Single().StackCount == first * 22, "coffee grows by damage removed then receives this action's grant");
                var munchi = Girl(35, upgraded); run = Battle(munchi); Set(run.Battle, "Damage", 100f);
                KeywordRules.Add(munchi.Keywords, KeywordRules.Art, 1, 3);
                KeywordRules.Add(munchi.Keywords, KeywordRules.Bullet, 1, 2);
                KeywordRules.Add(munchi.Keywords, KeywordRules.Sun, 0);
                Actions(run); Near(run.Battle.Damage, upgraded ? 300 : 150, "Sparta counts distinct kinds, not stacks");
            }
            var schoolSettings = ScriptableObject.CreateInstance<EconomySettings>(); settings.Add(schoolSettings);
            schoolSettings.startingMoney = 10000;
            var school = new PrototypeRun(schoolSettings); school.Students.Clear(); school.Students.Add(Girl(33));
            for (int i = 0; i < 5; i++) Check(school.Upgrade(0), "Seah repeated upgrade in same school visit");
            Near(school.Students[0].SkillValue, 320, "Seah exponential replacement");
            school.Students.Clear(); school.Students.Add(Girl(27));
            Check(school.Capacity == 1 && school.CanStartBattle, "Hyerim subtracts currently open slots");
            Check(school.Upgrade(0) && school.Capacity == 0 && !school.CanStartBattle, "upgraded Hyerim can exceed available capacity");
            Check(school.ExpandSlots() && school.Capacity == 1 && school.StartBattle() && school.Battle.TurnLimit == 5, "expand restores departure with extra turns");
            for (int count = 1; count <= 6; count++)
            {
                var run = Battle(Enumerable.Range(0, Math.Min(count, 3)).Select(i => Dummy(100, "연극부")).ToArray());
                while (run.Students.Count < count) run.Students.Add(Dummy(100, "연극부"));
                Resolve(run); Near(run.Battle.Damage, count * 100 * (count == 1 ? 2 : count == 4 ? 1.5f : 1), "theatre exact-count tiers " + count);
            }
            var ending = Battle(Dummy(1, "천문부"));
            for (int i = 0; i < 3; i++) { var clone = Dummy(1, "천문부"); clone.IsClone = true; ending.Students.Add(clone); }
            Set(ending.Battle, "BossHp", 1f); Resolve(ending);
            Check(ending.Students.Count == 1 && ending.ReturnToSchool() && ending.StarPower == 4, "end synergy snapshots clones before cleanup");
        }
        static void Rescue()
        {
            foreach (bool upgraded in new[] { false, true })
            {
                var suhyeon = Girl(28, upgraded); var run = Battle(suhyeon); Set(run, "StarPower", 5);
                int money = run.Money;
                Resolve(run); Resolve(run); Resolve(run);
                Check(run.Battle.Saved && !run.GameOver && !run.Battle.Won && run.SunRescueUsed, "sun rescues to result without a fake boss kill");
                Check(run.Money == money + (upgraded ? 0 : 500) && run.Battle.Reward != null, "sun income precedes settlement snapshot");
                Check(run.ReturnToSchool() && suhyeon.Keywords.Any(k => k.Definition.Id == KeywordRules.Sun.Id), "sun buff persists through school");
                Check(run.StartBattle(), "sun survivor starts next battle"); Set(run.Battle, "Damage", 100f); Actions(run);
                Near(run.Battle.Damage, (float)Math.Pow(100, upgraded ? 1.5f : 1.3f), "sun modifies each action after acquired");
                run.Students.Clear(); run.Students.Add(Girl(28)); // Selling/reacquiring must not reset the run-wide rescue.
                Resolve(run); Resolve(run); Resolve(run);
                Check(run.GameOver && run.Battle.Reward == null, "reacquisition does not grant a second rescue");
            }
        }
        public static void Run()
        {
            assertions = 0;
            try
            {
                Check(PrototypeCatalog.Students.Count == 36 && PrototypeCatalog.Students.Select(s => s.Id).Distinct().Count() == 36, "Girl0 through Girl35 unique roster");
                ArtSkills(); AstronomyAndTheatre(); Rescue();
                foreach (bool upgraded in new[] { false, true })
                {
                    var bullet = Girl(32, upgraded); var run = Battle(bullet);
                    Check(bullet.Keywords.Single().Stacks == (upgraded ? 2 : 1), "battle-start bullet stock");
                    Resolve(run); Near(run.Battle.Damage, 100000, "one bullet per action");
                    Resolve(run); Near(run.Battle.Damage, upgraded ? 100000 : 0, "bullets persist between turns and then run out");
                    Resolve(run); Near(run.Battle.Damage, 0, "no free attack without a bullet");
                }
                var hana = Girl(24); var hanaRun = Battle(Dummy(100), hana);
                KeywordRules.Add(hana.Keywords, KeywordRules.Lighting, 0);
                KeywordRules.Add(hana.Keywords, KeywordRules.Bullet, 1, 2);
                Resolve(hanaRun);
                // Lighting repeats the preceding dummy after Hana. Two bullets are not a qualifying singleton.
                Near(hanaRun.Battle.Damage, 211, "only singleton/nonstacking buffs multiply Hana's attack");
                var clover = Battle(Girl(31)); Resolve(clover);
                Near(clover.Battle.Damage, 360, "Shina adds 300 before final clover multiplier");
                foreach (bool upgraded in new[] { false, true })
                {
                    var mari = Girl(34, upgraded); mari.Definition.Club = "연극부";
                    var cloneRun = upgraded ? Battle(mari, Dummy(club: "연극부")) : Battle(mari, Dummy(club: "연극부"), Dummy(club: "연극부"));
                    Resolve(cloneRun);
                    Check(cloneRun.Battle.Log.Count(line => line == "돌돌마리 - 분신 생성") == 1, "self clone cannot recursively clone");
                    Check(cloneRun.Students.All(s => !s.IsClone), "self clone expires at end of turn");
                }
                Debug.Log("EXPANSION_CHECKS_PASS assertions=" + assertions);
            }
            finally { foreach (var e in settings) UnityEngine.Object.DestroyImmediate(e); settings.Clear(); }
        }
    }
}
