using System;
using System.Collections.Generic;
using Tessera.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Tessera.UI
{
    /// <summary>현재 가능한 Cast 후보를 표시하고 선택 이벤트를 전달한다.</summary>
    public class CastCandidatePopupView : MonoBehaviour
    {
        [Header("Entry")]
        [SerializeField] private RectTransform entryRoot;
        [SerializeField] private CastCandidateEntryView entryTemplate;

        [Header("Popup Options")]
        [SerializeField] private bool showPopup = true;

        [Header("Dynamic Size")]
        [SerializeField] private bool resizeHeightToEntryCount = true;
        [SerializeField] private float entryHeight = 30f;
        [SerializeField] private float entrySpacing = 7f;
        [SerializeField] private float verticalPadding = 20f;
        [SerializeField] private float minHeight = 46f;
        [SerializeField] private float maxHeight = 420f;

        private readonly List<CastCandidateEntryView> spawnedEntries = new List<CastCandidateEntryView>();
        private readonly List<CastBoardEntryModel> filteredEntries = new List<CastBoardEntryModel>();
        private RectTransform cachedRectTransform;

        /// <summary>템플릿 행을 숨기고 RectTransform을 캐싱한다.</summary>
        private void Awake()
        {
            cachedRectTransform = GetComponent<RectTransform>();

            if (entryTemplate != null)
                entryTemplate.gameObject.SetActive(false);
        }

        /// <summary>사용자 설정 기준 Popup 표시 허용 여부를 설정한다.</summary>
        public void SetPopupVisible(bool isVisible)
        {
            showPopup = isVisible;
            ApplyResolvedPopupVisibility();
        }

        /// <summary>Cast 후보 목록을 다시 생성한다.</summary>
        public void Refresh(
            CastBoardViewModel viewModel,
            RollPatternType selectedPatternType,
            RollPatternType recommendedPatternType,
            Action<RollPatternType> onCandidateClicked)
        {
            Clear();

            if (!showPopup)
            {
                ResizePopupHeight(0);
                ApplyResolvedPopupVisibility();
                return;
            }

            if (viewModel == null)
            {
                ResizePopupHeight(0);
                ApplyResolvedPopupVisibility();
                return;
            }

            BuildFilteredEntries(viewModel);
            SortEntriesByCastScoreDescending();

            for (int i = 0; i < filteredEntries.Count; i++)
                CreateEntry(filteredEntries[i], selectedPatternType, recommendedPatternType, onCandidateClicked);

            ResizePopupHeight(filteredEntries.Count);
            ApplyResolvedPopupVisibility();

            if (entryRoot != null && gameObject.activeInHierarchy)
                LayoutRebuilder.ForceRebuildLayoutImmediate(entryRoot);
        }

        /// <summary>생성된 Cast 후보 행을 모두 제거한다.</summary>
        public void Clear()
        {
            for (int i = spawnedEntries.Count - 1; i >= 0; i--)
            {
                if (spawnedEntries[i] != null)
                    Destroy(spawnedEntries[i].gameObject);
            }

            spawnedEntries.Clear();
            filteredEntries.Clear();

            ResizePopupHeight(0);
        }

        /// <summary>사용자 표시 설정과 후보 개수 기준으로 실제 Popup 활성 상태를 적용한다.</summary>
        private void ApplyResolvedPopupVisibility()
        {
            bool shouldBeVisible = showPopup && spawnedEntries.Count > 0;

            if (gameObject.activeSelf != shouldBeVisible)
                gameObject.SetActive(shouldBeVisible);
        }

        /// <summary>표시 가능한 후보만 필터링한다.</summary>
        private void BuildFilteredEntries(CastBoardViewModel viewModel)
        {
            filteredEntries.Clear();

            for (int i = 0; i < viewModel.Entries.Count; i++)
            {
                CastBoardEntryModel entry = viewModel.Entries[i];

                if (entry.Status != CastBoardEntryStatus.Available)
                    continue;

                // Broken Cast는 0점이어도 후보로 유지한다.
                if (entry.PatternType != RollPatternType.BrokenCast && entry.RawCastScore <= 0)
                    continue;

                filteredEntries.Add(entry);
            }
        }

        /// <summary>Cast Score 높은 순으로 후보 목록을 정렬한다.</summary>
        private void SortEntriesByCastScoreDescending()
        {
            filteredEntries.Sort(CompareEntry);
        }

        /// <summary>두 후보의 표시 우선순위를 비교한다.</summary>
        private static int CompareEntry(CastBoardEntryModel left, CastBoardEntryModel right)
        {
            if (left.PatternType == RollPatternType.BrokenCast && right.PatternType != RollPatternType.BrokenCast)
                return 1;

            if (right.PatternType == RollPatternType.BrokenCast && left.PatternType != RollPatternType.BrokenCast)
                return -1;

            int scoreCompare = right.RawCastScore.CompareTo(left.RawCastScore);

            if (scoreCompare != 0)
                return scoreCompare;

            int damageCompare = right.DamageAfterTableRules.CompareTo(left.DamageAfterTableRules);

            if (damageCompare != 0)
                return damageCompare;

            return left.PatternType.CompareTo(right.PatternType);
        }

        /// <summary>후보 행 하나를 생성한다.</summary>
        private void CreateEntry(
            CastBoardEntryModel entryModel,
            RollPatternType selectedPatternType,
            RollPatternType recommendedPatternType,
            Action<RollPatternType> onCandidateClicked)
        {
            if (entryRoot == null || entryTemplate == null)
                return;

            CastCandidateEntryView entryView = Instantiate(entryTemplate, entryRoot);

            bool isSelected = entryModel.PatternType == selectedPatternType;
            bool isRecommended = entryModel.PatternType == recommendedPatternType;

            entryView.gameObject.SetActive(true);
            entryView.Initialize(onCandidateClicked);
            entryView.Bind(
                entryModel.PatternType,
                entryModel.DisplayName,
                entryModel.RawCastScore,
                isSelected,
                isRecommended);

            spawnedEntries.Add(entryView);
        }

        /// <summary>후보 개수에 맞춰 Popup 높이를 조절한다.</summary>
        private void ResizePopupHeight(int entryCount)
        {
            if (!resizeHeightToEntryCount || cachedRectTransform == null)
                return;

            float spacingHeight = entryCount > 1 ? entrySpacing * (entryCount - 1) : 0f;
            float targetHeight = verticalPadding + entryHeight * entryCount + spacingHeight;
            targetHeight = Mathf.Clamp(targetHeight, minHeight, maxHeight);

            Vector2 sizeDelta = cachedRectTransform.sizeDelta;
            sizeDelta.y = targetHeight;
            cachedRectTransform.sizeDelta = sizeDelta;
        }
    }
}
