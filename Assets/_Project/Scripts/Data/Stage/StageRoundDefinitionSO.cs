using System.Collections.Generic;
using Tessera.Core;
using UnityEngine;

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
        [SerializeField] private int rewardParts = 20;
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

        /// <summary>Parts 보상.</summary>
        public int RewardParts => Mathf.Max(0, rewardParts);

        /// <summary>Overcharge 보상.</summary>
        public int RewardOvercharge => Mathf.Max(0, rewardOvercharge);

        /// <summary>보상 설명.</summary>
        public string RewardDescription => rewardDescription ?? string.Empty;

        /// <summary>RoundRuleContext를 생성한다.</summary>
        public RoundRuleContext BuildRuleContext(int runPlayerMaxHp)
        {
            List<TableRule> tableRules = new List<TableRule>();

            if (applyNonAcesDamagePenalty)
                tableRules.Add(TableRule.NonAcesDamagePercent(Mathf.Clamp(nonAcesDamagePercent, 0, 100)));

            if (disableChance)
                tableRules.Add(TableRule.DisableChance());

            if (disableBrokenCastReward)
                tableRules.Add(TableRule.DisableBrokenCastReward());

            int resolvedPlayerMaxHp = runPlayerMaxHp > 0 ? runPlayerMaxHp : playerMaxHp;

            return new RoundRuleContext(
                diceCount: Mathf.Max(1, diceCount),
                maxAttempts: Mathf.Max(1, maxAttempts),
                roundRollPool: Mathf.Max(1, roundRollPool),
                playerMaxHp: Mathf.Max(1, resolvedPlayerMaxHp),
                opponentMaxHp: Mathf.Max(1, opponentMaxHp),
                maxUsesPerCastPerRound: Mathf.Max(1, maxUsesPerCastPerRound),
                maxBrokenCastUsesPerRound: Mathf.Max(1, maxBrokenCastUsesPerRound),
                enemyStrikeDamage: Mathf.Max(0, enemyStrikeDamage),
                brokenCastGrantsOvercharge: brokenCastGrantsOvercharge,
                brokenCastOverchargeAmount: Mathf.Max(0, brokenCastOverchargeAmount),
                brokenCastGrantsNextAttemptFreeReroll: brokenCastGrantsNextAttemptFreeReroll,
                brokenCastFreeRerollTokenAmount: Mathf.Max(0, brokenCastFreeRerollTokenAmount),
                tableRules: tableRules);
        }
    }
}
