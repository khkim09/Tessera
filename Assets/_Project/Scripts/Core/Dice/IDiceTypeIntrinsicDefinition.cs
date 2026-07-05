namespace Tessera.Core
{
    /// <summary>Core 전투 계산이 DiceType SO를 직접 참조하지 않고 intrinsic 값만 읽기 위한 계약이다.</summary>
    public interface IDiceTypeIntrinsicDefinition
    {
        /// <summary>디버그 표시용 DiceType 이름이다.</summary>
        string DisplayName { get; }

        /// <summary>DiceType 고유 효과 타입이다.</summary>
        DiceIntrinsicEffectType IntrinsicEffectType { get; }

        /// <summary>DiceType 고유 효과에 쓰이는 정수 값이다.</summary>
        int IntValue { get; }

        /// <summary>DiceType 고유 효과에 쓰이는 실수 값이다.</summary>
        float FloatValue { get; }
    }
}
