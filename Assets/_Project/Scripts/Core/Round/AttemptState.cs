using System;

namespace Tessera.Core
{
    /// <summary>현재 Attempt 번호, 리롤 자원, 제출 가능 상태, Clash 진행 상태를 관리한다.</summary>
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

        /// <summary>이 Attempt가 Cast 제출 가능한 주사위 상태를 확보했는지 확인한다.</summary>
        public bool CanSubmitCast { get; private set; }

        /// <summary>Cast 제출 가능 상태가 된 원인.</summary>
        public CastReadinessSource CastReadinessSource { get; private set; }

        /// <summary>이 Attempt의 Clash가 이미 해결되었는지 확인한다.</summary>
        public bool IsSubmitted { get; private set; }

        /// <summary>이번 Attempt의 선공 주체.</summary>
        public InitiativeOwnerType InitiativeOwner { get; private set; }

        /// <summary>플레이어 Clash Cast 결과.</summary>
        public ClashCastResult PlayerClashResult { get; private set; }

        /// <summary>상대 Clash Cast 결과.</summary>
        public ClashCastResult OpponentClashResult { get; private set; }

        /// <summary>Clash 판정 결과.</summary>
        public ClashResolveResult ClashResolveResult { get; private set; }

        /// <summary>플레이어 결과가 확정되었는지 확인한다.</summary>
        public bool HasPlayerClashResult => PlayerClashResult != null;

        /// <summary>상대 결과가 확정되었는지 확인한다.</summary>
        public bool HasOpponentClashResult => OpponentClashResult != null;

        /// <summary>양측 결과가 모두 있어 Clash 판정 가능한지 확인한다.</summary>
        public bool CanResolveClash => HasPlayerClashResult && HasOpponentClashResult && ClashResolveResult == null;

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
            CanSubmitCast = false;
            CastReadinessSource = CastReadinessSource.None;
            IsSubmitted = false;
            InitiativeOwner = InitiativeOwnerType.Opponent;
            PlayerClashResult = null;
            OpponentClashResult = null;
            ClashResolveResult = null;
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

        /// <summary>현재 Attempt를 Cast 제출 가능 상태로 변경한다.</summary>
        public void MarkCastReady(CastReadinessSource source)
        {
            if (IsSubmitted)
                return;

            if (source == CastReadinessSource.None)
                throw new ArgumentException("Cast 제출 가능 상태의 원인은 None이 될 수 없습니다.", nameof(source));

            CanSubmitCast = true;
            CastReadinessSource = source;
        }

        /// <summary>현재 Attempt의 Cast 제출 가능 상태를 초기화한다.</summary>
        public void ClearCastReady()
        {
            if (IsSubmitted)
                return;

            CanSubmitCast = false;
            CastReadinessSource = CastReadinessSource.None;
        }

        /// <summary>Attempt의 선공 주체를 설정한다.</summary>
        public void SetInitiativeOwner(InitiativeOwnerType initiativeOwner)
        {
            if (IsSubmitted)
                return;

            InitiativeOwner = initiativeOwner;
        }

        /// <summary>플레이어 Clash Cast 결과를 기록한다.</summary>
        public void SetPlayerClashResult(ClashCastResult result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            if (IsSubmitted)
                return;

            PlayerClashResult = result;
        }

        /// <summary>상대 Clash Cast 결과를 기록한다.</summary>
        public void SetOpponentClashResult(ClashCastResult result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            if (IsSubmitted)
                return;

            OpponentClashResult = result;
        }

        /// <summary>이 Attempt를 제출 완료 상태로 변경한다.</summary>
        public void MarkSubmitted()
        {
            IsSubmitted = true;
        }

        /// <summary>Clash 판정 결과를 기록하고 Attempt를 완료 처리한다.</summary>
        public void MarkClashResolved(ClashResolveResult result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            if (IsSubmitted)
                return;

            ClashResolveResult = result;
            MarkSubmitted();
        }
    }
}
