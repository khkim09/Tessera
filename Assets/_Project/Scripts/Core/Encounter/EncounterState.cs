using System;

namespace Tessera.Core
{
    /// <summary>단일 전투의 플레이어와 상대 HP 상태를 관리한다.</summary>
    public class EncounterState
    {
        /// <summary>플레이어 최대 HP.</summary>
        public int PlayerMaxHp { get; }

        /// <summary>상대 최대 HP.</summary>
        public int OpponentMaxHp { get; }

        /// <summary>플레이어 현재 HP.</summary>
        public int PlayerCurrentHp { get; private set; }

        /// <summary>상대 현재 HP.</summary>
        public int OpponentCurrentHp { get; private set; }

        /// <summary>플레이어가 패배했는지 확인한다.</summary>
        public bool IsPlayerDefeated => PlayerCurrentHp <= 0;

        /// <summary>상대가 패배했는지 확인한다.</summary>
        public bool IsOpponentDefeated => OpponentCurrentHp <= 0;

        /// <summary>플레이어와 상대 HP를 가진 전투 상태를 생성한다.</summary>
        public EncounterState(int playerMaxHp, int opponentMaxHp)
        {
            if (playerMaxHp <= 0)
                throw new ArgumentOutOfRangeException(nameof(playerMaxHp), "플레이어 최대 HP는 1 이상이어야 합니다.");

            if (opponentMaxHp <= 0)
                throw new ArgumentOutOfRangeException(nameof(opponentMaxHp), "상대 최대 HP는 1 이상이어야 합니다.");

            PlayerMaxHp = playerMaxHp;
            OpponentMaxHp = opponentMaxHp;
            PlayerCurrentHp = playerMaxHp;
            OpponentCurrentHp = opponentMaxHp;
        }

        /// <summary>상대에게 피해를 적용한다.</summary>
        public void ApplyDamageToOpponent(int damage)
        {
            if (damage < 0)
                throw new ArgumentOutOfRangeException(nameof(damage), "피해량은 음수가 될 수 없습니다.");

            OpponentCurrentHp = Math.Max(0, OpponentCurrentHp - damage);
        }

        /// <summary>플레이어에게 피해를 적용한다.</summary>
        public void ApplyDamageToPlayer(int damage)
        {
            if (damage < 0)
                throw new ArgumentOutOfRangeException(nameof(damage), "피해량은 음수가 될 수 없습니다.");

            PlayerCurrentHp = Math.Max(0, PlayerCurrentHp - damage);
        }

        /// <summary>플레이어 HP를 회복한다.</summary>
        public void HealPlayer(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "회복량은 음수가 될 수 없습니다.");

            PlayerCurrentHp = Math.Min(PlayerMaxHp, PlayerCurrentHp + amount);
        }

        /// <summary>상대 HP를 회복한다.</summary>
        public void HealOpponent(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "회복량은 음수가 될 수 없습니다.");

            OpponentCurrentHp = Math.Min(OpponentMaxHp, OpponentCurrentHp + amount);
        }
    }
}
