namespace Tessera.Core
{
    /// <summary>테이블 규칙이 Cast에 적용된 결과를 담는다.</summary>
    public class TableRuleEvaluationResult
    {
        /// <summary>Cast 사용이 테이블 규칙에 의해 막혔는지 확인한다.</summary>
        public bool IsCastBlocked { get; }

        /// <summary>규칙 적용 전 CastPower 값이다.</summary>
        public int OriginalCastPower { get; }

        /// <summary>규칙 적용 후 CastPower 값이다.</summary>
        public int ModifiedCastPower { get; }

        /// <summary>Broken Cast 보상이 막혔는지 확인한다.</summary>
        public bool IsBrokenCastRewardSuppressed { get; }

        /// <summary>규칙 적용 설명.</summary>
        public string Message { get; }

        /// <summary>테이블 규칙 적용 결과를 생성한다.</summary>
        public TableRuleEvaluationResult(
            bool isCastBlocked,
            int originalCastPower,
            int modifiedCastPower,
            bool isBrokenCastRewardSuppressed,
            string message)
        {
            IsCastBlocked = isCastBlocked;
            OriginalCastPower = originalCastPower;
            ModifiedCastPower = modifiedCastPower;
            IsBrokenCastRewardSuppressed = isBrokenCastRewardSuppressed;
            Message = message ?? string.Empty;
        }
    }
}
