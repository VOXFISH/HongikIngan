using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace HongikIngan.Editor
{
    /// <summary>One-time migration. All layout is serialized by Unity, never rebuilt during Play.</summary>
    public static class BattleLayoutBuilder
    {
        static readonly Color Ink = new Color(.12f, .16f, .24f);
        static readonly Color Purple = new Color(.37f, .30f, .78f);
        static Font font;

        [MenuItem("HongikIngan/Add Battle debug energy button")]
        public static void AddDebugEnergyButton()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Battle.unity");
            var screen = UnityEngine.Object.FindFirstObjectByType<BattleScreen>();
            var attack = screen.hud.actionButton;
            if (attack.transform.parent.Find("DebugEnergyButton") != null) return;
            var debug = UnityEngine.Object.Instantiate(attack, attack.transform.parent);
            debug.name = "DebugEnergyButton";
            var rect = (RectTransform)debug.transform;
            rect.anchoredPosition += Vector2.up * (rect.rect.height + 10);
            debug.transform.SetSiblingIndex(attack.transform.GetSiblingIndex() + 1);
            debug.GetComponentInChildren<Text>().text = "미라클 에너지 +99,999";
            debug.onClick = new Button.ButtonClickedEvent();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(debug.onClick, screen.GrantDebugEnergy);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("BATTLE_DEBUG_ENERGY_BUTTON_SAVED");
        }

        [MenuItem("HongikIngan/Apply editable Battle layout")]
        public static void Apply()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Battle.unity");
            var screen = UnityEngine.Object.FindFirstObjectByType<BattleScreen>();
            if (screen == null) throw new InvalidOperationException("BattleScreen is missing.");
            if (screen.hud != null)
            {
                if (AddPracticeReturn(screen))
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
                Validate(screen);
                Debug.Log("BATTLE_LAYOUT_EXISTS: keeping authored layout.");
                return;
            }
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var root = new GameObject("BattleCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1440, 900);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var ui = Rect(root.transform, "Layout", 0, 0, 1440, 900);
            ui.anchorMin = ui.anchorMax = ui.pivot = new Vector2(.5f, .5f);
            var hud = root.AddComponent<BattleHud>();
            screen.hud = hud;
            Image(ui, "Background", 0, 0, 1440, 900, new Color(.95f, .95f, .99f));
            var header = Image(ui, "Header", 0, 0, 1440, 100, new Color(.89f, .88f, .98f)).transform;
            Text(header, "Title", 30, 14, 1000, 45, "방과 후 · 합동 필살기", 30);
            hud.summaryLabel = Text(header, "Summary", 32, 63, 1150, 30, "STAGE 1 · 미라클 에너지 · 폴라로이드", 18);
            var audioButton = Button(header, "SoundButton", 1255, 25, 155, 46, "사운드 설정");
            hud.turnLabel = Text(ui, "Turn", 35, 120, 950, 50, "TURN 0/3 · 조합을 바꿔 보스를 물리치세요", 24);
            hud.damageLabel = Text(ui, "Damage", 35, 180, 975, 75, "합동 데미지  0", 32);
            var boss = Image(ui, "BossPanel", 1040, 170, 350, 230, new Color(.27f, .24f, .42f)).transform;
            Text(boss, "BossName", 20, 25, 310, 70, "괴수", 32, Color.white, TextAnchor.MiddleCenter);
            hud.bossHpLabel = Text(boss, "HP", 20, 115, 310, 55, "HP 450 / 450", 19, Color.white, TextAnchor.MiddleCenter);
            hud.bossHpBar = Slider(boss, "HealthBar", 25, 190, 300, false);
            var presentation = Rect(ui, "Presentation", 0, 0, 1440, 900).gameObject.AddComponent<BattlePresentation>();
            presentation.beam = Image(presentation.transform, "Beam", 100, 290, 950, 45, new Color(.95f, .65f, .85f)).gameObject;
            presentation.beam.SetActive(false);
            screen.presentation = presentation;

            var formation = Rect(ui, "Formation", 30, 425, 1377, 195).gameObject.AddComponent<BattleFormationView>();
            formation.slots = new BattleStudentSlot[9];
            for (int i = 0; i < formation.slots.Length; i++)
            {
                var panel = Image(formation.transform, "Slot" + (i + 1).ToString("00"), i * 153, 0, 143, 195, new Color(.88f, .88f, .94f));
                panel.raycastTarget = true;
                var slot = panel.gameObject.AddComponent<BattleStudentSlot>();
                slot.emptyLabel = Text(panel.transform, "Empty", 10, 60, 123, 65, "빈 슬롯", 18, Ink, TextAnchor.MiddleCenter);
                slot.studentVisual = Image(panel.transform, "Student", 0, 0, 143, 195, Color.white).rectTransform;
                slot.visualGroup = slot.studentVisual.gameObject.AddComponent<CanvasGroup>();
                slot.portrait = Image(slot.studentVisual, "Portrait", 12, 12, 35, 35, new Color(.9f, .8f, .95f));
                Text(slot.portrait.transform, "Symbol", 0, 0, 35, 35, "✦", 20, Ink, TextAnchor.MiddleCenter);
                slot.nameLabel = Text(slot.studentVisual, "Name", 55, 12, 83, 35, "학생", 17);
                slot.roleLabel = Text(slot.studentVisual, "GradeAndRole", 12, 55, 119, 36, "1학년 · Dealer", 15);
                slot.synergyLabel = Text(slot.studentVisual, "Synergies", 12, 94, 119, 48, "부활동 · 성격", 15);
                slot.skillLabel = Text(slot.studentVisual, "Skill", 12, 146, 119, 42, "스킬 100", 16);
                formation.slots[i] = slot;
            }
            Text(ui, "FormationHint", 35, 380, 980, 32, "학생을 다른 학생 위로 드래그하여 공격 순서를 바꾸세요", 18);
            hud.statusLabel = Text(ui, "CurrentStep", 35, 645, 1360, 50, "준비가 되면 합동 공격을 시작하세요.", 24);
            hud.logLabel = Text(ui, "BattleLog", 40, 706, 1340, 100, "전투 기록", 17, Ink, TextAnchor.UpperLeft);
            hud.actionButton = Button(ui, "ActionButton", 1100, 815, 290, 55, "합동 공격");
            hud.actionLabel = hud.actionButton.GetComponentInChildren<Text>();
            formation.dragLayer = Rect(ui, "DragLayer", 0, 0, 1440, 900);
            screen.formation = formation;
            var audio = root.AddComponent<BattleAudioPanel>();
            audio.openButton = audioButton;
            var modal = Image(ui, "SoundModal", 0, 0, 1440, 900, new Color(0, 0, 0, .4f));
            modal.raycastTarget = true;
            audio.modal = modal.gameObject;
            var dialog = Image(modal.transform, "Dialog", 450, 190, 540, 470, new Color(.83f, .82f, .95f)).transform;
            Text(dialog, "Title", 30, 25, 420, 40, "사운드", 26);
            var volumes = new Slider[3]; var mutes = new Toggle[3];
            string[] names = { "BGM", "SFX", "Voice" };
            for (int i = 0; i < 3; i++)
            {
                var row = Rect(dialog, names[i], 30, 95 + i * 95, 480, 65);
                Text(row, "Category", 0, 0, 110, 35, names[i], 20);
                volumes[i] = Slider(row, "Volume", 120, 10, 210, true);
                mutes[i] = Toggle(row, "Mute", 350, 0);
            }
            audio.bgmVolume = volumes[0]; audio.sfxVolume = volumes[1]; audio.voiceVolume = volumes[2];
            audio.bgmMute = mutes[0]; audio.sfxMute = mutes[1]; audio.voiceMute = mutes[2];
            audio.statusLabel = Text(dialog, "BackendStatus", 30, 365, 480, 40, "", 15);
            audio.closeButton = Button(dialog, "Close", 340, 412, 160, 40, "닫기");
            modal.gameObject.SetActive(false);
            screen.audioPanel = audio;
            AddPracticeReturn(screen);
            root.AddComponent<BattleUiFont>();
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("BattleEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            EditorUtility.SetDirty(screen);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Validate(screen);
            PrototypeChecks.Run();
            Debug.Log("BATTLE_EDITABLE_LAYOUT_SAVED");
        }
        public static void Validate(BattleScreen screen)
        {
            foreach (var component in new MonoBehaviour[] { screen, screen.hud, screen.formation, screen.presentation, screen.audioPanel })
                ValidateReferences(component);
            if (screen.formation.slots.Length != 9) throw new InvalidOperationException("Battle requires nine authored slots.");
            foreach (var slot in screen.formation.slots) ValidateReferences(slot);
            foreach (var root in screen.gameObject.scene.GetRootGameObjects())
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) > 0)
                        throw new InvalidOperationException("Missing script: " + transform.name);
            Debug.Log("BATTLE_LAYOUT_BINDINGS_PASS");
        }
        static bool AddPracticeReturn(BattleScreen screen)
        {
            if (screen.hud.practiceReturnButton != null) return false;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            screen.hud.practiceReturnButton = Button(screen.hud.actionButton.transform.parent, "PracticeReturnButton",
                760, 815, 310, 55, "3턴부터 교실로 복귀");
            screen.hud.practiceReturnButton.transform.SetSiblingIndex(screen.hud.actionButton.transform.GetSiblingIndex() + 1);
            EditorUtility.SetDirty(screen.hud);
            return true;
        }
        static void ValidateReferences(MonoBehaviour component)
        {
            if (component == null) throw new InvalidOperationException("Missing Battle component.");
            var property = new SerializedObject(component).GetIterator();
            while (property.NextVisible(true))
                if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null)
                    throw new InvalidOperationException(component.name + ": missing " + property.propertyPath);
        }
        static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h);
            return rect;
        }
        static Image Image(Transform p, string name, float x, float y, float w, float h, Color color)
        {
            var image = Rect(p, name, x, y, w, h).gameObject.AddComponent<Image>();
            image.color = color; image.raycastTarget = false;
            return image;
        }
        static Text Text(Transform p, string name, float x, float y, float w, float h, string value, int size, Color? color = null, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var text = Rect(p, name, x, y, w, h).gameObject.AddComponent<Text>();
            text.font = font; text.fontSize = size; text.text = value; text.color = color ?? Ink;
            text.alignment = align; text.raycastTarget = false;
            return text;
        }
        static Button Button(Transform p, string name, float x, float y, float w, float h, string value)
        {
            var image = Image(p, name, x, y, w, h, Color.white); image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            Text(image.transform, "Label", 5, 2, w - 10, h - 4, value, 20, Ink, TextAnchor.MiddleCenter);
            return button;
        }
        static Slider Slider(Transform p, string name, float x, float y, float w, bool interactive)
        {
            var background = Image(p, name, x, y, w, 20, new Color(.65f, .63f, .75f));
            background.raycastTarget = interactive;
            var fill = Image(background.transform, "Fill", 0, 0, w, 20, Purple);
            fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            var slider = background.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform; slider.interactable = interactive;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            if (interactive)
            {
                var handle = Image(background.transform, "Handle", 0, -4, 16, 28, Color.white);
                handle.rectTransform.anchorMin = handle.rectTransform.anchorMax = new Vector2(0, .5f);
                handle.rectTransform.pivot = new Vector2(.5f, .5f);
                handle.rectTransform.anchoredPosition = Vector2.zero;
                handle.raycastTarget = true; slider.handleRect = handle.rectTransform; slider.targetGraphic = handle;
            }
            slider.value = 1;
            return slider;
        }
        static Toggle Toggle(Transform p, string name, float x, float y)
        {
            var box = Image(p, name, x, y, 28, 28, Color.white); box.raycastTarget = true;
            var check = Image(box.transform, "Check", 5, 5, 18, 18, Purple);
            var toggle = box.gameObject.AddComponent<Toggle>(); toggle.targetGraphic = box; toggle.graphic = check;
            Text(box.transform, "Label", 34, 0, 85, 30, "음소거", 17);
            return toggle;
        }
    }
}
