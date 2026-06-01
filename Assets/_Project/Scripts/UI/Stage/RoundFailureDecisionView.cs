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

        /// <summary>Retry 버튼 클릭 이벤트.</summary>
        public event Action RetryRequested;

        /// <summary>Retreat 버튼 클릭 이벤트.</summary>
        public event Action RetreatRequested;

        /// <summary>Abandon 버튼 클릭 이벤트.</summary>
        public event Action AbandonRequested;

        /// <summary>버튼 클릭 이벤트를 연결한다.</summary>
        private void OnEnable()
        {
            if (retryButton != null)
                retryButton.onClick.AddListener(HandleRetryClicked);

            if (retreatButton != null)
                retreatButton.onClick.AddListener(HandleRetreatClicked);

            if (abandonButton != null)
                abandonButton.onClick.AddListener(HandleAbandonClicked);
        }

        /// <summary>버튼 클릭 이벤트를 해제한다.</summary>
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
            int retryMoneyCost,
            string message)
        {
            SetVisible(true);

            StageBountyNodeState currentNode = boardState != null ? boardState.CurrentNode : null;
            string bountyName = currentNode != null && currentNode.Definition != null
                ? currentNode.Definition.DisplayName
                : "Unknown Bounty";

            int money = runSession != null ? runSession.Money : 0;
            int HP = runSession != null ? runSession.PlayerCurrentHP : 0;
            int maxHP = runSession != null ? runSession.PlayerMaxHP : 0;
            int overcharge = runSession != null ? runSession.Overcharge : 0;
            int chain = boardState != null ? boardState.ChainCount : 0;
            int stageThreat = boardState != null ? boardState.StageThreatLevel : 0;
            int pendingMoney = boardState != null ? boardState.PendingMoneyReward : 0;
            bool retreatRecovery = boardState != null && boardState.IsRetreatRecoveryActive;
            bool enraged = boardState != null && boardState.IsEnraged;
            bool canRetry = runSession != null && currentNode != null && money >= retryMoneyCost;

            if (titleText != null)
                titleText.text = "Round Lost";

            if (messageText != null)
                messageText.text = message ?? string.Empty;

            if (summaryText != null)
            {
                summaryText.text =
                    $"Bounty: {bountyName}\n" +
                    $"HP: {HP}/{maxHP}\n" +
                    $"Money: {money}\n" +
                    $"Overcharge: {overcharge}\n" +
                    $"Chain: {chain}\n" +
                    $"StageThreat: {stageThreat}" + (enraged ? " / Enraged" : string.Empty) + "\n" +
                    $"Pending Money: {pendingMoney}\n" +
                    $"Retreat Recovery: {retreatRecovery}\n\n" +
                    $"Retry: pay Money, restore HP to 100%, retry the same bounty.\n" +
                    $"Retreat: discard this bounty, pay out part of Pending Money, restore HP to 80% minimum, open Emergency Workshop.\n" +
                    $"Abandon: end the run.";
            }

            if (retryButtonText != null)
                retryButtonText.text = $"Retry - Money {retryMoneyCost}";

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

        /// <summary>Retry 버튼 클릭을 외부 이벤트로 전달한다.</summary>
        private void HandleRetryClicked()
        {
            RetryRequested?.Invoke();
        }

        /// <summary>Retreat 버튼 클릭을 외부 이벤트로 전달한다.</summary>
        private void HandleRetreatClicked()
        {
            RetreatRequested?.Invoke();
        }

        /// <summary>Abandon 버튼 클릭을 외부 이벤트로 전달한다.</summary>
        private void HandleAbandonClicked()
        {
            AbandonRequested?.Invoke();
        }
    }
}
