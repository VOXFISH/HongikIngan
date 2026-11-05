#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEditor.SceneManagement;

public class BulkFontReplacer : EditorWindow
{
    private TMP_FontAsset targetFont;

    [MenuItem("Tools/Bulk Replace TMP Fonts")]
    public static void ShowWindow()
    {
        GetWindow<BulkFontReplacer>("Font Replacer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Replace All TMP Fonts in Open Scenes", EditorStyles.boldLabel);

        targetFont = (TMP_FontAsset)EditorGUILayout.ObjectField("New Font", targetFont, typeof(TMP_FontAsset), false);

        if (GUILayout.Button("Replace Fonts") && targetFont != null)
        {
            ReplaceAllFonts();
        }
    }

    private void ReplaceAllFonts()
    {
        // 1. Process active scene objects
        TMP_Text[] sceneTexts = Resources.FindObjectsOfTypeAll<TMP_Text>();
        int count = 0;

        foreach (TMP_Text t in sceneTexts)
        {
            // Avoid modifying built-in Unity editor elements
            if (EditorUtility.IsPersistent(t) || !t.gameObject.scene.IsValid() || !t.gameObject.scene.isLoaded || t.hideFlags == HideFlags.NotEditable || t.hideFlags == HideFlags.HideAndDontSave)
                continue;

            Undo.RecordObject(t, "Bulk TMP Font Replace");
            t.font = targetFont;
            count++;

            // If it's a scene object, mark the scene dirty so it saves
            if (!EditorUtility.IsPersistent(t.gameObject))
            {
                EditorSceneManager.MarkSceneDirty(t.gameObject.scene);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Successfully replaced font on {count} TMP Text components!");
    }
}

#endif
