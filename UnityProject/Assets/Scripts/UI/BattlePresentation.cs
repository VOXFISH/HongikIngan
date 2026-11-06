using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        public CanvasGroup combatLogGroup;
        [Min(.01f)] public float stepDuration = 1f;
        [Min(.01f)] public float beamDuration = 1f;
        [Header("Combat text fades")]
        public bool fadeCombatText = true;
        [Min(0)] public float fadeInDuration = .12f;
        [Min(0)] public float fadeOutDuration = .2f;
        [Header("Damage stamp")]
        public bool stampDamageText = true;
        [Min(.01f)] public float damageStampDuration = .5f;
        [Min(1)] public float damageStampScale = 1.65f;
        [Min(0)] public float damageStampShakeDuration = .14f;
        [Min(0)] public float damageStampShakeAmount = 4f;
        readonly Dictionary<RectTransform, Vector3> stampScales = new Dictionary<RectTransform, Vector3>();
        readonly Dictionary<RectTransform, Vector3> stampShakeHomes = new Dictionary<RectTransform, Vector3>();
        // Presentation randomness must not consume the combat random stream.
        readonly System.Random stampRandom = new System.Random();
        bool stampActive, stampLanded;
        float stampTime, stampShakeTime;
        Vector3 stampDirection;
        [Header("Student animation")]
        [Tooltip("Hop height, duration and curve are editable on each BattleStudentSlot or its shared prefab.")]
        public bool animateStudents = true;
        [Header("Orb, laser and impact")]
        public RectTransform thrownOrb, bossFace;
        public CanvasGroup orbGroup;
        public RectTransform[] impactShakeTargets;
        [Min(0)] public float impactShakeDuration = .3f;
        [Min(0)] public float impactShakeAmount = 12f;
        [Header("Orb charge size (radius multiplier)")]
        [Min(.01f)] public float orbMinRadius = .6f;
        [Min(.01f)] public float orbMaxRadius = 1.5f;
        [Min(1)] public float orbSizeMaxDamage = 100000f;
        [Min(.1f)] public float orbBubbleFrequency = 3.5f;
        [Range(.05f, .95f)] public float orbBubbleDamping = .3f;
        Vector3 orbHome, orbHomeScale;
        float orbHomeAlpha, orbRadius, orbRadiusVelocity;
        bool orbCaptured;
        readonly Dictionary<RectTransform, Vector3> shakeHomes = new Dictionary<RectTransform, Vector3>();
        [Header("Boss reaction")]
        public Image bossVisual;
        public Sprite bossKoSprite;
        public Material bossFlashMaterial;
        [Min(0)] public float bossShakeAmount = 7f;
        [Min(0)] public float bossShakeFrequency = 24f;
        [Min(0)] public float bossWhiteHold = .2f;
        Vector3 bossHome;
        Sprite bossAliveSprite;
        Color bossAliveColor;
        Material bossOriginalMaterial, bossRuntimeMaterial;
        bool bossCaptured, bossDefeated, bossDying, bossAlivePreserveAspect;
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
            if (thrownOrb != null && !orbCaptured)
            {
                orbCaptured = true; orbHome = thrownOrb.localPosition; orbHomeScale = thrownOrb.localScale;
                orbHomeAlpha = orbGroup != null ? orbGroup.alpha : 1;
            }
            if (bossVisual != null)
            {
                if (!bossCaptured)
                {
                    bossCaptured = true;
                    bossHome = bossVisual.rectTransform.anchoredPosition3D;
                    bossAliveSprite = bossVisual.sprite;
                    bossAliveColor = bossVisual.color;
                    bossAlivePreserveAspect = bossVisual.preserveAspect;
                    bossOriginalMaterial = bossVisual.material;
                    if (bossFlashMaterial != null) bossRuntimeMaterial = new Material(bossFlashMaterial);
                }
                bossDefeated = bossDying = false;
                bossVisual.sprite = bossAliveSprite;
                bossVisual.preserveAspect = bossAlivePreserveAspect;
                bossVisual.color = bossAliveColor;
                bossVisual.material = bossOriginalMaterial;
                if (run.Battle != null && run.Battle.BossHp <= 0) ApplyBossKo();
            }
            DisplayedDamage = run.Battle?.Damage ?? 0;
            DisplayedHp = run.Battle?.BossHp ?? 0;
            maxHp = run.Battle?.MaxBossHp ?? 1;
            orbRadius = OrbRadiusForDamage(DisplayedDamage);
            orbRadiusVelocity = 0;
            Clear(); RenderTotals();
            SnapshotKeywords(run);
            var sound = SoundManager.EnsureExists();
            if (sound.BackendAvailable) sound.PlayBgm("event:/BGM/Battle");
        }
        // Scale authored timing too, so existing scenes receive the slower damage display.
        public float DurationFor(BattleStep step) => Mathf.Max(.01f,
            step.Kind == "beam" ? beamDuration : stepDuration * 1.5f);
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
                formation.ShowCalculation(step.Actor, BattleHud.StepText(step));
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
            float duration = DurationFor(step);
            if (!firing && stampDamageText && !string.IsNullOrEmpty(step.Operation)) BeginDamageStamp(duration);
            float elapsed = 0;
            while (elapsed < duration)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                DisplayedDamage = Interpolate(fromDamage, targetDamage, t);
                DisplayedHp = Interpolate(fromHp, targetHp, t);
                RenderTotals();
                SetTextAlpha(TextAlpha(elapsed, duration));
                UpdateDamageStamp(elapsed);
                if (!animateStudents) foreach (var slot in formation.slots) slot.CancelHop();
                if (firing)
                {
                    PlaceBeam();
                    if (bossVisual != null && !bossDefeated)
                    {
                        float wave = elapsed * bossShakeFrequency * Mathf.PI * 2;
                        bossVisual.rectTransform.anchoredPosition3D = bossHome + new Vector3(
                            Mathf.Sin(wave), Mathf.Sin(wave * 1.37f) * .4f, 0) * bossShakeAmount;
                    }
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
            DisplayedDamage = targetDamage; DisplayedHp = targetHp; RenderTotals();
            RestoreDamageStamp();
            SetTextAlpha(fadeCombatText ? 0 : 1);
            RestoreBossPosition();
            if (firing)
            {
                IsBeaming = false;
                if (beam != null) beam.SetActive(false);
                foreach (var slot in formation.slots) slot.CancelHop();
                CaptureImpactPositions();
                for (float time = 0; time < impactShakeDuration; time += Time.unscaledDeltaTime)
                {
                    ShakeImpact(time);
                    yield return null;
                }
                RestoreImpactPositions();
            }
            if (bossVisual != null && !bossDefeated && targetHp <= 0)
            {
                bossDefeated = bossDying = true;
                if (beam != null) beam.SetActive(false);
                if (bossRuntimeMaterial != null)
                {
                    bossVisual.material = bossRuntimeMaterial;
                    bossVisual.color = Color.white;
                    bossRuntimeMaterial.SetFloat("_Flash", 1);
                }
                yield return new WaitForSecondsRealtime(bossWhiteHold);
                ApplyBossKo();
            }
        }
        void BeginDamageStamp(float duration)
        {
            stampActive = true; stampLanded = false;
            // Preserve accelerated/short steps while keeping the authored default at 0.5 seconds.
            stampTime = Mathf.Min(Mathf.Max(.01f, damageStampDuration), duration * .75f);
            stampShakeTime = Mathf.Min(Mathf.Max(0, damageStampShakeDuration), duration - stampTime);
            if (hud != null && hud.logLabel != null) CaptureStampText(hud.logLabel);
            if (formation != null)
                foreach (var slot in formation.slots)
                    if (slot.calculationLabel != null && slot.calculationLabel.gameObject.activeInHierarchy)
                        CaptureStampText(slot.calculationLabel);
        }
        void CaptureStampText(TMP_Text text)
        {
            if (!string.IsNullOrEmpty(text.text)) stampScales[text.rectTransform] = text.rectTransform.localScale;
        }
        void UpdateDamageStamp(float elapsed)
        {
            if (!stampActive) return;
            if (!stampDamageText || !isActiveAndEnabled) { RestoreDamageStamp(); return; }
            if (elapsed < stampTime)
            {
                float t = elapsed / stampTime;
                float scale = t < .72f
                    ? Mathf.Lerp(Mathf.Max(1, damageStampScale), .92f, 1 - Mathf.Pow(1 - t / .72f, 3))
                    : Mathf.Lerp(.92f, 1, Mathf.SmoothStep(0, 1, (t - .72f) / .28f));
                foreach (var item in stampScales) if (item.Key != null) item.Key.localScale = item.Value * scale;
                return;
            }
            if (!stampLanded)
            {
                stampLanded = true;
                foreach (var item in stampScales) if (item.Key != null) item.Key.localScale = item.Value;
                var canvas = hud != null ? hud.GetComponentInParent<Canvas>() : null;
                if (canvas != null)
                    foreach (Transform child in canvas.rootCanvas.transform)
                        if (child is RectTransform rect) stampShakeHomes[rect] = rect.localPosition;
                float angle = (float)(stampRandom.NextDouble() * System.Math.PI * 2);
                stampDirection = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
            }
            float age = elapsed - stampTime;
            if (stampShakeTime <= 0 || age >= stampShakeTime) { RestoreDamageStamp(); return; }
            float progress = age / stampShakeTime;
            float impulse = Mathf.Cos(progress * Mathf.PI * 4) * (1 - progress) * (1 - progress);
            var offset = stampDirection * (Mathf.Max(0, damageStampShakeAmount) * impulse);
            foreach (var item in stampShakeHomes) if (item.Key != null) item.Key.localPosition = item.Value + offset;
        }
        void RestoreDamageStamp()
        {
            foreach (var item in stampScales) if (item.Key != null) item.Key.localScale = item.Value;
            foreach (var item in stampShakeHomes) if (item.Key != null) item.Key.localPosition = item.Value;
            stampScales.Clear(); stampShakeHomes.Clear();
            stampActive = stampLanded = false;
        }
        float OrbRadiusForDamage(float damage)
        {
            float minimum = Mathf.Max(.01f, orbMinRadius);
            float maximum = Mathf.Max(minimum, orbMaxRadius);
            float cap = Mathf.Max(1, orbSizeMaxDamage);
            if (float.IsNaN(damage)) damage = 0;
            float fraction = Mathf.Log10(1 + Mathf.Clamp(damage, 0, cap)) / Mathf.Log10(1 + cap);
            return Mathf.Lerp(minimum, maximum, fraction);
        }
        void UpdateOrbCharge()
        {
            if (!orbCaptured || thrownOrb == null) return;
            // Exact damped-spring integration gives a bubbling overshoot without frame-rate instability.
            float goal = OrbRadiusForDamage(DisplayedDamage);
            float omega = Mathf.Max(.1f, orbBubbleFrequency) * Mathf.PI * 2;
            float damping = Mathf.Clamp(orbBubbleDamping, .05f, .95f);
            float frequency = omega * Mathf.Sqrt(1 - damping * damping);
            float dt = Time.unscaledDeltaTime;
            float decay = Mathf.Exp(-damping * omega * dt);
            float cosine = Mathf.Cos(frequency * dt), sine = Mathf.Sin(frequency * dt);
            float displacement = orbRadius - goal;
            float previousVelocity = orbRadiusVelocity;
            orbRadius = goal + decay * (displacement * cosine +
                (previousVelocity + damping * omega * displacement) / frequency * sine);
            orbRadiusVelocity = decay * (previousVelocity * cosine -
                (damping * omega * previousVelocity + omega * omega * displacement) / frequency * sine);
            float minimum = Mathf.Max(.01f, orbMinRadius), maximum = Mathf.Max(minimum, orbMaxRadius);
            float bounded = Mathf.Clamp(orbRadius, minimum, maximum);
            if (bounded != orbRadius) orbRadiusVelocity = 0;
            orbRadius = bounded;
            thrownOrb.localScale = orbHomeScale * orbRadius;
            thrownOrb.localPosition = orbHome;
        }
        void CaptureImpactPositions()
        {
            shakeHomes.Clear();
            if (impactShakeTargets != null)
                foreach (var target in impactShakeTargets)
                    if (target != null) shakeHomes[target] = target.localPosition;
            foreach (var slot in formation.slots)
                if (slot.HasStudent && slot.portrait != null) shakeHomes[slot.portrait.rectTransform] = slot.portrait.rectTransform.localPosition;
        }
        void ShakeImpact(float time)
        {
            float fade = impactShakeDuration <= 0 ? 0 : 1 - Mathf.Clamp01(time / impactShakeDuration);
            var offset = new Vector3(Mathf.Sin(time * 103), Mathf.Sin(time * 137), 0) * (impactShakeAmount * fade);
            foreach (var item in shakeHomes)
                if (item.Key != null) item.Key.localPosition = item.Value + item.Key.parent.InverseTransformVector(transform.TransformVector(offset));
        }
        void RestoreImpactPositions()
        {
            foreach (var item in shakeHomes) if (item.Key != null) item.Key.localPosition = item.Value;
            shakeHomes.Clear();
        }
        void RestoreOrb()
        {
            if (!orbCaptured || thrownOrb == null) return;
            thrownOrb.localPosition = orbHome; thrownOrb.localScale = orbHomeScale * orbRadius;
            if (orbGroup != null) orbGroup.alpha = orbHomeAlpha;
        }
        void RestoreBossPosition()
        {
            if (bossCaptured && bossVisual != null) bossVisual.rectTransform.anchoredPosition3D = bossHome;
        }
        void ApplyBossKo()
        {
            bossDefeated = true; bossDying = false;
            if (bossVisual == null) return;
            if (bossKoSprite != null) bossVisual.sprite = bossKoSprite;
            bossVisual.preserveAspect = true;
            bossVisual.material = bossOriginalMaterial;
            bossVisual.color = bossAliveColor;
            RestoreBossPosition();
        }
        void OnDisable() { RestoreDamageStamp(); RestoreImpactPositions(); RestoreOrb(); RestoreBossPosition(); if (bossDying) ApplyBossKo(); }
        void OnDestroy()
        {
            RestoreDamageStamp();
            if (bossVisual != null && bossVisual.material == bossRuntimeMaterial) bossVisual.material = bossOriginalMaterial;
            if (bossRuntimeMaterial != null) Destroy(bossRuntimeMaterial);
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
            UpdateOrbCharge();
            if (IsBeaming && beam != null) PlaceBeam();
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
            Vector2 start = parent.InverseTransformPoint(thrownOrb != null
                ? thrownOrb.TransformPoint(thrownOrb.rect.center)
                : orbDamage.rectTransform.TransformPoint(orbDamage.rectTransform.rect.center));
            Vector2 end = parent.InverseTransformPoint(bossFace != null ? bossFace.position
                : hud.bossHpLabel.rectTransform.TransformPoint(hud.bossHpLabel.rectTransform.rect.center));
            Vector2 delta = end - start;
            rect.localPosition = start;
            rect.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, delta.magnitude);
        }
        public void Clear()
        {
            RestoreDamageStamp();
            RestoreImpactPositions();
            RestoreOrb();
            RestoreBossPosition();
            if (bossDying) ApplyBossKo();
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
