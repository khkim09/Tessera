using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Tessera.UI
{
    /// <summary>RunInfo 족보 창 전체 표시, 탭 전환, Entry Pool 생성, Tooltip 배치를 담당한다.</summary>
    public class RunInfoCastBookWindowView : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject root;

        [Header("Entries")]
        [SerializeField] private RectTransform entryRoot;
        [SerializeField] private RunInfoCastBookEntryView entryTemplate;
        [SerializeField] private int prewarmEntryCount = 14;

        [Header("Tooltip")]
        [SerializeField] private RectTransform tooltipOverlayRoot;
        [SerializeField] private RunInfoCastBookTooltipView tooltipView;
        [SerializeField] private ScrollRect entriesScrollRect;
        [SerializeField] private SmoothScrollRectWheelInput smoothWheelInput;
        [SerializeField] private RectTransform tooltipPlacementReferenceRect;
        [SerializeField] private float tooltipEdgePadding = 8f;
        [SerializeField] private bool hideTooltipOnScroll = false;

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

        /// <summary>RunInfo Cast Entry View 재사용 풀이다.</summary>
        private readonly List<RunInfoCastBookEntryView> entryPool = new List<RunInfoCastBookEntryView>();

        /// <summary>Show 호출로 Root를 활성화하는 중 Awake Hide를 막기 위한 플래그다.</summary>
        private bool isActivatingRootFromShow;

        /// <summary>현재 Tooltip 표시 기준 Entry다.</summary>
        private RunInfoCastBookEntryView tooltipEntryView;

        /// <summary>포인터 드래그로 ScrollView를 움직이는 중인지 여부다.</summary>
        private bool isScrollDraggingByPointer;

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

        /// <summary>초기 참조를 보정하고 버튼/스크롤/Entry 이벤트 및 Entry Pool을 준비한다.</summary>
        private void Awake()
        {
            if (root == null)
                root = gameObject;

            if (entryTemplate != null)
                entryTemplate.gameObject.SetActive(false);

            AssignTooltipReferencesIfMissing();
            PrewarmEntries();
            AddButtonListeners();
            AddScrollListeners();

            if (!isActivatingRootFromShow)
                Hide();
        }

        /// <summary>버튼, 스크롤, Entry 이벤트를 해제한다.</summary>
        private void OnDestroy()
        {
            RemoveEntryHoverListeners();
            RemoveScrollListeners();
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

        /// <summary>RunInfo 족보 창을 숨기고 Entry Pool과 Tooltip을 비활성화한다.</summary>
        public void Hide()
        {
            isScrollDraggingByPointer = false;
            SetEntryHoverSuppressed(false);
            HideTooltip();
            DeactivateAllEntries();

            GameObject targetRoot = ResolveRoot();

            if (targetRoot != null)
                targetRoot.SetActive(false);
        }

        /// <summary>Dice Deck 탭을 표시한다.</summary>
        private void ShowDiceDeckTab()
        {
            HideTooltip();
            SetTabState(true);
        }

        /// <summary>Dice Combos 탭을 표시한다.</summary>
        private void ShowDiceCombosTab()
        {
            HideTooltip();
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
            HideTooltip();
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
                AddEntryHoverListeners(entryView);
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

        /// <summary>Entry Hover 이벤트를 Tooltip 표시 이벤트로 연결한다.</summary>
        private void AddEntryHoverListeners(RunInfoCastBookEntryView entryView)
        {
            if (entryView == null)
                return;

            entryView.HoverStarted -= HandleEntryHoverStarted;
            entryView.HoverStarted += HandleEntryHoverStarted;
            entryView.HoverEnded -= HandleEntryHoverEnded;
            entryView.HoverEnded += HandleEntryHoverEnded;
        }

        /// <summary>Entry Pool의 Tooltip Hover 이벤트 연결을 모두 해제한다.</summary>
        private void RemoveEntryHoverListeners()
        {
            for (int i = 0; i < entryPool.Count; i++)
            {
                RunInfoCastBookEntryView entryView = entryPool[i];

                if (entryView == null)
                    continue;

                entryView.HoverStarted -= HandleEntryHoverStarted;
                entryView.HoverEnded -= HandleEntryHoverEnded;
            }
        }

        /// <summary>Entry Hover 시작 시 Tooltip 데이터를 채우고 Entry 위/아래에 배치한다.</summary>
        private void HandleEntryHoverStarted(RunInfoCastBookEntryView entryView, RunInfoCastBookEntrySnapshot snapshot)
        {
            if (entryView == null || snapshot == null || tooltipView == null)
                return;

            if (isScrollDraggingByPointer)
                return;

            tooltipEntryView = entryView;

            RunInfoCastBookTooltipContent content = RunInfoCastBookTooltipCatalog.Resolve(snapshot.PatternType);
            tooltipView.Show(snapshot, content);
            PositionTooltip(entryView);
        }

        /// <summary>Entry Hover 종료 시 현재 Entry의 Tooltip만 숨긴다.</summary>
        private void HandleEntryHoverEnded(RunInfoCastBookEntryView entryView, RunInfoCastBookEntrySnapshot snapshot)
        {
            if (tooltipEntryView != entryView)
                return;

            HideTooltip();
        }

        /// <summary>현재 Tooltip을 숨긴다.</summary>
        private void HideTooltip()
        {
            HideTooltip(true);
        }

        /// <summary>현재 Tooltip을 숨기고 Hover 기준 Entry 유지 여부를 적용한다.</summary>
        private void HideTooltip(bool clearHoverTarget)
        {
            if (clearHoverTarget)
                tooltipEntryView = null;

            if (tooltipView != null)
                tooltipView.Hide();
        }

        /// <summary>Tooltip을 기준 Rect 중앙에 따라 Entry 아래 또는 위에 맞닿게 배치한다.</summary>
        private void PositionTooltip(RunInfoCastBookEntryView entryView)
        {
            RectTransform entryRect = entryView != null ? entryView.RectTransform : null;
            RectTransform overlayRoot = ResolveTooltipOverlayRoot();
            RectTransform tooltipRect = tooltipView != null ? tooltipView.RectTransform : null;

            if (entryRect == null || overlayRoot == null || tooltipRect == null)
                return;

            Canvas canvas = overlayRoot.GetComponentInParent<Canvas>();
            Camera uiCamera = ResolveUICamera(canvas);

            Vector3[] entryCorners = new Vector3[4];
            entryRect.GetWorldCorners(entryCorners);

            bool placeBelow = ShouldPlaceTooltipBelow(entryCorners, uiCamera);

            Vector3 anchorWorld = placeBelow
                ? (entryCorners[0] + entryCorners[3]) * 0.5f
                : (entryCorners[1] + entryCorners[2]) * 0.5f;

            Vector2 anchorScreen = RectTransformUtility.WorldToScreenPoint(uiCamera, anchorWorld);

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    overlayRoot,
                    anchorScreen,
                    uiCamera,
                    out Vector2 localPoint))
            {
                return;
            }

            tooltipRect.SetParent(overlayRoot, false);
            tooltipRect.anchorMin = new Vector2(0.5f, 0.5f);
            tooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
            tooltipRect.pivot = new Vector2(0.5f, placeBelow ? 1f : 0f);

            tooltipRect.anchoredPosition = ClampTooltipAnchoredPosition(
                overlayRoot,
                tooltipRect,
                localPoint);
        }

        /// <summary>Tooltip 위치가 Overlay 경계를 넘지 않도록 보정한다.</summary>
        private Vector2 ClampTooltipAnchoredPosition(
            RectTransform overlayRoot,
            RectTransform tooltipRect,
            Vector2 anchoredPosition)
        {
            if (overlayRoot == null || tooltipRect == null)
                return anchoredPosition;

            Rect overlayRect = overlayRoot.rect;
            Rect tooltipLocalRect = tooltipRect.rect;
            Vector2 pivot = tooltipRect.pivot;
            float padding = Mathf.Max(0f, tooltipEdgePadding);

            float left = anchoredPosition.x - tooltipLocalRect.width * pivot.x;
            float right = anchoredPosition.x + tooltipLocalRect.width * (1f - pivot.x);
            float bottom = anchoredPosition.y - tooltipLocalRect.height * pivot.y;
            float top = anchoredPosition.y + tooltipLocalRect.height * (1f - pivot.y);

            float minX = overlayRect.xMin + padding;
            float maxX = overlayRect.xMax - padding;
            float minY = overlayRect.yMin + padding;
            float maxY = overlayRect.yMax - padding;

            if (left < minX)
                anchoredPosition.x += minX - left;

            if (right > maxX)
                anchoredPosition.x -= right - maxX;

            if (bottom < minY)
                anchoredPosition.y += minY - bottom;

            if (top > maxY)
                anchoredPosition.y -= top - maxY;

            return anchoredPosition;
        }

        /// <summary>Tooltip Overlay Root 참조를 반환한다.</summary>
        private RectTransform ResolveTooltipOverlayRoot()
        {
            if (tooltipOverlayRoot != null)
                return tooltipOverlayRoot;

            return transform as RectTransform;
        }

        /// <summary>Entry가 기준 Rect 중앙보다 위에 있어 Tooltip을 아래에 배치해야 하는지 반환한다.</summary>
        private bool ShouldPlaceTooltipBelow(Vector3[] entryCorners, Camera uiCamera)
        {
            if (entryCorners == null || entryCorners.Length < 4)
                return false;

            Vector3 entryCenterWorld = (entryCorners[0] + entryCorners[2]) * 0.5f;
            Vector2 entryCenterScreen = RectTransformUtility.WorldToScreenPoint(uiCamera, entryCenterWorld);

            RectTransform referenceRect = ResolveTooltipPlacementReferenceRect();
            Vector2 referenceCenterScreen = ResolveRectCenterScreenPoint(referenceRect, uiCamera);

            return entryCenterScreen.y > referenceCenterScreen.y;
        }

        /// <summary>Tooltip 상/하 배치 판단 기준 RectTransform을 반환한다.</summary>
        private RectTransform ResolveTooltipPlacementReferenceRect()
        {
            if (tooltipPlacementReferenceRect != null)
                return tooltipPlacementReferenceRect;

            if (entriesScrollRect != null && entriesScrollRect.viewport != null)
                return entriesScrollRect.viewport;

            if (entriesScrollRect != null)
                return entriesScrollRect.transform as RectTransform;

            if (entryRoot != null)
                return entryRoot;

            return ResolveTooltipOverlayRoot();
        }

        /// <summary>지정 RectTransform의 화면 중앙 좌표를 반환한다.</summary>
        private static Vector2 ResolveRectCenterScreenPoint(RectTransform rectTransform, Camera uiCamera)
        {
            if (rectTransform == null)
                return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            Vector3 centerWorld = (corners[0] + corners[2]) * 0.5f;
            return RectTransformUtility.WorldToScreenPoint(uiCamera, centerWorld);
        }

        /// <summary>Canvas RenderMode에 맞는 UI Camera를 반환한다.</summary>
        private static Camera ResolveUICamera(Canvas canvas)
        {
            if (canvas == null)
                return null;

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;

            return canvas.worldCamera;
        }

        /// <summary>닫기 버튼 클릭을 외부 요청 이벤트로 변환한다.</summary>
        private void HandleCloseClicked()
        {
            CloseRequested?.Invoke();
        }

        /// <summary>ScrollRect 값 변경 시 Tooltip 위치를 다시 맞추거나 표시만 숨긴다.</summary>
        private void HandleScrollValueChanged(Vector2 normalizedPosition)
        {
            if (tooltipEntryView == null)
                return;

            if (isScrollDraggingByPointer)
            {
                HideTooltip();
                return;
            }

            if (hideTooltipOnScroll)
            {
                HideTooltip(false);
                return;
            }

            PositionTooltip(tooltipEntryView);
        }

        /// <summary>ScrollView 포인터 드래그 상태 변경 시 Tooltip과 Entry Hover 표시를 제어한다.</summary>
        private void HandleScrollDragStateChanged(bool isDragging)
        {
            isScrollDraggingByPointer = isDragging;
            SetEntryHoverSuppressed(isDragging);

            if (isDragging)
                HideTooltip();
        }

        /// <summary>모든 Entry의 Hover 표시 입력 차단 상태를 적용한다.</summary>
        private void SetEntryHoverSuppressed(bool suppressed)
        {
            for (int i = 0; i < entryPool.Count; i++)
            {
                if (entryPool[i] == null)
                    continue;

                entryPool[i].SetHoverInputSuppressed(suppressed);
            }
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

        /// <summary>ScrollRect 이벤트를 등록한다.</summary>
        private void AddScrollListeners()
        {
            if (entriesScrollRect != null)
            {
                entriesScrollRect.onValueChanged.RemoveListener(HandleScrollValueChanged);
                entriesScrollRect.onValueChanged.AddListener(HandleScrollValueChanged);
            }

            if (smoothWheelInput != null)
            {
                smoothWheelInput.DragStateChanged -= HandleScrollDragStateChanged;
                smoothWheelInput.DragStateChanged += HandleScrollDragStateChanged;
            }
        }

        /// <summary>ScrollRect 이벤트를 해제한다.</summary>
        private void RemoveScrollListeners()
        {
            if (entriesScrollRect != null)
                entriesScrollRect.onValueChanged.RemoveListener(HandleScrollValueChanged);

            if (smoothWheelInput != null)
                smoothWheelInput.DragStateChanged -= HandleScrollDragStateChanged;
        }

        /// <summary>Tooltip 관련 참조를 자동 보정한다.</summary>
        private void AssignTooltipReferencesIfMissing()
        {
            if (tooltipOverlayRoot == null)
                tooltipOverlayRoot = transform as RectTransform;

            if (entriesScrollRect == null)
                entriesScrollRect = GetComponentInChildren<ScrollRect>(true);

            if (smoothWheelInput == null && entriesScrollRect != null)
                smoothWheelInput = entriesScrollRect.GetComponent<SmoothScrollRectWheelInput>();
        }

        /// <summary>실제 활성/비활성 대상 Root를 반환한다.</summary>
        private GameObject ResolveRoot()
        {
            return root != null ? root : gameObject;
        }
    }
}
