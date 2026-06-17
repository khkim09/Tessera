using System;
using Cysharp.Threading.Tasks;
using Tessera.Data;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tessera.UI
{
    /// <summary>테이블 위 3D Device 슬롯 5개의 표시를 관리한다.</summary>
    public class DeviceRack3DView : MonoBehaviour
    {
        [Header("Slots")]
        [SerializeField] private DeviceSlot3DView[] slots = new DeviceSlot3DView[5];

        [Header("Interaction")]
        [SerializeField] private bool interactionEnabled = true;

        [Header("Swap Preview")]
        [SerializeField] private Transform swapPreviewWorldRoot;
        [SerializeField] private float swapPreviewLiftDistance = 0.06f;
        [SerializeField] private float swapPreviewScaleMultiplier = 1.05f;

        [Header("Drop Confirm")]
        [SerializeField] private float dropConfirmSettleDuration = 0.1f;
        [SerializeField] private float dropConfirmSettleScaleMultiplier = 1.04f;

        [Header("Drag Snap")]
        [SerializeField] private float[] dragSnapSlotCenterLocalX = { -1.6f, -1.2f, -0.8f, -0.4f, 0f };
        [SerializeField] private float dragSnapOuterPadding = 0.2f;
        [SerializeField] private float dragSnapSwitchHysteresis = 0.006f;

        [Header("Invalid Drop Return")]
        [SerializeField] private float invalidDropReturnDuration = 0.14f;

        [Header("Debug")]
        [SerializeField] private bool enableInputDebugLog = true;

        private int activeDragSnapSlotIndex = -1; // Drag Snap 유지 슬롯
        private int currentDragPreviewSlotIndex = -1; // Drag Preview 대상 슬롯

        /// <summary>슬롯 개수를 반환한다.</summary>
        public int SlotCount => slots != null ? slots.Length : 0;

        public event Action<int> SlotDropped;

        public event Action<int> SlotHoverEntered;
        public event Action<int> SlotHoverExited;

        public event Action<int, PointerEventData> SlotDragStartedWithPointer;
        public event Action<int, PointerEventData> SlotDraggedWithPointer;
        public event Action<int, PointerEventData> SlotDragEndedWithPointer;

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
                slots[i].SetInteractionEnabled(interactionEnabled);
            }
        }

        /// <summary>Rack 하위 슬롯의 Hover/Drag/Drop 상호작용 허용 여부를 설정한다.</summary>
        public void SetInteractionEnabled(bool enabled)
        {
            interactionEnabled = enabled;

            if (slots == null)
                return;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                    continue;

                slots[i].SetInteractionEnabled(interactionEnabled);
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

        /// <summary>두 슬롯의 장착 View 오브젝트를 직접 교체하면서 Device 배열 상태를 반영한다.</summary>
        public void SetDevicesWithVisualSwap(SlotPairDeviceDefinitionSO[] devices, int sourceSlotIndex, int targetSlotIndex)
        {
            if (!IsValidSlotIndex(sourceSlotIndex) || !IsValidSlotIndex(targetSlotIndex))
            {
                SetDevices(devices);
                return;
            }

            if (sourceSlotIndex == targetSlotIndex)
            {
                SetDevices(devices);
                return;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                if (i == sourceSlotIndex || i == targetSlotIndex)
                    continue;

                if (slots[i] == null)
                    continue;

                slots[i].SetDevice(GetDeviceFromArray(devices, i));
            }

            GameObject sourceViewObject = slots[sourceSlotIndex].DetachEquippedDeviceViewObject();
            GameObject targetViewObject = slots[targetSlotIndex].DetachEquippedDeviceViewObject();

            SlotPairDeviceDefinitionSO sourceSlotDevice = GetDeviceFromArray(devices, sourceSlotIndex);
            SlotPairDeviceDefinitionSO targetSlotDevice = GetDeviceFromArray(devices, targetSlotIndex);

            slots[sourceSlotIndex].AttachEquippedDeviceViewObject(targetViewObject, sourceSlotDevice);
            slots[targetSlotIndex].AttachEquippedDeviceViewObject(sourceViewObject, targetSlotDevice);
        }

        /// <summary>슬롯 인덱스가 유효한지 확인한다.</summary>
        private bool IsValidSlotIndex(int slotIndex)
        {
            if (slots == null)
                return false;

            return slotIndex >= 0 && slotIndex < slots.Length && slots[slotIndex] != null;
        }

        /// <summary>Device 배열에서 지정 인덱스의 Device를 안전하게 반환한다.</summary>
        private static SlotPairDeviceDefinitionSO GetDeviceFromArray(SlotPairDeviceDefinitionSO[] devices, int slotIndex)
        {
            if (devices == null) return null;
            if (slotIndex < 0 || slotIndex >= devices.Length) return null;

            return devices[slotIndex];
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

                slots[i].Dropped -= HandleSlotDropped;
                slots[i].Dropped += HandleSlotDropped;

                slots[i].HoverEntered -= HandleSlotHoverEntered;
                slots[i].HoverEntered += HandleSlotHoverEntered;

                slots[i].HoverExited -= HandleSlotHoverExited;
                slots[i].HoverExited += HandleSlotHoverExited;

                slots[i].DragStartedWithPointer -= HandleSlotDragStartedWithPointer;
                slots[i].DragStartedWithPointer += HandleSlotDragStartedWithPointer;

                slots[i].DraggedWithPointer -= HandleSlotDraggedWithPointer;
                slots[i].DraggedWithPointer += HandleSlotDraggedWithPointer;

                slots[i].DragEndedWithPointer -= HandleSlotDragEndedWithPointer;
                slots[i].DragEndedWithPointer += HandleSlotDragEndedWithPointer;

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

                slots[i].Dropped -= HandleSlotDropped;

                slots[i].HoverEntered -= HandleSlotHoverEntered;
                slots[i].HoverExited -= HandleSlotHoverExited;

                slots[i].DragStartedWithPointer -= HandleSlotDragStartedWithPointer;
                slots[i].DraggedWithPointer -= HandleSlotDraggedWithPointer;
                slots[i].DragEndedWithPointer -= HandleSlotDragEndedWithPointer;
            }
        }

        /// <summary>자식 슬롯 Drop을 Rack 단위 이벤트로 전달한다.</summary>
        private void HandleSlotDropped(int slotIndex)
        {
            int resolvedSlotIndex = IsValidSlotIndex(currentDragPreviewSlotIndex)
                ? currentDragPreviewSlotIndex
                : slotIndex;

            LogInput($"[Dropped] Rack={name}, ColliderSlot={slotIndex}, ResolvedSlot={resolvedSlotIndex}");

            SlotDropped?.Invoke(resolvedSlotIndex);
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

        /// <summary>자식 슬롯 BeginDrag Pointer 이벤트를 Rack 단위 이벤트로 전달한다.</summary>
        private void HandleSlotDragStartedWithPointer(int slotIndex, PointerEventData eventData)
        {
            SlotDragStartedWithPointer?.Invoke(slotIndex, eventData);
        }

        /// <summary>자식 슬롯 Drag Pointer 이벤트를 Rack 단위 이벤트로 전달한다.</summary>
        private void HandleSlotDraggedWithPointer(int slotIndex, PointerEventData eventData)
        {
            SlotDraggedWithPointer?.Invoke(slotIndex, eventData);
        }

        /// <summary>자식 슬롯 EndDrag Pointer 이벤트를 Rack 단위 이벤트로 전달한다.</summary>
        private void HandleSlotDragEndedWithPointer(int slotIndex, PointerEventData eventData)
        {
            SlotDragEndedWithPointer?.Invoke(slotIndex, eventData);
        }

        #endregion

        // /// <summary>화면 좌표 기준으로 Rack 내부 슬롯 인덱스를 반환한다.</summary>
        // public bool TryFindSlotIndexUnderScreenPoint(Vector2 screenPoint, Camera targetCamera, out int slotIndex)
        // {
        //     slotIndex = -1;

        //     Camera cameraToUse = targetCamera != null ? targetCamera : Camera.main;

        //     if (cameraToUse == null)
        //         return false;

        //     Ray ray = cameraToUse.ScreenPointToRay(screenPoint);

        //     if (!Physics.Raycast(ray, out RaycastHit hit, 100f))
        //         return false;

        //     if (slots == null)
        //         return false;

        //     Collider hitCollider = hit.collider;

        //     for (int i = 0; i < slots.Length; i++)
        //     {
        //         if (slots[i] == null)
        //             continue;

        //         if (slots[i].ContainsCollider(hitCollider))
        //         {
        //             slotIndex = i;
        //             return true;
        //         }
        //     }

        //     return false;
        // }

        /// <summary>화면 좌표 기준으로 현재 Drag Preview 대상 슬롯 인덱스를 반환한다.</summary>
        public bool TryFindSlotIndexUnderScreenPoint(Vector2 screenPoint, Camera targetCamera, out int slotIndex)
        {
            slotIndex = -1;

            if (IsValidSlotIndex(currentDragPreviewSlotIndex))
            {
                slotIndex = currentDragPreviewSlotIndex;
                return true;
            }

            if (!TryProjectScreenPointToRackPlane(screenPoint, targetCamera, out Vector3 _, out Vector3 localPoint))
                return false;

            slotIndex = ResolveDragSnapSlotIndex(localPoint.x);
            currentDragPreviewSlotIndex = slotIndex;
            return IsValidSlotIndex(slotIndex);
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

        /// <summary>지정 슬롯의 장착 Device View를 드래그 표시용으로 분리한다.</summary>
        public bool TryBeginDeviceDragVisual(
            int sourceSlotIndex,
            out GameObject viewObject,
            out SlotPairDeviceDefinitionSO device)
        {
            viewObject = null;
            device = null;

            if (!IsValidSlotIndex(sourceSlotIndex))
                return false;

            bool detached = slots[sourceSlotIndex].TryDetachEquippedDeviceViewForDrag(out viewObject, out device);

            if (detached)
            {
                activeDragSnapSlotIndex = sourceSlotIndex;
                currentDragPreviewSlotIndex = sourceSlotIndex;
                SetAllSlotHoverFeedbackSuppressed(true);
            }

            return detached;
        }

        /// <summary>드래그 중인 Device View를 Rack local X 기준의 안정적인 Snap 규칙에 맞게 이동시킨다.</summary>
        public bool TryUpdateDeviceDragVisual(
            GameObject viewObject,
            PointerEventData eventData,
            ref Vector3 lastWorldPosition,
            out int snapSlotIndex)
        {
            snapSlotIndex = -1;

            if (viewObject == null)
                return false;

            if (!TryProjectPointerToRackPlane(eventData, out Vector3 worldPoint, out Vector3 localPoint))
            {
                currentDragPreviewSlotIndex = -1;
                return false;
            }

            snapSlotIndex = ResolveDragSnapSlotIndex(localPoint.x);
            currentDragPreviewSlotIndex = snapSlotIndex;

            if (!IsValidSlotIndex(snapSlotIndex))
            {
                viewObject.transform.position = worldPoint;
                lastWorldPosition = worldPoint;
                ClearAllSwapTargetPreviews();
                return true;
            }

            if (TryGetSlotEquippedDeviceAnchorPose(snapSlotIndex, out Vector3 anchorPosition, out Quaternion anchorRotation))
            {
                Vector3 floatingPosition = ApplySlotFloatingOffset(snapSlotIndex, anchorPosition);

                viewObject.transform.position = floatingPosition;
                viewObject.transform.rotation = anchorRotation;
                lastWorldPosition = floatingPosition;
                return true;
            }

            viewObject.transform.position = worldPoint;
            lastWorldPosition = worldPoint;
            ClearAllSwapTargetPreviews();
            return true;
        }

        /// <summary>드래그 중이던 Device View를 원래 슬롯으로 복귀시킨다.</summary>
        public async UniTask RestoreDeviceDragVisualAsync(
            int sourceSlotIndex,
            GameObject viewObject,
            SlotPairDeviceDefinitionSO device,
            bool playShake)
        {
            ResetDragSnapState();
            ClearAllSwapTargetPreviews();

            if (!IsValidSlotIndex(sourceSlotIndex))
            {
                if (viewObject != null)
                    Destroy(viewObject);

                return;
            }

            if (viewObject != null &&
                TryGetSlotEquippedDeviceAnchorPose(sourceSlotIndex, out Vector3 targetPosition, out Quaternion targetRotation))
            {
                await PlayInvalidDropReturnAsync(viewObject, targetPosition, targetRotation);
            }

            slots[sourceSlotIndex].RestoreEquippedDeviceViewAfterDrag(viewObject, device);

            if (playShake)
                await slots[sourceSlotIndex].PlayInvalidDropShakeAsync();

            RestoreAllSlotHoverFeedbackAfterDragDrop();
        }

        /// <summary>무효 Drop된 Device View를 현재 위치에서 원래 슬롯 위치까지 빠르게 복귀시킨다.</summary>
        private async UniTask PlayInvalidDropReturnAsync(
            GameObject viewObject,
            Vector3 targetPosition,
            Quaternion targetRotation)
        {
            if (viewObject == null)
                return;

            Transform viewTransform = viewObject.transform;
            Vector3 startPosition = viewTransform.position;
            Quaternion startRotation = viewTransform.rotation;

            float duration = Mathf.Max(0.01f, invalidDropReturnDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (viewObject == null)
                    return;

                elapsed += Time.deltaTime;

                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = Mathf.SmoothStep(0f, 1f, t);

                viewTransform.position = Vector3.Lerp(startPosition, targetPosition, easedT);
                viewTransform.rotation = Quaternion.Slerp(startRotation, targetRotation, easedT);

                await UniTask.Yield();
            }

            if (viewObject == null)
                return;

            viewTransform.position = targetPosition;
            viewTransform.rotation = targetRotation;
        }

        /// <summary>유효 Drop 완료 후 드래그 임시 View를 제거하고 source 슬롯 상태를 정리한다.</summary>
        public void CompleteDeviceDragVisual(int sourceSlotIndex, GameObject viewObject)
        {
            ResetDragSnapState();
            ClearAllSwapTargetPreviews();

            if (IsValidSlotIndex(sourceSlotIndex))
                slots[sourceSlotIndex].MarkDragVisualCompleted();

            if (viewObject != null)
                Destroy(viewObject);

            RestoreAllSlotHoverFeedbackAfterDragDrop();
        }

        /// <summary>유효 Drop 완료 후 드래그 View와 Swap Preview View를 최종 슬롯에 부착한다.</summary>
        public void CompleteDeviceDragVisualSwap(
            int sourceSlotIndex,
            int targetSlotIndex,
            GameObject draggedViewObject,
            SlotPairDeviceDefinitionSO draggedDevice)
        {
            ResetDragSnapState();

            if (!IsValidSlotIndex(sourceSlotIndex) || !IsValidSlotIndex(targetSlotIndex))
            {
                ClearAllSwapTargetPreviews();

                if (draggedViewObject != null)
                    Destroy(draggedViewObject);

                RestoreAllSlotHoverFeedbackAfterDragDrop();
                return;
            }

            if (sourceSlotIndex == targetSlotIndex)
            {
                ClearAllSwapTargetPreviews();
                slots[sourceSlotIndex].RestoreEquippedDeviceViewAfterDrag(draggedViewObject, draggedDevice);

                RestoreAllSlotHoverFeedbackAfterDragDrop();
                PlayDropConfirmSettleAsync(sourceSlotIndex, -1).Forget();
                return;
            }

            SlotPairDeviceDefinitionSO targetDevice = slots[targetSlotIndex].CurrentDevice;
            GameObject targetViewObject = slots[targetSlotIndex].DetachEquippedDeviceViewObject();

            slots[sourceSlotIndex].AttachEquippedDeviceViewObject(targetViewObject, targetDevice);
            slots[targetSlotIndex].AttachEquippedDeviceViewObject(draggedViewObject, draggedDevice);

            RestoreAllSlotHoverFeedbackAfterDragDrop();
            PlayDropConfirmSettleAsync(sourceSlotIndex, targetSlotIndex).Forget();
        }

        /// <summary>Drop 성공 후 source/target 슬롯에 짧은 확정 Settle 연출을 재생한다.</summary>
        private async UniTaskVoid PlayDropConfirmSettleAsync(int sourceSlotIndex, int targetSlotIndex)
        {
            UniTask sourceTask = IsValidSlotIndex(sourceSlotIndex)
                ? slots[sourceSlotIndex].PlayDropConfirmSettleAsync(
                    dropConfirmSettleDuration,
                    dropConfirmSettleScaleMultiplier)
                : UniTask.CompletedTask;

            UniTask targetTask = IsValidSlotIndex(targetSlotIndex)
                ? slots[targetSlotIndex].PlayDropConfirmSettleAsync(
                    dropConfirmSettleDuration,
                    dropConfirmSettleScaleMultiplier)
                : UniTask.CompletedTask;

            await UniTask.WhenAll(sourceTask, targetTask);
        }

        /// <summary>지정 슬롯의 장착 Anchor Pose를 반환한다.</summary>
        public bool TryGetSlotEquippedDeviceAnchorPose(int slotIndex, out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;

            if (!IsValidSlotIndex(slotIndex))
                return false;

            return slots[slotIndex].TryGetEquippedDeviceAnchorPose(out position, out rotation);
        }

        /// <summary>지정 슬롯의 장착 위치에 Swap/Drag 미리보기용 Floating Offset을 적용한 월드 위치를 반환한다.</summary>
        private Vector3 ApplySlotFloatingOffset(int slotIndex, Vector3 basePosition)
        {
            if (!IsValidSlotIndex(slotIndex))
                return basePosition;

            if (slots[slotIndex] == null)
                return basePosition;

            return basePosition + slots[slotIndex].transform.up * swapPreviewLiftDistance;
        }

        /// <summary>현재 스냅 대상 슬롯에 Swap 대상 미리보기를 적용한다.</summary>
        public void UpdateSwapTargetPreview(int sourceSlotIndex, int targetSlotIndex)
        {
            if (slots == null)
                return;

            bool hasSourcePose = TryGetSlotEquippedDeviceAnchorPose(
                sourceSlotIndex,
                out Vector3 sourceAnchorPosition,
                out Quaternion sourceAnchorRotation);

            Vector3 previewPosition = hasSourcePose
                ? ApplySlotFloatingOffset(sourceSlotIndex, sourceAnchorPosition)
                : sourceAnchorPosition;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                    continue;

                if (i == targetSlotIndex && i != sourceSlotIndex && hasSourcePose)
                {
                    slots[i].SetSwapTargetPreviewToPose(
                        sourceSlotIndex,
                        targetSlotIndex,
                        swapPreviewWorldRoot,
                        previewPosition,
                        sourceAnchorRotation,
                        swapPreviewScaleMultiplier);
                    continue;
                }

                slots[i].ClearSwapTargetPreview();
            }
        }

        /// <summary>모든 슬롯의 Swap 대상 미리보기를 제거한다.</summary>
        public void ClearAllSwapTargetPreviews()
        {
            if (slots == null)
                return;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                    continue;

                slots[i].ClearSwapTargetPreview();
            }
        }

        /// <summary>Rack 내부 모든 슬롯의 Hover Scale 피드백 억제 여부를 설정한다.</summary>
        private void SetAllSlotHoverFeedbackSuppressed(bool suppressed)
        {
            if (slots == null)
                return;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                    continue;

                slots[i].SetHoverFeedbackSuppressed(suppressed);
            }
        }

        /// <summary>현재 포인터 Hover 상태 기준으로 모든 슬롯 Hover Scale 피드백을 즉시 재평가한다.</summary>
        private void ForceRefreshAllSlotHoverFeedbackAfterDragDrop()
        {
            if (slots == null)
                return;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                    continue;

                slots[i].ForceRefreshHoverFeedbackAfterDragDrop();
            }
        }

        /// <summary>Device Drag 종료 후 억제된 Hover Scale 피드백을 복구하고 현재 Hover 상태를 즉시 재평가한다.</summary>
        private void RestoreAllSlotHoverFeedbackAfterDragDrop()
        {
            SetAllSlotHoverFeedbackSuppressed(false);
            ForceRefreshAllSlotHoverFeedbackAfterDragDrop();
        }

        /// <summary>Pointer screen 좌표를 DeviceRack 평면의 world/local 좌표로 변환한다.</summary>
        private bool TryProjectPointerToRackPlane(
            PointerEventData eventData,
            out Vector3 worldPoint,
            out Vector3 localPoint)
        {
            worldPoint = Vector3.zero;
            localPoint = Vector3.zero;

            if (eventData == null)
                return false;

            Camera cameraToUse = eventData.pressEventCamera != null
                ? eventData.pressEventCamera
                : Camera.main;

            return TryProjectScreenPointToRackPlane(eventData.position, cameraToUse, out worldPoint, out localPoint);
        }

        /// <summary>화면 좌표를 DeviceRack 평면의 world/local 좌표로 변환한다.</summary>
        private bool TryProjectScreenPointToRackPlane(
            Vector2 screenPoint,
            Camera targetCamera,
            out Vector3 worldPoint,
            out Vector3 localPoint)
        {
            worldPoint = Vector3.zero;
            localPoint = Vector3.zero;

            Camera cameraToUse = targetCamera != null ? targetCamera : Camera.main;

            if (cameraToUse == null)
                return false;

            Ray ray = cameraToUse.ScreenPointToRay(screenPoint);
            Plane rackPlane = new Plane(transform.up, transform.position);

            if (!rackPlane.Raycast(ray, out float distance))
                return false;

            worldPoint = ray.GetPoint(distance);
            localPoint = transform.InverseTransformPoint(worldPoint);
            return true;
        }

        /// <summary>Rack local X 기준으로 hysteresis가 적용된 드래그 Snap 대상 슬롯을 계산한다.</summary>
        private int ResolveDragSnapSlotIndex(float localX)
        {
            float safeHysteresis = Mathf.Clamp(dragSnapSwitchHysteresis, 0f, 0.02f);

            if (IsOutsideDragSnapBounds(localX))
            {
                activeDragSnapSlotIndex = -1;
                return -1;
            }

            if (IsValidSlotIndex(activeDragSnapSlotIndex) &&
                IsInsideActiveSnapHysteresis(localX, activeDragSnapSlotIndex, safeHysteresis))
            {
                return activeDragSnapSlotIndex;
            }

            int nearestSlotIndex = FindNearestSnapSlotIndex(localX);
            activeDragSnapSlotIndex = nearestSlotIndex;
            return nearestSlotIndex;
        }

        /// <summary>현재 local X가 전체 Drag Snap 영역 바깥인지 확인한다.</summary>
        private bool IsOutsideDragSnapBounds(float localX)
        {
            if (!TryGetDragSnapOuterBounds(out float leftBoundary, out float rightBoundary))
                return true;

            return localX < leftBoundary || localX > rightBoundary;
        }

        /// <summary>현재 active slot의 hysteresis 유지 영역 안에 local X가 있는지 확인한다.</summary>
        private bool IsInsideActiveSnapHysteresis(float localX, int slotIndex, float safeHysteresis)
        {
            if (dragSnapSlotCenterLocalX == null)
                return false;

            if (slotIndex < 0 || slotIndex >= dragSnapSlotCenterLocalX.Length)
                return false;

            if (!TryGetDragSnapOuterBounds(out float leftBoundary, out float rightBoundary))
                return false;

            if (slotIndex > 0)
            {
                float previousCenter = dragSnapSlotCenterLocalX[slotIndex - 1];
                float currentCenter = dragSnapSlotCenterLocalX[slotIndex];
                leftBoundary = (previousCenter + currentCenter) * 0.5f - safeHysteresis;
            }

            if (slotIndex < dragSnapSlotCenterLocalX.Length - 1)
            {
                float currentCenter = dragSnapSlotCenterLocalX[slotIndex];
                float nextCenter = dragSnapSlotCenterLocalX[slotIndex + 1];
                rightBoundary = (currentCenter + nextCenter) * 0.5f + safeHysteresis;
            }

            return localX >= leftBoundary && localX <= rightBoundary;
        }

        /// <summary>Drag Snap 전체 유효 local X 경계를 계산한다.</summary>
        private bool TryGetDragSnapOuterBounds(out float leftBoundary, out float rightBoundary)
        {
            leftBoundary = 0f;
            rightBoundary = 0f;

            if (dragSnapSlotCenterLocalX == null || dragSnapSlotCenterLocalX.Length <= 0)
                return false;

            float halfSpacing = CalculateDragSnapEdgeHalfSpacing();

            leftBoundary = dragSnapSlotCenterLocalX[0] - halfSpacing - dragSnapOuterPadding;
            rightBoundary = dragSnapSlotCenterLocalX[dragSnapSlotCenterLocalX.Length - 1] + halfSpacing + dragSnapOuterPadding;
            return true;
        }

        /// <summary>양끝 슬롯의 기본 반폭 계산에 사용할 슬롯 간격 절반을 반환한다.</summary>
        private float CalculateDragSnapEdgeHalfSpacing()
        {
            if (dragSnapSlotCenterLocalX == null || dragSnapSlotCenterLocalX.Length <= 1)
                return 0.2f;

            float firstSpacing = Mathf.Abs(dragSnapSlotCenterLocalX[1] - dragSnapSlotCenterLocalX[0]);
            return firstSpacing * 0.5f;
        }

        /// <summary>local X와 가장 가까운 슬롯 center의 인덱스를 반환한다.</summary>
        private int FindNearestSnapSlotIndex(float localX)
        {
            if (dragSnapSlotCenterLocalX == null || dragSnapSlotCenterLocalX.Length <= 0)
                return -1;

            int nearestIndex = -1;
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < dragSnapSlotCenterLocalX.Length; i++)
            {
                float distance = Mathf.Abs(localX - dragSnapSlotCenterLocalX[i]);

                if (distance >= nearestDistance)
                    continue;

                nearestDistance = distance;
                nearestIndex = i;
            }

            return nearestIndex;
        }

        /// <summary>Drag Snap 상태를 초기화한다.</summary>
        private void ResetDragSnapState()
        {
            activeDragSnapSlotIndex = -1;
            currentDragPreviewSlotIndex = -1;
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
