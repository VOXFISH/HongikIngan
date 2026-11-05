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
        public readonly List<StudentDefinition> RecruitOffers = new List<StudentDefinition>();
        public readonly List<RelicDefinition> RelicOffers = new List<RelicDefinition>();
        public readonly List<PetKind?> PetOffers = new List<PetKind?>();
        public readonly List<StudentDefinition> PendingCandidates = new List<StudentDefinition>();
        public readonly List<RelicDefinition> Relics = new List<RelicDefinition>();
        public readonly Dictionary<string, int> Photos = new Dictionary<string, int>();
        public IReadOnlyList<StudentDefinition> Catalog => PrototypeCatalog.Students;
        public int Money { get; private set; }
        public int Stage { get; private set; } = 1;
        public int BaseCapacity { get; private set; } = 3;
        public int Capacity => Math.Min(9, BaseCapacity + Relics.Where(r => r.Kind == RelicKind.ExtraSlots).Sum(r => (int)r.Value));
        public int RerollCost { get; private set; }
        public bool PendingPet { get; private set; }
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
            Students.Add(new StudentState(PrototypeCatalog.Students[0]));
            RefreshOffers();
        }

        private bool Fail(string message) { LastMessage = message; Changed?.Invoke(); return false; }
        private bool Done(string message) { LastMessage = message; Changed?.Invoke(); return true; }
        private bool SchoolAvailable => !InBattle && !GameOver;
        private bool SchoolUnlocked => SchoolAvailable && !PendingPet;
        public bool Owns(string id) => Students.Any(s => s.Definition.Id == id);

        public bool BuyRecruit(int index)
        {
            if (!SchoolUnlocked) return Fail("먼저 진행 중인 선택을 마쳐 주세요.");
            if (index < 0 || index >= RecruitOffers.Count || RecruitOffers[index] == null) return Fail("이미 구매했거나 없는 후보입니다.");
            var definition = RecruitOffers[index];
            if (Owns(definition.Id)) return Fail("이미 영입한 학생입니다.");
            if (Students.Count >= Capacity) return Fail("빈자리가 없습니다. 교체 버튼으로 계약을 해제해 주세요.");
            int price = Settings.RecruitPrice(definition.Grade);
            if (Money < price) return Fail("미라클 에너지가 부족합니다.");
            Money -= price;
            Students.Add(new StudentState(definition));
            RecruitOffers[index] = null;
            return Done(definition.Name + " 영입 완료!");
        }

        public bool BuyRelic(int index)
        {
            if (!SchoolUnlocked) return Fail("먼저 진행 중인 선택을 마쳐 주세요.");
            if (index < 0 || index >= RelicOffers.Count || RelicOffers[index] == null) return Fail("이미 구매했거나 없는 유물입니다.");
            var relic = RelicOffers[index];
            if (Relics.Any(r => r.Id == relic.Id)) return Fail("이미 획득한 유물입니다.");
            int price = Math.Max(0, Settings.relicPrice);
            if (Money < price) return Fail("미라클 에너지가 부족합니다.");
            Money -= price;
            Relics.Add(relic);
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
            PetOffers[index] = null;
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
            // Purchased recruit offers cannot offer a duplicate now acquired via a pet.
            for (int i = 0; i < RecruitOffers.Count; i++) if (RecruitOffers[i]?.Id == chosen.Id) RecruitOffers[i] = null;
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
            int price = Settings.SalePrice(student.Definition.Grade);
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
            if (BaseCapacity >= 7) return Fail("기본 슬롯 확장은 최대 7칸입니다.");
            int price = Math.Max(0, Settings.slotExpansionPrice);
            if (Money < price) return Fail("미라클 에너지가 부족합니다.");
            Money -= price; BaseCapacity++;
            return Done("책상 한 칸을 추가했습니다.");
        }

        public bool Reroll()
        {
            if (!SchoolUnlocked) return Fail("펫 선택을 마친 후 리롤할 수 있습니다.");
            if (Money < RerollCost) return Fail("리롤 비용이 부족합니다.");
            long nextCost = (long)RerollCost + Math.Max(0, Settings.rerollIncrease);
            if (nextCost > int.MaxValue) return Fail("리롤 비용의 표현 범위를 초과했습니다.");
            Money -= RerollCost;
            RerollCost = (int)nextCost;
            RefreshOffers();
            return Done("모집·유물·펫 제안을 새로 뽑았습니다.");
        }

        private void RefreshOffers()
        {
            RecruitOffers.Clear(); RecruitOffers.AddRange(DrawStudents(AvailableStudents(), 3));
            var relicPool = PrototypeCatalog.Relics.Where(r => !Relics.Any(owned => owned.Id == r.Id)).ToList();
            RelicOffers.Clear();
            while (RelicOffers.Count < 3 && relicPool.Count > 0)
            {
                int index = random.Next(relicPool.Count);
                RelicOffers.Add(relicPool[index]); relicPool.RemoveAt(index);
            }
            do
            {
                PetOffers.Clear();
                for (int i = 0; i < 3; i++)
                {
                    int roll = random.Next(100);
                    PetOffers.Add(roll < 40 ? PetKind.Normal : roll < 70 ? PetKind.Advanced : roll < 80 ? PetKind.Premium : PetKind.Synergy);
                }
            } while (PetOffers.TrueForAll(p => p == PetKind.Normal));
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
            if (!SchoolUnlocked) return Fail("펫 선택을 먼저 마쳐 주세요.");
            if (Students.Count == 0) return Fail("마법소녀를 한 명 이상 영입해 주세요.");
            RerollCost = Math.Max(0, Settings.rerollInitial);
            float hp = Settings.bossBaseHp * (float)Math.Pow(Settings.bossGrowth, Stage - 1);
            if (float.IsInfinity(hp) || hp <= 0) hp = float.MaxValue;
            Battle = new BattleState(hp);
            InBattle = true;
            return Done("방과 후! 최대 3턴 안에 보스를 처치하세요.");
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
            int reward = Settings.victoryReward + Settings.victoryRewardPerStage * (Stage - 1) + Relics.Where(r => r.Kind == RelicKind.VictoryMoney).Sum(r => (int)r.Value);
            Money += Math.Max(0, reward);
            Stage++;
            InBattle = false;
            foreach (var student in Students) student.Keywords.Clear();
            Battle = null;
            RefreshOffers();
            return Done("보스 처치 보상 +" + reward + ". 다음 스테이지를 준비하세요!");
        }

        internal void AddMoney(int amount) { Money = checked(Money + Math.Max(0, amount)); }
        internal void NotifyDamage() { DamageChanged?.Invoke(Battle.Damage); }
    }
}
