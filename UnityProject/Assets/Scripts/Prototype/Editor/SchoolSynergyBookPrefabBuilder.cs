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
    public static class SchoolSynergyBookPrefabBuilder
    {
        const string DeskPath = "Assets/UI/Prefabs/SchoolDeskSlot.prefab";
        const string BookPath = "Assets/UI/Prefabs/SchoolSynergyBook.prefab";

        [MenuItem("HongikIngan/Use synergy book prefab in School desks")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before editing the book prefab.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var root = PrefabUtility.LoadPrefabContents(DeskPath);
            try
            {
                var slot = root.GetComponent<SchoolStudentSlot>();
                if (slot.books.All(b => PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(b) == BookPath))
                {
                    Debug.Log("SCHOOL_SYNERGY_BOOK_PREFAB_ALREADY_APPLIED");
                    return;
                }
                if (AssetDatabase.LoadAssetAtPath<GameObject>(BookPath) != null)
                    throw new InvalidOperationException("A synergy book prefab already exists; preserve its edits before migrating again.");
                var prefab = PrefabUtility.SaveAsPrefabAsset(slot.books[0], BookPath);
                if (prefab == null) throw new InvalidOperationException("Could not save the synergy book prefab.");
                var originals = (GameObject[])slot.books.Clone();
                var siblings = originals.Select(b => b.transform.GetSiblingIndex()).ToArray();
                for (int i = 0; i < originals.Length; i++)
                {
                    var old = originals[i];
                    var book = (GameObject)PrefabUtility.InstantiatePrefab(prefab, old.transform.parent);
                    book.name = old.name;
                    book.SetActive(old.activeSelf);
                    var before = (RectTransform)old.transform;
                    var after = (RectTransform)book.transform;
                    after.anchorMin = before.anchorMin;
                    after.anchorMax = before.anchorMax;
                    after.pivot = before.pivot;
                    after.anchoredPosition3D = before.anchoredPosition3D;
                    after.localRotation = before.localRotation;
                    after.localScale = before.localScale;
                    after.sizeDelta = before.sizeDelta;
                    // The middle book keeps its distinct cover and spine colors.
                    foreach (var image in old.GetComponentsInChildren<Image>(true))
                    {
                        var path = AnimationUtility.CalculateTransformPath(image.transform, old.transform);
                        var target = path.Length == 0 ? book.transform : book.transform.Find(path);
                        var replacement = target == null ? null : target.GetComponent<Image>();
                        if (replacement == null) throw new InvalidOperationException("Missing book graphic: " + path);
                        if (replacement.color != image.color)
                        {
                            replacement.color = image.color;
                            PrefabUtility.RecordPrefabInstancePropertyModifications(replacement);
                        }
                    }
                    var labelPath = AnimationUtility.CalculateTransformPath(slot.bookLabels[i].transform, old.transform);
                    var label = book.transform.Find(labelPath)?.GetComponent<TMP_Text>();
                    if (label == null) throw new InvalidOperationException("Missing synergy label: " + labelPath);
                    if (label.text != slot.bookLabels[i].text)
                    {
                        label.text = slot.bookLabels[i].text;
                        PrefabUtility.RecordPrefabInstancePropertyModifications(label);
                    }
                    slot.books[i] = book;
                    slot.bookLabels[i] = label;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(after);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(book);
                }
                foreach (var old in originals) Object.DestroyImmediate(old);
                foreach (int i in Enumerable.Range(0, slot.books.Length).OrderBy(i => siblings[i]))
                    slot.books[i].transform.SetSiblingIndex(siblings[i]);
                EditorUtility.SetDirty(slot);
                if (PrefabUtility.SaveAsPrefabAsset(root, DeskPath) == null)
                    throw new InvalidOperationException("Could not save the nested book instances.");
                AssetDatabase.SaveAssets();
                Debug.Log("SCHOOL_SYNERGY_BOOK_PREFAB_SAVED: three nested books in SchoolDeskSlot");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
#endif
