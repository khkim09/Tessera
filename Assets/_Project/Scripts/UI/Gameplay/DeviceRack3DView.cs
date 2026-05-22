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
    }
}
