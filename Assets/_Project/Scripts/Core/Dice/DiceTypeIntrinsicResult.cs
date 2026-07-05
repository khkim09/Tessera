using System;

namespace Tessera.Core
{
    /// <summary>DiceType 고유 효과 계산에서 분리된 전투 보정값을 담는다.</summary>
    public readonly struct DiceTypeIntrinsicResult
    {
        /// <summary>아무 보정도 없는 DiceType 고유 효과 결과다.</summary>
        public static DiceTypeIntrinsicResult None => new DiceTypeIntrinsicResult(0, 0f, 1f, 0, 0, DiceIntrinsicEffectType.None, string.Empty);

        /// <summary>Score에 더할 고정 보정값이다.</summary>
        public int ScoreBonus { get; }

        /// <summary>Force에 더할 고정 보정값이다.</summary>
        public float ForceAdd { get; }

        /// <summary>Force에 곱할 배율 보정값이다.</summary>
        public float ForceMultiplier { get; }

        /// <summary>Round 승리 시 Money 보상에 더할 보정값이다.</summary>
        public int MoneyOnRoundWinBonus { get; }

        /// <summary>플레이어가 받는 최종 피해에서 차감할 보정값이다.</summary>
        public int IncomingDamageReduction { get; }

        /// <summary>적용된 DiceType 고유 효과 타입이다.</summary>
        public DiceIntrinsicEffectType EffectType { get; }

        /// <summary>디버그 로그에 사용할 짧은 적용 메시지다.</summary>
        public string Message { get; }

        /// <summary>DiceType 고유 효과 계산 결과를 생성한다.</summary>
        public DiceTypeIntrinsicResult(
            int scoreBonus,
            float forceAdd,
            float forceMultiplier,
            int moneyOnRoundWinBonus,
            int incomingDamageReduction,
            DiceIntrinsicEffectType effectType,
            string message)
        {
            ScoreBonus = Math.Max(0, scoreBonus);
            ForceAdd = Math.Max(0f, forceAdd);
            ForceMultiplier = forceMultiplier <= 0f ? 1f : forceMultiplier;
            MoneyOnRoundWinBonus = Math.Max(0, moneyOnRoundWinBonus);
            IncomingDamageReduction = Math.Max(0, incomingDamageReduction);
            EffectType = effectType;
            Message = message ?? string.Empty;
        }

        /// <summary>계산 결과에 실제 전투 보정이 있는지 확인한다.</summary>
        public bool HasBattleAdjustment => ScoreBonus > 0 || ForceAdd > 0f || Math.Abs(ForceMultiplier - 1f) > 0.001f || MoneyOnRoundWinBonus > 0 || IncomingDamageReduction > 0;
    }
}
