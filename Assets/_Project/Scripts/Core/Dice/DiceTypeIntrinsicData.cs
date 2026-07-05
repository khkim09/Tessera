namespace Tessera.Core
{
    /// <summary>Core 계산에 필요한 DiceType intrinsic 값만 담는 데이터다.</summary>
    public readonly struct DiceTypeIntrinsicData
    {
        /// <summary>효과가 없는 빈 DiceType intrinsic 데이터다.</summary>
        public static DiceTypeIntrinsicData Empty => new DiceTypeIntrinsicData(string.Empty, DiceIntrinsicEffectType.None, 0, 0f);

        /// <summary>디버그 표시용 DiceType 이름이다.</summary>
        public string DisplayName { get; }

        /// <summary>DiceType 고유 효과 타입이다.</summary>
        public DiceIntrinsicEffectType IntrinsicEffectType { get; }

        /// <summary>DiceType 고유 효과에 쓰이는 정수 값이다.</summary>
        public int IntValue { get; }

        /// <summary>DiceType 고유 효과에 쓰이는 실수 값이다.</summary>
        public float FloatValue { get; }

        /// <summary>실제 DiceType 데이터가 있는지 확인한다.</summary>
        public bool IsValid => IntrinsicEffectType != DiceIntrinsicEffectType.None || !string.IsNullOrEmpty(DisplayName);

        /// <summary>DiceType intrinsic 데이터 값을 생성한다.</summary>
        public DiceTypeIntrinsicData(string displayName, DiceIntrinsicEffectType intrinsicEffectType, int intValue, float floatValue)
        {
            DisplayName = displayName ?? string.Empty;
            IntrinsicEffectType = intrinsicEffectType;
            IntValue = intValue;
            FloatValue = floatValue;
        }
    }
}
