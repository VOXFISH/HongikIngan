#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HongikIngan.Editor
{
    /// <summary>Author the shared desk from DeskSlot0, preserving scene placement and references.</summary>
    public static class SchoolDeskPrefabBuilder
    {
        const string ScenePath = "Assets/Scenes/School.unity";
        const string PrefabPath = "Assets/UI/Prefabs/SchoolDeskSlot.prefab";

        [MenuItem("HongikIngan/Restore School drag layer")]
        public static void RestoreDragLayer()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before saving the School drag layer.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var screen = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SchoolScreen>(true)).Single();
            screen.EnsureDragLayer();
            EditorUtility.SetDirty(screen);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("SCHOOL_DRAG_LAYER_SAVED");
        }

        [MenuItem("HongikIngan/Use DeskSlot0 prefab in School")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before editing the School desks.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var screen = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SchoolScreen>(true)).Single();
            if (screen.dragLayer == null)
            {
                screen.EnsureDragLayer();
                EditorUtility.SetDirty(screen);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            var slots = (SchoolStudentSlot[])screen.slots.Clone();
            var source = slots.Single(s => s.name == "DeskSlot0");
            if (slots.All(s => PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(s.gameObject) == PrefabPath))
            {
                Debug.Log("SCHOOL_DESK_PREFAB_ALREADY_APPLIED");
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
                throw new InvalidOperationException("A desk prefab already exists; preserve its edits before migrating again.");
            if (!AssetDatabase.IsValidFolder("Assets/UI")) AssetDatabase.CreateFolder("Assets", "UI");
            if (!AssetDatabase.IsValidFolder("Assets/UI/Prefabs")) AssetDatabase.CreateFolder("Assets/UI", "Prefabs");
            var prefab = PrefabUtility.SaveAsPrefabAsset(source.gameObject, PrefabPath);
            if (prefab == null) throw new InvalidOperationException("Could not save the DeskSlot0 prefab.");

            var replacements = new Dictionary<Object, Object>();
            var oldObjects = new HashSet<Object>();
            var instances = new List<GameObject>();
            var siblingIndices = new List<int>();
            for (int i = 0; i < slots.Length; i++)
            {
                var old = slots[i];
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, old.transform.parent);
                instances.Add(instance);
                siblingIndices.Add(old.transform.GetSiblingIndex());
                instance.name = old.name;
                instance.SetActive(old.gameObject.activeSelf);
                var before = (RectTransform)old.transform;
                var after = (RectTransform)instance.transform;
                // Keep placement/depth; dimensions and child layout come from DeskSlot0.
                after.anchorMin = before.anchorMin;
                after.anchorMax = before.anchorMax;
                after.pivot = before.pivot;
                after.anchoredPosition3D = before.anchoredPosition3D;
                after.localRotation = before.localRotation;
                after.localScale = before.localScale;
                var order = instance.transform.Find("AttackOrder")?.GetComponent<TMP_Text>();
                if (order != null)
                {
                    order.text = "공격 순서 " + (i + 1);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(order);
                }
                PrefabUtility.RecordPrefabInstancePropertyModifications(after);
                PrefabUtility.RecordPrefabInstancePropertyModifications(instance);

                foreach (var oldTransform in old.GetComponentsInChildren<Transform>(true))
                {
                    var relative = AnimationUtility.CalculateTransformPath(oldTransform, old.transform);
                    var newTransform = relative.Length == 0 ? instance.transform : instance.transform.Find(relative);
                    oldObjects.Add(oldTransform.gameObject);
                    if (newTransform != null) replacements[oldTransform.gameObject] = newTransform.gameObject;
                    foreach (var component in oldTransform.GetComponents<Component>())
                    {
                        if (component == null) continue;
                        oldObjects.Add(component);
                        if (newTransform == null) continue;
                        var type = component.GetType();
                        int index = Array.IndexOf(oldTransform.GetComponents(type), component);
                        var candidates = newTransform.GetComponents(type);
                        if (index < candidates.Length) replacements[component] = candidates[index];
                    }
                }
            }

            // Rebind scene references (including SchoolScreen.slots) before removing originals.
            foreach (var component in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Component>(true)))
            {
                if (component == null || component is Transform || oldObjects.Contains(component)) continue;
                var serialized = new SerializedObject(component);
                var property = serialized.GetIterator();
                bool changed = false;
                while (property.Next(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                    var target = property.objectReferenceValue;
                    if (target == null || !oldObjects.Contains(target)) continue;
                    if (!replacements.TryGetValue(target, out var replacement))
                        throw new InvalidOperationException("DeskSlot0 has no equivalent for referenced object: " + target.name);
                    property.objectReferenceValue = replacement;
                    changed = true;
                }
                if (changed)
                {
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    if (PrefabUtility.IsPartOfPrefabInstance(component))
                        PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                }
            }
            foreach (var old in slots) Object.DestroyImmediate(old.gameObject);
            foreach (int i in Enumerable.Range(0, instances.Count).OrderBy(i => siblingIndices[i]))
            {
                instances[i].transform.SetSiblingIndex(siblingIndices[i]);
                PrefabUtility.RecordPrefabInstancePropertyModifications(instances[i].transform);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("SCHOOL_DESK_PREFAB_SAVED: " + instances.Count + " instances, " + PrefabPath);
        }
    }
}
#endif
