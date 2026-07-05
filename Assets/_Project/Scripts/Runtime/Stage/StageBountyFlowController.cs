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

        [Header("Debug Cheat")]
        [SerializeField] private bool enableDebugShopCheat = true;
        [SerializeField] private KeyCode debugShopCheatKey = KeyCode.Alpha1;
        [SerializeField] private int debugShopCheatMoney = 99;
        [SerializeField] private int debugShopCheatOvercharge = 9;

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
        private System.IDisposable shopEquippedDeviceSellSubscription;
        private System.IDisposable shopEquippedDeviceSwapSubscription;

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
            shopEquippedDeviceSellSubscription = TesseraEventBus.Subscribe<StageShopEquippedDeviceSellRequestedEvent>(HandleShopEquippedDeviceSellRequested);
            shopEquippedDeviceSwapSubscription = TesseraEventBus.Subscribe<StageShopEquippedDeviceSwapRequestedEvent>(HandleShopEquippedDeviceSwapRequested);
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

        /// <summary>디버그용 Workshop 즉시 진입 치트키를 처리한다.</summary>
        private void Update()
        {
            HandleDebugShopCheatInput();
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
            shopEquippedDeviceSellSubscription?.Dispose();
            shopEquippedDeviceSwapSubscription?.Dispose();
            roundWonSubscription?.Dispose();
            roundLostSubscription?.Dispose();
            economyRefreshSubscription?.Dispose();

            bountySelectedSubscription = null;
            rewardDecisionSubscription = null;
            failureDecisionSubscription = null;
            shopContinueSubscription = null;
            shopRepairSubscription = null;
            shopUpgradeTierSubscription = null;
            shopProductBuyConfirmedSubscription = null;
            shopEquippedDeviceSellSubscription = null;
            shopEquippedDeviceSwapSubscription = null;
            roundWonSubscription = null;
            roundLostSubscription = null;
            economyRefreshSubscription = null;
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

            TesseraEventBus.Publish(new GameModeChangeRequestedEvent(GameModeType.Shop, currentShopMessage));
            TesseraEventBus.Publish(new StageShopEnterRequestedEvent(currentShopReasonType, currentShopMessage));

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

            PublishStageEconomyChanged(message);
        }

        /// <summary>디버그 치트키 입력 시 Money/Overcharge를 충분히 지급하고 즉시 Workshop으로 진입한다.</summary>
        private void HandleDebugShopCheatInput()
        {
            if (!enableDebugShopCheat)
                return;

            if (!Application.isEditor && !Debug.isDebugBuild)
                return;

            if (!Input.GetKeyDown(debugShopCheatKey))
                return;

            if (runSession == null)
            {
                Debug.LogWarning("[Tessera][DebugShopCheat] RunSession is null.");
                return;
            }

            if (!EnsureStageStateForDebugShop())
                return;

            int money = Mathf.Max(0, debugShopCheatMoney);
            int overcharge = Mathf.Max(0, debugShopCheatOvercharge);

            if (money > 0)
                runSession.AddMoney(money);

            if (overcharge > 0)
                runSession.AddOvercharge(overcharge);

            runSession.UnlockIndividualDiceTypeUpgrade();

            string message = $"Debug Shop Cheat: Money +{money}, Overcharge +{overcharge}, Individual DiceType unlocked. Workshop opened.";
            Debug.Log($"[Tessera][DebugShopCheat] {message}");
            ShowShop(StageShopReasonType.Tutorial, message, true);
        }

        /// <summary>디버그 Shop 진입에 필요한 Stage 상태를 보장한다.</summary>
        private bool EnsureStageStateForDebugShop()
        {
            if (currentStageState != null)
                return true;

            if (stageDefinitions == null || stageDefinitions.Length == 0)
            {
                Debug.LogWarning("[Tessera][DebugShopCheat] Stage definitions are empty.");
                return false;
            }

            int stageIndex = runSession != null
                ? Mathf.Clamp(runSession.CurrentStageIndex, 0, stageDefinitions.Length - 1)
                : 0;
            StageDefinitionSO stageDefinition = stageDefinitions[stageIndex];

            if (stageDefinition == null || !stageDefinition.IsValidDefinition())
            {
                Debug.LogWarning("[Tessera][DebugShopCheat] Invalid stage definition.");
                return false;
            }

            currentStageIndex = stageIndex;
            currentStageState = new StageBountyBoardState(stageDefinition);
            currentShopInventorySlots.Clear();
            return true;
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

            EnemyIntent openingIntent = node.Definition.BuildOpeningEnemyIntent();

            TesseraEventBus.Publish(new GameModeChangeRequestedEvent(GameModeType.Gameplay, node.Definition.DisplayName));

            TesseraEventBus.Publish(
                new StageRoundStartRequestedEvent(
                    ruleContext,
                    Mathf.Max(1, runSession.PlayerCurrentHP),
                    runSession.StageOverchargeState,
                    node.Definition.DisplayName,
                    opponentDevices,
                    node.Definition,
                    openingIntent,
                    runSession.EquippedDiceTypes));

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
            currentStageState.CompleteCurrentNode(remainingAttempts, gameEvent.Result != null ? gameEvent.Result.MoneyOnRoundWinBonusFromDiceType : 0);
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

        /// <summary>Shop 상품 구매 확인을 처리한다. Device는 장착하고 Dice 계열은 현재 임시 구매 완료 처리한다.</summary>
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

            if (!CanPurchaseShopProductOnRuntime(slot, product, out string failureMessage))
            {
                ShowShop(currentShopReasonType, failureMessage, false);
                return;
            }

            if (!TryPayShopProductCost(slot, out failureMessage))
            {
                ShowShop(currentShopReasonType, failureMessage, false);
                return;
            }

            if (!TryApplyPurchasedShopProduct(product, out string applyMessage, out bool playerDevicesChanged, out bool playerDiceTypesChanged))
            {
                RefundShopProductCost(slot);
                ShowShop(currentShopReasonType, applyMessage, false);
                return;
            }

            slot.MarkSoldOut();

            string purchaseMessage = BuildShopProductPurchaseMessage(product, slot, applyMessage);

            Debug.Log(
                $"[Tessera][ShopProductPurchase] Slot={slot.SlotIndex}, Type={product.ProductType}, " +
                $"Product={product.DisplayName}, Money=-{slot.MoneyPrice}, Overcharge=-{slot.OverchargePrice}, Apply={applyMessage}");

            if (playerDevicesChanged)
            {
                TesseraEventBus.Publish(
                    new StageShopPlayerDevicesChangedEvent(
                        runSession,
                        purchaseMessage));
            }

            if (playerDiceTypesChanged)
            {
                TesseraEventBus.Publish(
                    new StageShopPlayerDiceTypesChangedEvent(
                        runSession,
                        purchaseMessage));
            }

            PublishOverchargeDisplayRefresh(string.Empty);
            ShowShop(currentShopReasonType, purchaseMessage, false);
        }

        /// <summary>Runtime 최종 구매 가능 여부를 검사한다.</summary>
        private bool CanPurchaseShopProductOnRuntime(
            ShopInventorySlot slot,
            ShopProductDefinitionSO product,
            out string failureMessage)
        {
            failureMessage = string.Empty;

            if (slot == null || product == null)
            {
                failureMessage = "Invalid shop product.";
                return false;
            }

            if (!product.IsPurchasableInCurrentBuild())
            {
                failureMessage = "This product type is not implemented yet.";
                return false;
            }

            if (product.ProductType == ShopProductType.Device && !runSession.HasEmptyDeviceSlot())
            {
                failureMessage = "Device slots are full. Sell an equipped Device before buying a new one.";
                return false;
            }

            if (runSession.Money < slot.MoneyPrice)
            {
                failureMessage = "Not enough Money.";
                return false;
            }

            if (runSession.Overcharge < slot.OverchargePrice)
            {
                failureMessage = "Not enough Overcharge.";
                return false;
            }

            return true;
        }

        /// <summary>Shop 상품 비용 지불을 시도한다.</summary>
        private bool TryPayShopProductCost(ShopInventorySlot slot, out string failureMessage)
        {
            failureMessage = string.Empty;

            if (slot == null)
            {
                failureMessage = "Invalid shop product.";
                return false;
            }

            if (!runSession.TrySpendMoney(slot.MoneyPrice))
            {
                failureMessage = "Not enough Money.";
                return false;
            }

            if (!runSession.TrySpendOvercharge(slot.OverchargePrice))
            {
                if (slot.MoneyPrice > 0)
                    runSession.AddMoney(slot.MoneyPrice);

                failureMessage = "Not enough Overcharge.";
                return false;
            }

            return true;
        }

        /// <summary>이미 지불된 Shop 상품 비용을 환불한다.</summary>
        private void RefundShopProductCost(ShopInventorySlot slot)
        {
            if (slot == null || runSession == null)
                return;

            if (slot.MoneyPrice > 0)
                runSession.AddMoney(slot.MoneyPrice);

            if (slot.OverchargePrice > 0)
                runSession.AddOvercharge(slot.OverchargePrice);
        }

        /// <summary>구매한 Shop 상품 효과를 현재 구현 범위 안에서 적용한다.</summary>
        private bool TryApplyPurchasedShopProduct(
            ShopProductDefinitionSO product,
            out string applyMessage,
            out bool playerDevicesChanged,
            out bool playerDiceTypesChanged)
        {
            applyMessage = string.Empty;
            playerDevicesChanged = false;
            playerDiceTypesChanged = false;

            if (product == null)
            {
                applyMessage = "Invalid shop product.";
                return false;
            }

            switch (product.ProductType)
            {
                case ShopProductType.Device:
                    return TryApplyPurchasedDeviceProduct(product, out applyMessage, out playerDevicesChanged);

                case ShopProductType.DiceSet:
                    return TryApplyPurchasedDiceSetProduct(product, out applyMessage, out playerDiceTypesChanged);

                case ShopProductType.SingleDice:
                case ShopProductType.DiceTypeUpgrade:
                    return TryApplyPurchasedIndividualDiceTypeProduct(product, out applyMessage, out playerDiceTypesChanged);

                case ShopProductType.DiceFaceUpgrade:
                    return TryApplyPurchasedDiceFaceUpgradeProduct(product, out applyMessage);

                default:
                    applyMessage = $"Product type {product.ProductType} purchase effect is not implemented.";
                    return false;
            }
        }

        /// <summary>구매한 Device 상품을 첫 빈 Device 슬롯에 장착한다.</summary>
        private bool TryApplyPurchasedDeviceProduct(
            ShopProductDefinitionSO product,
            out string applyMessage,
            out bool playerDevicesChanged)
        {
            applyMessage = string.Empty;
            playerDevicesChanged = false;

            if (!runSession.TryEquipDeviceToFirstEmptySlot(product.DeviceDefinition, out int equippedSlotIndex))
            {
                applyMessage = "Device slots are full. Sell an equipped Device before buying a new one.";
                return false;
            }

            playerDevicesChanged = true;
            applyMessage = $"Equipped to DeviceSlot {equippedSlotIndex + 1}.";
            return true;
        }

        /// <summary>구매한 DiceSet 상품을 5개 플레이어 주사위 전체에 적용한다.</summary>
        private bool TryApplyPurchasedDiceSetProduct(
            ShopProductDefinitionSO product,
            out string applyMessage,
            out bool playerDiceTypesChanged)
        {
            applyMessage = string.Empty;
            playerDiceTypesChanged = false;

            DiceTypeDefinitionSO diceType = product.DiceTypeDefinition;

            if (diceType == null)
            {
                applyMessage = "DiceType definition is missing.";
                return false;
            }

            if (!runSession.SetDiceSetType(diceType))
            {
                applyMessage = "Failed to apply DiceSet.";
                return false;
            }

            playerDiceTypesChanged = true;
            applyMessage = $"All player dice changed to {diceType.DisplayName}.";
            return true;
        }

        /// <summary>구매한 개별 DiceType 상품을 자동 선택된 첫 번째 대상 Dice 슬롯에 적용한다.</summary>
        private bool TryApplyPurchasedIndividualDiceTypeProduct(
            ShopProductDefinitionSO product,
            out string applyMessage,
            out bool playerDiceTypesChanged)
        {
            applyMessage = string.Empty;
            playerDiceTypesChanged = false;

            DiceTypeDefinitionSO diceType = product.DiceTypeDefinition;

            if (diceType == null)
            {
                applyMessage = "DiceType definition is missing.";
                return false;
            }

            if (!runSession.TryApplyPurchasedIndividualDiceType(diceType, out int appliedDiceIndex, out DiceTypeDefinitionSO previousDiceType))
            {
                applyMessage = "Failed to apply individual DiceType.";
                return false;
            }

            playerDiceTypesChanged = true;
            string previousName = previousDiceType != null ? previousDiceType.DisplayName : "None";
            applyMessage = $"Dice {appliedDiceIndex + 1} changed from {previousName} to {diceType.DisplayName}.";
            return true;
        }

        /// <summary>구매한 DiceFaceUpgrade 상품을 자동 선택된 첫 번째 대상 Dice/Face 슬롯에 장착한다.</summary>
        private bool TryApplyPurchasedDiceFaceUpgradeProduct(
            ShopProductDefinitionSO product,
            out string applyMessage)
        {
            applyMessage = string.Empty;

            DiceFaceUpgradeDefinitionSO upgradeDefinition = product.DiceFaceUpgradeDefinition;

            if (upgradeDefinition == null)
            {
                applyMessage = "DiceFaceUpgrade definition is missing.";
                return false;
            }

            if (!runSession.TryApplyPurchasedDiceFaceUpgrade(
                    upgradeDefinition,
                    out int appliedDiceIndex,
                    out int appliedFaceIndex,
                    out DiceFaceUpgradeDefinitionSO previousUpgrade))
            {
                applyMessage = "Failed to apply DiceFaceUpgrade.";
                return false;
            }

            string previousName = previousUpgrade != null ? previousUpgrade.DisplayName : "None";
            applyMessage = $"Dice {appliedDiceIndex + 1} Face {appliedFaceIndex + 1} changed from {previousName} to {upgradeDefinition.DisplayName}.";
            return true;
        }

        /// <summary>상품 타입이 Dice 관련 상품인지 확인한다.</summary>
        private static bool IsDiceProductType(ShopProductType productType)
        {
            return productType == ShopProductType.DiceSet
                    || productType == ShopProductType.SingleDice
                    || productType == ShopProductType.DiceTypeUpgrade
                    || productType == ShopProductType.DiceFaceUpgrade;
        }

        /// <summary>구매 완료 메시지를 생성한다.</summary>
        private static string BuildShopProductPurchaseMessage(
            ShopProductDefinitionSO product,
            ShopInventorySlot slot,
            string applyMessage)
        {
            string costMessage = BuildShopProductCostMessage(slot);
            string message = $"Purchased {product.DisplayName}. {costMessage}";

            if (!string.IsNullOrWhiteSpace(applyMessage))
                message += $" {applyMessage}";

            return message;
        }

        /// <summary>구매 비용 표시 문자열을 생성한다.</summary>
        private static string BuildShopProductCostMessage(ShopInventorySlot slot)
        {
            if (slot == null)
                return string.Empty;

            if (slot.MoneyPrice > 0 && slot.OverchargePrice > 0)
                return $"Money -{slot.MoneyPrice}, Overcharge -{slot.OverchargePrice}.";

            if (slot.MoneyPrice > 0)
                return $"Money -{slot.MoneyPrice}.";

            if (slot.OverchargePrice > 0)
                return $"Overcharge -{slot.OverchargePrice}.";

            return "No cost.";
        }

        /// <summary>Shop에서 장착된 Device 판매 요청을 처리한다.</summary>
        private void HandleShopEquippedDeviceSellRequested(StageShopEquippedDeviceSellRequestedEvent gameEvent)
        {
            if (runSession == null)
                return;

            SlotPairDeviceDefinitionSO device = runSession.GetEquippedDevice(gameEvent.SlotIndex);

            if (device == null)
            {
                ShowShop(currentShopReasonType, "No Device equipped in this slot.", false);
                return;
            }

            int refundMoney = ResolveDeviceRefundMoney(device);

            if (!runSession.ClearEquippedDeviceSlot(gameEvent.SlotIndex, out SlotPairDeviceDefinitionSO removedDevice))
            {
                ShowShop(currentShopReasonType, "Failed to sell Device.", false);
                return;
            }

            // 실제로 제거된 Device 기준으로 메시지를 구성한다.
            if (removedDevice != null)
                device = removedDevice;

            if (refundMoney > 0)
                runSession.AddMoney(refundMoney);

            string message = $"Sold {device.DisplayName}. Money +{refundMoney}.";

            TesseraEventBus.Publish(
                new StageShopPlayerDevicesChangedEvent(
                    runSession,
                    message));

            PublishStageEconomyChanged(string.Empty);
            ShowShop(currentShopReasonType, message, false);
        }

        /// <summary>Shop에서 장착된 Device 슬롯 Swap 요청을 처리한다.</summary>
        private void HandleShopEquippedDeviceSwapRequested(StageShopEquippedDeviceSwapRequestedEvent gameEvent)
        {
            if (runSession == null)
                return;

            int sourceSlotIndex = gameEvent.SourceSlotIndex;
            int targetSlotIndex = gameEvent.TargetSlotIndex;

            if (sourceSlotIndex == targetSlotIndex)
                return;

            if (!IsValidDeviceSlotIndex(sourceSlotIndex))
            {
                Debug.LogWarning($"[Tessera][ShopDeviceSwap] Invalid source slot. Source={sourceSlotIndex}");
                return;
            }

            if (!IsValidDeviceSlotIndex(targetSlotIndex))
            {
                Debug.LogWarning($"[Tessera][ShopDeviceSwap] Invalid target slot. Target={targetSlotIndex}");
                return;
            }

            SlotPairDeviceDefinitionSO sourceDevice = runSession.GetEquippedDevice(sourceSlotIndex);
            SlotPairDeviceDefinitionSO targetDevice = runSession.GetEquippedDevice(targetSlotIndex);

            if (sourceDevice == null)
            {
                Debug.LogWarning($"[Tessera][ShopDeviceSwap] Source slot is empty. Source={sourceSlotIndex}");
                return;
            }

            if (!runSession.SwapEquippedDevices(sourceSlotIndex, targetSlotIndex))
            {
                Debug.LogWarning($"[Tessera][ShopDeviceSwap] RunSession swap failed. Source={sourceSlotIndex}, Target={targetSlotIndex}");
                return;
            }

            string sourceName = sourceDevice != null ? sourceDevice.DisplayName : "Empty";
            string targetName = targetDevice != null ? targetDevice.DisplayName : "Empty";
            string message = $"Swapped {sourceName} and {targetName}.";

            TesseraEventBus.Publish(new StageShopPlayerDevicesChangedEvent(runSession, message));
            PublishStageEconomyChanged(string.Empty);
        }

        /// <summary>장착 Device의 판매 환불 Money를 계산한다.</summary>
        private int ResolveDeviceRefundMoney(SlotPairDeviceDefinitionSO device)
        {
            if (device == null)
                return 0;

            ShopProductDefinitionSO matchedProduct = FindShopProductByDevice(device);
            int basePrice = matchedProduct != null
                ? matchedProduct.BaseMoneyPrice
                : 2;

            // 현재 프로토타입 판매가는 구매가의 절반, 최소 1 Money로 처리한다.
            return Mathf.Max(1, Mathf.FloorToInt(basePrice * 0.5f));
        }

        /// <summary>현재 Workshop 슬롯 규칙에서 지정 Device를 판매하는 ShopProduct를 찾는다.</summary>
        private ShopProductDefinitionSO FindShopProductByDevice(SlotPairDeviceDefinitionSO device)
        {
            if (device == null)
                return null;

            StageWorkshopRulesSO rules = ResolveCurrentWorkshopRules();

            if (rules == null)
                return null;

            return FindShopProductByDeviceInSlotRules(rules, device);
        }

        /// <summary>SlotRules 기반 상품 풀에서 지정 Device 상품을 찾는다.</summary>
        private static ShopProductDefinitionSO FindShopProductByDeviceInSlotRules(
            StageWorkshopRulesSO rules,
            SlotPairDeviceDefinitionSO device)
        {
            if (rules.ProductSlotRules == null)
                return null;

            for (int i = 0; i < rules.ProductSlotRules.Count; i++)
            {
                ShopProductSlotRule slotRule = rules.ProductSlotRules[i];

                if (slotRule == null || slotRule.ProductPool == null)
                    continue;

                for (int j = 0; j < slotRule.ProductPool.Length; j++)
                {
                    ShopProductDefinitionSO product = slotRule.ProductPool[j];

                    if (IsMatchingDeviceProduct(product, device))
                        return product;
                }
            }

            return null;
        }

        /// <summary>지정 상품이 대상 Device와 연결된 Device 상품인지 확인한다.</summary>
        private static bool IsMatchingDeviceProduct(
            ShopProductDefinitionSO product,
            SlotPairDeviceDefinitionSO device)
        {
            if (product == null)
                return false;

            if (product.ProductType != ShopProductType.Device)
                return false;

            return product.DeviceDefinition == device;
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

            return Mathf.Max(
                0,
                baseRetryMoneyCost
                + currentStageState.ChainCount * 2
                + currentStageState.StageThreatLevel * 3
                + bountyRank * 2);
        }

        /// <summary>Round 승리 후 남은 Attempt 수를 계산한다.</summary>
        private static int ResolveRemainingAttempts(StageBountyNodeState node, ClashResolveResult result)
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

        /// <summary>DeviceSlot 인덱스가 RunSession 장착 슬롯 범위 안에 있는지 확인한다.</summary>
        private bool IsValidDeviceSlotIndex(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < TesseraRunSession.MaxDeviceSlots;
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
