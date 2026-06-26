using System;
using System.Collections.Generic;

namespace Tessera.Core
{
    /// <summary>한 Round에서 사용할 주사위, Attempt, Roll, Cast 사용 제한, 상대 규칙을 정의한다.</summary>
    public class RoundRuleContext
    {
        private readonly List<TableRule> _tableRules;

        /// <summary>한 번에 굴릴 주사위 개수.</summary>
        public int DiceCount { get; }

        /// <summary>Round에서 사용할 수 있는 최대 Attempt 수.</summary>
        public int MaxAttempts { get; }

        /// <summary>Attempt마다 기본으로 제공되는 Roll 횟수다.</summary>
        public int BaseRollsPerAttempt { get; }

        /// <summary>플레이어 최대 HP.</summary>
        public int PlayerMaxHP { get; }

        /// <summary>상대 최대 HP.</summary>
        public int OpponentMaxHP { get; }

        /// <summary>같은 Cast 카테고리를 Round 안에서 사용할 수 있는 기본 횟수.</summary>
        public int MaxUsesPerCastPerRound { get; }

        /// <summary>Broken Cast를 Round 안에서 사용할 수 있는 횟수.</summary>
        public int MaxBrokenCastUsesPerRound { get; }

        /// <summary>상대의 기본 Strike 피해량.</summary>
        public int EnemyStrikeDamage { get; }

        /// <summary>현재 Stage 내부 누적 위험도다.</summary>
        public int StageThreatLevel { get; }

        /// <summary>Round 또는 Boss Round에 적용되는 테이블 규칙 목록.</summary>
        public IReadOnlyList<TableRule> TableRules => _tableRules;

        /// <summary>Broken Cast가 Overcharge를 지급하는지 여부.</summary>
        public bool BrokenCastGrantsOvercharge { get; }

        /// <summary>Broken Cast로 지급할 Overcharge 양.</summary>
        public int BrokenCastOverchargeAmount { get; }

        /// <summary>Broken Cast가 다음 Attempt 무료 리롤을 지급하는지 여부.</summary>
        public bool BrokenCastGrantsNextAttemptFreeReroll { get; }

        /// <summary>Broken Cast로 지급할 다음 Attempt 무료 리롤 토큰 수.</summary>
        public int BrokenCastFreeRerollTokenAmount { get; }

        /// <summary>0보다 크면 적용되는 선택적 Impact 상한이며 0 이하면 비활성화된다.</summary>
        public int ImpactCap { get; }

        /// <summary>Round 규칙 정보를 생성한다.</summary>
        public RoundRuleContext(
            int diceCount,
            int maxAttempts,
            int baseRollsPerAttempt,
            int playerMaxHP,
            int opponentMaxHP,
            int maxUsesPerCastPerRound,
            int maxBrokenCastUsesPerRound,
            int enemyStrikeDamage,
            bool brokenCastGrantsOvercharge,
            int brokenCastOverchargeAmount,
            bool brokenCastGrantsNextAttemptFreeReroll,
            int brokenCastFreeRerollTokenAmount,
            int impactCap,
            IReadOnlyList<TableRule> tableRules = null,
            int stageThreatLevel = 0)
        {
            if (diceCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(diceCount), "주사위 개수는 1개 이상이어야 합니다.");

            if (maxAttempts <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxAttempts), "최대 Attempt 수는 1 이상이어야 합니다.");

            if (baseRollsPerAttempt <= 0)
                throw new ArgumentOutOfRangeException(nameof(baseRollsPerAttempt), "Base Rolls Per Attempt는 1 이상이어야 합니다.");

            if (playerMaxHP <= 0)
                throw new ArgumentOutOfRangeException(nameof(playerMaxHP), "플레이어 최대 HP는 1 이상이어야 합니다.");

            if (opponentMaxHP <= 0)
                throw new ArgumentOutOfRangeException(nameof(opponentMaxHP), "상대 최대 HP는 1 이상이어야 합니다.");

            if (maxUsesPerCastPerRound <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxUsesPerCastPerRound), "Cast 사용 가능 횟수는 1 이상이어야 합니다.");

            if (maxBrokenCastUsesPerRound <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxBrokenCastUsesPerRound), "Broken Cast 사용 가능 횟수는 1 이상이어야 합니다.");

            if (enemyStrikeDamage < 0)
                throw new ArgumentOutOfRangeException(nameof(enemyStrikeDamage), "상대 Strike 피해량은 음수가 될 수 없습니다.");

            if (stageThreatLevel < 0)
                throw new ArgumentOutOfRangeException(nameof(stageThreatLevel), "StageThreatLevel은 음수가 될 수 없습니다.");

            if (brokenCastOverchargeAmount < 0)
                throw new ArgumentOutOfRangeException(nameof(brokenCastOverchargeAmount), "Broken Cast Overcharge 보상은 음수가 될 수 없습니다.");

            if (brokenCastFreeRerollTokenAmount < 0)
                throw new ArgumentOutOfRangeException(nameof(brokenCastFreeRerollTokenAmount), "Broken Cast 무료 리롤 보상은 음수가 될 수 없습니다.");

            DiceCount = diceCount;
            MaxAttempts = maxAttempts;
            BaseRollsPerAttempt = baseRollsPerAttempt;
            PlayerMaxHP = playerMaxHP;
            OpponentMaxHP = opponentMaxHP;
            MaxUsesPerCastPerRound = maxUsesPerCastPerRound;
            MaxBrokenCastUsesPerRound = maxBrokenCastUsesPerRound;
            EnemyStrikeDamage = enemyStrikeDamage;
            StageThreatLevel = stageThreatLevel;
            BrokenCastGrantsOvercharge = brokenCastGrantsOvercharge;
            BrokenCastOverchargeAmount = brokenCastOverchargeAmount;
            BrokenCastGrantsNextAttemptFreeReroll = brokenCastGrantsNextAttemptFreeReroll;
            BrokenCastFreeRerollTokenAmount = brokenCastFreeRerollTokenAmount;
            _tableRules = tableRules != null ? new List<TableRule>(tableRules) : new List<TableRule>();
            ImpactCap = Math.Max(0, impactCap);
        }

        /// <summary>SO 없이 Core 테스트를 실행할 때 사용할 기본 Round 규칙을 생성한다.</summary>
        public static RoundRuleContext CreateDefault()
        {
            return new RoundRuleContext(
                diceCount: 5,
                maxAttempts: 3,
                baseRollsPerAttempt: 3,
                playerMaxHP: 20,
                opponentMaxHP: 18,
                maxUsesPerCastPerRound: 1,
                maxBrokenCastUsesPerRound: 3,
                enemyStrikeDamage: 3,
                brokenCastGrantsOvercharge: true,
                brokenCastOverchargeAmount: 1,
                brokenCastGrantsNextAttemptFreeReroll: true,
                brokenCastFreeRerollTokenAmount: 1,
                stageThreatLevel: 0,
                impactCap: 0);
        }
    }
}
