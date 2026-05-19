using System;
using System.Collections.Generic;
using Tessera.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Tessera.UI
{
    /// <summary>현재 제출 가능한 Cast 후보 목록을 동적으로 생성한다.</summary>
    public class CastCandidatePopupView : MonoBehaviour
    {
        [Header("Entry")]
        [SerializeField] private RectTransform entryRoot;
        [SerializeField] private CastCandidateEntryView entryTemplate;

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

        /// <summary>템플릿 행을 숨겨 실제 후보 행과 구분한다.</summary>
        private void Awake()
        {
            cachedRectTransform = GetComponent<RectTransform>();

            if (entryTemplate != null)
                entryTemplate.gameObject.SetActive(false);
        }

        /// <summary>Cast 후보 목록을 다시 생성한다.</summary>
        public void Refresh(
            CastBoardViewModel viewModel,
            int opponentCurrentHp,
            RollPatternType selectedPatternType,
            Action<RollPatternType> onCandidateClicked)
        {
            Clear();

            if (viewModel == null)
            {
                ResizePopupHeight(0);
                return;
            }

            BuildFilteredEntries(viewModel);
            SortEntriesByVisibleDamageDescending();

            for (int i = 0; i < filteredEntries.Count; i++)
                CreateEntry(filteredEntries[i], opponentCurrentHp, selectedPatternType, onCandidateClicked);

            ResizePopupHeight(filteredEntries.Count);

            if (entryRoot != null)
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
        }

        private void BuildFilteredEntries(CastBoardViewModel viewModel)
        {
            filteredEntries.Clear();

            for (int i = 0; i < viewModel.Entries.Count; i++)
            {
                CastBoardEntryModel entry = viewModel.Entries[i];

                if (entry.Status != CastBoardEntryStatus.Available)
                    continue;

                // Broken Cast는 피해 0이어도 항상 명시적인 선택지로 남긴다.
                if (entry.PatternType != RollPatternType.BrokenCast && entry.RawCastScore <= 0)
                    continue;

                filteredEntries.Add(entry);
            }
        }

        private void SortEntriesByVisibleDamageDescending()
        {
            filteredEntries.Sort(CompareEntry);
        }

        private static int CompareEntry(CastBoardEntryModel left, CastBoardEntryModel right)
        {
            // Broken Cast는 0점이어도 표시하되, 기본적으로 맨 아래로 보낸다.
            if (left.PatternType == RollPatternType.BrokenCast && right.PatternType != RollPatternType.BrokenCast)
                return 1;

            if (right.PatternType == RollPatternType.BrokenCast && left.PatternType != RollPatternType.BrokenCast)
                return -1;

            int rawCompare = right.RawCastScore.CompareTo(left.RawCastScore);

            if (rawCompare != 0)
                return rawCompare;

            int finalDamageCompare = right.DamageAfterTableRules.CompareTo(left.DamageAfterTableRules);

            if (finalDamageCompare != 0)
                return finalDamageCompare;

            return left.PatternType.CompareTo(right.PatternType);
        }

        private void CreateEntry(
            CastBoardEntryModel entryModel,
            int opponentCurrentHp,
            RollPatternType selectedPatternType,
            Action<RollPatternType> onCandidateClicked)
        {
            if (entryRoot == null || entryTemplate == null)
                return;

            CastCandidateEntryView entryView = Instantiate(entryTemplate, entryRoot);
            bool isKillCandidate = entryModel.DamageAfterTableRules >= opponentCurrentHp && entryModel.DamageAfterTableRules > 0;
            bool isSelected = entryModel.PatternType == selectedPatternType;

            entryView.gameObject.SetActive(true);
            entryView.Initialize(onCandidateClicked);
            entryView.Bind(
                entryModel.PatternType,
                entryModel.DisplayName,
                entryModel.RawCastScore,
                isKillCandidate,
                isSelected);

            spawnedEntries.Add(entryView);
        }

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
