using System;
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

        [Header("Bounty Card Text")]
        [SerializeField, TextArea(2, 4)] private string bountyDescription;
        [SerializeField, TextArea(1, 3)] private string intentDescription;
        [SerializeField, TextArea(1, 3)] private string specialRuleDescription;

        [Header("Rewards")]
        [FormerlySerializedAs("rewardMoney")]
        [SerializeField] private int baseRewardMoney = 20;
        [SerializeField] private int bountyRank = 1;
        [SerializeField] private int rewardOvercharge;
        [SerializeField] private string rewardDescription;

        [Header("Player / Round Rules")]
        [SerializeField] private int playerMaxHP = 20;
        [SerializeField] private int diceCount = 5;
        [SerializeField] private int maxAttempts = 3;
        [SerializeField] private int impactCap;
        [SerializeField] private int maxUsesPerCastPerRound = 1;
        [SerializeField] private int maxBrokenCastUsesPerRound = 3;

        [Header("Broken Cast")]
        [SerializeField] private bool brokenCastGrantsOvercharge = true;
        [SerializeField] private int brokenCastOverchargeAmount = 1;
        [SerializeField] private bool brokenCastGrantsNextAttemptFreeReroll = true;
        [SerializeField] private int brokenCastFreeRerollTokenAmount = 1;

        [Header("Table Rule Presets")]
        [SerializeField] private bool applyNonAcesCastPowerPenalty;
        [SerializeField] private int nonAcesCastPowerPercent = 50;
        [SerializeField] private bool disableChance;
        [SerializeField] private bool disableBrokenCastReward;

        [Header("Opponent Devices")]
        [SerializeField] private SlotPairDeviceDefinitionSO[] opponentDevicePool;
        [SerializeField] private int minOpponentDeviceCount;
        [SerializeField] private int maxOpponentDeviceCount = 5;
        [SerializeField] private bool allowDuplicateOpponentDevices = true;

        [Header("Enemy")]
        [SerializeField] private int opponentMaxHP = 80;
        [SerializeField] private int enemyStrikeDamage = 3;
        [SerializeField, Min(1)] private int opponentBaseRollsPerAttempt = 3;

        [Header("Enemy Dice")]
        [SerializeField] private EnemyDiceLoadoutDefinitionSO opponentDiceLoadout;

        [Header("Round Initiative")]
        [SerializeField] private bool useOpeningIntentInitiativeAsRoundInitiative = true;
        [SerializeField] private InitiativeOwnerType roundInitiativeOwner = InitiativeOwnerType.Opponent;

        [Header("Enemy Intent")]
        [SerializeField] private EnemyIntentProfileSO intentProfile;

        #region Getter

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

        /// <summary>수배지 카드에 표시할 간단 설명.</summary>
        public string BountyDescription => bountyDescription ?? string.Empty;
        /// <summary>수배지 카드에 표시할 상대 Intent 설명.</summary>
        public string IntentDescription => intentDescription ?? string.Empty;
        /// <summary>수배지 카드에 표시할 특수 규칙 설명.</summary>
        public string SpecialRuleDescription => specialRuleDescription ?? string.Empty;

        /// <summary>수배지 승리 시 PendingMoneyReward에 들어갈 기본 Money 보상.</summary>
        public int BaseRewardMoney => Mathf.Max(0, baseRewardMoney);
        /// <summary>수배지 기본 난이도 랭크.</summary>
        public int BountyRank => Mathf.Max(1, bountyRank);
        /// <summary>기존 Pending Overcharge 보상 호환용 접근자다. 신규 Bounty 보상 흐름에서는 사용하지 않는다.</summary>
        public int RewardOvercharge => Mathf.Max(0, rewardOvercharge);
        /// <summary>보상 설명.</summary>
        public string RewardDescription => rewardDescription ?? string.Empty;

        /// <summary>Round 시작 기준 플레이어 최대 HP다.</summary>
        public int PlayerMaxHP => Mathf.Max(1, playerMaxHP);
        /// <summary>Round에서 굴릴 주사위 개수다.</summary>
        public int DiceCount => Mathf.Max(1, diceCount);
        /// <summary>최대 Attempt 수.</summary>
        public int MaxAttempts => Mathf.Max(1, maxAttempts);
        /// <summary>0보다 크면 적용되는 선택적 Impact 상한이며 0 이하면 비활성화된다.</summary>
        public int ImpactCap => Mathf.Max(0, impactCap);
        /// <summary>같은 Cast를 Round 안에서 사용할 수 있는 횟수다.</summary>
        public int MaxUsesPerCastPerRound => Mathf.Max(1, maxUsesPerCastPerRound);
        /// <summary>Broken Cast를 Round 안에서 사용할 수 있는 횟수다.</summary>
        public int MaxBrokenCastUsesPerRound => Mathf.Max(1, maxBrokenCastUsesPerRound);

        /// <summary>Broken Cast가 지급하는 Overcharge 양이다.</summary>
        public int BrokenCastOverchargeAmount => Mathf.Max(0, brokenCastOverchargeAmount);
        /// <summary>Broken Cast가 지급하는 다음 Attempt 무료 Roll 토큰 수다.</summary>
        public int BrokenCastFreeRerollTokenAmount => Mathf.Max(0, brokenCastFreeRerollTokenAmount);

        /// <summary>Aces 외 CastPower 보정을 적용할지 여부다.</summary>
        public bool ApplyNonAcesCastPowerPenalty => applyNonAcesCastPowerPenalty;
        /// <summary>Aces 외 CastPower 보정 비율이다.</summary>
        public int NonAcesCastPowerPercent => Mathf.Clamp(nonAcesCastPowerPercent, 0, 100);
        /// <summary>Chance Cast를 비활성화할지 여부다.</summary>
        public bool DisableChance => disableChance;
        /// <summary>Broken Cast 보상을 비활성화할지 여부다.</summary>
        public bool DisableBrokenCastReward => disableBrokenCastReward;

        /// <summary>상대 Device 후보 풀.</summary>
        public IReadOnlyList<SlotPairDeviceDefinitionSO> OpponentDevicePool => opponentDevicePool;
        /// <summary>상대가 장착할 최소 Device 수.</summary>
        public int MinOpponentDeviceCount => Mathf.Clamp(minOpponentDeviceCount, 0, SlotPairDamageCalculator.SlotPairCount);
        /// <summary>상대가 장착할 최대 Device 수.</summary>
        public int MaxOpponentDeviceCount => Mathf.Clamp(
            maxOpponentDeviceCount,
            MinOpponentDeviceCount,
            SlotPairDamageCalculator.SlotPairCount);
        /// <summary>상대 Device 중복 장착 허용 여부.</summary>
        public bool AllowDuplicateOpponentDevices => allowDuplicateOpponentDevices;

        /// <summary>상대 최대 HP다.</summary>
        public int OpponentMaxHP => Mathf.Max(1, opponentMaxHP);
        /// <summary>상대 기본 Strike 피해량이다.</summary>
        public int EnemyStrikeDamage => Mathf.Max(0, enemyStrikeDamage);
        /// <summary>상대가 Attempt마다 사용할 기본 Roll 횟수다.</summary>
        public int OpponentBaseRollsPerAttempt => Mathf.Max(1, opponentBaseRollsPerAttempt);

        /// <summary>상대 Dice 로드아웃 정의. null이면 기본 D6 세트를 사용한다.</summary>
        public EnemyDiceLoadoutDefinitionSO OpponentDiceLoadout => opponentDiceLoadout;
        /// <summary>OpeningIntent의 Initiative를 Round 고정 Initiative로 사용할지 여부.</summary>
        public bool UseOpeningIntentInitiativeAsRoundInitiative => useOpeningIntentInitiativeAsRoundInitiative;

        /// <summary>Round 시작 Attempt에서 사용할 기본 상대 Intent 정의.</summary>
        public EnemyIntentDefinitionSO OpeningIntent => intentProfile != null ? intentProfile.OpeningIntent : null;
        /// <summary>Round 중 선택될 수 있는 상대 Intent 후보 목록.</summary>
        public IReadOnlyList<EnemyIntentDefinitionSO> IntentDeck => intentProfile != null ? intentProfile.IntentPool : Array.Empty<EnemyIntentDefinitionSO>();
        /// <summary>이 Bounty가 사용할 Intent 프로필이다.</summary>
        public EnemyIntentProfileSO IntentProfile => intentProfile;

        #endregion

        /// <summary>Round 전체에서 고정 사용할 선공권을 반환한다.</summary>
        public InitiativeOwnerType ResolveRoundInitiativeOwner()
        {
            if (!useOpeningIntentInitiativeAsRoundInitiative)
                return roundInitiativeOwner;

            EnemyIntentDefinitionSO resolvedOpeningIntent = OpeningIntent;

            if (resolvedOpeningIntent != null)
                return resolvedOpeningIntent.InitiativeOwner;

            return roundInitiativeOwner;
        }

        /// <summary>StageThreat 없이 RoundRuleContext를 생성한다.</summary>
        public RoundRuleContext BuildRuleContext(int runPlayerMaxHP)
        {
            return BuildRuleContext(runPlayerMaxHP, 0);
        }

        /// <summary>StageThreatLevel 보정을 반영하여 RoundRuleContext를 생성한다.</summary>
        public RoundRuleContext BuildRuleContext(int runPlayerMaxHP, int stageThreatLevel)
        {
            List<TableRule> tableRules = new List<TableRule>();

            if (ApplyNonAcesCastPowerPenalty)
                tableRules.Add(TableRule.NonAcesCastPowerPercent(NonAcesCastPowerPercent));

            if (DisableChance)
                tableRules.Add(TableRule.DisableChance());

            if (DisableBrokenCastReward)
                tableRules.Add(TableRule.DisableBrokenCastReward());

            int resolvedPlayerMaxHP = runPlayerMaxHP > 0 ? runPlayerMaxHP : PlayerMaxHP;
            int resolvedStageThreatLevel = Mathf.Max(0, stageThreatLevel);
            int resolvedEnemyStrikeDamage = EnemyStrikeDamage;
            int resolvedOpponentBaseRollsPerAttempt = OpponentBaseRollsPerAttempt;
            int resolvedOpponentMaxHP = OpponentMaxHP;

            if (resolvedStageThreatLevel >= 1)
                resolvedEnemyStrikeDamage += 1;

            if (resolvedStageThreatLevel >= 2)
                resolvedOpponentBaseRollsPerAttempt += 1;

            if (resolvedStageThreatLevel >= 3)
                resolvedOpponentMaxHP += resolvedStageThreatLevel * 5;

            return new RoundRuleContext(
                diceCount: DiceCount,
                maxAttempts: MaxAttempts,
                baseRollsPerAttempt: RoundState.DefaultPlayerBaseRollsPerAttempt,
                opponentBaseRollsPerAttempt: resolvedOpponentBaseRollsPerAttempt,
                playerMaxHP: Mathf.Max(1, resolvedPlayerMaxHP),
                opponentMaxHP: resolvedOpponentMaxHP,
                maxUsesPerCastPerRound: MaxUsesPerCastPerRound,
                maxBrokenCastUsesPerRound: MaxBrokenCastUsesPerRound,
                enemyStrikeDamage: resolvedEnemyStrikeDamage,
                brokenCastGrantsOvercharge: brokenCastGrantsOvercharge,
                brokenCastOverchargeAmount: BrokenCastOverchargeAmount,
                brokenCastGrantsNextAttemptFreeReroll: brokenCastGrantsNextAttemptFreeReroll,
                brokenCastFreeRerollTokenAmount: BrokenCastFreeRerollTokenAmount,
                tableRules: tableRules,
                stageThreatLevel: resolvedStageThreatLevel,
                impactCap: ImpactCap);
        }

        /// <summary>이 Round에서 사용할 상대 SlotPair Device 장착 배열을 생성한다.</summary>
        public SlotPairDeviceDefinitionSO[] BuildOpponentSlotPairDeviceLoadout(int seed)
        {
            SlotPairDeviceDefinitionSO[] result = new SlotPairDeviceDefinitionSO[SlotPairDamageCalculator.SlotPairCount];

            if (opponentDevicePool == null || opponentDevicePool.Length <= 0)
                return result;

            List<SlotPairDeviceDefinitionSO> candidates = new List<SlotPairDeviceDefinitionSO>();

            for (int i = 0; i < opponentDevicePool.Length; i++)
            {
                if (opponentDevicePool[i] != null)
                    candidates.Add(opponentDevicePool[i]);
            }

            if (candidates.Count <= 0)
                return result;

            System.Random random = new System.Random(seed);

            int minCount = MinOpponentDeviceCount;
            int maxCount = MaxOpponentDeviceCount;
            int equipCount = random.Next(minCount, maxCount + 1);

            if (!allowDuplicateOpponentDevices)
                equipCount = Mathf.Min(equipCount, candidates.Count);

            List<int> availableSlots = new List<int>();

            for (int i = 0; i < result.Length; i++)
                availableSlots.Add(i);

            for (int i = 0; i < equipCount; i++)
            {
                if (availableSlots.Count <= 0)
                    break;

                if (candidates.Count <= 0)
                    break;

                int slotPickIndex = random.Next(0, availableSlots.Count);
                int slotIndex = availableSlots[slotPickIndex];
                availableSlots.RemoveAt(slotPickIndex);

                int devicePickIndex = random.Next(0, candidates.Count);
                result[slotIndex] = candidates[devicePickIndex];

                if (!allowDuplicateOpponentDevices)
                    candidates.RemoveAt(devicePickIndex);
            }

            return result;
        }

        /// <summary>Round 시작 시 사용할 Core EnemyIntent를 생성한다.</summary>
        public EnemyIntent BuildOpeningEnemyIntent()
        {
            EnemyIntentDefinitionSO resolvedOpeningIntent = OpeningIntent;

            if (resolvedOpeningIntent != null)
                return resolvedOpeningIntent.ToCoreIntent(enemyStrikeDamage);

            return new EnemyIntent(
                EnemyIntentType.Strike,
                Mathf.Max(0, enemyStrikeDamage),
                string.IsNullOrWhiteSpace(intentDescription) ? "Opening strike." : intentDescription,
                EnemyIntentCategoryType.Aggression,
                InitiativeOwnerType.Opponent);
        }

        /// <summary>Round 고정 Initiative를 반영해 Round 시작 Core EnemyIntent를 생성한다.</summary>
        public EnemyIntent BuildOpeningEnemyIntent(InitiativeOwnerType fixedRoundInitiativeOwner)
        {
            EnemyIntentDefinitionSO resolvedOpeningIntent = OpeningIntent;

            if (resolvedOpeningIntent != null)
                return resolvedOpeningIntent.ToCoreIntent(enemyStrikeDamage, fixedRoundInitiativeOwner);

            EnemyIntentType intentType = fixedRoundInitiativeOwner == InitiativeOwnerType.Opponent
                ? EnemyIntentType.Strike
                : EnemyIntentType.None;

            return new EnemyIntent(
                intentType,
                Mathf.Max(0, enemyStrikeDamage),
                string.IsNullOrWhiteSpace(intentDescription) ? "Opening strike." : intentDescription,
                EnemyIntentCategoryType.Aggression,
                fixedRoundInitiativeOwner);
        }

        /// <summary>지정 Attempt에서 사용할 EnemyIntentDefinitionSO를 선택한다.</summary>
        public EnemyIntentDefinitionSO SelectIntentDefinitionForAttempt(int attemptNumber, int seed)
        {
            if (attemptNumber <= 1)
                return OpeningIntent;

            IReadOnlyList<EnemyIntentDefinitionSO> resolvedIntentDeck = IntentDeck;

            if (resolvedIntentDeck == null || resolvedIntentDeck.Count <= 0)
                return OpeningIntent;

            List<EnemyIntentDefinitionSO> candidates = new List<EnemyIntentDefinitionSO>();

            for (int i = 0; i < resolvedIntentDeck.Count; i++)
            {
                if (resolvedIntentDeck[i] != null)
                    candidates.Add(resolvedIntentDeck[i]);
            }

            if (candidates.Count <= 0)
                return OpeningIntent;

            int resolvedSeed = seed + attemptNumber * 397;
            System.Random random = new System.Random(resolvedSeed);
            int selectedIndex = random.Next(0, candidates.Count);

            return candidates[selectedIndex];
        }

        /// <summary>지정 Attempt에서 사용할 Core EnemyIntent를 생성한다.</summary>
        public EnemyIntent BuildEnemyIntentForAttempt(int attemptNumber, int seed)
        {
            EnemyIntentDefinitionSO selectedIntent = SelectIntentDefinitionForAttempt(attemptNumber, seed);

            if (selectedIntent != null)
                return selectedIntent.ToCoreIntent(enemyStrikeDamage);

            return BuildOpeningEnemyIntent();
        }

        /// <summary>지정 Attempt에서 사용할 Core EnemyIntent를 생성하되 Round 고정 Initiative를 유지한다.</summary>
        public EnemyIntent BuildEnemyIntentForAttempt(
            int attemptNumber,
            int seed,
            InitiativeOwnerType fixedRoundInitiativeOwner)
        {
            EnemyIntentDefinitionSO selectedIntent = SelectIntentDefinitionForAttempt(attemptNumber, seed);

            if (selectedIntent != null)
                return selectedIntent.ToCoreIntent(enemyStrikeDamage, fixedRoundInitiativeOwner);

            return BuildOpeningEnemyIntent(fixedRoundInitiativeOwner);
        }
    }
}
