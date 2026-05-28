using System;
using Tessera.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tessera.UI
{
    /// <summary>일반 수배지 승리 후 Cash Out / Chain / Boss 선택을 표시한다.</summary>
    public class StageRewardDecisionView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private TMP_Text rewardText;
        [SerializeField] private TMP_Text pressureText;
        [SerializeField] private Button cashOutButton;
        [SerializeField] private Button chainButton;
        [SerializeField] private Button bossButton;

        public event Action CashOutRequested;
        public event Action ChainRequested;
        public event Action BossRequested;

        private void OnEnable()
        {
            if (cashOutButton != null)
                cashOutButton.onClick.AddListener(HandleCashOutClicked);

            if (chainButton != null)
                chainButton.onClick.AddListener(HandleChainClicked);

            if (bossButton != null)
                bossButton.onClick.AddListener(HandleBossClicked);
        }

        private void OnDisable()
        {
            if (cashOutButton != null)
                cashOutButton.onClick.RemoveListener(HandleCashOutClicked);

            if (chainButton != null)
                chainButton.onClick.RemoveListener(HandleChainClicked);

            if (bossButton != null)
                bossButton.onClick.RemoveListener(HandleBossClicked);
        }

        /// <summary>보상 선택 화면을 표시한다.</summary>
        public void Show(StageBountyBoardState boardState, string message)
        {
            SetVisible(true);

            if (titleText != null)
                titleText.text = "Decision";

            if (messageText != null)
                messageText.text = message ?? string.Empty;

            if (rewardText != null)
            {
                int parts = boardState != null ? boardState.PendingPartsReward : 0;
                int overcharge = boardState != null ? boardState.PendingOverchargeReward : 0;
                rewardText.text = $"Pending Reward: Parts {parts}, Overcharge {overcharge}";
            }

            if (pressureText != null)
            {
                int chain = boardState != null ? boardState.ChainCount : 0;
                int pressure = boardState != null ? boardState.PressureLevel : 0;
                pressureText.text = $"Chain {chain} / Pressure {pressure}";
            }
        }

        /// <summary>View 표시 상태를 변경한다.</summary>
        public void SetVisible(bool visible)
        {
            if (root != null)
                root.SetActive(visible);
            else
                gameObject.SetActive(visible);
        }

        private void HandleCashOutClicked()
        {
            CashOutRequested?.Invoke();
        }

        private void HandleChainClicked()
        {
            ChainRequested?.Invoke();
        }

        private void HandleBossClicked()
        {
            BossRequested?.Invoke();
        }
    }
}
