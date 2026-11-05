using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HongikIngan
{
    public static class GirlContentChecks
    {
        static int assertions;
        static readonly List<EconomySettings> settings = new List<EconomySettings>();
        public static void Run()
        {
            assertions = 0;
            try
            {
                Roster(); Skills(); Movement(); Synergies(); PhotographySynergy(); Economy();
                Debug.Log("GIRL_CONTENT_CHECKS_PASS assertions=" + assertions);
            }
            finally { foreach (var s in settings) UnityEngine.Object.DestroyImmediate(s); settings.Clear(); }
        }
        static void Check(bool condition, string message)
        { assertions++; if (!condition) throw new InvalidOperationException("GIRL_CONTENT_CHECKS_FAIL: " + message); }
        static void Near(float actual, float expected, string message) => Check(Math.Abs(actual - expected) < Math.Max(.01f, Math.Abs(expected) * .00001f), message + ": " + actual + " != " + expected);
        static StudentState Girl(int index, bool synergies = false, bool upgraded = false)
        {
            var d = PrototypeCatalog.Students[index];
            var copy = new StudentDefinition { Id = d.Id, Name = d.Name, Grade = d.Grade, Role = d.Role,
                SkillName = d.SkillName, SkillDescription = d.SkillDescription, Action = d.Action,
                InitialValue = d.InitialValue, UpgradeStep = d.UpgradeStep, InitialPercent = d.InitialPercent,
                PercentUpgradeStep = d.PercentUpgradeStep, Club = synergies ? d.Club : "club_" + index,
                Personality = synergies ? d.Personality : "personality_" + index };
            return new StudentState(copy) { UpgradeCount = upgraded ? 1 : 0, SkillValue = copy.ValueAtUpgrade(upgraded ? 1 : 0) };
        }
        static StudentState Dummy(string id, float damage = 0, string club = null, string personality = null)
        {
            return new StudentState(new StudentDefinition { Id = id, Name = id, Grade = 1,
                Club = club ?? "club_" + id, Personality = personality ?? "personality_" + id,
                Action = ActionKind.AddDamage, InitialValue = damage });
        }
        static PrototypeRun Run(params StudentState[] actors)
        {
            var e = ScriptableObject.CreateInstance<EconomySettings>(); settings.Add(e);
            e.startingMoney = 100000; e.bossBaseHp = 1e30f;
            var run = new PrototypeRun(e, 912);
            for (int i = 0; i < 4; i++) run.ExpandSlots();
            run.Relics.Add(PrototypeCatalog.Relics.First(r => r.Kind == RelicKind.ExtraSlots));
            run.Students.Clear(); run.Students.AddRange(actors);
            Check(run.StartBattle(), "test battle starts");
            return run;
        }
        static void Resolve(PrototypeRun run)
        {
            int steps = 0;
            using (var iterator = run.ResolveTurn().GetEnumerator())
                while (iterator.MoveNext()) Check(++steps < 5000, "finite authored scenario terminates");
        }
        static void Roster()
        {
            string[] names = { "아이린", "효빈", "리오네", "찬비", "가키", "클로에", "민지", "루크레아", "가람", "한별", "나연", "다혜" };
            int[] grades = { 1, 1, 3, 2, 1, 1, 2, 3, 3, 2, 1, 1 };
            Check(PrototypeCatalog.Students.Count >= 12, "original twelve authored girls remain in the expanded roster");
            for (int i = 0; i < 12; i++)
            {
                var d = PrototypeCatalog.Students[i];
                Check(d.Id == "Girl" + i && d.Name == names[i] && d.Grade == grades[i], "title identity and grade " + i);
                Check(d.Club == (i < 6 ? "운동부" : "선도부") && !string.IsNullOrEmpty(d.SkillName), "club and named skill " + i);
                Check(d.UpgradeEnabled && !d.UnlimitedUpgrade, "documented one-time upgrade " + i);
                if (i >= 6) Check(d.Role == StudentRole.Dealer, "discipline club uses attack role " + i);
            }
            Check(PrototypeCatalog.Students[8].Personality == "리더", "leader alias normalized");
        }
        static void Skills()
        {
            var fourth = Run(Dummy("a"), Dummy("b"), Dummy("c"), Girl(0)); Resolve(fourth);
            Near(fourth.Battle.Damage, 120, "fourth batter triple");
            var normal = Run(Girl(0, upgraded: true)); Resolve(normal);
            Near(normal.Battle.Damage, 80, "upgraded batter outside fourth slot");
            foreach (int index in new[] { 6, 9 })
            {
                var team = Run(Dummy("a"), Girl(index, upgraded: true), Dummy("b")); Resolve(team);
                Near(team.Battle.Damage, 450, "upgraded team-size skill " + index);
            }
            var power = Run(Dummy("seed", 100), Girl(7)); Resolve(power);
            Near(power.Battle.Damage, (float)Math.Pow(100, 1.3f), "power replaces rather than adds");
            var overflow = Run(Dummy("seed", 1e30f), Girl(7, upgraded: true), Girl(8)); Resolve(overflow);
            Check(float.IsPositiveInfinity(overflow.Battle.Damage) && overflow.Battle.Won, "zero-target buff cannot turn overflow damage into NaN");
            var rebels = Run(Dummy("seed", 100, personality: "반항아"), Girl(8, upgraded: true)); Resolve(rebels);
            Near(rebels.Battle.Damage, 250, "rebel count times upgraded percentage");
            var good = Run(Dummy("seed", 100), Girl(10, upgraded: true)); Resolve(good);
            Near(good.Battle.Damage, 115, "upgraded good cop");
            var venom = Run(Dummy("seed", 100), Girl(11, upgraded: true)); Resolve(venom);
            Near(venom.Battle.Damage, 230, "upgraded venom adds then increases current damage");
            var spite = Run(Girl(4, upgraded: true));
            KeywordRules.Add(spite.Battle.BossKeywords, new KeywordDefinition("a", "a", false, true, false), 0, 4);
            KeywordRules.Add(spite.Battle.BossKeywords, new KeywordDefinition("b", "b", false, true, false), 0, 7);
            Resolve(spite);
            Check(spite.Battle.BossKeywords.Single(k => k.Definition.Id == "spite").Stacks == 4, "spite counts two kinds, not eleven stacks");
            Resolve(spite);
            Check(spite.Battle.BossKeywords.Single(k => k.Definition.Id == "spite").Stacks == 10, "spite persists and includes its existing kind next turn");
            var clover = Run(Dummy("seed", 100));
            KeywordRules.Add(clover.Battle.BossKeywords, KeywordRules.BlackClover, 1.2f, 3);
            Resolve(clover);
            Near(clover.Battle.Damage, 172.8f, "clover multiplies once per consumed stack");
            Check(clover.Battle.BossKeywords.Count == 0 && clover.Battle.Log.Count(s => s.Contains("클로버 1스택 소모")) == 3, "clover consumes one at a time and recasts remaining stacks");
            Check(!KeywordRules.BlackClover.Volatile && !KeywordRules.Spite.Volatile && !KeywordRules.Spite.Consumable, "keyword lifetime defaults");
        }
        static void Movement()
        {
            var step = Run(Dummy("seed", 10), Girl(1)); Resolve(step);
            Near(step.Battle.Damage, 320, "one forward move, 300 damage, partner repeats");
            Check(step.Students[0].Definition.Id == "Girl1", "mover occupies previous slot");
            var goat = Run(Dummy("a", 100), Dummy("b", 10), Girl(2)); Resolve(goat);
            Near(goat.Battle.Damage, 270.4f, "move effects and partner actions finish before next move");
            Check(goat.Students[0].Definition.Id == "Girl2", "goat reaches first slot");
            var chanbi = Run(Girl(3), Girl(1)); Resolve(chanbi);
            Near(chanbi.Battle.Damage, 2300, "external movement triggers all allies including chanbi");
            var four = Run(Dummy("a"), Dummy("b"), Dummy("c"), Girl(0), Girl(1)); Resolve(four);
            Near(four.Battle.Damage, 460, "fourth batter loses triple after displacement to fifth");
            var upgraded = Run(Dummy("a", 10), Dummy("b", 10), Girl(1, upgraded: true)); Resolve(upgraded);
            Near(upgraded.Battle.Damage, 640, "upgraded magic step moves two separate steps");
        }
        static void Mood(PrototypeRun run, float target)
        {
            var maker = Dummy("mood_source", personality: "분위기메이커");
            while (run.Atmosphere < target) BattleSynergies.AfterAction(run, maker).ToList();
        }
        static void Synergies()
        {
            float[] sportsExpected = { 0, 0, 200, 400, 1600, 16000, 5000000 };
            for (int count = 2; count <= 6; count++)
            {
                var sports = Run(Enumerable.Range(0, count).Select(i => Dummy("s" + i, club: "운동부")).ToArray()); Resolve(sports);
                Near(sports.Battle.Damage, sportsExpected[count], "highest sports tier " + count);
            }
            var leader = Girl(8, synergies: true);
            var buff = Run(Dummy("seed", 100, personality: "반항아"), leader); Resolve(buff);
            Near(buff.Battle.Damage, 260, "leader bonus follows its percentage buff");
            var love = Run(Dummy("l1", 100, personality: "사랑꾼"), Dummy("l2", personality: "사랑꾼")); Resolve(love);
            Near(love.Battle.Damage, 300, "two adjacent lovers give persistent two-hundred-percent bonus");
            var splitLove = Run(Dummy("l1", 100, personality: "사랑꾼"), Dummy("gap"), Dummy("l2", personality: "사랑꾼")); Resolve(splitLove);
            Near(splitLove.Battle.Damage, 100, "separated lovers do not gain adjacency bonus");
            var rebels = Run(Dummy("r1", 100, personality: "반항아"), Dummy("r2", 100, personality: "반항아")); Resolve(rebels);
            Near(rebels.Battle.Damage, 280, "rebel double actions and final point-seven multiplier");
            var mood = Run(Dummy("first", 10), Dummy("last", 10)); Mood(mood, 300); Resolve(mood);
            Check(mood.Battle.Log.Count(s => s.StartsWith("last ·")) == 2, "mood extra action limited once for target each turn");
            float saved = mood.Atmosphere; Resolve(mood);
            Check(mood.Atmosphere == saved && mood.Battle.Log.Count(s => s.StartsWith("last ·")) == 4, "mood persists and next turn renews replay");
            var movingLast = Run(Dummy("first"), Dummy("last")); Mood(movingLast, 300);
            Check(BattleSynergies.TakeMoodReplay(movingLast, movingLast.Students[1]), "first last-slot replay allowed");
            movingLast.Students.Reverse();
            Check(!BattleSynergies.TakeMoodReplay(movingLast, movingLast.Students[1]), "changing last-slot identity does not renew one replay this turn");
            var weighted = Run(Dummy("a", club: "運動"), Dummy("b", club: "運動")); Mood(weighted, 500);
            Check(BattleSynergies.Count(weighted, "運動") == 3, "five hundred adds one last-girl synergy count");
            Mood(weighted, 5000);
            Check(BattleSynergies.Count(weighted, "運動") == 4, "five thousand weights every girl");
            var harshActors = Enumerable.Range(0, 3).Select(i => Dummy("h" + i, i == 0 ? 100 : 0, personality: "독설가")).ToArray();
            harshActors[2].Definition.Role = StudentRole.Debuff;
            var harsh = Run(harshActors); Resolve(harsh); Near(harsh.Battle.Damage, 120, "harsh multiplier uses actual debuff-role actions");
            var cute = Run(Enumerable.Range(0, 9).Select(i => Dummy("c" + i, personality: "귀염둥이")).ToArray());
            cute.Relics.Add(PrototypeCatalog.Relics.First(r => r.Kind == RelicKind.OpeningDamage));
            var consumable = new KeywordDefinition("test_consumable", "test", true, true, false);
            KeywordRules.Add(cute.Students[0].Keywords, consumable, 5);
            Resolve(cute); Near(cute.Battle.Damage, 85, "cute nine doubles relic and applies keyword");
            Check(cute.Students[0].Keywords.Count == 0, "highest cute tier does not implicitly add lower-tier consumption immunity");
            var cuteSeven = Run(Enumerable.Range(0, 7).Select(i => Dummy("c" + i, personality: "귀염둥이")).ToArray());
            KeywordRules.Add(cuteSeven.Students[0].Keywords, consumable, 5);
            Resolve(cuteSeven);
            Check(cuteSeven.Students[0].Keywords.Single().Stacks == 1, "cute seven preserves consumable stack");
        }
        static void PhotographySynergy()
        {
            foreach (int count in new[] { 1, 2, 3, 4, 5, 6 })
            {
                var run = Run(Enumerable.Range(0, count).Select(i => Dummy("photo" + i, i == 0 ? 100 : 0, club: "사진부")).ToArray());
                run.Settings.polaroidDamage = 0;
                run.Photos["photo-source"] = 2;
                Resolve(run);
                float factor = count >= 6 ? 1.2f : count >= 4 ? 1.3f : count >= 2 ? 1.05f : 1f;
                Near(run.Battle.Damage, 100 * factor * factor, "photography highest tier applies once per photo " + count);
                foreach (var actor in run.Students) actor.SkillValue = 0;
                run.Photos["photo-source"] = int.MaxValue;
                Resolve(run);
                Near(run.Battle.Damage, 0, "zero damage never becomes NaN for many photos " + count);
            }
        }
        static void Economy()
        {
            foreach (int n in new[] { 2, 3, 4, 6 })
            {
                var e = Run(Enumerable.Range(0, n).Select(i => Dummy("d" + i, 100, club: "선도부")).ToArray());
                Check(BattleSynergies.Income(e, 15) == (n == 2 ? 22 : n == 6 ? 15 : 30), "all-income multiplier highest tier " + n);
                int before = e.Money;
                BattleSynergies.FinishBattle(e);
                Check(e.Money == before, "unfinished battle cannot grant end reward");
                Resolve(e); Resolve(e); Resolve(e);
                BattleSynergies.FinishBattle(e); BattleSynergies.FinishBattle(e);
                int expected = n >= 6 ? before * 2 : n >= 4 ? before + 200 : before;
                Check(e.Money == expected, "battle-end reward applied exactly once " + n);
            }
            var persist = Run(Girl(0, synergies: true));
            persist.Settings.bossBaseHp = 1; // Next battle uses this value; current battle must still be won normally.
            persist.Relics.Add(new RelicDefinition { Kind = RelicKind.OpeningDamage, Value = 1e30f, Name = "win fixture" });
            Resolve(persist);
            float atmosphere = persist.Atmosphere;
            Check(persist.ReturnToSchool() && persist.Atmosphere == atmosphere && atmosphere > 0, "atmosphere survives battle-to-school transition");
        }
    }
}
