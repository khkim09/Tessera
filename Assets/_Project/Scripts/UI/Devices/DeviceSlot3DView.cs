using System;
using Cysharp.Threading.Tasks;
using Tessera.Data;
using Tessera.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;

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

        private bool isDragVisualDetached; // 현재 장착 View - 슬롯에서 분리된 상태 여부
        private bool isSwapTargetPreviewActive; // Swap 대상 미리보기 적용 여부
        private Vector3 swapPreviewOriginalLocalPosition; // Swap 대상 미리보기 적용 전 로컬 위치
        private Quaternion swapPreviewOriginalLocalRotation = Quaternion.identity; // Swap 대상 미리보기 적용 전 로컬 회전
        private Vector3 swapPreviewOriginalLocalScale = Vector3.one; // Swap 대상 미리보기 적용 전 로컬 Scale

        private Transform swapPreviewOriginalParent; // Swap 적용 전 부모 Object
        private int swapPreviewOriginalSiblingIndex;
        private Quaternion swapPreviewOriginalWorldRotation = Quaternion.identity;
        private Vector3 swapPreviewOriginalWorldScale = Vector3.one;
        private bool suppressHoverFeedbackDuringDrag; // Rack Drag 중 Hover Scale 피드백 억제 여부
        private bool isPointerHovering; // 현재 포인터가 슬롯 Hover 영역 안에 있는지 여부

        public int SlotIndex => slotIndex;
        public SlotPairDeviceDefinitionSO CurrentDevice => currentDevice;
        public bool IsPointerHovering => isPointerHovering;

        public event Action<int> Dropped;

        public event Action<int> HoverEntered;
        public event Action<int> HoverExited;

        public event Action<int, PointerEventData> DragStartedWithPointer;
        public event Action<int, PointerEventData> DraggedWithPointer;
        public event Action<int, PointerEventData> DragEndedWithPointer;

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
            isPointerHovering = false;

            if (hoverHighlightTarget != null)
                hoverHighlightTarget.ResetHighlight();

            UnregisterClickRelays();
        }

        /// <summary>파괴 시 외부 이벤트 참조를 정리한다.</summary>
        private void OnDestroy()
        {
            UnregisterClickRelays();
            ClearEquippedDeviceView();

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

        /// <summary>Rack Drag 중 슬롯 Hover Scale 피드백 억제 여부를 설정한다.</summary>
        public void SetHoverFeedbackSuppressed(bool suppressed)
        {
            suppressHoverFeedbackDuringDrag = suppressed;

            if (hoverHighlightTarget != null && suppressHoverFeedbackDuringDrag)
                hoverHighlightTarget.ResetHighlight();

            RefreshHoverFeedbackEnabled();
        }

        /// <summary>현재 포인터 Hover 상태 기준으로 Hover Scale 피드백을 즉시 재평가한다.</summary>
        public void ForceRefreshHoverFeedbackAfterDragDrop()
        {
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
        public void NotifySlotDragStarted(PointerEventData eventData)
        {
            LogInput($"[DragStarted] Slot={name}, SlotIndex={slotIndex}");

            if (!interactionEnabled) return;
            if (slotIndex < 0) return;

            DragStartedWithPointer?.Invoke(slotIndex, eventData);
        }

        /// <summary>외부 Collider Relay에서 슬롯 드래그 이동을 전달받는다.</summary>
        public void NotifySlotDragged(PointerEventData eventData)
        {
            if (!interactionEnabled) return;
            if (slotIndex < 0) return;

            DraggedWithPointer?.Invoke(slotIndex, eventData);
        }

        /// <summary>외부 Collider Relay에서 슬롯 드래그 종료를 전달받는다.</summary>
        public void NotifySlotDragEnded(PointerEventData eventData)
        {
            LogInput($"[DragEnded] Slot={name}, SlotIndex={slotIndex}");

            if (!interactionEnabled) return;
            if (slotIndex < 0) return;

            DragEndedWithPointer?.Invoke(slotIndex, eventData);
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

            if (slotIndex < 0)
                return;

            isPointerHovering = true;
            RefreshHoverFeedbackEnabled();

            if (!interactionEnabled)
                return;

            HoverEntered?.Invoke(slotIndex);
        }

        /// <summary>외부 Collider Relay에서 슬롯 Hover 이탈을 전달받는다.</summary>
        public void NotifySlotHoverExited()
        {
            LogInput($"[HoverExited] Slot={name}, SlotIndex={slotIndex}");

            if (slotIndex < 0)
                return;

            isPointerHovering = false;
            RefreshHoverFeedbackEnabled();

            if (!interactionEnabled)
                return;

            HoverExited?.Invoke(slotIndex);
        }

        /// <summary>지정한 Device 데이터를 3D 슬롯에 반영한다.</summary>
        public void SetDevice(SlotPairDeviceDefinitionSO device)
        {
            bool isSameDeviceWithValidView =
                currentDevice == device &&
                device != null &&
                equippedDeviceViewObject != null &&
                !isDragVisualDetached &&
                !isSwapTargetPreviewActive;

            currentDevice = device;

            if (isSameDeviceWithValidView)
            {
                RefreshEquippedDeviceViewBinding();
                RefreshHoverFeedbackEnabled();
                return;
            }

            if (isDragVisualDetached)
            {
                RefreshHoverFeedbackEnabled();
                return;
            }

            if (isSwapTargetPreviewActive)
            {
                RefreshEquippedDeviceViewBinding();
                RefreshHoverFeedbackEnabled();
                return;
            }

            if (device == null)
            {
                SetEmpty();
                return;
            }

            SetEquipped(device);
        }

        /// <summary>유효 Drop으로 드래그 표시가 종료되었음을 슬롯에 반영한다.</summary>
        public void MarkDragVisualCompleted()
        {
            isDragVisualDetached = false;
            equippedDeviceViewObject = null;
            equippedDeviceView = null;
            RefreshHoverFeedbackEnabled();
        }

        /// <summary>현재 장착 Device View 오브젝트를 슬롯에서 분리해 반환한다.</summary>
        public GameObject DetachEquippedDeviceViewObject()
        {
            GameObject detachedViewObject = equippedDeviceViewObject;

            ClearSwapPreviewStateOnly();

            currentDevice = null;
            equippedDeviceViewObject = null;
            equippedDeviceView = null;
            isDragVisualDetached = false;

            if (detachedViewObject != null)
                detachedViewObject.transform.SetParent(null, true);

            if (equippedDeviceRoot != null)
                equippedDeviceRoot.gameObject.SetActive(false);

            RefreshHoverFeedbackEnabled();
            return detachedViewObject;
        }

        /// <summary>기존 장착 Device View 오브젝트를 이 슬롯에 부착한다.</summary>
        public void AttachEquippedDeviceViewObject(GameObject viewObject, SlotPairDeviceDefinitionSO device)
        {
            ClearSwapTargetPreview();
            isDragVisualDetached = false;

            currentDevice = device;

            if (device == null)
            {
                if (viewObject != null)
                    Destroy(viewObject);

                SetEmpty();
                return;
            }

            if (equippedDeviceRoot != null)
                equippedDeviceRoot.gameObject.SetActive(true);

            if (viewObject == null)
            {
                RebuildEquippedDeviceView(device);
                RefreshHoverFeedbackEnabled();
                return;
            }

            Transform parent = equippedDeviceViewAnchor != null ? equippedDeviceViewAnchor : equippedDeviceRoot;

            if (parent == null)
            {
                Destroy(viewObject);
                RebuildEquippedDeviceView(device);
                RefreshHoverFeedbackEnabled();
                return;
            }

            equippedDeviceViewObject = viewObject;
            equippedDeviceViewObject.transform.SetParent(parent, false);
            equippedDeviceViewObject.transform.localPosition = Vector3.zero;
            equippedDeviceViewObject.transform.localRotation = Quaternion.identity;
            equippedDeviceViewObject.transform.localScale = Vector3.one;

            equippedDeviceView = equippedDeviceViewObject.GetComponent<EquippedDevice3DView>();

            if (equippedDeviceView == null)
                equippedDeviceView = equippedDeviceViewObject.GetComponentInChildren<EquippedDevice3DView>(true);

            if (equippedDeviceView != null)
                equippedDeviceView.Bind(device);

            RefreshHoverFeedbackEnabled();
        }

        /// <summary>드래그 표시를 위해 현재 장착 Device View만 슬롯에서 분리한다.</summary>
        public bool TryDetachEquippedDeviceViewForDrag(
            out GameObject viewObject,
            out SlotPairDeviceDefinitionSO device)
        {
            viewObject = equippedDeviceViewObject;
            device = currentDevice;

            if (device == null)
                return false;

            if (viewObject == null)
            {
                RebuildEquippedDeviceView(device);
                viewObject = equippedDeviceViewObject;
            }

            if (viewObject == null)
                return false;

            ClearSwapTargetPreview();

            isDragVisualDetached = true;
            equippedDeviceViewObject = null;
            equippedDeviceView = null;

            viewObject.transform.SetParent(null, true);

            if (equippedDeviceRoot != null)
                equippedDeviceRoot.gameObject.SetActive(false);

            RefreshHoverFeedbackEnabled();
            return true;
        }

        /// <summary>드래그 중이던 Device View를 이 슬롯의 장착 Anchor로 복귀시킨다.</summary>
        public void RestoreEquippedDeviceViewAfterDrag(GameObject viewObject, SlotPairDeviceDefinitionSO device)
        {
            if (device == null)
            {
                if (viewObject != null)
                    Destroy(viewObject);

                SetEmpty();
                return;
            }

            AttachEquippedDeviceViewObject(viewObject, device);
        }

        /// <summary>유효 Drop 후 임시 드래그 View를 제거한다.</summary>
        public void DestroyDraggedDeviceView(GameObject viewObject)
        {
            if (viewObject == null)
                return;

            Destroy(viewObject);
        }

        /// <summary>장착 Device Anchor의 월드 위치와 회전을 반환한다.</summary>
        public bool TryGetEquippedDeviceAnchorPose(out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;

            Transform anchor = equippedDeviceViewAnchor != null ? equippedDeviceViewAnchor : equippedDeviceRoot;

            if (anchor == null)
                return false;

            position = anchor.position;
            rotation = anchor.rotation;
            return true;
        }

        /// <summary>잘못된 Drop 피드백으로 슬롯을 짧게 흔든다.</summary>
        public async UniTask PlayInvalidDropShakeAsync()
        {
            Transform shakeRoot = transform;
            Vector3 origin = shakeRoot.localPosition;

            float duration = 0.18f;
            float elapsed = 0f;
            float strength = 0.035f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                float wave = Mathf.Sin(elapsed * 90f);
                shakeRoot.localPosition = origin + new Vector3(wave * strength, 0f, 0f);

                await UniTask.Yield();
            }

            shakeRoot.localPosition = origin;
        }

        /// <summary>Drop 성공 확정 시 Device View에 짧은 Settle Scale 연출을 재생한다.</summary>
        public async UniTask PlayDropConfirmSettleAsync(float duration, float scaleMultiplier)
        {
            if (equippedDeviceViewObject == null)
                return;

            Transform targetTransform = equippedDeviceViewObject.transform;
            Vector3 originScale = targetTransform.localScale;
            Vector3 peakScale = originScale * Mathf.Max(1f, scaleMultiplier);

            float safeDuration = Mathf.Max(0.01f, duration);
            float elapsed = 0f;

            while (elapsed < safeDuration)
            {
                if (targetTransform == null)
                    return;

                elapsed += Time.deltaTime;

                float t = Mathf.Clamp01(elapsed / safeDuration);
                float wave = Mathf.Sin(t * Mathf.PI);

                targetTransform.localScale = Vector3.LerpUnclamped(originScale, peakScale, wave);

                await UniTask.Yield();
            }

            if (targetTransform == null)
                return;

            targetTransform.localScale = originScale;
        }

        /// <summary>Swap 대상 Device를 최종 배치 예정 슬롯 위치로 이동해 미리보기로 표시한다.</summary>
        public void SetSwapTargetPreviewToPose(
            int sourceSlotIndex,
            int targetSlotIndex,
            Transform previewWorldRoot,
            Vector3 previewWorldPosition,
            Quaternion previewWorldRotation,
            float previewScaleMultiplier)
        {
            if (sourceSlotIndex == targetSlotIndex)
            {
                ClearSwapTargetPreview();
                return;
            }

            if (currentDevice == null)
            {
                ClearSwapTargetPreview();
                return;
            }

            if (equippedDeviceViewObject == null)
            {
                ClearSwapTargetPreview();
                return;
            }

            Transform previewTransform = equippedDeviceViewObject.transform;

            if (!isSwapTargetPreviewActive)
            {
                swapPreviewOriginalParent = previewTransform.parent;
                swapPreviewOriginalSiblingIndex = previewTransform.GetSiblingIndex();
                swapPreviewOriginalLocalPosition = previewTransform.localPosition;
                swapPreviewOriginalLocalRotation = previewTransform.localRotation;
                swapPreviewOriginalLocalScale = previewTransform.localScale;
                swapPreviewOriginalWorldRotation = previewTransform.rotation;
                swapPreviewOriginalWorldScale = previewTransform.lossyScale;
                isSwapTargetPreviewActive = true;
            }

            Transform previewParent = previewWorldRoot != null ? previewWorldRoot : null;

            if (previewTransform.parent != previewParent)
                previewTransform.SetParent(previewParent, true);

            previewTransform.position = previewWorldPosition;
            previewTransform.rotation = previewWorldRotation;
            SetWorldScale(previewTransform, swapPreviewOriginalWorldScale * previewScaleMultiplier);

            RefreshEquippedDeviceViewBinding();
            RefreshHoverFeedbackEnabled();
        }

        /// <summary>Swap 대상 미리보기 상태를 원래 표시로 복구한다.</summary>
        public void ClearSwapTargetPreview()
        {
            if (!isSwapTargetPreviewActive)
                return;

            if (equippedDeviceViewObject != null)
            {
                Transform previewTransform = equippedDeviceViewObject.transform;

                previewTransform.SetParent(swapPreviewOriginalParent, false);

                if (swapPreviewOriginalParent != null)
                    previewTransform.SetSiblingIndex(swapPreviewOriginalSiblingIndex);

                previewTransform.localPosition = swapPreviewOriginalLocalPosition;
                previewTransform.localRotation = swapPreviewOriginalLocalRotation;
                previewTransform.localScale = swapPreviewOriginalLocalScale;
            }

            ClearSwapPreviewStateOnly();
            RefreshHoverFeedbackEnabled();
        }

        /// <summary>Swap Preview 위치를 되돌리지 않고 Preview 상태 플래그만 초기화한다.</summary>
        private void ClearSwapPreviewStateOnly()
        {
            swapPreviewOriginalParent = null;
            swapPreviewOriginalSiblingIndex = -1;
            swapPreviewOriginalLocalPosition = Vector3.zero;
            swapPreviewOriginalLocalRotation = Quaternion.identity;
            swapPreviewOriginalLocalScale = Vector3.one;
            swapPreviewOriginalWorldRotation = Quaternion.identity;
            swapPreviewOriginalWorldScale = Vector3.one;
            isSwapTargetPreviewActive = false;
        }

        /// <summary>현재 장착 Device View 컴포넌트를 다시 찾고 Device 데이터를 재바인딩한다.</summary>
        private void RefreshEquippedDeviceViewBinding()
        {
            if (equippedDeviceViewObject == null)
                return;

            if (equippedDeviceView == null)
                equippedDeviceView = equippedDeviceViewObject.GetComponent<EquippedDevice3DView>();

            if (equippedDeviceView == null)
                equippedDeviceView = equippedDeviceViewObject.GetComponentInChildren<EquippedDevice3DView>(true);

            if (equippedDeviceView != null && currentDevice != null)
                equippedDeviceView.Bind(currentDevice);
        }

        /// <summary>Transform의 월드 Scale을 지정 값에 가깝게 맞춘다.</summary>
        private static void SetWorldScale(Transform targetTransform, Vector3 targetWorldScale)
        {
            if (targetTransform == null)
                return;

            Transform parent = targetTransform.parent;

            if (parent == null)
            {
                targetTransform.localScale = targetWorldScale;
                return;
            }

            Vector3 parentScale = parent.lossyScale;

            targetTransform.localScale = new Vector3(
                SafeDivide(targetWorldScale.x, parentScale.x),
                SafeDivide(targetWorldScale.y, parentScale.y),
                SafeDivide(targetWorldScale.z, parentScale.z));
        }

        /// <summary>0에 가까운 값으로 나누지 않도록 보호한다.</summary>
        private static float SafeDivide(float value, float divisor)
        {
            if (Mathf.Abs(divisor) < 0.0001f)
                return value;

            return value / divisor;
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
            ClearSwapTargetPreview();
            isDragVisualDetached = false;

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

            if (equippedDeviceView == null)
                equippedDeviceView = equippedDeviceViewObject.GetComponentInChildren<EquippedDevice3DView>(true);

            if (equippedDeviceView != null)
                equippedDeviceView.Bind(device);
        }

        /// <summary>현재 생성된 장착 Device View 오브젝트를 제거한다.</summary>
        private void ClearEquippedDeviceView()
        {
            ClearSwapTargetPreview();
            equippedDeviceView = null;

            if (equippedDeviceViewObject == null)
                return;

            Destroy(equippedDeviceViewObject);
            equippedDeviceViewObject = null;
        }

        /// <summary>현재 장착 상태와 포인터 Hover 상태 기준으로 Hover Scale 표시를 갱신한다.</summary>
        private void RefreshHoverFeedbackEnabled()
        {
            if (hoverHighlightTarget == null)
                return;

            bool hasDevice = currentDevice != null;
            bool canUseHoverFeedback =
                interactionEnabled &&
                hasDevice &&
                !isDragVisualDetached &&
                !isSwapTargetPreviewActive &&
                !suppressHoverFeedbackDuringDrag;

            hoverHighlightTarget.SetHoverFeedbackEnabled(canUseHoverFeedback);

            if (!canUseHoverFeedback || !isPointerHovering)
            {
                hoverHighlightTarget.ResetHighlight();
                return;
            }

            hoverHighlightTarget.ForceApplyHoverHighlight();
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
