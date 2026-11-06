#if UNITY_EDITOR
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HongikIngan.Editor
{
    public static class SynergyUiBuilder
    {
        const string CardPath = "Assets/UI/Prefabs/StudentIdCard.prefab";
        static readonly Color Ink = new Color(.10f,.09f,.16f,1);
        public static void Build()
        {
            Apply(); SynergyUiChecks.Run(); PrototypeBuilder.BuildExpansion();
        }
        [MenuItem("HongikIngan/Apply synergy inspection UI")]
        public static void Apply()
        {
            var schoolScene = EditorSceneManager.OpenScene("Assets/Scenes/School.unity");
            var school = Object.FindFirstObjectByType<SchoolScreen>();
            var layout = (RectTransform)school.studentCard.transform.parent;
            if (layout.Find("SynergyInspectionV1") == null)
            {
                NewRect(layout,"SynergyInspectionV1",0,0,0,0);
                var card = school.studentCard.AddComponent<StudentCardView>();
                card.title = school.detailTitle; card.body = school.detailBody;
                // Preserve the authored student-card design, with enough room for skill and trait links.
                card.body.enableAutoSizing = true; card.body.fontSizeMin = 14; card.body.fontSizeMax = 18;
                card.body.richText = true; card.body.raycastTarget = true;
                card.body.textWrappingMode = TextWrappingModes.Normal;
                card.body.overflowMode = TextOverflowModes.Overflow;
                card.body.gameObject.AddComponent<SynergyHover>();
                school.studentCard.GetComponent<Image>().raycastTarget = true;
                PrefabUtility.SaveAsPrefabAssetAndConnect(school.studentCard, CardPath, InteractionMode.AutomatedAction);
                card.bounds = layout;
                var popup = Popup(layout, school.detailBody.font);
                card.body.GetComponent<SynergyHover>().popup = popup;
                SetupList(school.synergyLabel, popup);
                school.synergyLabel.transform.parent.GetComponent<Image>().color = Ink;
                var frame = school.synergyLabel.transform.parent.parent;
                frame.GetComponent<Image>().color = Ink;
                var fill = frame.Find("Fill"); if (fill != null) fill.GetComponent<Image>().color = Ink;
                foreach (var title in frame.GetComponentsInChildren<TMP_Text>()) title.color = Color.white;
                EditorSceneManager.MarkSceneDirty(schoolScene); EditorSceneManager.SaveScene(schoolScene);
            }
            var battleScene = EditorSceneManager.OpenScene("Assets/Scenes/Battle.unity");
            var battle = Object.FindFirstObjectByType<BattleScreen>();
            layout = (RectTransform)battle.hud.transform.Find("Layout");
            if (layout.Find("SynergyInspectionV1") == null)
            {
                NewRect(layout,"SynergyInspectionV1",0,0,0,0);
                Object.DestroyImmediate(battle.formation.tooltip.panel.gameObject);
                var tooltip = layout.Find("SynergyInspectionV1").gameObject.AddComponent<BattleStudentTooltip>();
                battle.formation.tooltip = tooltip;
                var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CardPath), layout);
                var card = root.GetComponent<StudentCardView>(); card.bounds = layout;
                foreach (var button in root.GetComponentsInChildren<Button>(true)) button.gameObject.SetActive(false);
                // Combat inspection uses the same information and trait links, without school-only transactions.
                var panel = (RectTransform)root.transform; panel.pivot = new Vector2(.5f,0);
                tooltip.panel = panel; tooltip.bounds = layout; tooltip.card = card;
                tooltip.nameLabel = card.title; tooltip.effectLabel = card.body; tooltip.skillLabel = card.title;
                tooltip.offset = new Vector2(0,25);
                var group = root.GetComponent<CanvasGroup>();
                if (group != null) { group.blocksRaycasts = true; group.interactable = true; }
                var popup = Popup(layout, card.body.font);
                card.body.GetComponent<SynergyHover>().popup = popup;
                SetupList(battle.hud.synergyLabel, popup);
                battle.hud.synergyLabel.transform.parent.GetComponent<Image>().color = Ink;
                Put(battle.hud.synergyLabel.transform.parent,24,100,260,390);
                Put(battle.hud.synergyLabel.transform,14,14,232,362);
                root.SetActive(false);
                EditorUtility.SetDirty(tooltip);
                EditorUtility.SetDirty(battle.formation);
                EditorSceneManager.MarkSceneDirty(battleScene); EditorSceneManager.SaveScene(battleScene);
            }
            RepairPrefabBindings();
            RefineReadability();
            AssetDatabase.SaveAssets();
            Debug.Log("SYNERGY_UI_SAVED");
        }
        static void RefineReadability()
        {
            var prefab = PrefabUtility.LoadPrefabContents(CardPath);
            try
            {
                foreach (var button in prefab.GetComponentsInChildren<Button>(true))
                {
                    var text = button.GetComponentInChildren<TMP_Text>(true);
                    if (text != null && (text.text == "×" || text.text == "✕")) text.text = "X";
                }
                PrefabUtility.SaveAsPrefabAsset(prefab,CardPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            foreach (string name in new[]{"School","Battle"})
            {
                var scene = EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");
                var popup = Object.FindFirstObjectByType<SynergyPopup>();
                if (popup.bounds.Find("SynergyReadabilityV2") != null) continue;
                NewRect(popup.bounds,"SynergyReadabilityV2",0,0,0,0);
                popup.panel.GetComponent<Image>().color = Ink;
                if (name == "School")
                {
                    var label = Object.FindFirstObjectByType<SchoolScreen>().synergyLabel;
                    ((RectTransform)label.transform.parent.parent).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,320);
                    ((RectTransform)label.transform.parent).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,270);
                }
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
        }
        // Record external scene references and child visibility as actual instance overrides.
        static void RepairPrefabBindings()
        {
            foreach (string name in new[]{"School","Battle"})
            {
                var scene = EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");
                var popup = Object.FindFirstObjectByType<SynergyPopup>();
                StudentCardView card;
                if (name == "School") card = Object.FindFirstObjectByType<SchoolScreen>().studentCard.GetComponent<StudentCardView>();
                else card = Object.FindFirstObjectByType<BattleScreen>().formation.tooltip.card;
                card.bounds = popup.bounds; card.body.GetComponent<SynergyHover>().popup = popup;
                if (name == "Battle") foreach (var button in card.GetComponentsInChildren<Button>(true)) button.gameObject.SetActive(false);
                foreach (var component in card.GetComponentsInChildren<Component>(true))
                {
                    if (component == null) continue;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(component.gameObject);
                }
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
        }
        static void SetupList(TMP_Text text, SynergyPopup popup)
        {
            text.color = Color.white; text.richText = true; text.raycastTarget = true;
            text.enableAutoSizing = true; text.fontSizeMin = 13; text.fontSizeMax = 20;
            if (text.GetComponent<ContentSizeFitter>() != null) { text.enableAutoSizing = false; text.fontSize = 18; }
            text.textWrappingMode = TextWrappingModes.Normal; text.overflowMode = TextOverflowModes.Overflow;
            (text.GetComponent<SynergyHover>() ?? text.gameObject.AddComponent<SynergyHover>()).popup = popup;
        }
        static SynergyPopup Popup(RectTransform layout, TMP_FontAsset font)
        {
            var controller = NewRect(layout,"SynergyPopup",0,0,0,0).gameObject.AddComponent<SynergyPopup>();
            var panel = NewRect(layout,"SynergyPopupPanel",0,0,480,580);
            panel.gameObject.AddComponent<Image>().color = Ink;
            panel.GetComponent<Image>().raycastTarget = false;
            var group = panel.gameObject.AddComponent<CanvasGroup>(); group.blocksRaycasts = false; group.interactable = false;
            var text = NewRect(panel,"Description",22,20,436,540).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.fontSize = 20; text.fontSizeMin = 15; text.fontSizeMax = 20;
            text.enableAutoSizing = true; text.color = Color.white; text.richText = true;
            text.raycastTarget = false; text.alignment = TextAlignmentOptions.TopLeft;
            controller.bounds = layout; controller.panel = panel; controller.label = text;
            panel.gameObject.SetActive(false); return controller;
        }
        static RectTransform NewRect(Transform parent,string name,float x,float y,float w,float h)
        {
            var rect = new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent,false); Put(rect,x,y,w,h); return rect;
        }
        static void Put(Transform target,float x,float y,float w,float h)
        {
            var rect = (RectTransform)target; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0,1);
            rect.anchoredPosition = new Vector2(x,-y); rect.sizeDelta = new Vector2(w,h);
        }
    }
}
#endif
