#if UNITY_EDITOR
using TMPro;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HongikIngan.Editor
{
    public static class BattlePolishBuilder
    {
        const string SlotPath = "Assets/UI/Prefabs/BattleStudentSlot.prefab";
        static readonly Color Panel = new Color(.12f,.10f,.20f,.96f);
        public static void Build() { Apply(); PrototypeBuilder.BuildExpansion(); }

        [MenuItem("HongikIngan/Connect Battle boss reactions")]
        public static void ConnectBossReactions()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var ko = AssetDatabase.LoadAllAssetsAtPath("Assets/sprite/background/monsterKO.png")
                .OfType<Sprite>().OrderByDescending(s => s.rect.width * s.rect.height).First();
            var flash = AssetDatabase.LoadAssetAtPath<Material>("Assets/UI/StudentTransformation.mat");
            if (flash == null) throw new System.InvalidOperationException("Boss white-flash material is missing.");
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Battle.unity");
            var presentation = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<BattlePresentation>(true)).Single();
            var boss = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Image>(true))
                .Single(i => i.name == "BossVisual");
            presentation.bossVisual = boss;
            presentation.bossKoSprite = ko;
            presentation.bossFlashMaterial = flash;
            var orb = (RectTransform)presentation.orbDamage.transform.parent;
            // A centered pivot lets radius change without shifting the orb's center or X coordinate.
            if (orb.pivot != new Vector2(.5f, .5f))
            {
                var center = orb.TransformPoint(orb.rect.center);
                orb.pivot = new Vector2(.5f, .5f);
                orb.position = center;
            }
            presentation.thrownOrb = orb;
            Canvas.ForceUpdateCanvases();
            var screenRect = (RectTransform)orb.GetComponentInParent<Canvas>().rootCanvas.transform;
            orb.position = screenRect.TransformPoint(screenRect.rect.center);
            presentation.orbMinRadius = .6f;
            presentation.orbMaxRadius = 1.5f;
            var group = orb.GetComponent<CanvasGroup>();
            if (group == null) group = orb.gameObject.AddComponent<CanvasGroup>();
            presentation.orbGroup = group;
            // The log is stationary UI, so it must not travel or fade with the orb.
            if (presentation.combatLogGroup != null && presentation.combatLogGroup.transform.IsChildOf(orb))
                presentation.combatLogGroup.transform.SetParent(presentation.transform, true);
            var face = boss.transform.Find("BossFace") as RectTransform;
            if (face == null)
            {
                face = new GameObject("BossFace", typeof(RectTransform)).GetComponent<RectTransform>();
                face.SetParent(boss.transform, false);
                face.anchorMin = face.anchorMax = new Vector2(.3f, .78f);
                face.anchoredPosition = Vector2.zero;
                face.sizeDelta = Vector2.zero;
            }
            presentation.bossFace = face;
            var canvas = boss.transform.parent;
            presentation.impactShakeTargets = new[] { canvas.Find("Background") as RectTransform,
                canvas.Find("Foreground") as RectTransform, boss.rectTransform }.Where(t => t != null).ToArray();
            EditorUtility.SetDirty(presentation);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("BATTLE_BOSS_REACTIONS_SAVED: beam tremble, white 0.2s, monsterKO.");
        }

        [MenuItem("HongikIngan/Polish full-screen Battle presentation")]
        public static void Apply()
        {
            BattleArcLayoutBuilder.BindAvailableArtwork();
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Battle.unity");
            var screen = Object.FindFirstObjectByType<BattleScreen>();
            var layout = (RectTransform)screen.hud.transform.Find("Layout");
            if (layout.Find("FullBleedPresentationV1") != null) return;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            var prefab = PrefabUtility.LoadPrefabContents(SlotPath);
            try
            {
                var slot = prefab.GetComponent<BattleStudentSlot>();
                var panel = new GameObject("CalculationPanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
                panel.transform.SetParent(slot.studentVisual,false);
                Put(panel.transform,-65,-157,260,108);
                panel.GetComponent<Image>().color = Panel;
                panel.GetComponent<Image>().raycastTarget = false;
                slot.calculationGroup = panel.GetComponent<CanvasGroup>();
                slot.calculationGroup.blocksRaycasts = false; slot.calculationGroup.interactable = false;
                slot.calculationLabel.transform.SetParent(panel.transform,false);
                Put(slot.calculationLabel.transform,12,9,236,90);
                Style(slot.calculationLabel,20,24);
                slot.calculationLabel.alignment = TextAlignmentOptions.Center;
                slot.animateEffects = true; slot.hopHeight = 14; slot.hopDuration = .5f;
                panel.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(prefab,SlotPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            scene = EditorSceneManager.OpenScene("Assets/Scenes/Battle.unity");
            screen = Object.FindFirstObjectByType<BattleScreen>();
            layout = (RectTransform)screen.hud.transform.Find("Layout");
            var marker = new GameObject("FullBleedPresentationV1",typeof(RectTransform)); marker.transform.SetParent(layout,false);
            // 1600x900 is 16:9: at Full HD the authored UI scales uniformly by 1.2.
            var canvas = screen.hud.GetComponent<Canvas>();
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.referenceResolution = new Vector2(1600,900);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            layout.sizeDelta = new Vector2(1600,900);
            // Stretch scenery to the Canvas itself, so even other aspect ratios have no exposed clear color.
            var background = (RectTransform)layout.Find("Background");
            background.SetParent(canvas.transform,false); background.SetAsFirstSibling(); Stretch(background);
            var city = layout.Find("CityBack");
            var boss = city.Find("BossVisual"); Shift(boss,80);
            var sky = city.GetComponent<Image>(); if (sky != null) sky.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,1600);
            foreach (var image in city.GetComponentsInChildren<Image>(true))
                if (image.rectTransform.rect.width >= 1400) image.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,1600);
            // Existing objects remain independently editable; only this explicit migration sets their initial position.
            Shift(screen.hud.bossHpLabel.transform.parent,80);
            Shift(screen.hud.bossDebuffsLabel.transform,80);
            Shift(screen.presentation.orbDamage.transform.parent,80);
            Shift(screen.hud.summaryLabel.transform,80);
            Shift(screen.audioPanel.openButton.transform,160);
            Shift(screen.hud.actionButton.transform,160);
            foreach (var slot in screen.formation.slots)
            {
                Shift(slot.transform,80);
                PrefabUtility.RecordPrefabInstancePropertyModifications(slot.transform);
            }
            foreach (var modal in new[]{layout.Find("SoundModal"),layout.Find("TurnTransition"),screen.rewardPopup?.modal.transform})
            {
                if (modal == null) continue;
                var rect = (RectTransform)modal;
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,1600);
                foreach (Transform child in modal)
                {
                    var childRect = child as RectTransform;
                    if (childRect != null && childRect.anchorMin.x == 0 && childRect.anchorMax.x == 0) Shift(child,80);
                }
            }
            var orb = screen.presentation.orbDamage.transform.parent;
            var logPanel = new GameObject("CombatLogPanel",typeof(RectTransform),typeof(Image),typeof(CanvasGroup));
            logPanel.transform.SetParent(orb,false); Put(logPanel.transform,14,108,242,108);
            logPanel.GetComponent<Image>().color = Panel; logPanel.GetComponent<Image>().raycastTarget = false;
            screen.presentation.combatLogGroup=logPanel.GetComponent<CanvasGroup>();
            screen.presentation.combatLogGroup.alpha=0; screen.presentation.combatLogGroup.blocksRaycasts=false;
            screen.hud.logLabel.transform.SetParent(logPanel.transform,false); Put(screen.hud.logLabel.transform,10,8,222,92);
            Style(screen.hud.logLabel,16,18);
            screen.hud.damageLabel.fontSize = 44; screen.hud.damageLabel.fontStyle = FontStyles.Bold;
            screen.hud.bossHpLabel.fontSize = 28; screen.hud.bossHpLabel.fontStyle = FontStyles.Bold;
            screen.presentation.fadeCombatText = true; screen.presentation.fadeInDuration = .12f; screen.presentation.fadeOutDuration = .2f;
            screen.presentation.animateStudents = true;
            EditorUtility.SetDirty(screen.presentation); EditorUtility.SetDirty(screen.hud);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            BattleLayoutBuilder.Validate(screen);
            Debug.Log("BATTLE_FULL_BLEED_PRESENTATION_SAVED");
        }
        static void Style(TMP_Text text,float min,float max)
        {
            text.color = Color.white; text.fontStyle = FontStyles.Bold;
            text.enableAutoSizing = true; text.fontSizeMin = min; text.fontSizeMax = max;
            text.textWrappingMode = TextWrappingModes.Normal; text.overflowMode = TextOverflowModes.Ellipsis;
        }
        static void Shift(Transform transform,float x) { ((RectTransform)transform).anchoredPosition += new Vector2(x,0); }
        static void Stretch(RectTransform rect)
        { rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero; }
        static void Put(Transform transform,float x,float y,float width,float height)
        {
            var rect=(RectTransform)transform; rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
            rect.anchoredPosition=new Vector2(x,-y); rect.sizeDelta=new Vector2(width,height);
        }
    }
}
#endif
