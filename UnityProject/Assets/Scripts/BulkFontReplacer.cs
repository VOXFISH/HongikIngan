using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.SceneManagement;

public class BulkFontReplacer : EditorWindow
{
    private Font targetFont;

    [MenuItem("Tools/Bulk Replace Legacy Fonts")]
    public static void ShowWindow()
    {
        GetWindow<BulkFontReplacer>("Font Replacer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Replace All Legacy Fonts in Open Scenes & Prefabs", EditorStyles.boldLabel);

        targetFont = (Font)EditorGUILayout.ObjectField("New Font", targetFont, typeof(Font), false);

        if (GUILayout.Button("Replace Fonts") && targetFont != null)
        {
            ReplaceAllFonts();
        }
    }

    private void ReplaceAllFonts()
    {
        // 1. Process active scene objects
        Text[] sceneTexts = Resources.FindObjectsOfTypeAll<Text>();
        int count = 0;

        foreach (Text t in sceneTexts)
        {
            // Avoid modifying built-in Unity editor elements
            if (t.hideFlags == HideFlags.NotEditable || t.hideFlags == HideFlags.HideAndDontSave)
                continue;

            Undo.RecordObject(t, "Bulk Font Replace");
            t.font = targetFont;
            count++;

            // If it's a scene object, mark the scene dirty so it saves
            if (!EditorUtility.IsPersistent(t.gameObject))
            {
                EditorSceneManager.MarkSceneDirty(t.gameObject.scene);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Successfully replaced font on {count} Legacy Text components!");
    }
}
