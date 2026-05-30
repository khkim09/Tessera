using System;
using Tessera.Core;
using Tessera.Runtime;
using UnityEngine;

namespace Tessera.UI
{
    /// <summary>Runtime StageFlow 이벤트와 실제 UI/Gameplay Presenter를 연결하는 UI 계층 Bridge다.</summary>
    public class StageFlowUIBridge : MonoBehaviour
    {
        [Header("Gameplay")]
        [SerializeField] private TesseraGameplayBattlePresenter gameplayPresenter;

        [Header("Stage UI")]
        [SerializeField] private BountyBoardView bountyBoardView;
        [SerializeField] private StageRewardDecisionView rewardDecisionView;
        [SerializeField] private RoundFailureDecisionView roundFailureDecisionView;
        [SerializeField] private StageShopFlowView shopFlowView;

        private IDisposable roundStartSubscription;
        private IDisposable bountyBoardShowSubscription;
        private IDisposable rewardDecisionShowSubscription;
        private IDisposable roundFailureShowSubscription;
        private IDisposable shopShowSubscription;

        /// <summary>이벤트 구독 및 View 이벤트 연결을 수행한다.</summary>
        private void OnEnable()
        {
            roundStartSubscription = TesseraEventBus.Subscribe<StageRoundStartRequestedEvent>(HandleRoundStartRequested);
            bountyBoardShowSubscription = TesseraEventBus.Subscribe<BountyBoardShowRequestedEvent>(HandleBountyBoardShowRequested);
            rewardDecisionShowSubscription = TesseraEventBus.Subscribe<RewardDecisionShowRequestedEvent>(HandleRewardDecisionShowRequested);
            roundFailureShowSubscription = TesseraEventBus.Subscribe<RoundFailureShowRequestedEvent>(HandleRoundFailureShowRequested);
            shopShowSubscription = TesseraEventBus.Subscribe<StageShopShowRequestedEvent>(HandleShopShowRequested);

            if (gameplayPresenter != null)
            {
                gameplayPresenter.RoundWon += HandleRoundWon;
                gameplayPresenter.RoundLost += HandleRoundLost;
            }

            if (bountyBoardView != null)
                bountyBoardView.RoundSelected += HandleBountySelected;

            if (rewardDecisionView != null)
            {
                rewardDecisionView.CashOutRequested += HandleCashOutRequested;
                rewardDecisionView.KeepFightingRequested += HandleKeepFightingRequested;
            }

            if (roundFailureDecisionView != null)
            {
                roundFailureDecisionView.RetryRequested += HandleFailureRetryRequested;
                roundFailureDecisionView.RetreatRequested += HandleFailureRetreatRequested;
                roundFailureDecisionView.AbandonRequested += HandleFailureAbandonRequested;
            }

            if (shopFlowView != null)
            {
                shopFlowView.ContinueRequested += HandleShopContinueRequested;
                shopFlowView.RepairRequested += HandleShopRepairRequested;
                shopFlowView.UpgradeTierRequested += HandleShopUpgradeTierRequested;
            }
        }

        /// <summary>이벤트 구독 및 View 이벤트 연결을 해제한다.</summary>
        private void OnDisable()
        {
            roundStartSubscription?.Dispose();
            bountyBoardShowSubscription?.Dispose();
            rewardDecisionShowSubscription?.Dispose();
            roundFailureShowSubscription?.Dispose();
            shopShowSubscription?.Dispose();

            roundStartSubscription = null;
            bountyBoardShowSubscription = null;
            rewardDecisionShowSubscription = null;
            roundFailureShowSubscription = null;
            shopShowSubscription = null;

            if (gameplayPresenter != null)
            {
                gameplayPresenter.RoundWon -= HandleRoundWon;
                gameplayPresenter.RoundLost -= HandleRoundLost;
            }

            if (bountyBoardView != null)
                bountyBoardView.RoundSelected -= HandleBountySelected;

            if (rewardDecisionView != null)
            {
                rewardDecisionView.CashOutRequested -= HandleCashOutRequested;
                rewardDecisionView.KeepFightingRequested -= HandleKeepFightingRequested;
            }

            if (roundFailureDecisionView != null)
            {
                roundFailureDecisionView.RetryRequested -= HandleFailureRetryRequested;
                roundFailureDecisionView.RetreatRequested -= HandleFailureRetreatRequested;
                roundFailureDecisionView.AbandonRequested -= HandleFailureAbandonRequested;
            }

            if (shopFlowView != null)
            {
                shopFlowView.ContinueRequested -= HandleShopContinueRequested;
                shopFlowView.RepairRequested -= HandleShopRepairRequested;
                shopFlowView.UpgradeTierRequested -= HandleShopUpgradeTierRequested;
            }
        }

        /// <summary>Stage Round 시작 요청을 Gameplay Presenter에 전달한다.</summary>
        private void HandleRoundStartRequested(StageRoundStartRequestedEvent gameEvent)
        {
            if (gameplayPresenter == null)
                return;

            gameplayPresenter.StartRound(
                gameEvent.RuleContext,
                gameEvent.PlayerHpAtStart,
                gameEvent.StageOverchargeState,
                gameEvent.RoundDisplayName);
        }

        /// <summary>Bounty Board 표시 요청을 View에 전달한다.</summary>
        private void HandleBountyBoardShowRequested(BountyBoardShowRequestedEvent gameEvent)
        {
            if (bountyBoardView == null)
                return;

            bountyBoardView.Show(gameEvent.BoardState, gameEvent.Message);
        }

        /// <summary>Reward Decision 표시 요청을 View에 전달한다.</summary>
        private void HandleRewardDecisionShowRequested(RewardDecisionShowRequestedEvent gameEvent)
        {
            if (rewardDecisionView == null)
                return;

            rewardDecisionView.Show(gameEvent.BoardState, gameEvent.Message);
        }

        /// <summary>Round Failure Decision 표시 요청을 View에 전달한다.</summary>
        private void HandleRoundFailureShowRequested(RoundFailureShowRequestedEvent gameEvent)
        {
            if (roundFailureDecisionView == null)
                return;

            roundFailureDecisionView.Show(
                gameEvent.RunSession,
                gameEvent.BoardState,
                gameEvent.RetryMoneyCost,
                gameEvent.Message);
        }

        /// <summary>Workshop Shell 표시 요청을 View에 전달한다.</summary>
        private void HandleShopShowRequested(StageShopShowRequestedEvent gameEvent)
        {
            if (shopFlowView == null)
                return;

            shopFlowView.Show(
                gameEvent.RunSession,
                gameEvent.BoardState,
                gameEvent.ReasonType,
                gameEvent.Message);
        }

        /// <summary>View의 수배지 선택을 Runtime 이벤트로 변환한다.</summary>
        private void HandleBountySelected(StageBountyNodeState node)
        {
            TesseraEventBus.Publish(new BountyRoundSelectedEvent(node));
        }

        /// <summary>CashOut 요청을 Runtime 이벤트로 변환한다.</summary>
        private void HandleCashOutRequested()
        {
            TesseraEventBus.Publish(new RewardDecisionRequestedEvent(StageRewardDecisionType.CashOut));
        }

        /// <summary>Keep Fighting 요청을 Runtime 이벤트로 변환한다.</summary>
        private void HandleKeepFightingRequested()
        {
            TesseraEventBus.Publish(new RewardDecisionRequestedEvent(StageRewardDecisionType.ChainRush));
        }

        /// <summary>Retry 요청을 Runtime 이벤트로 변환한다.</summary>
        private void HandleFailureRetryRequested()
        {
            TesseraEventBus.Publish(new RoundFailureDecisionRequestedEvent(RoundFailureDecisionType.Retry));
        }

        /// <summary>Retreat 요청을 Runtime 이벤트로 변환한다.</summary>
        private void HandleFailureRetreatRequested()
        {
            TesseraEventBus.Publish(new RoundFailureDecisionRequestedEvent(RoundFailureDecisionType.Retreat));
        }

        /// <summary>Abandon 요청을 Runtime 이벤트로 변환한다.</summary>
        private void HandleFailureAbandonRequested()
        {
            TesseraEventBus.Publish(new RoundFailureDecisionRequestedEvent(RoundFailureDecisionType.Abandon));
        }

        /// <summary>Workshop Continue 요청을 Runtime 이벤트로 변환한다.</summary>
        private void HandleShopContinueRequested()
        {
            TesseraEventBus.Publish(new StageShopContinueRequestedEvent());
        }

        /// <summary>Workshop Repair 요청을 Runtime 이벤트로 변환한다.</summary>
        private void HandleShopRepairRequested()
        {
            TesseraEventBus.Publish(new StageShopRepairRequestedEvent());
        }

        /// <summary>Workshop Tier 업그레이드 요청을 Runtime 이벤트로 변환한다.</summary>
        private void HandleShopUpgradeTierRequested()
        {
            TesseraEventBus.Publish(new StageShopUpgradeTierRequestedEvent());
        }

        /// <summary>Gameplay Presenter의 Round 승리 이벤트를 Runtime 이벤트로 변환한다.</summary>
        private void HandleRoundWon(CastSubmitResult result)
        {
            int playerHp = GetCurrentPlayerHpFromPresenter();
            TesseraEventBus.Publish(new GameplayRoundWonEvent(result, playerHp));
        }

        /// <summary>Gameplay Presenter의 Round 패배 이벤트를 Runtime 이벤트로 변환한다.</summary>
        private void HandleRoundLost(CastSubmitResult result)
        {
            int playerHp = GetCurrentPlayerHpFromPresenter();
            TesseraEventBus.Publish(new GameplayRoundLostEvent(result, playerHp));
        }

        /// <summary>Gameplay Presenter의 현재 RoundState에서 플레이어 HP를 읽는다.</summary>
        private int GetCurrentPlayerHpFromPresenter()
        {
            RoundState roundState = gameplayPresenter != null ? gameplayPresenter.CurrentRoundState : null;

            if (roundState == null || roundState.Encounter == null)
                return 1;

            return roundState.Encounter.PlayerCurrentHp;
        }
    }
}
