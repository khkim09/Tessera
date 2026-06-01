using System;
using Tessera.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tessera.UI
{
    /// <summary>Stage Economy v1 Workshop Shell View다.</summary>
    public class StageShopFlowView : MonoBehaviour
    {
        private const int RepairCostMoney = 8;
        private const int RepairHealAmount = 10;
        private const int UpgradeTierOverchargeCost = 1;

        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private TMP_Text resourceText;

        [Header("Buttons")]
        [SerializeField] private Button repairButton;
        [SerializeField] private TMP_Text repairButtonText;
        [SerializeField] private Button upgradeTierButton;
        [SerializeField] private TMP_Text upgradeTierButtonText;
        [SerializeField] private Button continueButton;
        [SerializeField] private TMP_Text continueButtonText;

        /// <summary>Continue 버튼 클릭 이벤트.</summary>
        public event Action ContinueRequested;

        /// <summary>Repair 버튼 클릭 이벤트.</summary>
        public event Action RepairRequested;

        /// <summary>Upgrade Tier 버튼 클릭 이벤트.</summary>
        public event Action UpgradeTierRequested;

        /// <summary>버튼 클릭 이벤트를 연결한다.</summary>
        private void OnEnable()
        {
            if (repairButton != null)
                repairButton.onClick.AddListener(HandleRepairClicked);

            if (upgradeTierButton != null)
                upgradeTierButton.onClick.AddListener(HandleUpgradeTierClicked);

            if (continueButton != null)
                continueButton.onClick.AddListener(HandleContinueClicked);
        }

        /// <summary>버튼 클릭 이벤트를 해제한다.</summary>
        private void OnDisable()
        {
            if (repairButton != null)
                repairButton.onClick.RemoveListener(HandleRepairClicked);

            if (upgradeTierButton != null)
                upgradeTierButton.onClick.RemoveListener(HandleUpgradeTierClicked);

            if (continueButton != null)
                continueButton.onClick.RemoveListener(HandleContinueClicked);
        }

        /// <summary>Shop Shell을 표시한다.</summary>
        public void Show(
            TesseraRunSession runSession,
            StageBountyBoardState boardState,
            StageShopReasonType reasonType,
            string message)
        {
            SetVisible(true);

            if (titleText != null)
                titleText.text = ResolveTitle(reasonType);

            if (messageText != null)
                messageText.text = message ?? string.Empty;

            RefreshResourceText(runSession, boardState);
            RefreshButtons(runSession, reasonType);
        }

        /// <summary>View 표시 상태를 변경한다.</summary>
        public void SetVisible(bool visible)
        {
            if (root != null)
                root.SetActive(visible);
            else
                gameObject.SetActive(visible);
        }

        /// <summary>자원 표시 텍스트를 갱신한다.</summary>
        private void RefreshResourceText(TesseraRunSession runSession, StageBountyBoardState boardState)
        {
            if (resourceText == null)
                return;

            int money = runSession != null ? runSession.Money : 0;
            int HP = runSession != null ? runSession.PlayerCurrentHP : 0;
            int maxHP = runSession != null ? runSession.PlayerMaxHP : 0;
            int overcharge = runSession != null ? runSession.Overcharge : 0;
            int workshopTier = runSession != null ? runSession.CurrentWorkshopTier : 1;
            int runChain = runSession != null ? runSession.RunChainCount : 0;
            int stageChain = boardState != null ? boardState.ChainCount : 0;
            int stageThreat = boardState != null ? boardState.StageThreatLevel : 0;
            int pendingMoney = boardState != null ? boardState.PendingMoneyReward : 0;
            bool retreatRecovery = boardState != null && boardState.IsRetreatRecoveryActive;
            bool enraged = boardState != null && boardState.IsEnraged;

            resourceText.text =
                $"HP {HP}/{maxHP}\n" +
                $"Money {money}\n" +
                $"Overcharge {overcharge}\n" +
                $"Workshop Tier {workshopTier}\n" +
                $"Run Chain {runChain}\n" +
                $"Stage Chain {stageChain}\n" +
                $"StageThreat {stageThreat}" + (enraged ? " / Enraged" : string.Empty) + "\n" +
                $"Pending Money {pendingMoney}\n" +
                $"Retreat Recovery: {retreatRecovery}";
        }

        /// <summary>버튼 텍스트와 상호작용 상태를 갱신한다.</summary>
        private void RefreshButtons(TesseraRunSession runSession, StageShopReasonType reasonType)
        {
            int money = runSession != null ? runSession.Money : 0;
            int overcharge = runSession != null ? runSession.Overcharge : 0;
            int HP = runSession != null ? runSession.PlayerCurrentHP : 0;
            int maxHP = runSession != null ? runSession.PlayerMaxHP : 0;

            if (repairButtonText != null)
                repairButtonText.text = $"Repair +{RepairHealAmount} / Money {RepairCostMoney}";

            if (upgradeTierButtonText != null)
                upgradeTierButtonText.text = $"Upgrade Tier / Overcharge {UpgradeTierOverchargeCost}";

            if (continueButtonText != null)
                continueButtonText.text = reasonType == StageShopReasonType.StageClear ? "Continue to Next Stage" : "Continue";

            if (repairButton != null)
                repairButton.interactable = runSession != null && money >= RepairCostMoney && HP < maxHP;

            if (upgradeTierButton != null)
                upgradeTierButton.interactable = runSession != null && overcharge >= UpgradeTierOverchargeCost;
        }

        /// <summary>Workshop 진입 사유에 맞는 타이틀을 반환한다.</summary>
        private static string ResolveTitle(StageShopReasonType reasonType)
        {
            if (reasonType == StageShopReasonType.StageClear)
                return "Workshop - Stage Clear";

            if (reasonType == StageShopReasonType.CashOut)
                return "Workshop - Cash Out";

            if (reasonType == StageShopReasonType.Retreat)
                return "Workshop - Emergency Retreat";

            if (reasonType == StageShopReasonType.Tutorial)
                return "Workshop - Tutorial";

            return "Workshop";
        }

        /// <summary>Repair 버튼 클릭을 외부 이벤트로 전달한다.</summary>
        private void HandleRepairClicked()
        {
            RepairRequested?.Invoke();
        }

        /// <summary>Upgrade Tier 버튼 클릭을 외부 이벤트로 전달한다.</summary>
        private void HandleUpgradeTierClicked()
        {
            UpgradeTierRequested?.Invoke();
        }

        /// <summary>Continue 버튼 클릭을 외부 이벤트로 전달한다.</summary>
        private void HandleContinueClicked()
        {
            ContinueRequested?.Invoke();
        }
    }
}
