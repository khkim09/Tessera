using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Tessera.UI
{
    /// <summary>RunInfo 족보 창 전체 표시, 탭 전환, Entry Pool 생성을 담당한다.</summary>
    public class RunInfoCastBookWindowView : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject root;

        [Header("Entries")]
        [SerializeField] private RectTransform entryRoot;
        [SerializeField] private RunInfoCastBookEntryView entryTemplate;
        [SerializeField] private int prewarmEntryCount = 14;

        [Header("Buttons")]
        [SerializeField] private Button closeButton;

        [Header("Tabs")]
        [SerializeField] private Button diceDeckButton;
        [SerializeField] private Button diceCombosButton;
        [SerializeField] private GameObject diceDeckBody;
        [SerializeField] private GameObject diceCombosBody;
        [SerializeField] private GameObject diceDeckCursorImage;
        [SerializeField] private GameObject diceCombosCursorImage;
        [SerializeField] private bool showDiceCombosOnOpen = true;

        private readonly List<RunInfoCastBookEntryView> entryPool = new List<RunInfoCastBookEntryView>();

        /// <summary>Show 호출로 Root를 활성화하는 중 Awake Hide를 막기 위한 플래그다.</summary>
        private bool isActivatingRootFromShow;

        /// <summary>창 닫기 요청 이벤트다.</summary>
        public event Action CloseRequested;

        /// <summary>현재 창 표시 여부다.</summary>
        public bool IsVisible
        {
            get
            {
                GameObject targetRoot = ResolveRoot();
                return targetRoot != null && targetRoot.activeSelf;
            }
        }

        /// <summary>초기 참조를 보정하고 버튼 이벤트 및 Entry Pool을 준비한다.</summary>
        private void Awake()
        {
            if (root == null)
                root = gameObject;

            if (entryTemplate != null)
                entryTemplate.gameObject.SetActive(false);

            PrewarmEntries();
            AddButtonListeners();

            if (!isActivatingRootFromShow)
                Hide();
        }

        /// <summary>버튼 이벤트를 해제한다.</summary>
        private void OnDestroy()
        {
            RemoveButtonListeners();
        }

        /// <summary>RunInfo 족보 창을 표시하고 Entry 내용을 갱신한다.</summary>
        public void Show(IReadOnlyList<RunInfoCastBookEntrySnapshot> snapshots)
        {
            GameObject targetRoot = ResolveRoot();

            try
            {
                isActivatingRootFromShow = true;

                if (targetRoot != null)
                    targetRoot.SetActive(true);
            }
            finally
            {
                isActivatingRootFromShow = false;
            }

            if (showDiceCombosOnOpen)
                ShowDiceCombosTab();
            else
                ShowDiceDeckTab();

            RefreshEntries(snapshots);

            if (entryRoot != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(entryRoot);
        }

        /// <summary>RunInfo 족보 창을 숨기고 Entry Pool을 비활성화한다.</summary>
        public void Hide()
        {
            DeactivateAllEntries();

            GameObject targetRoot = ResolveRoot();

            if (targetRoot != null)
                targetRoot.SetActive(false);
        }

        /// <summary>Dice Deck 탭을 표시한다.</summary>
        private void ShowDiceDeckTab()
        {
            SetTabState(true);
        }

        /// <summary>Dice Combos 탭을 표시한다.</summary>
        private void ShowDiceCombosTab()
        {
            SetTabState(false);
        }

        /// <summary>RunInfo 탭 표시 상태를 적용한다.</summary>
        private void SetTabState(bool showDiceDeck)
        {
            if (diceDeckBody != null)
                diceDeckBody.SetActive(showDiceDeck);

            if (diceCombosBody != null)
                diceCombosBody.SetActive(!showDiceDeck);

            if (diceDeckCursorImage != null)
                diceDeckCursorImage.SetActive(showDiceDeck);

            if (diceCombosCursorImage != null)
                diceCombosCursorImage.SetActive(!showDiceDeck);
        }

        /// <summary>스냅샷 수만큼 Entry Pool을 활성화하고 데이터를 반영한다.</summary>
        private void RefreshEntries(IReadOnlyList<RunInfoCastBookEntrySnapshot> snapshots)
        {
            DeactivateAllEntries();

            if (snapshots == null)
                return;

            EnsureEntryPool(snapshots.Count);

            for (int i = 0; i < snapshots.Count; i++)
            {
                RunInfoCastBookEntryView entryView = entryPool[i];

                if (entryView == null)
                    continue;

                entryView.transform.SetSiblingIndex(i);
                entryView.Bind(snapshots[i]);
                entryView.gameObject.SetActive(true);
            }
        }

        /// <summary>Entry Pool을 미리 생성한다.</summary>
        private void PrewarmEntries()
        {
            int safeCount = Mathf.Max(0, prewarmEntryCount);
            EnsureEntryPool(safeCount);
            DeactivateAllEntries();
        }

        /// <summary>필요한 수만큼 Entry Pool을 확장한다.</summary>
        private void EnsureEntryPool(int requiredCount)
        {
            if (entryRoot == null || entryTemplate == null)
                return;

            while (entryPool.Count < requiredCount)
            {
                RunInfoCastBookEntryView entryView = Instantiate(entryTemplate, entryRoot);
                entryView.name = $"RunInfoCastBookEntry_{entryPool.Count:00}";
                entryView.gameObject.SetActive(false);
                entryPool.Add(entryView);
            }
        }

        /// <summary>Entry Pool 전체를 비활성화한다.</summary>
        private void DeactivateAllEntries()
        {
            for (int i = 0; i < entryPool.Count; i++)
            {
                if (entryPool[i] == null)
                    continue;

                entryPool[i].gameObject.SetActive(false);
            }
        }

        /// <summary>닫기 버튼 클릭을 외부 요청 이벤트로 변환한다.</summary>
        private void HandleCloseClicked()
        {
            CloseRequested?.Invoke();
        }

        /// <summary>버튼 이벤트를 등록한다.</summary>
        private void AddButtonListeners()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(HandleCloseClicked);
                closeButton.onClick.AddListener(HandleCloseClicked);
            }

            if (diceDeckButton != null)
            {
                diceDeckButton.onClick.RemoveListener(ShowDiceDeckTab);
                diceDeckButton.onClick.AddListener(ShowDiceDeckTab);
            }

            if (diceCombosButton != null)
            {
                diceCombosButton.onClick.RemoveListener(ShowDiceCombosTab);
                diceCombosButton.onClick.AddListener(ShowDiceCombosTab);
            }
        }

        /// <summary>버튼 이벤트를 해제한다.</summary>
        private void RemoveButtonListeners()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(HandleCloseClicked);

            if (diceDeckButton != null)
                diceDeckButton.onClick.RemoveListener(ShowDiceDeckTab);

            if (diceCombosButton != null)
                diceCombosButton.onClick.RemoveListener(ShowDiceCombosTab);
        }

        /// <summary>실제 활성/비활성 대상 Root를 반환한다.</summary>
        private GameObject ResolveRoot()
        {
            return root != null ? root : gameObject;
        }
    }
}
