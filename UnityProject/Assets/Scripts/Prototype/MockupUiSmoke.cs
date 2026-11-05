using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HongikIngan
{
    public sealed class MockupUiSmoke : MonoBehaviour
    {
        string output; int assertions;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (!Environment.GetCommandLineArgs().Contains("--mockup-ui-smoke")) return;
            var go=new GameObject("MockupUiSmoke"); DontDestroyOnLoad(go); go.AddComponent<MockupUiSmoke>();
        }
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs(); int index=Array.IndexOf(args,"--smoke-output");
            output=index>=0?args[index+1]:Path.Combine(Application.dataPath,"../MockupSmoke");
            Directory.CreateDirectory(output); Application.runInBackground=true;
            var checks=CheckFlow();
            while(true)
            {
                object next=null; bool more=false; Exception failure=null;
                try { more=checks.MoveNext(); if(more)next=checks.Current; } catch(Exception e) { failure=e; }
                if(failure!=null) { File.WriteAllText(Path.Combine(output,"mockup-ui-result.txt"),"FAIL "+failure); Application.Quit(1); yield break; }
                if(!more)break; yield return next;
            }
            File.WriteAllText(Path.Combine(output,"mockup-ui-result.txt"),"PASS assertions="+assertions+": editable authored positions, uGUI recruitment/replacement, modal locks, scene curtain, Turn 1/2/3, no mid-battle return, win/defeat boundaries.");
            Application.Quit();
        }
        IEnumerator CheckFlow()
        {
            while(!UnityEngine.Rendering.SplashScreen.isFinished)yield return null;
            Screen.SetResolution(1440,810,FullScreenMode.Windowed);
            FindFirstObjectByType<MainMenuScreen>().startButton.onClick.Invoke();
            yield return new WaitForSeconds(1.5f);
            var school=FindFirstObjectByType<SchoolScreen>(); var run=school.CurrentRun;
            Check(run.Money==100 && run.Students.Count==3,"100 energy and three starters");
            Check(school.CanInteract && school.dialogs!=null,"school ready with scene dialogs");
            Check(run.RelicOffers.Count==1,"one relic proposal");
            var desk=(RectTransform)school.slots[0].transform; var position=desk.anchoredPosition; var scale=desk.localScale;
            desk.anchoredPosition+=new Vector2(17,11); school.Refresh();
            Check(desk.anchoredPosition==position+new Vector2(17,11) && desk.localScale==scale,"refresh preserves Inspector position and scale");
            desk.anchoredPosition=position;
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"school-1440x810.png")); yield return new WaitForSeconds(.3f);
            school.SelectStudent(0); Check(school.studentCard.activeSelf,"student ID card opens");
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"student-id.png")); yield return new WaitForSeconds(.3f);
            school.dialogs.closeStudent.onClick.Invoke(); Check(!school.studentCard.activeSelf,"ID card closes");
            school.dialogs.caller.onClick.Invoke();
            Check(school.dialogs.offers.activeSelf && !school.CanInteract,"pet caller opens modal");
            Check(school.petButtons[0].IsInteractable(),"offer ignores disabled background CanvasGroup");
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"pet-offers.png")); yield return new WaitForSeconds(.3f);
            school.petButtons[0].onClick.Invoke();
            Check(run.PendingPet && school.dialogs.candidates.activeSelf,"pet payment opens real candidate UI");
            Check(!school.dialogs.candidateButtons[0].interactable,"full formation cannot recruit");
            Check(!run.ReturnToSchool() && !run.CanStartBattle,"pending choice cannot enter or return from battle");
            school.dialogs.replace.onClick.Invoke(); Check(school.dialogs.replacement.activeSelf,"replacement overlay opens");
            school.dialogs.replacementButtons[0].onClick.Invoke();
            Check(run.PendingPet && run.Students.Count==2 && !school.dialogs.replacement.activeSelf,"sale preserves pending candidates without auto recruitment");
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"pet-candidates.png")); yield return new WaitForSeconds(.3f);
            Check(school.dialogs.candidateButtons[0].IsInteractable(),"candidate receives input after replacement sale");
            school.dialogs.candidateButtons[0].onClick.Invoke();
            Check(!run.PendingPet && run.Students.Count==3 && school.CanInteract,"actual recruit button completes choice");
            Check(run.PetOffers[0].HasValue,"consumed pet immediately refilled");
            school.audioButton.onClick.Invoke(); Check(school.dialogs.audioPanel.IsOpen && !school.CanInteract,"audio modal locks school");
            school.dialogs.audioPanel.closeButton.onClick.Invoke(); Check(school.CanInteract,"audio closes");
            school.restartButton.onClick.Invoke(); Check(school.dialogs.restart.activeSelf && !school.CanInteract,"restart confirmation locks school");
            school.dialogs.cancelRestart.onClick.Invoke(); Check(school.CanInteract,"restart cancellation preserves run");
            run.Students.Clear(); run.Students.Add(new StudentState(PrototypeCatalog.Students[0]));
            run.Settings.bossBaseHp=100000; run.Settings.bossGrowth=1; school.Refresh();
            school.battleButton.onClick.Invoke();
            Check(SceneTransitionOverlay.Current.IsBusy,"sortie curtain starts before loading");
            yield return new WaitForSeconds(.45f);
            var battle=FindFirstObjectByType<BattleScreen>();
            Check(battle!=null && SceneManager.GetActiveScene().name=="Battle","sortie loads Battle");
            yield return new WaitForSeconds(.2f);
            Check(battle.turnTransition.IsShowing && battle.turnTransition.title.text=="Turn 1","first turn transition");
            Check(!battle.CanReorder && !battle.hud.actionButton.interactable,"turn overlay locks input");
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"turn-1.png")); yield return new WaitForSeconds(.3f);
            float deadline=Time.realtimeSinceStartup+5;
            while(!battle.CanReorder && Time.realtimeSinceStartup<deadline)yield return null;
            Check(battle.CanReorder && !float.IsInfinity(run.Battle.BossHp),"finite battle ready");
            Check(battle.hud.actionButton.IsInteractable(),"attack button receives input after turn overlay");
            battle.presentation.stepDuration=.01f;
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"battle-1440x810.png")); yield return new WaitForSeconds(.3f);
            for(int turn=1;turn<=3;turn++)
            {
                Check(!run.ReturnToSchool(),"no school return before turn "+turn);
                battle.hud.actionButton.onClick.Invoke();
                Check(battle.IsRunning && !run.ReturnToSchool(),"no return during turn "+turn);
                deadline=Time.realtimeSinceStartup+10;
                while(battle.IsRunning && Time.realtimeSinceStartup<deadline)yield return null;
                Check(run.Battle.Turn==turn,"completed turn "+turn);
                if(turn<3)
                {
                    Check(SceneManager.GetActiveScene().name=="Battle" && !run.ReturnToSchool(),"surviving boss keeps current scene");
                    Check(battle.turnTransition.LastShownTurn==turn+1 && battle.turnTransition.IsShowing,"next Turn overlay before input");
                    ScreenCapture.CaptureScreenshot(Path.Combine(output,"turn-"+(turn+1)+".png")); yield return new WaitForSeconds(.15f);
                    deadline=Time.realtimeSinceStartup+5;
                    while(!battle.CanReorder && Time.realtimeSinceStartup<deadline)yield return null;
                    Check(battle.CanReorder,"next turn can reorder");
                }
            }
            Check(run.Battle.Lost && !battle.turnTransition.IsShowing && !run.ReturnToSchool(),"third turn ends in defeat without a fourth turn or victory return");
            battle.hud.actionButton.onClick.Invoke(); yield return new WaitForSeconds(1);
            school=FindFirstObjectByType<SchoolScreen>(); run=school.CurrentRun;
            Check(run.Stage==1 && run.Money==100 && run.Students.Count==3,"defeat starts a fresh school");
            run.Students.Clear(); run.Students.Add(new StudentState(PrototypeCatalog.Students[0])); run.Settings.bossBaseHp=1;
            school.Refresh(); school.battleButton.onClick.Invoke(); yield return new WaitForSeconds(1.6f);
            battle=FindFirstObjectByType<BattleScreen>(); Check(battle.CanReorder,"victory fixture ready"); battle.presentation.stepDuration=.01f;
            battle.PerformAction(); deadline=Time.realtimeSinceStartup+10;
            while(battle.IsRunning && Time.realtimeSinceStartup<deadline)yield return null;
            Check(run.Battle.Won && battle.turnTransition.LastShownTurn==1 && !battle.turnTransition.IsShowing,"victory has no extra turn overlay");
            int previous=run.Money, reward=run.VictoryReward;
            battle.hud.actionButton.onClick.Invoke(); Check(SceneTransitionOverlay.Current.IsBusy,"return curtain begins");
            yield return new WaitForSeconds(1);
            Check(SceneManager.GetActiveScene().name=="School" && run.Stage==2 && run.Money==previous+reward,"victory return and preview reward agree");
            Check(FindObjectsByType<SceneTransitionOverlay>(FindObjectsSortMode.None).Length==1,"single persistent scene curtain");
            Screen.SetResolution(1280,720,FullScreenMode.Windowed); yield return new WaitForSeconds(.5f);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"school-1280x720.png")); yield return new WaitForSeconds(.3f);
        }
        void Check(bool condition,string message) { assertions++; if(!condition)throw new InvalidOperationException(message); }
    }
}
