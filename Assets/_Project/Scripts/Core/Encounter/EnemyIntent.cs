using System;

namespace Tessera.Core
{
    /// <summary>상대의 현재 Attempt 행동 의도와 Initiative 정보를 나타낸다.</summary>
    public class EnemyIntent
    {
        /// <summary>Intent 타입.</summary>
        public EnemyIntentType IntentType { get; }

        /// <summary>Intent 기준 수치. 현재는 Strike 목표/압박 수치로 사용한다.</summary>
        public int Damage { get; }

        /// <summary>표시용 설명.</summary>
        public string Description { get; }

        /// <summary>Intent 카테고리.</summary>
        public EnemyIntentCategoryType CategoryType { get; }

        /// <summary>이 Intent에서 선공할 주체.</summary>
        public InitiativeOwnerType InitiativeOwner { get; }

        /// <summary>상대 행동 정보를 생성한다.</summary>
        public EnemyIntent(
            EnemyIntentType intentType,
            int damage,
            string description,
            EnemyIntentCategoryType categoryType,
            InitiativeOwnerType initiativeOwner)
        {
            if (damage < 0)
                throw new ArgumentOutOfRangeException(nameof(damage), "Intent 수치는 음수가 될 수 없습니다.");

            IntentType = intentType;
            Damage = damage;
            Description = description ?? string.Empty;
            CategoryType = categoryType;
            InitiativeOwner = initiativeOwner;
        }

        /// <summary>아무 행동도 하지 않는 상대 Intent를 생성한다.</summary>
        public static EnemyIntent None()
        {
            return new EnemyIntent(
                EnemyIntentType.None,
                0,
                "No intent.",
                EnemyIntentCategoryType.Tactics,
                InitiativeOwnerType.Player);
        }

        /// <summary>상대 선공 압박 Intent를 생성한다.</summary>
        public static EnemyIntent Strike(int damage)
        {
            return new EnemyIntent(
                EnemyIntentType.Strike,
                damage,
                $"Precise Strike target {damage}.",
                EnemyIntentCategoryType.Aggression,
                InitiativeOwnerType.Opponent);
        }
    }
}
