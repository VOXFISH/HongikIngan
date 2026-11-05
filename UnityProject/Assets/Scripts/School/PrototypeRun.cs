using System;
using System.Collections.Generic;
using System.Linq;

namespace HongikIngan
{
    /// <summary>Scene-independent run state. Prices and sample balance live in EconomySettings.</summary>
    public sealed class PrototypeRun
    {
        private readonly Random random;
        public readonly EconomySettings Settings;
        public readonly List<StudentState> Students = new List<StudentState>();
        // School desk positions are independent of the compact list used by combat.
        // Keeping empty desks out of Students avoids null participants in combat/transactions.
        private readonly Dictionary<StudentState, int> schoolDesks = new Dictionary<StudentState, int>();
        public readonly List<RelicDefinition> RelicOffers = new List<RelicDefinition>();
        public readonly List<PetKind?> PetOffers = new List<PetKind?>();
        public readonly List<StudentDefinition> PendingCandidates = new List<StudentDefinition>();
        public readonly List<RelicDefinition> Relics = new List<RelicDefinition>();
        public readonly Dictionary<string, int> Photos = new Dictionary<string, int>();
        public IReadOnlyList<StudentDefinition> Catalog => PrototypeCatalog.Students;
        public int Money { get; private set; }
        public int Stage { get; private set; } = 1;
        public int BaseCapacity { get; private set; } = 3;
        public int Capacity => Math.Min(7, BaseCapacity + Relics.Where(r => r.Kind == RelicKind.ExtraSlots).Sum(r => (int)r.Value * BattleSynergies.RelicApplications(this, r)));
        public float Atmosphere { get; internal set; }
        public int RerollCost { get; private set; }
        public bool PendingPet { get; private set; }
        public bool RelicPurchasedThisBattle { get; private set; }
        public bool CanRerollRelic => SchoolUnlocked && !RelicPurchasedThisBattle && RelicOffers.Any(r => r != null);
        public bool InBattle { get; private set; }
        public bool GameOver => Battle != null && Battle.Lost;
        public string LastMessage { get; private set; } = "교실에 오신 것을 환영합니다.";
        public BattleState Battle { get; private set; }
        public int PhotoCount => Photos.Values.Sum();
        public event Action Changed;
        public event Action<float> DamageChanged;

        public PrototypeRun(EconomySettings settings, int seed = 1234)
        {
            Settings = settings != null ? settings : throw new ArgumentNullException(nameof(settings));
            random = new Random(seed);
            Money = Math.Max(0, settings.startingMoney);
            RerollCost = Math.Max(0, settings.rerollInitial);
            // Reuse pet grade weighting (4:2) while excluding third-years and duplicates.
            var starters = DrawStudents(PrototypeCatalog.Students.Where(s => s.Grade == 1 || s.Grade == 2).ToList(), 3);
            Students.AddRange(starters.Select(s => new StudentState(s)));
            RefreshOffers();
        }

        private bool Fail(string message) { LastMessage = message; Changed?.Invoke(); return false; }
        private bool Done(string message) { LastMessage = message; Changed?.Invoke(); return true; }
        private bool SchoolAvailable => !InBattle && !GameOver;
        private bool SchoolUnlocked => SchoolAvailable && !PendingPet;
        public bool CanStartBattle => StartBattleBlockReason == null;
        public string StartBattleBlockReason => !SchoolUnlocked ? "펫 선택을 먼저 마쳐 주세요." :
            Students.Count == 0 ? "마법소녀를 한 명 이상 영입해 주세요." : null;
        public int VictoryRewardBase => Settings.victoryReward;
        public int VictoryRewardStageBonus => Settings.victoryRewardPerStage * (Stage - 1);
        public int VictoryRewardRelicBonus => Relics.Where(r => r.Kind == RelicKind.VictoryMoney)
            .Sum(r => (int)r.Value * BattleSynergies.RelicApplications(this, r));
        public int VictoryReward => BattleSynergies.Income(this, VictoryRewardBase + VictoryRewardStageBonus + VictoryRewardRelicBonus);
        public bool Owns(string id) => Students.Any(s => s.Definition.Id == id);

        public bool BuyRelic(int index)
        {
            if (!SchoolUnlocked) return Fail("먼저 진행 중인 선택을 마쳐 주세요.");
            if (RelicPurchasedThisBattle) return Fail("이번 전투 준비에서는 이미 유물을 구매했습니다.");
            if (index < 0 || index >= RelicOffers.Count || RelicOffers[index] == null) return Fail("이미 구매했거나 없는 유물입니다.");
            var relic = RelicOffers[index];
            if (Relics.Any(r => r.Id == relic.Id)) return Fail("이미 획득한 유물입니다.");
            int price = Math.Max(0, Settings.relicPrice);
            if (Money < price) return Fail("미라클 에너지가 부족합니다.");
            Money -= price;
            Relics.Add(relic);
            RelicPurchasedThisBattle = true;
            RelicOffers[index] = null;
            return Done(relic.Name + " 획득!");
        }

        public bool OpenPet(int index)
        {
            if (!SchoolUnlocked) return Fail("먼저 진행 중인 선택을 마쳐 주세요.");
            if (index < 0 || index >= PetOffers.Count || !PetOffers[index].HasValue) return Fail("이미 사용했거나 없는 펫입니다.");
            PetKind kind = PetOffers[index].Value;
            int price = Settings.PetPrice(kind);
            if (Money < price) return Fail("미라클 에너지가 부족합니다.");
            var pool = AvailableStudents();
            if (pool.Count == 0) return Fail("새로 영입할 학생 후보가 없습니다.");
            // Generate privately, then charge before exposing candidates to the UI.
            if (kind == PetKind.Synergy) pool = SynergyPool(pool);
            var candidates = DrawStudents(pool, kind == PetKind.Advanced || kind == PetKind.Premium ? 5 : 3);
            Money -= price;
            RefillPet(index);
            PendingCandidates.Clear();
            PendingCandidates.AddRange(candidates);
            PendingPet = true;
            return Done("결제 완료. 한 명을 영입하거나 포기하세요. 환불되지 않습니다.");
        }

        public bool ChoosePet(int index)
        {
            if (!SchoolAvailable || !PendingPet) return Fail("진행 중인 펫 선택이 없습니다.");
            if (index < 0 || index >= PendingCandidates.Count) return Fail("없는 후보입니다.");
            var chosen = PendingCandidates[index];
            if (Owns(chosen.Id)) return Fail("이미 영입한 학생입니다.");
            if (Students.Count >= Capacity) return Fail("교체 버튼에서 학생을 계약 해제한 후 선택하세요.");
            Students.Add(new StudentState(chosen));
            CompletePet(chosen.Id);
            return Done(chosen.Name + " 영입! 나머지 후보의 폴라로이드를 받았습니다.");
        }

        public bool SkipPet()
        {
            if (!SchoolAvailable || !PendingPet) return Fail("진행 중인 펫 선택이 없습니다.");
            CompletePet(null);
            return Done("영입을 포기했습니다. 환불 없이 모든 후보의 폴라로이드를 받았습니다.");
        }

        private void CompletePet(string chosenId)
        {
            foreach (var student in PendingCandidates)
            {
                if (student.Id == chosenId) continue;
                Photos.TryGetValue(student.Id, out int count);
                Photos[student.Id] = checked(count + 1);
            }
            PendingCandidates.Clear();
            PendingPet = false;
        }

        // During a pet choice this is the explicit replacement panel operation.
        public bool Sell(int index)
        {
            if (!SchoolAvailable) return Fail("교실에서만 계약을 해제할 수 있습니다.");
            if (index < 0 || index >= Students.Count) return Fail("없는 학생입니다.");
            var student = Students[index];
            int price;
            try { price = BattleSynergies.Income(this, Settings.SalePrice(student.Definition.Grade)); }
            catch (OverflowException) { return Fail("판매 가격 설정 또는 재화 범위를 확인해 주세요."); }
            if (price == int.MaxValue || (long)Money + price > int.MaxValue) return Fail("판매 가격 설정 또는 재화 범위를 확인해 주세요.");
            Money += price;
            Students.RemoveAt(index);
            return Done(student.Definition.Name + " 계약 해제. 재영입 시 강화는 초기화됩니다.");
        }

        public bool Upgrade(int index)
        {
            if (!SchoolUnlocked) return Fail("먼저 진행 중인 선택을 마쳐 주세요.");
            if (index < 0 || index >= Students.Count) return Fail("없는 학생입니다.");
            var student = Students[index];
            if (!student.Definition.UpgradeEnabled) return Fail("강화 수치가 아직 정해지지 않은 스킬입니다.");
            if (!student.Definition.UnlimitedUpgrade && student.UpgradeCount >= 1) return Fail("1회 강화가 이미 완료되었습니다.");
            int price = Settings.UpgradePrice(student.Definition.Grade);
            if (Money < price) return Fail("미라클 에너지가 부족합니다.");
            float replacement = student.Definition.ValueAtUpgrade(student.UpgradeCount + 1);
            if (float.IsNaN(replacement) || float.IsInfinity(replacement)) return Fail("강화 수치의 표현 범위를 초과했습니다.");
            Money -= price;
            student.UpgradeCount++;
            student.SkillValue = replacement;
            return Done(student.Definition.Name + " 보충 학습 완료!");
        }

        private void EnsureSchoolDesks()
        {
            foreach (var student in schoolDesks.Keys.Where(s => !Students.Contains(s)).ToList())
                schoolDesks.Remove(student);
            foreach (var student in Students)
            {
                if (schoolDesks.ContainsKey(student)) continue;
                int desk = 0;
                while (schoolDesks.ContainsValue(desk)) desk++;
                schoolDesks.Add(student, desk);
            }
        }

        public StudentState StudentAtSchoolDesk(int desk)
        {
            EnsureSchoolDesks();
            return Students.FirstOrDefault(s => schoolDesks[s] == desk);
        }

        public bool MoveSchoolStudent(int fromDesk, int toDesk)
        {
            if (!SchoolUnlocked) return Fail("지금은 교실 배치를 바꿀 수 없습니다.");
            if (fromDesk < 0 || toDesk < 0 || fromDesk >= Capacity || toDesk >= Capacity)
                return Fail("확장된 책상 안에서 이동해 주세요.");
            var moving = StudentAtSchoolDesk(fromDesk);
            if (moving == null) return Fail("옮길 학생이 없습니다.");
            if (fromDesk == toDesk) return true;
            var target = StudentAtSchoolDesk(toDesk);
            schoolDesks[moving] = toDesk;
            if (target != null) schoolDesks[target] = fromDesk;
            return Done(target == null ? "빈 책상으로 이동했습니다." : "배치 순서를 교체했습니다.");
        }

        public bool Swap(int first, int second)
        {
            if (PendingPet || GameOver || (Battle != null && Battle.IsResolving)) return Fail("지금은 배치를 바꿀 수 없습니다.");
            if (first < 0 || second < 0 || first >= Students.Count || second >= Students.Count) return Fail("없는 자리입니다.");
            if (first == second) return true;
            var temp = Students[first]; Students[first] = Students[second]; Students[second] = temp;
            return Done("배치 순서를 교체했습니다.");
        }

        public bool ExpandSlots()
        {
            if (!SchoolUnlocked) return Fail("먼저 진행 중인 선택을 마쳐 주세요.");
            if (Capacity >= 7) return Fail("일반 전대는 최대 7명입니다. 추가 두 칸은 복제 분신 전용입니다.");
            int price = Math.Max(0, Settings.slotExpansionPrice);
            if (Money < price) return Fail("미라클 에너지가 부족합니다.");
            Money -= price; BaseCapacity++;
            return Done("책상 한 칸을 추가했습니다.");
        }

        public bool Reroll()
        {
            if (!SchoolUnlocked) return Fail("펫 선택을 마친 후 리롤할 수 있습니다.");
            if (RelicPurchasedThisBattle) return Fail("유물 구매 후에는 다음 전투까지 리롤할 수 없습니다.");
            if (!RelicOffers.Any(r => r != null)) return Fail("새로 제시할 유물이 없습니다.");
            if (Money < RerollCost) return Fail("리롤 비용이 부족합니다.");
            long nextCost = (long)RerollCost + Math.Max(0, Settings.rerollIncrease);
            if (nextCost > int.MaxValue) return Fail("리롤 비용의 표현 범위를 초과했습니다.");
            Money -= RerollCost;
            RerollCost = (int)nextCost;
            RefreshRelicOffer();
            return Done("유물 제안을 새로 뽑았습니다.");
        }

        private void RefreshOffers()
        {
            RefreshRelicOffer();
            // Returning from battle does not replace unused pets.
            if (PetOffers.Count > 0) return;
            do
            {
                PetOffers.Clear();
                for (int i = 0; i < 3; i++) PetOffers.Add(DrawPet());
            } while (PetOffers.TrueForAll(p => p == PetKind.Normal));
        }

        private void RefreshRelicOffer()
        {
            var previous = RelicOffers.FirstOrDefault(r => r != null);
            RelicOffers.Clear();
            if (RelicPurchasedThisBattle) return;
            var pool = PrototypeCatalog.Relics.Where(r => !Relics.Any(owned => owned.Id == r.Id)).ToList();
            // With multiple eligible relics a paid reroll must offer a different one.
            if (pool.Count > 1 && previous != null) pool.RemoveAll(r => r.Id == previous.Id);
            if (pool.Count > 0) RelicOffers.Add(pool[random.Next(pool.Count)]);
        }

        private PetKind DrawPet()
        {
            int roll = random.Next(100);
            return roll < 40 ? PetKind.Normal : roll < 70 ? PetKind.Advanced : roll < 80 ? PetKind.Premium : PetKind.Synergy;
        }

        private void RefillPet(int index)
        {
            // Preserve the other two slots, including when preventing three normal pets.
            do { PetOffers[index] = DrawPet(); }
            while (PetOffers.Count == 3 && PetOffers.TrueForAll(p => p == PetKind.Normal));
        }

        private List<StudentDefinition> AvailableStudents() => PrototypeCatalog.Students.Where(s => !Owns(s.Id)).ToList();

        private List<StudentDefinition> SynergyPool(List<StudentDefinition> available)
        {
            var weights = new Dictionary<string, int>();
            foreach (var student in Students)
            {
                foreach (var synergy in new[] { student.Definition.Club, student.Definition.Personality })
                {
                    if (!available.Any(s => s.Club == synergy || s.Personality == synergy)) continue;
                    weights.TryGetValue(synergy, out int count); weights[synergy] = count + 1;
                }
            }
            if (weights.Count == 0) return available;
            int roll = random.Next(weights.Values.Sum());
            foreach (var pair in weights)
            {
                roll -= pair.Value;
                if (roll < 0) return available.Where(s => s.Club == pair.Key || s.Personality == pair.Key).ToList();
            }
            return available;
        }

        private List<StudentDefinition> DrawStudents(List<StudentDefinition> pool, int count)
        {
            pool = new List<StudentDefinition>(pool);
            var result = new List<StudentDefinition>();
            double[] weights = { 4, 2, .5 };
            while (result.Count < count && pool.Count > 0)
            {
                double total = 0;
                for (int grade = 1; grade <= 3; grade++) if (pool.Any(s => s.Grade == grade)) total += weights[grade - 1];
                double roll = random.NextDouble() * total;
                int selectedGrade = 1;
                for (int grade = 1; grade <= 3; grade++)
                {
                    if (!pool.Any(s => s.Grade == grade)) continue;
                    selectedGrade = grade; roll -= weights[grade - 1]; if (roll < 0) break;
                }
                var candidates = pool.Where(s => s.Grade == selectedGrade).ToList();
                var selected = candidates[random.Next(candidates.Count)];
                result.Add(selected); pool.Remove(selected);
            }
            return result;
        }

        public bool StartBattle()
        {
            if (!CanStartBattle) return Fail(StartBattleBlockReason);
            EnsureSchoolDesks();
            Students.Sort((first, second) => schoolDesks[first].CompareTo(schoolDesks[second]));
            RerollCost = Math.Max(0, Settings.rerollInitial);
            float hp = Settings.bossBaseHp * (float)Math.Pow(Settings.bossGrowth, Stage - 1);
            if (float.IsInfinity(hp) || hp <= 0) hp = float.MaxValue;
            Battle = new BattleState(hp);
            InBattle = true;
            return Done("방과 후! 최대 3턴 안에 보스를 처치하세요.");
        }

        internal StudentState CloneNext(StudentState source)
        {
            int index = Students.IndexOf(source);
            if (source.IsClone || index < 0 || index + 1 >= Students.Count || Students.Count >= 9) return null;
            var target = Students[index + 1];
            var clone = new StudentState(target.Definition)
            {
                UpgradeCount = target.UpgradeCount, SkillValue = target.SkillValue,
                IsClone = true, CloneLastsBattle = source.UpgradeCount > 0
            };
            foreach (var keyword in target.Keywords)
            {
                if (keyword.Values.Count == 0) continue;
                var copy = new KeywordState(keyword.Definition, keyword.Values[0]);
                copy.Values.Clear(); copy.Values.AddRange(keyword.Values);
                clone.Keywords.Add(copy);
            }
            Students.Insert(index + 2, clone);
            source.PrecisionCopies++;
            return clone;
        }

        internal void RemoveExpiredClones(bool battleEnded)
        {
            Students.RemoveAll(s => s.IsClone && (battleEnded || !s.CloneLastsBattle));
        }

        public IEnumerable<BattleStep> ResolveTurn()
        {
            if (!InBattle || Battle == null || Battle.IsResolving || Battle.Won || Battle.Lost) yield break;
            foreach (var step in CombatEngine.ResolveTurn(this))
            {
                Changed?.Invoke();
                yield return step;
            }
            Changed?.Invoke();
        }

        public List<BattleStep> ResolveNextTurn() => ResolveTurn().ToList();

        public bool ReturnToSchool()
        {
            if (!InBattle || Battle == null || !Battle.Won || Battle.IsResolving) return Fail("보스 처치 후 교실로 돌아갈 수 있습니다.");
            int reward = VictoryRewardBase + VictoryRewardStageBonus + VictoryRewardRelicBonus;
            int earned = BattleSynergies.Income(this, reward);
            AddMoney(reward);
            BattleSynergies.FinishBattle(this);
            Stage++;
            InBattle = false;
            RemoveExpiredClones(true);
            foreach (var student in Students) student.Keywords.Clear();
            Battle = null;
            RelicPurchasedThisBattle = false;
            RefreshOffers();
            return Done("보스 처치 보상 +" + earned + ". 다음 스테이지를 준비하세요!");
        }

        internal void AddMoney(int amount) { Money = checked(Money + BattleSynergies.Income(this, amount)); }
        internal bool RollCombatChance(float probability) => random.NextDouble() < probability;
        internal void NotifyDamage() { DamageChanged?.Invoke(Battle.Damage); }
    }
}
