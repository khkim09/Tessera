namespace Tessera.Core
{
    /// <summary>Cast Board 한 행에 표시할 순수 데이터 모델이다.</summary>
    public class CastBoardEntryModel
    {
        /// <summary>Cast 카테고리 종류다.</summary>
        public RollPatternType PatternType { get; }

        /// <summary>UI에 표시할 Cast 이름이다.</summary>
        public string DisplayName { get; }

        /// <summary>현재 Cast Board 행 상태다.</summary>
        public CastBoardEntryStatus Status { get; }

        /// <summary>현재 주사위 조건상 제출 가능한지 여부다.</summary>
        public bool IsConditionMet { get; }

        /// <summary>이번 Round 사용 제한상 제출 가능한지 여부다.</summary>
        public bool IsUsageAllowed { get; }

        /// <summary>TableRule에 의해 막혔는지 여부다.</summary>
        public bool IsBlockedByTableRule { get; }

        /// <summary>이번 Round에서 이미 사용되었는지 여부다.</summary>
        public bool IsUsedThisRound { get; }

        /// <summary>현재 사용 횟수다.</summary>
        public int UseCount { get; }

        /// <summary>이번 Round 최대 사용 가능 횟수다.</summary>
        public int MaxUseCount { get; }

        /// <summary>야찌식 기본 Cast Score 값이다.</summary>
        public int RawCastScore { get; }

        /// <summary>Cast Score 계산에 포함된 주사위 합이다.</summary>
        public int IncludedDiceSum { get; }

        /// <summary>TableRule 적용 전 CastPower 값이다.</summary>
        public int CastPowerBeforeTableRules { get; }

        /// <summary>TableRule 적용 후 CastPower 값이다.</summary>
        public int CastPowerAfterTableRules { get; }

        /// <summary>현재 가능한 Cast 중 추천 Cast인지 여부다.</summary>
        public bool IsRecommended { get; }

        /// <summary>UI에 표시할 비활성화 또는 규칙 설명이다.</summary>
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
            int castPowerBeforeTableRules,
            int castPowerAfterTableRules,
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
            CastPowerBeforeTableRules = castPowerBeforeTableRules;
            CastPowerAfterTableRules = castPowerAfterTableRules;
            IsRecommended = isRecommended;
            Message = message ?? string.Empty;
        }
    }
}
