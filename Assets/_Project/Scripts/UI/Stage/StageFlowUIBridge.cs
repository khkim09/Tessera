using System;
using Tessera.Core;
using Tessera.Data;
using Tessera.Runtime;
using UnityEngine;

namespace Tessera.UI
{
    /// <summary>Runtime StageFlow 이벤트와 실제 UI/Gameplay Presenter를 연결하는 UI 계층 Bridge다.</summary>
    public class StageFlowUIBridge : MonoBehaviour
    {
        [Header("Gameplay")]
        [SerializeField] private TesseraGameplayBattlePresenter gameplayPresenter;
        [SerializeField] private DeviceRack3DView playerDeviceRackForShop;

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
        private IDisposable playerHPDisplayRefreshSubscription;
        private IDisposable overchargeDisplayRefreshSubscription;
        private IDisposable shopPlayerDevicesChangedSubscription;

        /// <summary>이벤트 구독 및 View 이벤트 연결을 수행한다.</summary>
        private void OnEnable()
        {
            roundStartSubscription = TesseraEventBus.Subscribe<StageRoundStartRequestedEvent>(HandleRoundStartRequested);
            bountyBoardShowSubscription = TesseraEventBus.Subscribe<BountyBoardShowRequestedEvent>(HandleBountyBoardShowRequested);
            rewardDecisionShowSubscription = TesseraEventBus.Subscribe<RewardDecisionShowRequestedEvent>(HandleRewardDecisionShowRequested);
            roundFailureShowSubscription = TesseraEventBus.Subscribe<RoundFailureShowRequestedEvent>(HandleRoundFailureShowRequested);
            shopShowSubscription = TesseraEventBus.Subscribe<StageShopShowRequestedEvent>(HandleShopShowRequested);
            playerHPDisplayRefreshSubscription = TesseraEventBus.Subscribe<PlayerHPDisplayRefreshRequestedEvent>(HandlePlayerHPDisplayRefreshRequested);
            overchargeDisplayRefreshSubscription = TesseraEventBus.Subscribe<OverchargeDisplayRefreshRequestedEvent>(HandleOverchargeDisplayRefreshRequested);
            shopPlayerDevicesChangedSubscription = TesseraEventBus.Subscribe<StageShopPlayerDevicesChangedEvent>(HandleShopPlayerDevicesChanged);

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
                shopFlowView.ProductBuyConfirmed += HandleShopProductBuyConfirmed;
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
            playerHPDisplayRefreshSubscription?.Dispose();
            overchargeDisplayRefreshSubscription?.Dispose();
            shopPlayerDevicesChangedSubscription?.Dispose();

            roundStartSubscription = null;
            bountyBoardShowSubscription = null;
            rewardDecisionShowSubscription = null;
            roundFailureShowSubscription = null;
            shopShowSubscription = null;
            playerHPDisplayRefreshSubscription = null;
            overchargeDisplayRefreshSubscription = null;
            shopPlayerDevicesChangedSubscription = null;

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
                shopFlowView.ProductBuyConfirmed -= HandleShopProductBuyConfirmed;
            }
        }

        /// <summary>Stage Round 시작 요청을 Gameplay Presenter에 전달한다.</summary>
        private void HandleRoundStartRequested(StageRoundStartRequestedEvent gameEvent)
        {
            if (gameplayPresenter == null)
                return;

            gameplayPresenter.StartRound(
                gameEvent.RuleContext,
                gameEvent.PlayerHPAtStart,
                gameEvent.StageOverchargeState,
                gameEvent.RoundDisplayName,
                gameEvent.OpponentSlotPairDevices,
                gameEvent.FirstTurnPolicy);
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
                gameEvent.Message,
                gameEvent.ProductSlots,
                gameEvent.WorkshopRules);

            RefreshPlayerDeviceRackForShop(gameEvent.RunSession);
        }

        /// <summary>StageFlow에서 변경된 플레이어 HP 표시 갱신 요청을 Gameplay Presenter에 전달한다.</summary>
        private void HandlePlayerHPDisplayRefreshRequested(PlayerHPDisplayRefreshRequestedEvent gameEvent)
        {
            if (gameplayPresenter == null)
                return;

            gameplayPresenter.RefreshExternalPlayerHPDisplay(
                gameEvent.CurrentHP,
                gameEvent.MaxHP,
                gameEvent.Reason);
        }

        /// <summary>StageFlow에서 변경된 Overcharge 표시 갱신 요청을 Gameplay Presenter에 전달한다.</summary>
        private void HandleOverchargeDisplayRefreshRequested(OverchargeDisplayRefreshRequestedEvent gameEvent)
        {
            if (gameplayPresenter == null)
                return;

            gameplayPresenter.RefreshExternalOverchargeDisplay(
                gameEvent.CurrentOvercharge,
                gameEvent.Reason);
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

        /// <summary>Shop 상품 구매 확정을 Runtime 이벤트로 변환한다.</summary>
        private void HandleShopProductBuyConfirmed(int productSlotIndex)
        {
            TesseraEventBus.Publish(new StageShopProductBuyConfirmedEvent(productSlotIndex));
        }

        /// <summary>Shop에서 Player Device 장착 변경 이벤트를 받으면 3D Rack과 Gameplay Presenter 표시를 갱신한다.</summary>
        private void HandleShopPlayerDevicesChanged(StageShopPlayerDevicesChangedEvent gameEvent)
        {
            RefreshPlayerDeviceRackForShop(gameEvent.RunSession);

            if (gameplayPresenter != null)
                gameplayPresenter.RefreshEquippedDevicesFromRunSession(gameEvent.Reason);
        }

        /// <summary>Shop 화면에서 Player DeviceRack3D를 RunSession 장착 상태와 동기화한다.</summary>
        private void RefreshPlayerDeviceRackForShop(TesseraRunSession runSession)
        {
            if (playerDeviceRackForShop == null || runSession == null)
                return;

            SlotPairDeviceDefinitionSO[] devices = new SlotPairDeviceDefinitionSO[TesseraRunSession.MaxDeviceSlots];

            for (int i = 0; i < devices.Length; i++)
                devices[i] = runSession.EquippedSlotPairDevices[i];

            playerDeviceRackForShop.SetDevices(devices);
        }

        /// <summary>Gameplay Presenter의 Round 승리 이벤트를 Runtime 이벤트로 변환한다.</summary>
        private void HandleRoundWon(CastSubmitResult result)
        {
            int playerHP = GetCurrentPlayerHPFromPresenter();
            TesseraEventBus.Publish(new GameplayRoundWonEvent(result, playerHP));
        }

        /// <summary>Gameplay Presenter의 Round 패배 이벤트를 Runtime 이벤트로 변환한다.</summary>
        private void HandleRoundLost(CastSubmitResult result)
        {
            int playerHP = GetCurrentPlayerHPFromPresenter();
            TesseraEventBus.Publish(new GameplayRoundLostEvent(result, playerHP));
        }

        /// <summary>Gameplay Presenter의 현재 RoundState에서 플레이어 HP를 읽는다.</summary>
        private int GetCurrentPlayerHPFromPresenter()
        {
            RoundState roundState = gameplayPresenter != null ? gameplayPresenter.CurrentRoundState : null;

            if (roundState == null || roundState.Encounter == null)
                return 1;

            return roundState.Encounter.PlayerCurrentHP;
        }
    }
}
