using System;
using UnityEngine;

namespace HongikIngan.Editor
{
    public static class PracticeBattleChecks
    {
        static int assertions;
        public static void BuildAndCheck()
        {
            assertions = 0;
            PrototypeChecks.Run();
            var settings = ScriptableObject.CreateInstance<EconomySettings>();
            try
            {
                var run = new PrototypeRun(settings) { PracticeBattles = true };
                Check(run.StartBattle(), "start practice battle");
                Check(float.IsPositiveInfinity(run.Battle.BossHp), "infinite starting HP");
                Check(!run.ReturnFromPracticeBattle(), "cannot return on turn zero");
                run.Students[0].SkillValue = float.PositiveInfinity;
                for (int turn = 1; turn <= 3; turn++)
                {
                    run.ResolveNextTurn();
                    Check(run.Battle.Turn == turn, "turn number");
                    Check(float.IsPositiveInfinity(run.Battle.BossHp) && !float.IsNaN(run.Battle.BossHp), "infinite damage cannot corrupt HP");
                    Check(!run.Battle.Won && !run.Battle.Lost, "practice never wins or loses");
                    Check(run.CanReturnFromPractice == (turn == 3), "return threshold");
                    if (turn < 3) Check(!run.ReturnFromPracticeBattle(), "early return rejected");
                }
                Check(run.ResolveNextTurn().Count == 0 && run.Battle.Turn == 3, "fourth turn rejected");
                int stage = run.Stage, money = run.Money, count = run.Students.Count;
                KeywordRules.Add(run.Students[0].Keywords, KeywordRules.Starlight, 10);
                Check(run.ReturnFromPracticeBattle(), "third turn return");
                Check(!run.InBattle && run.Battle == null && run.Students[0].Keywords.Count == 0, "return cleanup");
                Check(run.Stage == stage && run.Money == money && run.Students.Count == count, "no reward or stage advance");
                Check(run.StartBattle() && run.Battle.Turn == 0 && float.IsPositiveInfinity(run.Battle.BossHp), "repeat practice session");
                Debug.Log("PRACTICE_RULE_CHECKS_PASS assertions=" + assertions);
            }
            finally { UnityEngine.Object.DestroyImmediate(settings); }
            PrototypeBuilder.Build();
        }
        static void Check(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
