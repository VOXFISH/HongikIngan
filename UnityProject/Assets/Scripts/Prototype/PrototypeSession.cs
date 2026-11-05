using UnityEngine;
using UnityEngine.SceneManagement;
using HongikIngan.Audio;

namespace HongikIngan
{
    public sealed class PrototypeSession : MonoBehaviour
    {
        public static PrototypeSession Instance { get; private set; }
        public PrototypeRun Run { get; private set; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic() { Instance = null; }
        public static PrototypeSession Ensure()
        {
            if (Instance == null) new GameObject("PrototypeSession").AddComponent<PrototypeSession>();
            return Instance;
        }
        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            ResetRun();
            SoundManager.EnsureExists();
        }
        public void ResetRun()
        {
            var economy = Resources.Load<EconomySettings>("PrototypeEconomy");
            if (economy == null) economy = ScriptableObject.CreateInstance<EconomySettings>();
            Run = new PrototypeRun(economy, System.Environment.TickCount);
            Run.PracticeBattles = true;
        }
        public void EnterBattle()
        {
            if (Run.StartBattle()) SceneManager.LoadScene("Battle");
        }
        public void BackToSchool()
        {
            if (Run.ReturnToSchool()) SceneManager.LoadScene("School");
        }
        public void BackFromPractice()
        {
            if (Run.ReturnFromPracticeBattle()) SceneManager.LoadScene("School");
        }
        public void Restart() { ResetRun(); SceneManager.LoadScene("School"); }
        void OnDestroy() { if (Instance == this) Instance = null; }
    }
}
