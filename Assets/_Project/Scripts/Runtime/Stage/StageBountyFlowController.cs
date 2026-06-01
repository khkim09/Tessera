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

        private System.IDisposable bountySelectedSubscription;
        private System.IDisposable rewardDecisionSubscription;
        private System.IDisposable failureDecisionSubscription;
        private System.IDisposable shopContinueSubscription;
        private System.IDisposable shopRepairSubscription;
        private System.IDisposable shopUpgradeTierSubscription;
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

            runSession.SetCurrentStageIndex(stageIndex, resetStageChain: true);

            TesseraEventBus.Publish(new StageStartedEvent(stageDefinition.StageNumber, stageDefinition.DisplayName));

            PublishStageEconomyChanged("Stage started.");

            ShowBountyBoard("Choose a bounty.");

            // StageBountyNodeState tutorialNode = currentStageState.FindTutorialForcedNode();

            // if (tutorialNode != null)
            // {
            //     StartBountyRound(tutorialNode);
            //     return;
            // }

            // ShowBountyBoard("Choose your first bounty.");
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
                ResetWorkshopVisitLimits();

            TesseraEventBus.Publish(
                new StageShopShowRequestedEvent(
                    runSession,
                    currentStageState,
                    currentShopReasonType,
                    currentShopMessage));

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

            TesseraEventBus.Publish(
                new StageRoundStartRequestedEvent(
                    ruleContext,
                    Mathf.Max(1, runSession.PlayerCurrentHP),
                    runSession.StageOverchargeState,
                    node.Definition.DisplayName));

            TesseraEventBus.Publish(new GameModeChangeRequestedEvent(GameModeType.Gameplay, node.Definition.DisplayName));
            PublishStageEconomyChanged($"Bounty started: {node.Definition.DisplayName}");
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

            PublishOverchargeDisplayRefresh(
                $"Workshop Tier upgraded. Overcharge {runSession.StageOverchargeState.CurrentOvercharge}.");

            ShowShop(
                currentShopReasonType,
                $"Workshop Tier upgraded to {runSession.CurrentWorkshopTier}. Overcharge -{overchargeCost}. Upgrade {currentWorkshopTierUpgradeCount}/{maxUpgradeCount}.",
                false);
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
