#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HongikIngan.Editor
{
    public static class BattlePrefabBuilder
    {
        [MenuItem("HongikIngan/Use Battle slot and building prefabs")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before editing Battle prefabs.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Battle.unity");
            var formation = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<BattleFormationView>(true)).Single();
            var slots = formation.slots.Select(s => s.gameObject).ToArray();
            ConvertGroup(scene, slots, "Assets/UI/Prefabs/BattleStudentSlot.prefab");
            var city = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<RectTransform>(true)).Single(t => t.name == "CityBack");
            var buildings = Enumerable.Range(0, 14).Select(i => city.Find("Building" + i).gameObject).ToArray();
            ConvertGroup(scene, buildings, "Assets/UI/Prefabs/BattleBuilding.prefab");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("BATTLE_PREFABS_SAVED: 9 slots and 14 buildings");
        }

        static void ConvertGroup(Scene scene, GameObject[] originals, string path)
        {
            if (originals.All(o => PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(o) == path)) return;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
                throw new InvalidOperationException("Preserve existing prefab edits before migrating: " + path);
            var prefab = PrefabUtility.SaveAsPrefabAsset(originals[0], path);
            if (prefab == null) throw new InvalidOperationException("Could not save " + path);
            var replacements = new Dictionary<Object, Object>();
            var oldObjects = new HashSet<Object>();
            var instances = new List<GameObject>();
            var siblings = originals.Select(o => o.transform.GetSiblingIndex()).ToArray();
            foreach (var old in originals)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, old.transform.parent);
                instances.Add(instance);
                instance.name = old.name;
                foreach (var oldTransform in old.GetComponentsInChildren<Transform>(true))
                {
                    string relative = AnimationUtility.CalculateTransformPath(oldTransform, old.transform);
                    var target = relative.Length == 0 ? instance.transform : instance.transform.Find(relative);
                    if (target == null) throw new InvalidOperationException("Different prefab hierarchy: " + old.name + "/" + relative);
                    oldObjects.Add(oldTransform.gameObject);
                    replacements[oldTransform.gameObject] = target.gameObject;
                    target.gameObject.SetActive(oldTransform.gameObject.activeSelf);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(target.gameObject);
                    var before = oldTransform as RectTransform;
                    var after = target as RectTransform;
                    if (before != null && after != null)
                    {
                        after.anchorMin = before.anchorMin;
                        after.anchorMax = before.anchorMax;
                        after.pivot = before.pivot;
                        after.sizeDelta = before.sizeDelta;
                        after.anchoredPosition3D = before.anchoredPosition3D;
                    }
                    target.localRotation = oldTransform.localRotation;
                    target.localScale = oldTransform.localScale;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(target);
                    foreach (var component in oldTransform.GetComponents<Component>())
                    {
                        if (component == null) continue;
                        oldObjects.Add(component);
                        var type = component.GetType();
                        int index = Array.IndexOf(oldTransform.GetComponents(type), component);
                        var candidates = target.GetComponents(type);
                        if (index >= candidates.Length) throw new InvalidOperationException("Different component layout: " + old.name + "/" + relative);
                        var replacement = candidates[index];
                        replacements[component] = replacement;
                        if (component is Image image && replacement is Image newImage)
                        {
                            newImage.color = image.color;
                            newImage.sprite = image.sprite;
                            PrefabUtility.RecordPrefabInstancePropertyModifications(newImage);
                        }
                        if (component is TMP_Text text && replacement is TMP_Text newText)
                        {
                            newText.text = text.text;
                            newText.color = text.color;
                            newText.fontSize = text.fontSize;
                            newText.alignment = text.alignment;
                            PrefabUtility.RecordPrefabInstancePropertyModifications(newText);
                        }
                    }
                }
            }
            // Relink formation slots and any other scene references before deleting originals.
            foreach (var component in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Component>(true)))
            {
                if (component == null || component is Transform || oldObjects.Contains(component)) continue;
                var serialized = new SerializedObject(component);
                var property = serialized.GetIterator();
                bool changed = false;
                while (property.Next(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                    var value = property.objectReferenceValue;
                    if (value == null || !oldObjects.Contains(value)) continue;
                    if (!replacements.TryGetValue(value, out var replacement))
                        throw new InvalidOperationException("Cannot preserve reference: " + value.name);
                    property.objectReferenceValue = replacement;
                    changed = true;
                }
                if (!changed) continue;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                if (PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            }
            foreach (var old in originals) Object.DestroyImmediate(old);
            foreach (int i in Enumerable.Range(0, instances.Count).OrderBy(i => siblings[i]))
            {
                instances[i].transform.SetSiblingIndex(siblings[i]);
                PrefabUtility.RecordPrefabInstancePropertyModifications(instances[i].transform);
            }
        }
    }
}
#endif
