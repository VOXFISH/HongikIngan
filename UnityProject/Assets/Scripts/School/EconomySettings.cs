using UnityEngine;

namespace HongikIngan
{
    [CreateAssetMenu(menuName = "HongikIngan/Economy Settings")]
    public sealed class EconomySettings : ScriptableObject
    {
        [Header("Prototype balance - edit this asset")]
        public int startingMoney = 100;
        public int[] recruitPrices = { 30, 60, 90 };
        public int[] upgradePrices = { 20, 40, 60 };
        public int[] salePrices = { 15, 30, 45 };
        public int[] petPrices = { 30, 50, 100, 60 };
        public int relicPrice = 30;
        public int rerollInitial = 20;
        public int rerollIncrease = 20;
        public int slotExpansionPrice = 40;
        public int victoryReward = 70;
        public int victoryRewardPerStage = 15;
        public float bossBaseHp = 450f;
        public float bossGrowth = 1.65f;
        public float polaroidDamage = 10f;

        public int RecruitPrice(int grade) => Price(recruitPrices, grade);
        public int UpgradePrice(int grade) => Price(upgradePrices, grade);
        public int SalePrice(int grade) => Price(salePrices, grade);
        public int PetPrice(PetKind kind)
        {
            int index = (int)kind;
            if (petPrices == null || index < 0 || index >= petPrices.Length) return int.MaxValue;
            return Mathf.Max(0, petPrices[index]);
        }
        private static int Price(int[] prices, int grade)
        {
            // An incomplete designer array must never silently make a transaction free.
            return prices != null && prices.Length >= grade && grade > 0 ? Mathf.Max(0, prices[grade - 1]) : int.MaxValue;
        }
    }
}
