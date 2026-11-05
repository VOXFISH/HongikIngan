using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HongikIngan
{
    /// <summary>Content-only view; the dialog and all controls are authored in the Battle scene.</summary>
    public sealed class BattleRewardPopup : MonoBehaviour
    {
        public GameObject modal;
        public TMP_Text detailsLabel;
        public Button claimButton;
        public bool IsOpen => modal != null && modal.activeSelf;

        public void Render(PrototypeRun run, bool ready)
        {
            var reward = run.Battle?.Reward;
            bool show = ready && run.Battle != null && run.Battle.CanReturnToSchool && reward != null && !reward.Claimed;
            modal.SetActive(show);
            if (!show) return;
            detailsLabel.text = (run.Battle.Saved ? "내일의 태양이 게임오버를 막았습니다.\n\n" : "전투 종료! 보상을 수령하세요.\n\n") +
                "기본 보상  +" + reward.Base + "\n" +
                "남은 턴 " + reward.RemainingTurns + " x 100  +" + reward.TurnBonus + "\n" +
                "정산 전 에너지 " + reward.EnergyBeforeSettlement + " x 10%  +" + reward.Interest + "\n" +
                "유물 추가 보상  +" + reward.RelicBonus + "\n\n" +
                "획득 배율 적용 후 수령 에너지  " + reward.Total + "\n" +
                "수령하면 전투 종료 시너지를 적용하고 학교로 돌아갑니다.";
            claimButton.interactable = true;
        }
    }
}
