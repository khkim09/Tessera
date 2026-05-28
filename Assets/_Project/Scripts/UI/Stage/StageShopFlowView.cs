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
                titleText.text = reasonType == StageShopReasonType.StageClear ? "Workshop - Stage Clear" : "Workshop - Cash Out";

            if (messageText != null)
                messageText.text = message ?? string.Empty;

            if (resourceText != null)
            {
                int parts = runSession != null ? runSession.Parts : 0;
                int hp = runSession != null ? runSession.PlayerCurrentHp : 0;
                int maxHp = runSession != null ? runSession.PlayerMaxHp : 0;
                int overcharge = runSession != null ? runSession.Overcharge : 0;
                int chain = runSession != null ? runSession.RunChainCount : 0;

                resourceText.text = $"HP {hp}/{maxHp}\nParts {parts}\nOvercharge {overcharge}\nRun Chain {chain}";
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

        private void HandleContinueClicked()
        {
            ContinueRequested?.Invoke();
        }
    }
}
