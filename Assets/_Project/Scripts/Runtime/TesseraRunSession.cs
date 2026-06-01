using Tessera.Core;
using Tessera.Data;
using UnityEngine;

namespace Tessera.Runtime
{
    /// <summary>한 Run 동안 유지되는 Stage, HP, Money, Overcharge, Chain, 장착 Device 상태를 관리한다.</summary>
    public class TesseraRunSession
    {
        public const int MaxDeviceSlots = 5;

        private readonly SlotPairDeviceDefinitionSO[] equippedSlotPairDevices = new SlotPairDeviceDefinitionSO[MaxDeviceSlots];

        /// <summary>현재 Stage 인덱스.</summary>
        public int CurrentStageIndex { get; private set; }

        /// <summary>현재 Stage 번호.</summary>
        public int CurrentStageNumber => CurrentStageIndex + 1;

        /// <summary>현재 보유 Money.</summary>
        public int Money { get; private set; }

        /// <summary>기존 Parts 기반 코드 호환용 접근자다. 신규 코드는 Money를 사용한다.</summary>
        public int Parts => Money;

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

        /// <summary>기존 Pressure 기반 코드 호환용 접근자다. 신규 코드는 StageThreatLevel을 사용한다.</summary>
        public int StagePressureLevel => StageThreatLevel;

        /// <summary>현재 Workshop Tier.</summary>
        public int CurrentWorkshopTier { get; private set; }

        /// <summary>Stage/Run 단위 Overcharge 상태.</summary>
        public OverchargeState StageOverchargeState { get; }

        /// <summary>현재 Overcharge 수치.</summary>
        public int Overcharge => StageOverchargeState.CurrentOvercharge;

        /// <summary>현재 장착된 SlotPair Device 배열.</summary>
        public IReadOnlyListWrapper EquippedSlotPairDevices => new IReadOnlyListWrapper(equippedSlotPairDevices);

        /// <summary>RunSession을 생성한다.</summary>
        public TesseraRunSession(int startParts = 0, int playerMaxHP = 100)
        {
            Money = Mathf.Max(0, startParts);
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

        /// <summary>기존 Parts 기반 코드 호환용 메서드다. 신규 코드는 AddMoney를 사용한다.</summary>
        public void AddParts(int amount)
        {
            AddMoney(amount);
        }

        /// <summary>기존 Parts 기반 코드 호환용 메서드다. 신규 코드는 TrySpendMoney를 사용한다.</summary>
        public bool TrySpendParts(int amount)
        {
            return TrySpendMoney(amount);
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

        /// <summary>기존 Pressure 기반 코드 호환용 메서드다. 신규 코드는 AddChainAndStageThreat를 사용한다.</summary>
        public void AddChainAndPressure(int chainAmount, int pressureAmount)
        {
            AddChainAndStageThreat(chainAmount, pressureAmount);
        }

        /// <summary>기존 Pressure 기반 코드 호환용 메서드다. 신규 코드는 ResetStageChainAndStageThreat를 사용한다.</summary>
        public void ResetStageChainAndPressure()
        {
            ResetStageChainAndStageThreat();
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
    }
}
