using System;
using Tessera.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tessera.UI
{
    /// <summary>일반 수배지 승리 후 Cash Out / Chain Rush / Boss 선택을 표시한다.</summary>
    public class StageRewardDecisionView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private TMP_Text rewardText;
        [SerializeField] private TMP_Text stageStateText;
        [SerializeField] private TMP_Text cashOutButtonText;
        [SerializeField] private TMP_Text keepFightingButtonText;
        [SerializeField] private Button cashOutButton;
        [SerializeField] private Button keepFightingButton;

        /// <summary>CashOut 버튼 클릭 이벤트.</summary>
        public event Action CashOutRequested;

        /// <summary>Keep Fighting 버튼 클릭 이벤트.</summary>
        public event Action KeepFightingRequested;

        /// <summary>버튼 클릭 이벤트를 연결한다.</summary>
        private void OnEnable()
        {
            if (cashOutButton != null)
                cashOutButton.onClick.AddListener(HandleCashOutClicked);

            if (keepFightingButton != null)
                keepFightingButton.onClick.AddListener(HandleKeepFightingClicked);
        }

        /// <summary>버튼 클릭 이벤트를 해제한다.</summary>
        private void OnDisable()
        {
            if (cashOutButton != null)
                cashOutButton.onClick.RemoveListener(HandleCashOutClicked);

            if (keepFightingButton != null)
                keepFightingButton.onClick.RemoveListener(HandleKeepFightingClicked);
        }

        /// <summary>보상 선택 화면을 표시한다.</summary>
        public void Show(StageBountyBoardState boardState, string message)
        {
            SetVisible(true);

            if (titleText != null)
                titleText.text = "Bounty Cleared";

            if (messageText != null)
                messageText.text = message ?? string.Empty;

            if (rewardText != null)
                rewardText.text = BuildRewardText(boardState);

            if (stageStateText != null)
                stageStateText.text = BuildStageStateText(boardState);

            if (cashOutButtonText != null)
                cashOutButtonText.text = "Cash Out";

            if (keepFightingButtonText != null)
                keepFightingButtonText.text = "Keep Fighting";

            if (cashOutButton != null)
                cashOutButton.interactable = boardState != null && boardState.PendingMoneyReward > 0;

            if (keepFightingButton != null)
                keepFightingButton.interactable = boardState != null && boardState.HasAvailableNormalBounty;
        }

        /// <summary>View 표시 상태를 변경한다.</summary>
        public void SetVisible(bool visible)
        {
            if (root != null)
                root.SetActive(visible);
            else
                gameObject.SetActive(visible);
        }

        /// <summary>보상 상세 텍스트를 생성한다.</summary>
        private static string BuildRewardText(StageBountyBoardState boardState)
        {
            if (boardState == null)
                return "Pending Money: 0";

            return
                $"Last Reward Money: {boardState.LastCompletedRewardMoney}\n" +
                $"  Base: {boardState.LastBaseRewardMoney}\n" +
                $"  Chain Bonus: {boardState.LastChainBonusMoney}\n" +
                $"  BountyRank Bonus: {boardState.LastBountyRankBonusMoney}\n" +
                $"  StageThreat Bonus: {boardState.LastStageThreatBonusMoney}\n" +
                $"  Remaining Attempt Bonus: {boardState.LastRemainingAttemptBonusMoney}\n" +
                $"Pending Money: {boardState.PendingMoneyReward}";
        }

        /// <summary>Stage 상태 텍스트를 생성한다.</summary>
        private static string BuildStageStateText(StageBountyBoardState boardState)
        {
            if (boardState == null)
                return "Chain 0 / StageThreat 0";

            string enragedText = boardState.IsEnraged ? " / Enraged" : string.Empty;
            string retreatRecoveryText = boardState.IsRetreatRecoveryActive ? " / Retreat Recovery" : string.Empty;

            return $"Chain {boardState.ChainCount} / StageThreat {boardState.StageThreatLevel}{enragedText}{retreatRecoveryText}";
        }

        /// <summary>CashOut 버튼 클릭을 외부 이벤트로 전달한다.</summary>
        private void HandleCashOutClicked()
        {
            CashOutRequested?.Invoke();
        }

        /// <summary>Keep Fighting 버튼 클릭을 외부 이벤트로 전달한다.</summary>
        private void HandleKeepFightingClicked()
        {
            KeepFightingRequested?.Invoke();
        }
    }
}
