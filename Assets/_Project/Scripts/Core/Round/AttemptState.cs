using System;

namespace Tessera.Core
{
    /// <summary>현재 Attempt 번호, 남은 리롤 수, 무료 리롤 토큰, 제출 여부를 관리한다.</summary>
    public class AttemptState
    {
        /// <summary>현재 Attempt 번호.</summary>
        public int AttemptNumber { get; }

        /// <summary>Attempt 시작 시 제공된 일반 리롤 수.</summary>
        public int MaxRegularRerolls { get; }

        /// <summary>현재 남은 일반 리롤 수.</summary>
        public int RemainingRegularRerolls { get; private set; }

        /// <summary>현재 Attempt에서 사용할 수 있는 무료 리롤 토큰 수.</summary>
        public int FreeRerollTokens { get; private set; }

        /// <summary>이 Attempt가 이미 제출되었는지 확인한다.</summary>
        public bool IsSubmitted { get; private set; }

        /// <summary>Attempt 상태를 생성한다.</summary>
        public AttemptState(int attemptNumber, int maxRegularRerolls, int freeRerollTokens)
        {
            if (attemptNumber <= 0)
                throw new ArgumentOutOfRangeException(nameof(attemptNumber), "Attempt 번호는 1 이상이어야 합니다.");

            if (maxRegularRerolls < 0)
                throw new ArgumentOutOfRangeException(nameof(maxRegularRerolls), "일반 리롤 수는 음수가 될 수 없습니다.");

            if (freeRerollTokens < 0)
                throw new ArgumentOutOfRangeException(nameof(freeRerollTokens), "무료 리롤 토큰 수는 음수가 될 수 없습니다.");

            AttemptNumber = attemptNumber;
            MaxRegularRerolls = maxRegularRerolls;
            RemainingRegularRerolls = maxRegularRerolls;
            FreeRerollTokens = freeRerollTokens;
            IsSubmitted = false;
        }

        /// <summary>일반 리롤 1회 사용을 시도한다.</summary>
        public bool TrySpendRegularReroll()
        {
            if (IsSubmitted)
                return false;

            if (RemainingRegularRerolls <= 0)
                return false;

            RemainingRegularRerolls--;
            return true;
        }

        /// <summary>무료 리롤 토큰 1개 사용을 시도한다.</summary>
        public bool TrySpendFreeRerollToken()
        {
            if (IsSubmitted)
                return false;

            if (FreeRerollTokens <= 0)
                return false;

            FreeRerollTokens--;
            return true;
        }

        /// <summary>현재 Attempt에 무료 리롤 토큰을 추가한다.</summary>
        public void AddFreeRerollTokens(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "무료 리롤 토큰 추가량은 음수가 될 수 없습니다.");

            FreeRerollTokens += amount;
        }

        /// <summary>이 Attempt를 제출 완료 상태로 변경한다.</summary>
        public void MarkSubmitted()
        {
            IsSubmitted = true;
        }
    }
}
