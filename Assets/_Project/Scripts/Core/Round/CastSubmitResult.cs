namespace Tessera.Core
{
    /// <summary>Cast 제출 후 피해, 보상, 상대 Intent, Round 진행 결과를 담는다.</summary>
    public class CastSubmitResult
    {
        /// <summary>Cast를 제출한 Attempt 번호.</summary>
        public int AttemptNumber { get; }

        /// <summary>선택된 Cast 판정 결과.</summary>
        public PatternResult PatternResult { get; }

        /// <summary>테이블 규칙 적용 결과.</summary>
        public TableRuleEvaluationResult TableRuleEvaluationResult { get; }

        /// <summary>상대에게 실제 적용된 피해량.</summary>
        public int DamageApplied { get; }

        /// <summary>피해 적용 후 상대 HP.</summary>
        public int OpponentHpAfterDamage { get; }

        /// <summary>Broken Cast인지 확인한다.</summary>
        public bool IsBrokenCast { get; }

        /// <summary>이번 제출로 Overcharge가 지급되었는지 확인한다.</summary>
        public bool DidGrantOvercharge { get; }

        /// <summary>이번 제출로 지급된 Overcharge 양.</summary>
        public int GrantedOverchargeAmount { get; }

        /// <summary>이번 제출로 다음 Attempt에 지급된 무료 리롤 토큰 수.</summary>
        public int GrantedNextAttemptFreeRerollTokens { get; }

        /// <summary>상대 Intent 실행 결과.</summary>
        public EnemyIntentResult EnemyIntentResult { get; }

        /// <summary>이 제출 후 Round 결과 타입.</summary>
        public RoundOutcomeType OutcomeType { get; }

        /// <summary>이 제출 후 Round 승리 상태인지 확인한다.</summary>
        public bool IsRoundWon => OutcomeType == RoundOutcomeType.Won;

        /// <summary>이 제출 후 Round 패배 상태인지 확인한다.</summary>
        public bool IsRoundLost => OutcomeType == RoundOutcomeType.Lost;

        /// <summary>이 제출 후 Round가 계속 진행 중인지 확인한다.</summary>
        public bool IsRoundOngoing => OutcomeType == RoundOutcomeType.Ongoing;

        /// <summary>다음 Attempt로 진행할 수 있는지 확인한다.</summary>
        public bool CanStartNextAttempt { get; }

        /// <summary>디버그 표시용 메시지.</summary>
        public string Message { get; }

        /// <summary>Cast 제출 결과를 생성한다.</summary>
        public CastSubmitResult(
            int attemptNumber,
            PatternResult patternResult,
            TableRuleEvaluationResult tableRuleEvaluationResult,
            int damageApplied,
            int opponentHpAfterDamage,
            bool isBrokenCast,
            bool didGrantOvercharge,
            int grantedOverchargeAmount,
            int grantedNextAttemptFreeRerollTokens,
            EnemyIntentResult enemyIntentResult,
            RoundOutcomeType outcomeType,
            bool canStartNextAttempt,
            string message)
        {
            AttemptNumber = attemptNumber;
            PatternResult = patternResult;
            TableRuleEvaluationResult = tableRuleEvaluationResult;
            DamageApplied = damageApplied;
            OpponentHpAfterDamage = opponentHpAfterDamage;
            IsBrokenCast = isBrokenCast;
            DidGrantOvercharge = didGrantOvercharge;
            GrantedOverchargeAmount = grantedOverchargeAmount;
            GrantedNextAttemptFreeRerollTokens = grantedNextAttemptFreeRerollTokens;
            EnemyIntentResult = enemyIntentResult;
            OutcomeType = outcomeType;
            CanStartNextAttempt = canStartNextAttempt;
            Message = message;
        }
    }
}
