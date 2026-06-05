using System.Collections.Generic;
using Tessera.Core;
using Tessera.Data;
using UnityEngine;

namespace Tessera.Runtime
{
    /// <summary>Chain-Bounty Stage 진행 흐름을 UI에 의존하지 않고 관리한다.</summary>
    public class StageBountyFlowController : MonoBehaviour
    {
        [Header("Stage Definitions")]
        [SerializeField] private StageDefinitionSO[] stageDefinitions;

        [Header("Reward / Recovery")]
        [SerializeField] private float cashOutHealRatio = 0.3f;
        [SerializeField] private float stageClearHealRatio = 0.5f;
        [SerializeField] private float retreatMinimumHPRatio = 0.8f;
        [SerializeField] private int stageClearBonusMoney = 25;

        [Header("Round Failure")]
        [SerializeField] private int baseRetryMoneyCost = 5;
        [SerializeField] private float retreatPayoutPercent = 0.2f;
        [SerializeField] private int emergencyRetreatMoney = 3;

        [Header("Workshop Shell")]
        [SerializeField] private int repairCostMoney = 8;
        [SerializeField] private int repairHealAmount = 10;
        [SerializeField] private int shopTierUpgradeOverchargeCost = 1;
        [SerializeField] private int maxWorkshopTierUpgradePerVisit = 1;

        [Header("Fallback Workshop Rules")]
        [SerializeField] private StageWorkshopRulesSO fallbackWorkshopRules;

        private TesseraRunSession runSession;
        private StageBountyBoardState currentStageState;
        private StageShopReasonType currentShopReasonType;
        private string currentShopMessage;
        private int currentStageIndex;
        private bool isInitialized;
        private int currentWorkshopTierUpgradeCount;
        private readonly List<ShopInventorySlot> currentShopInventorySlots = new List<ShopInventorySlot>();

        private System.IDisposable bountySelectedSubscription;
        private System.IDisposable rewardDecisionSubscription;
        private System.IDisposable failureDecisionSubscription;
        private System.IDisposable shopContinueSubscription;
        private System.IDisposable shopRepairSubscription;
        private System.IDisposable shopUpgradeTierSubscription;
        private System.IDisposable shopProductBuyConfirmedSubscription;
        private System.IDisposable roundWonSubscription;
        private System.IDisposable roundLostSubscription;
        private System.IDisposable economyRefreshSubscription;

        /// <summary>런 세션을 연결하고 Runtime 이벤트를 구독한다.</summary>
        public void Initialize(TesseraRunSession session)
        {
            if (isInitialized)
                return;

            runSession = session;

            bountySelectedSubscription = TesseraEventBus.Subscribe<BountyRoundSelectedEvent>(HandleBountySelected);
            rewardDecisionSubscription = TesseraEventBus.Subscribe<RewardDecisionRequestedEvent>(HandleRewardDecisionRequested);
            failureDecisionSubscription = TesseraEventBus.Subscribe<RoundFailureDecisionRequestedEvent>(HandleFailureDecisionRequested);
            shopContinueSubscription = TesseraEventBus.Subscribe<StageShopContinueRequestedEvent>(HandleShopContinueRequested);
            shopRepairSubscription = TesseraEventBus.Subscribe<StageShopRepairRequestedEvent>(HandleShopRepairRequested);
            shopUpgradeTierSubscription = TesseraEventBus.Subscribe<StageShopUpgradeTierRequestedEvent>(HandleShopUpgradeTierRequested);
            shopProductBuyConfirmedSubscription = TesseraEventBus.Subscribe<StageShopProductBuyConfirmedEvent>(HandleShopProductBuyConfirmed);
            roundWonSubscription = TesseraEventBus.Subscribe<GameplayRoundWonEvent>(HandleRoundWon);
            roundLostSubscription = TesseraEventBus.Subscribe<GameplayRoundLostEvent>(HandleRoundLost);
            economyRefreshSubscription = TesseraEventBus.Subscribe<StageEconomyRefreshRequestedEvent>(HandleStageEconomyRefreshRequested);
            isInitialized = true;
        }

        /// <summary>Stage Flow를 시작한다.</summary>
        public void StartFlow()
        {
            if (runSession == null)
            {
                Debug.LogWarning("[Tessera][StageFlow] RunSession is null.");
                return;
            }

            if (stageDefinitions == null || stageDefinitions.Length == 0)
            {
                Debug.LogWarning("[Tessera][StageFlow] Stage definitions are empty.");
                return;
            }

            currentStageIndex = Mathf.Clamp(runSession.CurrentStageIndex, 0, stageDefinitions.Length - 1);
            StartStage(currentStageIndex);
        }

        /// <summary>오브젝트 파괴 시 이벤트 구독을 해제한다.</summary>
        private void OnDestroy()
        {
            DisposeSubscriptions();
        }

        /// <summary>Runtime 이벤트 구독을 해제한다.</summary>
        private void DisposeSubscriptions()
        {
            bountySelectedSubscription?.Dispose();
            rewardDecisionSubscription?.Dispose();
            failureDecisionSubscription?.Dispose();
            shopContinueSubscription?.Dispose();
            shopRepairSubscription?.Dispose();
            shopUpgradeTierSubscription?.Dispose();
            shopProductBuyConfirmedSubscription?.Dispose();
            roundWonSubscription?.Dispose();
            roundLostSubscription?.Dispose();
            economyRefreshSubscription?.Dispose();

            bountySelectedSubscription = null;
            rewardDecisionSubscription = null;
            failureDecisionSubscription = null;
            shopContinueSubscription = null;
            shopRepairSubscription = null;
            shopUpgradeTierSubscription = null;
            roundWonSubscription = null;
            roundLostSubscription = null;
            economyRefreshSubscription = null;
            shopProductBuyConfirmedSubscription = null;
        }

        /// <summary>지정 Stage를 시작한다.</summary>
        private void StartStage(int stageIndex)
        {
            StageDefinitionSO stageDefinition = stageDefinitions[Mathf.Clamp(stageIndex, 0, stageDefinitions.Length - 1)];

            if (stageDefinition == null || !stageDefinition.IsValidDefinition())
            {
                Debug.LogWarning("[Tessera][StageFlow] Invalid stage definition.");
                return;
            }

            currentStageState = new StageBountyBoardState(stageDefinition);
            currentShopReasonType = StageShopReasonType.None;
            currentShopMessage = string.Empty;
            currentShopInventorySlots.Clear();

            runSession.SetCurrentStageIndex(stageIndex, resetStageChain: true);

            TesseraEventBus.Publish(new StageStartedEvent(stageDefinition.StageNumber, stageDefinition.DisplayName));

            PublishStageEconomyChanged("Stage started.");

            ShowBountyBoard("Choose a bounty.");

            currentShopInventorySlots.Clear();
        }

        /// <summary>Bounty Board를 표시한다.</summary>
        private void ShowBountyBoard(string message)
        {
            if (currentStageState == null)
                return;

            TesseraEventBus.Publish(new BountyBoardShowRequestedEvent(currentStageState, message));
            TesseraEventBus.Publish(new GameModeChangeRequestedEvent(GameModeType.BountyBoard, message));

            PublishStageEconomyChanged(message);
        }

        /// <summary>Reward Decision 화면을 표시한다.</summary>
        private void ShowRewardDecision(string message)
        {
            if (currentStageState == null)
                return;

            TesseraEventBus.Publish(new RewardDecisionShowRequestedEvent(currentStageState, message));
            TesseraEventBus.Publish(new GameModeChangeRequestedEvent(GameModeType.RewardDecision, message));

            PublishStageEconomyChanged(message);
        }

        /// <summary>Round Failure Decision 화면을 표시한다.</summary>
        private void ShowRoundFailureDecision(string message)
        {
            if (currentStageState == null || runSession == null)
                return;

            int retryMoneyCost = CalculateRetryMoneyCost();

            TesseraEventBus.Publish(
                new RoundFailureShowRequestedEvent(
                    runSession,
                    currentStageState,
                    retryMoneyCost,
                    message));

            TesseraEventBus.Publish(new GameModeChangeRequestedEvent(GameModeType.RoundFailureDecision, message));
            PublishStageEconomyChanged(message);
        }

        /// <summary>Workshop Shell을 표시한다. resetVisitLimit=false 기본값으로 동작한다.</summary>
        private void ShowShop(StageShopReasonType reasonType, string message)
        {
            ShowShop(reasonType, message, false);
        }

        /// <summary>Workshop Shell을 표시한다. resetVisitLimit=true 시 방문 제한값을 초기화한다.</summary>
        private void ShowShop(StageShopReasonType reasonType, string message, bool resetVisitLimit)
        {
            currentShopReasonType = reasonType == StageShopReasonType.None
                ? currentShopReasonType
                : reasonType;

            currentShopMessage = message ?? string.Empty;

            if (resetVisitLimit)
            {
                ResetWorkshopVisitLimits();
                RegenerateShopInventory();
            }

            StageWorkshopRulesSO rules = ResolveCurrentWorkshopRules();

            TesseraEventBus.Publish(
                new StageShopShowRequestedEvent(
                    runSession,
                    currentStageState,
                    currentShopReasonType,
                    currentShopMessage,
                    currentShopInventorySlots,
                    rules));

            TesseraEventBus.Publish(
                new StageShopPlayerDevicesChangedEvent(
                    runSession,
                    "Shop view refreshed."));

            TesseraEventBus.Publish(new StageShopEnterRequestedEvent(currentShopReasonType, currentShopMessage));
            TesseraEventBus.Publish(new GameModeChangeRequestedEvent(GameModeType.Shop, currentShopMessage));
            PublishStageEconomyChanged(message);
        }

        /// <summary>Workshop 진입 시 1회성 제한값을 초기화한다.</summary>
        private void ResetWorkshopVisitLimits()
        {
            currentWorkshopTierUpgradeCount = 0;

            if (runSession == null)
                return;

            StageWorkshopRulesSO rules = ResolveCurrentWorkshopRules();

            if (rules != null)
                runSession.SetWorkshopTier(rules.BaseWorkshopTier);
            else
                runSession.ResetWorkshopTier();
        }

        /// <summary>수배지 선택 이벤트를 처리한다.</summary>
        private void HandleBountySelected(BountyRoundSelectedEvent gameEvent)
        {
            StartBountyRound(gameEvent.NodeState);
        }

        /// <summary>수배지 Round를 시작한다.</summary>
        private void StartBountyRound(StageBountyNodeState node)
        {
            if (currentStageState == null || node == null || runSession == null)
                return;

            if (!currentStageState.TrySelectNode(node))
            {
                Debug.LogWarning("[Tessera][StageFlow] Cannot select bounty node.");
                return;
            }

            RoundRuleContext ruleContext = node.Definition.BuildRuleContext(
                runSession.PlayerMaxHP,
                currentStageState.StageThreatLevel);

            int opponentDeviceSeed = CreateOpponentDeviceLoadoutSeed(node);
            SlotPairDeviceDefinitionSO[] opponentDevices =
                node.Definition.BuildOpponentSlotPairDeviceLoadout(opponentDeviceSeed);

            TesseraEventBus.Publish(
                new StageRoundStartRequestedEvent(
                    ruleContext,
                    Mathf.Max(1, runSession.PlayerCurrentHP),
                    runSession.StageOverchargeState,
                    node.Definition.DisplayName,
                    opponentDevices,
                    node.Definition.FirstTurnPolicy));

            TesseraEventBus.Publish(new GameModeChangeRequestedEvent(GameModeType.Gameplay, node.Definition.DisplayName));
            PublishStageEconomyChanged($"Bounty started: {node.Definition.DisplayName}");
        }

        /// <summary>상대 Device 로드아웃 생성을 위한 결정적 Seed를 만든다.</summary>
        private int CreateOpponentDeviceLoadoutSeed(StageBountyNodeState node)
        {
            int stageNumber = currentStageState != null && currentStageState.StageDefinition != null
                ? currentStageState.StageDefinition.StageNumber
                : 1;

            int bountyRank = node != null && node.Definition != null
                ? node.Definition.BountyRank
                : 1;

            int stageThreat = currentStageState != null
                ? currentStageState.StageThreatLevel
                : 0;

            int chainCount = currentStageState != null
                ? currentStageState.ChainCount
                : 0;

            return stageNumber * 100000
                    + bountyRank * 10000
                    + stageThreat * 1000
                    + chainCount * 100
                    + Mathf.Abs(Time.frameCount % 100);
        }

        /// <summary>Round 승리 이벤트를 처리한다.</summary>
        private void HandleRoundWon(GameplayRoundWonEvent gameEvent)
        {
            if (runSession == null || currentStageState == null || currentStageState.CurrentNode == null)
                return;

            StageBountyNodeState completedNode = currentStageState.CurrentNode;
            runSession.SetPlayerCurrentHP(gameEvent.PlayerHPAfterRound);

            int remainingAttempts = ResolveRemainingAttempts(completedNode, gameEvent.Result);
            currentStageState.CompleteCurrentNode(remainingAttempts);
            SyncRunSessionStageState();

            if (currentStageState.IsStageCleared)
            {
                int pendingMoney = currentStageState.DrainPendingMoneyReward();
                runSession.AddMoney(pendingMoney);
                runSession.AddMoney(stageClearBonusMoney);

                int healed = runSession.HealByRatio(stageClearHealRatio);

                PublishPlayerHPDisplayRefresh(
                    $"Stage cleared. HP {runSession.PlayerCurrentHP}/{runSession.PlayerMaxHP}.");

                currentStageState.ApplyStageClear();
                runSession.ResetStageChainAndStageThreat();

                ShowShop(
                    StageShopReasonType.StageClear,
                    $"Stage cleared. Money +{pendingMoney}, Stage Clear Bonus +{stageClearBonusMoney}, HP +{healed}.",
                    true);
                return;
            }

            ShowRewardDecision(
                $"Bounty cleared. Pending Money +{currentStageState.LastCompletedRewardMoney}. Choose Cash Out or Keep Fighting.");
        }

        /// <summary>Round 패배 이벤트를 처리한다.</summary>
        private void HandleRoundLost(GameplayRoundLostEvent gameEvent)
        {
            if (runSession != null)
                runSession.SetPlayerCurrentHP(gameEvent.PlayerHPAfterRound);

            ShowRoundFailureDecision("Round lost. Choose how to continue this run.");
        }

        /// <summary>현재 Economy 상태 재발행 요청을 처리한다.</summary>
        private void HandleStageEconomyRefreshRequested(StageEconomyRefreshRequestedEvent gameEvent)
        {
            PublishStageEconomyChanged("Economy refresh requested.");
        }

        /// <summary>Reward Decision 요청을 처리한다.</summary>
        private void HandleRewardDecisionRequested(RewardDecisionRequestedEvent gameEvent)
        {
            if (gameEvent.DecisionType == StageRewardDecisionType.CashOut)
            {
                HandleCashOutRequested();
                return;
            }

            if (gameEvent.DecisionType == StageRewardDecisionType.ChainRush)
            {
                HandleChainRequested();
                return;
            }

            if (gameEvent.DecisionType == StageRewardDecisionType.Boss)
                HandleBossRequested();
        }

        /// <summary>Failure Decision 요청을 처리한다.</summary>
        private void HandleFailureDecisionRequested(RoundFailureDecisionRequestedEvent gameEvent)
        {
            if (gameEvent.DecisionType == RoundFailureDecisionType.Retry)
            {
                HandleFailureRetryRequested();
                return;
            }

            if (gameEvent.DecisionType == RoundFailureDecisionType.Retreat)
            {
                HandleFailureRetreatRequested();
                return;
            }

            if (gameEvent.DecisionType == RoundFailureDecisionType.Abandon)
                HandleFailureAbandonRequested();
        }

        /// <summary>패배 후 Retry 요청을 처리한다.</summary>
        private void HandleFailureRetryRequested()
        {
            if (currentStageState == null || runSession == null)
                return;

            StageBountyNodeState retryNode = currentStageState.CurrentNode;

            if (retryNode == null)
            {
                TesseraEventBus.Publish(new GameModeChangeRequestedEvent(GameModeType.Result, "Retry failed. No active bounty."));
                return;
            }

            int cost = CalculateRetryMoneyCost();

            if (!runSession.TrySpendMoney(cost))
            {
                ShowRoundFailureDecision("Not enough Money to retry.");
                return;
            }

            runSession.RestorePlayerToFullHP();
            StartBountyRound(retryNode);
        }

        /// <summary>패배 후 Retreat 요청을 처리한다.</summary>
        private void HandleFailureRetreatRequested()
        {
            if (currentStageState == null || runSession == null)
                return;

            bool increaseStageThreat = true;
            int payout = currentStageState.ApplyFailureRetreat(
                retreatPayoutPercent,
                emergencyRetreatMoney,
                increaseStageThreat);

            runSession.AddMoney(payout);

            int healed = runSession.HealToMinimumRatio(retreatMinimumHPRatio);
            SyncRunSessionStageState();

            PublishPlayerHPDisplayRefresh(
                $"Retreat recovery applied. HP {runSession.PlayerCurrentHP}/{runSession.PlayerMaxHP}.");

            Debug.Log(
                $"[Tessera][StageFlow] Retreat applied. " +
                $"StageThreat={currentStageState.StageThreatLevel}, " +
                $"Chain={currentStageState.ChainCount}, " +
                $"Money+{payout}, HP+{healed}");

            ShowShop(
                StageShopReasonType.Retreat,
                $"Retreated from the bounty. Money +{payout}, HP adjusted +{healed}. StageThreat {currentStageState.StageThreatLevel}. Emergency Workshop opened.",
                true);
        }

        /// <summary>패배 후 Abandon 요청을 처리한다.</summary>
        private void HandleFailureAbandonRequested()
        {
            TesseraEventBus.Publish(new GameModeChangeRequestedEvent(GameModeType.Result, "Run abandoned after round loss."));
        }

        /// <summary>CashOut 요청을 처리한다.</summary>
        private void HandleCashOutRequested()
        {
            if (currentStageState == null || runSession == null)
                return;

            int pendingMoney = currentStageState.DrainPendingMoneyReward();
            runSession.AddMoney(pendingMoney);

            int healed = runSession.HealByRatio(cashOutHealRatio);
            currentStageState.ApplyCashOut();
            SyncRunSessionStageState();

            PublishPlayerHPDisplayRefresh(
                $"Cash Out complete. HP {runSession.PlayerCurrentHP}/{runSession.PlayerMaxHP}.");

            ShowShop(
                StageShopReasonType.CashOut,
                $"Cash Out complete. Money +{pendingMoney}, HP +{healed}. StageThreat {currentStageState.StageThreatLevel}. Workshop opened.",
                true);
        }

        /// <summary>Chain Rush 요청을 처리한다.</summary>
        private void HandleChainRequested()
        {
            if (currentStageState == null || runSession == null)
                return;

            currentStageState.ApplyChainRush();
            SyncRunSessionStageState();

            ShowBountyBoard("Chain Rush selected. Pending Money is preserved and StageThreat increased.");
        }

        /// <summary>Boss 수배지 직접 진입 요청을 처리한다.</summary>
        private void HandleBossRequested()
        {
            if (currentStageState == null)
                return;

            StageBountyNodeState bossNode = currentStageState.FindBossNode();

            if (bossNode == null)
            {
                ShowBountyBoard("Boss bounty does not exist in this stage.");
                return;
            }

            if (!bossNode.IsAvailable)
            {
                ShowRewardDecision("Boss is not available yet. Clear at least one bounty or resolve remaining bounties first.");
                return;
            }

            StartBountyRound(bossNode);
        }

        /// <summary>Workshop Continue 요청을 처리한다.</summary>
        private void HandleShopContinueRequested(StageShopContinueRequestedEvent gameEvent)
        {
            if (currentStageState == null)
                return;

            if (currentShopReasonType == StageShopReasonType.StageClear || currentStageState.IsStageCleared)
            {
                MoveToNextStage();
                return;
            }

            ShowBountyBoard("Return to Bounty Board.");
        }

        /// <summary>Workshop Repair 요청을 처리한다.</summary>
        private void HandleShopRepairRequested(StageShopRepairRequestedEvent gameEvent)
        {
            if (runSession == null)
                return;

            if (runSession.PlayerCurrentHP >= runSession.PlayerMaxHP)
            {
                ShowShop(currentShopReasonType, "HP is already full.", false);
                return;
            }

            if (!runSession.TrySpendMoney(repairCostMoney))
            {
                ShowShop(currentShopReasonType, "Not enough Money to repair.", false);
                return;
            }

            int healed = runSession.RepairPlayerHP(repairHealAmount);

            PublishPlayerHPDisplayRefresh(
                $"Repair complete. HP {runSession.PlayerCurrentHP}/{runSession.PlayerMaxHP}.");

            ShowShop(
                currentShopReasonType,
                $"Repair complete. Money -{repairCostMoney}, HP +{healed}. Current HP {runSession.PlayerCurrentHP}/{runSession.PlayerMaxHP}.",
                false);
        }

        /// <summary>Workshop Tier 업그레이드 요청을 처리한다.</summary>
        private void HandleShopUpgradeTierRequested(StageShopUpgradeTierRequestedEvent gameEvent)
        {
            if (runSession == null)
                return;

            int maxUpgradeCount = ResolveMaxWorkshopTierUpgradePerVisit();

            if (currentWorkshopTierUpgradeCount >= maxUpgradeCount)
            {
                ShowShop(
                    currentShopReasonType,
                    $"Workshop Tier upgrade limit reached for this visit. Limit {maxUpgradeCount}.",
                    false);
                return;
            }

            int overchargeCost = ResolveWorkshopTierUpgradeOverchargeCost();

            if (!runSession.TrySpendOvercharge(overchargeCost))
            {
                ShowShop(currentShopReasonType, "Not enough Overcharge to upgrade Workshop Tier.", false);
                return;
            }

            int tierIncrease = ResolveWorkshopTierIncreasePerUpgrade();
            runSession.SetWorkshopTier(runSession.CurrentWorkshopTier + tierIncrease);
            currentWorkshopTierUpgradeCount++;

            RegenerateShopInventory();

            PublishOverchargeDisplayRefresh(
                $"Workshop Tier upgraded. Overcharge {runSession.StageOverchargeState.CurrentOvercharge}.");

            ShowShop(
                currentShopReasonType,
                $"Workshop Tier upgraded to {runSession.CurrentWorkshopTier}. Overcharge -{overchargeCost}. Upgrade {currentWorkshopTierUpgradeCount}/{maxUpgradeCount}. Shop inventory refreshed.",
                false);
        }

        /// <summary>Shop 상품 구매 확인을 처리한다. Device 상품은 결제 후 첫 빈 Player DeviceSlot에 즉시 장착한다.</summary>
        private void HandleShopProductBuyConfirmed(StageShopProductBuyConfirmedEvent gameEvent)
        {
            if (runSession == null)
                return;

            ShopInventorySlot slot = FindShopInventorySlot(gameEvent.ProductSlotIndex);

            if (slot == null || slot.ProductDefinition == null)
            {
                ShowShop(currentShopReasonType, "Invalid shop product.", false);
                return;
            }

            if (slot.IsSoldOut)
            {
                ShowShop(currentShopReasonType, "This product is already sold out.", false);
                return;
            }

            ShopProductDefinitionSO product = slot.ProductDefinition;

            if (!product.IsPurchasableInCurrentBuild())
            {
                ShowShop(currentShopReasonType, "This product type is not implemented yet.", false);
                return;
            }

            if (product.ProductType != ShopProductType.Device || product.DeviceDefinition == null)
            {
                ShowShop(currentShopReasonType, "Only Device products can be purchased in this version.", false);
                return;
            }

            if (!runSession.HasEmptyDeviceSlot())
            {
                ShowShop(
                    currentShopReasonType,
                    "Device slots are full. Sell an equipped Device before buying a new one.",
                    false);
                return;
            }

            if (runSession.Money < slot.MoneyPrice)
            {
                ShowShop(currentShopReasonType, "Not enough Money.", false);
                return;
            }

            if (runSession.Overcharge < slot.OverchargePrice)
            {
                ShowShop(currentShopReasonType, "Not enough Overcharge.", false);
                return;
            }

            if (!runSession.TrySpendMoney(slot.MoneyPrice))
            {
                ShowShop(currentShopReasonType, "Not enough Money.", false);
                return;
            }

            if (!runSession.TrySpendOvercharge(slot.OverchargePrice))
            {
                runSession.AddMoney(slot.MoneyPrice);
                ShowShop(currentShopReasonType, "Not enough Overcharge.", false);
                return;
            }

            if (!runSession.TryEquipDeviceToFirstEmptySlot(product.DeviceDefinition, out int equippedSlotIndex))
            {
                runSession.AddMoney(slot.MoneyPrice);

                if (slot.OverchargePrice > 0)
                    runSession.AddOvercharge(slot.OverchargePrice);

                ShowShop(
                    currentShopReasonType,
                    "Device slots are full. Sell an equipped Device before buying a new one.",
                    false);
                return;
            }

            slot.MarkSoldOut();

            string purchaseMessage =
                $"Purchased {product.DisplayName}. Equipped to DeviceSlot {equippedSlotIndex + 1}.";

            TesseraEventBus.Publish(
                new StageShopPlayerDevicesChangedEvent(
                    runSession,
                    purchaseMessage));

            // Overcharge/HUD는 갱신만 수행한다. 구매 메시지는 StageShopPlayerDevicesChangedEvent 경로에서 1회만 표시한다.
            PublishOverchargeDisplayRefresh(string.Empty);
            PublishStageEconomyChanged(string.Empty);

            ShowShop(currentShopReasonType, purchaseMessage, false);
        }

        /// <summary>현재 Workshop Tier 기준으로 Shop 상품 목록을 재생성한다.</summary>
        private void RegenerateShopInventory()
        {
            currentShopInventorySlots.Clear();

            StageWorkshopRulesSO rules = ResolveCurrentWorkshopRules();

            if (rules == null || runSession == null)
                return;

            int seed = CreateShopInventorySeed();
            List<ShopInventorySlot> generatedSlots = ShopInventoryGenerator.Generate(
                rules,
                runSession.CurrentWorkshopTier,
                seed);

            for (int i = 0; i < generatedSlots.Count; i++)
                currentShopInventorySlots.Add(generatedSlots[i]);
        }

        /// <summary>Shop 상품 슬롯 인덱스로 현재 상품 슬롯을 찾는다.</summary>
        private ShopInventorySlot FindShopInventorySlot(int slotIndex)
        {
            for (int i = 0; i < currentShopInventorySlots.Count; i++)
            {
                ShopInventorySlot slot = currentShopInventorySlots[i];

                if (slot != null && slot.SlotIndex == slotIndex)
                    return slot;
            }

            return null;
        }

        /// <summary>Shop 상품 목록 생성을 위한 Seed를 만든다.</summary>
        private int CreateShopInventorySeed()
        {
            int stageNumber = currentStageState != null && currentStageState.StageDefinition != null
                ? currentStageState.StageDefinition.StageNumber
                : 1;

            int stageThreat = currentStageState != null
                ? currentStageState.StageThreatLevel
                : 0;

            int chainCount = currentStageState != null
                ? currentStageState.ChainCount
                : 0;

            int workshopTier = runSession != null
                ? runSession.CurrentWorkshopTier
                : 1;

            return stageNumber * 100000
                    + workshopTier * 10000
                    + stageThreat * 1000
                    + chainCount * 100
                    + Mathf.Abs(Time.frameCount % 100);
        }

        /// <summary>다음 Stage로 이동한다.</summary>
        private void MoveToNextStage()
        {
            currentStageIndex++;

            if (stageDefinitions == null || currentStageIndex >= stageDefinitions.Length)
            {
                TesseraEventBus.Publish(new GameModeChangeRequestedEvent(GameModeType.Result, "Run clear prototype."));
                return;
            }

            StartStage(currentStageIndex);
        }

        /// <summary>Retry 비용을 계산한다.</summary>
        private int CalculateRetryMoneyCost()
        {
            if (currentStageState == null)
                return Mathf.Max(0, baseRetryMoneyCost);

            StageBountyNodeState currentNode = currentStageState.CurrentNode;
            int bountyRank = currentNode != null && currentNode.Definition != null
                ? currentNode.Definition.BountyRank
                : 1;

            return Mathf.Max(0, baseRetryMoneyCost + currentStageState.ChainCount * 2 + bountyRank * 3);
        }

        /// <summary>Round 승리 후 남은 Attempt 수를 계산한다.</summary>
        private static int ResolveRemainingAttempts(StageBountyNodeState node, CastSubmitResult result)
        {
            if (node == null || node.Definition == null || result == null)
                return 0;

            return Mathf.Max(0, node.Definition.MaxAttempts - result.AttemptNumber);
        }

        /// <summary>BoardState의 Stage Chain/Threat를 RunSession에 동기화한다.</summary>
        private void SyncRunSessionStageState()
        {
            if (runSession == null || currentStageState == null)
                return;

            runSession.SetStageChainAndThreat(
                currentStageState.ChainCount,
                currentStageState.StageThreatLevel);
        }

        /// <summary>플레이어 HP 표시 갱신 이벤트를 발행한다.</summary>
        private void PublishPlayerHPDisplayRefresh(string reason)
        {
            if (runSession == null)
                return;

            TesseraEventBus.Publish(
                new PlayerHPDisplayRefreshRequestedEvent(
                    runSession.PlayerCurrentHP,
                    runSession.PlayerMaxHP,
                    reason));
        }

        /// <summary>Overcharge 표시 갱신 이벤트를 발행한다.</summary>
        private void PublishOverchargeDisplayRefresh(string reason)
        {
            if (runSession == null)
                return;

            TesseraEventBus.Publish(
                new OverchargeDisplayRefreshRequestedEvent(
                    runSession.StageOverchargeState.CurrentOvercharge,
                    reason));
        }

        /// <summary>현재 Stage Economy 상태 갱신 이벤트를 발행한다.</summary>
        private void PublishStageEconomyChanged(string reason)
        {
            if (runSession == null)
                return;

            TesseraEventBus.Publish(new StageEconomyChangedEvent(runSession, currentStageState, reason));
        }

        #region 상점 Rules

        /// <summary>현재 Stage에 적용할 Workshop 규칙을 반환한다.</summary>
        private StageWorkshopRulesSO ResolveCurrentWorkshopRules()
        {
            if (currentStageState != null &&
                currentStageState.StageDefinition != null &&
                currentStageState.StageDefinition.WorkshopRules != null)
                return currentStageState.StageDefinition.WorkshopRules;

            return fallbackWorkshopRules;
        }

        /// <summary>현재 Workshop 방문에서 허용되는 Tier 업그레이드 최대 횟수를 반환한다.</summary>
        private int ResolveMaxWorkshopTierUpgradePerVisit()
        {
            StageWorkshopRulesSO rules = ResolveCurrentWorkshopRules();

            if (rules != null)
                return rules.MaxTierUpgradePerVisit;

            return Mathf.Max(0, maxWorkshopTierUpgradePerVisit);
        }

        /// <summary>Workshop Tier 업그레이드 Overcharge 비용을 반환한다.</summary>
        private int ResolveWorkshopTierUpgradeOverchargeCost()
        {
            StageWorkshopRulesSO rules = ResolveCurrentWorkshopRules();

            if (rules != null)
                return rules.TierUpgradeOverchargeCost;

            return Mathf.Max(0, shopTierUpgradeOverchargeCost);
        }

        /// <summary>Workshop Tier 업그레이드 증가량을 반환한다.</summary>
        private int ResolveWorkshopTierIncreasePerUpgrade()
        {
            StageWorkshopRulesSO rules = ResolveCurrentWorkshopRules();

            if (rules != null)
                return rules.TierIncreasePerUpgrade;

            return 1;
        }

        #endregion
    }
}
