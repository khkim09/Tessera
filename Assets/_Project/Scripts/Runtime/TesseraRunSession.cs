using Tessera.Core;
using Tessera.Data;
using UnityEngine;

namespace Tessera.Runtime
{
    /// <summary>한 Run 동안 유지되는 Stage, HP, 재화, Chain, 장착 Device 상태를 관리한다.</summary>
    public class TesseraRunSession
    {
        public const int MaxDeviceSlots = 5;

        private readonly SlotPairDeviceDefinitionSO[] equippedSlotPairDevices = new SlotPairDeviceDefinitionSO[MaxDeviceSlots];

        /// <summary>현재 Stage 인덱스.</summary>
        public int CurrentStageIndex { get; private set; }

        /// <summary>현재 Stage 번호.</summary>
        public int CurrentStageNumber => CurrentStageIndex + 1;

        /// <summary>현재 보유 Parts.</summary>
        public int Parts { get; private set; }

        /// <summary>플레이어 최대 HP.</summary>
        public int PlayerMaxHp { get; private set; }

        /// <summary>플레이어 현재 HP.</summary>
        public int PlayerCurrentHp { get; private set; }

        /// <summary>Run 전체 Chain 누적값.</summary>
        public int RunChainCount { get; private set; }

        /// <summary>현재 Stage Chain 누적값.</summary>
        public int StageChainCount { get; private set; }

        /// <summary>현재 Stage Pressure 단계.</summary>
        public int StagePressureLevel { get; private set; }

        /// <summary>Stage 단위 Overcharge 상태.</summary>
        public OverchargeState StageOverchargeState { get; }

        /// <summary>현재 Overcharge 수치.</summary>
        public int Overcharge => StageOverchargeState.CurrentOvercharge;

        /// <summary>현재 장착된 SlotPair Device 배열.</summary>
        public IReadOnlyListWrapper EquippedSlotPairDevices => new IReadOnlyListWrapper(equippedSlotPairDevices);

        /// <summary>RunSession을 생성한다.</summary>
        public TesseraRunSession(int startParts = 0, int playerMaxHp = 100)
        {
            Parts = Mathf.Max(0, startParts);
            PlayerMaxHp = Mathf.Max(1, playerMaxHp);
            PlayerCurrentHp = PlayerMaxHp;
            CurrentStageIndex = 0;
            RunChainCount = 0;
            StageChainCount = 0;
            StagePressureLevel = 0;
            StageOverchargeState = new OverchargeState();
        }

        /// <summary>현재 Stage 인덱스를 지정한다.</summary>
        public void SetCurrentStageIndex(int stageIndex, bool resetStageChain)
        {
            CurrentStageIndex = Mathf.Max(0, stageIndex);

            if (!resetStageChain)
                return;

            StageChainCount = 0;
            StagePressureLevel = 0;
        }

        /// <summary>Stage 시작 시 Overcharge를 초기화한다.</summary>
        public void ResetOverchargeForStageStart()
        {
            StageOverchargeState.ResetForStageStart();
        }

        /// <summary>보유 Parts를 증가시킨다.</summary>
        public void AddParts(int amount)
        {
            if (amount <= 0)
                return;

            Parts += amount;
        }

        /// <summary>지정 Parts를 지불할 수 있으면 차감한다.</summary>
        public bool TrySpendParts(int amount)
        {
            if (amount < 0)
                return false;

            if (Parts < amount)
                return false;

            Parts -= amount;
            return true;
        }

        /// <summary>Overcharge를 증가시킨다.</summary>
        public void AddOvercharge(int amount)
        {
            if (amount <= 0)
                return;

            StageOverchargeState.AddOvercharge(amount);
        }

        /// <summary>Overcharge 지불을 시도한다.</summary>
        public bool TrySpendOvercharge(int amount)
        {
            if (amount < 0)
                return false;

            return StageOverchargeState.TrySpendOvercharge(amount);
        }

        /// <summary>전투 종료 후 플레이어 HP를 반영한다.</summary>
        public void SetPlayerCurrentHp(int currentHp)
        {
            PlayerCurrentHp = Mathf.Clamp(currentHp, 0, PlayerMaxHp);
        }

        /// <summary>Cash Out 회복을 적용한다.</summary>
        public int HealByCashOutRatio(float ratio)
        {
            if (ratio <= 0f)
                return 0;

            int healAmount = Mathf.FloorToInt(PlayerMaxHp * ratio);
            int previousHp = PlayerCurrentHp;

            PlayerCurrentHp = Mathf.Min(PlayerMaxHp, PlayerCurrentHp + healAmount);
            return PlayerCurrentHp - previousHp;
        }

        /// <summary>Chain Rush 누적값을 반영한다.</summary>
        public void AddChainAndPressure(int chainAmount, int pressureAmount)
        {
            if (chainAmount > 0)
            {
                StageChainCount += chainAmount;
                RunChainCount += chainAmount;
            }

            if (pressureAmount > 0)
                StagePressureLevel += pressureAmount;
        }

        /// <summary>현재 장착 Device를 지정 슬롯에 강제로 설정한다.</summary>
        public bool SetEquippedDevice(int slotIndex, SlotPairDeviceDefinitionSO device)
        {
            if (!IsValidDeviceSlotIndex(slotIndex))
                return false;

            equippedSlotPairDevices[slotIndex] = device;
            return true;
        }

        /// <summary>현재 장착 Device 슬롯 두 개를 서로 교체한다.</summary>
        public bool SwapEquippedDevices(int sourceSlotIndex, int targetSlotIndex)
        {
            if (!IsValidDeviceSlotIndex(sourceSlotIndex))
                return false;

            if (!IsValidDeviceSlotIndex(targetSlotIndex))
                return false;

            if (sourceSlotIndex == targetSlotIndex)
                return true;

            SlotPairDeviceDefinitionSO temp = equippedSlotPairDevices[sourceSlotIndex];
            equippedSlotPairDevices[sourceSlotIndex] = equippedSlotPairDevices[targetSlotIndex];
            equippedSlotPairDevices[targetSlotIndex] = temp;

            return true;
        }

        /// <summary>첫 번째 빈 Device 슬롯에 Device를 장착한다.</summary>
        public bool TryEquipDeviceToFirstEmptySlot(SlotPairDeviceDefinitionSO device, out int equippedSlotIndex)
        {
            equippedSlotIndex = -1;

            if (device == null)
                return false;

            int emptySlotIndex = FindFirstEmptyDeviceSlotIndex();

            if (emptySlotIndex < 0)
                return false;

            equippedSlotPairDevices[emptySlotIndex] = device;
            equippedSlotIndex = emptySlotIndex;
            return true;
        }

        private int FindFirstEmptyDeviceSlotIndex()
        {
            for (int i = 0; i < equippedSlotPairDevices.Length; i++)
            {
                if (equippedSlotPairDevices[i] == null)
                    return i;
            }

            return -1;
        }

        private static bool IsValidDeviceSlotIndex(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < MaxDeviceSlots;
        }

        /// <summary>배열 노출을 최소화하기 위한 읽기 전용 래퍼다.</summary>
        public readonly struct IReadOnlyListWrapper
        {
            private readonly SlotPairDeviceDefinitionSO[] source;

            /// <summary>래퍼를 생성한다.</summary>
            public IReadOnlyListWrapper(SlotPairDeviceDefinitionSO[] source)
            {
                this.source = source;
            }

            /// <summary>요소 개수를 반환한다.</summary>
            public int Count => source != null ? source.Length : 0;

            /// <summary>지정 인덱스의 Device를 반환한다.</summary>
            public SlotPairDeviceDefinitionSO this[int index]
            {
                get
                {
                    if (source == null)
                        return null;

                    if (index < 0 || index >= source.Length)
                        return null;

                    return source[index];
                }
            }
        }
    }
}
