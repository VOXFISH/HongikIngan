using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using HongikIngan.Audio;
using UnityEngine;

namespace HongikIngan
{
    public sealed class BattlePresentation : MonoBehaviour
    {
        public GameObject beam;
        public TMP_Text orbDamage, damagePop, chant, actorLabel;
        public CanvasGroup combatLogGroup;
        [Min(.01f)] public float stepDuration = 1f;
        [Min(.01f)] public float beamDuration = 1f;
        [Header("Combat text fades")]
        public bool fadeCombatText = true;
        [Min(0)] public float fadeInDuration = .12f;
        [Min(0)] public float fadeOutDuration = .2f;
        [Header("Student animation")]
        [Tooltip("Hop height, duration and curve are editable on each BattleStudentSlot or its shared prefab.")]
        public bool animateStudents = true;
        readonly Dictionary<StudentState,string> keywordSnapshots = new Dictionary<StudentState,string>();
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
            SnapshotKeywords(run);
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
                formation.ShowCalculation(step.Actor, (step.Damage != fromDamage ?
                    "<size=115%>" + PrototypeCatalog.FormatDamage(fromDamage) + " => " + PrototypeCatalog.FormatDamage(step.Damage) + "</size>\n" : "") + BattleHud.DisplayText(step.Message));
            var participants = new HashSet<StudentState>();
            if (step.Actor != null) participants.Add(step.Actor);
            if (step.Target != null) participants.Add(step.Target);
            foreach (var student in run.Students)
                if (!keywordSnapshots.TryGetValue(student, out var previous) || previous != KeywordSignature(student)) participants.Add(student);
            SnapshotKeywords(run);
            if (animateStudents)
                foreach (var slot in formation.slots)
                    if (slot.HasStudent && participants.Contains(slot.Student)) slot.PlayEffectHop();
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
                SetTextAlpha(TextAlpha(elapsed, duration));
                if (!animateStudents) foreach (var slot in formation.slots) slot.CancelHop();
                if (firing) PlaceBeam();
                elapsed += Time.deltaTime;
                yield return null;
            }
            DisplayedDamage = targetDamage; DisplayedHp = targetHp; RenderTotals();
            SetTextAlpha(fadeCombatText ? 0 : 1);
        }
        static string KeywordSignature(StudentState student) => string.Join("|", student.Keywords.Select(k => k.Definition.Id + ":" + k.StackCount + ":" + string.Join(",", k.Values)));
        void SnapshotKeywords(PrototypeRun run)
        {
            keywordSnapshots.Clear();
            foreach (var student in run.Students) keywordSnapshots[student] = KeywordSignature(student);
        }
        float TextAlpha(float elapsed, float duration)
        {
            if (!fadeCombatText) return 1;
            // Preserve a readable hold even when an editor duration exceeds the step.
            float fadeIn = Mathf.Min(Mathf.Max(0,fadeInDuration),duration*.4f);
            float fadeOut = Mathf.Min(Mathf.Max(0,fadeOutDuration),duration*.4f);
            float enter = fadeIn <= 0 ? 1 : Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/fadeIn));
            float leave = fadeOut <= 0 ? 1 : Mathf.SmoothStep(0,1,Mathf.Clamp01((duration-elapsed)/fadeOut));
            return Mathf.Min(enter,leave);
        }
        void SetTextAlpha(float alpha)
        {
            if (hud != null) hud.logLabel.alpha = combatLogGroup != null ? 1 : alpha;
            if (combatLogGroup != null) combatLogGroup.alpha = hud == null || string.IsNullOrEmpty(hud.logLabel.text) ? 0 : alpha;
            if (chant != null) chant.alpha = alpha;
            if (formation != null) foreach (var slot in formation.slots) slot.SetCalculationAlpha(alpha);
        }
        void LateUpdate()
        {
            // HUD binding can clear the log after BeginBattle; keep an empty backing invisible too.
            if (combatLogGroup != null && hud != null && string.IsNullOrWhiteSpace(hud.logLabel.text)) combatLogGroup.alpha = 0;
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
            if (formation != null) foreach (var slot in formation.slots) slot.CancelHop();
            SetTextAlpha(1);
            IsBeaming = false;
            if (beam != null) beam.SetActive(false);
            if (damagePop != null) damagePop.gameObject.SetActive(false);
            if (chant != null) chant.gameObject.SetActive(false);
            if (actorLabel != null) actorLabel.gameObject.SetActive(false);
            if (formation != null) formation.ShowCalculation(null, "");
        }
    }
}
