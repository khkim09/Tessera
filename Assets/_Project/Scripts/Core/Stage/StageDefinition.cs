using System;
using System.Collections.Generic;

namespace Tessera.Core
{
    /// <summary>하나의 Stage를 구성하는 Round 목록과 표시 정보를 정의한다.</summary>
    public class StageDefinition
    {
        private readonly List<StageRoundDefinition> _rounds;

        /// <summary>Stage 번호.</summary>
        public int StageNumber { get; }

        /// <summary>Stage 표시 이름.</summary>
        public string DisplayName { get; }

        /// <summary>Stage 안에 포함된 Round 목록.</summary>
        public IReadOnlyList<StageRoundDefinition> Rounds => _rounds;

        /// <summary>Stage 클리어 시 해금 또는 지급될 보상 설명.</summary>
        public string StageRewardDescription { get; }

        /// <summary>Stage 정의를 생성한다.</summary>
        public StageDefinition(
            int stageNumber,
            string displayName,
            IReadOnlyList<StageRoundDefinition> rounds,
            string stageRewardDescription)
        {
            if (stageNumber <= 0)
                throw new ArgumentOutOfRangeException(nameof(stageNumber), "Stage 번호는 1 이상이어야 합니다.");

            if (rounds == null)
                throw new ArgumentNullException(nameof(rounds));

            if (rounds.Count == 0)
                throw new ArgumentException("Stage에는 최소 1개 이상의 Round가 필요합니다.", nameof(rounds));

            StageNumber = stageNumber;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? $"Stage {stageNumber}" : displayName;
            _rounds = new List<StageRoundDefinition>(rounds);
            StageRewardDescription = stageRewardDescription ?? string.Empty;
        }

        /// <summary>지정한 인덱스의 Stage Round 정의를 반환한다.</summary>
        public StageRoundDefinition GetRound(int roundIndex)
        {
            if (roundIndex < 0 || roundIndex >= _rounds.Count)
                throw new ArgumentOutOfRangeException(nameof(roundIndex), "Stage Round 인덱스가 범위를 벗어났습니다.");

            return _rounds[roundIndex];
        }

        /// <summary>디버그용 첫 번째 Stage 정의를 생성한다.</summary>
        public static StageDefinition CreateDebugStageOne()
        {
            List<StageRoundDefinition> rounds = new List<StageRoundDefinition>
            {
                StageRoundDefinition.CreateNormal(
                    roundIndex: 0,
                    displayName: "Round 1 - Alley Table",
                    rewardMoney: 20,
                    skipRewardMoney: 8,
                    ruleContext: RoundRuleContext.CreateDefault()),

                StageRoundDefinition.CreateNormal(
                    roundIndex: 1,
                    displayName: "Round 2 - Rust Table",
                    rewardMoney: 25,
                    skipRewardMoney: 10,
                    ruleContext: RoundRuleContext.CreateDefault()),

                StageRoundDefinition.CreateBoss(
                    roundIndex: 2,
                    displayName: "Boss Round - The Clerk",
                    rewardMoney: 60,
                    ruleContext: RoundRuleContext.CreateDebugAcesBoss())
            };

            return new StageDefinition(
                stageNumber: 1,
                displayName: "Stage 1 - Broken Ledger",
                rounds: rounds,
                stageRewardDescription: "Unlock candidate: Ledger Deck / Aces-focused starter deck.");
        }
    }
}
