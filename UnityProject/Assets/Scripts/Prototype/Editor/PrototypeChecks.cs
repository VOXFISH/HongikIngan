using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HongikIngan
{
    /// <summary>Run with -executeMethod HongikIngan.PrototypeChecks.Run. No scenes or assets are modified.</summary>
    public static class PrototypeChecks
    {
        private static int assertions;
        private static readonly List<EconomySettings> settings = new List<EconomySettings>();

        public static void Run()
        {
            assertions = 0;
            try
            {
                PetTransactions();
                FailedTransactions();
                SamplingAndOffers();
                ReplacementAndUpgrades();
                CapacityAndRelics();
                RerollAndBattle();
                Keywords();
                CombatOrderingAndChains();
                DamageFormatting();
                Debug.Log("PROTOTYPE_CHECKS_PASS assertions=" + assertions);
            }
            finally
            {
                foreach (var item in settings) UnityEngine.Object.DestroyImmediate(item);
                settings.Clear();
            }
        }

        private static EconomySettings Economy(int money = 100000)
        {
            var value = ScriptableObject.CreateInstance<EconomySettings>();
            value.startingMoney = money;
            settings.Add(value);
            return value;
        }

        private static void Check(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new InvalidOperationException("PROTOTYPE_CHECKS_FAIL: " + message);
        }

        private static void PetOffer(PrototypeRun run, PetKind kind)
        {
            run.PetOffers.Clear();
            run.PetOffers.Add(kind);
        }

        private static void PetTransactions()
        {
            foreach (PetKind kind in Enum.GetValues(typeof(PetKind)))
            {
                var economy = Economy();
                var run = new PrototypeRun(economy, 100 + (int)kind);
                PetOffer(run, kind);
                int money = run.Money;
                Check(run.PendingCandidates.Count == 0, "No candidates revealed before purchase");
                Check(run.OpenPet(0), "Pet opens: " + kind);
                Check(run.PendingPet && run.Money == money - economy.PetPrice(kind), "Exactly one prepaid pet cost");
                int candidateCount = run.PendingCandidates.Count;
                Check(candidateCount > 0, "Pet has eligible candidates");
                if (kind != PetKind.Synergy)
                    Check(candidateCount == (kind == PetKind.Normal ? 3 : 5), "Pet candidate count");
                Check(run.PendingCandidates.Select(s => s.Id).Distinct().Count() == candidateCount, "Unique pet candidates");
                Check(run.PendingCandidates.All(s => !run.Owns(s.Id)), "Pet excludes owned students");
                int paid = run.Money;
                Check(!run.Reroll() && !run.StartBattle() && !run.Upgrade(0) && !run.ExpandSlots(), "Pending choice locks unrelated operations");
                Check(run.Money == paid && run.PendingCandidates.Count == candidateCount, "Locked operations preserve payment and candidates");
                string chosen = run.PendingCandidates[0].Id;
                int oldCount = run.Students.Count;
                Check(run.ChoosePet(0), "Choose a student");
                Check(run.Students.Count == oldCount + 1 && run.Owns(chosen), "Exactly one student added");
                Check(!run.PendingPet && !run.ChoosePet(0), "All pet tiers finish after one choice, including premium");
                Check(run.Money == paid, "No grade price charged after pet payment");
                Check(run.PhotoCount == candidateCount - 1 && !run.Photos.ContainsKey(chosen), "Only unchosen candidates become photos");
                Check(run.PetOffers[0] == null, "Purchased offer remains empty");

                var skip = new PrototypeRun(economy, 200 + (int)kind);
                PetOffer(skip, kind);
                Check(skip.OpenPet(0), "Open pet for abandonment");
                int skipMoney = skip.Money;
                int photoCount = skip.PendingCandidates.Count;
                Check(skip.SkipPet(), "Skip selection");
                Check(skip.Money == skipMoney && skip.PhotoCount == photoCount && skip.Students.Count == 1, "Skip has no refund and grants all candidate photos");
                Check(!skip.SkipPet() && skip.PhotoCount == photoCount, "Repeated skip cannot duplicate photos");
            }

            var accumulate = new PrototypeRun(Economy(), 703);
            for (int i = 0; i < 12; i++)
            {
                PetOffer(accumulate, PetKind.Normal);
                Check(accumulate.OpenPet(0) && accumulate.SkipPet(), "Repeat paid photo acquisition");
            }
            Check(accumulate.PhotoCount == 36 && accumulate.Photos.Values.Any(n => n > 1), "Photo duplicates accumulate as total copies");

            var fallback = new PrototypeRun(Economy(), 401);
            fallback.Students.Clear();
            PetOffer(fallback, PetKind.Synergy);
            Check(fallback.OpenPet(0) && fallback.PendingCandidates.Count == 3, "Synergy pet falls back when no synergies are owned");
            Check(fallback.Money == fallback.Settings.startingMoney - fallback.Settings.PetPrice(PetKind.Synergy), "Fallback retains purchased pet cost");
        }

        private static void FailedTransactions()
        {
            var run = new PrototypeRun(Economy(0), 5);
            var recruits = run.RecruitOffers.Select(s => s.Id).ToArray();
            int cost = run.RerollCost;
            Check(!run.BuyRecruit(0) && !run.BuyRelic(0) && !run.OpenPet(0), "Insufficient funds prevent purchases");
            Check(!run.Upgrade(0) && !run.Reroll() && !run.ExpandSlots(), "Insufficient funds prevent paid operations");
            Check(run.Money == 0 && run.Students.Count == 1 && run.Relics.Count == 0 && !run.PendingPet, "Failed operations are atomic");
            Check(run.RerollCost == cost && recruits.SequenceEqual(run.RecruitOffers.Select(s => s.Id)), "Failed reroll preserves offers and next cost");
            Check(!run.BuyRecruit(-1) && !run.OpenPet(999) && !run.Sell(999), "Invalid indices rejected");
        }

        private static void SamplingAndOffers()
        {
            int[] grades = new int[3];
            bool repeatedNonNormalPet = false;
            var economy = Economy();
            for (int seed = 0; seed < 400; seed++)
            {
                var run = new PrototypeRun(economy, seed);
                Check(run.RecruitOffers.Count == 3, "Recruit offer has three candidates");
                Check(run.RecruitOffers.All(s => !run.Owns(s.Id)), "Recruit candidates exclude owned students");
                Check(run.RecruitOffers.Select(s => s.Id).Distinct().Count() == 3, "Recruit candidate uniqueness");
                foreach (var student in run.RecruitOffers) grades[student.Grade - 1]++;
                Check(run.PetOffers.Count == 3 && !run.PetOffers.All(p => p == PetKind.Normal), "Triple normal pet offers are rejected");
                repeatedNonNormalPet |= run.PetOffers.GroupBy(p => p).Any(g => g.Count() > 1 && g.Key != PetKind.Normal);
                Check(run.RelicOffers.Count == 3 && run.RelicOffers.Select(r => r.Id).Distinct().Count() == 3, "Unique relic offers");
            }
            Check(repeatedNonNormalPet, "Other repeated pet kinds remain possible");
            float total = grades.Sum();
            // Wide deterministic smoke bounds detect accidental uniform grade sampling, not statistical balance tuning.
            Check(grades[0] / total > .54f && grades[0] / total < .69f, "First grade weight approximately 4/6.5");
            Check(grades[1] / total > .23f && grades[1] / total < .39f, "Second grade weight approximately 2/6.5");
            Check(grades[2] / total > .03f && grades[2] / total < .13f, "Third grade weight approximately .5/6.5");
        }

        private static void ReplacementAndUpgrades()
        {
            var run = new PrototypeRun(Economy(), 11);
            var once = PrototypeCatalog.Students.First(s => !s.UnlimitedUpgrade);
            run.Students.Clear();
            run.Students.Add(new StudentState(once));
            Check(run.Upgrade(0), "Once skill upgrades");
            Check(run.Students[0].SkillValue == once.ValueAtUpgrade(1), "Upgrade replaces rather than adds computed value");
            int money = run.Money;
            Check(!run.Upgrade(0) && run.Money == money && run.Students[0].UpgradeCount == 1, "Once upgrade cap prevents repeat payment");
            Check(run.Sell(0), "Sell upgraded student");
            Check(run.Money == money + run.Settings.SalePrice(once.Grade), "Sale pays grade price");
            run.RecruitOffers.Clear(); run.RecruitOffers.Add(once);
            Check(run.BuyRecruit(0), "Released student can be recruited again");
            Check(run.Students[0].UpgradeCount == 0 && run.Students[0].SkillValue == once.InitialValue, "Recruiting again resets upgrades");
            Check(run.RecruitOffers[0] == null, "Recruit purchase leaves offer empty");

            var unlimited = PrototypeCatalog.Students.First(s => s.UnlimitedUpgrade);
            run.Students.Clear(); run.Students.Add(new StudentState(unlimited));
            for (int i = 1; i <= 4; i++)
                Check(run.Upgrade(0) && run.Students[0].UpgradeCount == i && run.Students[0].SkillValue == unlimited.ValueAtUpgrade(i), "Unlimited replacement rule at level " + i);

            run.Students.Add(new StudentState(PrototypeCatalog.Students[1]));
            run.Students.Add(new StudentState(PrototypeCatalog.Students[2]));
            PetOffer(run, PetKind.Premium);
            Check(run.OpenPet(0), "Full team can pay for pet choice");
            int paid = run.Money;
            var chosen = run.PendingCandidates[0];
            Check(!run.ChoosePet(0) && run.PendingPet && run.Money == paid, "Full team keeps paid choice pending");
            int sale = run.Settings.SalePrice(run.Students[0].Definition.Grade);
            Check(run.Sell(0), "Replacement sale permitted during choice");
            Check(run.PendingPet && run.Money == paid + sale, "Replacement preserves paid session and credits sale");
            Check(run.ChoosePet(0) && run.Owns(chosen.Id) && run.Students.Count == 3, "Replacement makes room for pending candidate");
        }

        private static void CapacityAndRelics()
        {
            var run = new PrototypeRun(Economy(), 28);
            Check(run.BaseCapacity == 3 && run.Capacity == 3, "Initial capacity is three");
            for (int i = 0; i < 4; i++) Check(run.ExpandSlots(), "Expand ordinary slot");
            int money = run.Money;
            Check(run.BaseCapacity == 7 && run.Capacity == 7 && !run.ExpandSlots() && run.Money == money, "Ordinary capacity caps at seven without extra charge");
            var desk = PrototypeCatalog.Relics.First(r => r.Kind == RelicKind.ExtraSlots);
            run.RelicOffers.Clear(); run.RelicOffers.Add(desk);
            Check(run.BuyRelic(0) && run.Capacity == 9, "Relic increases total capacity to nine");
            Check(run.Money == money - run.Settings.relicPrice && run.RelicOffers[0] == null, "Relic cost and empty purchased offer");
            run.RelicOffers[0] = desk;
            money = run.Money;
            Check(!run.BuyRelic(0) && run.Money == money, "Owned relic cannot be bought again");
            Check(run.Reroll() && run.RelicOffers.All(r => r.Id != desk.Id), "Relic redraw excludes owned relics");
        }

        private static void RerollAndBattle()
        {
            var economy = Economy(); economy.bossBaseHp = 1f;
            var run = new PrototypeRun(economy, 55);
            int money = run.Money;
            Check(run.RerollCost == 20 && run.Reroll(), "First reroll succeeds at twenty");
            Check(run.Money == money - 20 && run.RerollCost == 40 && run.Reroll(), "Second reroll costs forty");
            Check(run.Money == money - 60 && run.RerollCost == 60, "Next reroll costs sixty");
            Check(run.StartBattle() && run.RerollCost == 20, "Battle entry resets reroll cost");
            Check(!run.BuyRecruit(0) && !run.Sell(0) && !run.Reroll(), "Battle locks school transactions");
            var student = run.Students[0];
            run.ResolveNextTurn();
            Check(run.Battle.Won && !run.Battle.Lost, "Boss death wins before turn limit");
            money = run.Money;
            Check(run.ReturnToSchool() && !run.InBattle && run.Stage == 2, "Win returns to next stage school");
            Check(ReferenceEquals(student, run.Students[0]) && run.Money >= money, "School return preserves students and rewards");
            Check(run.RecruitOffers.Count == 3 && run.RelicOffers.Count == 3 && run.PetOffers.Count == 3, "Victory refreshes all offers");

            var losingEconomy = Economy(); losingEconomy.bossBaseHp = 1000000f;
            var losing = new PrototypeRun(losingEconomy, 56);
            Check(losing.StartBattle(), "Start losing scenario");
            var resets = new List<float>(); losing.DamageChanged += value => resets.Add(value);
            for (int turn = 1; turn <= 3; turn++)
            {
                int before = resets.Count;
                losing.ResolveNextTurn();
                Check(resets.Count > before && resets[before] == 0f, "Each turn first announces zero damage");
                Check(losing.Battle.Lost == (turn == 3), "Loss occurs only after third surviving turn");
            }
            Check(losing.GameOver && !losing.ReturnToSchool(), "Loss cannot return to school as a victory");
            int logCount = losing.Battle.Log.Count;
            Check(losing.ResolveNextTurn().Count == 0 && losing.Battle.Log.Count == logCount, "Ended battle cannot resolve another turn");
            var empty = new PrototypeRun(Economy(), 57); empty.Students.Clear();
            Check(!empty.StartBattle(), "Empty team cannot enter battle");
        }

        private static void Keywords()
        {
            for (int mask = 0; mask < 8; mask++)
            {
                bool consume = (mask & 1) != 0, stack = (mask & 2) != 0, expire = (mask & 4) != 0;
                var definition = new KeywordDefinition("keyword_" + mask, "Test", consume, stack, expire);
                var states = new List<KeywordState>();
                KeywordRules.Add(states, definition, 2f, 3);
                Check(states.Count == 1 && states[0].Stacks == (stack ? 3 : 1), "Initial stack policy " + mask);
                KeywordRules.Add(states, definition, 4f, 2);
                Check(states[0].Stacks == (stack ? 5 : 1), "Repeat application stack policy " + mask);
                float bonus = KeywordRules.Apply(states, states[0]);
                Check(bonus == (consume ? 2f : stack ? 14f : 2f), "Keyword application value " + mask);
                Check(states.Count == (consume && !stack ? 0 : 1), "Consumable nonstack keyword removed " + mask);
                if (states.Count > 0)
                    Check(states[0].Stacks == (stack ? (consume ? 4 : 5) : 1), "Only one consumable stack spent " + mask);
                KeywordRules.EndTurn(states);
                Check(states.Count == (expire || (consume && !stack) ? 0 : 1), "Volatile clears all stacks; persistent survives " + mask);
            }
            var unlimited = new List<KeywordState>();
            KeywordRules.Add(unlimited, new KeywordDefinition("many", "Many", false, true, false), 1f, 4096);
            Check(unlimited[0].Stacks == 4096, "No gameplay stack cap");
        }

        private static StudentState Actor(string id, ActionKind action, float value, bool reaction = false)
        {
            return new StudentState(new StudentDefinition
            {
                Id = id, Name = id, Club = "club_" + id, Personality = "personality_" + id,
                Grade = 1, Action = action, InitialValue = value, Role = StudentRole.Dealer,
                Reaction = reaction, SkillDescription = id
            });
        }

        private static PrototypeRun ControlledBattle(float hp, params StudentState[] actors)
        {
            var economy = Economy(); economy.bossBaseHp = hp;
            var run = new PrototypeRun(economy, 150);
            run.Students.Clear(); run.Students.AddRange(actors);
            Check(run.StartBattle(), "Controlled battle starts");
            return run;
        }

        private static void CombatOrderingAndChains()
        {
            var first = ControlledBattle(100000f, Actor("A", ActionKind.AddDamage, 100f), Actor("B", ActionKind.MultiplyDamage, 2f), Actor("C", ActionKind.AddDamage, 10f));
            var updates = new List<float>(); first.DamageChanged += updates.Add;
            first.ResolveNextTurn();
            Check(first.Battle.Damage == 210f, "Mixed damage operations preserve occurrence order");
            Check(updates.SequenceEqual(new[] { 0f, 100f, 200f, 210f }), "Every damage mutation publishes the running float value");
            var second = ControlledBattle(100000f, Actor("A", ActionKind.AddDamage, 100f), Actor("C", ActionKind.AddDamage, 10f), Actor("B", ActionKind.MultiplyDamage, 2f));
            second.ResolveNextTurn();
            Check(second.Battle.Damage == 220f, "Changing slot order changes mixed-operation damage");

            var exact = ControlledBattle(300f, Actor("exact", ActionKind.AddDamage, 100f));
            exact.ResolveNextTurn(); exact.ResolveNextTurn();
            Check(!exact.Battle.Won && !exact.Battle.Lost, "Two surviving turns do not finish the battle");
            exact.ResolveNextTurn();
            Check(exact.Battle.Turn == 3 && exact.Battle.Won && !exact.Battle.Lost && exact.Battle.BossHp == 0f, "Exact third-turn kill wins instead of losing");

            var mover = Actor("move", ActionKind.SwapNext, 0f);
            var partner = Actor("partner", ActionKind.AddDamage, 1f);
            var swapping = ControlledBattle(100000f, mover, partner);
            var names = new List<string>();
            using (var iterator = swapping.ResolveTurn().GetEnumerator())
            {
                for (int guard = 0; guard < 30 && names.Count < 3 && iterator.MoveNext(); guard++)
                    if (iterator.Current.Kind == "action") names.Add(iterator.Current.Message.Split(' ')[0]);
            }
            Check(names.SequenceEqual(new[] { "move", "partner", "move" }), "Swapped pair repeats in ascending NEW slot order");
            Check(!swapping.Battle.IsResolving, "Disposing streamed turn releases resolving state");

            var chains = ControlledBattle(100000f, Actor("initial", ActionKind.AddDamage, 1f), Actor("react1", ActionKind.AddDamage, 1f, true), Actor("react2", ActionKind.AddDamage, 1f, true));
            var actionNames = new List<string>();
            using (var iterator = chains.ResolveTurn().GetEnumerator())
            {
                // Intentional mutual reaction scenario: inspect a bounded prefix, never exhaust an infinite combo.
                for (int guard = 0; guard < 80 && actionNames.Count < 12 && iterator.MoveNext(); guard++)
                    if (iterator.Current.Kind == "action") actionNames.Add(iterator.Current.Message.Split(' ')[0]);
            }
            Check(actionNames.Count == 12 && actionNames[0] == "initial", "Reaction actions emit further reactions without an arbitrary combo cap");
            Check(actionNames.Skip(1).All(n => n == "react1" || n == "react2"), "Only other reacting allies continue the reaction chain");
            for (int i = 2; i < actionNames.Count; i++) Check(actionNames[i] != actionNames[i - 1], "Reaction does not trigger itself");
        }

        private static void DamageFormatting()
        {
            Check(PrototypeCatalog.FormatDamage(1234.75f) == "1 234", "Damage display truncates decimals and groups digits");
            Check(PrototypeCatalog.FormatDamage(1e11f) == "1.00e11", "Large float display has two decimal places");
            Check(PrototypeCatalog.FormatDamage(2.05e11f) == "2.05e11", "Scientific format avoids int32 overflow");
        }
    }
}
