using System;
using System.Linq;
using UnityEngine;

namespace HongikIngan
{
    public static class SettlementChecks
    {
        static int assertions;
        static void Check(bool condition, string message)
        { assertions++; if (!condition) throw new InvalidOperationException("SETTLEMENT_CHECKS_FAIL: " + message); }

        public static void Run()
        {
            assertions = 0;
            var settings = ScriptableObject.CreateInstance<EconomySettings>();
            try
            {
                settings.startingMoney = 105; settings.bossBaseHp = 1;
                for (int winningTurn = 1; winningTurn <= 3; winningTurn++)
                {
                    settings.bossBaseHp = winningTurn;
                    var run = new PrototypeRun(settings);
                    run.Students.Clear();
                    run.Students.Add(new StudentState(new StudentDefinition {
                        Id = "reward-test", Name = "reward-test", Club = "none", Personality = "none",
                        Grade = 1, Action = ActionKind.AddDamage, InitialValue = 1
                    }));
                    Check(run.StartBattle(), "battle begins");
                    for (int turn = 1; turn <= winningTurn; turn++) run.ResolveNextTurn();
                    var battle = run.Battle;
                    Check(battle.Won && !battle.Lost, "victory takes priority on last turn");
                    Check(battle.Reward.EnergyBeforeSettlement == 105 && battle.Reward.Interest == 10, "interest snapshots and truncates");
                    int expected = 50 + (3 - winningTurn) * 100 + 10;
                    Check(battle.Reward.Total == expected && run.Money == 105, "pending reward is not paid early");
                    typeof(PrototypeRun).GetMethod("AddMoney", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        .Invoke(run, new object[] { 100 });
                    Check(run.VictoryReward == expected, "later income cannot change settlement interest");
                    Check(run.ReturnToSchool() && run.Money == 205 + expected, "claim pays once and returns to school");
                    Check(battle.Reward.Claimed && !run.ReturnToSchool() && run.Money == 205 + expected, "duplicate claim rejected");
                }
                settings.bossBaseHp = 1;
                var loss = new PrototypeRun(settings);
                loss.Students.Clear();
                loss.Students.Add(new StudentState(new StudentDefinition {
                    Id = "zero", Club = "none", Personality = "none", Grade = 1, Action = ActionKind.AddDamage
                }));
                loss.StartBattle();
                for (int i = 0; i < 3; i++) loss.ResolveNextTurn();
                Check(loss.GameOver && loss.Battle.Reward == null && loss.Money == 105 && !loss.ReturnToSchool(), "game over never grants settlement");
                Debug.Log("SETTLEMENT_CHECKS_PASS assertions=" + assertions);
            }
            finally { UnityEngine.Object.DestroyImmediate(settings); }
        }
    }
}
