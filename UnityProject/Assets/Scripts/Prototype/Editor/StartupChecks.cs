using System;
using System.Linq;
using UnityEngine;

namespace HongikIngan
{
    public static class StartupChecks
    {
        public static void Run()
        {
            var settings = ScriptableObject.CreateInstance<EconomySettings>();
            try
            {
                int firstYears = 0, secondYears = 0;
                var combinations = new System.Collections.Generic.HashSet<string>();
                for (int seed = 0; seed < 2000; seed++)
                {
                    var run = new PrototypeRun(settings, seed);
                    if (run.Money != 100 || run.Students.Count != 3 || run.InBattle || run.Stage != 1)
                        throw new InvalidOperationException("STARTUP_CHECKS_FAIL: new school resources/state");
                    if (run.Students.Select(s => s.Definition.Id).Distinct().Count() != 3 ||
                        run.Students.Any(s => s.Definition.Grade < 1 || s.Definition.Grade > 2))
                        throw new InvalidOperationException("STARTUP_CHECKS_FAIL: unique first/second-year roster");
                    firstYears += run.Students.Count(s => s.Definition.Grade == 1);
                    secondYears += run.Students.Count(s => s.Definition.Grade == 2);
                    combinations.Add(string.Join(",", run.Students.Select(s => s.Definition.Id)));
                    var same = new PrototypeRun(settings, seed);
                    if (!run.Students.Select(s => s.Definition.Id).SequenceEqual(same.Students.Select(s => s.Definition.Id)))
                        throw new InvalidOperationException("STARTUP_CHECKS_FAIL: seeded reproducibility");
                }
                double ratio = (double)firstYears / (firstYears + secondYears);
                if (ratio < .63 || ratio > .70 || combinations.Count < 50)
                    throw new InvalidOperationException("STARTUP_CHECKS_FAIL: pet 4:2 weighting / random variety: " + ratio);
                Debug.Log("STARTUP_CHECKS_PASS seeds=2000 firstYearRatio=" + ratio);
            }
            finally { UnityEngine.Object.DestroyImmediate(settings); }
        }
    }
}
