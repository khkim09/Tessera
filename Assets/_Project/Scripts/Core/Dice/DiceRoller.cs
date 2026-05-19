using System;
using System.Collections.Generic;

namespace Tessera.Core
{
    /// <summary>순수 C# 난수로 주사위를 굴린다.</summary>
    public class DiceRoller
    {
        private readonly Random _random;

        /// <summary>랜덤 시드 없이 주사위 굴림기를 생성한다.</summary>
        public DiceRoller()
        {
            _random = new Random();
        }

        /// <summary>고정 시드 기반 주사위 굴림기를 생성한다.</summary>
        public DiceRoller(int seed)
        {
            _random = new Random(seed);
        }

        /// <summary>잠기지 않은 주사위만 다시 굴린다.</summary>
        public void RollUnlocked(IReadOnlyList<DiceInstance> dice)
        {
            if (dice == null)
                throw new ArgumentNullException(nameof(dice));

            for (int i = 0; i < dice.Count; i++)
            {
                // 잠긴 주사위는 현재 눈금을 유지한다.
                if (dice[i].IsLocked) continue;

                RollSingle(dice[i]);
            }
        }

        /// <summary>주사위 하나를 잠금 여부와 상관없이 굴린다.</summary>
        public void RollSingle(DiceInstance dice)
        {
            if (dice == null)
                throw new ArgumentNullException(nameof(dice));

            int index = _random.Next(0, dice.Faces.Count);
            dice.SetCurrentFace(dice.Faces[index]);
        }

        /// <summary>기본 6면 주사위 세트를 생성하고 즉시 굴린다.</summary>
        public List<DiceInstance> CreateRolledStandardDiceSet(int diceCount)
        {
            if (diceCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(diceCount), "주사위 개수는 1개 이상이어야 합니다.");

            List<DiceInstance> dice = new List<DiceInstance>(diceCount);

            for (int i = 0; i < diceCount; i++)
            {
                DiceInstance instance = DiceInstance.CreateStandardD6();

                // 생성 직후 1번 면으로 고정되지 않도록 즉시 1회 굴린다.
                RollSingle(instance);

                dice.Add(instance);
            }

            return dice;
        }
    }
}
