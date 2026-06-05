namespace Tessera.Core
{
    /// <summary>Player와 Opponent의 Cast 결과를 비교한 Clash 판정 결과다.</summary>
    public class ClashResolveResult
    {
        /// <summary>Clash가 해결된 Attempt 번호.</summary>
        public int AttemptNumber { get; }

        /// <summary>플레이어 Cast 결과.</summary>
        public ClashCastResult PlayerResult { get; }

        /// <summary>상대 Cast 결과.</summary>
        public ClashCastResult OpponentResult { get; }

        /// <summary>Clash 승자. 무승부면 null이다.</summary>
        public ClashParticipantType? Winner { get; }

        /// <summary>플레이어에게 적용된 피해.</summary>
        public int DamageToPlayer { get; }

        /// <summary>상대에게 적용된 피해.</summary>
        public int DamageToOpponent { get; }

        /// <summary>플레이어 Broken Cast 방어가 발동했는지 여부.</summary>
        public bool PlayerUsedBrokenCastDefense { get; }

        /// <summary>플레이어 Broken Cast 보상 지급 여부.</summary>
        public bool DidGrantOvercharge { get; }

        /// <summary>지급된 Overcharge 양.</summary>
        public int GrantedOverchargeAmount { get; }

        /// <summary>지급된 다음 Attempt 무료 리롤 토큰 수.</summary>
        public int GrantedNextAttemptFreeRerollTokens { get; }

        /// <summary>Clash 이후 Round 결과.</summary>
        public RoundOutcomeType OutcomeType { get; }

        /// <summary>다음 Attempt 진행 가능 여부.</summary>
        public bool CanStartNextAttempt { get; }

        /// <summary>디버그/표시용 메시지.</summary>
        public string Message { get; }

        /// <summary>Clash 판정 결과를 생성한다.</summary>
        public ClashResolveResult(
            int attemptNumber,
            ClashCastResult playerResult,
            ClashCastResult opponentResult,
            ClashParticipantType? winner,
            int damageToPlayer,
            int damageToOpponent,
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
            DamageToPlayer = damageToPlayer;
            DamageToOpponent = damageToOpponent;
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
