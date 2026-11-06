#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace HongikIngan.Editor
{
    public static class SchoolSweepBuilder
    {
        const string ImagePath = "Assets/sprite/UI/sides (1).png";
        const string PrefabPath = "Assets/UI/Prefabs/SchoolBattleSweep.prefab";

        [MenuItem("HongikIngan/Apply School tile sweep transition")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            // Preserve the three existing sprite rectangles and their GUIDs.
            var sprites = AssetDatabase.LoadAllAssetsAtPath(ImagePath).OfType<Sprite>().OrderBy(s => s.rect.y).ToArray();
            if (sprites.Length != 3) throw new InvalidOperationException("sides (1).png must contain exactly three sliced sprites.");
            var logoSprite = AssetDatabase.LoadAllAssetsAtPath("Assets/sprite/UI/ui two.png")
                .OfType<Sprite>().Single(s => s.name == "battle_start");
            bool existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null;
            var root = existing ? PrefabUtility.LoadPrefabContents(PrefabPath)
                : new GameObject("SchoolBattleSweep", typeof(RectTransform));
            try
            {
                var rect = (RectTransform)root.transform;
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                for (int i = 0; !existing && i < sprites.Length; i++)
                {
                    var tile = new GameObject("Tile" + (i + 1), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    tile.transform.SetParent(root.transform, false);
                    var image = tile.GetComponent<Image>();
                    image.sprite = sprites[i]; image.raycastTarget = false;
                    var tileRect = image.rectTransform;
                    tileRect.anchorMin = tileRect.anchorMax = tileRect.pivot = new Vector2(.5f, .5f);
                    tileRect.sizeDelta = new Vector2(3552, 1145);
                    tileRect.anchoredPosition = new Vector2(-2744, 0);
                }
                var logo = root.transform.Find("BattleStartLogo");
                if (logo == null)
                {
                    logo = new GameObject("BattleStartLogo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).transform;
                    logo.SetParent(root.transform, false);
                    var logoRect = (RectTransform)logo;
                    logoRect.anchorMin = logoRect.anchorMax = logoRect.pivot = new Vector2(.5f, .5f);
                    logoRect.anchoredPosition = Vector2.zero;
                    logoRect.sizeDelta = new Vector2(1296, 720);
                }
                logo.SetAsLastSibling();
                logo.localScale = Vector3.zero;
                var logoImage = logo.GetComponent<Image>();
                logoImage.sprite = logoSprite; logoImage.preserveAspect = true; logoImage.raycastTarget = false;
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                var scene = EditorSceneManager.OpenScene("Assets/Scenes/School.unity");
                var screen = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SchoolScreen>(true)).Single();
                screen.battleSweepPrefab = prefab;
                EditorUtility.SetDirty(screen);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                Debug.Log("SCHOOL_TILE_SWEEP_SAVED: 1 purple, 2 pink, 3 orange; interval 0.1 seconds.");
            }
            finally
            {
                if (existing) PrefabUtility.UnloadPrefabContents(root);
                else UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }
}
#endif
