using System;
using System.Collections.Generic;
using UnityEngine;

namespace HongikIngan.Audio
{
    public enum AudioCategory { BGM, SFX, Voice }

    /// <summary>Persistent FMOD event owner. Settings work even before the FMOD package/banks are configured.</summary>
    public sealed class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }
        public event Action SettingsChanged;
        private readonly float[] volumes = { 0.7f, 0.8f, 0.8f };
        private readonly bool[] muted = new bool[3];
        private bool dirty;
        private string lastError = string.Empty;
        private bool warnedUnavailable;

#if HONGIK_FMOD
        private FMOD.Studio.EventInstance bgm;
        private string bgmPath;
        private readonly List<PlayingEvent> playing = new List<PlayingEvent>();
        private struct PlayingEvent
        {
            public FMOD.Studio.EventInstance Instance;
            public AudioCategory Category;
        }
#endif

        public bool BackendAvailable
        {
            get
            {
#if HONGIK_FMOD
                return true;
#else
                return false;
#endif
            }
        }

        public string Status => BackendAvailable
            ? (string.IsNullOrEmpty(lastError) ? "FMOD 이벤트 재생 준비" : lastError)
            : "FMOD 패키지 연결 필요 — 볼륨 설정은 저장됩니다.";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic() { Instance = null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap() { EnsureExists(); }

        public static SoundManager EnsureExists()
        {
            if (Instance != null) return Instance;
            var existing = FindFirstObjectByType<SoundManager>();
            if (existing != null) { existing.Initialize(); return existing; }
            return new GameObject("SoundManager").AddComponent<SoundManager>();
        }

        private void Awake() { Initialize(); }

        private void Initialize()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            if (Instance == this) return;
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            for (var i = 0; i < 3; i++)
            {
                var saved = PlayerPrefs.GetFloat(VolumeKey((AudioCategory)i), volumes[i]);
                volumes[i] = float.IsNaN(saved) || float.IsInfinity(saved) ? 1f : Mathf.Clamp01(saved);
                muted[i] = PlayerPrefs.GetInt(MuteKey((AudioCategory)i), 0) != 0;
            }
        }

        private static string VolumeKey(AudioCategory category) => "Hongik.Audio." + category + ".Volume";
        private static string MuteKey(AudioCategory category) => "Hongik.Audio." + category + ".Muted";
        private static int Index(AudioCategory category)
        {
            var index = (int)category;
            if (index < 0 || index >= 3) throw new ArgumentOutOfRangeException(nameof(category));
            return index;
        }

        public float GetVolume(AudioCategory category) => volumes[Index(category)];
        public bool GetMuted(AudioCategory category) => muted[Index(category)];
        public float GetEffectiveVolume(AudioCategory category) => GetMuted(category) ? 0f : GetVolume(category);

        public void SetVolume(AudioCategory category, float volume)
        {
            if (float.IsNaN(volume) || float.IsInfinity(volume)) return;
            var index = Index(category);
            volume = Mathf.Clamp01(volume);
            if (Mathf.Approximately(volumes[index], volume)) return;
            volumes[index] = volume;
            PlayerPrefs.SetFloat(VolumeKey(category), volume);
            Changed();
        }

        public void SetMuted(AudioCategory category, bool value)
        {
            var index = Index(category);
            if (muted[index] == value) return;
            muted[index] = value;
            PlayerPrefs.SetInt(MuteKey(category), value ? 1 : 0);
            Changed();
        }

        private void Changed()
        {
            dirty = true;
#if HONGIK_FMOD
            if (bgm.isValid()) bgm.setVolume(GetEffectiveVolume(AudioCategory.BGM));
            foreach (var sound in playing)
                if (sound.Instance.isValid()) sound.Instance.setVolume(GetEffectiveVolume(sound.Category));
#endif
            SettingsChanged?.Invoke();
        }

        public void SaveSettings()
        {
            if (!dirty) return;
            PlayerPrefs.Save();
            dirty = false;
        }

        public bool PlayBgm(string eventPath)
        {
            if (string.IsNullOrWhiteSpace(eventPath)) return false;
#if HONGIK_FMOD
            if (bgm.isValid() && bgmPath == eventPath &&
                bgm.getPlaybackState(out var currentState) == FMOD.RESULT.OK &&
                currentState != FMOD.Studio.PLAYBACK_STATE.STOPPED) return true;
            try
            {
                var next = FMODUnity.RuntimeManager.CreateInstance(eventPath);
                if (!next.isValid()) return Failed("유효하지 않은 FMOD 이벤트: " + eventPath);
                next.setVolume(GetEffectiveVolume(AudioCategory.BGM));
                var result = next.start();
                if (result != FMOD.RESULT.OK) { next.release(); return Failed(result.ToString()); }
                StopBgm();
                bgm = next;
                bgmPath = eventPath;
                lastError = string.Empty;
                return true;
            }
            catch (Exception exception) { return Failed(exception.Message); }
#else
            return MissingBackend();
#endif
        }

        public void StopBgm()
        {
#if HONGIK_FMOD
            if (!bgm.isValid()) return;
            bgm.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            bgm.release();
            bgm.clearHandle();
            bgmPath = null;
#endif
        }

        public bool PlayOneShot(string eventPath, AudioCategory category = AudioCategory.SFX)
        {
            Index(category);
            if (category == AudioCategory.BGM) return PlayBgm(eventPath);
            if (string.IsNullOrWhiteSpace(eventPath)) return false;
#if HONGIK_FMOD
            try
            {
                var instance = FMODUnity.RuntimeManager.CreateInstance(eventPath);
                if (!instance.isValid()) return Failed("유효하지 않은 FMOD 이벤트: " + eventPath);
                instance.setVolume(GetEffectiveVolume(category));
                var result = instance.start();
                if (result != FMOD.RESULT.OK) { instance.release(); return Failed(result.ToString()); }
                playing.Add(new PlayingEvent { Instance = instance, Category = category });
                lastError = string.Empty;
                return true;
            }
            catch (Exception exception) { return Failed(exception.Message); }
#else
            return MissingBackend();
#endif
        }

        private void Update()
        {
#if HONGIK_FMOD
            for (var i = playing.Count - 1; i >= 0; i--)
            {
                var instance = playing[i].Instance;
                if (!instance.isValid()) { playing.RemoveAt(i); continue; }
                var result = instance.getPlaybackState(out var state);
                if (result != FMOD.RESULT.OK || state == FMOD.Studio.PLAYBACK_STATE.STOPPED)
                { instance.release(); playing.RemoveAt(i); }
            }
#endif
        }

        private bool Failed(string message)
        {
            if (lastError != message) Debug.LogWarning("[SoundManager] " + message, this);
            lastError = message;
            return false;
        }

        private bool MissingBackend()
        {
            if (!warnedUnavailable)
            {
                Debug.LogWarning("[SoundManager] FMOD integration is not installed/enabled. No audio was played.", this);
                warnedUnavailable = true;
            }
            return false;
        }

        private void OnApplicationPause(bool paused) { if (paused) SaveSettings(); }
        private void OnApplicationQuit() { SaveSettings(); }
        private void OnDestroy()
        {
            if (Instance != this) return;
            SaveSettings();
            StopBgm();
#if HONGIK_FMOD
            foreach (var sound in playing)
            {
                if (!sound.Instance.isValid()) continue;
                sound.Instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                sound.Instance.release();
            }
            playing.Clear();
#endif
            Instance = null;
        }
    }
}
