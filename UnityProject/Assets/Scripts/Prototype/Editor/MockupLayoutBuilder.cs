#if UNITY_EDITOR
using TMPro;
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace HongikIngan.Editor
{
    // Explicit, one-time editor migration. The saved scenes own every position thereafter.
    public static class MockupLayoutBuilder
    {
        static readonly Color Ink = Hex("2E2118"), Paper = Hex("FFF8EA"), Green = Hex("38965F"), Navy = Hex("2E2A3D"), Gold = Hex("FFD166");
        static TMP_FontAsset Font => BattleUiFont.DefaultFont;
        [MenuItem("HongikIngan/Apply HTML mockup layout once")]
        public static void Apply()
        {
            School(); Battle(); RefineReadability(); AssetDatabase.SaveAssets();
        }
        public static void Build() { Apply(); PrototypeBuilder.Build(); }

        static void RefineReadability()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/School.unity");
            var school=UnityEngine.Object.FindFirstObjectByType<SchoolScreen>(); var ui=school.schoolCanvas.transform.Find("Layout");
            if(ui.Find("ReadabilityV4_2")==null)
            {
                Rect(ui,"ReadabilityV4_2",0,0,0,0); FitLabel(school.dialogs.closeStudent,17);
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            scene=EditorSceneManager.OpenScene("Assets/Scenes/Battle.unity");
            var battle=UnityEngine.Object.FindFirstObjectByType<BattleScreen>(); ui=battle.hud.transform.Find("Layout");
            if(ui.Find("ReadabilityV4_2")!=null) return;
            Rect(ui,"ReadabilityV4_2",0,0,0,0);
            Put(ui.Find("Header/Title"),30,8,1000,34); ui.Find("Header/Title").GetComponent<TMP_Text>().fontSize=28;
            Put(battle.hud.summaryLabel.transform,30,48,1150,24);
            for(int i=0;i<14;i++)
            {
                var building=(RectTransform)ui.Find("CityBack/Building"+i);
                building.anchoredPosition=new Vector2(i*103,building.anchoredPosition.y);
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }

        static void School()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/School.unity");
            var s = UnityEngine.Object.FindFirstObjectByType<SchoolScreen>();
            if (s.schoolCanvas.transform.Find("Layout/MockupLayoutV4") != null)
            {
                if (RefineSchool(s)) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
                return;
            }
            var root = s.schoolCanvas.transform;
            var children = new Transform[root.childCount];
            for (int i=0;i<children.Length;i++) children[i]=root.GetChild(i);
            var ui = Rect(root,"Layout",0,0,1440,810);
            ui.anchorMin=ui.anchorMax=ui.pivot=new Vector2(.5f,.5f); ui.anchoredPosition=Vector2.zero;
            foreach(var child in children) child.SetParent(ui,false);
            Rect(ui,"MockupLayoutV4",0,0,0,0);
            Put(ui.Find("ClassroomBackdrop"),0,0,1440,810); ColorOf(ui.Find("ClassroomBackdrop"),"F3E9D0");
            Put(ui.Find("WallWainscot"),0,0,165,810); ColorOf(ui.Find("WallWainscot"),"E7D6AF");
            Put(ui.Find("Floor"),0,455,1440,355); ColorOf(ui.Find("Floor"),"D7AE79");
            Put(ui.Find("WallRail"),0,455,1440,6);
            for(int i=0;i<12;i++) { var wall=ui.Find("WallPanel"+i); Put(wall,165+i*105,0,8,455); ColorOf(wall,"E6D8B9"); }
            var decor=Rect(ui,"ClassroomArchitecture",0,0,1440,810); decor.SetSiblingIndex(16);
            var door=Frame(decor,"ClassDoor",18,140,76,595,Hex("CA995F"));
            Frame(door,"DoorGlass",15,90,46,245,Hex("B8E4F5"));
            Box(door,"Handle",59,355,7,65,Ink);
            var window=Frame(decor,"Window",1330,130,105,355,Hex("B8E4F5"));
            Box(window,"VerticalFrame",49,0,7,355,Paper); Box(window,"HorizontalFrame",0,170,105,8,Paper);
            var clock=Frame(decor,"WallClock",235,74,100,100,Paper);
            Label(clock,"Numbers",0,0,100,100,"12\n9       3\n6",18);
            Box(clock,"HourHand",49,49,28,4,Ink); Box(clock,"MinuteHand",48,20,4,32,Ink);
            var board=Frame(decor,"PolaroidBoard",435,15,357,242,Hex("C99E73"));
            Label(board,"Title",20,12,317,36,"우리 반 추억",24);
            Label(board,"Hint",25,80,307,95,"펫 후보 중 영입하지 않은 학생의\n폴라로이드가 모여요",18);
            s.photosLabel.transform.SetParent(board,false); Put(s.photosLabel.transform,24,174,309,54); s.photosLabel.fontSize=18;
            var calendar=Frame(decor,"Calendar",805,45,66,75,Paper); Label(calendar,"Caption",0,4,66,67,"방과 후\n학교",18);
            var shelf=Frame(decor,"Bookshelf",886,299,336,290,Hex("BA8957"));
            for(int row=0;row<3;row++)
            {
                Box(shelf,"Shelf"+row,8,90+row*90,320,10,Ink);
                for(int col=0;col<10;col++) Box(shelf,"Book"+row+"_"+col,16+col*30,30+row*90,21,60,Color.Lerp(Paper,Green,(col%4)*.18f));
            }
            Put(s.energyLabel.transform.parent,21,18,228,64);
            Put(s.rosterLabel.transform.parent,260,18,154,64);
            Put(s.stageLabel.transform,905,25,240,42); s.stageLabel.fontSize=19;
            Put(s.restartButton.transform,1175,20,115,45); Put(s.audioButton.transform,1300,20,115,45);
            FitLabel(s.restartButton,18); FitLabel(s.audioButton,18);
            var locker=ui.Find("Chalkboard"); locker.name="RelicLockers"; Put(locker,166,289,627,269);
            foreach(Transform child in locker) if(child.name=="Fill" || child.name=="Caption") child.gameObject.SetActive(false);
            var shell=Box(locker,"LockerBody",0,0,627,269,Hex("1F5A38")); shell.transform.SetAsFirstSibling();
            for(int row=0;row<3;row++) for(int col=0;col<7;col++)
            {
                if(row==0 && col<4) continue;
                var cell=Frame(locker,"LockerDoor"+row+"_"+col,8+col*88,8+row*85,82,78,Green);
                for(int vent=0;vent<3;vent++) Box(cell,"Vent"+vent,14,14+vent*6,51,3,Hex("1D6A3F"));
                Box(cell,"Handle",65,44,5,18,Paper);
            }
            Put(s.relicButtons[0].transform,8,8,258,78); s.relicButtons[0].GetComponent<Image>().color=Paper;
            foreach(Transform child in s.relicButtons[0].transform) if(child.GetComponent<TMP_Text>()==null) child.gameObject.SetActive(false);
            Put(s.relicLabels[0].transform,8,3,242,72); s.relicLabels[0].fontSize=16; s.relicLabels[0].alignment=TextAlignmentOptions.Center;
            s.rerollButton.transform.SetParent(locker,false); Put(s.rerollButton.transform,272,8,82,78);
            s.rerollButton.GetComponent<Image>().color=Gold; s.rerollButton.GetComponentInChildren<TMP_Text>().color=Ink;
            FitLabel(s.rerollButton,15);
            s.ownedRelicsLabel.transform.SetParent(ui,false); Put(s.ownedRelicsLabel.transform,170,225,618,54); s.ownedRelicsLabel.fontSize=16;
            var synergies=s.synergyLabel.transform.parent.parent; Put(synergies,890,105,330,180);
            var syTitle=synergies.Find("Title"); Put(syTitle,12,3,306,32);
            Put(s.synergyLabel.transform.parent,16,40,298,130); Put(s.synergyLabel.transform,0,0,298,130); s.synergyLabel.fontSize=17;
            var desks=s.slots[0].transform.parent; Put(desks,0,0,1440,810);
            var points=new[]{new Vector2(625,350),new Vector2(850,350),new Vector2(1075,350),new Vector2(535,490),new Vector2(760,490),new Vector2(985,490),new Vector2(310,490),new Vector2(50,490),new Vector2(50,490)};
            for(int i=0;i<s.slots.Length;i++)
            {
                var desk=(RectTransform)s.slots[i].transform; desk.anchoredPosition=new Vector2(points[i].x,-points[i].y); desk.localScale=Vector3.one*(i<3?.78f:.9f);
                Label(desk,"AttackOrder",2,245,200,22,"공격 순서 "+(i+1),14);
                s.slots[i].gameObject.SetActive(i<6);
            }
            var obsolete=ui.Find("DeskHint"); if(obsolete!=null) obsolete.gameObject.SetActive(false);
            Put(s.expandButton.transform,905,750,240,38); FitLabel(s.expandButton,17);
            Put(s.statusLabel.transform,300,785,820,23); s.statusLabel.fontSize=15;
            Put(s.battleButton.transform,1193,671,221,110); FitLabel(s.battleButton,26);
            var card=s.detailTitle.transform.parent; card.name="StudentIdCard"; Put(card,27,96,323,510);
            foreach(Transform child in card) if(child.name=="LeftPage"||child.name=="RightPage"||child.name=="Binding"||child.name=="PortraitPlaceholder") child.gameObject.SetActive(false);
            var fill=Box(card,"CardPaper",4,4,315,502,Paper); fill.transform.SetAsFirstSibling();
            Label(card,"SchoolName",20,20,260,38,"홍익인간 - 학생증",23);
            Label(card,"CardCaption",20,63,270,22,"STUDENT ID CARD",14);
            Put(s.detailTitle.transform,20,108,283,58); s.detailTitle.fontSize=22;
            Put(s.detailBody.transform,20,184,283,220); s.detailBody.fontSize=18;
            Put(s.learnButton.transform,18,427,139,64); FitLabel(s.learnButton,17);
            Put(s.sellButton.transform,167,427,139,64); FitLabel(s.sellButton,17);
            card.SetAsLastSibling(); s.studentCard=card.gameObject; s.studentCard.SetActive(false);
            s.dragLayer.SetAsLastSibling();
            Dialogs(s,ui,card);
            AddSceneCurtain();
            RefineSchool(s);
            EditorUtility.SetDirty(s); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }

        static bool RefineSchool(SchoolScreen s)
        {
            var ui=s.schoolCanvas.transform.Find("Layout");
            if(ui.Find("MockupLayoutV4_1")!=null) return false;
            Rect(ui,"MockupLayoutV4_1",0,0,0,0);
            ui.Find("Photos").gameObject.SetActive(false);
            ui.Find("FormationCaption").gameObject.SetActive(false);
            s.ownedRelicsLabel.color=Ink;
            Put(ui.Find("ClassroomArchitecture/WallClock"),235,95,100,100);
            return true;
        }

        static void Dialogs(SchoolScreen s,RectTransform ui,Transform card)
        {
            var d=s.gameObject.AddComponent<SchoolDialogs>(); s.dialogs=d;
            d.closeStudent=Button(card,"Close",275,15,30,30,"×",Paper);
            d.caller=Button(ui,"PetCaller",89,620,143,150,"펫 부르기\n\n학생 영입",Paper); FitLabel(d.caller,23);
            d.offers=Modal(ui,"PetOfferPopup","새로운 인연 - 펫 부르기");
            for(int i=0;i<3;i++)
            {
                s.petButtons[i].transform.SetParent(d.offers.transform,false); Put(s.petButtons[i].transform,315+i*280,220,250,330);
                foreach(Transform child in s.petButtons[i].transform) if(child.GetComponent<TMP_Text>()==null) child.gameObject.SetActive(false);
                Put(s.petLabels[i].transform,15,20,220,280); s.petLabels[i].fontSize=26; s.petLabels[i].alignment=TextAlignmentOptions.Center;
            }
            var oldPets=ui.Find("Pets"); if(oldPets!=null) oldPets.gameObject.SetActive(false);
            d.closeOffers=Button(d.offers.transform,"Close",610,650,220,52,"닫기",Paper);
            d.candidates=Modal(ui,"PetCandidatePopup","마법소녀 한 명을 선택하세요");
            d.candidateButtons=new Button[5]; d.candidateLabels=new TMP_Text[5];
            for(int i=0;i<5;i++) { d.candidateButtons[i]=Button(d.candidates.transform,"Candidate"+i,60+i*267,175,252,400,"",Paper); d.candidateLabels[i]=d.candidateButtons[i].GetComponentInChildren<TMP_Text>(); d.candidateLabels[i].fontSize=18; }
            d.candidateStatus=Label(d.candidates.transform,"Status",80,595,1280,45,"",18,Paper);
            d.skip=Button(d.candidates.transform,"Skip",975,665,365,54,"영입하지 않음 (환불 없음)",Paper);
            d.restart=Modal(ui,"RestartConfirmPopup","새 게임을 시작할까요?");
            Label(d.restart.transform,"Warning",300,270,840,100,"현재 학생-재화-유물-사진과 진행이 초기화됩니다.",25,Paper);
            d.cancelRestart=Button(d.restart.transform,"Cancel",420,500,260,60,"취소",Paper);
            d.acceptRestart=Button(d.restart.transform,"Accept",750,500,310,60,"새 게임 시작",Gold);
            d.audioPanel=Audio(s,ui);
            d.offers.SetActive(false); d.candidates.SetActive(false); d.restart.SetActive(false);
            EditorUtility.SetDirty(d);
        }
        static BattleAudioPanel Audio(SchoolScreen s,Transform ui)
        {
            var audio=s.gameObject.AddComponent<BattleAudioPanel>(); audio.openButton=s.audioButton;
            audio.modal=Modal(ui,"SoundModal","사운드 설정");
            var sliders=new Slider[3]; var toggles=new Toggle[3];
            string[] names={"BGM","SFX","Voice"};
            for(int i=0;i<3;i++)
            {
                Label(audio.modal.transform,names[i],410,230+i*110,120,40,names[i],24,Paper);
                var track=Box(audio.modal.transform,names[i]+"Volume",560,240+i*110,300,22,Paper); track.raycastTarget=true;
                var fill=Box(track.transform,"Fill",0,0,300,22,Green); Stretch(fill.rectTransform);
                var handle=Box(track.transform,"Handle",0,-5,18,32,Gold);
                sliders[i]=track.gameObject.AddComponent<Slider>(); sliders[i].fillRect=fill.rectTransform; sliders[i].handleRect=handle.rectTransform; sliders[i].targetGraphic=handle;
                var box=Box(audio.modal.transform,names[i]+"Mute",920,235+i*110,32,32,Paper); box.raycastTarget=true;
                var check=Box(box.transform,"Check",5,5,22,22,Green);
                toggles[i]=box.gameObject.AddComponent<Toggle>(); toggles[i].targetGraphic=box; toggles[i].graphic=check;
                Label(audio.modal.transform,names[i]+"MuteLabel",965,230+i*110,110,40,"음소거",21,Paper);
            }
            audio.bgmVolume=sliders[0]; audio.sfxVolume=sliders[1]; audio.voiceVolume=sliders[2];
            audio.bgmMute=toggles[0]; audio.sfxMute=toggles[1]; audio.voiceMute=toggles[2];
            audio.statusLabel=Label(audio.modal.transform,"Status",300,585,840,50,"",18,Paper);
            audio.closeButton=Button(audio.modal.transform,"Close",600,665,240,54,"닫기",Paper); audio.modal.SetActive(false);
            return audio;
        }

        static void Battle()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Battle.unity");
            var s=UnityEngine.Object.FindFirstObjectByType<BattleScreen>(); var ui=s.hud.transform.Find("Layout");
            if(ui.Find("MockupLayoutV4")!=null) return;
            Rect(ui,"MockupLayoutV4",0,0,0,0);
            ColorOf(ui.Find("Background"),"CCC3E7");
            var city=Rect(ui,"CityBack",0,0,1440,900); city.SetSiblingIndex(1);
            for(int i=0;i<14;i++)
            {
                float height=160+(i%5)*45;
                var building=Box(city,"Building"+i,i*110,710-height,100,height,Hex(i%2==0?"938BA9":"AAA0BE"));
                for(int row=0;row<3;row++) for(int col=0;col<3;col++) Box(building.transform,"Window"+row+col,12+col*29,25+row*40,13,22,Hex("DAD5E9"));
            }
            var boss=Frame(city,"BossVisual",1040,235,270,475,Hex("554562"));
            Box(boss,"LeftEye",42,70,50,32,Gold); Box(boss,"RightEye",174,70,50,32,Gold);
            Box(boss,"Mouth",73,150,125,20,Ink);
            Box(city,"CityFront",0,710,1440,190,Hex("746B8C"));
            Put(ui.Find("Header"),0,0,1440,85); Put(s.hud.summaryLabel.transform,30,47,1150,29); s.hud.summaryLabel.fontSize=17;
            Put(s.hud.turnLabel.transform,520,90,460,45); s.hud.turnLabel.fontSize=21;
            Put(s.hud.damageLabel.transform,30,100,480,45); s.hud.damageLabel.fontSize=23;
            var hp=s.hud.bossHpLabel.transform.parent; Put(hp,1010,90,340,115);
            Put(hp.Find("BossName"),10,5,320,30); hp.Find("BossName").GetComponent<TMP_Text>().fontSize=23;
            Put(s.hud.bossHpLabel.transform,10,38,320,35); Put(s.hud.bossHpBar.transform,15,87,310,18);
            Put(s.formation.transform,0,0,1440,900);
            var points=new[]{new Vector2(60,180),new Vector2(230,150),new Vector2(30,400),new Vector2(150,590),new Vector2(330,565),new Vector2(430,180),new Vector2(535,550),new Vector2(620,180),new Vector2(755,545)};
            for(int i=0;i<9;i++)
            {
                var rect=(RectTransform)s.formation.slots[i].transform; rect.anchoredPosition=new Vector2(points[i].x,-points[i].y); rect.localScale=Vector3.one*.86f;
                Label(rect,"Order",0,-24,143,22,(i+1).ToString(),18);
            }
            Put(ui.Find("FormationHint"),30,850,1010,30);
            var orb=Frame(s.presentation.transform,"Orb",280,365,160,140,Hex("B9F5FF"));
            Label(orb,"Caption",8,12,144,25,"원기옥",19);
            s.presentation.orbDamage=Label(orb,"Damage",8,47,144,66,"0",25);
            s.presentation.actorLabel=Label(s.presentation.transform,"Actor",220,325,300,35,"",20);
            Put(s.presentation.beam.transform,440,420,660,42);
            s.presentation.damagePop=Label(s.presentation.transform,"DamagePop",955,230,430,90,"0",43,Paper);
            s.presentation.chant=Label(s.presentation.transform,"Chant",300,665,650,70,"영창",30,Paper);
            var bonus=Frame(ui,"BonusPanel",535,365,370,150,Paper);
            s.hud.logLabel.transform.SetParent(bonus,false); Put(s.hud.logLabel.transform,15,15,340,120); s.hud.logLabel.fontSize=17;
            Put(s.hud.statusLabel.transform,30,770,1010,72); s.hud.statusLabel.fontSize=21; s.hud.statusLabel.color=Paper;
            s.turnTransition=s.gameObject.AddComponent<TurnTransitionView>();
            var turn=Box(ui,"TurnTransition",0,0,1440,900,new Color(.12f,.10f,.2f,.82f)); turn.raycastTarget=true;
            var band=Box(turn.transform,"Band",0,330,1440,210,Hex("5D4B80"));
            s.turnTransition.panel=turn.gameObject.AddComponent<CanvasGroup>();
            s.turnTransition.title=Label(band.transform,"TurnTitle",250,35,940,140,"Turn 1",70,Paper);
            turn.gameObject.SetActive(false); ui.Find("SoundModal").SetAsLastSibling();
            AddSceneCurtain();
            EditorUtility.SetDirty(s); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }

        static void AddSceneCurtain()
        {
            var root=new GameObject("SceneTransitionOverlay",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay; root.GetComponent<Canvas>().sortingOrder=200;
            var scaler=root.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1440,900); scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            var view=root.AddComponent<SceneTransitionOverlay>();
            var curtain=Box(root.transform,"Curtain",0,0,1440,900,Navy); Stretch(curtain.rectTransform); curtain.raycastTarget=true;
            view.curtain=curtain.gameObject.AddComponent<CanvasGroup>(); view.curtain.alpha=0; view.curtain.blocksRaycasts=false;
            var band=Box(curtain.transform,"Band",0,0,1000,210,Green); band.rectTransform.anchorMin=band.rectTransform.anchorMax=band.rectTransform.pivot=new Vector2(.5f,.5f); band.rectTransform.anchoredPosition=Vector2.zero;
            view.title=Label(band.transform,"Title",20,25,960,160,"출동!",68,Paper);
        }
        static GameObject Modal(Transform p,string name,string title)
        {
            var bg=Box(p,name,0,0,1440,810,new Color(.13f,.10f,.19f,.95f)); bg.raycastTarget=true;
            var group=bg.gameObject.AddComponent<CanvasGroup>(); group.ignoreParentGroups=true;
            Label(bg.transform,"Title",100,75,1240,70,title,36,Paper); return bg.gameObject;
        }
        static Color Hex(string hex) { ColorUtility.TryParseHtmlString("#"+hex,out var color); return color; }
        static void ColorOf(Transform t,string color) { if(t!=null && t.TryGetComponent<Image>(out var image)) image.color=Hex(color); }
        static void Put(Transform t,float x,float y,float w,float h) { if(t==null)return; var r=(RectTransform)t; r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1); r.anchoredPosition=new Vector2(x,-y); r.sizeDelta=new Vector2(w,h); var fill=t.Find("Fill"); if(fill!=null && t.GetComponent<Slider>()==null) Put(fill,4,4,w-8,h-8); }
        static RectTransform Rect(Transform p,string name,float x,float y,float w,float h) { var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(p,false); Put(r,x,y,w,h); return r; }
        static Image Box(Transform p,string name,float x,float y,float w,float h,Color color) { var i=Rect(p,name,x,y,w,h).gameObject.AddComponent<Image>(); i.color=color; i.raycastTarget=false; return i; }
        static RectTransform Frame(Transform p,string name,float x,float y,float w,float h,Color color) { var f=Box(p,name,x,y,w,h,Ink); Box(f.transform,"Fill",4,4,w-8,h-8,color); return f.rectTransform; }
        static TMP_Text Label(Transform p,string name,float x,float y,float w,float h,string value,int size,Color? color=null) { var t=Rect(p,name,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>(); t.font=Font; t.text=value; t.fontSize=size; t.color=color??Ink; t.alignment=TextAlignmentOptions.Center; t.raycastTarget=false; t.textWrappingMode=TextWrappingModes.Normal; t.overflowMode=TextOverflowModes.Truncate; t.parseCtrlCharacters=false; return t; }
        static Button Button(Transform p,string name,float x,float y,float w,float h,string value,Color color) { var image=Box(p,name,x,y,w,h,color); image.raycastTarget=true; var b=image.gameObject.AddComponent<Button>(); b.targetGraphic=image; Label(b.transform,"Label",8,4,w-16,h-8,value,22); return b; }
        static void FitLabel(Button b,int size) { var t=b.GetComponentInChildren<TMP_Text>(true); Put(t.transform,6,3,((RectTransform)b.transform).rect.width-12,((RectTransform)b.transform).rect.height-6); t.fontSize=size; }
        static void Stretch(RectTransform r) { r.anchorMin=Vector2.zero; r.anchorMax=Vector2.one; r.offsetMin=r.offsetMax=Vector2.zero; }
    }
}
#endif
