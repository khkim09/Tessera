using System;
using Cysharp.Threading.Tasks;
using Tessera.Core;
using Tessera.Data;
using Tessera.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tessera.UI
{
    /// <summary>Runtime StageFlow 이벤트와 실제 UI/Gameplay Presenter를 연결하는 UI 계층 Bridge다.</summary>
    public class StageFlowUIBridge : MonoBehaviour
    {
        [Header("Gameplay")]
        [SerializeField] private TesseraGameplayBattlePresenter gameplayPresenter;
        [SerializeField] private DeviceRack3DView playerDeviceRackForShop;
        [SerializeField] private TesseraCameraPoseController cameraPoseController;

        [Header("Stage UI")]
        [SerializeField] private BountyBoardView bountyBoardView;
        [SerializeField] private StageRewardDecisionView rewardDecisionView;
        [SerializeField] private RoundFailureDecisionView roundFailureDecisionView;
        [SerializeField] private StageShopFlowView shopFlowView;

        [Header("Shop Device Hover UI")]
        [SerializeField] private DeviceSlotHoverActionUIView[] playerDeviceSlotHoverActionUI = new DeviceSlotHoverActionUIView[5];
        [SerializeField] private int defaultDeviceSellRefundMoney = 1;

        [Header("Debug")]
        [SerializeField] private bool enableShopDeviceInputDebugLog = true;

        private bool isShopVisible;
        private int draggingShopDeviceSlotIndex = -1; // 드래그 중인 deviceslot
        private bool isShopDeviceDragging; // 드래그 발생 여부
        private int hoveredDeviceSlotIndex = -1; // 호버 중인 DeviceSlot
        private int visibleDeviceHoverUIIndex = -1; // 현재 표시 중인 슬롯별 Hover UI
        private int hoverHideRequestSerial; // Hide 요청 무효화
        private bool isDeviceSlotSwapAllowed; // 현재 Stage/UI 흐름상 Device swap 허용 여부
        private bool wasDeviceDropHandled; // 현재 Drag에서 Drop 이벤트가 처리되었는지 여부
        private bool wasDeviceDropAccepted; // 현재 Drag에서 유효 Drop이 승인되었는지 여부
        private bool shouldShakeOnDeviceDragEnd; // DragEnd 복귀 시 shake 여부
        private GameObject draggingDeviceViewObject; // 드래그 중인 장착 Device View
        private SlotPairDeviceDefinitionSO draggingDeviceDefinition; // 드래그 중인 Device 데이터
        private Vector3 lastDraggingDeviceWorldPosition; // dead-zone 유지용 마지막 드래그 위치

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

            if (playerDeviceRackForShop != null)
            {
                playerDeviceRackForShop.SlotHoverEntered += HandleShopDeviceSlotHoverEntered;
                playerDeviceRackForShop.SlotHoverExited += HandleShopDeviceSlotHoverExited;
                playerDeviceRackForShop.SlotDropped += HandleShopDeviceSlotDropped;

                playerDeviceRackForShop.SlotDragStartedWithPointer += HandleDeviceSlotDragStartedWithPointer;
                playerDeviceRackForShop.SlotDraggedWithPointer += HandleDeviceSlotDraggedWithPointer;
                playerDeviceRackForShop.SlotDragEndedWithPointer += HandleDeviceSlotDragEndedWithPointer;

                LogShopDeviceInput($"[Subscribe] Rack={playerDeviceRackForShop.name}, Hover/Drag handlers registered.");
            }
            else
            {
                LogShopDeviceInputWarning("[Subscribe] playerDeviceRackForShop is not assigned. DeviceSlot sell/swap input cannot be received.");
            }

            SubscribeDeviceSlotHoverActionUIs();
            HideAllDeviceHoverUIs();
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

            if (playerDeviceRackForShop != null)
            {
                playerDeviceRackForShop.SlotHoverEntered -= HandleShopDeviceSlotHoverEntered;
                playerDeviceRackForShop.SlotHoverExited -= HandleShopDeviceSlotHoverExited;
                playerDeviceRackForShop.SlotDropped -= HandleShopDeviceSlotDropped;

                playerDeviceRackForShop.SlotDragStartedWithPointer -= HandleDeviceSlotDragStartedWithPointer;
                playerDeviceRackForShop.SlotDraggedWithPointer -= HandleDeviceSlotDraggedWithPointer;
                playerDeviceRackForShop.SlotDragEndedWithPointer -= HandleDeviceSlotDragEndedWithPointer;
            }

            UnsubscribeDeviceSlotHoverActionUIs();
        }

        /// <summary>Stage Round 시작 요청을 Gameplay Presenter에 전달한다.</summary>
        private void HandleRoundStartRequested(StageRoundStartRequestedEvent gameEvent)
        {
            // Round 시작 시 전투용 사선 카메라로 복귀한다.
            if (cameraPoseController != null)
                cameraPoseController.MoveToBattleView("Round started.");

            if (gameplayPresenter == null)
                return;

            // Round 시작부터 PlayerIdle/EnemyTurn 구간 Device swap은 다시 허용한다.
            isShopVisible = false;
            isDeviceSlotSwapAllowed = true;
            ResetShopDeviceInteractionState();
            HideAllDeviceHoverUIs();

            LogShopDeviceInput("[RoundStart] Gameplay device swap enabled.");

            gameplayPresenter.StartRound(
                gameEvent.RuleContext,
                gameEvent.PlayerHPAtStart,
                gameEvent.StageOverchargeState,
                gameEvent.RoundDisplayName,
                gameEvent.OpponentSlotPairDevices,
                gameEvent.RoundDefinition,
                gameEvent.OpeningIntent);
        }

        /// <summary>Bounty Board 표시 요청을 View에 전달한다.</summary>
        private void HandleBountyBoardShowRequested(BountyBoardShowRequestedEvent gameEvent)
        {
            if (bountyBoardView == null)
                return;

            isShopVisible = false;
            isDeviceSlotSwapAllowed = false;
            ResetShopDeviceInteractionState();
            HideAllDeviceHoverUIs();

            LogShopDeviceInput("[BountyBoard] Device swap disabled.");

            bountyBoardView.Show(gameEvent.BoardState, gameEvent.Message);
        }

        /// <summary>Reward Decision 표시 요청을 View에 전달한다.</summary>
        private void HandleRewardDecisionShowRequested(RewardDecisionShowRequestedEvent gameEvent)
        {
            if (rewardDecisionView == null)
                return;

            isShopVisible = false;
            isDeviceSlotSwapAllowed = false;
            ResetShopDeviceInteractionState();
            HideAllDeviceHoverUIs();

            LogShopDeviceInput("[RewardDecision] Device swap disabled.");

            rewardDecisionView.Show(gameEvent.BoardState, gameEvent.Message);
        }

        /// <summary>Round Failure Decision 표시 요청을 View에 전달한다.</summary>
        private void HandleRoundFailureShowRequested(RoundFailureShowRequestedEvent gameEvent)
        {
            if (roundFailureDecisionView == null)
                return;

            isShopVisible = false;
            isDeviceSlotSwapAllowed = false;
            ResetShopDeviceInteractionState();
            HideAllDeviceHoverUIs();

            LogShopDeviceInput("[RoundFailure] Device swap disabled.");

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

            // Shop 진입 시 Device 관리가 편하도록 TopDown 카메라로 전환한다.
            if (cameraPoseController != null)
                cameraPoseController.MoveToShopView("Shop opened.");

            // Shop 진입/구성 중에는 아직 swap을 금지한다.
            isShopVisible = true;
            isDeviceSlotSwapAllowed = false;
            ResetShopDeviceInteractionState();
            HideAllDeviceHoverUIs();

            LogShopDeviceInput("[ShowShop] Shop device input preparing.");

            shopFlowView.Show(
                gameEvent.RunSession,
                gameEvent.BoardState,
                gameEvent.ReasonType,
                gameEvent.Message,
                gameEvent.ProductSlots,
                gameEvent.WorkshopRules);

            RefreshPlayerDeviceRackForShop(gameEvent.RunSession);

            isDeviceSlotSwapAllowed = true;
            LogShopDeviceInput("[ShowShop] Shop device input enabled.");
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
            isDeviceSlotSwapAllowed = false;
            ResetShopDeviceInteractionState();
            HideAllDeviceHoverUIs();

            TesseraEventBus.Publish(new BountyRoundSelectedEvent(node));
        }

        /// <summary>CashOut 요청을 Runtime 이벤트로 변환한다.</summary>
        private void HandleCashOutRequested()
        {
            isDeviceSlotSwapAllowed = false;
            ResetShopDeviceInteractionState();
            HideAllDeviceHoverUIs();

            TesseraEventBus.Publish(new RewardDecisionRequestedEvent(StageRewardDecisionType.CashOut));
        }

        /// <summary>Keep Fighting 요청을 Runtime 이벤트로 변환한다.</summary>
        private void HandleKeepFightingRequested()
        {
            isDeviceSlotSwapAllowed = false;
            ResetShopDeviceInteractionState();
            HideAllDeviceHoverUIs();

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

        /// <summary>Shop Continue 요청을 Runtime 이벤트로 변환한다.</summary>
        private void HandleShopContinueRequested()
        {
            // Continue 직후 BountyBoard 또는 Round 흐름으로 돌아가므로 우선 전투 카메라로 복귀시킨다.
            if (cameraPoseController != null)
                cameraPoseController.MoveToBattleView("Shop continue requested.");

            // Shop Continue 후에는 DeviceSlot 판매/스왑 입력을 막는다.
            isShopVisible = false;
            isDeviceSlotSwapAllowed = false;
            ResetShopDeviceInteractionState();
            HideAllDeviceHoverUIs();

            LogShopDeviceInput("[ShopContinue] Shop device input disabled.");

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

        /// <summary>Shop에서 Player Device 장착 변경 이벤트를 받으면 RunSession 기준으로 3D Rack과 Gameplay Presenter 표시를 갱신한다.</summary>
        private void HandleShopPlayerDevicesChanged(StageShopPlayerDevicesChangedEvent gameEvent)
        {
            HideAllDeviceHoverUIs();

            if (isShopDeviceDragging && !wasDeviceDropAccepted)
            {
                LogShopDeviceInput("[DevicesChangedIgnored] Reason=DraggingNotAccepted");
                return;
            }

            RefreshPlayerDeviceRackForShop(gameEvent.RunSession);

            if (gameplayPresenter != null)
                gameplayPresenter.RefreshEquippedDevicesFromRunSession(gameEvent.Reason);

            RefreshDeviceHoverUIFromCurrentPointerAfterFrameAsync().Forget();
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
        private void HandleRoundWon(ClashResolveResult result)
        {
            isDeviceSlotSwapAllowed = false;
            ResetShopDeviceInteractionState();
            HideAllDeviceHoverUIs();

            int playerHP = GetCurrentPlayerHPFromPresenter();
            TesseraEventBus.Publish(new GameplayRoundWonEvent(result, playerHP));
        }

        /// <summary>Gameplay Presenter의 Round 패배 이벤트를 Runtime 이벤트로 변환한다.</summary>
        private void HandleRoundLost(ClashResolveResult result)
        {
            isDeviceSlotSwapAllowed = false;
            ResetShopDeviceInteractionState();
            HideAllDeviceHoverUIs();

            int playerHP = GetCurrentPlayerHPFromPresenter();
            TesseraEventBus.Publish(new GameplayRoundLostEvent(result, playerHP));
        }

        #region Event

        /// <summary>Player DeviceSlot Hover 진입을 Tooltip 또는 Shop Action UI 표시로 변환한다.</summary>
        private void HandleShopDeviceSlotHoverEntered(int slotIndex)
        {
            // Tooltip은 항상 허용하고, Sell 버튼만 Shop 상태에서 허용한다.
            LogShopDeviceInput($"[HoverEntered] Slot={slotIndex}, IsShopVisible={isShopVisible}, IsDragging={isShopDeviceDragging}");

            if (isShopDeviceDragging) return;
            if (playerDeviceRackForShop == null) return;

            SlotPairDeviceDefinitionSO device = playerDeviceRackForShop.GetDevice(slotIndex);

            if (device == null)
            {
                HideAllDeviceHoverUIs();
                LogShopDeviceInput($"[HoverIgnored] Reason=EmptySlot, Slot={slotIndex}");
                return;
            }

            hoveredDeviceSlotIndex = slotIndex;
            ShowDeviceHoverUI(slotIndex, device);
        }

        /// /// <summary>Player DeviceSlot Hover 이탈을 Tooltip 또는 Shop Action UI 숨김 후보로 처리한다.</summary>
        private void HandleShopDeviceSlotHoverExited(int slotIndex)
        {
            // 버튼으로 이동하는 중일 수 있으므로 한 프레임 늦게 Hide 여부를 판단한다.
            LogShopDeviceInput($"[HoverExited] Slot={slotIndex}, Visible={visibleDeviceHoverUIIndex}");

            if (hoveredDeviceSlotIndex == slotIndex)
                hoveredDeviceSlotIndex = -1;

            RequestHideDeviceHoverUI(slotIndex);
        }

        /// <summary>Player DeviceSlot Drop 대상을 Swap 요청으로 변환한다.</summary>
        private void HandleShopDeviceSlotDropped(int targetSlotIndex)
        {
            LogShopDeviceInput(
                $"[Drop] Target={targetSlotIndex}, Source={draggingShopDeviceSlotIndex}, " +
                $"IsDragging={isShopDeviceDragging}, CanCommit={CanCommitDeviceSlotSwap()}");

            if (!isShopDeviceDragging || draggingShopDeviceSlotIndex < 0)
            {
                LogShopDeviceInput($"[DropBlocked] Reason=NoDragSource, Target={targetSlotIndex}");
                return;
            }

            wasDeviceDropHandled = true;

            if (!CanCommitDeviceSlotSwap())
            {
                shouldShakeOnDeviceDragEnd = true;
                LogShopDeviceInput($"[DropBlocked] Reason=TimingBlocked, Target={targetSlotIndex}");
                return;
            }

            int sourceSlotIndex = draggingShopDeviceSlotIndex;

            if (sourceSlotIndex == targetSlotIndex)
            {
                LogShopDeviceInput($"[DropIgnored] Reason=SameSlot, Slot={targetSlotIndex}");
                return;
            }

            HideAllDeviceHoverUIs();

            wasDeviceDropAccepted = true;

            if (playerDeviceRackForShop != null)
            {
                playerDeviceRackForShop.CompleteDeviceDragVisualSwap(
                    sourceSlotIndex,
                    targetSlotIndex,
                    draggingDeviceViewObject,
                    draggingDeviceDefinition);
            }

            draggingDeviceViewObject = null;
            draggingDeviceDefinition = null;

            LogShopDeviceInput($"[SwapRequest] Source={sourceSlotIndex}, Target={targetSlotIndex}");

            if (isShopVisible)
            {
                TesseraEventBus.Publish(new StageShopEquippedDeviceSwapRequestedEvent(sourceSlotIndex, targetSlotIndex));
                return;
            }

            if (gameplayPresenter != null)
                gameplayPresenter.RequestDeviceSlotSwap(sourceSlotIndex, targetSlotIndex);
        }

        /// <summary>Player DeviceSlot 드래그 시작 시 실물 Device View를 분리한다.</summary>
        private void HandleDeviceSlotDragStartedWithPointer(int slotIndex, PointerEventData eventData)
        {
            LogShopDeviceInput($"[DragStarted] Slot={slotIndex}, CanSwap={CanStartDeviceSlotSwap()}");

            ResetDeviceDragVisualState();

            if (!CanStartDeviceSlotSwap())
                return;

            if (playerDeviceRackForShop == null)
                return;

            SlotPairDeviceDefinitionSO device = playerDeviceRackForShop.GetDevice(slotIndex);

            if (device == null)
            {
                LogShopDeviceInput($"[DragStartIgnored] Reason=EmptySource, Slot={slotIndex}");
                return;
            }

            bool detached = playerDeviceRackForShop.TryBeginDeviceDragVisual(
                slotIndex,
                out draggingDeviceViewObject,
                out draggingDeviceDefinition);

            if (!detached)
                return;

            isShopDeviceDragging = true;
            draggingShopDeviceSlotIndex = slotIndex;
            wasDeviceDropHandled = false;
            wasDeviceDropAccepted = false;
            shouldShakeOnDeviceDragEnd = false;

            if (draggingDeviceViewObject != null)
                lastDraggingDeviceWorldPosition = draggingDeviceViewObject.transform.position;

            HideAllDeviceHoverUIs();

            playerDeviceRackForShop.TryUpdateDeviceDragVisual(
                draggingDeviceViewObject,
                eventData,
                ref lastDraggingDeviceWorldPosition,
                out int snapSlotIndex);

            playerDeviceRackForShop.UpdateSwapTargetPreview(draggingShopDeviceSlotIndex, snapSlotIndex);

            LogShopDeviceInput($"[DragStartedAccepted] Source={draggingShopDeviceSlotIndex}");
        }

        /// <summary>Player DeviceSlot 드래그 중 Device View를 마우스 또는 슬롯 Anchor로 이동한다.</summary>
        private void HandleDeviceSlotDraggedWithPointer(int slotIndex, PointerEventData eventData)
        {
            if (!isShopDeviceDragging)
                return;

            if (playerDeviceRackForShop == null)
                return;

            playerDeviceRackForShop.TryUpdateDeviceDragVisual(
                draggingDeviceViewObject,
                eventData,
                ref lastDraggingDeviceWorldPosition,
                out int snapSlotIndex);

            playerDeviceRackForShop.UpdateSwapTargetPreview(draggingShopDeviceSlotIndex, snapSlotIndex);
        }

        /// <summary>Player DeviceSlot 드래그 종료 시 유효 Drop 여부에 따라 확정 또는 원위치 복귀한다.</summary>
        private void HandleDeviceSlotDragEndedWithPointer(int slotIndex, PointerEventData eventData)
        {
            LogShopDeviceInput(
                $"[DragEnded] Slot={slotIndex}, Source={draggingShopDeviceSlotIndex}, " +
                $"DropHandled={wasDeviceDropHandled}, DropAccepted={wasDeviceDropAccepted}");

            if (!isShopDeviceDragging)
            {
                ResetDeviceDragVisualState();
                return;
            }

            if (!wasDeviceDropHandled && playerDeviceRackForShop != null)
            {
                playerDeviceRackForShop.TryUpdateDeviceDragVisual(
                    draggingDeviceViewObject,
                    eventData,
                    ref lastDraggingDeviceWorldPosition,
                    out int snapSlotIndex);

                playerDeviceRackForShop.UpdateSwapTargetPreview(draggingShopDeviceSlotIndex, snapSlotIndex);
            }

            if (!wasDeviceDropHandled)
                TryCommitDeviceSlotDropFromPointer(eventData);

            if (wasDeviceDropAccepted)
            {
                ResetDeviceDragVisualState();
                RefreshDeviceHoverUIFromCurrentPointerAfterFrameAsync().Forget();
                return;
            }

            bool playShake = shouldShakeOnDeviceDragEnd || !wasDeviceDropHandled;

            if (playerDeviceRackForShop != null)
                playerDeviceRackForShop.ClearAllSwapTargetPreviews();

            RestoreRejectedDeviceDragAsync(playShake).Forget();
        }

        /// <summary>Collider Drop이 발생하지 않은 경우 Pointer 위치 기준으로 DeviceSlot Drop을 보정 처리한다.</summary>
        private void TryCommitDeviceSlotDropFromPointer(PointerEventData eventData)
        {
            if (wasDeviceDropHandled)
                return;

            if (eventData == null)
                return;

            if (playerDeviceRackForShop == null)
                return;

            Camera cameraToUse = eventData.pressEventCamera != null
                ? eventData.pressEventCamera
                : Camera.main;

            if (!playerDeviceRackForShop.TryFindSlotIndexUnderScreenPoint(eventData.position, cameraToUse, out int targetSlotIndex))
            {
                LogShopDeviceInput(
                    $"[DropFallbackMiss] Position={eventData.position}, Source={draggingShopDeviceSlotIndex}");
                return;
            }

            LogShopDeviceInput(
                $"[DropFallbackHit] Target={targetSlotIndex}, Source={draggingShopDeviceSlotIndex}, Position={eventData.position}");

            HandleShopDeviceSlotDropped(targetSlotIndex);
        }

        #endregion

        #region Helper

        /// <summary>슬롯별 Hover UI 이벤트를 구독한다.</summary>
        private void SubscribeDeviceSlotHoverActionUIs()
        {
            // 배열 순서를 슬롯 인덱스로 사용하고 각 UI의 Sell/Exit 이벤트를 Bridge로 연결한다.
            if (playerDeviceSlotHoverActionUI == null)
                return;

            for (int i = 0; i < playerDeviceSlotHoverActionUI.Length; i++)
            {
                DeviceSlotHoverActionUIView hoverUI = playerDeviceSlotHoverActionUI[i];

                if (hoverUI == null)
                    continue;

                hoverUI.Initialize(i);

                hoverUI.SellRequested -= HandleDeviceHoverUISellRequested;
                hoverUI.SellRequested += HandleDeviceHoverUISellRequested;

                hoverUI.HoverAreaExited -= HandleDeviceHoverUIAreaExited;
                hoverUI.HoverAreaExited += HandleDeviceHoverUIAreaExited;
            }
        }

        /// <summary>슬롯별 Hover UI 이벤트 구독을 해제한다.</summary>
        private void UnsubscribeDeviceSlotHoverActionUIs()
        {
            // Bridge 비활성화 시 슬롯별 UI 이벤트 참조를 정리한다.
            if (playerDeviceSlotHoverActionUI == null)
                return;

            for (int i = 0; i < playerDeviceSlotHoverActionUI.Length; i++)
            {
                DeviceSlotHoverActionUIView hoverUI = playerDeviceSlotHoverActionUI[i];

                if (hoverUI == null)
                    continue;

                hoverUI.SellRequested -= HandleDeviceHoverUISellRequested;
                hoverUI.HoverAreaExited -= HandleDeviceHoverUIAreaExited;
            }
        }

        /// <summary>슬롯별 Hover UI의 Sell 요청을 Runtime 판매 이벤트로 변환한다.</summary>
        private void HandleDeviceHoverUISellRequested(int slotIndex)
        {
            // Sell 실행 후에는 장착 상태가 바뀌므로 Hover UI를 모두 닫는다.
            LogShopDeviceInput($"[SellRequested] Slot={slotIndex}, IsShopVisible={isShopVisible}");

            if (!isShopVisible)
                return;

            HideAllDeviceHoverUIs();
            ResetShopDeviceInteractionState();

            // 실제 판매와 Money 증가는 Runtime Controller가 처리한다.
            TesseraEventBus.Publish(new StageShopEquippedDeviceSellRequestedEvent(slotIndex));
        }

        /// <summary>슬롯별 Hover UI 확장 영역 이탈을 숨김 후보로 처리한다.</summary>
        private void HandleDeviceHoverUIAreaExited(int slotIndex)
        {
            // 버튼 영역에서 나간 직후 슬롯 위에 다시 들어갈 수 있으므로 지연 Hide를 요청한다.
            RequestHideDeviceHoverUI(slotIndex);
        }

        /// <summary>지정 슬롯의 고정 배치 Hover UI를 현재 상태에 맞게 표시한다.</summary>
        private void ShowDeviceHoverUI(int slotIndex, SlotPairDeviceDefinitionSO device)
        {
            // 새 Hover 표시가 들어오면 이전 지연 Hide 요청을 무효화한다.
            hoverHideRequestSerial++;

            if (visibleDeviceHoverUIIndex >= 0 && visibleDeviceHoverUIIndex != slotIndex)
                HideCurrentDeviceHoverUI();

            DeviceSlotHoverActionUIView hoverUI = GetDeviceHoverUI(slotIndex);

            if (hoverUI == null)
            {
                LogShopDeviceInputWarning($"[HoverUIBlocked] Reason=MissingHoverUI, Slot={slotIndex}");
                return;
            }

            visibleDeviceHoverUIIndex = slotIndex;

            if (isShopVisible)
            {
                int refundMoney = ResolveDeviceSellRefundMoneyForPreview(device);
                hoverUI.ShowSell(device, refundMoney);

                LogShopDeviceInput($"[HoverUIShown] Mode=Sell, Slot={slotIndex}, Device={device.DisplayName}, Refund={refundMoney}");
                return;
            }

            hoverUI.ShowTooltip(device);

            LogShopDeviceInput($"[HoverUIShown] Mode=TooltipOnly, Slot={slotIndex}, Device={device.DisplayName}");
        }

        /// <summary>드래그/스왑 처리 이후 현재 포인터 Hover 슬롯 기준으로 Hover UI를 다시 표시한다.</summary>
        private async UniTaskVoid RefreshDeviceHoverUIFromCurrentPointerAfterFrameAsync()
        {
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

            RefreshDeviceHoverUIFromCurrentPointer();
        }

        /// <summary>현재 포인터 Hover 슬롯의 최신 Device 정보를 조회해 Tooltip 또는 Sell UI를 표시한다.</summary>
        private void RefreshDeviceHoverUIFromCurrentPointer()
        {
            if (isShopDeviceDragging)
                return;

            if (playerDeviceRackForShop == null)
                return;

            if (!playerDeviceRackForShop.TryGetPointerHoveredSlotIndex(out int slotIndex))
                return;

            SlotPairDeviceDefinitionSO device = playerDeviceRackForShop.GetDevice(slotIndex);

            if (device == null)
            {
                HideAllDeviceHoverUIs();
                LogShopDeviceInput($"[HoverUIRefreshIgnored] Reason=EmptySlot, Slot={slotIndex}");
                return;
            }

            hoveredDeviceSlotIndex = slotIndex;
            ShowDeviceHoverUI(slotIndex, device);

            LogShopDeviceInput($"[HoverUIRefresh] Slot={slotIndex}, Device={device.DisplayName}");
        }

        /// <summary>지정 슬롯 Hover UI 숨김 판단을 한 프레임 지연 요청한다.</summary>
        private void RequestHideDeviceHoverUI(int slotIndex)
        {
            // Physics Hover Exit과 UI Pointer Enter의 이벤트 순서 차이를 흡수한다.
            int requestSerial = ++hoverHideRequestSerial;
            TryHideDeviceHoverUIAfterFrameAsync(slotIndex, requestSerial).Forget();
        }

        /// <summary>한 프레임 뒤 현재 Hover 상태를 다시 확인하고 Hover UI를 숨긴다.</summary>
        private async UniTaskVoid TryHideDeviceHoverUIAfterFrameAsync(int slotIndex, int requestSerial)
        {
            // ActionButton으로 포인터가 이동하는 시간을 주기 위해 프레임 끝까지 기다린다.
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

            if (requestSerial != hoverHideRequestSerial)
                return;

            TryHideDeviceHoverUI(slotIndex);
        }

        /// <summary>슬롯과 ActionButton Hover 상태를 보고 Hover UI 숨김 여부를 결정한다.</summary>
        private void TryHideDeviceHoverUI(int slotIndex)
        {
            // 현재 표시 중인 슬롯과 다른 슬롯의 이탈 이벤트는 무시한다.
            if (visibleDeviceHoverUIIndex != slotIndex)
                return;

            DeviceSlotHoverActionUIView hoverUI = GetDeviceHoverUI(slotIndex);

            if (hoverUI == null)
            {
                HideCurrentDeviceHoverUI();
                return;
            }

            if (hoveredDeviceSlotIndex == slotIndex)
                return;

            if (hoverUI.IsPointerOverActionArea)
                return;

            HideCurrentDeviceHoverUI();
        }

        /// <summary>현재 표시 중인 Hover UI를 숨긴다.</summary>
        private void HideCurrentDeviceHoverUI()
        {
            // 현재 활성 슬롯 UI만 닫아 다른 슬롯 Anchor 상태를 건드리지 않는다.
            if (visibleDeviceHoverUIIndex >= 0)
            {
                DeviceSlotHoverActionUIView hoverUI = GetDeviceHoverUI(visibleDeviceHoverUIIndex);

                if (hoverUI != null)
                    hoverUI.Hide();
            }

            visibleDeviceHoverUIIndex = -1;
        }

        /// <summary>모든 슬롯별 Hover UI를 숨기고 Hover 상태를 초기화한다.</summary>
        private void HideAllDeviceHoverUIs()
        {
            // 화면 전환/드래그/판매 이후에는 stale target 방지를 위해 전체를 닫는다.
            hoverHideRequestSerial++;
            hoveredDeviceSlotIndex = -1;
            visibleDeviceHoverUIIndex = -1;

            if (playerDeviceSlotHoverActionUI == null)
                return;

            for (int i = 0; i < playerDeviceSlotHoverActionUI.Length; i++)
            {
                if (playerDeviceSlotHoverActionUI[i] == null)
                    continue;

                playerDeviceSlotHoverActionUI[i].Hide();
            }
        }

        /// <summary>지정 슬롯 인덱스에 해당하는 Hover UI를 반환한다.</summary>
        private DeviceSlotHoverActionUIView GetDeviceHoverUI(int slotIndex)
        {
            // 배열 범위를 벗어난 슬롯 요청은 안전하게 무시한다.
            if (playerDeviceSlotHoverActionUI == null)
                return null;

            if (slotIndex < 0 || slotIndex >= playerDeviceSlotHoverActionUI.Length)
                return null;

            return playerDeviceSlotHoverActionUI[slotIndex];
        }

        /// <summary>장착 Device 판매 미리보기용 환불 금액을 계산한다.</summary>
        private int ResolveDeviceSellRefundMoneyForPreview(SlotPairDeviceDefinitionSO device)
        {
            if (device == null)
                return 0;

            // 실제 환불 금액은 Runtime Controller가 최종 결정한다.
            // 현재 UI 미리보기는 프로토타입 기본값을 사용한다.
            return Mathf.Max(1, defaultDeviceSellRefundMoney);
        }

        /// <summary>Shop DeviceSlot 드래그/선택 상태를 초기화한다.</summary>
        private void ResetShopDeviceInteractionState()
        {
            ResetDeviceDragVisualState();
        }

        /// <summary>Gameplay Presenter의 현재 RoundState에서 플레이어 HP를 읽는다.</summary>
        private int GetCurrentPlayerHPFromPresenter()
        {
            RoundState roundState = gameplayPresenter != null ? gameplayPresenter.CurrentRoundState : null;

            if (roundState == null || roundState.Encounter == null)
                return 1;

            return roundState.Encounter.PlayerCurrentHP;
        }

        /// <summary>현재 DeviceSlot swap을 시작할 수 있는지 확인한다.</summary>
        private bool CanStartDeviceSlotSwap()
        {
            if (playerDeviceRackForShop == null)
                return false;

            if (!isDeviceSlotSwapAllowed)
                return false;

            if (isShopVisible)
                return true;

            if (gameplayPresenter == null)
                return false;

            return gameplayPresenter.CanSwapPlayerDevicesNow();
        }

        /// <summary>현재 DeviceSlot swap을 Drop 확정할 수 있는지 확인한다.</summary>
        private bool CanCommitDeviceSlotSwap()
        {
            return CanStartDeviceSlotSwap();
        }

        /// <summary>무효 Drop된 Device View를 원래 슬롯으로 복귀시킨다.</summary>
        private async UniTaskVoid RestoreRejectedDeviceDragAsync(bool playShake)
        {
            if (playerDeviceRackForShop != null)
            {
                await playerDeviceRackForShop.RestoreDeviceDragVisualAsync(
                    draggingShopDeviceSlotIndex,
                    draggingDeviceViewObject,
                    draggingDeviceDefinition,
                    playShake);
            }

            ResetDeviceDragVisualState();
            RefreshDeviceHoverUIFromCurrentPointerAfterFrameAsync().Forget();
        }

        /// <summary>Device Drag View 상태를 초기화한다.</summary>
        private void ResetDeviceDragVisualState()
        {
            isShopDeviceDragging = false;
            draggingShopDeviceSlotIndex = -1;
            wasDeviceDropHandled = false;
            wasDeviceDropAccepted = false;
            shouldShakeOnDeviceDragEnd = false;
            draggingDeviceViewObject = null;
            draggingDeviceDefinition = null;
            lastDraggingDeviceWorldPosition = Vector3.zero;
        }

        #endregion

        #region Debug

        /// <summary>Shop Device 입력 디버그 로그를 출력한다.</summary>
        private void LogShopDeviceInput(string message)
        {
            if (!enableShopDeviceInputDebugLog)
                return;

            Debug.Log($"[Tessera][ShopDeviceInput]{message}");
        }

        /// <summary>Shop Device 입력 디버그 경고 로그를 출력한다.</summary>
        private void LogShopDeviceInputWarning(string message)
        {
            if (!enableShopDeviceInputDebugLog)
                return;

            Debug.LogWarning($"[Tessera][ShopDeviceInput]{message}");
        }

        #endregion
    }
}
