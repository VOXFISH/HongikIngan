#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HongikIngan.Editor
{
    public static class PrototypeBuilder
    {
        [MenuItem("HongikIngan/Create prototype scenes")]
        public static void Create()
        {
            Directory.CreateDirectory("Assets/Resources");
            AssetDatabase.Refresh();
            var economy=AssetDatabase.LoadAssetAtPath<EconomySettings>("Assets/Resources/PrototypeEconomy.asset");
            if(economy==null) AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<EconomySettings>(),"Assets/Resources/PrototypeEconomy.asset");
            MainLayoutBuilder.Apply();
            CreateScene<SchoolScreen>("School");
            SchoolLayoutBuilder.Apply();
            CreateScene<BattleScreen>("Battle");
            BattleLayoutBuilder.Apply();
            var paths=new[]{"Assets/Scenes/Main.unity","Assets/Scenes/School.unity","Assets/Scenes/Battle.unity"};
            EditorBuildSettings.scenes=paths.Select(p=>new EditorBuildSettingsScene(p,true))
                .Concat(EditorBuildSettings.scenes.Where(s=>!paths.Contains(s.path))).ToArray();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("PROTOTYPE_SCENES_CREATED");
        }
        private static void CreateScene<T>(string name) where T:MonoBehaviour
        {
            string path="Assets/Scenes/"+name+".unity";
            if(File.Exists(path)) return;
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera=new GameObject("Camera").AddComponent<Camera>();
            camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.95f,.95f,.99f);
            camera.transform.position=new Vector3(0,0,-10);
            camera.orthographic=true;
            camera.gameObject.AddComponent<AudioListener>();
            new GameObject(name+"Screen").AddComponent<T>();
            EditorSceneManager.SaveScene(scene,path);
        }
        public static void Build()
        {
            Create();
            Directory.CreateDirectory("Builds/Prototype");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes=new[]{"Assets/Scenes/Main.unity","Assets/Scenes/School.unity","Assets/Scenes/Battle.unity"},
                locationPathName="Builds/Prototype/HongikIngan.exe", target=BuildTarget.StandaloneWindows64,
                options=BuildOptions.None
            });
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new Exception("Prototype build failed: "+report.summary.result);
            Debug.Log("PROTOTYPE_BUILD_PASSED");
        }
        public static void ValidateExpansion()
        {
            SchoolLayoutBuilder.ApplyOverflowRecruitment();
            BattleLayoutBuilder.ApplyRewardPopup();
            PrototypeChecks.Run();
        }
        public static void BuildExpansion()
        {
            ValidateExpansion();
            Build();
        }
        public static void BuildFortune()
        {
            SchoolLayoutBuilder.ApplyFortune();
            Directory.CreateDirectory("Builds/Fortune");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { "Assets/Scenes/Main.unity", "Assets/Scenes/School.unity", "Assets/Scenes/Battle.unity" },
                locationPathName = "Builds/Fortune/HongikIngan.exe", target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new Exception("Fortune build failed: " + report.summary.result);
            Debug.Log("FORTUNE_BUILD_PASS");
        }
    }
}
#endif
