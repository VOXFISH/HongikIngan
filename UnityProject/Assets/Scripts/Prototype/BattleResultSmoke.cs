using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace HongikIngan
{
    public sealed class BattleResultSmoke : MonoBehaviour
    {
        int assertions; string output; Mouse mouse; PrototypeSession session;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if(!Environment.GetCommandLineArgs().Contains("--battle-result-smoke"))return;
            var go=new GameObject(nameof(BattleResultSmoke));DontDestroyOnLoad(go);go.AddComponent<BattleResultSmoke>();
        }
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--smoke-output");
            output=index>=0?args[index+1]:Path.Combine(Application.persistentDataPath,"BattleResultSmoke");Directory.CreateDirectory(output);
            Application.runInBackground=true;var stack=new Stack<IEnumerator>();stack.Push(Verify());
            while(stack.Count>0)
            {
                object next=null;Exception error=null;
                try {var it=stack.Peek();if(!it.MoveNext())stack.Pop();else next=it.Current;}catch(Exception e){error=e;}
                if(error!=null){File.WriteAllText(Path.Combine(output,"result.txt"),"FAIL assertions="+assertions+" "+error);Application.Quit(1);yield break;}
                if(next is IEnumerator nested)stack.Push(nested);else if(next!=null||stack.Count>0)yield return next;
            }
            if(mouse!=null)InputSystem.RemoveDevice(mouse);
            File.WriteAllText(Path.Combine(output,"result.txt"),"PASS assertions="+assertions+": reward/game-over slides, logo timing, typewriter, transfer, skip, score, confirm-once, main/school routing and 720p/1080p bounds.");
            yield return new WaitForSecondsRealtime(.25f);Application.Quit();
        }
        IEnumerator Verify()
        {
            while(!UnityEngine.Rendering.SplashScreen.isFinished)yield return null;
            Screen.SetResolution(1920,1080,FullScreenMode.Windowed);
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            mouse=InputSystem.AddDevice<Mouse>("ResultTestMouse");mouse.MakeCurrent();
            foreach(var physical in InputSystem.devices.OfType<Mouse>().Where(m=>m!=mouse).ToArray())InputSystem.DisableDevice(physical);
            session=PrototypeSession.Ensure();var run=NewRun(1,100);session.EnterBattle();
            yield return Wait(()=>FindFirstObjectByType<BattleScreen>()!=null,"battle loaded");
            var battle=FindFirstObjectByType<BattleScreen>();yield return Ready(battle);
            battle.hud.actionButton.onClick.Invoke();yield return Wait(()=>battle.rewardPopup.IsOpen,"victory result opens after action");
            var view=battle.rewardPopup;var reward=run.Battle.Reward;
            Check(run.Battle.Won&&SceneManager.GetActiveScene().name=="Battle","victory remains in battle until confirmation");
            Check(view.cover.sprite==view.rewardSprite&&!view.IsGameOver,"third sprite for rewards");
            Check(!battle.CanInspect&&!battle.CanReorder,"results block battle input");
            Check(view.DisplayedEnergy==105&&run.Money==105,"initial wallet and unpaid model");
            yield return Wait(()=>Mathf.Abs(view.cover.rectTransform.anchoredPosition.x)<.1f,"tile holds center");
            float coveredAt=Time.realtimeSinceStartup;Check(view.logo.color.a>.75f,"logo still visible at coverage");
            Capture(view,"01-covered-logo");
            yield return Wait(()=>view.content.alpha>.99f,"logo fades before results");
            Check(Time.realtimeSinceStartup-coveredAt>=.42f,"logo fade lasts half a second");
            Check(view.logo.color.a==0&&Mathf.Abs(view.cover.rectTransform.anchoredPosition.x)<.1f,"background stays covered after logo");
            yield return Wait(()=>view.lines[0].dots.maxVisibleCharacters>2,"dot leaders type in");
            Check(view.lines[0].value.maxVisibleCharacters==0,"number waits for caption and dots");
            yield return Wait(()=>view.lines[0].value.maxVisibleCharacters==99999,"first payout appears");
            float numberAt=Time.realtimeSinceStartup;yield return new WaitForSecondsRealtime(.35f);
            Check(view.DisplayedEnergy==105,"number remains before one second transfer");
            yield return Wait(()=>view.DisplayedEnergy>105,"energy transfer starts");
            Check(Time.realtimeSinceStartup-numberAt>=.9f,"one second hold before transfer");
            Check(run.Money==105&&!reward.Claimed,"animation has no economic side effects");
            Capture(view,"02-reward-transfer");yield return Click(ButtonCenter(view));
            Check(view.IsReady&&!view.IsLeaving&&run.InBattle,"skip completes without also confirming");
            Check(view.DisplayedEnergy==105+reward.Total&&view.lines.All(l=>l.value.text=="+ 0"),"skip transfers all three remaining amounts");
            foreach(var row in view.lines){Check(row.label.maxVisibleCharacters==99999&&row.dots.maxVisibleCharacters==99999,"skip completes characters");Bounds(row.value.rectTransform);}
            CheckDots(view); Capture(view,"03-reward-ready");yield return null;
            yield return Click(ButtonCenter(view)); Check(view.IsLeaving,"actual button click starts logo return");
            view.claimButton.onClick.Invoke();Check(view.IsLeaving,"confirmation starts logo return once");
            yield return Wait(()=>SceneManager.GetActiveScene().name=="School"&&FindFirstObjectByType<BattleRewardPopup>()==null,"reward returns to school");
            Check(run.Stage==2&&run.Money==105+reward.Total&&reward.Claimed,"one payment and one stage advance");
            Check(run.CumulativeDamage==100,"score retained after victory");

            run.Settings.bossBaseHp=100000;SetStudent(run,7);session.EnterBattle();
            yield return Wait(()=>FindFirstObjectByType<BattleScreen>()!=null,"second battle loaded");battle=FindFirstObjectByType<BattleScreen>();
            for(int turn=0;turn<3;turn++){yield return Ready(battle);battle.hud.actionButton.onClick.Invoke();yield return Wait(()=>!battle.IsRunning,"turn presentation ends");}
            yield return Wait(()=>battle.rewardPopup.IsOpen,"game over result opens");view=battle.rewardPopup;
            Check(run.GameOver&&view.IsGameOver&&view.cover.sprite==view.gameOverSprite,"second sprite for game over");
            Check(run.CumulativeDamage==121,"score adds every turn across stages");
            yield return Wait(()=>view.lines[1].root.activeSelf&&view.lines[1].dots.maxVisibleCharacters>2,"game-over dot leaders animate");
            Capture(view,"04-game-over-typing");yield return Click(new Vector2(20,20));
            Check(view.IsReady&&!view.IsLeaving&&SceneManager.GetActiveScene().name=="Battle","game-over skip cannot return in same click");
            Check(view.titleLabel.text=="Game Over"&&view.lines[0].value.text=="2"&&view.lines[1].value.text=="121","game-over stage and score snapshot");
            Check(!view.claimButton.gameObject.activeSelf&&view.returnLabel.gameObject.activeSelf&&view.returnLabel.text=="Press Any Key to Return","complete return prompt appears at once");
            Check(run.Money==105+reward.Total,"game over never pays reward");CheckDots(view);
            yield return new WaitForSecondsRealtime(1.5f);Check(view.returnLabel.alpha<.55f,"prompt fades gently at half period");
            yield return new WaitForSecondsRealtime(1.5f);Check(view.returnLabel.alpha>.85f,"prompt returns after three seconds");
            Capture(view,"05-game-over-ready-1080");
            Screen.SetResolution(1280,720,FullScreenMode.Windowed);yield return null;yield return null;
            foreach(var row in view.lines){Bounds(row.label.rectTransform);Bounds(row.value.rectTransform);Check(!row.value.isTextOverflowing,"right aligned number fits");}
            Bounds(view.returnLabel.rectTransform);Capture(view,"06-game-over-ready-720");
            yield return Click(new Vector2(20,20));
            yield return Wait(()=>SceneManager.GetActiveScene().name=="Main"&&FindFirstObjectByType<BattleRewardPopup>()==null,"game over returns to main");
            Check(session.Run.CumulativeDamage==0&&session.Run.Stage==1&&!session.Run.InBattle,"return main resets run safely");
            Check(FindFirstObjectByType<MainMenuScreen>()!=null,"main menu stays waiting for input");

            run=NewRun(1,100);session.EnterBattle();yield return Wait(()=>FindFirstObjectByType<BattleScreen>()!=null,"natural completion battle loaded");
            battle=FindFirstObjectByType<BattleScreen>();yield return Ready(battle);view=battle.rewardPopup;
            view.characterInterval=.002f;view.numberDelay=.01f;view.transferWait=.03f;view.transferDuration=.05f;
            battle.hud.actionButton.onClick.Invoke();yield return Wait(()=>view.IsReady,"unskipped animation completes");
            Check(view.DisplayedEnergy==run.Money+run.Battle.Reward.Total&&view.lines.All(l=>l.value.text=="+ 0"),"natural and skipped final states agree");
            Check(run.InBattle&&!run.Battle.Reward.Claimed,"natural completion still requires confirmation");
        }
        PrototypeRun NewRun(float hp,float damage)
        {
            var settings=Instantiate(Resources.Load<EconomySettings>("PrototypeEconomy"));settings.bossBaseHp=hp;settings.startingMoney=105;
            var run=new PrototypeRun(settings,17);SetStudent(run,damage);typeof(PrototypeSession).GetProperty("Run").SetValue(session,run);return run;
        }
        static void SetStudent(PrototypeRun run,float damage)
        {run.Students.Clear();run.Students.Add(new StudentState(new StudentDefinition{Id="result-test",Name="결과 검증",Club="none",Personality="none",Grade=1,Action=ActionKind.AddDamage,InitialValue=damage}));}
        IEnumerator Ready(BattleScreen battle)
        {yield return Wait(()=>battle.CanReorder&&battle.CanInspect,"battle ready");battle.presentation.stepDuration=.03f;battle.presentation.beamDuration=.06f;}
        IEnumerator Wait(Func<bool> predicate,string message)
        {float deadline=Time.realtimeSinceStartup+25;while(!predicate()&&Time.realtimeSinceStartup<deadline)yield return null;Check(predicate(),message);}
        IEnumerator Click(Vector2 point)
        {
            InputSystem.QueueStateEvent(mouse,new MouseState{position=point,buttons=1});yield return null;yield return null;
            InputSystem.QueueStateEvent(mouse,new MouseState{position=point,buttons=0});yield return null;yield return null;
        }
        void CheckDots(BattleRewardPopup view)
        {
            Canvas.ForceUpdateCanvases();
            foreach(var line in view.lines)
            {
                int visible=0;
                foreach(var c in line.dots.textInfo.characterInfo.Take(line.dots.textInfo.characterCount))
                {
                    if(c.character!='.'||!c.isVisible)continue;
                    var vertices=line.dots.textInfo.meshInfo[c.materialReferenceIndex].vertices;
                    if((vertices[c.vertexIndex+2]-vertices[c.vertexIndex]).sqrMagnitude>.001f)visible++;
                }
                Check(visible==16,"all dot leader glyphs have visible geometry after reveal/skip");
            }
        }
        void Bounds(RectTransform rect)
        {var corners=new Vector3[4];rect.GetWorldCorners(corners);foreach(var p in corners)Check(p.x>=-1&&p.x<=Screen.width+1&&p.y>=-1&&p.y<=Screen.height+1,"screen bounds "+rect.name);}
        static Vector2 ButtonCenter(BattleRewardPopup view)
        {var rect=(RectTransform)view.claimButton.transform;return RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));}
        void Capture(BattleRewardPopup view,string name)
        {
            Canvas.ForceUpdateCanvases();
            BattleUiCapture.Capture(view.GetComponent<Canvas>(),Path.Combine(output,name+".png"));
            var info=string.Join("\n",view.lines.Select(l=>l.dots.name+": active="+l.dots.gameObject.activeInHierarchy+" cull="+l.dots.canvasRenderer.cull+" alpha="+l.dots.canvasRenderer.GetAlpha()+" color="+l.dots.color+" bounds="+l.dots.bounds+" mesh="+l.dots.mesh.bounds+" glyphs="+string.Join(";",l.dots.textInfo.characterInfo.Take(l.dots.textInfo.characterCount).Where(c=>c.character=='.').Take(1).Select(c=>"visible="+c.isVisible+" bl="+c.bottomLeft+" tr="+c.topRight+" vertex="+l.dots.textInfo.meshInfo[c.materialReferenceIndex].vertices[c.vertexIndex]+" color="+l.dots.textInfo.meshInfo[c.materialReferenceIndex].colors32[c.vertexIndex]))));
            File.WriteAllText(Path.Combine(output,name+"-dots.txt"),info);
        }
        void Check(bool condition,string message){assertions++;if(!condition)throw new InvalidOperationException(message);}
    }
}
