using TMPro;
using HongikIngan.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace HongikIngan
{
    public sealed class BattlePresentation : MonoBehaviour
    {
        public GameObject beam;
        public TMP_Text orbDamage, damagePop, chant, actorLabel;
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
            if (orbDamage != null) orbDamage.text = PrototypeCatalog.FormatDamage(step.Damage);
            if (damagePop != null) { damagePop.gameObject.SetActive(step.Kind == "beam"); damagePop.text = PrototypeCatalog.FormatDamage(step.Damage); }
            if (chant != null) { chant.gameObject.SetActive(step.Kind == "chant"); chant.text = step.Message; }
            if (actorLabel != null) actorLabel.text = step.Actor == null ? "" : step.Actor.Definition.Name + "의 액션";
            var sound = SoundManager.EnsureExists();
            if (!sound.BackendAvailable) return;
            if (step.Kind == "beam") sound.PlayOneShot("event:/SFX/Beam", AudioCategory.SFX);
            if (step.Kind == "chant") sound.PlayOneShot("event:/Voice/Chant", AudioCategory.Voice);
        }
        public void Clear()
        {
            if (beam != null) beam.SetActive(false);
            if (damagePop != null) damagePop.gameObject.SetActive(false);
            if (chant != null) chant.gameObject.SetActive(false);
        }
    }
}
