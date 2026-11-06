using System;
using System.Collections;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace HongikIngan
{
    // Opt-in real-player acceptance check. No test state is used in normal games.
    public sealed class SynergyUiSmoke : MonoBehaviour
    {
        string output; int assertions; Mouse mouse;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (!Environment.GetCommandLineArgs().Contains("--synergy-ui-smoke")) return;
            var root = new GameObject(nameof(SynergyUiSmoke)); DontDestroyOnLoad(root); root.AddComponent<SynergyUiSmoke>();
        }
        IEnumerator Start()
        {
            var args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args,"--smoke-output");
            output = i < 0 ? Path.Combine(Application.persistentDataPath,"SynergyUiSmoke") : args[i+1];
            Directory.CreateDirectory(output); Application.runInBackground = true;
            var check = Verify();
            while (true)
            {
                object next = null; bool more = false; Exception failure = null;
                try { more = check.MoveNext(); if (more) next = check.Current; } catch (Exception e) { failure = e; }
                if (failure != null)
                {
                    var canvas = FindObjectsByType<Canvas>(FindObjectsSortMode.None).FirstOrDefault(c => c.name == "SchoolCanvas" || c.name == "BattleCanvas");
                    if (canvas != null) Capture(canvas,"failure");
                    File.WriteAllText(Path.Combine(output,"result.txt"),"FAIL " + failure); Application.Quit(1); yield break;
                }
                if (!more) break; yield return next;
            }
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            File.WriteAllText(Path.Combine(output,"result.txt"),"PASS assertions="+assertions+": school/battle links, student card transfer, tier colors, rotation, screen bounds and modal dismissal");
            Application.Quit();
        }
        IEnumerator Verify()
        {
            while (!UnityEngine.Rendering.SplashScreen.isFinished) yield return null;
            Screen.SetResolution(1920,1080,FullScreenMode.Windowed);
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            mouse = InputSystem.AddDevice<Mouse>("SynergyTestMouse"); mouse.MakeCurrent();
            // Keep physical mouse/focus events from replacing the test pointer in a hidden player.
            foreach (var device in InputSystem.devices.OfType<Mouse>().Where(d => d != mouse).ToArray()) InputSystem.DisableDevice(device);
            FindFirstObjectByType<MainMenuScreen>().startButton.onClick.Invoke();
            yield return new WaitForSecondsRealtime(1.5f);
            var school = FindFirstObjectByType<SchoolScreen>(); var run = school.CurrentRun;
            while (!school.CanInteract) yield return null;
            Roster(run,0,1,30); school.Refresh();
            yield return null;
            Check(school.synergyLabel.text.Contains("활성 시너지") && school.synergyLabel.text.Contains("비활성 시너지"),"school groups");
            var listHover = school.synergyLabel.GetComponent<SynergyHover>();
            Point(LinkPoint(school.synergyLabel,"운동부")); yield return null;
            listHover.Inspect(LinkPoint(school.synergyLabel,"운동부"));
            Check(listHover.popup.IsOpen && listHover.popup.label.text.Contains("(2) 데미지 +200"),"school raycast opens full tier popup");
            OnScreen(listHover.popup.panel);
            Capture(school.schoolCanvas.GetComponent<Canvas>(),"01-school-synergy");
            Point(Vector2.zero); yield return null;
            Check(!listHover.popup.IsOpen,"pointer exit closes popup");
            school.SelectStudent(0); yield return null;
            var card = school.studentCard.GetComponent<StudentCardView>();
            Check(card.body.GetComponent<SynergyHover>().popup != null && card.bounds != null,"school prefab scene overrides");
            Check(card.body.text.Contains("활력") == false && card.body.text.Contains("운동부"),"card keeps synergy names");
            float rotation = Angle(card.transform);
            school.Refresh(); Check(Mathf.Approximately(rotation,Angle(card.transform)),"refresh preserves card rotation");
            Point(LinkPoint(card.body,"운동부")); yield return null;
            card.body.GetComponent<SynergyHover>().Inspect(LinkPoint(card.body,"운동부"));
            Capture(school.schoolCanvas.GetComponent<Canvas>(),"02-school-student-card");
            Check(listHover.popup.IsOpen,"school card nested synergy popup"); OnScreen((RectTransform)card.transform);
            Point(Vector2.zero); yield return null;
            foreach (var definition in PrototypeCatalog.Students)
            {
                card.Render(new StudentState(definition)); card.body.ForceMeshUpdate();
                Check(!card.body.isTextOverflowing,"student text fits "+definition.Id);
            }
            card.Render(run.Students[0]);
            for (int n=0;n<20;n++)
            {
                school.DeselectStudent(); school.SelectStudent(0);
                Check(Mathf.Abs(Angle(card.transform))<=10.001f,"school card rotation bound"); OnScreen((RectTransform)card.transform);
            }
            school.DeselectStudent();
            school.battleButton.onClick.Invoke();
            while (FindFirstObjectByType<BattleScreen>() == null) yield return null;
            var battle = FindFirstObjectByType<BattleScreen>(); var formation = battle.formation;
            while (!battle.CanInspect) yield return null;
            Check(battle.CanInspect,"battle inspection enabled");
            Check(!battle.hud.synergyLabel.text.Contains("사랑꾼") && battle.hud.synergyLabel.text.Contains("연극부"),"battle active only");
            Point(LinkPoint(battle.hud.synergyLabel,"연극부")); yield return null;
            battle.hud.synergyLabel.GetComponent<SynergyHover>().Inspect(LinkPoint(battle.hud.synergyLabel,"연극부"));
            var popup = battle.hud.synergyLabel.GetComponent<SynergyHover>().popup;
            Check(popup.IsOpen && popup.label.text.Contains(SynergyText.Color("(4) "+SynergyCatalog.Find("연극부").Effects[1],SynergyText.Gray)),"battle unreached tier gray");
            Capture(battle.hud.GetComponent<Canvas>(),"03-battle-synergy");
            var slot = formation.StudentSlot(0);
            Point(RectTransformUtility.WorldToScreenPoint(null,slot.nameLabel.transform.position));
            slot.OnPointerEnter(new PointerEventData(EventSystem.current)); yield return null;
            var tooltip = formation.tooltip;
            Check(tooltip.IsOpen && tooltip.card.body.text == StudentCardView.Body(slot.Student),"same student card content");
            Check(Mathf.Abs(Angle(tooltip.panel))<=10.001f,"battle card rotation bound"); OnScreen(tooltip.panel);
            slot.OnPointerExit(new PointerEventData(EventSystem.current));
            Point(LinkPoint(tooltip.card.body,"운동부")); yield return new WaitForSecondsRealtime(.5f);
            Check(tooltip.IsOpen && popup.IsOpen,"pointer transfer retains card and synergy popup");
            Capture(battle.hud.GetComponent<Canvas>(),"04-battle-student-card");
            battle.audioPanel.openButton.onClick.Invoke(); yield return null;
            Check(!tooltip.IsOpen && !popup.IsOpen,"modal dismisses both popups");
            battle.audioPanel.closeButton.onClick.Invoke();
            for (int n=0;n<20;n++)
            {
                tooltip.Hide(); tooltip.Show(slot);
                Check(Mathf.Abs(Angle(tooltip.panel))<=10.001f,"repeat battle rotation bound"); OnScreen(tooltip.panel);
            }
            tooltip.Hide(); Point(Vector2.zero);
            // Exact-count invalidation after live formation changes.
            Roster(run,30,31,32); formation.Render(run); battle.hud.Render(run,"",false,false,false);
            Check(!battle.hud.synergyLabel.text.Contains("연극부"),"three theatre absent from active list");
            Check(SynergyText.Popup(run,"연극부").Contains("<color=#999999>(1)"),"exceeded exact tier gray");
            // Largest documented tooltip must fit the authored panel at common resolutions.
            foreach (var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1280,720)})
            {
                Screen.SetResolution(size.x,size.y,FullScreenMode.Windowed); yield return null; yield return null;
                popup.Show(this,"분위기메이커",new Vector2(Screen.width-5,Screen.height-5),null);
                Canvas.ForceUpdateCanvases(); popup.label.ForceMeshUpdate();
                Check(!popup.label.isTextOverflowing,"long tooltip text fits"); OnScreen(popup.panel);
                Capture(battle.hud.GetComponent<Canvas>(),"05-mood-"+size.x);
                popup.Hide(this);
            }
        }
        Vector2 LinkPoint(TMP_Text label,string name)
        {
            Canvas.ForceUpdateCanvases(); label.ForceMeshUpdate();
            var links = label.textInfo.linkInfo.Take(label.textInfo.linkCount).Where(l => l.GetLinkID()==name).ToArray();
            Check(links.Length==1,"link exists "+name);
            var character = label.textInfo.characterInfo[links[0].linkTextfirstCharacterIndex];
            return RectTransformUtility.WorldToScreenPoint(null,label.transform.TransformPoint((character.bottomLeft+character.topRight)*.5f));
        }
        void Point(Vector2 point) => InputSystem.QueueStateEvent(mouse,new MouseState{position=point});
        static float Angle(Transform target) => Mathf.DeltaAngle(0,target.localEulerAngles.z);
        void OnScreen(RectTransform rect)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            foreach (var p in corners) Check(p.x>=-1 && p.x<=Screen.width+1 && p.y>=-1 && p.y<=Screen.height+1,"on screen "+rect.name+" "+p);
        }
        static void Roster(PrototypeRun run,params int[] ids)
        { run.Students.Clear(); run.Students.AddRange(ids.Select(i=>new StudentState(PrototypeCatalog.Students.Single(d=>d.Id=="Girl"+i)))); }
        void Capture(Canvas canvas,string name) => BattleUiCapture.Capture(canvas,Path.Combine(output,name+".png"));
        void Check(bool pass,string message) { assertions++; if(!pass)throw new InvalidOperationException(message); }
    }
}
