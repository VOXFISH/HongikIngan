using System;
using System.Linq;
using UnityEngine;
namespace HongikIngan
{
    public static class BattleResultChecks
    {
        static int count;
        static void Check(bool ok,string message){count++;if(!ok)throw new InvalidOperationException("RESULT_CHECKS_FAIL "+message);}
        public static void Run()
        {
            count=0;
            foreach(int energy in new[]{0,105,1000})foreach(int turns in new[]{0,2,4})foreach(float multiplier in new[]{1f,1.5f,10f})
            {
                var reward=new BattleReward(energy,turns,multiplier,amount=>(int)Math.Floor(amount*1.5));
                var parts=BattleRewardPopup.SettlementParts(reward);
                Check(parts.Length==3&&parts.All(n=>n>=0)&&parts.Sum()==reward.Total,"three payouts preserve rounded total");
                Check(reward.EnergyBeforeSettlement==energy&&!reward.Claimed,"presentation allocation does not claim");
            }
            var settings=ScriptableObject.CreateInstance<EconomySettings>();settings.bossBaseHp=1;settings.startingMoney=105;
            try
            {
                var run=new PrototypeRun(settings);run.Students.Clear();
                run.Students.Add(new StudentState(new StudentDefinition{Id="result",Name="결과 검증",Club="none",Personality="none",Grade=1,Action=ActionKind.AddDamage,InitialValue=100}));
                run.StartBattle();run.ResolveNextTurn();
                Check(run.CumulativeDamage==100,"score includes final attack and overkill once");
                Check(run.Money==105&&!run.Battle.Reward.Claimed,"result is pending");
                int payout=run.Battle.Reward.Total;Check(run.ReturnToSchool()&&run.Money==105+payout,"confirm uses existing settlement");
                Check(run.CumulativeDamage==100&&!run.ReturnToSchool(),"score survives school and duplicate claim rejected");
                run.StartBattle();run.ResolveNextTurn();Check(run.CumulativeDamage==200,"score survives stages");
                var fresh=new PrototypeRun(settings);Check(fresh.CumulativeDamage==0,"new game clears score");
                var coffee=new PrototypeRun(settings);coffee.Students.Clear();coffee.Students.Add(run.Students[0]);coffee.StartBattle();
                KeywordRules.Add(coffee.Battle.BossKeywords,KeywordRules.Coffee,1);coffee.ResolveNextTurn();
                Check(coffee.CumulativeDamage==1,"direct coffee damage contributes without phantom beam");
            }
            finally { UnityEngine.Object.DestroyImmediate(settings); }
            Debug.Log("RESULT_CHECKS_PASS assertions="+count);
        }
    }
}
