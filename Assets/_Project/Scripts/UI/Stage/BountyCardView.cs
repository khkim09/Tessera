using System;
using System.Collections.Generic;
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
        [SerializeField] private TMP_Text stageThreatText;

        private StageBountyNodeState node;
        private StageBountyBoardState boardState;

        /// <summary>카드 선택 이벤트.</summary>
        public event Action<StageBountyNodeState> Selected;

        /// <summary>버튼 클릭 이벤트를 연결한다.</summary>
        private void OnEnable()
        {
            if (button != null)
                button.onClick.AddListener(HandleClicked);
        }

        /// <summary>버튼 클릭 이벤트를 해제한다.</summary>
        private void OnDisable()
        {
            if (button != null)
                button.onClick.RemoveListener(HandleClicked);
        }

        /// <summary>카드를 지정 수배지 상태로 갱신한다.</summary>
        public void Bind(StageBountyNodeState nodeState, StageBountyBoardState ownerBoardState)
        {
            node = nodeState;
            boardState = ownerBoardState;

            if (titleText != null)
                titleText.text = node?.Definition != null ? node.Definition.DisplayName : "Empty";

            if (typeText != null)
                typeText.text = BuildTypeText();

            if (rewardText != null)
                rewardText.text = BuildRewardText();

            if (stageThreatText != null)
                stageThreatText.text = BuildThreatText();

            if (stateText != null)
                stateText.text = BuildStateText();

            if (button != null)
                button.interactable = node != null && node.IsAvailable;
        }

        /// <summary>기존 호출 호환용 Bind 메서드다.</summary>
        public void Bind(StageBountyNodeState nodeState)
        {
            Bind(nodeState, null);
        }

        /// <summary>카드 타입 텍스트를 생성한다.</summary>
        private string BuildTypeText()
        {
            if (node == null || node.Definition == null)
                return "-";

            string roundTypeText = node.Definition.RoundType == Tessera.Core.StageRoundType.Boss
                ? "Boss"
                : "Bounty";

            return $"{roundTypeText} · Rank {node.Definition.BountyRank}";
        }

        /// <summary>카드 본문 설명 텍스트를 생성한다.</summary>
        private string BuildRewardText()
        {
            if (node == null || node.Definition == null)
                return "-";

            List<string> lines = new List<string>();

            if (!string.IsNullOrWhiteSpace(node.Definition.BountyDescription))
                lines.Add(node.Definition.BountyDescription);

            if (!string.IsNullOrWhiteSpace(node.Definition.IntentDescription))
                lines.Add($"Intent: {node.Definition.IntentDescription}");

            if (!string.IsNullOrWhiteSpace(node.Definition.SpecialRuleDescription))
                lines.Add($"Rule: {node.Definition.SpecialRuleDescription}");

            if (lines.Count <= 0)
                lines.Add(BuildFallbackDescription());

            return string.Join("\n", lines);
        }

        /// <summary>SO 설명이 비어 있을 때 사용할 기본 설명을 생성한다.</summary>
        private string BuildFallbackDescription()
        {
            if (node == null || node.Definition == null)
                return "-";

            string typeText = node.Definition.RoundType == Tessera.Core.StageRoundType.Boss
                ? "Boss bounty."
                : "Standard bounty.";

            return $"{typeText}\nDefeat the opponent within limited attempts.";
        }

        /// <summary>StageThreat / Enraged 표시 텍스트를 생성한다.</summary>
        private string BuildThreatText()
        {
            if (boardState == null)
                return string.Empty;

            string enragedText = boardState.IsEnraged ? " / Enraged" : string.Empty;
            string retreatLockText = node != null && node.IsLockedByRetreatRecovery ? " / Retreat Locked" : string.Empty;

            return $"StageThreat {boardState.StageThreatLevel}{enragedText}{retreatLockText}";
        }

        /// <summary>카드 상태 텍스트를 생성한다.</summary>
        private string BuildStateText()
        {
            if (node == null)
                return "None";

            if (node.IsCleared)
                return "Cleared";

            if (node.IsDiscarded)
                return "Discarded";

            if (node.IsLockedByRetreatRecovery)
                return "Retreat Locked";

            if (node.IsAvailable)
                return "Available";

            return "Locked";
        }

        /// <summary>버튼 클릭을 선택 이벤트로 전달한다.</summary>
        private void HandleClicked()
        {
            if (node == null)
                return;

            if (!node.IsAvailable)
                return;

            Selected?.Invoke(node);
        }
    }
}
