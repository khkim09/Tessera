using System;

namespace Tessera.Core
{
    /// <summary>Stage 안의 개별 Round 설정과 규칙을 정의한다.</summary>
    public class StageRoundDefinition
    {
        /// <summary>Stage 안에서의 Round 순서 인덱스.</summary>
        public int RoundIndex { get; }

        /// <summary>Round 표시 이름.</summary>
        public string DisplayName { get; }

        /// <summary>Round 종류.</summary>
        public StageRoundType RoundType { get; }

        /// <summary>이 Round를 Skip할 수 있는지 여부.</summary>
        public bool CanSkip { get; }

        /// <summary>Round 클리어 시 기본 Money 보상.</summary>
        public int RewardMoney { get; }

        /// <summary>Round Skip 시 받을 Money 보상.</summary>
        public int SkipRewardMoney { get; }

        /// <summary>이 Round에 적용되는 Core Round 규칙.</summary>
        public RoundRuleContext RuleContext { get; }

        /// <summary>Stage Round 정의를 생성한다.</summary>
        public StageRoundDefinition(
            int roundIndex,
            string displayName,
            StageRoundType roundType,
            bool canSkip,
            int rewardMoney,
            int skipRewardMoney,
            RoundRuleContext ruleContext)
        {
            if (roundIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(roundIndex), "Round 인덱스는 음수가 될 수 없습니다.");

            if (rewardMoney < 0)
                throw new ArgumentOutOfRangeException(nameof(rewardMoney), "보상 Money는 음수가 될 수 없습니다.");

            if (skipRewardMoney < 0)
                throw new ArgumentOutOfRangeException(nameof(skipRewardMoney), "Skip 보상 Money는 음수가 될 수 없습니다.");

            RoundIndex = roundIndex;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? $"Round {roundIndex + 1}" : displayName;
            RoundType = roundType;
            CanSkip = canSkip;
            RewardMoney = rewardMoney;
            SkipRewardMoney = skipRewardMoney;
            RuleContext = ruleContext ?? throw new ArgumentNullException(nameof(ruleContext));
        }

        /// <summary>일반 Round 정의를 생성한다.</summary>
        public static StageRoundDefinition CreateNormal(
            int roundIndex,
            string displayName,
            int rewardMoney,
            int skipRewardMoney,
            RoundRuleContext ruleContext)
        {
            return new StageRoundDefinition(
                roundIndex,
                displayName,
                StageRoundType.Normal,
                true,
                rewardMoney,
                skipRewardMoney,
                ruleContext);
        }

        /// <summary>Boss Round 정의를 생성한다.</summary>
        public static StageRoundDefinition CreateBoss(
            int roundIndex,
            string displayName,
            int rewardMoney,
            RoundRuleContext ruleContext)
        {
            return new StageRoundDefinition(
                roundIndex,
                displayName,
                StageRoundType.Boss,
                false,
                rewardMoney,
                0,
                ruleContext);
        }
    }
}
