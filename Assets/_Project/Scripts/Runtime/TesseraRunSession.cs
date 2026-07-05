using System.Collections;
using System.Collections.Generic;
using Tessera.Core;
using Tessera.Data;
using UnityEngine;

namespace Tessera.Runtime
{
    /// <summary>한 Run 동안 유지되는 Stage, HP, Money, Overcharge, Chain, 장착 Device 상태를 관리한다.</summary>
    public class TesseraRunSession
    {
        public const int MaxDeviceSlots = 5;
        public const int PlayerDiceCount = 5;
        public const int DiceFaceCount = 6;

        private readonly SlotPairDeviceDefinitionSO[] equippedSlotPairDevices = new SlotPairDeviceDefinitionSO[MaxDeviceSlots];
        private readonly DiceTypeDefinitionSO[] equippedDiceTypes = new DiceTypeDefinitionSO[PlayerDiceCount];
        private readonly DiceFaceUpgradeDefinitionSO[,] equippedDiceFaceUpgrades = new DiceFaceUpgradeDefinitionSO[PlayerDiceCount, DiceFaceCount];

        /// <summary>개별 DiceType 변경 기능이 해금되었는지 나타낸다.</summary>
        private bool individualDiceTypeUpgradeUnlocked;

        /// <summary>마지막으로 전체 적용된 DiceSet 타입이다.</summary>
        private DiceTypeDefinitionSO currentDiceSetType;

        #region Public Getter

        /// <summary>현재 Stage 인덱스.</summary>
        public int CurrentStageIndex { get; private set; }

        /// <summary>현재 Stage 번호.</summary>
        public int CurrentStageNumber => CurrentStageIndex + 1;

        /// <summary>현재 보유 Money.</summary>
        public int Money { get; private set; }

        /// <summary>플레이어 최대 HP.</summary>
        public int PlayerMaxHP { get; private set; }

        /// <summary>플레이어 현재 HP.</summary>
        public int PlayerCurrentHP { get; private set; }

        /// <summary>Run 전체 Chain 누적값.</summary>
        public int RunChainCount { get; private set; }

        /// <summary>현재 Stage Chain 누적값.</summary>
        public int StageChainCount { get; private set; }

        /// <summary>현재 Stage 내부 누적 위험도.</summary>
        public int StageThreatLevel { get; private set; }

        /// <summary>현재 Workshop Tier.</summary>
        public int CurrentWorkshopTier { get; private set; }

        /// <summary>Stage/Run 단위 Overcharge 상태.</summary>
        public OverchargeState StageOverchargeState { get; }

        /// <summary>현재 Overcharge 수치.</summary>
        public int Overcharge => StageOverchargeState.CurrentOvercharge;

        /// <summary>현재 장착된 SlotPair Device 배열.</summary>
        public IReadOnlyListWrapper EquippedSlotPairDevices => new IReadOnlyListWrapper(equippedSlotPairDevices);

        /// <summary>현재 장착된 DiceType 배열.</summary>
        public IReadOnlyDiceTypeListWrapper EquippedDiceTypes => new IReadOnlyDiceTypeListWrapper(equippedDiceTypes);

        /// <summary>개별 DiceType 변경 기능이 해금되었는지 반환한다.</summary>
        public bool IsIndividualDiceTypeUpgradeUnlocked => individualDiceTypeUpgradeUnlocked;

        /// <summary>마지막으로 전체 적용된 DiceSet 타입을 반환한다.</summary>
        public DiceTypeDefinitionSO CurrentDiceSetType => currentDiceSetType;

        #endregion

        /// <summary>RunSession을 생성한다.</summary>
        public TesseraRunSession(int startMoney = 0, int playerMaxHP = 100)
        {
            Money = Mathf.Max(0, startMoney);
            PlayerMaxHP = Mathf.Max(1, playerMaxHP);
            PlayerCurrentHP = PlayerMaxHP;
            CurrentStageIndex = 0;
            RunChainCount = 0;
            StageChainCount = 0;
            StageThreatLevel = 0;
            CurrentWorkshopTier = 1;
            StageOverchargeState = new OverchargeState();
        }

        /// <summary>현재 Stage 인덱스를 지정한다.</summary>
        public void SetCurrentStageIndex(int stageIndex, bool resetStageChain)
        {
            CurrentStageIndex = Mathf.Max(0, stageIndex);

            if (!resetStageChain)
                return;

            ResetStageChainAndStageThreat();
        }

        /// <summary>
        /// 기존 Stage 시작 Overcharge 초기화 호출 호환용 메서드다.
        /// 최신 경제 구조에서 Overcharge는 Stage 시작/종료 시 초기화하지 않는다.
        /// </summary>
        public void ResetOverchargeForStageStart()
        {
        }

        /// <summary>보유 Money를 증가시킨다.</summary>
        public void AddMoney(int amount)
        {
            if (amount <= 0)
                return;

            Money += amount;
        }

        /// <summary>지정 Money를 지불할 수 있으면 차감한다.</summary>
        public bool TrySpendMoney(int amount)
        {
            if (amount < 0)
                return false;

            if (Money < amount)
                return false;

            Money -= amount;
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
        public void SetPlayerCurrentHP(int currentHP)
        {
            PlayerCurrentHP = Mathf.Clamp(currentHP, 0, PlayerMaxHP);
        }

        /// <summary>플레이어 HP를 최대치로 회복한다.</summary>
        public void RestorePlayerToFullHP()
        {
            PlayerCurrentHP = PlayerMaxHP;
        }

        /// <summary>최대 HP 비율만큼 현재 HP를 추가 회복한다.</summary>
        public int HealByRatio(float ratio)
        {
            if (ratio <= 0f)
                return 0;

            int healAmount = Mathf.FloorToInt(PlayerMaxHP * ratio);
            return RepairPlayerHP(healAmount);
        }

        /// <summary>현재 HP가 지정 비율 미만이면 해당 비율까지 보정한다.</summary>
        public int HealToMinimumRatio(float minimumRatio)
        {
            if (minimumRatio <= 0f)
                return 0;

            int targetHP = Mathf.FloorToInt(PlayerMaxHP * minimumRatio);
            int previousHP = PlayerCurrentHP;

            PlayerCurrentHP = Mathf.Clamp(Mathf.Max(PlayerCurrentHP, targetHP), 0, PlayerMaxHP);
            return PlayerCurrentHP - previousHP;
        }

        /// <summary>고정량만큼 플레이어 HP를 회복한다.</summary>
        public int RepairPlayerHP(int healAmount)
        {
            if (healAmount <= 0)
                return 0;

            int previousHP = PlayerCurrentHP;
            PlayerCurrentHP = Mathf.Min(PlayerMaxHP, PlayerCurrentHP + healAmount);
            return PlayerCurrentHP - previousHP;
        }

        /// <summary>기존 CashOut 회복 호출 호환용 메서드다. 신규 코드는 HealByRatio를 사용한다.</summary>
        public int HealByCashOutRatio(float ratio)
        {
            return HealByRatio(ratio);
        }

        /// <summary>Chain Rush 누적값과 StageThreat를 반영한다.</summary>
        public void AddChainAndStageThreat(int chainAmount, int stageThreatAmount)
        {
            if (chainAmount > 0)
            {
                StageChainCount += chainAmount;
                RunChainCount += chainAmount;
            }

            if (stageThreatAmount > 0)
                StageThreatLevel += stageThreatAmount;
        }

        /// <summary>현재 Stage의 Chain과 StageThreat를 명시적으로 동기화한다.</summary>
        public void SetStageChainAndThreat(int chainCount, int stageThreatLevel)
        {
            StageChainCount = Mathf.Max(0, chainCount);
            StageThreatLevel = Mathf.Max(0, stageThreatLevel);
        }

        /// <summary>현재 Stage의 Chain과 StageThreat를 초기화한다.</summary>
        public void ResetStageChainAndStageThreat()
        {
            StageChainCount = 0;
            StageThreatLevel = 0;
        }

        /// <summary>Overcharge를 지불하고 Workshop Tier 상승을 시도한다.</summary>
        public bool TryUpgradeWorkshopTier(int overchargeCost)
        {
            if (overchargeCost < 0)
                return false;

            if (!TrySpendOvercharge(overchargeCost))
                return false;

            CurrentWorkshopTier++;
            return true;
        }

        /// <summary>현재 Workshop Tier를 초기화한다.</summary>
        public void ResetWorkshopTier()
        {
            CurrentWorkshopTier = 1;
        }

        /// <summary>현재 장착 Device를 지정 슬롯에 강제로 설정한다.</summary>
        public bool SetEquippedDevice(int slotIndex, SlotPairDeviceDefinitionSO device)
        {
            if (!IsValidDeviceSlotIndex(slotIndex))
                return false;

            equippedSlotPairDevices[slotIndex] = device;
            return true;
        }

        /// <summary>초기 장착 Device 배열을 RunSession에 복사한다. 디버그 시작 장비 또는 세이브 로드 복원에 사용한다.</summary>
        public void SetEquippedDevices(IReadOnlyList<SlotPairDeviceDefinitionSO> devices)
        {
            for (int i = 0; i < equippedSlotPairDevices.Length; i++)
            {
                equippedSlotPairDevices[i] = devices != null && i < devices.Count
                    ? devices[i]
                    : null;
            }
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

        /// <summary>현재 빈 Device 슬롯이 있는지 확인한다.</summary>
        public bool HasEmptyDeviceSlot()
        {
            return FindFirstEmptyDeviceSlotIndex() >= 0;
        }

        /// <summary>현재 장착된 Device 개수를 반환한다.</summary>
        public int GetEquippedDeviceCount()
        {
            int count = 0;

            for (int i = 0; i < equippedSlotPairDevices.Length; i++)
            {
                if (equippedSlotPairDevices[i] != null)
                    count++;
            }

            return count;
        }

        /// <summary>지정 슬롯의 Device를 반환한다.</summary>
        public SlotPairDeviceDefinitionSO GetEquippedDevice(int slotIndex)
        {
            if (!IsValidDeviceSlotIndex(slotIndex))
                return null;

            return equippedSlotPairDevices[slotIndex];
        }

        /// <summary>지정 슬롯에 Device를 배치하고 기존 Device를 반환한다.</summary>
        public bool TryPlaceDeviceToSlot(
            int slotIndex,
            SlotPairDeviceDefinitionSO device,
            out SlotPairDeviceDefinitionSO previousDevice)
        {
            previousDevice = null;

            if (!IsValidDeviceSlotIndex(slotIndex))
                return false;

            if (device == null)
                return false;

            previousDevice = equippedSlotPairDevices[slotIndex];
            equippedSlotPairDevices[slotIndex] = device;
            return true;
        }

        /// <summary>첫 번째 빈 Device 슬롯에 Device를 장착한다.</summary>
        public bool TryEquipDeviceToFirstEmptySlot(
            SlotPairDeviceDefinitionSO device,
            out int equippedSlotIndex)
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

        /// <summary>지정 Device 슬롯을 비우고 제거된 Device를 반환한다.</summary>
        public bool ClearEquippedDeviceSlot(
            int slotIndex,
            out SlotPairDeviceDefinitionSO removedDevice)
        {
            removedDevice = null;

            if (!IsValidDeviceSlotIndex(slotIndex))
                return false;

            removedDevice = equippedSlotPairDevices[slotIndex];
            equippedSlotPairDevices[slotIndex] = null;
            return removedDevice != null;
        }

        /// <summary>현재 Overcharge 값을 직접 지정한다.</summary>
        public void SetCurrentOvercharge(int value)
        {
            StageOverchargeState.SetCurrentOvercharge(Mathf.Max(0, value));
        }

        /// <summary>현재 Workshop Tier를 지정한다.</summary>
        public void SetWorkshopTier(int tier)
        {
            CurrentWorkshopTier = Mathf.Max(1, tier);
        }

        /// <summary>개별 DiceType 변경 기능을 해금한다.</summary>
        public void UnlockIndividualDiceTypeUpgrade()
        {
            individualDiceTypeUpgradeUnlocked = true;
        }

        /// <summary>개별 DiceType 변경 기능을 잠근다.</summary>
        public void LockIndividualDiceTypeUpgrade()
        {
            individualDiceTypeUpgradeUnlocked = false;
        }

        /// <summary>지정 DiceIndex의 DiceType을 강제로 설정한다.</summary>
        public bool SetEquippedDiceType(int diceIndex, DiceTypeDefinitionSO diceType)
        {
            if (!IsValidDiceIndex(diceIndex))
                return false;

            equippedDiceTypes[diceIndex] = diceType;
            return true;
        }

        /// <summary>5개 주사위 전체 DiceType을 동일 타입으로 교체한다.</summary>
        public bool SetAllEquippedDiceTypes(DiceTypeDefinitionSO diceType)
        {
            if (diceType == null)
                return false;

            currentDiceSetType = diceType;

            for (int i = 0; i < equippedDiceTypes.Length; i++)
                equippedDiceTypes[i] = diceType;

            return true;
        }

        /// <summary>DiceSet 상품 구매용으로 전체 DiceType을 교체한다.</summary>
        public bool SetDiceSetType(DiceTypeDefinitionSO diceType)
        {
            return SetAllEquippedDiceTypes(diceType);
        }

        /// <summary>개별 DiceType 변경 해금 상태에서 특정 DiceIndex의 DiceType 변경을 시도한다.</summary>
        public bool TrySetDiceTypeForSlot(int diceIndex, DiceTypeDefinitionSO diceType)
        {
            if (!individualDiceTypeUpgradeUnlocked)
                return false;

            if (!IsValidDiceIndex(diceIndex))
                return false;

            if (diceType == null)
                return false;

            equippedDiceTypes[diceIndex] = diceType;
            return true;
        }

        /// <summary>
        /// Shop의 SingleDice/DiceTypeUpgrade 상품 구매용으로 대상 선택 UI 없이 적용할 Dice 슬롯을 자동 선택한다.
        /// 구매한 DiceType과 다른 첫 번째 슬롯을 우선 사용하고, 모두 동일하면 첫 번째 슬롯에 재적용한다.
        /// </summary>
        public bool TryApplyPurchasedIndividualDiceType(
            DiceTypeDefinitionSO diceType,
            out int appliedDiceIndex,
            out DiceTypeDefinitionSO previousDiceType)
        {
            appliedDiceIndex = -1;
            previousDiceType = null;

            if (diceType == null)
                return false;

            int targetIndex = FindFirstDiceTypeMismatchIndex(diceType);
            if (targetIndex < 0)
                targetIndex = 0;

            previousDiceType = equippedDiceTypes[targetIndex];
            equippedDiceTypes[targetIndex] = diceType;
            appliedDiceIndex = targetIndex;
            return true;
        }

        /// <summary>지정 DiceIndex의 DiceType을 반환한다.</summary>
        public DiceTypeDefinitionSO GetEquippedDiceType(int diceIndex)
        {
            if (!IsValidDiceIndex(diceIndex))
                return null;

            return equippedDiceTypes[diceIndex];
        }

        /// <summary>지정 DiceIndex의 현재 DiceType을 반환한다.</summary>
        public DiceTypeDefinitionSO GetDiceTypeForSlot(int diceIndex)
        {
            return GetEquippedDiceType(diceIndex);
        }

        /// <summary>특정 Dice의 특정 FaceIndex에 FaceUpgrade를 장착한다.</summary>
        public bool SetDiceFaceUpgrade(
            int diceIndex,
            int faceIndex,
            DiceFaceUpgradeDefinitionSO upgradeDefinition)
        {
            if (!IsValidDiceIndex(diceIndex))
                return false;

            if (!IsValidFaceIndex(faceIndex))
                return false;

            equippedDiceFaceUpgrades[diceIndex, faceIndex] = upgradeDefinition;
            return true;
        }

        /// <summary>특정 Dice의 특정 FaceIndex에 장착된 FaceUpgrade를 반환한다.</summary>
        public DiceFaceUpgradeDefinitionSO GetDiceFaceUpgrade(int diceIndex, int faceIndex)
        {
            if (!IsValidDiceIndex(diceIndex))
                return null;

            if (!IsValidFaceIndex(faceIndex))
                return null;

            return equippedDiceFaceUpgrades[diceIndex, faceIndex];
        }

        /// <summary>특정 Dice의 특정 FaceIndex 각인을 제거한다.</summary>
        public bool ClearDiceFaceUpgrade(int diceIndex, int faceIndex)
        {
            if (!IsValidDiceIndex(diceIndex))
                return false;

            if (!IsValidFaceIndex(faceIndex))
                return false;

            equippedDiceFaceUpgrades[diceIndex, faceIndex] = null;
            return true;
        }

        private static bool IsValidDiceIndex(int diceIndex)
        {
            return diceIndex >= 0 && diceIndex < PlayerDiceCount;
        }

        private static bool IsValidFaceIndex(int faceIndex)
        {
            return faceIndex >= 0 && faceIndex < DiceFaceCount;
        }

        /// <summary>지정 DiceType과 다른 첫 번째 Dice 슬롯 인덱스를 찾는다.</summary>
        private int FindFirstDiceTypeMismatchIndex(DiceTypeDefinitionSO diceType)
        {
            if (diceType == null)
                return -1;

            for (int i = 0; i < equippedDiceTypes.Length; i++)
            {
                if (equippedDiceTypes[i] != diceType)
                    return i;
            }

            return -1;
        }

        /// <summary>첫 번째 빈 Device 슬롯 인덱스를 찾는다.</summary>
        private int FindFirstEmptyDeviceSlotIndex()
        {
            for (int i = 0; i < equippedSlotPairDevices.Length; i++)
            {
                if (equippedSlotPairDevices[i] == null)
                    return i;
            }

            return -1;
        }

        /// <summary>Device 슬롯 인덱스가 유효한지 확인한다.</summary>
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

        /// <summary>DiceType 배열 노출을 최소화하기 위한 읽기 전용 래퍼다.</summary>
        public readonly struct IReadOnlyDiceTypeListWrapper : IReadOnlyList<DiceTypeDefinitionSO>
        {
            private readonly DiceTypeDefinitionSO[] source;

            /// <summary>DiceType 읽기 전용 래퍼를 생성한다.</summary>
            public IReadOnlyDiceTypeListWrapper(DiceTypeDefinitionSO[] source)
            {
                this.source = source;
            }

            /// <summary>DiceType 요소 개수를 반환한다.</summary>
            public int Count => source != null ? source.Length : 0;

            /// <summary>지정 인덱스의 DiceType을 반환한다.</summary>
            public DiceTypeDefinitionSO this[int index]
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
            /// <summary>DiceType 목록 순회를 위한 제네릭 열거자를 반환한다.</summary>
            public IEnumerator<DiceTypeDefinitionSO> GetEnumerator()
            {
                for (int i = 0; i < Count; i++)
                    yield return this[i];
            }

            /// <summary>DiceType 목록 순회를 위한 비제네릭 열거자를 반환한다.</summary>
            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
