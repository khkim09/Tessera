using System.Collections.Generic;
using Tessera.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace Tessera.Data
{
    /// <summary>Stage 안에서 선택 가능한 수배지/라운드 정의 ScriptableObject다.</summary>
    [CreateAssetMenu(
        fileName = "StageRoundDefinition",
        menuName = "Tessera/Stage/Stage Round Definition")]
    public class StageRoundDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string roundId;
        [SerializeField] private string displayName = "New Bounty";
        [SerializeField] private StageRoundType roundType = StageRoundType.Normal;
        [SerializeField] private bool tutorialForcedRound;
        [SerializeField] private bool initiallyAvailable = true;

        [Header("Rewards")]
        [FormerlySerializedAs("rewardParts")]
        [SerializeField] private int baseRewardMoney = 20;
        [SerializeField] private int bountyRank = 1;
        [SerializeField] private int rewardOvercharge;
        [SerializeField] private string rewardDescription;

        [Header("Enemy")]
        [SerializeField] private int opponentMaxHp = 80;
        [SerializeField] private int enemyStrikeDamage = 3;

        [Header("Player / Round Rules")]
        [SerializeField] private int playerMaxHp = 100;
        [SerializeField] private int diceCount = 5;
        [SerializeField] private int maxAttempts = 3;
        [SerializeField] private int roundRollPool = 8;
        [SerializeField] private int maxUsesPerCastPerRound = 1;
        [SerializeField] private int maxBrokenCastUsesPerRound = 3;

        [Header("Broken Cast")]
        [SerializeField] private bool brokenCastGrantsOvercharge = true;
        [SerializeField] private int brokenCastOverchargeAmount = 1;
        [SerializeField] private bool brokenCastGrantsNextAttemptFreeReroll = true;
        [SerializeField] private int brokenCastFreeRerollTokenAmount = 1;

        [Header("Table Rule Presets")]
        [SerializeField] private bool applyNonAcesDamagePenalty;
        [SerializeField] private int nonAcesDamagePercent = 50;
        [SerializeField] private bool disableChance;
        [SerializeField] private bool disableBrokenCastReward;

        /// <summary>라운드 고유 ID.</summary>
        public string RoundId => string.IsNullOrWhiteSpace(roundId) ? name : roundId;

        /// <summary>표시 이름.</summary>
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;

        /// <summary>라운드 타입.</summary>
        public StageRoundType RoundType => roundType;

        /// <summary>Stage 진입 직후 강제 튜토리얼 라운드인지 여부.</summary>
        public bool TutorialForcedRound => tutorialForcedRound;

        /// <summary>초기 선택 가능 여부.</summary>
        public bool InitiallyAvailable => initiallyAvailable;

        /// <summary>수배지 승리 시 PendingMoneyReward에 들어갈 기본 Money 보상.</summary>
        public int BaseRewardMoney => Mathf.Max(0, baseRewardMoney);

        /// <summary>수배지 기본 난이도 랭크.</summary>
        public int BountyRank => Mathf.Max(1, bountyRank);

        /// <summary>최대 Attempt 수.</summary>
        public int MaxAttempts => Mathf.Max(1, maxAttempts);

        /// <summary>기존 Parts 기반 코드 호환용 접근자다. 신규 코드는 BaseRewardMoney를 사용한다.</summary>
        public int RewardParts => BaseRewardMoney;

        /// <summary>기존 Pending Overcharge 보상 호환용 접근자다. 신규 Bounty 보상 흐름에서는 사용하지 않는다.</summary>
        public int RewardOvercharge => Mathf.Max(0, rewardOvercharge);

        /// <summary>보상 설명.</summary>
        public string RewardDescription => rewardDescription ?? string.Empty;

        /// <summary>StageThreat 없이 RoundRuleContext를 생성한다.</summary>
        public RoundRuleContext BuildRuleContext(int runPlayerMaxHp)
        {
            return BuildRuleContext(runPlayerMaxHp, 0);
        }

        /// <summary>StageThreatLevel 보정을 반영하여 RoundRuleContext를 생성한다.</summary>
        public RoundRuleContext BuildRuleContext(int runPlayerMaxHp, int stageThreatLevel)
        {
            List<TableRule> tableRules = new List<TableRule>();

            if (applyNonAcesDamagePenalty)
                tableRules.Add(TableRule.NonAcesDamagePercent(Mathf.Clamp(nonAcesDamagePercent, 0, 100)));

            if (disableChance)
                tableRules.Add(TableRule.DisableChance());

            if (disableBrokenCastReward)
                tableRules.Add(TableRule.DisableBrokenCastReward());

            int resolvedPlayerMaxHp = runPlayerMaxHp > 0 ? runPlayerMaxHp : playerMaxHp;
            int resolvedStageThreatLevel = Mathf.Max(0, stageThreatLevel);
            int resolvedEnemyStrikeDamage = Mathf.Max(0, enemyStrikeDamage);
            int resolvedRoundRollPool = Mathf.Max(1, roundRollPool);
            int resolvedOpponentMaxHp = Mathf.Max(1, opponentMaxHp);

            if (resolvedStageThreatLevel >= 1)
                resolvedEnemyStrikeDamage += 1;

            if (resolvedStageThreatLevel >= 2)
                resolvedRoundRollPool = Mathf.Max(1, resolvedRoundRollPool - 1);

            if (resolvedStageThreatLevel >= 3)
                resolvedOpponentMaxHp += resolvedStageThreatLevel * 5;

            return new RoundRuleContext(
                diceCount: Mathf.Max(1, diceCount),
                maxAttempts: Mathf.Max(1, maxAttempts),
                roundRollPool: resolvedRoundRollPool,
                playerMaxHp: Mathf.Max(1, resolvedPlayerMaxHp),
                opponentMaxHp: resolvedOpponentMaxHp,
                maxUsesPerCastPerRound: Mathf.Max(1, maxUsesPerCastPerRound),
                maxBrokenCastUsesPerRound: Mathf.Max(1, maxBrokenCastUsesPerRound),
                enemyStrikeDamage: resolvedEnemyStrikeDamage,
                brokenCastGrantsOvercharge: brokenCastGrantsOvercharge,
                brokenCastOverchargeAmount: Mathf.Max(0, brokenCastOverchargeAmount),
                brokenCastGrantsNextAttemptFreeReroll: brokenCastGrantsNextAttemptFreeReroll,
                brokenCastFreeRerollTokenAmount: Mathf.Max(0, brokenCastFreeRerollTokenAmount),
                tableRules: tableRules);
        }
    }
}
