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

        [Header("Cash Out")]
        [SerializeField] private float cashOutHealRatio = 0.5f;

        private TesseraRunSession runSession;
        private StageBountyBoardState currentStageState;
        private int currentStageIndex;
        private bool isInitialized;

        private System.IDisposable bountySelectedSubscription;
        private System.IDisposable rewardDecisionSubscription;
        private System.IDisposable shopContinueSubscription;
        private System.IDisposable roundWonSubscription;
        private System.IDisposable roundLostSubscription;

        /// <summary>런 세션을 연결하고 Runtime 이벤트를 구독한다.</summary>
        public void Initialize(TesseraRunSession session)
        {
            if (isInitialized)
                return;

            runSession = session;

            bountySelectedSubscription = TesseraEventBus.Subscribe<BountyRoundSelectedEvent>(HandleBountySelected);
            rewardDecisionSubscription = TesseraEventBus.Subscribe<RewardDecisionRequestedEvent>(HandleRewardDecisionRequested);
            shopContinueSubscription = TesseraEventBus.Subscribe<StageShopContinueRequestedEvent>(HandleShopContinueRequested);
            roundWonSubscription = TesseraEventBus.Subscribe<GameplayRoundWonEvent>(HandleRoundWon);
            roundLostSubscription = TesseraEventBus.Subscribe<GameplayRoundLostEvent>(HandleRoundLost);

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

        private void OnDestroy()
        {
            DisposeSubscriptions();
        }

        private void DisposeSubscriptions()
        {
            bountySelectedSubscription?.Dispose();
            rewardDecisionSubscription?.Dispose();
            shopContinueSubscription?.Dispose();
            roundWonSubscription?.Dispose();
            roundLostSubscription?.Dispose();

            bountySelectedSubscription = null;
            rewardDecisionSubscription = null;
            shopContinueSubscription = null;
            roundWonSubscription = null;
            roundLostSubscription = null;
        }

        private void StartStage(int stageIndex)
        {
            StageDefinitionSO stageDefinition = stageDefinitions[Mathf.Clamp(stageIndex, 0, stageDefinitions.Length - 1)];

            if (stageDefinition == null || !stageDefinition.IsValidDefinition())
            {
                Debug.LogWarning("[Tessera][StageFlow] Invalid stage definition.");
                return;
            }

            currentStageState = new StageBountyBoardState(stageDefinition);
            runSession.SetCurrentStageIndex(stageIndex, resetStageChain: true);
            runSession.ResetOverchargeForStageStart();

            TesseraEventBus.Publish(new StageStartedEvent(stageDefinition.StageNumber, stageDefinition.DisplayName));

            StageBountyNodeState tutorialNode = currentStageState.FindTutorialForcedNode();

            if (tutorialNode != null)
            {
                StartBountyRound(tutorialNode);
                return;
            }

            ShowBountyBoard("Choose your first bounty.");
        }

        private void ShowBountyBoard(string message)
        {
            if (currentStageState == null)
                return;

            TesseraEventBus.Publish(new BountyBoardShowRequestedEvent(currentStageState, message));
            TesseraEventBus.Publish(new GameModeChangeRequestedEvent(GameModeType.BountyBoard, message));
        }

        private void ShowRewardDecision(string message)
        {
            if (currentStageState == null)
                return;

            TesseraEventBus.Publish(new RewardDecisionShowRequestedEvent(currentStageState, message));
            TesseraEventBus.Publish(new GameModeChangeRequestedEvent(GameModeType.RewardDecision, message));
        }

        private void ShowShop(StageShopReasonType reasonType, string message)
        {
            TesseraEventBus.Publish(new StageShopShowRequestedEvent(runSession, currentStageState, reasonType, message));
            TesseraEventBus.Publish(new StageShopEnterRequestedEvent(reasonType, message));
            TesseraEventBus.Publish(new GameModeChangeRequestedEvent(GameModeType.Shop, message));
        }

        private void HandleBountySelected(BountyRoundSelectedEvent gameEvent)
        {
            StartBountyRound(gameEvent.NodeState);
        }

        private void StartBountyRound(StageBountyNodeState node)
        {
            if (currentStageState == null || node == null)
                return;

            if (!currentStageState.TrySelectNode(node))
            {
                Debug.LogWarning("[Tessera][StageFlow] Cannot select bounty node.");
                return;
            }

            RoundRuleContext ruleContext = node.Definition.BuildRuleContext(runSession.PlayerMaxHp);

            TesseraEventBus.Publish(
                new StageRoundStartRequestedEvent(
                    ruleContext,
                    Mathf.Max(1, runSession.PlayerCurrentHp),
                    runSession.StageOverchargeState,
                    node.Definition.DisplayName));

            TesseraEventBus.Publish(new GameModeChangeRequestedEvent(GameModeType.Gameplay, node.Definition.DisplayName));
        }

        private void HandleRoundWon(GameplayRoundWonEvent gameEvent)
        {
            if (runSession == null || currentStageState == null)
                return;

            runSession.SetPlayerCurrentHp(gameEvent.PlayerHpAfterRound);
            currentStageState.CompleteCurrentNode();

            if (currentStageState.IsStageCleared)
            {
                ApplyPendingRewardsToRunSession();
                ShowShop(StageShopReasonType.StageClear, "Stage cleared. Workshop opened automatically.");
                return;
            }

            ShowRewardDecision("Bounty cleared. Choose Cash Out or Chain Rush.");
        }

        private void HandleRoundLost(GameplayRoundLostEvent gameEvent)
        {
            if (runSession != null)
                runSession.SetPlayerCurrentHp(gameEvent.PlayerHpAfterRound);

            TesseraEventBus.Publish(new GameModeChangeRequestedEvent(GameModeType.Result, "Round lost."));
        }

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

        private void HandleCashOutRequested()
        {
            if (currentStageState == null || runSession == null)
                return;

            ApplyPendingRewardsToRunSession();

            int healed = runSession.HealByCashOutRatio(cashOutHealRatio);
            currentStageState.ApplyCashOutAndForceBoss();

            ShowShop(StageShopReasonType.CashOut, $"Cash Out. Recovered {healed} HP. Next fight is Boss.");
        }

        private void HandleChainRequested()
        {
            if (currentStageState == null || runSession == null)
                return;

            currentStageState.ApplyChainRush();
            runSession.AddChainAndPressure(1, 1);

            ShowBountyBoard("Chain Rush. Pressure increased.");
        }

        private void HandleBossRequested()
        {
            if (currentStageState == null)
                return;

            StageBountyNodeState bossNode = currentStageState.FindBossNode();

            if (bossNode == null)
                return;

            bossNode.SetAvailable(true);
            StartBountyRound(bossNode);
        }

        private void HandleShopContinueRequested(StageShopContinueRequestedEvent gameEvent)
        {
            if (currentStageState == null)
                return;

            if (currentStageState.IsStageCleared)
            {
                MoveToNextStage();
                return;
            }

            StageBountyNodeState bossNode = currentStageState.FindBossNode();

            if (bossNode != null && currentStageState.IsBossForcedAfterCashOut)
            {
                StartBountyRound(bossNode);
                return;
            }

            ShowBountyBoard("Return to Bounty Board.");
        }

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

        private void ApplyPendingRewardsToRunSession()
        {
            if (runSession == null || currentStageState == null)
                return;

            runSession.AddParts(currentStageState.DrainPendingPartsReward());
            runSession.AddOvercharge(currentStageState.DrainPendingOverchargeReward());
        }
    }
}
