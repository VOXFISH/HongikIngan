using HongikIngan.Audio;
using UnityEngine;

namespace HongikIngan
{
    public sealed class BattlePresentation : MonoBehaviour
    {
        public GameObject beam;
        [Min(.01f)] public float stepDuration = .35f;
        public void BeginBattle()
        {
            Clear();
            var sound = SoundManager.EnsureExists();
            if (sound.BackendAvailable) sound.PlayBgm("event:/BGM/Battle");
        }
        public void Show(BattleStep step)
        {
            beam.SetActive(step.Kind == "beam");
            var sound = SoundManager.EnsureExists();
            if (!sound.BackendAvailable) return;
            if (step.Kind == "beam") sound.PlayOneShot("event:/SFX/Beam", AudioCategory.SFX);
            if (step.Kind == "chant") sound.PlayOneShot("event:/Voice/Chant", AudioCategory.Voice);
        }
        public void Clear() { if (beam != null) beam.SetActive(false); }
    }
}
