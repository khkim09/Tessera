using System;
using Tessera.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tessera.UI
{
    /// <summary>Round 패배 후 Retry / Retreat / Abandon 선택지를 표시하는 View다.</summary>
    public class RoundFailureDecisionView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private TMP_Text summaryText;
        [SerializeField] private TMP_Text retryButtonText;
        [SerializeField] private TMP_Text retreatButtonText;
        [SerializeField] private TMP_Text abandonButtonText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button retreatButton;
        [SerializeField] private Button abandonButton;

        public event Action RetryRequested;
        public event Action RetreatRequested;
        public event Action AbandonRequested;

        private void OnEnable()
        {
            if (retryButton != null)
                retryButton.onClick.AddListener(HandleRetryClicked);

            if (retreatButton != null)
                retreatButton.onClick.AddListener(HandleRetreatClicked);

            if (abandonButton != null)
                abandonButton.onClick.AddListener(HandleAbandonClicked);
        }

        private void OnDisable()
        {
            if (retryButton != null)
                retryButton.onClick.RemoveListener(HandleRetryClicked);

            if (retreatButton != null)
                retreatButton.onClick.RemoveListener(HandleRetreatClicked);

            if (abandonButton != null)
                abandonButton.onClick.RemoveListener(HandleAbandonClicked);
        }

        /// <summary>패배 선택 화면을 표시한다.</summary>
        public void Show(
            TesseraRunSession runSession,
            StageBountyBoardState boardState,
            int retryPartsCost,
            string message)
        {
            SetVisible(true);

            StageBountyNodeState currentNode = boardState != null ? boardState.CurrentNode : null;
            string bountyName = currentNode != null && currentNode.Definition != null
                ? currentNode.Definition.DisplayName
                : "Unknown Bounty";

            int parts = runSession != null ? runSession.Parts : 0;
            int hp = runSession != null ? runSession.PlayerCurrentHp : 0;
            int maxHp = runSession != null ? runSession.PlayerMaxHp : 0;
            int overcharge = runSession != null ? runSession.Overcharge : 0;
            int chain = boardState != null ? boardState.ChainCount : 0;
            int pressure = boardState != null ? boardState.PressureLevel : 0;
            int pendingParts = boardState != null ? boardState.PendingPartsReward : 0;
            int pendingOvercharge = boardState != null ? boardState.PendingOverchargeReward : 0;
            bool canRetry = runSession != null && currentNode != null && parts >= retryPartsCost;

            if (titleText != null)
                titleText.text = "Round Lost";

            if (messageText != null)
                messageText.text = message ?? string.Empty;

            if (summaryText != null)
            {
                summaryText.text =
                    $"Bounty: {bountyName}\n" +
                    $"HP: {hp}/{maxHp}\n" +
                    $"Parts: {parts}\n" +
                    $"Overcharge: {overcharge}\n" +
                    $"Chain: {chain}\n" +
                    $"Pressure: {pressure}\n" +
                    $"Pending Reward: Parts {pendingParts}, Overcharge {pendingOvercharge}\n\n" +
                    $"Retry keeps the same bounty and current risk.\n" +
                    $"Retreat discards this bounty and pending reward, then opens Workshop.\n" +
                    $"Abandon ends the run.";
            }

            if (retryButtonText != null)
                retryButtonText.text = $"Retry - Parts {retryPartsCost}";

            if (retreatButtonText != null)
                retreatButtonText.text = "Retreat";

            if (abandonButtonText != null)
                abandonButtonText.text = "Abandon Run";

            if (retryButton != null)
                retryButton.interactable = canRetry;
        }

        /// <summary>View 표시 상태를 변경한다.</summary>
        public void SetVisible(bool visible)
        {
            if (root != null)
                root.SetActive(visible);
            else
                gameObject.SetActive(visible);
        }

        private void HandleRetryClicked()
        {
            RetryRequested?.Invoke();
        }

        private void HandleRetreatClicked()
        {
            RetreatRequested?.Invoke();
        }

        private void HandleAbandonClicked()
        {
            AbandonRequested?.Invoke();
        }
    }
}
