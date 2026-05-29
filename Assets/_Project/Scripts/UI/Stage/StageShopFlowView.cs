using System;
using Tessera.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tessera.UI
{
    /// <summary>Stage Flow 테스트용 임시 Shop Shell View다.</summary>
    public class StageShopFlowView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private TMP_Text resourceText;
        [SerializeField] private Button continueButton;

        public event Action ContinueRequested;

        private void OnEnable()
        {
            if (continueButton != null)
                continueButton.onClick.AddListener(HandleContinueClicked);
        }

        private void OnDisable()
        {
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

            if (resourceText != null)
            {
                int parts = runSession != null ? runSession.Parts : 0;
                int hp = runSession != null ? runSession.PlayerCurrentHp : 0;
                int maxHp = runSession != null ? runSession.PlayerMaxHp : 0;
                int overcharge = runSession != null ? runSession.Overcharge : 0;
                int runChain = runSession != null ? runSession.RunChainCount : 0;
                int stageChain = boardState != null ? boardState.ChainCount : 0;
                int pressure = boardState != null ? boardState.PressureLevel : 0;
                bool bossForced = boardState != null && boardState.IsBossForcedAfterCashOut;

                resourceText.text =
                    $"HP {hp}/{maxHp}\n" +
                    $"Parts {parts}\n" +
                    $"Overcharge {overcharge}\n" +
                    $"Run Chain {runChain}\n" +
                    $"Stage Chain {stageChain}\n" +
                    $"Pressure {pressure}\n" +
                    $"Next Boss Forced: {bossForced}";
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

        private static string ResolveTitle(StageShopReasonType reasonType)
        {
            if (reasonType == StageShopReasonType.StageClear)
                return "Workshop - Stage Clear";

            if (reasonType == StageShopReasonType.CashOut)
                return "Workshop - Cash Out";

            if (reasonType == StageShopReasonType.Retreat)
                return "Workshop - Retreat";

            if (reasonType == StageShopReasonType.Tutorial)
                return "Workshop - Tutorial";

            return "Workshop";
        }

        private void HandleContinueClicked()
        {
            ContinueRequested?.Invoke();
        }
    }
}
