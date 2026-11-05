using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HongikIngan
{
    /// <summary>Updates content only; authored RectTransforms are never repositioned.</summary>
    public sealed class BattleHud : MonoBehaviour
    {
        public Text summaryLabel, turnLabel, damageLabel, bossHpLabel, statusLabel, logLabel, actionLabel;
        public Button actionButton;
        public Slider bossHpBar;

        public void Render(PrototypeRun run, string status, bool running, bool modalOpen, bool interrupted)
        {
            summaryLabel.text = $"마법소녀전대  ·  STAGE {run.Stage}  ·  미라클 에너지 {run.Money}  ·  폴라로이드 {run.PhotoCount}  ·  분위기 {run.Atmosphere}";
            var battle = run.Battle;
            int displayedTurn = battle == null ? 1 : battle.IsResolving || battle.Won || battle.Lost ? battle.Turn : Mathf.Min(3, battle.Turn + 1);
            turnLabel.text = battle == null ? "전투를 시작할 수 없습니다." : $"TURN {displayedTurn}/3  ·  조합을 바꿔 보스를 물리치세요";
            damageLabel.text = "합동 데미지  " + BattleScreen.FormatDamage(battle?.Damage ?? 0);
            bossHpLabel.text = battle == null ? "" : $"HP {BattleScreen.FormatDamage(battle.BossHp)} / {BattleScreen.FormatDamage(battle.MaxBossHp)}";
            bossHpBar.SetValueWithoutNotify(battle == null ? 0 : Mathf.Clamp01(battle.BossHp / battle.MaxBossHp));
            statusLabel.text = status;
            logLabel.text = battle == null ? "" : string.Join("\n", battle.Log.Skip(Mathf.Max(0, battle.Log.Count - 4)));
            actionLabel.text = interrupted ? "진행 중단 · 다시 시작" : battle == null ? "학교로 돌아가기" :
                battle.Won ? "승리! 교실로 복귀" : battle.Lost ? "게임 오버 · 다시 시작" : running ? "필살기 준비 중…" : "합동 공격";
            actionButton.interactable = !running && !modalOpen && (battle == null || !battle.IsResolving);
        }
    }
}
