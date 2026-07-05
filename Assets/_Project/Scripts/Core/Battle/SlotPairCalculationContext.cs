using System.Collections.Generic;

namespace Tessera.Core
{
    /// <summary>SlotPair 계산 중 외부 전투 상태 조건을 읽기 전용으로 전달한다.</summary>
    public class SlotPairCalculationContext
    {
        /// <summary>기본 SlotPair 계산 컨텍스트다.</summary>
        public static SlotPairCalculationContext Empty { get; } = new SlotPairCalculationContext(0, null);

        private readonly List<DiceTypeIntrinsicData> equippedDiceTypes;

        /// <summary>현재 Stage 내부 누적 위험도다.</summary>
        public int StageThreatLevel { get; }

        /// <summary>DiceIndex별 장착 DiceType 목록이다.</summary>
        public IReadOnlyList<DiceTypeIntrinsicData> EquippedDiceTypes => equippedDiceTypes;

        /// <summary>SlotPair 계산 컨텍스트를 생성한다.</summary>
        public SlotPairCalculationContext(int stageThreatLevel)
            : this(stageThreatLevel, null)
        {
        }

        /// <summary>DiceType까지 포함한 SlotPair 계산 컨텍스트를 생성한다.</summary>
        public SlotPairCalculationContext(int stageThreatLevel, IReadOnlyList<DiceTypeIntrinsicData> equippedDiceTypes)
        {
            StageThreatLevel = stageThreatLevel < 0 ? 0 : stageThreatLevel;
            this.equippedDiceTypes = equippedDiceTypes != null
                ? new List<DiceTypeIntrinsicData>(equippedDiceTypes)
                : new List<DiceTypeIntrinsicData>();
        }

        /// <summary>지정 DiceIndex에 장착된 DiceType을 반환한다.</summary>
        public DiceTypeIntrinsicData GetDiceType(int diceIndex)
        {
            if (diceIndex < 0 || diceIndex >= equippedDiceTypes.Count)
                return DiceTypeIntrinsicData.Empty;

            return equippedDiceTypes[diceIndex];
        }
    }
}
