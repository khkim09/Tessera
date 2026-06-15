using System;
using Tessera.Data;
using Tessera.Presentation;
using UnityEngine;

namespace Tessera.UI
{
    /// <summary>테이블 위 3D Device 슬롯 하나의 표시 상태를 관리한다.</summary>
    public class DeviceSlot3DView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform equippedDeviceRoot;
        [SerializeField] private Transform equippedDeviceViewAnchor;
        [SerializeField] private GameObject fallbackEquippedDeviceViewPrefab;
        [SerializeField] private TesseraHoverHighlightTarget hoverHighlightTarget;

        [Header("Debug")]
        [SerializeField] private bool enableInputDebugLog = true;

        private SlotPairDeviceDefinitionSO currentDevice;
        private int slotIndex = -1;
        private bool interactionEnabled = true;
        private GameObject equippedDeviceViewObject;
        private EquippedDevice3DView equippedDeviceView;

        public int SlotIndex => slotIndex;
        public SlotPairDeviceDefinitionSO CurrentDevice => currentDevice;

        public event Action<int> DragStarted;
        public event Action<int> DragEnded;
        public event Action<int> Dropped;

        public event Action<int> HoverEntered;
        public event Action<int> HoverExited;

        /// <summary>컴포넌트가 추가될 때 기본 참조를 자동 보정한다.</summary>
        private void Reset()
        {
            hoverHighlightTarget = GetComponent<TesseraHoverHighlightTarget>();
        }

        /// <summary>런타임 Material을 준비한다.</summary>
        private void Awake()
        {
            // 자식 Collider가 EventSystem 이벤트를 부모 슬롯으로 전달하도록 Relay를 등록한다.
            RegisterClickRelays();
            RefreshHoverFeedbackEnabled();
        }

        /// <summary>활성화 시 Collider Relay와 버튼 이벤트를 연결한다.</summary>
        private void OnEnable()
        {
            // Pooling/재활성화 상황에서도 참조가 살아 있도록 보정한다.
            RegisterClickRelays();
        }

        /// <summary>비활성화 시 버튼 이벤트와 Relay owner 참조를 해제한다.</summary>
        private void OnDisable()
        {
            UnregisterClickRelays();
        }

        /// <summary>파괴 시 외부 이벤트 참조를 정리한다.</summary>
        private void OnDestroy()
        {
            UnregisterClickRelays();
            ClearEquippedDeviceView();

            DragStarted = null;
            DragEnded = null;
            Dropped = null;
            HoverEntered = null;
            HoverExited = null;
        }

        /// <summary>슬롯 인덱스를 초기화한다.</summary>
        public void Initialize(int slotIndex)
        {
            this.slotIndex = slotIndex;
            LogInput($"[Initialize] Slot={name}, SlotIndex={slotIndex}");
        }

        /// <summary>슬롯 Hover/Drag/Drop 상호작용 허용 여부를 설정한다.</summary>
        public void SetInteractionEnabled(bool enabled)
        {
            interactionEnabled = enabled;

            if (!interactionEnabled && hoverHighlightTarget != null)
                hoverHighlightTarget.ResetHighlight();

            RefreshHoverFeedbackEnabled();
        }

        /// <summary>지정한 Collider가 이 슬롯 또는 자식에 속하는지 확인한다.</summary>
        public bool ContainsCollider(Collider targetCollider)
        {
            if (targetCollider == null)
                return false;

            return targetCollider.transform == transform || targetCollider.transform.IsChildOf(transform);
        }

        /// <summary>외부 Collider Relay에서 슬롯 드래그 시작을 전달받는다.</summary>
        public void NotifySlotDragStarted()
        {
            LogInput($"[DragStarted] Slot={name}, SlotIndex={slotIndex}");

            if (!interactionEnabled) return;
            if (slotIndex < 0) return;

            DragStarted?.Invoke(slotIndex);
        }

        /// <summary>외부 Collider Relay에서 슬롯 드래그 종료를 전달받는다.</summary>
        public void NotifySlotDragEnded()
        {
            LogInput($"[DragEnded] Slot={name}, SlotIndex={slotIndex}");

            if (!interactionEnabled) return;
            if (slotIndex < 0) return;

            DragEnded?.Invoke(slotIndex);
        }

        /// <summary>외부 Collider Relay에서 슬롯 Drop 대상을 전달받는다.</summary>
        public void NotifySlotDropped()
        {
            LogInput($"[Dropped] Slot={name}, SlotIndex={slotIndex}");

            if (!interactionEnabled) return;
            if (slotIndex < 0) return;

            Dropped?.Invoke(slotIndex);
        }

        /// <summary>외부 Collider Relay에서 슬롯 Hover 진입을 전달받는다.</summary>
        public void NotifySlotHoverEntered()
        {
            LogInput($"[HoverEntered] Slot={name}, SlotIndex={slotIndex}");

            if (!interactionEnabled) return;
            if (slotIndex < 0) return;

            HoverEntered?.Invoke(slotIndex);
        }

        /// <summary>외부 Collider Relay에서 슬롯 Hover 이탈을 전달받는다.</summary>
        public void NotifySlotHoverExited()
        {
            LogInput($"[HoverExited] Slot={name}, SlotIndex={slotIndex}");

            if (!interactionEnabled) return;
            if (slotIndex < 0) return;

            HoverExited?.Invoke(slotIndex);
        }

        /// <summary>지정한 Device 데이터를 3D 슬롯에 반영한다.</summary>
        public void SetDevice(SlotPairDeviceDefinitionSO device)
        {
            currentDevice = device;

            if (device == null)
            {
                SetEmpty();
                return;
            }

            SetEquipped(device);
        }

        /// <summary>계산 또는 선택 강조 상태를 표시한다.</summary>
        public void SetHighlighted(bool isHighlighted)
        {
            if (equippedDeviceView == null)
                return;

            equippedDeviceView.SetHighlighted(isHighlighted);
        }

        /// <summary>빈 슬롯 상태로 표시한다.</summary>
        private void SetEmpty()
        {
            ClearEquippedDeviceView();

            if (equippedDeviceRoot != null)
                equippedDeviceRoot.gameObject.SetActive(false);

            RefreshHoverFeedbackEnabled();
        }

        /// <summary>장착된 Device 상태로 표시한다.</summary>
        private void SetEquipped(SlotPairDeviceDefinitionSO device)
        {
            if (equippedDeviceRoot != null)
                equippedDeviceRoot.gameObject.SetActive(true);

            RebuildEquippedDeviceView(device);
            RefreshHoverFeedbackEnabled();
        }

        /// <summary>장착 Device SO에 맞는 View 프리팹을 생성한다.</summary>
        private void RebuildEquippedDeviceView(SlotPairDeviceDefinitionSO device)
        {
            ClearEquippedDeviceView();

            Transform parent = equippedDeviceViewAnchor != null ? equippedDeviceViewAnchor : equippedDeviceRoot;

            if (parent == null)
                return;

            GameObject prefab = device != null && device.EquippedViewPrefab != null
                ? device.EquippedViewPrefab
                : fallbackEquippedDeviceViewPrefab;

            if (prefab == null)
                return;

            equippedDeviceViewObject = Instantiate(prefab, parent);
            equippedDeviceViewObject.transform.localPosition = Vector3.zero;
            equippedDeviceViewObject.transform.localRotation = Quaternion.identity;
            equippedDeviceViewObject.transform.localScale = Vector3.one;

            equippedDeviceView = equippedDeviceViewObject.GetComponent<EquippedDevice3DView>();

            if (equippedDeviceView != null)
                equippedDeviceView.Bind(device);
        }

        /// <summary>현재 생성된 장착 Device View 오브젝트를 제거한다.</summary>
        private void ClearEquippedDeviceView()
        {
            equippedDeviceView = null;

            if (equippedDeviceViewObject == null)
                return;

            Destroy(equippedDeviceViewObject);
            equippedDeviceViewObject = null;
        }

        /// <summary>현재 장착 상태와 상호작용 허용 여부 기준으로 Hover Scale 허용 여부를 갱신한다.</summary>
        private void RefreshHoverFeedbackEnabled()
        {
            if (hoverHighlightTarget == null)
                return;

            bool hasDevice = currentDevice != null;
            hoverHighlightTarget.SetHoverFeedbackEnabled(interactionEnabled && hasDevice);
        }

        /// <summary>자식 Collider에 Pointer Relay를 자동 등록한다.</summary>
        private void RegisterClickRelays()
        {
            Collider[] childColliders = GetComponentsInChildren<Collider>(true);

            for (int i = 0; i < childColliders.Length; i++)
            {
                if (childColliders[i] == null)
                    continue;

                DeviceSlotClickRelay3D relay = childColliders[i].GetComponent<DeviceSlotClickRelay3D>();

                if (relay == null)
                    relay = childColliders[i].gameObject.AddComponent<DeviceSlotClickRelay3D>();

                relay.Bind(this);
            }
        }

        /// <summary>자식 Collider Relay의 owner 참조를 해제한다.</summary>
        private void UnregisterClickRelays()
        {
            DeviceSlotClickRelay3D[] relays = GetComponentsInChildren<DeviceSlotClickRelay3D>(true);

            for (int i = 0; i < relays.Length; i++)
            {
                if (relays[i] == null)
                    continue;

                relays[i].Unbind(this);
            }
        }

        /// <summary>DeviceSlot 입력 디버그 로그를 출력한다.</summary>
        private void LogInput(string message)
        {
            if (!enableInputDebugLog)
                return;

            Debug.Log($"[Tessera][DeviceSlotInput]{message}");
        }
    }
}
