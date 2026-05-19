namespace Tessera.Core
{
    /// <summary>SlotPair 한 칸의 계산 전후 Score와 Force 변화를 기록한다.</summary>
    public class SlotPairDamageStep
    {
        /// <summary>0부터 시작하는 SlotPair 인덱스.</summary>
        public int SlotIndex { get; }

        /// <summary>해당 슬롯에 배치된 원본 주사위 인덱스.</summary>
        public int DiceIndex { get; }

        /// <summary>해당 슬롯에 배치된 주사위 값.</summary>
        public int DiceValue { get; }

        /// <summary>해당 슬롯에 대응된 Device 타입.</summary>
        public SlotPairDeviceType DeviceType { get; }

        /// <summary>Device 적용 전 Score.</summary>
        public int ScoreBefore { get; }

        /// <summary>Device 적용 후 Score.</summary>
        public int ScoreAfter { get; }

        /// <summary>Device 적용 전 Force.</summary>
        public float ForceBefore { get; }

        /// <summary>Device 적용 후 Force.</summary>
        public float ForceAfter { get; }

        /// <summary>Device 효과가 실제 적용되었는지 확인한다.</summary>
        public bool DidApply { get; }

        /// <summary>디버그 및 추후 연출용 메시지.</summary>
        public string Message { get; }

        /// <summary>SlotPair 계산 단계 정보를 생성한다.</summary>
        public SlotPairDamageStep(
            int slotIndex,
            int diceIndex,
            int diceValue,
            SlotPairDeviceType deviceType,
            int scoreBefore,
            int scoreAfter,
            float forceBefore,
            float forceAfter,
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
            DidApply = didApply;
            Message = message ?? string.Empty;
        }
    }
}
