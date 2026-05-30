using System;
using System.Collections.Generic;
using Tessera.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tessera.UI
{
    /// <summary>
    /// Stage 내 수배지 선택 화면 View다.
    /// 수배지 카드는 CardRoot 아래에 가로 Layout으로 자동 배치된다.
    /// </summary>
    public class BountyBoardView : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject root;

        [Header("Texts")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private TMP_Text stateText;

        [Header("Cards")]
        [SerializeField] private RectTransform cardRoot;
        [SerializeField] private BountyCardView cardTemplate;

        [Header("Card Layout")]
        [SerializeField] private float cardWidth = 310f;
        [SerializeField] private float cardHeight = 450f;
        [SerializeField] private float cardSpacing = 24f;
        [SerializeField] private float horizontalPadding = 20f;
        [SerializeField] private float verticalPadding = 10f;
        [SerializeField] private bool configureLayoutOnAwake = true;

        private readonly List<BountyCardView> spawnedCards = new List<BountyCardView>();

        /// <summary>수배지 선택 이벤트.</summary>
        public event Action<StageBountyNodeState> RoundSelected;

        /// <summary>초기 Layout 컴포넌트를 보정한다.</summary>
        private void Awake()
        {
            if (configureLayoutOnAwake)
                ConfigureCardRootLayout();
        }

        /// <summary>Bounty Board를 표시한다.</summary>
        public void Show(StageBountyBoardState boardState, string message)
        {
            SetVisible(true);

            if (titleText != null)
            {
                string stageName = boardState?.StageDefinition != null
                    ? boardState.StageDefinition.DisplayName
                    : "Bounty Board";

                titleText.text = stageName;
            }

            if (messageText != null)
                messageText.text = message ?? string.Empty;

            if (stateText != null)
                stateText.text = BuildStateText(boardState);

            RebuildCards(boardState);
        }

        /// <summary>View 표시 상태를 변경한다.</summary>
        public void SetVisible(bool visible)
        {
            if (root != null)
                root.SetActive(visible);
            else
                gameObject.SetActive(visible);
        }

        /// <summary>CardRoot에 HorizontalLayoutGroup과 ContentSizeFitter를 보정한다.</summary>
        private void ConfigureCardRootLayout()
        {
            if (cardRoot == null)
                return;

            HorizontalLayoutGroup layoutGroup = cardRoot.GetComponent<HorizontalLayoutGroup>();

            if (layoutGroup == null)
                layoutGroup = cardRoot.gameObject.AddComponent<HorizontalLayoutGroup>();

            layoutGroup.padding = new RectOffset(
                Mathf.RoundToInt(horizontalPadding),
                Mathf.RoundToInt(horizontalPadding),
                Mathf.RoundToInt(verticalPadding),
                Mathf.RoundToInt(verticalPadding));

            layoutGroup.spacing = cardSpacing;
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.reverseArrangement = false;

            ContentSizeFitter contentSizeFitter = cardRoot.GetComponent<ContentSizeFitter>();

            if (contentSizeFitter == null)
                contentSizeFitter = cardRoot.gameObject.AddComponent<ContentSizeFitter>();

            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        /// <summary>현재 BoardState 기준으로 카드 목록을 재생성한다.</summary>
        private void RebuildCards(StageBountyBoardState boardState)
        {
            ClearCards();

            if (boardState == null || cardTemplate == null || cardRoot == null)
                return;

            ConfigureCardRootLayout();

            for (int i = 0; i < boardState.BountyNodes.Count; i++)
            {
                BountyCardView card = Instantiate(cardTemplate, cardRoot);
                RectTransform cardRectTransform = card.GetComponent<RectTransform>();

                if (cardRectTransform != null)
                    cardRectTransform.sizeDelta = new Vector2(cardWidth, cardHeight);

                LayoutElement layoutElement = card.GetComponent<LayoutElement>();

                if (layoutElement == null)
                    layoutElement = card.gameObject.AddComponent<LayoutElement>();

                layoutElement.preferredWidth = cardWidth;
                layoutElement.preferredHeight = cardHeight;
                layoutElement.flexibleWidth = 0f;
                layoutElement.flexibleHeight = 0f;

                card.gameObject.SetActive(true);
                card.Selected += HandleCardSelected;
                card.Bind(boardState.BountyNodes[i], boardState);

                spawnedCards.Add(card);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(cardRoot);
        }

        /// <summary>Bounty Board 상태 텍스트를 생성한다.</summary>
        private static string BuildStateText(StageBountyBoardState boardState)
        {
            if (boardState == null)
                return string.Empty;

            string enragedText = boardState.IsEnraged ? " / Enraged" : string.Empty;
            string retreatRecoveryText = boardState.IsRetreatRecoveryActive ? " / Retreat Recovery" : string.Empty;

            return
                $"Pending Money {boardState.PendingMoneyReward} | " +
                $"Chain {boardState.ChainCount} | " +
                $"StageThreat {boardState.StageThreatLevel}{enragedText}{retreatRecoveryText}";
        }

        /// <summary>기존 생성 카드들을 제거한다.</summary>
        private void ClearCards()
        {
            for (int i = 0; i < spawnedCards.Count; i++)
            {
                if (spawnedCards[i] == null)
                    continue;

                spawnedCards[i].Selected -= HandleCardSelected;
                Destroy(spawnedCards[i].gameObject);
            }

            spawnedCards.Clear();

            if (cardRoot != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(cardRoot);
        }

        /// <summary>카드 선택 이벤트를 외부로 전달한다.</summary>
        private void HandleCardSelected(StageBountyNodeState node)
        {
            RoundSelected?.Invoke(node);
        }
    }
}
