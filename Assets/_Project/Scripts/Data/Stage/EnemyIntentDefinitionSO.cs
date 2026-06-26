using Tessera.Core;
using UnityEngine;

namespace Tessera.Data
{
    /// <summary>수배지/보스가 Attempt 시작 시 사용할 상대 Intent 정의다.</summary>
    [CreateAssetMenu(
        fileName = "EnemyIntentDefinition",
        menuName = "Tessera/Stage/Enemy Intent Definition")]
    public class EnemyIntentDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string intentId;
        [SerializeField] private string displayName = "New Intent";

        [Header("Presentation")]
        [SerializeField, TextArea(2, 4)] private string shortDescription;
        [SerializeField, TextArea(2, 4)] private string bountyCardDescription;

        [Header("Initiative")]
        [SerializeField] private EnemyIntentCategoryType categoryType = EnemyIntentCategoryType.Aggression;
        [SerializeField] private InitiativeOwnerType initiativeOwner = InitiativeOwnerType.Opponent;

        [Header("Opponent Cast")]
        [SerializeField] private bool useOpponentDevices = true;
        [SerializeField] private OpponentCastSelectionPolicy castSelectionPolicy = OpponentCastSelectionPolicy.UtilityBest;

        [Header("Opponent Roll AI")]
        [SerializeField, Min(1)] private int opponentRollCount = 1;
        [SerializeField, Min(0)] private int targetPowerToStop;
        [SerializeField, Min(0)] private int targetImpactToStop;
        [SerializeField] private bool stopIfBeatsPlayerPower = true;
        [SerializeField] private OpponentRollStrategyType rollStrategy = OpponentRollStrategyType.Balanced;

        /// <summary>Intent 고유 ID다.</summary>
        public string IntentId => string.IsNullOrWhiteSpace(intentId) ? name : intentId;

        /// <summary>표시 이름이다.</summary>
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;

        /// <summary>짧은 설명이다.</summary>
        public string ShortDescription => shortDescription ?? string.Empty;

        /// <summary>수배지 카드 표시용 설명이다.</summary>
        public string BountyCardDescription => bountyCardDescription ?? string.Empty;

        /// <summary>Intent 카테고리다.</summary>
        public EnemyIntentCategoryType CategoryType => categoryType;

        /// <summary>Attempt 선공 주체다.</summary>
        public InitiativeOwnerType InitiativeOwner => initiativeOwner;

        /// <summary>상대 Device를 계산에 사용할지 여부다.</summary>
        public bool UseOpponentDevices => useOpponentDevices;

        /// <summary>상대가 가능한 Cast 중 최종 제출 Cast를 고르는 정책이다.</summary>
        public OpponentCastSelectionPolicy CastSelectionPolicy => castSelectionPolicy;

        /// <summary>레거시 호환용 상대 Roll 횟수이며 현재 전투 플로우는 Attempt당 기본 3 Roll을 사용한다.</summary>
        public int OpponentRollCount => Mathf.Max(1, opponentRollCount);

        /// <summary>이 CastPower 이상이면 남은 Roll이 있어도 즉시 Cast를 확정한다.</summary>
        public int TargetPowerToStop => Mathf.Max(0, targetPowerToStop);

        /// <summary>이 예상 Impact 이상이면 남은 Roll이 있어도 즉시 Cast를 확정한다.</summary>
        public int TargetImpactToStop => Mathf.Max(0, targetImpactToStop);

        /// <summary>플레이어 후공 상황에서 상대 CastPower가 플레이어 CastPower를 초과하면 즉시 Cast를 확정할지 여부다.</summary>
        public bool StopIfBeatsPlayerPower => stopIfBeatsPlayerPower;

        /// <summary>Stop 조건 미충족 시 다음 Roll 전에 유지할 주사위를 선택하는 전략이다.</summary>
        public OpponentRollStrategyType RollStrategy => rollStrategy;

        /// <summary>Core EnemyIntent 모델을 생성한다.</summary>
        public EnemyIntent ToCoreIntent(int fallbackIntentValue)
        {
            EnemyIntentType intentType = initiativeOwner == InitiativeOwnerType.Opponent
                ? EnemyIntentType.Strike
                : EnemyIntentType.None;

            return new EnemyIntent(
                intentType,
                Mathf.Max(0, fallbackIntentValue),
                !string.IsNullOrWhiteSpace(shortDescription) ? shortDescription : DisplayName,
                categoryType,
                initiativeOwner);
        }

        /// <summary>지정한 Round 고정 선공권을 사용해 Core EnemyIntent 모델을 생성한다.</summary>
        public EnemyIntent ToCoreIntent(int fallbackIntentValue, InitiativeOwnerType forcedInitiativeOwner)
        {
            EnemyIntentType intentType = forcedInitiativeOwner == InitiativeOwnerType.Opponent
                ? EnemyIntentType.Strike
                : EnemyIntentType.None;

            return new EnemyIntent(
                intentType,
                Mathf.Max(0, fallbackIntentValue),
                !string.IsNullOrWhiteSpace(shortDescription) ? shortDescription : DisplayName,
                categoryType,
                forcedInitiativeOwner);
        }
    }
}
