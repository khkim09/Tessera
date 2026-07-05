using System.Collections.Generic;
using Tessera.Core;
using Tessera.Data;

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

    /// <summary>StageFlow가 Gameplay Round 시작을 요청하는 이벤트다.</summary>
    public readonly struct StageRoundStartRequestedEvent
    {
        public RoundRuleContext RuleContext { get; }
        public int PlayerHPAtStart { get; }
        public OverchargeState StageOverchargeState { get; }
        public string RoundDisplayName { get; }
        public SlotPairDeviceDefinitionSO[] OpponentSlotPairDevices { get; }
        public StageRoundDefinitionSO RoundDefinition { get; }
        public EnemyIntent OpeningIntent { get; }
        public IReadOnlyList<DiceTypeDefinitionSO> EquippedDiceTypes { get; }

        /// <summary>Gameplay Round 시작 요청 이벤트를 생성한다.</summary>
        public StageRoundStartRequestedEvent(
            RoundRuleContext ruleContext,
            int playerHPAtStart,
            OverchargeState stageOverchargeState,
            string roundDisplayName,
            SlotPairDeviceDefinitionSO[] opponentSlotPairDevices,
            StageRoundDefinitionSO roundDefinition,
            EnemyIntent openingIntent,
            IReadOnlyList<DiceTypeDefinitionSO> equippedDiceTypes = null)
        {
            RuleContext = ruleContext;
            PlayerHPAtStart = playerHPAtStart;
            StageOverchargeState = stageOverchargeState;
            RoundDisplayName = roundDisplayName ?? string.Empty;
            OpponentSlotPairDevices = opponentSlotPairDevices;
            RoundDefinition = roundDefinition;
            OpeningIntent = openingIntent ?? EnemyIntent.None();
            EquippedDiceTypes = equippedDiceTypes;
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
        public IReadOnlyList<ShopInventorySlot> ProductSlots { get; }
        public StageWorkshopRulesSO WorkshopRules { get; }

        public StageShopShowRequestedEvent(
            TesseraRunSession runSession,
            StageBountyBoardState boardState,
            StageShopReasonType reasonType,
            string message,
            IReadOnlyList<ShopInventorySlot> productSlots,
            StageWorkshopRulesSO workshopRules)
        {
            RunSession = runSession;
            BoardState = boardState;
            ReasonType = reasonType;
            Message = message ?? string.Empty;
            ProductSlots = productSlots;
            WorkshopRules = workshopRules;
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

    /// <summary>UI에서 Shop 상품 구매 확정을 눌렀을 때 Runtime으로 보내는 이벤트다.</summary>
    public readonly struct StageShopProductBuyConfirmedEvent
    {
        public int ProductSlotIndex { get; }

        public StageShopProductBuyConfirmedEvent(int productSlotIndex)
        {
            ProductSlotIndex = productSlotIndex;
        }
    }

    /// <summary>UI에서 장착 Device 판매 확인 패널 표시를 요청했을 때 Runtime으로 보내는 이벤트다.</summary>
    public readonly struct StageShopEquippedDeviceSellPreviewRequestedEvent
    {
        /// <summary>판매 확인을 표시할 Player Device 슬롯 인덱스다.</summary>
        public int SlotIndex { get; }

        /// <summary>판매 확인 패널 표시 요청 이벤트를 생성한다.</summary>
        public StageShopEquippedDeviceSellPreviewRequestedEvent(int slotIndex)
        {
            SlotIndex = slotIndex;
        }
    }

    /// <summary>UI에서 장착된 Player Device 판매를 요청했을 때 Runtime으로 보내는 이벤트다.</summary>
    public readonly struct StageShopEquippedDeviceSellRequestedEvent
    {
        /// <summary>판매할 Player Device 슬롯 인덱스다.</summary>
        public int SlotIndex { get; }

        /// <summary>판매 요청 이벤트를 생성한다.</summary>
        public StageShopEquippedDeviceSellRequestedEvent(int slotIndex)
        {
            SlotIndex = slotIndex;
        }
    }

    /// <summary>UI에서 장착된 Player Device 슬롯 교체를 요청했을 때 Runtime으로 보내는 이벤트다.</summary>
    public readonly struct StageShopEquippedDeviceSwapRequestedEvent
    {
        /// <summary>드래그 시작 Device 슬롯 인덱스다.</summary>
        public int SourceSlotIndex { get; }

        /// <summary>드롭 대상 Device 슬롯 인덱스다.</summary>
        public int TargetSlotIndex { get; }

        /// <summary>Device 슬롯 교체 요청 이벤트를 생성한다.</summary>
        public StageShopEquippedDeviceSwapRequestedEvent(int sourceSlotIndex, int targetSlotIndex)
        {
            SourceSlotIndex = sourceSlotIndex;
            TargetSlotIndex = targetSlotIndex;
        }
    }

    /// <summary>Shop에서 Player Device 장착 상태가 변경되었음을 알리는 이벤트다.</summary>
    public readonly struct StageShopPlayerDevicesChangedEvent
    {
        public TesseraRunSession RunSession { get; }
        public string Reason { get; }

        public StageShopPlayerDevicesChangedEvent(TesseraRunSession runSession, string reason)
        {
            RunSession = runSession;
            Reason = reason ?? string.Empty;
        }
    }

    /// <summary>Shop에서 Player DiceType 장착 상태가 변경되었음을 알리는 이벤트다.</summary>
    public readonly struct StageShopPlayerDiceTypesChangedEvent
    {
        public TesseraRunSession RunSession { get; }
        public string Reason { get; }

        public StageShopPlayerDiceTypesChangedEvent(TesseraRunSession runSession, string reason)
        {
            RunSession = runSession;
            Reason = reason ?? string.Empty;
        }
    }

    /// <summary>Gameplay Presenter가 Round 승리를 Runtime으로 전달하는 이벤트다.</summary>
    public readonly struct GameplayRoundWonEvent
    {
        public ClashResolveResult Result { get; }
        public int PlayerHPAfterRound { get; }

        public GameplayRoundWonEvent(ClashResolveResult result, int playerHPAfterRound)
        {
            Result = result;
            PlayerHPAfterRound = playerHPAfterRound;
        }
    }

    /// <summary>Gameplay Presenter가 Round 패배를 Runtime으로 전달하는 이벤트다.</summary>
    public readonly struct GameplayRoundLostEvent
    {
        public ClashResolveResult Result { get; }
        public int PlayerHPAfterRound { get; }

        public GameplayRoundLostEvent(ClashResolveResult result, int playerHPAfterRound)
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
