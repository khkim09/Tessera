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
        [SerializeField] private StageShopFlowView shopFlowView;

        private IDisposable roundStartSubscription;
        private IDisposable bountyBoardShowSubscription;
        private IDisposable rewardDecisionShowSubscription;
        private IDisposable shopShowSubscription;

        private void OnEnable()
        {
            roundStartSubscription = TesseraEventBus.Subscribe<StageRoundStartRequestedEvent>(HandleRoundStartRequested);
            bountyBoardShowSubscription = TesseraEventBus.Subscribe<BountyBoardShowRequestedEvent>(HandleBountyBoardShowRequested);
            rewardDecisionShowSubscription = TesseraEventBus.Subscribe<RewardDecisionShowRequestedEvent>(HandleRewardDecisionShowRequested);
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
                rewardDecisionView.ChainRequested += HandleChainRequested;
                rewardDecisionView.BossRequested += HandleBossRequested;
            }

            if (shopFlowView != null)
                shopFlowView.ContinueRequested += HandleShopContinueRequested;
        }

        private void OnDisable()
        {
            roundStartSubscription?.Dispose();
            bountyBoardShowSubscription?.Dispose();
            rewardDecisionShowSubscription?.Dispose();
            shopShowSubscription?.Dispose();

            roundStartSubscription = null;
            bountyBoardShowSubscription = null;
            rewardDecisionShowSubscription = null;
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
                rewardDecisionView.ChainRequested -= HandleChainRequested;
                rewardDecisionView.BossRequested -= HandleBossRequested;
            }

            if (shopFlowView != null)
                shopFlowView.ContinueRequested -= HandleShopContinueRequested;
        }

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

        private void HandleBountyBoardShowRequested(BountyBoardShowRequestedEvent gameEvent)
        {
            if (bountyBoardView == null)
                return;

            bountyBoardView.Show(gameEvent.BoardState, gameEvent.Message);
        }

        private void HandleRewardDecisionShowRequested(RewardDecisionShowRequestedEvent gameEvent)
        {
            if (rewardDecisionView == null)
                return;

            rewardDecisionView.Show(gameEvent.BoardState, gameEvent.Message);
        }

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

        private void HandleBountySelected(StageBountyNodeState node)
        {
            TesseraEventBus.Publish(new BountyRoundSelectedEvent(node));
        }

        private void HandleCashOutRequested()
        {
            TesseraEventBus.Publish(new RewardDecisionRequestedEvent(StageRewardDecisionType.CashOut));
        }

        private void HandleChainRequested()
        {
            TesseraEventBus.Publish(new RewardDecisionRequestedEvent(StageRewardDecisionType.ChainRush));
        }

        private void HandleBossRequested()
        {
            TesseraEventBus.Publish(new RewardDecisionRequestedEvent(StageRewardDecisionType.Boss));
        }

        private void HandleShopContinueRequested()
        {
            TesseraEventBus.Publish(new StageShopContinueRequestedEvent());
        }

        private void HandleRoundWon(CastSubmitResult result)
        {
            int playerHp = GetCurrentPlayerHpFromPresenter();
            TesseraEventBus.Publish(new GameplayRoundWonEvent(result, playerHp));
        }

        private void HandleRoundLost(CastSubmitResult result)
        {
            int playerHp = GetCurrentPlayerHpFromPresenter();
            TesseraEventBus.Publish(new GameplayRoundLostEvent(result, playerHp));
        }

        private int GetCurrentPlayerHpFromPresenter()
        {
            RoundState roundState = gameplayPresenter != null ? gameplayPresenter.CurrentRoundState : null;

            if (roundState == null || roundState.Encounter == null)
                return 1;

            return roundState.Encounter.PlayerCurrentHp;
        }
    }
}
