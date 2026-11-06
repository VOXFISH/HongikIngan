using TMPro;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HongikIngan
{
    /// <summary>Updates content only; authored RectTransforms are never repositioned.</summary>
    public sealed class BattleHud : MonoBehaviour
    {
        public TMP_Text summaryLabel, turnLabel, damageLabel, bossHpLabel, statusLabel, logLabel, actionLabel;
        public Button actionButton;
        public Slider bossHpBar;
        public TMP_Text synergyLabel, bossDebuffsLabel;
        public static string DisplayText(string text) => (text ?? "").Replace("×", "x").Replace("→", "=>");
        public static string KeywordText(IEnumerable<KeywordState> keywords) => string.Join("\n", keywords
            .Where(k => k.StackCount > 0).Select(k => k.Definition.Name + (k.Definition.Stackable ? " " + k.StackCount.ToString("0") : "")));
        public void RenderTotals(float damage, float hp, float maxHp)
        {
            damageLabel.text = BattleScreen.FormatDamage(damage);
            bossHpLabel.text = $"HP {BattleScreen.FormatDamage(hp)} / {BattleScreen.FormatDamage(maxHp)}";
            bossHpBar.SetValueWithoutNotify(maxHp <= 0 ? 0 : Mathf.Clamp01(hp / maxHp));
        }

        public void Render(PrototypeRun run, string status, bool running, bool modalOpen, bool interrupted)
        {
            summaryLabel.text = $"마법소녀전대  -  STAGE {run.Stage}  -  미라클 에너지 {run.Money}  -  폴라로이드 {run.PhotoCount}  -  분위기 {run.Atmosphere}";
            var battle = run.Battle;
            int displayedTurn = battle == null ? 1 : battle.IsResolving || battle.Ended ? battle.Turn : Mathf.Min(battle.TurnLimit, battle.Turn + 1);
            turnLabel.text = battle == null ? "전투 준비" : $"TURN {displayedTurn} / {battle.TurnLimit}";
            if (synergyLabel != null)
            {
                synergyLabel.text = SynergyText.List(run, true);
            }
            if (bossDebuffsLabel != null) bossDebuffsLabel.text = battle == null ? "" : KeywordText(battle.BossKeywords);
            statusLabel.text = status;
            logLabel.text = battle == null ? "" : DisplayText(string.Join("\n", battle.Log.Skip(Mathf.Max(0, battle.Log.Count - 2))));
            actionLabel.text = interrupted ? "진행 중단 - 다시 시작" : battle == null ? "학교로 돌아가기" :
                battle.CanReturnToSchool ? "보상 수령 후 학교로" : battle.Lost ? "게임 오버 - 다시 시작" : running ? "필살기 준비 중…" : "합동 공격";
            actionButton.interactable = !running && !modalOpen && (battle == null || !battle.IsResolving);
        }
    }
}
