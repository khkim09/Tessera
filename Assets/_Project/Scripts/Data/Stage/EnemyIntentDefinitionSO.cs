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
        [SerializeField] private bool chooseBestAvailableCast = true;

        /// <summary>Intent 고유 ID.</summary>
        public string IntentId => string.IsNullOrWhiteSpace(intentId) ? name : intentId;

        /// <summary>표시 이름.</summary>
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;

        /// <summary>짧은 설명.</summary>
        public string ShortDescription => shortDescription ?? string.Empty;

        /// <summary>수배지 카드 표시용 설명.</summary>
        public string BountyCardDescription => bountyCardDescription ?? string.Empty;

        /// <summary>Intent 카테고리.</summary>
        public EnemyIntentCategoryType CategoryType => categoryType;

        /// <summary>Attempt 선공 주체.</summary>
        public InitiativeOwnerType InitiativeOwner => initiativeOwner;

        /// <summary>상대 Device를 계산에 사용할지 여부.</summary>
        public bool UseOpponentDevices => useOpponentDevices;

        /// <summary>상대가 가능한 Cast 중 최적 Cast를 고를지 여부.</summary>
        public bool ChooseBestAvailableCast => chooseBestAvailableCast;

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
    }
}
