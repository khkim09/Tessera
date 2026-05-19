namespace Tessera.Core
{
    /// <summary>테이블 규칙이 Cast에 적용된 결과를 담는다.</summary>
    public class TableRuleEvaluationResult
    {
        /// <summary>Cast 사용이 테이블 규칙에 의해 막혔는지 확인한다.</summary>
        public bool IsCastBlocked { get; }

        /// <summary>규칙 적용 전 피해량.</summary>
        public int OriginalDamage { get; }

        /// <summary>규칙 적용 후 피해량.</summary>
        public int ModifiedDamage { get; }

        /// <summary>Broken Cast 보상이 막혔는지 확인한다.</summary>
        public bool IsBrokenCastRewardSuppressed { get; }

        /// <summary>규칙 적용 설명.</summary>
        public string Message { get; }

        /// <summary>테이블 규칙 적용 결과를 생성한다.</summary>
        public TableRuleEvaluationResult(
            bool isCastBlocked,
            int originalDamage,
            int modifiedDamage,
            bool isBrokenCastRewardSuppressed,
            string message)
        {
            IsCastBlocked = isCastBlocked;
            OriginalDamage = originalDamage;
            ModifiedDamage = modifiedDamage;
            IsBrokenCastRewardSuppressed = isBrokenCastRewardSuppressed;
            Message = message ?? string.Empty;
        }
    }
}
