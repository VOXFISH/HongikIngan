using System;
using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace HongikIngan
{
    // Animation previews settlement; the existing model pays it once on confirmation.
    public sealed class BattleRewardPopup : MonoBehaviour
    {
        [Serializable] public sealed class ResultLine
        {
            public GameObject root;
            public TMP_Text label, dots, value;
            public Image energyIcon;
        }
        public GameObject modal;
        public TMP_Text detailsLabel, titleLabel, balanceLabel, returnLabel;
        public Button claimButton;
        public Image cover, logo;
        public Sprite rewardSprite, gameOverSprite;
        public CanvasGroup content;
        public ResultLine[] lines;
        [Min(.01f)] public float sweepDuration = .45f, logoFadeDuration = .5f;
        [Min(.001f)] public float characterInterval = .018f;
        [Min(0)] public float numberDelay = .2f, transferWait = 1f;
        [Min(.01f)] public float transferDuration = .55f;
        public bool IsOpen => modal != null && modal.activeSelf;
        public bool IsReady { get; private set; }
        public bool IsLeaving { get; private set; }
        public bool IsGameOver { get; private set; }
        public long DisplayedEnergy { get; private set; }
        public bool CanContinue => IsReady && !IsLeaving && !waitingRelease && Time.frameCount > completedFrame;
        BattleState shownBattle;
        Coroutine sequence;
        string[] captions, numbers;
        int[] payouts;
        long initialEnergy;
        int completedFrame;
        bool waitingRelease;
        float readyTime, travel;

        public void Render(PrototypeRun state, bool ready)
        {
            if (!ready || state.Battle == null || !state.Battle.Ended || shownBattle == state.Battle) return;
            if (!state.Battle.Lost && (state.Battle.Reward == null || state.Battle.Reward.Claimed)) return;
            shownBattle = state.Battle; IsGameOver = shownBattle.Lost;
            IsReady = IsLeaving = waitingRelease = false;
            initialEnergy = state.Money; DisplayedEnergy = initialEnergy;
            modal.SetActive(true); content.alpha = 0;
            claimButton.gameObject.SetActive(!IsGameOver); claimButton.interactable = false;
            returnLabel.gameObject.SetActive(false);
            cover.sprite = IsGameOver ? gameOverSprite : rewardSprite;
            SetLogoAlpha(1); FitCover(); SetCoverX(-travel);
            titleLabel.text = IsGameOver ? "Game Over" : "스테이지 " + state.Stage;
            if (IsGameOver)
            {
                captions = new[] { "Stage", "Score", "소지 에너지" };
                numbers = new[] { state.Stage.ToString(), PrototypeCatalog.FormatDamage(state.CumulativeDamage), Money(initialEnergy) };
                payouts = new int[3]; detailsLabel.text = "";
            }
            else
            {
                var reward = shownBattle.Reward;
                captions = new[] { "보스 격퇴 보상", "남아 있는 턴  (" + reward.RemainingTurns + " x 100)", "이자 (10%)" };
                payouts = SettlementParts(reward);
                numbers = Array.ConvertAll(payouts, amount => "+ " + Money(amount));
                detailsLabel.text = shownBattle.Saved ? "내일의 태양이 당신을 지켜냈습니다." : "BATTLE REWARDS";
                if (reward.Total != reward.Base + reward.TurnBonus + reward.Interest)
                    detailsLabel.text += "\n운세와 에너지 획득 배율이 반영된 보상입니다.";
                if (shownBattle.EndDisciplineCount >= 4) detailsLabel.text += "\n선도부의 전투 종료 보너스는 확인 시 추가 지급됩니다.";
            }
            balanceLabel.text = Money(DisplayedEnergy);
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i].root.SetActive(false);
                lines[i].label.text = captions[i]; lines[i].label.maxVisibleCharacters = 0;
                // An invisible ascender keeps punctuation-only TMP bounds stable through reveal/canvas changes.
                lines[i].dots.text = "<alpha=#00>M<alpha=#FF><pos=0>. . . . . . . . . . . . . . . ."; lines[i].dots.maxVisibleCharacters = 0;
                lines[i].value.text = numbers[i]; lines[i].value.maxVisibleCharacters = 0;
                lines[i].energyIcon.gameObject.SetActive(!IsGameOver || i == 2);
            }
            sequence = StartCoroutine(Reveal());
        }
        // Cumulative allocation preserves the exact existing rounded settlement total.
        public static int[] SettlementParts(BattleReward reward)
        {
            long raw = (long)reward.Base + reward.TurnBonus + reward.Interest;
            int first = raw == 0 ? 0 : (int)((long)reward.Total * reward.Base / raw);
            int second = raw == 0 ? 0 : (int)((long)reward.Total * (reward.Base + (long)reward.TurnBonus) / raw);
            return new[] { first, second - first, reward.Total - second };
        }
        IEnumerator Reveal()
        {
            yield return Slide(-travel, 0);
            yield return FadeLogo(1, 0, logoFadeDuration);
            content.alpha = 1;
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i]; line.root.SetActive(true);
                yield return Type(line.label); yield return Type(line.dots);
                yield return new WaitForSecondsRealtime(numberDelay);
                line.value.maxVisibleCharacters = 99999;
                if (!IsGameOver)
                {
                    yield return new WaitForSecondsRealtime(transferWait);
                    long from = DisplayedEnergy;
                    for (float time = 0; time < transferDuration; time += Time.unscaledDeltaTime)
                    {
                        int moved = (int)Math.Floor(payouts[i] * (double)Mathf.SmoothStep(0, 1, time / transferDuration));
                        DisplayedEnergy = from + moved; balanceLabel.text = Money(DisplayedEnergy);
                        line.value.text = "+ " + Money(payouts[i] - moved); yield return null;
                    }
                    DisplayedEnergy = from + payouts[i]; balanceLabel.text = Money(DisplayedEnergy); line.value.text = "+ 0";
                }
            }
            Complete(); sequence = null;
        }
        IEnumerator Type(TMP_Text text)
        {
            text.ForceMeshUpdate(); int count = text.textInfo.characterCount;
            for (float time = 0; text.maxVisibleCharacters < count; time += Time.unscaledDeltaTime)
            { text.maxVisibleCharacters = Mathf.Min(count, (int)(time / Mathf.Max(.001f, characterInterval)) + 1); yield return null; }
            text.maxVisibleCharacters = 99999; text.ForceMeshUpdate();
        }
        public void Skip()
        {
            if (!IsOpen || IsReady || IsLeaving) return;
            if (sequence != null) StopCoroutine(sequence);
            sequence = null; waitingRelease = Pointer.current != null && Pointer.current.press.isPressed;
            Complete();
        }
        void Complete()
        {
            SetCoverX(0); SetLogoAlpha(0); content.alpha = 1;
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i].root.SetActive(true);
                lines[i].label.maxVisibleCharacters = lines[i].dots.maxVisibleCharacters = lines[i].value.maxVisibleCharacters = 99999;
                lines[i].value.text = IsGameOver ? numbers[i] : "+ 0";
                lines[i].label.ForceMeshUpdate(); lines[i].dots.ForceMeshUpdate(); lines[i].value.ForceMeshUpdate();
            }
            DisplayedEnergy = initialEnergy + (IsGameOver ? 0 : shownBattle.Reward.Total);
            balanceLabel.text = Money(DisplayedEnergy);
            IsReady = true; completedFrame = Time.frameCount; readyTime = Time.unscaledTime;
            claimButton.interactable = !IsGameOver;
            returnLabel.gameObject.SetActive(IsGameOver); returnLabel.alpha = 1;
        }
        void Update()
        {
            if (!IsOpen || IsLeaving) return;
            if (waitingRelease && (Pointer.current == null || !Pointer.current.press.isPressed))
            { waitingRelease = false; completedFrame = Time.frameCount; }
            bool click = Pointer.current != null && Pointer.current.press.wasPressedThisFrame;
            bool key = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
            if (click && !IsReady) { Skip(); return; }
            if (IsGameOver && CanContinue && (click || key)) claimButton.onClick.Invoke();
            if (IsGameOver && IsReady)
                returnLabel.alpha = .35f + .65f * (.5f + .5f * Mathf.Cos((Time.unscaledTime - readyTime) * Mathf.PI * 2 / 3));
        }
        public void ReturnTo(Action changeScene)
        {
            if (!CanContinue) return;
            IsLeaving = true; claimButton.interactable = false;
            DontDestroyOnLoad(gameObject);
            StartCoroutine(Leave(changeScene));
        }
        IEnumerator Leave(Action changeScene)
        {
            content.alpha = 0; returnLabel.gameObject.SetActive(false);
            yield return FadeLogo(0, 1, .2f);
            changeScene(); yield return null;
            yield return new WaitForSecondsRealtime(.2f);
            yield return Slide(0, travel, true);
            Destroy(gameObject);
        }
        void FitCover()
        {
            Canvas.ForceUpdateCanvases(); var rect = (RectTransform)modal.transform;
            float width = Mathf.Max(rect.rect.width * 1.85f, rect.rect.height * 1.85f);
            cover.rectTransform.sizeDelta = new Vector2(width, rect.rect.height * 1.1f);
            travel = (rect.rect.width + width) * .5f + 8;
        }
        IEnumerator Slide(float from, float to, bool fadeLogo = false)
        {
            for (float time = 0; time < sweepDuration; time += Time.unscaledDeltaTime)
            {
                float t = Mathf.SmoothStep(0, 1, time / Mathf.Max(.01f, sweepDuration));
                SetCoverX(Mathf.Lerp(from, to, t)); if (fadeLogo) SetLogoAlpha(1 - t); yield return null;
            }
            SetCoverX(to); if (fadeLogo) SetLogoAlpha(0);
        }
        IEnumerator FadeLogo(float from, float to, float duration)
        {
            for (float time = 0; time < duration; time += Time.unscaledDeltaTime)
            { SetLogoAlpha(Mathf.Lerp(from, to, time / Mathf.Max(.01f, duration))); yield return null; }
            SetLogoAlpha(to);
        }
        void SetCoverX(float x) => cover.rectTransform.anchoredPosition = new Vector2(x, 0);
        void SetLogoAlpha(float alpha) { var color = logo.color; color.a = alpha; logo.color = color; }
        static string Money(long value) => value.ToString("N0", CultureInfo.InvariantCulture).Replace(',', ' ');
    }
}
