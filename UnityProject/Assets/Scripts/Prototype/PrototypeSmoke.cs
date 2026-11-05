using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using HongikIngan.Audio;

namespace HongikIngan
{
    public sealed class PrototypeSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartIfRequested()
        {
            if(Environment.GetCommandLineArgs().Contains("--prototype-smoke"))
            { var go=new GameObject("PrototypeSmoke"); DontDestroyOnLoad(go); go.AddComponent<PrototypeSmoke>(); }
        }
        IEnumerator Start()
        {
            string output=Path.Combine(Application.dataPath,"../Smoke");
            var args=Environment.GetCommandLineArgs(); int index=Array.IndexOf(args,"--smoke-output");
            if(index>=0&&index+1<args.Length) output=args[index+1];
            Directory.CreateDirectory(output);
            Application.runInBackground=true;
            while(!UnityEngine.Rendering.SplashScreen.isFinished) yield return null;
            yield return new WaitForSeconds(1);
            var sound=SoundManager.EnsureExists();
            float original=sound.GetVolume(AudioCategory.BGM); bool originalMute=sound.GetMuted(AudioCategory.Voice);
            sound.SetVolume(AudioCategory.BGM,.23f); sound.SetMuted(AudioCategory.Voice,true);
            var session=PrototypeSession.Ensure(); session.Run.Settings.bossBaseHp=1;
            session.Run.PracticeBattles=false;
            session.EnterBattle();
            yield return new WaitForSeconds(.7f);
            if(SceneManager.GetActiveScene().name!="Battle"||SoundManager.Instance!=sound||Math.Abs(sound.GetVolume(AudioCategory.BGM)-.23f)>.001f||!sound.GetMuted(AudioCategory.Voice))
            { File.WriteAllText(Path.Combine(output,"result.txt"),"FAILED scene/audio persistence"); Application.Quit(1); yield break; }
            foreach(var step in session.Run.ResolveTurn()) yield return new WaitForSeconds(.12f);
            session.BackToSchool();
            yield return new WaitForSeconds(.7f);
            bool pass=SceneManager.GetActiveScene().name=="School"&&session.Run.Stage==2&&SoundManager.Instance==sound&&FindObjectsByType<SoundManager>(FindObjectsSortMode.None).Length==1;
            sound.SetVolume(AudioCategory.BGM,original); sound.SetMuted(AudioCategory.Voice,originalMute); sound.SaveSettings();
            File.WriteAllText(Path.Combine(output,"result.txt"),pass?"PASS: School -> Battle -> School; victory stage advance; single persistent SoundManager; BGM volume and Voice mute survive scene transitions. FMOD playback skipped: package not installed.":"FAILED return scene");
            Application.Quit(pass?0:1);
        }
    }
}
