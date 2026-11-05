#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace HongikIngan.Editor
{
    public static class SchoolPortraitBuilder
    {
        const string Folder = "Assets/sprite/characters";
        const string CatalogPath = "Assets/UI/StudentPortraitCatalog.asset";
        const string DeskPath = "Assets/UI/Prefabs/SchoolDeskSlot.prefab";

        [MenuItem("HongikIngan/Connect School student portraits")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before saving portrait bindings.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var magical = Directory.GetFiles("Assets/sprite/magical_char", "*.png")
                .ToDictionary(p => "Girl" + int.Parse(Regex.Match(Path.GetFileName(p), @"^Girl(\d+)", RegexOptions.IgnoreCase).Groups[1].Value),
                    p => ImportPortrait(p));
            var entries = Directory.GetFiles(Folder, "*.png").OrderBy(p => p).Select(path =>
            {
                string assetPath = path.Replace('\\', '/');
                var match = Regex.Match(Path.GetFileNameWithoutExtension(path), @"^Girl(\d+)$", RegexOptions.IgnoreCase);
                if (!match.Success) throw new InvalidOperationException("Unrecognized student image ID: " + assetPath);
                var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
                if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single)
                {
                    // Each PNG is a complete portrait, not an animation sheet. Keep detached artwork too.
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.SaveAndReimport();
                }
                var sprites = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().ToArray();
                if (sprites.Length != 1)
                    throw new InvalidOperationException("Expected exactly one imported sprite: " + assetPath);
                string id = "Girl" + int.Parse(match.Groups[1].Value);
                magical.TryGetValue(id, out var variant);
                return new StudentPortraitCatalog.Entry { studentId = id, sprite = sprites[0], magicalSprite = variant };
            }).ToArray();
            if (entries.Select(e => e.studentId).Distinct().Count() != entries.Length)
                throw new InvalidOperationException("Duplicate student image IDs.");
            var catalog = AssetDatabase.LoadAssetAtPath<StudentPortraitCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<StudentPortraitCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            catalog.entries = entries;
            EditorUtility.SetDirty(catalog);
            const string materialPath = "Assets/UI/StudentTransformation.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/UI/StudentTransformation.shader");
                if (shader == null) throw new InvalidOperationException("Student transformation shader has not imported yet.");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            var root = PrefabUtility.LoadPrefabContents(DeskPath);
            try
            {
                var slot = root.GetComponent<SchoolStudentSlot>();
                var portrait = slot.studentVisual.Find("StudentPlaceholder");
                if (portrait == null) throw new InvalidOperationException("StudentPlaceholder is missing from the desk prefab.");
                slot.studentPortrait = portrait.GetComponent<Image>();
                if (slot.studentPortrait == null) throw new InvalidOperationException("StudentPlaceholder Image is missing.");
                var fill = portrait.Find("Fill");
                if (fill != null) UnityEngine.Object.DestroyImmediate(fill.gameObject);
                slot.portraitCatalog = catalog;
                slot.transformationMaterial = material;
                if (slot.selectionFrame != null) slot.selectionFrame.enabled = false;
                slot.studentPortrait.sprite = null;
                slot.studentPortrait.color = Color.white;
                slot.studentPortrait.enabled = false;
                slot.studentPortrait.preserveAspect = true;
                slot.studentPortrait.raycastTarget = false;
                EditorUtility.SetDirty(slot);
                if (PrefabUtility.SaveAsPrefabAsset(root, DeskPath) == null)
                    throw new InvalidOperationException("Could not save portrait bindings.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            Debug.Log("SCHOOL_PORTRAITS_SAVED: " + entries.Length + " student ID mappings");
        }
        static Sprite ImportPortrait(string path)
        {
            path = path.Replace('\\', '/');
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single || !importer.alphaIsTransparency)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
#endif
