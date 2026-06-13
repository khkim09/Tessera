using System;
using Tessera.Data;
using TMPro;
using UnityEngine;

namespace Tessera.UI
{
    /// <summary>테이블 위 3D Device 슬롯 하나의 표시 상태를 관리한다.</summary>
    public class DeviceSlot3DView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MeshRenderer slotRenderer;
        [SerializeField] private TMP_Text displayNameText;
        [SerializeField] private TMP_Text indexText;

        [Header("Colors")]
        [SerializeField] private Color emptyColor = new Color(0.42f, 0.46f, 0.50f, 1f);
        [SerializeField] private Color equippedColor = new Color(0.18f, 0.25f, 0.42f, 1f);
        [SerializeField] private Color highlightedColor = new Color(0.85f, 0.72f, 0.25f, 1f);

        [Header("Debug")]
        [SerializeField] private bool enableInputDebugLog = true;

        private SlotPairDeviceDefinitionSO currentDevice;
        private Material runtimeMaterial;
        private int slotIndex = -1;

        /// <summary>현재 슬롯 인덱스를 반환한다.</summary>
        public int SlotIndex => slotIndex;

        /// <summary>현재 표시 중인 Device를 반환한다.</summary>
        public SlotPairDeviceDefinitionSO CurrentDevice => currentDevice;

        public event Action<int> Clicked;
        public event Action<int> DragStarted;
        public event Action<int> DragEnded;
        public event Action<int> Dropped;

        public event Action<int> HoverEntered;
        public event Action<int> HoverExited;

        /// <summary>컴포넌트가 추가될 때 기본 참조를 자동 보정한다.</summary>
        private void Reset()
        {
            // 같은 오브젝트의 MeshRenderer를 기본 슬롯 렌더러로 사용한다.
            slotRenderer = GetComponent<MeshRenderer>();
        }

        /// <summary>런타임 Material을 준비한다.</summary>
        private void Awake()
        {
            // 원본 Material을 오염시키지 않도록 인스턴스를 사용한다.
            EnsureRuntimeMaterial();

            // 자식 Collider가 EventSystem 이벤트를 부모 슬롯으로 전달하도록 Relay를 등록한다.
            RegisterClickRelays();
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

            Clicked = null;
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

            // 슬롯 번호는 디버그 단계에서만 작게 표시한다.
            SetText(indexText, (slotIndex + 1).ToString());
        }

        /// <summary>지정한 Collider가 이 슬롯 또는 자식에 속하는지 확인한다.</summary>
        public bool ContainsCollider(Collider targetCollider)
        {
            if (targetCollider == null)
                return false;

            return targetCollider.transform == transform || targetCollider.transform.IsChildOf(transform);
        }

        #region Event Nofifiers

        /// <summary>외부 Collider Relay에서 슬롯 클릭을 전달받는다.</summary>
        public void NotifySlotClicked()
        {
            LogInput($"[Clicked] Slot={name}, SlotIndex={slotIndex}");

            if (slotIndex < 0)
                return;

            Clicked?.Invoke(slotIndex);
        }

        /// <summary>외부 Collider Relay에서 슬롯 드래그 시작을 전달받는다.</summary>
        public void NotifySlotDragStarted()
        {
            LogInput($"[DragStarted] Slot={name}, SlotIndex={slotIndex}");

            if (slotIndex < 0)
                return;

            DragStarted?.Invoke(slotIndex);
        }

        /// <summary>외부 Collider Relay에서 슬롯 드래그 종료를 전달받는다.</summary>
        public void NotifySlotDragEnded()
        {
            LogInput($"[DragEnded] Slot={name}, SlotIndex={slotIndex}");

            if (slotIndex < 0)
                return;

            DragEnded?.Invoke(slotIndex);
        }

        /// <summary>외부 Collider Relay에서 슬롯 Drop 대상을 전달받는다.</summary>
        public void NotifySlotDropped()
        {
            LogInput($"[Dropped] Slot={name}, SlotIndex={slotIndex}");

            if (slotIndex < 0)
                return;

            Dropped?.Invoke(slotIndex);
        }

        /// <summary>외부 Collider Relay에서 슬롯 Hover 진입을 전달받는다.</summary>
        public void NotifySlotHoverEntered()
        {
            // Hover 시작을 슬롯 인덱스 기준으로 외부에 전달한다.
            LogInput($"[HoverEntered] Slot={name}, SlotIndex={slotIndex}");

            if (slotIndex < 0)
                return;

            HoverEntered?.Invoke(slotIndex);
        }

        /// <summary>외부 Collider Relay에서 슬롯 Hover 이탈을 전달받는다.</summary>
        public void NotifySlotHoverExited()
        {
            // Hover 종료를 슬롯 인덱스 기준으로 외부에 전달한다.
            LogInput($"[HoverExited] Slot={name}, SlotIndex={slotIndex}");

            if (slotIndex < 0)
                return;

            HoverExited?.Invoke(slotIndex);
        }

        #endregion

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

        /// <summary>빈 슬롯 상태로 표시한다.</summary>
        private void SetEmpty()
        {
            // 슬롯 틀은 유지하고, 장착 Device 표시 Root만 숨긴다.
            SetSlotColor(emptyColor);
            SetText(displayNameText, string.Empty);
        }

        /// <summary>장착된 Device 상태로 표시한다.</summary>
        private void SetEquipped(SlotPairDeviceDefinitionSO device)
        {
            // 슬롯 틀은 유지하고, 장착 Device 앞면만 표시한다.
            SetSlotColor(equippedColor);
            SetText(displayNameText, device != null ? device.DisplayName : string.Empty);
        }

        #region Material

        /// <summary>계산 또는 선택 강조 상태를 표시한다.</summary>
        public void SetHighlighted(bool isHighlighted)
        {
            // 계산 중인 SlotPair를 임시 색상으로 강조한다.
            SetSlotColor(isHighlighted ? highlightedColor : GetBaseColor());
        }

        /// <summary>현재 상태 기준 기본 색상을 반환한다.</summary>
        private Color GetBaseColor()
        {
            return currentDevice == null ? emptyColor : equippedColor;
        }

        /// <summary>슬롯 렌더러 색상을 변경한다.</summary>
        private void SetSlotColor(Color color)
        {
            if (slotRenderer == null)
                return;

            EnsureRuntimeMaterial();

            if (runtimeMaterial == null)
                return;

            runtimeMaterial.color = color;
        }

        /// <summary>런타임 전용 Material 인스턴스를 준비한다.</summary>
        private void EnsureRuntimeMaterial()
        {
            if (slotRenderer == null)
                return;

            if (runtimeMaterial != null)
                return;

            // sharedMaterial 대신 material을 통해 슬롯별 색상 변경을 독립시킨다.
            runtimeMaterial = slotRenderer.material;
        }

        #endregion

        /// <summary>TMP 텍스트를 안전하게 갱신한다.</summary>
        private static void SetText(TMP_Text targetText, string value)
        {
            if (targetText == null)
                return;

            targetText.text = value;
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

                // Collider 이벤트를 이 슬롯 View로 전달한다.
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

        #region Debug

        /// <summary>DeviceSlot 입력 디버그 로그를 출력한다.</summary>
        private void LogInput(string message)
        {
            if (!enableInputDebugLog)
                return;

            Debug.Log($"[Tessera][DeviceSlotInput]{message}");
        }

        /// <summary>DeviceSlot 입력 디버그 경고 로그를 출력한다.</summary>
        private void LogInputWarning(string message)
        {
            if (!enableInputDebugLog)
                return;

            Debug.LogWarning($"[Tessera][DeviceSlotInput]{message}");
        }

        #endregion
    }
}
