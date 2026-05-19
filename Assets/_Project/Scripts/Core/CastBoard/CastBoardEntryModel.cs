namespace Tessera.Core
{
    /// <summary>Cast Board 한 행에 표시할 순수 데이터 모델이다.</summary>
    public class CastBoardEntryModel
    {
        /// <summary>Cast 카테고리 종류.</summary>
        public RollPatternType PatternType { get; }

        /// <summary>UI에 표시할 Cast 이름.</summary>
        public string DisplayName { get; }

        /// <summary>현재 Cast Board 행 상태.</summary>
        public CastBoardEntryStatus Status { get; }

        /// <summary>현재 주사위 조건상 제출 가능한지 여부.</summary>
        public bool IsConditionMet { get; }

        /// <summary>이번 Round 사용 제한상 제출 가능한지 여부.</summary>
        public bool IsUsageAllowed { get; }

        /// <summary>Table Rule에 의해 막혔는지 여부.</summary>
        public bool IsBlockedByTableRule { get; }

        /// <summary>이번 Round에서 이미 사용되었는지 여부.</summary>
        public bool IsUsedThisRound { get; }

        /// <summary>현재 사용 횟수.</summary>
        public int UseCount { get; }

        /// <summary>이번 Round 최대 사용 가능 횟수.</summary>
        public int MaxUseCount { get; }

        /// <summary>야추식 기본 Cast 점수.</summary>
        public int RawCastScore { get; }

        /// <summary>Cast 점수 계산에 포함된 주사위 합.</summary>
        public int IncludedDiceSum { get; }

        /// <summary>Table Rule 적용 전 피해량.</summary>
        public int DamageBeforeTableRules { get; }

        /// <summary>Table Rule 적용 후 실제 예상 피해량.</summary>
        public int DamageAfterTableRules { get; }

        /// <summary>현재 가능한 Cast 중 추천 Cast인지 여부.</summary>
        public bool IsRecommended { get; }

        /// <summary>UI에 표시할 비활성화 또는 규칙 설명.</summary>
        public string Message { get; }

        /// <summary>Cast Board 한 행의 표시 데이터를 생성한다.</summary>
        public CastBoardEntryModel(
            RollPatternType patternType,
            string displayName,
            CastBoardEntryStatus status,
            bool isConditionMet,
            bool isUsageAllowed,
            bool isBlockedByTableRule,
            bool isUsedThisRound,
            int useCount,
            int maxUseCount,
            int rawCastScore,
            int includedDiceSum,
            int damageBeforeTableRules,
            int damageAfterTableRules,
            bool isRecommended,
            string message)
        {
            PatternType = patternType;
            DisplayName = displayName;
            Status = status;
            IsConditionMet = isConditionMet;
            IsUsageAllowed = isUsageAllowed;
            IsBlockedByTableRule = isBlockedByTableRule;
            IsUsedThisRound = isUsedThisRound;
            UseCount = useCount;
            MaxUseCount = maxUseCount;
            RawCastScore = rawCastScore;
            IncludedDiceSum = includedDiceSum;
            DamageBeforeTableRules = damageBeforeTableRules;
            DamageAfterTableRules = damageAfterTableRules;
            IsRecommended = isRecommended;
            Message = message ?? string.Empty;
        }
    }
}
