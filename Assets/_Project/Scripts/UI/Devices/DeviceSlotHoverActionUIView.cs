using System;
using Tessera.Data;
using UnityEngine;

namespace Tessera.UI
{
    /// <summary>하나의 DeviceSlot에 고정 배치된 Tooltip과 ActionButton 묶음 View다.</summary>
    public sealed class DeviceSlotHoverActionUIView : MonoBehaviour
    {
        /// <summary>이 Hover UI가 연결된 DeviceSlot 인덱스다.</summary>
        [Header("Slot")]
        [SerializeField] private int slotIndex;

        /// <summary>장착 Device 설명을 표시할 Tooltip View다.</summary>
        [Header("Views")]
        [SerializeField] private ShopProductTooltipView tooltipView;

        /// <summary>Sell/Buy 버튼을 표시할 ActionButton View다.</summary>
        [SerializeField] private EquippedDeviceActionButton3DView actionButtonView;

        /// <summary>Hover UI 디버그 로그 출력 여부다.</summary>
        [Header("Debug")]
        [SerializeField] private bool enableDebugLog;

        /// <summary>ActionButton Hover 영역 위에 포인터가 올라와 있는지 여부다.</summary>
        private bool isPointerOverActionArea;

        /// <summary>이 Hover UI가 연결된 DeviceSlot 인덱스를 반환한다.</summary>
        public int SlotIndex => slotIndex;

        /// <summary>ActionButton Hover 영역 위에 포인터가 있는지 반환한다.</summary>
        public bool IsPointerOverActionArea => isPointerOverActionArea;

        /// <summary>Sell 버튼 클릭 요청 이벤트다.</summary>
        public event Action<int> SellRequested;

        /// <summary>Hover 확장 영역 이탈 이벤트다.</summary>
        public event Action<int> HoverAreaExited;

        /// <summary>컴포넌트 추가 시 기본 참조를 자동 연결한다.</summary>
        private void Reset()
        {
            // 비활성 자식까지 포함해 Tooltip/Button View를 자동 수집한다.
            AssignReferencesIfMissing();
        }

        /// <summary>초기화 시 참조와 Pointer Relay를 보정하고 숨김 상태로 시작한다.</summary>
        private void Awake()
        {
            // 고정 Anchor Root는 켜진 상태로 두고 내부 View만 숨긴다.
            AssignReferencesIfMissing();
            BindPointerRelays();
            Hide();
        }

        /// <summary>활성화 시 버튼 이벤트와 Pointer Relay를 연결한다.</summary>
        private void OnEnable()
        {
            // 재활성화 상황에서도 이벤트 연결을 보장한다.
            AssignReferencesIfMissing();
            BindPointerRelays();

            if (actionButtonView != null)
            {
                actionButtonView.SellRequested -= HandleSellRequested;
                actionButtonView.SellRequested += HandleSellRequested;
            }
        }

        /// <summary>비활성화 시 버튼 이벤트와 Pointer Relay를 해제한다.</summary>
        private void OnDisable()
        {
            // 비활성화 상태에서 외부 이벤트가 남지 않도록 정리한다.
            if (actionButtonView != null)
                actionButtonView.SellRequested -= HandleSellRequested;

            UnbindPointerRelays();
        }

        /// <summary>파괴 시 외부 이벤트 참조를 정리한다.</summary>
        private void OnDestroy()
        {
            // 씬 종료 또는 오브젝트 제거 시 이벤트 참조를 끊는다.
            SellRequested = null;
            HoverAreaExited = null;
        }

        /// <summary>런타임에서 슬롯 인덱스를 초기화한다.</summary>
        public void Initialize(int newSlotIndex)
        {
            // StageFlowUIBridge 배열 순서와 View 내부 인덱스를 동기화한다.
            slotIndex = newSlotIndex;
        }

        /// <summary>장착 Device 기준으로 Tooltip과 Sell 버튼을 표시한다.</summary>
        public void ShowSell(SlotPairDeviceDefinitionSO device, int refundMoney)
        {
            // 빈 슬롯이면 Hover UI를 표시하지 않는다.
            if (device == null)
            {
                Hide();
                return;
            }

            AssignReferencesIfMissing();

            if (tooltipView != null)
                tooltipView.ShowDeviceStatic(device);

            if (actionButtonView != null)
                actionButtonView.ShowSellStatic(slotIndex, device, refundMoney);

            Log($"[ShowSell] Slot={slotIndex}, Device={device.DisplayName}, Refund={refundMoney}");
        }

        /// <summary>Tooltip과 ActionButton을 모두 숨긴다.</summary>
        public void Hide()
        {
            // Hover 확장 영역 상태를 초기화하고 내부 View만 닫는다.
            isPointerOverActionArea = false;

            if (tooltipView != null)
                tooltipView.Hide();

            if (actionButtonView != null)
                actionButtonView.Hide();

            Log($"[Hide] Slot={slotIndex}");
        }

        /// <summary>ActionButton Hover 영역에 포인터가 진입했음을 기록한다.</summary>
        public void NotifyPointerEntered()
        {
            // DeviceSlot 밖으로 이동해도 버튼 위에 있으면 UI를 유지하기 위한 상태다.
            isPointerOverActionArea = true;
            Log($"[PointerEntered] Slot={slotIndex}");
        }

        /// <summary>ActionButton Hover 영역에서 포인터가 이탈했음을 기록한다.</summary>
        public void NotifyPointerExited()
        {
            // 버튼에서 마우스가 나가면 Bridge가 최종 Hide 여부를 판단한다.
            isPointerOverActionArea = false;
            Log($"[PointerExited] Slot={slotIndex}");
            HoverAreaExited?.Invoke(slotIndex);
        }

        /// <summary>ActionButton Sell 요청을 Hover UI 단위 이벤트로 전달한다.</summary>
        private void HandleSellRequested(int requestedSlotIndex)
        {
            // 버튼 내부 슬롯 인덱스를 그대로 Bridge로 전달한다.
            Log($"[SellRequested] Slot={requestedSlotIndex}");
            SellRequested?.Invoke(requestedSlotIndex);
        }

        /// <summary>누락된 View 참조를 런타임에서 보정한다.</summary>
        private void AssignReferencesIfMissing()
        {
            // Slot별 UI Root 하위의 비활성 View도 찾을 수 있게 true를 사용한다.
            if (tooltipView == null)
                tooltipView = GetComponentInChildren<ShopProductTooltipView>(true);

            if (actionButtonView == null)
                actionButtonView = GetComponentInChildren<EquippedDeviceActionButton3DView>(true);
        }

        /// <summary>ActionButton에 Pointer Relay를 연결한다.</summary>
        private void BindPointerRelays()
        {
            // Button GameObject에 Relay를 붙여 Hover 유지 영역을 확장한다.
            if (actionButtonView == null)
                return;

            DeviceHoverActionPointerRelay relay =
                actionButtonView.GetComponent<DeviceHoverActionPointerRelay>();

            if (relay == null)
                relay = actionButtonView.gameObject.AddComponent<DeviceHoverActionPointerRelay>();

            relay.Bind(this);
        }

        /// <summary>ActionButton Pointer Relay 연결을 해제한다.</summary>
        private void UnbindPointerRelays()
        {
            // 현재 Hover UI를 owner로 가진 Relay만 해제한다.
            if (actionButtonView == null)
                return;

            DeviceHoverActionPointerRelay relay =
                actionButtonView.GetComponent<DeviceHoverActionPointerRelay>();

            if (relay == null)
                return;

            relay.Unbind(this);
        }

        /// <summary>Hover UI 디버그 로그를 출력한다.</summary>
        private void Log(string message)
        {
            // 필요할 때만 Hover UI 상태를 추적한다.
            if (!enableDebugLog)
                return;

            Debug.Log($"[Tessera][DeviceSlotHoverUI]{message}", this);
        }
    }
}
