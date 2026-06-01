using Tessera.Core;

namespace Tessera.Runtime
{
    /// <summary>GameMode 변경 요청 이벤트다.</summary>
    public readonly struct GameModeChangeRequestedEvent
    {
        public GameModeType RequestedMode { get; }
        public string Reason { get; }

        public GameModeChangeRequestedEvent(GameModeType requestedMode, string reason)
        {
            RequestedMode = requestedMode;
            Reason = reason ?? string.Empty;
        }
    }

    /// <summary>GameMode 변경 완료 이벤트다.</summary>
    public readonly struct GameModeChangedEvent
    {
        public GameModeType CurrentMode { get; }

        public GameModeChangedEvent(GameModeType currentMode)
        {
            CurrentMode = currentMode;
        }
    }

    /// <summary>Stage 시작 이벤트다.</summary>
    public readonly struct StageStartedEvent
    {
        public int StageNumber { get; }
        public string StageName { get; }

        public StageStartedEvent(int stageNumber, string stageName)
        {
            StageNumber = stageNumber;
            StageName = stageName ?? string.Empty;
        }
    }

    /// <summary>Stage Round 시작 요청 이벤트다. UI Bridge가 Gameplay Presenter에 전달한다.</summary>
    public readonly struct StageRoundStartRequestedEvent
    {
        public RoundRuleContext RuleContext { get; }
        public int PlayerHPAtStart { get; }
        public OverchargeState StageOverchargeState { get; }
        public string RoundDisplayName { get; }

        public StageRoundStartRequestedEvent(
            RoundRuleContext ruleContext,
            int playerHPAtStart,
            OverchargeState stageOverchargeState,
            string roundDisplayName)
        {
            RuleContext = ruleContext;
            PlayerHPAtStart = playerHPAtStart;
            StageOverchargeState = stageOverchargeState;
            RoundDisplayName = roundDisplayName ?? string.Empty;
        }
    }

    /// <summary>Bounty Board 표시 요청 이벤트다.</summary>
    public readonly struct BountyBoardShowRequestedEvent
    {
        public StageBountyBoardState BoardState { get; }
        public string Message { get; }

        public BountyBoardShowRequestedEvent(StageBountyBoardState boardState, string message)
        {
            BoardState = boardState;
            Message = message ?? string.Empty;
        }
    }

    /// <summary>보상 선택 화면 표시 요청 이벤트다.</summary>
    public readonly struct RewardDecisionShowRequestedEvent
    {
        public StageBountyBoardState BoardState { get; }
        public string Message { get; }

        public RewardDecisionShowRequestedEvent(StageBountyBoardState boardState, string message)
        {
            BoardState = boardState;
            Message = message ?? string.Empty;
        }
    }

    /// <summary>Round 패배 후 복구/포기 선택 화면 표시 요청 이벤트다.</summary>
    public readonly struct RoundFailureShowRequestedEvent
    {
        public TesseraRunSession RunSession { get; }
        public StageBountyBoardState BoardState { get; }
        public int RetryMoneyCost { get; }
        public int RetryPartsCost => RetryMoneyCost;
        public string Message { get; }

        public RoundFailureShowRequestedEvent(
            TesseraRunSession runSession,
            StageBountyBoardState boardState,
            int retryMoneyCost,
            string message)
        {
            RunSession = runSession;
            BoardState = boardState;
            RetryMoneyCost = retryMoneyCost;
            Message = message ?? string.Empty;
        }
    }

    /// <summary>Stage Shop Shell 표시 요청 이벤트다.</summary>
    public readonly struct StageShopShowRequestedEvent
    {
        public TesseraRunSession RunSession { get; }
        public StageBountyBoardState BoardState { get; }
        public StageShopReasonType ReasonType { get; }
        public string Message { get; }

        public StageShopShowRequestedEvent(
            TesseraRunSession runSession,
            StageBountyBoardState boardState,
            StageShopReasonType reasonType,
            string message)
        {
            RunSession = runSession;
            BoardState = boardState;
            ReasonType = reasonType;
            Message = message ?? string.Empty;
        }
    }

    /// <summary>Stage Economy 표시 갱신 요청 이벤트다.</summary>
    public readonly struct StageEconomyChangedEvent
    {
        public TesseraRunSession RunSession { get; }
        public StageBountyBoardState BoardState { get; }
        public string Reason { get; }

        public StageEconomyChangedEvent(
            TesseraRunSession runSession,
            StageBountyBoardState boardState,
            string reason)
        {
            RunSession = runSession;
            BoardState = boardState;
            Reason = reason ?? string.Empty;
        }
    }

    /// <summary>현재 Stage Economy 상태 재발행 요청 이벤트다.</summary>
    public readonly struct StageEconomyRefreshRequestedEvent
    {
    }

    /// <summary>Stage Shop 진입 요청 이벤트다.</summary>
    public readonly struct StageShopEnterRequestedEvent
    {
        public StageShopReasonType ReasonType { get; }
        public string Message { get; }

        public StageShopEnterRequestedEvent(StageShopReasonType reasonType, string message)
        {
            ReasonType = reasonType;
            Message = message ?? string.Empty;
        }
    }

    /// <summary>UI에서 수배지를 선택했을 때 Runtime으로 보내는 이벤트다.</summary>
    public readonly struct BountyRoundSelectedEvent
    {
        public StageBountyNodeState NodeState { get; }

        public BountyRoundSelectedEvent(StageBountyNodeState nodeState)
        {
            NodeState = nodeState;
        }
    }

    /// <summary>UI에서 보상 선택 버튼을 눌렀을 때 Runtime으로 보내는 이벤트다.</summary>
    public readonly struct RewardDecisionRequestedEvent
    {
        public StageRewardDecisionType DecisionType { get; }

        public RewardDecisionRequestedEvent(StageRewardDecisionType decisionType)
        {
            DecisionType = decisionType;
        }
    }

    /// <summary>UI에서 Round 패배 후 복구/포기 선택 버튼을 눌렀을 때 Runtime으로 보내는 이벤트다.</summary>
    public readonly struct RoundFailureDecisionRequestedEvent
    {
        public RoundFailureDecisionType DecisionType { get; }

        public RoundFailureDecisionRequestedEvent(RoundFailureDecisionType decisionType)
        {
            DecisionType = decisionType;
        }
    }

    /// <summary>UI에서 Shop Continue를 눌렀을 때 Runtime으로 보내는 이벤트다.</summary>
    public readonly struct StageShopContinueRequestedEvent
    {
    }

    /// <summary>UI에서 Workshop Repair를 눌렀을 때 Runtime으로 보내는 이벤트다.</summary>
    public readonly struct StageShopRepairRequestedEvent
    {
    }

    /// <summary>UI에서 Workshop Tier 업그레이드를 눌렀을 때 Runtime으로 보내는 이벤트다.</summary>
    public readonly struct StageShopUpgradeTierRequestedEvent
    {
    }

    /// <summary>Gameplay Presenter가 Round 승리를 Runtime으로 전달하는 이벤트다.</summary>
    public readonly struct GameplayRoundWonEvent
    {
        public CastSubmitResult Result { get; }
        public int PlayerHPAfterRound { get; }

        public GameplayRoundWonEvent(CastSubmitResult result, int playerHPAfterRound)
        {
            Result = result;
            PlayerHPAfterRound = playerHPAfterRound;
        }
    }

    /// <summary>Gameplay Presenter가 Round 패배를 Runtime으로 전달하는 이벤트다.</summary>
    public readonly struct GameplayRoundLostEvent
    {
        public CastSubmitResult Result { get; }
        public int PlayerHPAfterRound { get; }

        public GameplayRoundLostEvent(CastSubmitResult result, int playerHPAfterRound)
        {
            Result = result;
            PlayerHPAfterRound = playerHPAfterRound;
        }
    }

    /// <summary>RunSession의 플레이어 HP 표시 갱신 요청 이벤트다.</summary>
    public readonly struct PlayerHPDisplayRefreshRequestedEvent
    {
        public int CurrentHP { get; }
        public int MaxHP { get; }
        public string Reason { get; }

        public PlayerHPDisplayRefreshRequestedEvent(int currentHP, int maxHP, string reason)
        {
            CurrentHP = currentHP;
            MaxHP = maxHP;
            Reason = reason ?? string.Empty;
        }
    }

    /// <summary>RunSession의 Overcharge 표시 갱신 요청 이벤트다.</summary>
    public readonly struct OverchargeDisplayRefreshRequestedEvent
    {
        public int CurrentOvercharge { get; }
        public string Reason { get; }

        public OverchargeDisplayRefreshRequestedEvent(int currentOvercharge, string reason)
        {
            CurrentOvercharge = currentOvercharge;
            Reason = reason ?? string.Empty;
        }
    }

    /// <summary>Reward Decision 선택 타입이다.</summary>
    public enum StageRewardDecisionType
    {
        None = 0,
        CashOut = 1,
        ChainRush = 2,
        Boss = 3
    }

    /// <summary>Round 패배 후 복구/포기 선택 타입이다.</summary>
    public enum RoundFailureDecisionType
    {
        None = 0,
        Retry = 1,
        Retreat = 2,
        Abandon = 3
    }

    /// <summary>Shop 진입 이유를 정의한다.</summary>
    public enum StageShopReasonType
    {
        None = 0,
        CashOut = 1,
        StageClear = 2,
        Tutorial = 3,
        Retreat = 4
    }
}
