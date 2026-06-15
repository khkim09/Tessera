using System;
using Tessera.Data;
using UnityEngine;

namespace Tessera.UI
{
    /// <summary>테이블 위 3D Device 슬롯 5개의 표시를 관리한다.</summary>
    public class DeviceRack3DView : MonoBehaviour
    {
        [Header("Slots")]
        [SerializeField] private DeviceSlot3DView[] slots = new DeviceSlot3DView[5];

        [Header("Debug")]
        [SerializeField] private bool enableInputDebugLog = true;

        /// <summary>슬롯 개수를 반환한다.</summary>
        public int SlotCount => slots != null ? slots.Length : 0;

        public event Action<int> SlotDragStarted;
        public event Action<int> SlotDragEnded;
        public event Action<int> SlotDropped;

        public event Action<int> SlotHoverEntered;
        public event Action<int> SlotHoverExited;

        /// <summary>인스펙터에서 슬롯을 자동 수집한다.</summary>
        private void Reset()
        {
            // 자식에 붙은 DeviceSlot3DView를 자동으로 수집한다.
            slots = GetComponentsInChildren<DeviceSlot3DView>(true);
        }

        /// <summary>슬롯 인덱스를 초기화한다.</summary>
        private void Awake()
        {
            // 슬롯 순서와 이름이 맞는지 초기화한다.
            InitializeSlots();
        }

        /// <summary>슬롯 클릭 이벤트를 구독한다.</summary>
        private void OnEnable()
        {
            LogInput($"[OnEnable] Rack={name}, SlotCount={SlotCount}");

            // 각 슬롯의 Pointer 이벤트를 Rack 단위 이벤트로 중계한다.
            SubscribeSlotClickEvents();
        }

        /// <summary>슬롯 클릭 이벤트를 해제한다.</summary>
        private void OnDisable()
        {
            UnsubscribeSlotClickEvents();
        }

        /// <summary>슬롯 배열의 각 슬롯에 인덱스를 부여한다.</summary>
        public void InitializeSlots()
        {
            if (slots == null)
                return;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                    continue;

                slots[i].Initialize(i);
            }
        }

        /// <summary>배열 기준으로 3D Device 슬롯 표시를 갱신한다.</summary>
        public void SetDevices(SlotPairDeviceDefinitionSO[] devices)
        {
            if (slots == null)
                return;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                    continue;

                SlotPairDeviceDefinitionSO device = null;

                if (devices != null && i >= 0 && i < devices.Length)
                    device = devices[i];

                // Presenter의 slotPairDevices 배열을 그대로 반영한다.
                slots[i].SetDevice(device);
            }
        }

        #region Slot Events

        /// <summary>자식 DeviceSlot 클릭 이벤트를 구독한다.</summary>
        private void SubscribeSlotClickEvents()
        {
            if (slots == null)
                return;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                    continue;

                slots[i].DragStarted -= HandleSlotDragStarted;
                slots[i].DragStarted += HandleSlotDragStarted;

                slots[i].DragEnded -= HandleSlotDragEnded;
                slots[i].DragEnded += HandleSlotDragEnded;

                slots[i].Dropped -= HandleSlotDropped;
                slots[i].Dropped += HandleSlotDropped;

                slots[i].HoverEntered -= HandleSlotHoverEntered;
                slots[i].HoverEntered += HandleSlotHoverEntered;

                slots[i].HoverExited -= HandleSlotHoverExited;
                slots[i].HoverExited += HandleSlotHoverExited;

                LogInput(
                    $"[Subscribe] Rack={name}, ArrayIndex={i}, " +
                    $"Slot={slots[i].name}, SlotIndex={slots[i].SlotIndex}");
            }
        }

        /// <summary>자식 DeviceSlot 클릭 이벤트 구독을 해제한다.</summary>
        private void UnsubscribeSlotClickEvents()
        {
            if (slots == null)
                return;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                    continue;

                slots[i].DragStarted -= HandleSlotDragStarted;
                slots[i].DragEnded -= HandleSlotDragEnded;
                slots[i].Dropped -= HandleSlotDropped;

                slots[i].HoverEntered -= HandleSlotHoverEntered;
                slots[i].HoverExited -= HandleSlotHoverExited;
            }
        }

        /// <summary>자식 슬롯 BeginDrag를 Rack 단위 이벤트로 전달한다.</summary>
        private void HandleSlotDragStarted(int slotIndex)
        {
            LogInput($"[DragStarted] Rack={name}, Slot={slotIndex}");

            SlotDragStarted?.Invoke(slotIndex);
        }

        /// <summary>자식 슬롯 EndDrag를 Rack 단위 이벤트로 전달한다.</summary>
        private void HandleSlotDragEnded(int slotIndex)
        {
            LogInput($"[DragEnded] Rack={name}, Slot={slotIndex}");

            SlotDragEnded?.Invoke(slotIndex);
        }

        /// <summary>자식 슬롯 Drop을 Rack 단위 이벤트로 전달한다.</summary>
        private void HandleSlotDropped(int slotIndex)
        {
            LogInput($"[Dropped] Rack={name}, Slot={slotIndex}");

            SlotDropped?.Invoke(slotIndex);
        }

        /// <summary>자식 슬롯 Hover 진입을 Rack 단위 이벤트로 전달한다.</summary>
        private void HandleSlotHoverEntered(int slotIndex)
        {
            // DeviceSlot 단위 hover를 Rack 외부에서 처리할 수 있게 중계한다.
            LogInput($"[HoverEntered] Rack={name}, Slot={slotIndex}");

            SlotHoverEntered?.Invoke(slotIndex);
        }

        /// <summary>자식 슬롯 Hover 이탈을 Rack 단위 이벤트로 전달한다.</summary>
        private void HandleSlotHoverExited(int slotIndex)
        {
            // DeviceSlot 단위 hover 종료를 Rack 외부에서 처리할 수 있게 중계한다.
            LogInput($"[HoverExited] Rack={name}, Slot={slotIndex}");

            SlotHoverExited?.Invoke(slotIndex);
        }

        #endregion

        /// <summary>화면 좌표 기준으로 Rack 내부 슬롯 인덱스를 반환한다.</summary>
        public bool TryFindSlotIndexUnderScreenPoint(Vector2 screenPoint, Camera targetCamera, out int slotIndex)
        {
            slotIndex = -1;

            Camera cameraToUse = targetCamera != null ? targetCamera : Camera.main;

            if (cameraToUse == null)
                return false;

            Ray ray = cameraToUse.ScreenPointToRay(screenPoint);

            if (!Physics.Raycast(ray, out RaycastHit hit, 100f))
                return false;

            if (slots == null)
                return false;

            Collider hitCollider = hit.collider;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                    continue;

                if (slots[i].ContainsCollider(hitCollider))
                {
                    slotIndex = i;
                    return true;
                }
            }

            return false;
        }

        #region Helper

        /// <summary>지정 슬롯만 계산 강조 상태로 표시한다.</summary>
        public void HighlightSlot(int slotIndex)
        {
            if (slots == null)
                return;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                    continue;

                slots[i].SetHighlighted(i == slotIndex);
            }
        }

        /// <summary>모든 슬롯의 계산 강조 상태를 해제한다.</summary>
        public void ClearHighlight()
        {
            if (slots == null)
                return;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                    continue;

                slots[i].SetHighlighted(false);
            }
        }

        /// <summary>지정 슬롯의 Transform을 반환한다.</summary>
        public Transform GetSlotTransform(int slotIndex)
        {
            if (slots == null)
                return null;

            if (slotIndex < 0 || slotIndex >= slots.Length)
                return null;

            if (slots[slotIndex] == null)
                return null;

            return slots[slotIndex].transform;
        }

        /// <summary>지정 슬롯의 시각 중심 월드 좌표를 반환한다.</summary>
        public bool TryGetSlotVisualCenter(int slotIndex, out Vector3 center)
        {
            center = Vector3.zero;

            Transform slotTransform = GetSlotTransform(slotIndex);

            if (slotTransform == null)
                return false;

            Renderer slotRenderer = slotTransform.GetComponentInChildren<Renderer>(true);

            if (slotRenderer != null)
            {
                // 실제 화면에 보이는 Mesh bounds 중심을 기준점으로 사용한다.
                center = slotRenderer.bounds.center;
                return true;
            }

            Collider slotCollider = slotTransform.GetComponentInChildren<Collider>(true);

            if (slotCollider != null)
            {
                // Renderer가 없으면 Collider bounds 중심을 대체 기준점으로 사용한다.
                center = slotCollider.bounds.center;
                return true;
            }

            // 둘 다 없으면 Transform 위치를 fallback 기준점으로 사용한다.
            center = slotTransform.position;
            return true;
        }


        /// <summary>지정 슬롯의 Presentation 좌표계 기준 벡터를 반환한다.</summary>
        public bool TryGetSlotPresentationBasis(
            int slotIndex,
            Camera targetCamera,
            out Vector3 center,
            out Vector3 right,
            out Vector3 up,
            out Vector3 towardPlayer)
        {
            center = Vector3.zero;
            right = Vector3.right;
            up = Vector3.up;
            towardPlayer = Vector3.back;

            Transform slotTransform = GetSlotTransform(slotIndex);

            if (slotTransform == null)
                return false;

            if (!TryGetSlotVisualCenter(slotIndex, out center))
                return false;

            // 슬롯 Transform 기준의 로컬 축을 Presentation 좌표계의 기준으로 사용한다.
            up = SafeNormalized(slotTransform.up, Vector3.up);
            right = Vector3.ProjectOnPlane(slotTransform.right, up);
            right = SafeNormalized(right, slotTransform.right);

            towardPlayer = BuildTowardPlayerDirection(slotTransform, targetCamera, up);
            return true;
        }

        /// <summary>카메라 방향을 슬롯 평면에 투영해 플레이어 쪽 방향을 계산한다.</summary>
        private static Vector3 BuildTowardPlayerDirection(Transform slotTransform, Camera targetCamera, Vector3 slotUp)
        {
            if (targetCamera != null)
            {
                Vector3 projectedCameraForward = Vector3.ProjectOnPlane(targetCamera.transform.forward, slotUp);

                if (projectedCameraForward.sqrMagnitude > 0.0001f)
                {
                    // 카메라 forward의 반대 방향이 테이블 평면상 플레이어 쪽이다.
                    return -projectedCameraForward.normalized;
                }
            }

            Vector3 projectedSlotForward = Vector3.ProjectOnPlane(slotTransform.forward, slotUp);

            if (projectedSlotForward.sqrMagnitude > 0.0001f)
                return projectedSlotForward.normalized;

            Vector3 fallback = Vector3.ProjectOnPlane(Vector3.back, slotUp);
            return SafeNormalized(fallback, Vector3.back);
        }

        /// <summary>벡터가 너무 작으면 대체 벡터를 정규화해 반환한다.</summary>
        private static Vector3 SafeNormalized(Vector3 value, Vector3 fallback)
        {
            if (value.sqrMagnitude > 0.0001f)
                return value.normalized;

            if (fallback.sqrMagnitude > 0.0001f)
                return fallback.normalized;

            return Vector3.up;
        }

        /// <summary>지정 슬롯의 시각 중심 기준 Transform 회전을 반환한다.</summary>
        public Quaternion GetSlotRotation(int slotIndex)
        {
            Transform slotTransform = GetSlotTransform(slotIndex);

            if (slotTransform == null)
                return Quaternion.identity;

            return slotTransform.rotation;
        }

        /// <summary>지정 슬롯의 현재 Device를 반환한다.</summary>
        public SlotPairDeviceDefinitionSO GetDevice(int slotIndex)
        {
            if (slots == null)
                return null;

            if (slotIndex < 0 || slotIndex >= slots.Length)
                return null;

            if (slots[slotIndex] == null)
                return null;

            return slots[slotIndex].CurrentDevice;
        }

        #endregion

        #region Debug

        /// <summary>DeviceRack 입력 디버그 로그를 출력한다.</summary>
        private void LogInput(string message)
        {
            if (!enableInputDebugLog)
                return;

            Debug.Log($"[Tessera][DeviceRackInput]{message}");
        }

        #endregion
    }
}
