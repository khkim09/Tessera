using System;

namespace Tessera.Core
{
    /// <summary>상대가 플레이어 Cast 이후 실행할 의도 행동을 표현한다.</summary>
    public class EnemyIntent
    {
        /// <summary>상대 행동 종류.</summary>
        public EnemyIntentType IntentType { get; }

        /// <summary>상대가 플레이어에게 줄 피해량.</summary>
        public int Damage { get; }

        /// <summary>디버그 및 UI 표시용 설명.</summary>
        public string Description { get; }

        /// <summary>상대 행동 정보를 생성한다.</summary>
        public EnemyIntent(EnemyIntentType intentType, int damage, string description)
        {
            if (damage < 0)
                throw new ArgumentOutOfRangeException(nameof(damage), "상대 피해량은 음수가 될 수 없습니다.");

            IntentType = intentType;
            Damage = damage;
            Description = description ?? string.Empty;
        }

        /// <summary>아무 행동도 하지 않는 상대 Intent를 생성한다.</summary>
        public static EnemyIntent None()
        {
            return new EnemyIntent(EnemyIntentType.None, 0, "No intent.");
        }

        /// <summary>고정 피해를 주는 상대 Strike Intent를 생성한다.</summary>
        public static EnemyIntent Strike(int damage)
        {
            return new EnemyIntent(EnemyIntentType.Strike, damage, $"Strike for {damage} damage.");
        }
    }
}
