using System;
using Cysharp.Threading.Tasks;
using Tessera.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tessera.UI
{
    /// <summary>테이블 위 DeviceSlot 근처에 표시되는 Sell/Buy 공용 ActionButton View다.</summary>
    public class EquippedDeviceActionButton3DView : MonoBehaviour
    {
        /// <summary>ActionButton의 동작 종류다.</summary>
        public enum ActionKind
        {
            Sell,
            Buy
        }

        [Header("References")]
        [SerializeField] private Button actionButton;
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Canvas ownerCanvas;

        [Header("Colors")]
        [SerializeField] private Color sellColor = new Color(0.1f, 0.65f, 0.25f, 1f);
        [SerializeField] private Color buyColor = new Color(0.1f, 0.35f, 0.95f, 1f);

        [Header("Debug")]
        [SerializeField] private bool enableDebugLog;

        private int selectedSlotIndex = -1;
        private ActionKind currentActionKind = ActionKind.Sell;
        private int currentMoneyValue = -1;
        private bool isHandlingClick;

        /// <summary>Sell 버튼 요청 이벤트다.</summary>
        public event Action<int> SellRequested;

        /// <summary>Buy 버튼 요청 이벤트다.</summary>
        public event Action<int> BuyRequested;

        /// <summary>현재 표시 중인 슬롯 인덱스를 반환한다.</summary>
        public int SelectedSlotIndex => selectedSlotIndex;

        /// <summary>컴포넌트 추가 시 기본 참조를 자동 연결한다.</summary>
        private void Reset()
        {
            // 비활성 자식까지 포함해 버튼/텍스트/이미지를 자동 수집한다.
            AssignReferencesIfMissing();
        }

        /// <summary>초기화 시 참조를 보정하고 기본 숨김 상태로 만든다.</summary>
        private void Awake()
        {
            // 씬 시작 시 이전 테스트 표시가 남지 않도록 닫는다.
            AssignReferencesIfMissing();
            AssignWorldSpaceCanvasEventCamera();
        }

        /// <summary>활성화 시 버튼 클릭 이벤트를 연결한다.</summary>
        private void OnEnable()
        {
            // 재활성화마다 중복 구독을 제거한 뒤 다시 연결한다.
            AssignReferencesIfMissing();
            AssignWorldSpaceCanvasEventCamera();

            if (actionButton != null)
            {
                actionButton.onClick.RemoveListener(HandleActionButtonClicked);
                actionButton.onClick.AddListener(HandleActionButtonClicked);
            }
        }

        /// <summary>비활성화 시 버튼 클릭 이벤트를 해제한다.</summary>
        private void OnDisable()
        {
            // 비활성 상태에서 클릭 이벤트가 남지 않도록 정리한다.
            if (actionButton != null)
                actionButton.onClick.RemoveListener(HandleActionButtonClicked);
        }

        /// <summary>파괴 시 외부 이벤트 참조를 정리한다.</summary>
        private void OnDestroy()
        {
            // 씬 종료 또는 오브젝트 제거 시 이벤트 참조를 끊는다.
            if (actionButton != null)
                actionButton.onClick.RemoveListener(HandleActionButtonClicked);

            SellRequested = null;
            BuyRequested = null;
        }

        /// <summary>고정 배치된 현재 위치에서 Sell 버튼 내용을 갱신하고 표시한다.</summary>
        public void ShowSellStatic(int slotIndex, SlotPairDeviceDefinitionSO device, int refundMoney)
        {
            // 빈 Device에는 Sell 버튼을 표시하지 않는다.
            if (device == null)
            {
                Hide();
                return;
            }

            ShowInternal(slotIndex, ActionKind.Sell, refundMoney, $"SELL\n${refundMoney}", sellColor);
        }

        /// <summary>고정 배치된 현재 위치에서 Buy 버튼 내용을 갱신하고 표시한다.</summary>
        public void ShowBuyStatic(int slotIndex, int price)
        {
            // Buy 버튼 내용을 공용 표시 로직으로 전달한다.
            ShowInternal(slotIndex, ActionKind.Buy, price, $"BUY\n${price}", buyColor);
        }

        /// <summary>버튼을 숨기고 현재 대상 상태를 초기화한다.</summary>
        public void Hide()
        {
            // 다음 클릭이 이전 슬롯으로 전달되지 않도록 내부 상태를 초기화한다.
            selectedSlotIndex = -1;
            currentMoneyValue = -1;
            isHandlingClick = false;
            gameObject.SetActive(false);
        }

        /// <summary>Sell/Buy 버튼을 공통 방식으로 표시한다.</summary>
        private void ShowInternal(
            int slotIndex,
            ActionKind actionKind,
            int moneyValue,
            string label,
            Color backgroundColor)
        {
            // 비활성 상태에서 최초 Show가 호출될 수 있으므로 먼저 참조를 보정한다.
            AssignReferencesIfMissing();
            AssignWorldSpaceCanvasEventCamera();

            // 고정 Anchor 방식이므로 위치는 건드리지 않고 내용과 색상만 바꾼다.
            selectedSlotIndex = slotIndex;
            currentActionKind = actionKind;
            currentMoneyValue = moneyValue;
            isHandlingClick = false;

            SetLabel(label);
            SetBackgroundColor(backgroundColor);

            // 모든 표시 데이터를 채운 뒤 마지막에 오브젝트를 활성화한다.
            gameObject.SetActive(true);

            Log($"[Show] Slot={slotIndex}, Action={actionKind}, Money={moneyValue}");
        }

        /// <summary>버튼 라벨 텍스트를 변경한다.</summary>
        private void SetLabel(string text)
        {
            // TMP 참조가 없으면 텍스트 표시만 생략한다.
            if (labelText == null)
                return;

            labelText.text = text ?? string.Empty;
        }

        /// <summary>버튼 배경색을 변경한다.</summary>
        private void SetBackgroundColor(Color color)
        {
            // Image 참조가 없으면 배경색 변경만 생략한다.
            if (backgroundImage == null)
                return;

            backgroundImage.color = color;
        }

        /// <summary>Unity Button 클릭을 UniTask 기반 처리로 전달한다.</summary>
        private void HandleActionButtonClicked()
        {
            // Button 이벤트는 동기 콜백이므로 UniTaskVoid로 위임한다.
            HandleActionButtonClickedAsync().Forget();
        }

        /// <summary>현재 버튼 클릭을 Sell 또는 Buy 요청 이벤트로 변환한다.</summary>
        private async UniTaskVoid HandleActionButtonClickedAsync()
        {
            // UI 상태 변경과 이벤트 발행을 MainThread에서 처리한다.
            await UniTask.SwitchToMainThread();

            if (isHandlingClick)
                return;

            if (selectedSlotIndex < 0)
                return;

            isHandlingClick = true;

            int slotIndex = selectedSlotIndex;
            ActionKind actionKind = currentActionKind;

            // 액션 수행 후 슬롯 상태가 바뀌므로 stale target 방지를 위해 먼저 숨긴다.
            Hide();

            if (actionKind == ActionKind.Sell)
                SellRequested?.Invoke(slotIndex);
            else if (actionKind == ActionKind.Buy)
                BuyRequested?.Invoke(slotIndex);
        }

        /// <summary>누락된 UI 참조를 런타임에서 보정한다.</summary>
        private void AssignReferencesIfMissing()
        {
            // Prefab unpack 또는 씬 수동 배치 중 빠진 참조를 보정한다.
            if (actionButton == null)
                actionButton = GetComponentInChildren<Button>(true);

            if (labelText == null)
                labelText = GetComponentInChildren<TMP_Text>(true);

            if (backgroundImage == null)
                backgroundImage = GetComponentInChildren<Image>(true);

            if (ownerCanvas == null)
                ownerCanvas = GetComponentInParent<Canvas>(true);
        }

        /// <summary>World Space Canvas의 Event Camera를 자동 할당한다.</summary>
        private void AssignWorldSpaceCanvasEventCamera()
        {
            // Prefab에는 씬 카메라를 직접 물릴 수 없으므로 런타임에서 보정한다.
            AssignReferencesIfMissing();

            if (ownerCanvas == null)
                return;

            if (ownerCanvas.renderMode != RenderMode.WorldSpace)
                return;

            if (ownerCanvas.worldCamera != null)
                return;

            ownerCanvas.worldCamera = Camera.main;
        }

        /// <summary>ActionButton 디버그 로그를 출력한다.</summary>
        private void Log(string message)
        {
            // 필요할 때만 버튼 상태를 추적한다.
            if (!enableDebugLog)
                return;

            Debug.Log($"[Tessera][EquippedDeviceActionButton]{message}", this);
        }
    }
}

