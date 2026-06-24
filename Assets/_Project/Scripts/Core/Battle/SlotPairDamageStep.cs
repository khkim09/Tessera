namespace Tessera.Core
{
    /// <summary>SlotPair 한 칸의 계산 전후 Score, Force, TruePower 변화를 기록한다.</summary>
    public class SlotPairDamageStep
    {
        /// <summary>계산 대상 SlotPair 인덱스다.</summary>
        public int SlotIndex { get; }

        /// <summary>해당 슬롯에 배치된 DiceIndex다.</summary>
        public int DiceIndex { get; }

        /// <summary>해당 슬롯에 배치된 주사위 눈금이다.</summary>
        public int DiceValue { get; }

        /// <summary>이번 슬롯에서 평가한 Device 타입이다.</summary>
        public SlotPairDeviceType DeviceType { get; }

        /// <summary>Device 적용 전 Score 값이다.</summary>
        public int ScoreBefore { get; }

        /// <summary>Device 적용 후 Score 값이다.</summary>
        public int ScoreAfter { get; }

        /// <summary>Device 적용 전 Force 값이다.</summary>
        public float ForceBefore { get; }

        /// <summary>Device 적용 후 Force 값이다.</summary>
        public float ForceAfter { get; }

        /// <summary>Device 적용 전 고정 Power 값이다.</summary>
        public int TruePowerBefore { get; }

        /// <summary>Device 적용 후 고정 Power 값이다.</summary>
        public int TruePowerAfter { get; }

        /// <summary>이번 Device가 실제로 적용되었는지 여부다.</summary>
        public bool DidApply { get; }

        /// <summary>계산 결과 설명 메시지다.</summary>
        public string Message { get; }

        /// <summary>이번 Step에서 추가된 Score 값이다.</summary>
        public int AddedScore => ScoreAfter - ScoreBefore;

        /// <summary>이번 Step에서 추가된 Force 값이다.</summary>
        public float AddedForce => ForceAfter - ForceBefore;

        /// <summary>이번 Step에서 추가된 고정 Power 값이다.</summary>
        public int AddedTruePower => TruePowerAfter - TruePowerBefore;

        /// <summary>SlotPair Step 계산 결과를 생성한다.</summary>
        public SlotPairDamageStep(
            int slotIndex,
            int diceIndex,
            int diceValue,
            SlotPairDeviceType deviceType,
            int scoreBefore,
            int scoreAfter,
            float forceBefore,
            float forceAfter,
            int truePowerBefore,
            int truePowerAfter,
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
            TruePowerBefore = truePowerBefore;
            TruePowerAfter = truePowerAfter;
            DidApply = didApply;
            Message = message ?? string.Empty;
        }
    }
}
