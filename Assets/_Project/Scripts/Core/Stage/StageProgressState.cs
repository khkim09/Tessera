using System;
using System.Collections.Generic;

namespace Tessera.Core
{
    /// <summary>현재 Stage 진행 위치, Round 완료 상태, 누적 보상을 관리한다.</summary>
    public class StageProgressState
    {
        private readonly List<StageRoundCompletionType> _roundCompletionStates;

        /// <summary>진행 중인 Stage 정의.</summary>
        public StageDefinition StageDefinition { get; }

        /// <summary>현재 진행 대상 Round 인덱스.</summary>
        public int CurrentRoundIndex { get; private set; }

        /// <summary>현재 Stage에서 누적된 Parts 보상.</summary>
        public int EarnedParts { get; private set; }

        /// <summary>현재 Stage가 클리어되었는지 확인한다.</summary>
        public bool IsStageCleared { get; private set; }

        /// <summary>현재 Stage가 실패했는지 확인한다.</summary>
        public bool IsStageFailed { get; private set; }

        /// <summary>Round별 완료 상태 목록.</summary>
        public IReadOnlyList<StageRoundCompletionType> RoundCompletionStates => _roundCompletionStates;

        /// <summary>Stage 진행 상태를 생성한다.</summary>
        public StageProgressState(StageDefinition stageDefinition)
        {
            StageDefinition = stageDefinition ?? throw new ArgumentNullException(nameof(stageDefinition));
            CurrentRoundIndex = 0;
            EarnedParts = 0;
            IsStageCleared = false;
            IsStageFailed = false;
            _roundCompletionStates = new List<StageRoundCompletionType>(stageDefinition.Rounds.Count);

            for (int i = 0; i < stageDefinition.Rounds.Count; i++)
                _roundCompletionStates.Add(StageRoundCompletionType.NotStarted);
        }

        /// <summary>현재 Round 정의를 반환한다.</summary>
        public StageRoundDefinition GetCurrentRound()
        {
            if (IsStageCleared || IsStageFailed)
                return null;

            if (CurrentRoundIndex < 0 || CurrentRoundIndex >= StageDefinition.Rounds.Count)
                return null;

            return StageDefinition.GetRound(CurrentRoundIndex);
        }

        /// <summary>현재 Round를 클리어 처리하고 보상을 지급한다.</summary>
        public void CompleteCurrentRound()
        {
            StageRoundDefinition currentRound = GetCurrentRound();

            if (currentRound == null)
                throw new InvalidOperationException("완료할 수 있는 현재 Round가 없습니다.");

            _roundCompletionStates[CurrentRoundIndex] = StageRoundCompletionType.Completed;
            EarnedParts += currentRound.RewardParts;
            AdvanceRoundIndex();
        }

        /// <summary>현재 Round를 Skip 처리하고 Skip 보상을 지급한다.</summary>
        public bool TrySkipCurrentRound()
        {
            StageRoundDefinition currentRound = GetCurrentRound();

            if (currentRound == null)
                return false;

            if (!currentRound.CanSkip)
                return false;

            _roundCompletionStates[CurrentRoundIndex] = StageRoundCompletionType.Skipped;
            EarnedParts += currentRound.SkipRewardParts;
            AdvanceRoundIndex();
            return true;
        }

        /// <summary>현재 Round를 실패 처리하고 Stage를 실패 상태로 만든다.</summary>
        public void FailCurrentRound()
        {
            StageRoundDefinition currentRound = GetCurrentRound();

            if (currentRound == null)
                throw new InvalidOperationException("실패 처리할 수 있는 현재 Round가 없습니다.");

            _roundCompletionStates[CurrentRoundIndex] = StageRoundCompletionType.Failed;
            IsStageFailed = true;
        }

        private void AdvanceRoundIndex()
        {
            CurrentRoundIndex++;

            if (CurrentRoundIndex >= StageDefinition.Rounds.Count)
                IsStageCleared = true;
        }
    }
}
