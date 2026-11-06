#if UNITY_EDITOR
using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HongikIngan.Editor
{
    // Explicit editor migration. Runtime binding never recalculates the authored arc positions.
    public static class BattleArcLayoutBuilder
    {
        const string SlotPath = "Assets/UI/Prefabs/BattleStudentSlot.prefab";
        static readonly Color Ink = new Color(.14f,.12f,.23f);
        static readonly Color Paper = new Color(.97f,.96f,1);
        public static void Build() { Apply(); RefineReadability(); RefineTextBounds(); BindAvailableArtwork(); PrototypeBuilder.BuildExpansion(); }
        [MenuItem("HongikIngan/Bind available Girl artwork")]
        public static void BindAvailableArtwork()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Battle.unity");
            var formation=Object.FindFirstObjectByType<BattleFormationView>(); int bound=0;
            foreach(var entry in formation.artwork)
            {
                if(entry.sprite!=null) continue;
                if(!int.TryParse(entry.girlId.Replace("Girl",""),out int id)) continue;
                string path="Assets/sprite/characters/Girl"+id.ToString("00")+".png";
                var importer=AssetImporter.GetAtPath(path) as TextureImporter;
                if(importer==null)continue;
                // Imported art may have separate accent slices; use the character body.
                var sprite=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
                    .OrderByDescending(s=>s.rect.width*s.rect.height).FirstOrDefault();
                if(sprite==null)
                {
                    importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
                    importer.alphaIsTransparency=true; importer.SaveAndReimport();
                    sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
                }
                if(sprite!=null) { entry.sprite=sprite; bound++; }
            }
            if(bound>0)
            {
                EditorUtility.SetDirty(formation); EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            }
            Debug.Log("BATTLE_ARTWORK_BOUND count="+formation.artwork.Count(a=>a.sprite!=null));
        }
        static void RefineTextBounds()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Battle.unity");
            var screen=Object.FindFirstObjectByType<BattleScreen>(); var ui=screen.hud.transform.Find("Layout");
            if(ui.Find("ArcTextBoundsV1")!=null)return;
            Rect(ui,"ArcTextBoundsV1",0,0,0,0);
            screen.hud.bossHpLabel.transform.parent.GetComponent<Image>().color=Color.clear;
            screen.hud.bossHpLabel.color=Ink;
            Put(screen.hud.logLabel.transform,45,112,180,83);
            screen.hud.logLabel.fontSizeMin=11; screen.hud.logLabel.fontSizeMax=14;
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            var prefab=PrefabUtility.LoadPrefabContents(SlotPath);
            try
            {
                var slot=prefab.GetComponent<BattleStudentSlot>();
                Put(slot.calculationLabel.transform,-45,-127,220,70);
                PrefabUtility.SaveAsPrefabAsset(prefab,SlotPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            AssetDatabase.SaveAssets();
        }
        static void RefineReadability()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Battle.unity");
            var screen=Object.FindFirstObjectByType<BattleScreen>(); var ui=screen.hud.transform.Find("Layout");
            if(screen.formation.artwork.Length==0)
            {
                screen.formation.artwork=PrototypeCatalog.Students.Select(d=>new BattleGirlArtwork{girlId=d.Id}).ToArray();
                EditorUtility.SetDirty(screen.formation);
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            if(ui.Find("ArcReadabilityV1")!=null)return;
            Rect(ui,"ArcReadabilityV1",0,0,0,0);
            var boss=ui.Find("CityBack/BossVisual"); Put(boss,555,120,330,180);
            Put(boss.Find("Mouth"),105,115,120,25);
            var hp=screen.hud.bossHpLabel.transform.parent; Put(hp,525,35,390,75);
            Put(screen.hud.bossDebuffsLabel.transform,930,42,260,160);
            var orb=screen.presentation.orbDamage.transform.parent;
            Put(orb,585,320,270,230);
            Put(orb.Find("Caption"),40,20,190,30);
            Put(screen.presentation.orbDamage.transform,15,55,240,50);
            Put(screen.hud.logLabel.transform,25,111,220,95);
            screen.presentation.beam.transform.SetParent(orb,false);
            Put(screen.presentation.beam.transform,0,0,1,14);
            ((RectTransform)screen.presentation.beam.transform).pivot=new Vector2(0,.5f);
            ApplyEnergyArt(orb.GetComponent<Image>(),screen.presentation.beam.GetComponent<Image>());
            screen.presentation.beam.transform.SetSiblingIndex(orb.Find("Caption").GetSiblingIndex());
            hp.SetSiblingIndex(screen.presentation.transform.GetSiblingIndex()+1);
            screen.hud.statusLabel.gameObject.SetActive(false);
            foreach(var slot in screen.formation.slots)
            {
                var r=(RectTransform)slot.transform; r.anchoredPosition+=new Vector2(5,0);
                PrefabUtility.RecordPrefabInstancePropertyModifications(r);
            }
            EditorUtility.SetDirty(screen.presentation);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        }
        // Both images draw their art untinted and must not block clicks on the UI beneath them.
        static void ApplyEnergyArt(Image orb,Image beam)
        {
            orb.sprite=LoadUiSprite("EnergyOrb"); orb.type=Image.Type.Simple; orb.preserveAspect=true;
            orb.color=Color.white; orb.raycastTarget=false;
            beam.sprite=LoadUiSprite("EnergyBeam"); beam.type=Image.Type.Simple; beam.preserveAspect=false;
            beam.color=Color.white; beam.raycastTarget=false;
            // BattlePresentation sets the length at runtime; this height keeps the art's proportions at the usual distance.
            beam.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,60);
        }
        static Sprite LoadUiSprite(string name)
        {
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/sprite/UI/"+name+".png");
            if(sprite==null)throw new System.InvalidOperationException(name+" sprite is missing.");
            return sprite;
        }
        [MenuItem("HongikIngan/Apply centered arc Battle layout")]
        public static void Apply()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Battle.unity");
            var screen = Object.FindFirstObjectByType<BattleScreen>();
            var ui = screen.hud.transform.Find("Layout");
            if (ui.Find("CenteredArcV1") != null) { BattleLayoutBuilder.Validate(screen); return; }
            // Close the scene before editing its shared slot asset.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            EditSlotPrefab();
            scene = EditorSceneManager.OpenScene("Assets/Scenes/Battle.unity");
            screen = Object.FindFirstObjectByType<BattleScreen>(); ui = screen.hud.transform.Find("Layout");
            Rect(ui,"CenteredArcV1",0,0,0,0);
            ui.Find("Header/Title").gameObject.SetActive(false);
            Put(ui.Find("Header"),0,0,1440,0);
            Put(screen.hud.summaryLabel.transform,300,12,850,26); screen.hud.summaryLabel.fontSize=14;
            Put(screen.audioPanel.openButton.transform,1260,18,155,42);
            Put(screen.hud.turnLabel.transform,24,20,240,60); screen.hud.turnLabel.fontSize=32;
            screen.hud.turnLabel.alignment=TextAlignmentOptions.Left;
            var synergy=Box(ui,"CurrentSynergies",24,100,245,340,new Color(.94f,.92f,1,.95f));
            screen.hud.synergyLabel=Label(synergy.transform,"List",18,16,209,308,"현재 시너지",21);
            screen.hud.synergyLabel.enableAutoSizing=true; screen.hud.synergyLabel.fontSizeMin=13;
            screen.hud.synergyLabel.fontSizeMax=21;
            var boss=ui.Find("CityBack/BossVisual"); Put(boss,600,130,330,205);
            Put(boss.Find("LeftEye"),55,48,62,28); Put(boss.Find("RightEye"),213,48,62,28);
            Put(boss.Find("Mouth"),105,123,120,28);
            var hp=screen.hud.bossHpLabel.transform.parent; Put(hp,570,45,390,75);
            hp.GetComponent<Image>().color=new Color(.22f,.18f,.32f);
            hp.Find("BossName").gameObject.SetActive(false);
            Put(screen.hud.bossHpLabel.transform,10,4,370,42); screen.hud.bossHpLabel.fontSize=25;
            Put(screen.hud.bossHpBar.transform,16,54,358,13);
            screen.hud.bossDebuffsLabel=Label(ui,"BossDebuffs",975,48,250,165,"",18);
            var orb=screen.presentation.orbDamage.transform.parent;
            Put(orb,600,350,330,260);
            orb.Find("Fill").gameObject.SetActive(false);
            Put(orb.Find("Caption"),70,28,190,30); orb.Find("Caption").GetComponent<TMP_Text>().fontSize=21;
            screen.hud.damageLabel.gameObject.SetActive(false);
            screen.hud.damageLabel=screen.presentation.orbDamage;
            Put(screen.presentation.orbDamage.transform,30,64,270,56); screen.presentation.orbDamage.fontSize=38;
            screen.presentation.orbDamage.enableAutoSizing=true; screen.presentation.orbDamage.fontSizeMin=19;
            screen.presentation.orbDamage.fontSizeMax=38;
            screen.hud.logLabel.transform.SetParent(orb,false); Put(screen.hud.logLabel.transform,38,128,254,104);
            screen.hud.logLabel.fontSize=16; screen.hud.logLabel.enableAutoSizing=true;
            screen.hud.logLabel.fontSizeMin=12; screen.hud.logLabel.fontSizeMax=16;
            ui.Find("BonusPanel").gameObject.SetActive(false);
            Put(screen.presentation.chant.transform,460,305,610,42); screen.presentation.chant.fontSize=21;
            screen.presentation.chant.color=Ink;
            screen.presentation.actorLabel.gameObject.SetActive(false); screen.presentation.damagePop.gameObject.SetActive(false);
            Put(screen.presentation.beam.transform,0,0,1,18);
            ((RectTransform)screen.presentation.beam.transform).pivot=new Vector2(0,.5f);
            ApplyEnergyArt(orb.GetComponent<Image>(),screen.presentation.beam.GetComponent<Image>());
            screen.presentation.beam.transform.SetAsFirstSibling();
            screen.presentation.stepDuration=1; screen.presentation.beamDuration=1;
            Put(screen.formation.transform,0,0,1440,900);
            float[] heights={480,555,625,680,705,680,625,555,480};
            for(int i=0;i<screen.formation.slots.Length;i++)
            {
                var slot=screen.formation.slots[i];
                foreach(var child in slot.GetComponentsInChildren<RectTransform>(true))
                    if(child!=slot.transform && PrefabUtility.IsPartOfPrefabInstance(child))
                        PrefabUtility.RevertObjectOverride(child,InteractionMode.AutomatedAction);
                foreach(var item in new[]{slot.roleLabel,slot.synergyLabel,slot.skillLabel,slot.emptyLabel})
                { item.gameObject.SetActive(false); PrefabUtility.RecordPrefabInstancePropertyModifications(item.gameObject); }
                foreach(var image in new[]{slot.GetComponent<Image>(),slot.studentVisual.GetComponent<Image>()})
                { image.color=Color.clear; PrefabUtility.RecordPrefabInstancePropertyModifications(image); }
                var symbol=slot.portrait.transform.Find("Symbol"); if(symbol!=null) {symbol.gameObject.SetActive(false); PrefabUtility.RecordPrefabInstancePropertyModifications(symbol.gameObject);}
                var oldOrder=slot.transform.Find("Order"); if(oldOrder!=null && oldOrder!=slot.orderLabel.transform) oldOrder.gameObject.SetActive(false);
                Put(slot.transform,70+i*145,heights[i],130,175); slot.transform.localScale=Vector3.one;
                PrefabUtility.RecordPrefabInstancePropertyModifications(slot.transform);
            }
            Put(screen.hud.actionButton.transform,1200,815,215,60);
            Put(screen.hud.statusLabel.transform,300,75,250,95); screen.hud.statusLabel.fontSize=16; screen.hud.statusLabel.color=Ink;
            Put(ui.Find("FormationHint"),24,865,450,25); ui.Find("FormationHint").GetComponent<TMP_Text>().fontSize=13;
            var tooltipRoot=Rect(ui,"StudentTooltip",0,0,360,245);
            tooltipRoot.pivot=new Vector2(.5f,0);
            var tooltipImage=tooltipRoot.gameObject.AddComponent<Image>(); tooltipImage.color=Paper; tooltipImage.raycastTarget=false;
            var tooltip=tooltipRoot.gameObject.AddComponent<BattleStudentTooltip>();
            tooltip.panel=tooltipRoot; tooltip.bounds=(RectTransform)ui;
            tooltip.nameLabel=Label(tooltipRoot,"Name",18,15,324,32,"",24);
            tooltip.skillLabel=Label(tooltipRoot,"Skill",18,54,324,30,"",20);
            tooltip.effectLabel=Label(tooltipRoot,"Effect",18,94,324,135,"",17);
            tooltip.effectLabel.enableAutoSizing=true; tooltip.effectLabel.fontSizeMin=12; tooltip.effectLabel.fontSizeMax=17;
            screen.formation.tooltip=tooltip; tooltip.Hide();
            // Modals always cover and block the floating content.
            ui.Find("TurnTransition").SetAsLastSibling(); ui.Find("SoundModal").SetAsLastSibling();
            if(screen.rewardPopup!=null) screen.rewardPopup.modal.transform.SetAsLastSibling();
            EditorUtility.SetDirty(screen.hud); EditorUtility.SetDirty(screen.formation); EditorUtility.SetDirty(screen.presentation);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            BattleLayoutBuilder.Validate(screen);
            Debug.Log("BATTLE_ARC_LAYOUT_SAVED");
        }
        static void EditSlotPrefab()
        {
            var root=PrefabUtility.LoadPrefabContents(SlotPath);
            try
            {
                var slot=root.GetComponent<BattleStudentSlot>();
                Put(root.transform,0,0,130,175); root.GetComponent<Image>().color=Color.clear;
                Put(slot.studentVisual,0,0,130,175); slot.studentVisual.GetComponent<Image>().color=Color.clear;
                Put(slot.nameLabel.transform,0,0,130,28); slot.nameLabel.fontSize=20;
                slot.nameLabel.fontSizeMin=11; slot.nameLabel.fontSizeMax=20; slot.nameLabel.enableAutoSizing=true;
                slot.nameLabel.alignment=TextAlignmentOptions.Center;
                Put(slot.portrait.transform,10,32,110,118); slot.portrait.preserveAspect=true;
                var symbol=slot.portrait.transform.Find("Symbol"); if(symbol!=null) symbol.gameObject.SetActive(false);
                foreach(var item in new[]{slot.roleLabel,slot.synergyLabel,slot.skillLabel,slot.emptyLabel}) item.gameObject.SetActive(false);
                slot.orderLabel=Label(slot.studentVisual,"AttackOrder",40,153,50,24,"1",19); slot.orderLabel.alignment=TextAlignmentOptions.Center;
                slot.buffsLabel=Label(slot.studentVisual,"Buffs",-30,-55,190,50,"",15); slot.buffsLabel.alignment=TextAlignmentOptions.Bottom;
                slot.buffsLabel.enableAutoSizing=true; slot.buffsLabel.fontSizeMin=10; slot.buffsLabel.fontSizeMax=15;
                slot.calculationLabel=Label(slot.studentVisual,"Calculation",-45,-142,220,82,"",18);
                slot.calculationLabel.alignment=TextAlignmentOptions.Center; slot.calculationLabel.enableAutoSizing=true;
                slot.calculationLabel.fontSizeMin=12; slot.calculationLabel.fontSizeMax=18;
                slot.calculationLabel.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root,SlotPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        static void Put(Transform t,float x,float y,float w,float h)
        {
            var r=(RectTransform)t; r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);
            r.anchoredPosition=new Vector2(x,-y); r.sizeDelta=new Vector2(w,h);
            var fill=t.Find("Fill"); if(fill!=null && t.GetComponent<Slider>()==null) Put(fill,4,4,w-8,h-8);
        }
        static RectTransform Rect(Transform p,string name,float x,float y,float w,float h)
        { var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(p,false); Put(r,x,y,w,h); return r; }
        static Image Box(Transform p,string name,float x,float y,float w,float h,Color color)
        { var image=Rect(p,name,x,y,w,h).gameObject.AddComponent<Image>(); image.color=color; image.raycastTarget=false; return image; }
        static TMP_Text Label(Transform p,string name,float x,float y,float w,float h,string text,int size)
        {
            var label=Rect(p,name,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>(); label.font=BattleUiFont.DefaultFont;
            label.fontSize=size; label.text=text; label.color=Ink; label.alignment=TextAlignmentOptions.TopLeft;
            label.enableAutoSizing=false; label.textWrappingMode=TextWrappingModes.Normal;
            label.overflowMode=TextOverflowModes.Truncate; label.raycastTarget=false; return label;
        }
    }
}
#endif
