using TMPro;
using System;
using HongikIngan.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace HongikIngan
{
    public sealed class BattleAudioPanel : MonoBehaviour
    {
        public GameObject modal;
        public Button openButton, closeButton;
        public Slider bgmVolume, sfxVolume, voiceVolume;
        public Toggle bgmMute, sfxMute, voiceMute;
        public TMP_Text statusLabel;
        public bool IsOpen => modal != null && modal.activeSelf;
        public event Action VisibilityChanged;
        void OnEnable()
        {
            openButton.onClick.AddListener(Open);
            closeButton.onClick.AddListener(Close);
            bgmVolume.onValueChanged.AddListener(SetBgm);
            sfxVolume.onValueChanged.AddListener(SetSfx);
            voiceVolume.onValueChanged.AddListener(SetVoice);
            bgmMute.onValueChanged.AddListener(MuteBgm);
            sfxMute.onValueChanged.AddListener(MuteSfx);
            voiceMute.onValueChanged.AddListener(MuteVoice);
        }
        void OnDisable()
        {
            openButton.onClick.RemoveListener(Open);
            closeButton.onClick.RemoveListener(Close);
            bgmVolume.onValueChanged.RemoveListener(SetBgm);
            sfxVolume.onValueChanged.RemoveListener(SetSfx);
            voiceVolume.onValueChanged.RemoveListener(SetVoice);
            bgmMute.onValueChanged.RemoveListener(MuteBgm);
            sfxMute.onValueChanged.RemoveListener(MuteSfx);
            voiceMute.onValueChanged.RemoveListener(MuteVoice);
            if (SoundManager.Instance != null && IsOpen) SoundManager.Instance.SaveSettings();
        }
        public void Open()
        {
            var sound = SoundManager.EnsureExists();
            bgmVolume.SetValueWithoutNotify(sound.GetVolume(AudioCategory.BGM));
            sfxVolume.SetValueWithoutNotify(sound.GetVolume(AudioCategory.SFX));
            voiceVolume.SetValueWithoutNotify(sound.GetVolume(AudioCategory.Voice));
            bgmMute.SetIsOnWithoutNotify(sound.GetMuted(AudioCategory.BGM));
            sfxMute.SetIsOnWithoutNotify(sound.GetMuted(AudioCategory.SFX));
            voiceMute.SetIsOnWithoutNotify(sound.GetMuted(AudioCategory.Voice));
            statusLabel.text = sound.Status;
            modal.SetActive(true);
            VisibilityChanged?.Invoke();
        }
        public void Close()
        {
            SoundManager.EnsureExists().SaveSettings();
            modal.SetActive(false);
            VisibilityChanged?.Invoke();
        }
        void SetBgm(float v) => SoundManager.EnsureExists().SetVolume(AudioCategory.BGM, v);
        void SetSfx(float v) => SoundManager.EnsureExists().SetVolume(AudioCategory.SFX, v);
        void SetVoice(float v) => SoundManager.EnsureExists().SetVolume(AudioCategory.Voice, v);
        void MuteBgm(bool v) => SoundManager.EnsureExists().SetMuted(AudioCategory.BGM, v);
        void MuteSfx(bool v) => SoundManager.EnsureExists().SetMuted(AudioCategory.SFX, v);
        void MuteVoice(bool v) => SoundManager.EnsureExists().SetMuted(AudioCategory.Voice, v);
    }
}
