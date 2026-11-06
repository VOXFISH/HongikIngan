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
                Capacity();
                FortuneChecks();
                RerollAndBattle();
                PetRefillAndFortune();
                Keywords();
                CombatOrderingAndChains();
                DocumentedDamageActions();
                DamageFormatting();
                StartupChecks.Run();
                GirlContentChecks.Run();
                PhotographyChecks.Run();
                SettlementChecks.Run();
                ExpansionChecks.Run();
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

        // Transaction/combat fixtures deliberately start with one known student.
        // Startup distribution is checked separately against the real constructor.
        private static PrototypeRun SingleStudentRun(EconomySettings economy, int seed = 1234)
        {
            var run = new PrototypeRun(economy, seed);
            run.Students.Clear();
            run.Students.Add(new StudentState(PrototypeCatalog.Students[0]));
            return run;
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
                var run = SingleStudentRun(economy, 100 + (int)kind);
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
                Check(run.PetOffers[0].HasValue, "Consumed pet is immediately replaced");

                var skip = SingleStudentRun(economy, 200 + (int)kind);
                PetOffer(skip, kind);
                Check(skip.OpenPet(0), "Open pet for abandonment");
                int skipMoney = skip.Money;
                int photoCount = skip.PendingCandidates.Count;
                Check(skip.SkipPet(), "Skip selection");
                Check(skip.Money == skipMoney && skip.PhotoCount == photoCount && skip.Students.Count == 1, "Skip has no refund and grants all candidate photos");
                Check(!skip.SkipPet() && skip.PhotoCount == photoCount, "Repeated skip cannot duplicate photos");
            }

            var accumulate = SingleStudentRun(Economy(), 703);
            for (int i = 0; i < 12; i++)
            {
                PetOffer(accumulate, PetKind.Normal);
                Check(accumulate.OpenPet(0) && accumulate.SkipPet(), "Repeat paid photo acquisition");
            }
            Check(accumulate.PhotoCount == 36 && accumulate.Photos.Values.Any(n => n > 1), "Photo duplicates accumulate as total copies");

            var fallback = SingleStudentRun(Economy(), 401);
            fallback.Students.Clear();
            PetOffer(fallback, PetKind.Synergy);
            Check(fallback.OpenPet(0) && fallback.PendingCandidates.Count == 3, "Synergy pet falls back when no synergies are owned");
            Check(fallback.Money == fallback.Settings.startingMoney - fallback.Settings.PetPrice(PetKind.Synergy), "Fallback retains purchased pet cost");
        }

        private static void FailedTransactions()
        {
            var run = SingleStudentRun(Economy(0), 5);
            var fortune = run.Fortune;
            var pets = run.PetOffers.ToArray();
            int cost = run.RerollCost;
            Check(!run.OpenPet(0), "Insufficient funds prevent purchases");
            Check(!run.Upgrade(0) && !run.Reroll() && !run.ExpandSlots(), "Insufficient funds prevent paid operations");
            Check(run.Money == 0 && run.Students.Count == 1 && run.Fortune == null && !run.PendingPet, "Failed operations are atomic");
            Check(run.RerollCost == cost && fortune == run.Fortune && pets.SequenceEqual(run.PetOffers), "Failed reroll preserves offers and next cost");
            Check(!run.OpenPet(999) && !run.Sell(999), "Invalid indices rejected");
        }

        private static void SamplingAndOffers()
        {
            int[] grades = new int[3];
            bool repeatedNonNormalPet = false;
            var economy = Economy();
            for (int seed = 0; seed < 400; seed++)
            {
                var run = SingleStudentRun(economy, seed);
                Check(run.PetOffers.Count == 3 && !run.PetOffers.All(p => p == PetKind.Normal), "Triple normal pet offers are rejected");
                repeatedNonNormalPet |= run.PetOffers.GroupBy(p => p).Any(g => g.Count() > 1 && g.Key != PetKind.Normal);
                Check(run.Fortune == null, "New day starts without an active fortune");
                PetOffer(run, PetKind.Normal);
                Check(run.OpenPet(0), "Open weighted pet candidates");
                Check(run.PendingCandidates.Count == 3 && run.PendingCandidates.All(s => !run.Owns(s.Id)), "Pet candidates exclude owned students");
                Check(run.PendingCandidates.Select(s => s.Id).Distinct().Count() == 3, "Pet candidate uniqueness");
                foreach (var student in run.PendingCandidates) grades[student.Grade - 1]++;
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
            var run = SingleStudentRun(Economy(), 11);
            var once = PrototypeCatalog.Students.First(s => !s.UnlimitedUpgrade);
            run.Students.Clear();
            run.Students.Add(new StudentState(once));
            Check(run.Upgrade(0), "Once skill upgrades");
            Check(run.Students[0].SkillValue == once.ValueAtUpgrade(1), "Upgrade replaces rather than adds computed value");
            int money = run.Money;
            Check(!run.Upgrade(0) && run.Money == money && run.Students[0].UpgradeCount == 1, "Once upgrade cap prevents repeat payment");
            Check(run.Sell(0), "Sell upgraded student");
            Check(run.Money == money + run.Settings.SalePrice(once.Grade), "Sale pays grade price");
            PetOffer(run, PetKind.Normal);
            Check(run.OpenPet(0), "Pay pet to recruit released student");
            run.PendingCandidates.Clear(); run.PendingCandidates.Add(once);
            Check(run.ChoosePet(0), "Released student can be recruited again through pet");
            Check(run.Students[0].UpgradeCount == 0 && run.Students[0].SkillValue == once.InitialValue, "Recruiting again resets upgrades");

            var unlimited = new StudentDefinition { Id = "unlimited_fixture", Name = "강화 검사", Grade = 1,
                InitialValue = 100, UpgradeStep = 25, Action = ActionKind.AddDamage, UnlimitedUpgrade = true };
            run.Students.Clear(); run.Students.Add(new StudentState(unlimited));
            for (int i = 1; i <= 4; i++)
                Check(run.Upgrade(0) && run.Students[0].UpgradeCount == i && run.Students[0].SkillValue == unlimited.ValueAtUpgrade(i), "Unlimited replacement rule at level " + i);

            run.Students.Add(new StudentState(PrototypeCatalog.Students[1]));
            run.Students.Add(new StudentState(PrototypeCatalog.Students[2]));
            PetOffer(run, PetKind.Premium);
            Check(run.OpenPet(0), "Full team can pay for pet choice");
            int paid = run.Money;
            var chosen = run.PendingCandidates[0];
            Check(run.ChoosePet(0) && !run.PendingPet && run.Money == paid, "Full team may recruit without another charge");
            Check(run.Students.Count == 4 && !run.CanStartBattle, "Overflow blocks departure");
            int sale = run.Settings.SalePrice(run.Students[0].Definition.Grade);
            Check(run.Sell(0), "Replacement sale permitted during choice");
            Check(!run.PendingPet && run.Money == paid + sale, "School dismissal credits sale");
            Check(run.CanStartBattle && run.Owns(chosen.Id) && run.Students.Count == 3, "Dismissal resolves overflow");
        }

        private static void Capacity()
        {
            var run = SingleStudentRun(Economy(), 28);
            Check(run.BaseCapacity == 3 && run.Capacity == 3, "Initial capacity is three");
            for (int i = 0; i < 4; i++) Check(run.ExpandSlots(), "Expand ordinary slot");
            int money = run.Money;
            Check(run.BaseCapacity == 7 && run.Capacity == 7 && !run.ExpandSlots() && run.Money == money, "Ordinary capacity caps at seven without extra charge");
        }

        private static void RerollAndBattle()
        {
            var economy = Economy(); economy.bossBaseHp = 1f;
            var run = SingleStudentRun(economy, 55);
            int money = run.Money;
            Check(run.RerollCost == 20 && Roll(run), "First reroll succeeds at twenty");
            Check(run.Money == money - 20 && run.RerollCost == 40 && Roll(run), "Second reroll costs forty");
            Check(run.Money == money - 60 && run.RerollCost == 60, "Next reroll costs sixty");
            Check(run.StartBattle(), "Battle entry preserves today fortune");
            Check(!run.OpenPet(0) && !run.Sell(0) && !run.Reroll(), "Battle locks school transactions");
            var student = run.Students[0];
            run.ResolveNextTurn();
            Check(run.Battle.Won && !run.Battle.Lost, "Boss death wins before turn limit");
            money = run.Money;
            Check(run.ReturnToSchool() && !run.InBattle && run.Stage == 2, "Win returns to next stage school");
            Check(ReferenceEquals(student, run.Students[0]) && run.Money >= money, "School return preserves students and rewards");
            Check(run.Fortune == null && run.RerollCost == 20 && run.PetOffers.Count == 3, "Next day resets fortune and cost and keeps pets");

            var losingEconomy = Economy(); losingEconomy.bossBaseHp = 1000000f;
            var losing = SingleStudentRun(losingEconomy, 56);
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
            var empty = SingleStudentRun(Economy(), 57); empty.Students.Clear();
            Check(!empty.StartBattle(), "Empty team cannot enter battle");
        }

        private static void PetRefillAndFortune()
        {
            var economy = Economy(); economy.bossBaseHp = 1;
            var run = SingleStudentRun(economy, 502);
            var pets = run.PetOffers.ToArray();
            Check(run.Reroll() && run.IsFortuneRolling && pets.SequenceEqual(run.PetOffers), "Fortune roll preserves pets");
            int paid = run.Money, cost = run.RerollCost;
            Check(!run.Reroll() && !run.StartBattle() && !run.OpenPet(0) && !run.Upgrade(0) && !run.Sell(0), "Spinning locks transactions");
            Check(run.Money == paid && run.RerollCost == cost, "Double callbacks do not charge twice");
            Check(run.CompleteFortuneRoll() && run.Fortune != null && !run.IsFortuneRolling, "Result applied once");
            Check(!run.CompleteFortuneRoll(), "Repeated completion cannot duplicate grants");
            for (int i = 0; i < 3; i++)
            {
                pets = run.PetOffers.ToArray();
                Check(run.OpenPet(i), "Pets remain usable after fortune roll");
                Check(run.PetOffers[i].HasValue, "Used pet slot refills before choice completes");
                Check(Enumerable.Range(0, 3).Where(n => n != i).All(n => run.PetOffers[n] == pets[n]), "Refill preserves other pet slots");
                Check(!run.PetOffers.All(p => p == PetKind.Normal), "Refill never leaves three normal pets");
                paid = run.Money;
                var candidates = run.PendingCandidates.ToArray();
                pets = run.PetOffers.ToArray();
                Check(!run.OpenPet(i) && !run.OpenPet((i + 1) % 3) && run.Money == paid &&
                    candidates.SequenceEqual(run.PendingCandidates) && pets.SequenceEqual(run.PetOffers), "Pending selection prevents double charge and extra refill");
                Check(run.SkipPet(), "Finish paid choice without refund");
            }
            pets = run.PetOffers.ToArray();
            var fortune = run.Fortune;
            Check(!run.ReturnToSchool() && run.Fortune == fortune, "Invalid return preserves fortune");
            Check(run.StartBattle() && run.Fortune == fortune, "Battle keeps active fortune");
            run.ResolveNextTurn();
            Check(run.ReturnToSchool() && run.Fortune == null && run.RerollCost == 20, "Victory return resets day");
            Check(pets.SequenceEqual(run.PetOffers), "Returning from battle preserves unconsumed pets");
            Check(Roll(run) && run.Fortune != null, "Next day permits a new fortune");
            var empty = SingleStudentRun(Economy(0));
            pets = empty.PetOffers.ToArray();
            Check(!empty.OpenPet(0) && pets.SequenceEqual(empty.PetOffers), "Unaffordable pet does not refill");
            Check(!empty.Reroll() && !empty.IsFortuneRolling, "Failed payment never starts a roll");
            var exhausted = SingleStudentRun(Economy());
            exhausted.Students.Clear();
            exhausted.Students.AddRange(PrototypeCatalog.Students.Select(s => new StudentState(s)));
            pets = exhausted.PetOffers.ToArray(); paid = exhausted.Money;
            Check(!exhausted.OpenPet(0) && exhausted.Money == paid && pets.SequenceEqual(exhausted.PetOffers), "No eligible students means no payment or pet refill");
        }

        static bool Roll(PrototypeRun run) => run.Reroll() && run.CompleteFortuneRoll();

        static void SetFortune(PrototypeRun run, string id, int strength = 1)
        {
            typeof(PrototypeRun).GetMethod("ApplyFortune", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(run, new object[] { PrototypeCatalog.Fortunes.Single(f => f.Id == id), strength });
        }
        static void FortuneChecks()
        {
            float[] weights = { 2, 2, 1, 3, 3, 3, 3, 3, 5, 1, .5f, 1, .3f, 1, 1, .3f };
            Check(PrototypeCatalog.Fortunes.Count == 16 && PrototypeCatalog.Fortunes.Select(f => f.Weight).SequenceEqual(weights), "All sixteen authored weights preserved");
            Check(PrototypeCatalog.Fortunes.Count(f => f.IsBlank) == 6, "Exactly six blanks");
            var random = new System.Random(492);
            var counts = PrototypeCatalog.Fortunes.ToDictionary(f => f.Id, f => 0);
            for (int i = 0; i < 100000; i++) counts[PrototypeCatalog.DrawFortune(random, false).Id]++;
            foreach (var fortune in PrototypeCatalog.Fortunes)
                Check(Math.Abs(counts[fortune.Id] / 100000.0 - fortune.Weight / weights.Sum()) < .006, "Weighted frequency " + fortune.Id);
            for (int i = 0; i < 1000; i++) Check(!PrototypeCatalog.DrawFortune(random, true).IsBlank, "Cute filtering excludes blanks");

            var run = SingleStudentRun(Economy(), 123);
            SetFortune(run, "reroll"); Check(run.RerollCost == 10, "Fifty percent reroll discount");
            Check(run.Reroll(), "Discounted roll starts");
            Check(run.Money == run.Settings.startingMoney - 10 && run.RerollCost == 20, "Charge discounted current price and grow base price");
            run.CompleteFortuneRoll();
            SetFortune(run, "recruit");
            foreach (PetKind kind in Enum.GetValues(typeof(PetKind))) Check(run.PetPrice(kind) == run.Settings.PetPrice(kind) / 2, "All recruitment prices discounted");
            SetFortune(run, "recruit", 2); Check(run.PetPrice(PetKind.Premium) == 0, "Doubled discount makes recruitment free");
            SetFortune(run, "reroll", 2); Check(run.RerollCost == 0, "Doubled discount makes reroll free");
            SetFortune(run, "toe"); Check(run.RerollCost == 40 && run.PetPrice(PetKind.Normal) == 30, "Replacing fortune removes prior discounts");
            int money = run.Money;
            SetFortune(run, "energy", 2); Check(run.Money == money + 200, "Doubled energy granted immediately");
            Check(!run.CompleteFortuneRoll() && run.Money == money + 200, "No duplicate immediate grant");
            SetFortune(run, "toe"); Check(run.Money == money + 200, "Replacing fortune preserves granted energy");

            var once = PrototypeCatalog.Students.First(s => !s.UnlimitedUpgrade && s.UpgradeEnabled);
            var unlimited = new StudentDefinition { Id = "fortune-upgrade", Name = "fortune-upgrade", Grade = 1,
                Club = "none", Personality = "none", UnlimitedUpgrade = true, Action = ActionKind.AddDamage, InitialValue = 100, UpgradeStep = 25 };
            run.Students.Clear(); run.Students.Add(new StudentState(once)); run.Students.Add(new StudentState(unlimited));
            money = run.Money;
            SetFortune(run, "upgrade", 2);
            Check(run.Students[0].UpgradeCount == 1 && run.Students[1].UpgradeCount == 2 && run.Students[1].SkillValue == 150, "Double upgrades respect once-only cap");
            SetFortune(run, "toe");
            Check(run.Students[1].UpgradeCount == 2 && run.Money == money, "Free upgrade survives replacing fortune");
            run.Settings.bossBaseHp = 1;
            Check(run.StartBattle(), "Upgrade persistence battle starts"); run.ResolveNextTurn();
            Check(run.ReturnToSchool() && run.Students[1].UpgradeCount == 2, "Permanent upgrades survive day reset");

            foreach (int tier in new[] { 4, 5, 7, 9 })
            {
                var cute = SingleStudentRun(Economy()); cute.Students.Clear();
                for (int i = 0; i < tier; i++) cute.Students.Add(new StudentState(new StudentDefinition {
                    Id = "cute" + i, Club = "none", Personality = "귀염둥이", Grade = 1, Action = ActionKind.AddDamage }));
                Check(BattleSynergies.FortuneStrength(cute) == (tier == 5 || tier == 7 ? 2 : 1), "Highest cute tier determines fortune bonus " + tier);
                Check(BattleSynergies.KeepAllyConsumables(cute) == (tier == 7), "Seven keeps existing consumable protection");
                Check(BattleSynergies.AllCostsFree(cute) == (tier == 9), "Nine makes every cost free");
                if (tier == 9)
                {
                    Check(cute.RerollCost == 0 && cute.PetPrice(PetKind.Premium) == 0 && cute.UpgradePrice(3) == 0 && cute.ExpansionPrice == 0, "All costs free");
                    money = cute.Money;
                    Check(cute.ExpandSlots() && cute.Upgrade(0) && cute.OpenPet(0) && cute.Money == money, "Free transactions do not charge");
                    cute.SkipPet();
                }
                for (int i = 0; i < 12; i++)
                {
                    SetFortune(cute, "toe");
                    Check(Roll(cute), "Cute reroll completes");
                    if (tier == 5 || tier == 7) Check(!cute.Fortune.IsBlank && cute.FortuneStrength == 2, "Cute removes blanks and captures double strength");
                }
            }

            var synergy = SingleStudentRun(Economy());
            SetFortune(synergy, "synergies", 2);
            Check(BattleSynergies.Count(synergy, "운동부") == 3 && BattleSynergies.Count(synergy, "사진부") == 2, "All synergies includes absent kinds");
            Check(synergy.Students.Count == 1, "Virtual synergy does not add students");
            SetFortune(synergy, "toe"); Check(BattleSynergies.Count(synergy, "사진부") == 0, "Replacing fortune removes virtual counts");
            foreach (PetKind kind in Enum.GetValues(typeof(PetKind)))
            {
                SetFortune(synergy, "synergy_recruit"); PetOffer(synergy, kind);
                Check(synergy.OpenPet(0), "Fortune converts recruitment " + kind);
                Check(synergy.PendingCandidates.All(s => s.Club == "운동부") || synergy.PendingCandidates.All(s => s.Personality == "분위기메이커"), "Converted candidates share a held synergy");
                synergy.SkipPet();
            }

            foreach (int strength in new[] { 1, 2 })
            {
                var damage = SingleStudentRun(Economy()); damage.Students.Clear();
                damage.Students.Add(new StudentState(unlimited)); damage.Settings.bossBaseHp = 100000;
                SetFortune(damage, "damage", strength);
                Check(damage.StartBattle(), "Damage fortune starts battle"); damage.ResolveNextTurn();
                Check(damage.Battle.Damage == 100 * 2 * strength, "Final fortune damage multiplier");
                damage.ResolveNextTurn(); Check(damage.Battle.Damage == 100 * 2 * strength, "Damage fortune persists between turns without stacking");
                foreach (string id in new[] { "reward_1_5", "reward_2", "reward_10" })
                {
                    var rewardRun = SingleStudentRun(Economy(105)); rewardRun.Settings.bossBaseHp = 1;
                    SetFortune(rewardRun, id, strength);
                    Check(rewardRun.StartBattle(), "Reward battle starts"); rewardRun.ResolveNextTurn();
                    var reward = rewardRun.Battle.Reward;
                    float multiplier = PrototypeCatalog.Fortunes.Single(f => f.Id == id).Value * strength;
                    int expected = (int)Math.Floor(260 * multiplier);
                    Check(reward.Total == expected && reward.FortuneMultiplier == multiplier, "All reward components multiplied " + id);
                    Check(rewardRun.ReturnToSchool() && rewardRun.Money == 105 + expected, "Reward paid once before day reset");
                    Check(rewardRun.Fortune == null && rewardRun.FortuneRewardMultiplier == 1 && rewardRun.RerollCost == 20, "Next day clears effect and cost");
                    Check(!rewardRun.ReturnToSchool() && rewardRun.Money == 105 + expected, "Duplicate reward claim rejected");
                }
            }
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
            var run = SingleStudentRun(economy, 150);
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

        private static void DocumentedDamageActions()
        {
            var minji = Actor("Girl6", ActionKind.TeamSizeDamage, 100);
            var hanbyeol = Actor("Girl9", ActionKind.TeamSizeDamage, 100);
            var team = ControlledBattle(100000, minji, hanbyeol, Actor("helper", ActionKind.AddDamage, 0));
            team.ResolveNextTurn();
            Check(team.Battle.Damage == 600, "Each team-size skill adds 100 per current team member, including self");

            var nayeon = Actor("Girl10", ActionKind.AddThenPercentDamage, 0);
            nayeon.Definition.InitialPercent = 10;
            var percent = ControlledBattle(100000, Actor("seed", ActionKind.AddDamage, 250), nayeon);
            percent.ResolveNextTurn();
            Check(percent.Battle.Damage == 275, "Good Cop adds ten percent of accumulated damage");

            var dahye = Actor("Girl11", ActionKind.AddThenPercentDamage, 50);
            dahye.Definition.InitialPercent = 10;
            dahye.Definition.UpgradeStep = 50;
            dahye.Definition.PercentUpgradeStep = 5;
            var venom = ControlledBattle(100000, Actor("seed", ActionKind.AddDamage, 150), dahye);
            var values = new List<float>(); venom.DamageChanged += values.Add;
            venom.ResolveNextTurn();
            Check(values.SequenceEqual(new[] { 0f, 150f, 200f, 220f }), "Venom adds flat damage before current-damage percentage and reports both changes");
            dahye.UpgradeCount = 1;
            dahye.SkillValue = dahye.Definition.ValueAtUpgrade(1);
            venom.ResolveNextTurn();
            Check(venom.Battle.Damage == 287.5f, "Upgraded Venom applies 100 then fifteen percent");
        }

        private static void DamageFormatting()
        {
            Check(PrototypeCatalog.FormatDamage(1234.75f) == "1 234", "Damage display truncates decimals and groups digits");
            Check(PrototypeCatalog.FormatDamage(1e11f) == "1.00e11", "Large float display has two decimal places");
            Check(PrototypeCatalog.FormatDamage(2.05e11f) == "2.05e11", "Scientific format avoids int32 overflow");
        }
    }
}
