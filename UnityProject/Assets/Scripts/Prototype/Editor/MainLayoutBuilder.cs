#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace HongikIngan.Editor
{
    public static class MainLayoutBuilder
    {
        const string Path = "Assets/Scenes/Main.unity";
        static Font font;

        [MenuItem("HongikIngan/Create Main menu")]
        public static void Apply()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(Path))
            {
                EditorSceneManager.OpenScene(Path);
                Validate();
                return;
            }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.transform.position = new Vector3(0, 0, -10);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.95f, .94f, .99f);
            var root = new GameObject("MainCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1440, 900);
            scaler.matchWidthOrHeight = .5f;
            var screen = root.AddComponent<MainMenuScreen>();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var layout = Rect(root.transform, "Layout", 0, 0, 1440, 900);
            layout.anchorMin = layout.anchorMax = layout.pivot = new Vector2(.5f, .5f);
            Panel(layout, "Background", 0, 0, 1440, 900, new Color(.95f, .94f, .99f));
            Panel(layout, "Card", 370, 120, 700, 660, Color.white);
            Panel(layout, "Accent", 370, 120, 700, 12, new Color(.75f, .38f, .62f));
            Label(layout, "Title", 420, 195, 600, 80, "홍익인간", 54, new Color(.22f, .19f, .35f));
            Label(layout, "Subtitle", 420, 285, 600, 55, "마법소녀전대 · 새로운 하루", 24, new Color(.5f, .38f, .62f));
            Label(layout, "StartingRoster", 420, 365, 600, 65, "100 에너지와 1·2학년 소녀 3명으로\n학교에서 준비를 시작합니다", 21, new Color(.28f, .29f, .39f));
            screen.startButton = Button(layout, "StartBattleButton", 500, 475, "전투 시작", new Color(.39f, .30f, .63f), Color.white);
            screen.exitButton = Button(layout, "EndBattleButton", 500, 575, "전투 종료", new Color(.93f, .90f, .95f), new Color(.25f, .22f, .34f));
            Label(layout, "Flow", 420, 690, 600, 40, "학교에서 전대를 준비한 뒤 전투로 출발하세요", 18, new Color(.45f, .43f, .51f));
            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            EditorSceneManager.SaveScene(scene, Path);
            Validate();
        }

        static void Validate()
        {
            var screen = UnityEngine.Object.FindFirstObjectByType<MainMenuScreen>();
            if (screen == null || screen.startButton == null || screen.exitButton == null ||
                UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
                throw new InvalidOperationException("Main menu references are missing.");
            Debug.Log("MAIN_LAYOUT_BINDINGS_PASS");
        }

        static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }
        static Image Panel(Transform parent, string name, float x, float y, float width, float height, Color color)
        {
            var panel = Rect(parent, name, x, y, width, height).gameObject.AddComponent<Image>();
            panel.color = color; panel.raycastTarget = false; return panel;
        }
        static void Label(Transform parent, string name, float x, float y, float width, float height, string value, int size, Color color)
        {
            var label = Rect(parent, name, x, y, width, height).gameObject.AddComponent<Text>();
            label.font = font; label.text = value; label.fontSize = size; label.color = color;
            label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false;
        }
        static Button Button(Transform parent, string name, float x, float y, string value, Color background, Color foreground)
        {
            var panel = Panel(parent, name, x, y, 440, 76, background); panel.raycastTarget = true;
            var button = panel.gameObject.AddComponent<Button>(); button.targetGraphic = panel;
            Label(panel.transform, "Label", 12, 0, 416, 76, value, 26, foreground);
            return button;
        }
    }
}
#endif
