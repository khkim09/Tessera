namespace Tessera.Core
{
    /// <summary>SlotPair 한 칸의 계산 전후 Score, Force, TrueDamage 변화를 기록한다.</summary>
    public class SlotPairDamageStep
    {
        public int SlotIndex { get; }
        public int DiceIndex { get; }
        public int DiceValue { get; }

        public SlotPairDeviceType DeviceType { get; }

        public int ScoreBefore { get; }
        public int ScoreAfter { get; }

        public float ForceBefore { get; }
        public float ForceAfter { get; }

        public int TrueDamageBefore { get; }
        public int TrueDamageAfter { get; }

        public bool DidApply { get; }
        public string Message { get; }

        public int AddedScore => ScoreAfter - ScoreBefore;
        public float AddedForce => ForceAfter - ForceBefore;
        public int AddedTrueDamage => TrueDamageAfter - TrueDamageBefore;

        public SlotPairDamageStep(
            int slotIndex,
            int diceIndex,
            int diceValue,
            SlotPairDeviceType deviceType,
            int scoreBefore,
            int scoreAfter,
            float forceBefore,
            float forceAfter,
            int trueDamageBefore,
            int trueDamageAfter,
            bool didApply,
            string message)
        {
            SlotIndex = slotIndex;
            DiceIndex = diceIndex;
            DiceValue = diceValue;
            DeviceType = deviceType;
            ScoreBefore = scoreBefore;
            ScoreAfter = scoreAfter;
            ForceBefore = forceBefore;
            ForceAfter = forceAfter;
            TrueDamageBefore = trueDamageBefore;
            TrueDamageAfter = trueDamageAfter;
            DidApply = didApply;
            Message = message ?? string.Empty;
        }
    }
}
