using System;

namespace Tessera.Core
{
    /// <summary>Stage 단위 Overcharge와 다음 Attempt 무료 리롤 토큰을 관리한다.</summary>
    public class OverchargeState
    {
        /// <summary>현재 Stage에서 누적된 Overcharge 양.</summary>
        public int CurrentOvercharge { get; private set; }

        /// <summary>다음 Attempt에서 받을 무료 리롤 토큰 수.</summary>
        public int NextAttemptFreeRerollTokens { get; private set; }

        /// <summary>비어 있는 Overcharge 상태를 생성한다.</summary>
        public OverchargeState()
        {
            CurrentOvercharge = 0;
            NextAttemptFreeRerollTokens = 0;
        }

        /// <summary>Overcharge를 추가한다.</summary>
        public void AddOvercharge(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Overcharge 추가량은 음수가 될 수 없습니다.");

            CurrentOvercharge += amount;
        }

        /// <summary>Overcharge 사용을 시도한다.</summary>
        public bool TrySpendOvercharge(int cost)
        {
            if (cost < 0)
                throw new ArgumentOutOfRangeException(nameof(cost), "Overcharge 비용은 음수가 될 수 없습니다.");

            if (CurrentOvercharge < cost)
                return false;

            CurrentOvercharge -= cost;
            return true;
        }

        /// <summary>다음 Attempt 무료 리롤 토큰을 추가한다.</summary>
        public void AddNextAttemptFreeRerollTokens(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "무료 리롤 토큰 추가량은 음수가 될 수 없습니다.");

            NextAttemptFreeRerollTokens += amount;
        }

        /// <summary>무료 리롤 토큰 1개 사용을 시도한다.</summary>
        public bool TryConsumeFreeRerollToken()
        {
            if (NextAttemptFreeRerollTokens <= 0)
                return false;

            NextAttemptFreeRerollTokens--;
            return true;
        }

        /// <summary>다음 Attempt용 무료 리롤 토큰을 모두 꺼내고 대기 값을 비운다.</summary>
        public int DrainNextAttemptFreeRerollTokens()
        {
            int tokens = NextAttemptFreeRerollTokens;
            NextAttemptFreeRerollTokens = 0;
            return tokens;
        }

        /// <summary>초기 Core 테스트용 Broken Cast 보상을 적용한다.</summary>
        public void ApplyDefaultBrokenCastReward()
        {
            // 초기 규칙: Broken Cast 시 Overcharge +1, 다음 Attempt 무료 리롤 토큰 +1.
            AddOvercharge(1);
            AddNextAttemptFreeRerollTokens(1);
        }

        /// <summary>Stage 시작 상태로 Overcharge를 초기화한다.</summary>
        public void ResetForStageStart()
        {
            CurrentOvercharge = 0;
            NextAttemptFreeRerollTokens = 0;
        }

        /// <summary>현재 Overcharge 값을 직접 지정한다.</summary>
        public void SetCurrentOvercharge(int value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Overcharge 값은 음수가 될 수 없습니다.");

            CurrentOvercharge = value;
        }
    }
}
