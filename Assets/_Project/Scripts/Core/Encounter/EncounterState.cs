using System;

namespace Tessera.Core
{
    /// <summary>단일 전투의 플레이어와 상대 HP 상태를 관리한다.</summary>
    public class EncounterState
    {
        /// <summary>플레이어 최대 HP.</summary>
        public int PlayerMaxHP { get; }

        /// <summary>상대 최대 HP.</summary>
        public int OpponentMaxHP { get; }

        /// <summary>플레이어 현재 HP.</summary>
        public int PlayerCurrentHP { get; private set; }

        /// <summary>상대 현재 HP.</summary>
        public int OpponentCurrentHP { get; private set; }

        /// <summary>플레이어가 패배했는지 확인한다.</summary>
        public bool IsPlayerDefeated => PlayerCurrentHP <= 0;

        /// <summary>상대가 패배했는지 확인한다.</summary>
        public bool IsOpponentDefeated => OpponentCurrentHP <= 0;

        /// <summary>지정 현재 HP를 가진 전투 상태를 생성한다.</summary>
        public EncounterState(int playerMaxHP, int opponentMaxHP, int playerCurrentHP, int opponentCurrentHP)
        {
            if (playerMaxHP <= 0)
                throw new ArgumentOutOfRangeException(nameof(playerMaxHP), "플레이어 최대 HP는 1 이상이어야 합니다.");

            if (opponentMaxHP <= 0)
                throw new ArgumentOutOfRangeException(nameof(opponentMaxHP), "상대 최대 HP는 1 이상이어야 합니다.");

            PlayerMaxHP = playerMaxHP;
            OpponentMaxHP = opponentMaxHP;
            PlayerCurrentHP = ClampHP(playerCurrentHP, playerMaxHP);
            OpponentCurrentHP = ClampHP(opponentCurrentHP, opponentMaxHP);
        }

        /// <summary>상대에게 피해를 적용한다.</summary>
        public void ApplyDamageToOpponent(int damage)
        {
            if (damage < 0)
                throw new ArgumentOutOfRangeException(nameof(damage), "피해량은 음수가 될 수 없습니다.");

            OpponentCurrentHP = Math.Max(0, OpponentCurrentHP - damage);
        }

        /// <summary>플레이어에게 피해를 적용한다.</summary>
        public void ApplyDamageToPlayer(int damage)
        {
            if (damage < 0)
                throw new ArgumentOutOfRangeException(nameof(damage), "피해량은 음수가 될 수 없습니다.");

            PlayerCurrentHP = Math.Max(0, PlayerCurrentHP - damage);
        }

        /// <summary>플레이어 HP를 회복한다.</summary>
        public void HealPlayer(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "회복량은 음수가 될 수 없습니다.");

            PlayerCurrentHP = Math.Min(PlayerMaxHP, PlayerCurrentHP + amount);
        }

        /// <summary>상대 HP를 회복한다.</summary>
        public void HealOpponent(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "회복량은 음수가 될 수 없습니다.");

            OpponentCurrentHP = Math.Min(OpponentMaxHP, OpponentCurrentHP + amount);
        }

        private static int ClampHP(int value, int maxHP)
        {
            if (value < 0)
                return 0;

            if (value > maxHP)
                return maxHP;

            return value;
        }
    }
}
