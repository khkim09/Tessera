namespace Tessera.Core
{
    /// <summary>SlotPair 계산 시 외부 전투 상태가 필요한 조건들을 전달하는 읽기 전용 컨텍스트다.</summary>
    public class SlotPairCalculationContext
    {
        public int StageThreatLevel { get; }

        public SlotPairCalculationContext(int stageThreatLevel)
        {
            StageThreatLevel = stageThreatLevel < 0 ? 0 : stageThreatLevel;
        }

        public static SlotPairCalculationContext Empty { get; } = new SlotPairCalculationContext(0);
    }
}
