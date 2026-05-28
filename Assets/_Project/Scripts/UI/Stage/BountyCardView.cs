using System;
using Tessera.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tessera.UI
{
    /// <summary>Bounty Board에 표시되는 수배지 카드 View다.</summary>
    public class BountyCardView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text typeText;
        [SerializeField] private TMP_Text rewardText;
        [SerializeField] private TMP_Text stateText;
        [SerializeField] private TMP_Text pressureText;

        private StageBountyNodeState node;

        /// <summary>카드 선택 이벤트.</summary>
        public event Action<StageBountyNodeState> Selected;

        private void OnEnable()
        {
            if (button != null)
                button.onClick.AddListener(HandleClicked);
        }

        private void OnDisable()
        {
            if (button != null)
                button.onClick.RemoveListener(HandleClicked);
        }

        /// <summary>카드를 지정 수배지 상태로 갱신한다.</summary>
        public void Bind(StageBountyNodeState nodeState)
        {
            node = nodeState;

            if (titleText != null)
                titleText.text = node?.Definition != null ? node.Definition.DisplayName : "Empty";

            if (typeText != null)
                typeText.text = node?.Definition != null ? node.Definition.RoundType.ToString() : "-";

            if (rewardText != null)
                rewardText.text = node?.Definition != null ? $"Parts +{node.Definition.RewardParts}" : "-";

            if (pressureText != null)
                pressureText.text = node != null ? $"Enrage {node.EnrageLevel}" : string.Empty;

            if (stateText != null)
                stateText.text = BuildStateText();

            if (button != null)
                button.interactable = node != null && node.IsAvailable;
        }

        private void HandleClicked()
        {
            if (node == null)
                return;

            if (!node.IsAvailable)
                return;

            Selected?.Invoke(node);
        }

        private string BuildStateText()
        {
            if (node == null)
                return "None";

            if (node.IsCleared)
                return "Cleared";

            if (node.IsDiscarded)
                return "Discarded";

            if (node.IsAvailable)
                return "Available";

            return "Locked";
        }
    }
}
