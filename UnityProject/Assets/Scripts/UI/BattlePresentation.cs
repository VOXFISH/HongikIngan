using System.Collections;
using TMPro;
using HongikIngan.Audio;
using UnityEngine;

namespace HongikIngan
{
    public sealed class BattlePresentation : MonoBehaviour
    {
        public GameObject beam;
        public TMP_Text orbDamage, damagePop, chant, actorLabel;
        [Min(.01f)] public float stepDuration = 1f;
        [Min(.01f)] public float beamDuration = 1f;
        public float DisplayedDamage { get; private set; }
        public float DisplayedHp { get; private set; }
        public bool IsBeaming { get; private set; }
        BattleHud hud;
        BattleFormationView formation;
        bool spent;
        float maxHp;

        public void BeginBattle(PrototypeRun run, BattleHud owner, BattleFormationView students)
        {
            hud = owner; formation = students;
            DisplayedDamage = run.Battle?.Damage ?? 0;
            DisplayedHp = run.Battle?.BossHp ?? 0;
            maxHp = run.Battle?.MaxBossHp ?? 1;
            Clear(); RenderTotals();
            var sound = SoundManager.EnsureExists();
            if (sound.BackendAvailable) sound.PlayBgm("event:/BGM/Battle");
        }
        public IEnumerator Play(BattleStep step, PrototypeRun run)
        {
            Clear();
            if (step.Kind == "turn") spent = false;
            float fromDamage = DisplayedDamage, fromHp = DisplayedHp;
            bool firing = step.Kind == "beam";
            float targetDamage = firing || spent ? 0 : step.Damage;
            float targetHp = firing ? Mathf.Max(0, run.Battle.BossHp - step.Damage) : run.Battle.BossHp;
            if (step.Kind == "turn") fromDamage = 0;
            if (firing) { IsBeaming = true; beam.SetActive(true); spent = true; }
            if (chant != null) { chant.gameObject.SetActive(step.Kind == "chant"); chant.text = step.Message; }
            if (step.Actor != null)
                formation.ShowCalculation(step.Actor, BattleHud.DisplayText(step.Message) + (step.Damage != fromDamage ?
                    "\n" + PrototypeCatalog.FormatDamage(fromDamage) + " => " + PrototypeCatalog.FormatDamage(step.Damage) : ""));
            var sound = SoundManager.EnsureExists();
            if (sound.BackendAvailable)
            {
                if (firing) sound.PlayOneShot("event:/SFX/Beam", AudioCategory.SFX);
                if (step.Kind == "chant") sound.PlayOneShot("event:/Voice/Chant", AudioCategory.Voice);
            }
            float duration = Mathf.Max(.01f, firing ? beamDuration : stepDuration);
            float elapsed = 0;
            while (elapsed < duration)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                DisplayedDamage = Interpolate(fromDamage, targetDamage, t);
                DisplayedHp = Interpolate(fromHp, targetHp, t);
                RenderTotals();
                if (firing) PlaceBeam();
                elapsed += Time.deltaTime;
                yield return null;
            }
            DisplayedDamage = targetDamage; DisplayedHp = targetHp; RenderTotals();
        }
        static float Interpolate(float from, float to, float t)
        {
            if (t >= 1) return to;
            if (float.IsInfinity(from) || float.IsInfinity(to)) return from;
            return Mathf.Lerp(from, to, t);
        }
        void RenderTotals() { if (hud != null) hud.RenderTotals(DisplayedDamage, DisplayedHp, maxHp); }
        void PlaceBeam()
        {
            var rect = (RectTransform)beam.transform;
            var parent = (RectTransform)rect.parent;
            // Use the numeric text boxes, not fixed scene coordinates, as the endpoints.
            Vector2 start = parent.InverseTransformPoint(orbDamage.rectTransform.TransformPoint(orbDamage.rectTransform.rect.center));
            Vector2 end = parent.InverseTransformPoint(hud.bossHpLabel.rectTransform.TransformPoint(hud.bossHpLabel.rectTransform.rect.center));
            Vector2 delta = end - start;
            rect.localPosition = start;
            rect.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, delta.magnitude);
        }
        public void Clear()
        {
            IsBeaming = false;
            if (beam != null) beam.SetActive(false);
            if (damagePop != null) damagePop.gameObject.SetActive(false);
            if (chant != null) chant.gameObject.SetActive(false);
            if (actorLabel != null) actorLabel.gameObject.SetActive(false);
            if (formation != null) formation.ShowCalculation(null, "");
        }
    }
}
