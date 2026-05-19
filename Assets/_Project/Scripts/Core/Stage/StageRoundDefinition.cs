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

        /// <summary>Round 클리어 시 기본 Parts 보상.</summary>
        public int RewardParts { get; }

        /// <summary>Round Skip 시 받을 Parts 보상.</summary>
        public int SkipRewardParts { get; }

        /// <summary>이 Round에 적용되는 Core Round 규칙.</summary>
        public RoundRuleContext RuleContext { get; }

        /// <summary>Stage Round 정의를 생성한다.</summary>
        public StageRoundDefinition(
            int roundIndex,
            string displayName,
            StageRoundType roundType,
            bool canSkip,
            int rewardParts,
            int skipRewardParts,
            RoundRuleContext ruleContext)
        {
            if (roundIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(roundIndex), "Round 인덱스는 음수가 될 수 없습니다.");

            if (rewardParts < 0)
                throw new ArgumentOutOfRangeException(nameof(rewardParts), "보상 Parts는 음수가 될 수 없습니다.");

            if (skipRewardParts < 0)
                throw new ArgumentOutOfRangeException(nameof(skipRewardParts), "Skip 보상 Parts는 음수가 될 수 없습니다.");

            RoundIndex = roundIndex;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? $"Round {roundIndex + 1}" : displayName;
            RoundType = roundType;
            CanSkip = canSkip;
            RewardParts = rewardParts;
            SkipRewardParts = skipRewardParts;
            RuleContext = ruleContext ?? throw new ArgumentNullException(nameof(ruleContext));
        }

        /// <summary>일반 Round 정의를 생성한다.</summary>
        public static StageRoundDefinition CreateNormal(
            int roundIndex,
            string displayName,
            int rewardParts,
            int skipRewardParts,
            RoundRuleContext ruleContext)
        {
            return new StageRoundDefinition(
                roundIndex,
                displayName,
                StageRoundType.Normal,
                true,
                rewardParts,
                skipRewardParts,
                ruleContext);
        }

        /// <summary>Boss Round 정의를 생성한다.</summary>
        public static StageRoundDefinition CreateBoss(
            int roundIndex,
            string displayName,
            int rewardParts,
            RoundRuleContext ruleContext)
        {
            return new StageRoundDefinition(
                roundIndex,
                displayName,
                StageRoundType.Boss,
                false,
                rewardParts,
                0,
                ruleContext);
        }
    }
}
