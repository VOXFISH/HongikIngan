using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HongikIngan
{
    public sealed class BattleArcSmoke : MonoBehaviour
    {
        string output; int assertions;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (!Environment.GetCommandLineArgs().Contains("--battle-arc-smoke")) return;
            var go=new GameObject("BattleArcSmoke"); DontDestroyOnLoad(go); go.AddComponent<BattleArcSmoke>();
        }
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs(); int index=Array.IndexOf(args,"--smoke-output");
            output=index>=0?args[index+1]:Path.Combine(Application.dataPath,"../ArcSmoke");
            Directory.CreateDirectory(output); Application.runInBackground=true;
            var checks=CheckScene();
            while(true)
            {
                object next=null; bool more=false; Exception failure=null;
                try { more=checks.MoveNext(); if(more)next=checks.Current; } catch(Exception e){failure=e;}
                if(failure!=null){File.WriteAllText(Path.Combine(output,"arc-result.txt"),"FAIL "+failure); Application.Quit(1);yield break;}
                if(!more)break; yield return next;
            }
            File.WriteAllText(Path.Combine(output,"arc-result.txt"),"PASS assertions="+assertions+": centered 3/4/5/7/9, logical drag order, hover gating, authored positions, timed arithmetic, live beam endpoints and synchronized damage/HP.");
            Application.Quit();
        }
        IEnumerator CheckScene()
        {
            while(!UnityEngine.Rendering.SplashScreen.isFinished)yield return null;
            bool fullHd=Environment.GetCommandLineArgs().Contains("--full-hd");
            int width=fullHd?1920:1440, height=fullHd?1080:900;
            Screen.SetResolution(width,height,FullScreenMode.Windowed);
            var session=PrototypeSession.Ensure(); var run=session.Run;
            run.AddMoney(10000); for(int i=0;i<4;i++)run.ExpandSlots();
            run.Students.Clear(); foreach(int id in new[]{0,8,9})run.Students.Add(new StudentState(PrototypeCatalog.Students[id]));
            Check(run.Settings.bossBaseHp==2000 && run.Settings.bossGrowth==2,"runtime boss balance");
            session.EnterBattle(); yield return new WaitForSeconds(1.8f);
            var screen=FindFirstObjectByType<BattleScreen>(); var formation=screen.formation;
            Check(Screen.width==width && Screen.height==height,"requested render resolution");
            OnScreen(screen.hud.turnLabel.rectTransform);
            OnScreen(screen.hud.synergyLabel.rectTransform);
            OnScreen(screen.hud.bossHpLabel.rectTransform);
            OnScreen(screen.hud.bossDebuffsLabel.rectTransform);
            OnScreen(screen.hud.damageLabel.rectTransform);
            OnScreen(screen.hud.logLabel.rectTransform);
            OnScreen((RectTransform)screen.hud.actionButton.transform);
            Check(screen.CanReorder,"ready for input");
            Check(screen.presentation.stepDuration==1 && screen.presentation.beamDuration==1,"one second authored timings");
            var actual=run.Students.ToArray();
            int[][] expected={new[]{3,4,5},new[]{2,3,4,5},new[]{2,3,4,5,6},new[]{1,2,3,4,5,6,7},new[]{0,1,2,3,4,5,6,7,8}};
            foreach(var positions in expected)
            {
                run.Students.Clear();run.Students.AddRange(PrototypeCatalog.Students.Take(positions.Length).Select(d=>new StudentState(d)));
                formation.Render(run);
                screen.hud.Render(run,"",false,false,false);
                Check(formation.slots.Select((s,i)=>new{s,i}).Where(x=>x.s.gameObject.activeSelf).Select(x=>x.i).SequenceEqual(positions),"centered count "+positions.Length);
                for(int i=0;i<positions.Length;i++)
                {
                    var slot=formation.slots[positions[i]];
                    Check(slot.Index==i && slot.orderLabel.text==(i+1).ToString() && slot.Student==run.Students[i],"left-to-right binding "+positions.Length+"/"+i);
                    Check(!slot.roleLabel.gameObject.activeSelf && !slot.skillLabel.gameObject.activeSelf,"compact sprite view");
                    OnScreen(slot.portrait.rectTransform);
                    OnScreen(slot.nameLabel.rectTransform);
                    OnScreen(slot.orderLabel.rectTransform);
                    if(i>0)Check(slot.transform.position.x>formation.slots[positions[i-1]].transform.position.x,"ordered spatial positions");
                }
                yield return null;
                BattleUiCapture.Capture(screen.hud.GetComponent<Canvas>(),Path.Combine(output,"arc-"+positions.Length+".png"));yield return new WaitForSeconds(.25f);
            }
            run.Students.Clear();run.Students.AddRange(actual);formation.Render(run);
            Check(formation.artwork.Length==36,"Inspector artwork entry for every Girl");
            var artwork=formation.artwork.Single(a=>a.girlId==actual[0].Definition.Id);
            var originalArtwork=artwork.sprite;
            var testSprite=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),new Vector2(.5f,.5f));
            artwork.sprite=testSprite;formation.Render(run);
            var first=formation.StudentSlot(0); var second=formation.StudentSlot(1);
            Check(first.portrait.sprite==testSprite,"assigned Girl sprite renders");
            var rect=(RectTransform)first.transform;var authored=rect.anchoredPosition;rect.anchoredPosition+=new Vector2(11,7);
            formation.Render(run);Check(rect.anchoredPosition==authored+new Vector2(11,7),"render preserves inspector position");rect.anchoredPosition=authored;
            KeywordRules.Add(first.Student.Keywords,KeywordRules.Art,1,2);formation.Render(run);
            Check(first.buffsLabel.text.Contains("미감 2"),"buff over head");
            KeywordRules.Add(run.Battle.BossKeywords,KeywordRules.Spite,20,2);
            screen.hud.Render(run,"",false,false,false);
            Check(screen.hud.bossDebuffsLabel.text.Contains("표독 2"),"boss debuff near HP");
            var e=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(null,first.studentVisual.position),pointerDrag=first.gameObject};
            first.Render(new StudentState(PrototypeCatalog.Students.Single(d=>d.Id=="Girl35")));
            first.OnPointerEnter(e);Check(formation.tooltip.effectLabel.text.Contains("0.5"),"fractional skill parameter stays precise");
            first.OnPointerExit(e);formation.Render(run);
            first.OnPointerEnter(e);Check(formation.tooltip.IsOpen && formation.tooltip.nameLabel.text==first.Student.Definition.Name && formation.tooltip.effectLabel.text.Contains(first.Student.Definition.SkillDescription),"hover details");
            OnScreen(formation.tooltip.nameLabel.rectTransform);
            OnScreen(formation.tooltip.effectLabel.rectTransform);
            BattleUiCapture.Capture(screen.hud.GetComponent<Canvas>(),Path.Combine(output,"hover.png"));yield return new WaitForSeconds(.3f);
            first.OnPointerExit(e);Check(!formation.tooltip.IsOpen,"exit hides tooltip");
            first.OnPointerEnter(e);screen.audioPanel.openButton.onClick.Invoke();Check(!formation.tooltip.IsOpen,"modal hides tooltip");
            first.OnPointerEnter(e);Check(!formation.tooltip.IsOpen,"modal rejects hover");screen.audioPanel.closeButton.onClick.Invoke();
            var original=run.Students[0];first.OnBeginDrag(e);second.OnDrop(e);first.OnEndDrag(e);
            Check(run.Students[1]==original,"centered drag uses logical index");
            Check(formation.StudentSlot(1).portrait.sprite==testSprite,"Girl sprite follows reordered student");
            artwork.sprite=originalArtwork;formation.Render(run);Destroy(testSprite);
            first=formation.StudentSlot(0);first.OnPointerEnter(e);screen.PerformAction();first.OnPointerEnter(e);
            Check(screen.IsRunning && !formation.tooltip.IsOpen,"attack hides and rejects hover");
            float deadline=Time.realtimeSinceStartup+120;
            float beamAt=-1,beamDamage=0,beamHp=0,lastFormulaAt=0;string lastFormula=null;
            bool formulaCaptured=false,transferChecked=false;int spacedFormulas=0;
            while(screen.IsRunning && Time.realtimeSinceStartup<deadline)
            {
                var active=formation.slots.FirstOrDefault(s=>s.calculationLabel.gameObject.activeSelf);
                if(active!=null && active.calculationLabel.text!=lastFormula)
                {
                    if(lastFormula!=null){Check(Time.realtimeSinceStartup-lastFormulaAt>=.9f,"one second per formula");spacedFormulas++;}
                    lastFormula=active.calculationLabel.text;lastFormulaAt=Time.realtimeSinceStartup;
                }
                if(active!=null && !formulaCaptured && Time.realtimeSinceStartup-lastFormulaAt>.3f)
                {BattleUiCapture.Capture(screen.hud.GetComponent<Canvas>(),Path.Combine(output,"calculation.png"));formulaCaptured=true;}
                if(screen.presentation.IsBeaming)
                {
                    if(beamAt<0){beamAt=Time.realtimeSinceStartup;beamDamage=screen.presentation.DisplayedDamage;beamHp=screen.presentation.DisplayedHp;}
                    if(!transferChecked && Time.realtimeSinceStartup-beamAt>.35f)
                    {
                        float spent=beamDamage-screen.presentation.DisplayedDamage;
                        float removed=beamHp-screen.presentation.DisplayedHp;
                        Check(spent>0 && removed>0 && Math.Abs(spent-removed)<2,"orb and HP drain together");
                        var beam=(RectTransform)screen.presentation.beam.transform;
                        var from=screen.presentation.orbDamage.rectTransform;
                        var to=screen.hud.bossHpLabel.rectTransform;
                        Check(Vector3.Distance(beam.TransformPoint(Vector3.zero),from.TransformPoint(from.rect.center))<1,"beam starts at orb number");
                        Check(Vector3.Distance(beam.TransformPoint(new Vector3(beam.rect.width,0,0)),to.TransformPoint(to.rect.center))<1,"beam ends at HP number");
                        BattleUiCapture.Capture(screen.hud.GetComponent<Canvas>(),Path.Combine(output,"beam.png"));transferChecked=true;
                    }
                }
                yield return null;
            }
            Check(!screen.IsRunning && formulaCaptured && spacedFormulas>0 && transferChecked,"complete visible combat");
            Check(screen.presentation.DisplayedDamage==0 && screen.hud.damageLabel.text=="0","spent orb remains zero");
            Check(Math.Abs(screen.presentation.DisplayedHp-run.Battle.BossHp)<.1f,"display and combat HP agree");
            yield return new WaitForSeconds(1.2f);Check(screen.CanReorder,"next turn ready");
            formation.StudentSlot(0).OnPointerEnter(e);Check(formation.tooltip.IsOpen,"next preparation hover restored");
        }
        void Check(bool condition,string message){assertions++;if(!condition)throw new InvalidOperationException(message);}
        void OnScreen(RectTransform rect)
        {
            var corners=new Vector3[4]; rect.GetWorldCorners(corners);
            Check(corners.All(c=>c.x>=-.5f && c.y>=-.5f && c.x<=Screen.width+.5f && c.y<=Screen.height+.5f),
                "visible at "+Screen.width+"x"+Screen.height+": "+rect.name);
        }
    }
}
