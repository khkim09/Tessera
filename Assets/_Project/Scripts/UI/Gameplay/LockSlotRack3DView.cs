using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tessera.UI
{
    /// <summary>테이블 위 3D LockSlot 5개의 표시 상태와 클릭 입력을 관리한다.</summary>
    public class LockSlotRack3DView : MonoBehaviour
    {
        [Header("Slots")]
        [SerializeField] private LockSlot3DView[] slots = new LockSlot3DView[5];

        private Action<int> slotClickedCallback;

        /// <summary>슬롯 개수를 반환한다.</summary>
        public int SlotCount => slots != null ? slots.Length : 0;

        /// <summary>인스펙터에서 자식 LockSlot3DView를 자동 수집한다.</summary>
        private void Reset()
        {
            // 자식에 붙은 LockSlot3DView를 자동 수집한다.
            slots = GetComponentsInChildren<LockSlot3DView>(true);
        }

        /// <summary>슬롯 클릭 콜백과 슬롯 인덱스를 초기화한다.</summary>
        public void InitializeSlots(Action<int> slotClickedCallback)
        {
            this.slotClickedCallback = slotClickedCallback;

            if (slots == null)
                return;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                    continue;

                slots[i].Initialize(i, this.slotClickedCallback);
            }
        }

        /// <summary>LockSlot 매핑과 현재 주사위 값 기준으로 3D 슬롯 표시를 갱신한다.</summary>
        public void SetLockedDiceSlots(IReadOnlyList<int> lockedDiceIndexBySlot, IReadOnlyList<int> diceValues)
        {
            if (slots == null)
                return;

            for (int slotIndex = 0; slotIndex < slots.Length; slotIndex++)
            {
                if (slots[slotIndex] == null)
                    continue;

                int diceIndex = GetDiceIndexAtSlot(lockedDiceIndexBySlot, slotIndex);

                if (diceIndex < 0)
                {
                    slots[slotIndex].SetEmpty();
                    continue;
                }

                int diceValue = GetDiceValue(diceValues, diceIndex);

                // Core의 원본 DiceIndex와 현재 눈금을 슬롯에 표시한다.
                slots[slotIndex].SetLockedDice(diceIndex, diceValue);
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

        /// <summary>지정 슬롯의 Dice 배치 기준 Transform을 반환한다.</summary>
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

        /// <summary>지정 슬롯에 배치된 DiceIndex를 안전하게 반환한다.</summary>
        private static int GetDiceIndexAtSlot(IReadOnlyList<int> lockedDiceIndexBySlot, int slotIndex)
        {
            if (lockedDiceIndexBySlot == null)
                return -1;

            if (slotIndex < 0 || slotIndex >= lockedDiceIndexBySlot.Count)
                return -1;

            return lockedDiceIndexBySlot[slotIndex];
        }

        /// <summary>지정 DiceIndex의 현재 눈금을 안전하게 반환한다.</summary>
        private static int GetDiceValue(IReadOnlyList<int> diceValues, int diceIndex)
        {
            if (diceValues == null)
                return 0;

            if (diceIndex < 0 || diceIndex >= diceValues.Count)
                return 0;

            return diceValues[diceIndex];
        }
    }
}
