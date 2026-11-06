using System;
using System.Linq;
using UnityEngine;

namespace HongikIngan
{
    public static class SynergyUiChecks
    {
        static int assertions;
        public static void Run()
        {
            assertions = 0;
            var settings = ScriptableObject.CreateInstance<EconomySettings>();
            try
            {
                var run = new PrototypeRun(settings);
                var sports = SynergyCatalog.Find("운동부");
                foreach (int n in Enumerable.Range(0,9))
                {
                    Roster(run, Enumerable.Repeat(0,n).ToArray());
                    string row = SynergyText.Entry(run,sports);
                    Check(row.Contains($"({n} / {(n < 2 ? 2 : n < 6 ? n+1 : 6)})"), "sports next threshold " + n);
                    Check(row.Contains(n < 2 ? SynergyText.Gray : n < 6 ? SynergyText.White : SynergyText.Yellow), "sports color " + n);
                }
                var theatre = SynergyCatalog.Find("연극부");
                for (int n = 0; n <= 9; n++)
                {
                    Roster(run,Enumerable.Repeat(30,n).ToArray());
                    Check(SynergyCatalog.Active(run,theatre) == (n == 1 || n == 4), "exact theatre " + n);
                    var popup = SynergyText.Popup(run,"연극부");
                    Check(popup.Contains(SynergyText.Color("(1) " + theatre.Effects[0], n == 1 ? SynergyText.White : SynergyText.Gray)), "theatre tier 1 " + n);
                    Check(popup.Contains(SynergyText.Color("(4) " + theatre.Effects[1], n == 4 ? SynergyText.White : SynergyText.Gray)), "theatre tier 4 " + n);
                }
                Roster(run,0,1); Set(run, "Atmosphere", 500f);
                Check(SynergyText.Entry(run,sports).Contains("(3 / 4)"),"mood virtual count");
                Set(run, "Atmosphere", 5000f);
                Check(SynergyText.Entry(run,sports).Contains("(4 / 5)"),"mood all virtual count");
                Set(run, "Atmosphere", 0f);
                Roster(run,1,0,7);
                Check(!SynergyCatalog.Active(run,SynergyCatalog.Find("사랑꾼")),"separated lovers inactive");
                Roster(run,1,7,0);
                Check(SynergyCatalog.Active(run,SynergyCatalog.Find("사랑꾼")),"adjacent lovers active");
                run.StartBattle(); Set(run.Battle, "Turn", 1); BattleSynergies.BeginTurn(run);
                var middle = run.Students[1]; run.Students[1] = run.Students[2]; run.Students[2] = middle;
                Check(SynergyCatalog.LoverCount(run) == 2,"battle lovers use frozen formation");
                Roster(run,0,2,30);
                Check(SynergyText.List(run,true).Contains("운동부") && !SynergyText.List(run,true).Contains("비활성"),"battle list active only");
                Roster(run,3,9,4,11,5,10);
                var chant = CombatEngine.BuildChant(run.Students);
                Check(chant.Contains("꿈") && chant.Contains("너네 XX") && chant.Contains("매력"),"confirmed chant words");
                Check(!chant.Contains("반항아") && !chant.Contains("운동부"),"chant omits synergy names");
                foreach (var d in SynergyCatalog.All)
                    Check(!string.IsNullOrWhiteSpace(d.Word) && !string.IsNullOrWhiteSpace(d.Description) && d.Effects.Length == d.Thresholds.Length,"complete metadata " + d.Name);
                foreach (var d in PrototypeCatalog.Students)
                {
                    var student = new StudentState(d);
                    Check(StudentCardView.Body(student).Contains(SynergyText.Link(d.Club)),"student link " + d.Id);
                }
                Roster(run); Set(run,"Atmosphere",0f);
                Set(run,"Fortune",PrototypeCatalog.Fortunes.Single(f=>f.Kind==FortuneKind.AllSynergies));
                Check(SynergyText.List(run,false).Contains(SynergyText.Link("운동부",SynergyText.Color("운동부 (1 / 2)",SynergyText.Gray))),"fortune adds unowned synergy");
                Set(run,"FortuneStrength",2);
                Check(SynergyText.List(run,true).Contains("운동부 (2 / 3)"),"doubled fortune activates synergy");
                Check(!SynergyText.List(run,true).Contains("연극부"),"fortune excess disables exact theatre");
                Check(SynergyText.Popup(run,"귀염둥이").Contains("모든 비용 공짜!"),"cute final tier matches fortune document");
                Debug.Log("SYNERGY_UI_CHECKS_PASS assertions=" + assertions);
            }
            finally { UnityEngine.Object.DestroyImmediate(settings); }
        }
        static void Set(object target,string property,object value) => target.GetType().GetProperty(property).SetValue(target,value);
        static void Roster(PrototypeRun run, params int[] ids)
        { run.Students.Clear(); run.Students.AddRange(ids.Select(id => new StudentState(PrototypeCatalog.Students.Single(d => d.Id == "Girl" + id)))); }
        static void Check(bool pass,string message)
        { assertions++; if (!pass) throw new InvalidOperationException("SYNERGY_UI_CHECKS_FAIL " + message); }
    }
}
