using System.Collections.Generic;
using Tessera.Data;
using UnityEngine;

namespace Tessera.UI
{
    /// <summary>테이블 위 3D Device 슬롯 5개의 표시를 관리한다.</summary>
    public class DeviceRack3DView : MonoBehaviour
    {
        [Header("Slots")]
        [SerializeField] private DeviceSlot3DView[] slots = new DeviceSlot3DView[5];

        /// <summary>슬롯 개수를 반환한다.</summary>
        public int SlotCount => slots != null ? slots.Length : 0;

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

        /// <summary>SO 배열 기준으로 3D Device 슬롯 표시를 갱신한다.</summary>
        public void SetDevices(IReadOnlyList<SlotPairDeviceDefinitionSO> devices)
        {
            if (slots == null)
                return;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                    continue;

                SlotPairDeviceDefinitionSO device = null;

                if (devices != null && i >= 0 && i < devices.Count)
                    device = devices[i];

                // 각 슬롯은 null이면 빈 슬롯으로 표시한다.
                slots[i].SetDevice(device);
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

        /// <summary>지정 슬롯의 DeviceSlot Transform을 반환한다.</summary>
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
    }
}
