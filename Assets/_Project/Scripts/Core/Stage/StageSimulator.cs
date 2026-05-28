using System;

namespace Tessera.Core
{
    /// <summary>구형 순차 Stage 정의와 진행 상태를 이용해 Stage 안의 Round 흐름을 제어한다. Chain-Bounty 전환 후 삭제 후보.</summary>
    public class StageSimulator
    {
        private readonly CoreRoundSimulator roundSimulator;

        /// <summary>시드 없는 Stage 시뮬레이터를 생성한다.</summary>
        public StageSimulator()
        {
            roundSimulator = new CoreRoundSimulator();
        }

        /// <summary>고정 시드 기반 Stage 시뮬레이터를 생성한다.</summary>
        public StageSimulator(int seed)
        {
            roundSimulator = new CoreRoundSimulator(seed);
        }

        /// <summary>지정한 Stage 정의로 새 Stage 진행을 시작한다.</summary>
        public StageProgressState StartStage(StageDefinition stageDefinition)
        {
            return new StageProgressState(stageDefinition);
        }

        /// <summary>디버그용 Stage 1 진행을 시작한다.</summary>
        public StageProgressState StartDebugStageOne()
        {
            return StartStage(StageDefinition.CreateDebugStageOne());
        }

        /// <summary>현재 Stage Round의 Core RoundState를 생성한다.</summary>
        public RoundState StartCurrentRound(
            StageProgressState stageProgressState,
            int playerCurrentHp,
            OverchargeState stageOverchargeState)
        {
            if (stageProgressState == null)
                throw new ArgumentNullException(nameof(stageProgressState));

            StageRoundDefinition currentRound = stageProgressState.GetCurrentRound();

            if (currentRound == null)
                throw new InvalidOperationException("시작할 수 있는 현재 Stage Round가 없습니다.");

            return roundSimulator.StartRound(
                currentRound.RuleContext,
                playerCurrentHp,
                stageOverchargeState);
        }

        /// <summary>현재 Stage Round를 클리어 처리한다.</summary>
        public void CompleteCurrentRound(StageProgressState stageProgressState)
        {
            if (stageProgressState == null)
                throw new ArgumentNullException(nameof(stageProgressState));

            stageProgressState.CompleteCurrentRound();
        }

        /// <summary>현재 Stage Round Skip을 시도한다.</summary>
        public bool TrySkipCurrentRound(StageProgressState stageProgressState)
        {
            if (stageProgressState == null)
                throw new ArgumentNullException(nameof(stageProgressState));

            return stageProgressState.TrySkipCurrentRound();
        }

        /// <summary>현재 Stage Round를 실패 처리한다.</summary>
        public void FailCurrentRound(StageProgressState stageProgressState)
        {
            if (stageProgressState == null)
                throw new ArgumentNullException(nameof(stageProgressState));

            stageProgressState.FailCurrentRound();
        }
    }
}
