namespace Tessera.Core
{
    /// <summary>SlotPair 계산 중 외부 전투 상태 조건을 읽기 전용으로 전달한다.</summary>
    public class SlotPairCalculationContext
    {
        /// <summary>기본 SlotPair 계산 컨텍스트다.</summary>
        public static SlotPairCalculationContext Empty { get; } = new SlotPairCalculationContext(0);

        /// <summary>현재 Stage 내부 누적 위험도다.</summary>
        public int StageThreatLevel { get; }

        /// <summary>SlotPair 계산 컨텍스트를 생성한다.</summary>
        public SlotPairCalculationContext(int stageThreatLevel)
        {
            StageThreatLevel = stageThreatLevel < 0 ? 0 : stageThreatLevel;
        }
    }
}
