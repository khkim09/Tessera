namespace Tessera.Core
{
    /// <summary>상대 Intent 실행 결과를 담는다.</summary>
    public class EnemyIntentResult
    {
        /// <summary>상대 Intent가 실제로 실행되었는지 확인한다.</summary>
        public bool DidExecute { get; }

        /// <summary>실행된 상대 Intent 종류.</summary>
        public EnemyIntentType IntentType { get; }

        /// <summary>플레이어에게 적용된 피해량.</summary>
        public int DamageToPlayer { get; }

        /// <summary>피해 적용 후 플레이어 HP.</summary>
        public int PlayerHPAfterDamage { get; }

        /// <summary>디버그 및 UI 표시용 메시지.</summary>
        public string Message { get; }

        /// <summary>상대 Intent 실행 결과를 생성한다.</summary>
        public EnemyIntentResult(
            bool didExecute,
            EnemyIntentType intentType,
            int damageToPlayer,
            int playerHPAfterDamage,
            string message)
        {
            DidExecute = didExecute;
            IntentType = intentType;
            DamageToPlayer = damageToPlayer;
            PlayerHPAfterDamage = playerHPAfterDamage;
            Message = message;
        }

        /// <summary>실행되지 않은 상대 Intent 결과를 생성한다.</summary>
        public static EnemyIntentResult NotExecuted(int playerHP)
        {
            return new EnemyIntentResult(false, EnemyIntentType.None, 0, playerHP, "Enemy intent was not executed.");
        }
    }
}
