namespace Tessera.Core
{
    /// <summary>Player와 Opponent의 CastPower를 비교한 Clash 판정 결과다.</summary>
    public class ClashResolveResult
    {
        /// <summary>Clash가 해결된 Attempt 번호다.</summary>
        public int AttemptNumber { get; }

        /// <summary>플레이어 Cast 결과다.</summary>
        public ClashCastResult PlayerResult { get; }

        /// <summary>상대 Cast 결과다.</summary>
        public ClashCastResult OpponentResult { get; }

        /// <summary>Clash 승자이며 무승부면 null이다.</summary>
        public ClashParticipantType? Winner { get; }

        /// <summary>플레이어가 가한 ImpactDamage 계산 내역이다.</summary>
        public ImpactDamageBreakdown PlayerImpactDamage { get; }

        /// <summary>상대가 가한 ImpactDamage 계산 내역이다.</summary>
        public ImpactDamageBreakdown OpponentImpactDamage { get; }

        /// <summary>플레이어 HP에 최종 적용된 ImpactDamage 값이다.</summary>
        public int AppliedImpactDamageToPlayer { get; }

        /// <summary>상대 HP에 최종 적용된 ImpactDamage 값이다.</summary>
        public int AppliedImpactDamageToOpponent { get; }

        /// <summary>플레이어 Broken Cast 방어가 발동했는지 여부다.</summary>
        public bool PlayerUsedBrokenCastDefense { get; }

        /// <summary>플레이어 Broken Cast 보상 지급 여부다.</summary>
        public bool DidGrantOvercharge { get; }

        /// <summary>지급된 Overcharge 양이다.</summary>
        public int GrantedOverchargeAmount { get; }

        /// <summary>지급된 다음 Attempt 무료 리롤 토큰 수다.</summary>
        public int GrantedNextAttemptFreeRerollTokens { get; }

        /// <summary>Clash 이후 Round 결과다.</summary>
        public RoundOutcomeType OutcomeType { get; }

        /// <summary>다음 Attempt 진행 가능 여부다.</summary>
        public bool CanStartNextAttempt { get; }

        /// <summary>디버그 및 표시용 메시지다.</summary>
        public string Message { get; }

        /// <summary>Clash 판정 결과를 생성한다.</summary>
        public ClashResolveResult(
            int attemptNumber,
            ClashCastResult playerResult,
            ClashCastResult opponentResult,
            ClashParticipantType? winner,
            ImpactDamageBreakdown playerImpactDamage,
            ImpactDamageBreakdown opponentImpactDamage,
            int appliedImpactDamageToPlayer,
            int appliedImpactDamageToOpponent,
            bool playerUsedBrokenCastDefense,
            bool didGrantOvercharge,
            int grantedOverchargeAmount,
            int grantedNextAttemptFreeRerollTokens,
            RoundOutcomeType outcomeType,
            bool canStartNextAttempt,
            string message)
        {
            AttemptNumber = attemptNumber;
            PlayerResult = playerResult;
            OpponentResult = opponentResult;
            Winner = winner;
            PlayerImpactDamage = playerImpactDamage ?? ImpactDamageBreakdown.Zero(0);
            OpponentImpactDamage = opponentImpactDamage ?? ImpactDamageBreakdown.Zero(0);
            AppliedImpactDamageToPlayer = appliedImpactDamageToPlayer;
            AppliedImpactDamageToOpponent = appliedImpactDamageToOpponent;
            PlayerUsedBrokenCastDefense = playerUsedBrokenCastDefense;
            DidGrantOvercharge = didGrantOvercharge;
            GrantedOverchargeAmount = grantedOverchargeAmount;
            GrantedNextAttemptFreeRerollTokens = grantedNextAttemptFreeRerollTokens;
            OutcomeType = outcomeType;
            CanStartNextAttempt = canStartNextAttempt;
            Message = message ?? string.Empty;
        }
    }
}
