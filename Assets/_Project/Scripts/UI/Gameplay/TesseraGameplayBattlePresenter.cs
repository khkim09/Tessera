using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading;
using Tessera.Core;
using Tessera.Data;
using Tessera.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Tessera.Infrastructure;

namespace Tessera.UI
{
    /// <summary>실제 플레이형 Round 진행 UI와 Core Round 상태를 연결한다.</summary>
    public class TesseraGameplayBattlePresenter : MonoBehaviour, IDeviceSlotReorderHandler
    {

        #region Nested Types

        /// <summary>Battle Presenter가 허용할 플레이어 입력 상태를 명시적으로 관리한다.</summary>
        private enum BattleInteractionState
        {
            None,
            PlayerIdle,
            DiceRolling,
            CastResolving,
            EnemyTurn,
            AttemptTransition,
            RoundEnded
        }

        #endregion

        #region Serialized Fields

        [Header("Round Simulator")]
        [Tooltip("디버그 고정")]
        [SerializeField] private bool useDeterministicCombatSeed = false; // true면 출시용 난수 대신 debugCombatSeed를 사용한다.
        [SerializeField] private int debugCombatSeed = 12345; // useDeterministicCombatSeed가 켜졌을 때 재현용으로 쓰는 Seed다.
        [SerializeField] private bool logCombatSeed; // true면 전투마다 activeCombatSeed를 Console에 출력한다.
        [SerializeField] private bool useFixedSeed = true;
        [SerializeField] private int seed = 12345;

        [Header("Manual Dice")]
        [SerializeField] private bool useManualDiceValuesOnStart = true;
        [SerializeField] private int die1 = 2;
        [SerializeField] private int die2 = 2;
        [SerializeField] private int die3 = 2;
        [SerializeField] private int die4 = 5;
        [SerializeField] private int die5 = 5;

        [Header("Runtime Player Slot Pair Devices Debug Mirror")]
        [SerializeField, ReadOnlyInspector] private SlotPairDeviceDefinitionSO[] slotPairDevices = new SlotPairDeviceDefinitionSO[5];

        [Header("Dice Synergy Rules")]
        [SerializeField] private DiceSynergyDefinitionSO[] diceSynergyDefinitions;

        [Header("Runtime Opponent Slot Pair Devices Debug Mirror")]
        [SerializeField, ReadOnlyInspector] private SlotPairDeviceDefinitionSO[] opponentSlotPairDevices = new SlotPairDeviceDefinitionSO[5];

        [Header("3D Device Views")]
        [SerializeField] private DeviceRack3DView playerDeviceRack3DView; // 플레이어 device slot
        [SerializeField] private DeviceRack3DView opponentDeviceRack3DView; // 상대 device slot

        [Header("3D Dice Tray Views")]
        [SerializeField] private DiceTray3DView diceTray3DView; // 주사위 굴리는 tray

        [Header("Dice Cup Roll Input")]
        [SerializeField] private DiceCup3DView diceCup3DView;
        [SerializeField] private bool useDiceCupRollInput = true;

        [Header("Dice Cup Roll - Dice Enter")]
        [SerializeField] private float diceCupEnterDuration = 0.22f;
        [SerializeField] private float diceCupEnterStagger = 0.04f;
        [SerializeField] private float diceCupEnterArcHeight = 0.18f;
        [SerializeField] private Vector3 diceCupEnterRollEuler = new Vector3(210f, 120f, 160f);

        [Header("Dice Cup Roll - Cup Motion")]
        [SerializeField] private float diceCupLiftHeight = 0.38f;
        [SerializeField] private float diceCupLiftDuration = 0.16f;
        [SerializeField] private float diceCupShakeDuration = 0.58f;
        [SerializeField] private float diceCupDropDuration = 0.18f;
        [SerializeField] private float diceCupShakeAngle = 12f;
        [SerializeField] private float diceCupShakeFrequency = 9f;

        [Header("Dice Cup Roll - Dice Scatter")]
        [SerializeField] private float diceCupScatterDuration = 0.34f;
        [SerializeField] private float diceCupScatterStagger = 0.035f;
        [SerializeField] private float diceCupScatterArcHeight = 0.28f;
        [SerializeField] private Vector3 diceCupScatterRollEuler = new Vector3(360f, 240f, 180f);

        [Header("SlotPair Evaluation Presentation")]
        [SerializeField] private bool playSlotPairSequenceOnSubmit = true;
        [SerializeField] private float slotPairSequenceStartDelay = 0.25f;
        [SerializeField] private float slotPairHighlightDuration = 0.65f;
        [SerializeField] private float slotPairHighlightGap = 0.15f;
        /// <summary>SlotPair 슬롯 연출 시작 후 홀로그램 누적값 갱신을 시작할 시점 비율이다.</summary>
        [SerializeField] private float slotPairHologramUpdateLeadRatio = 0.58f;

        [Header("SlotPair Floating Text")]
        [SerializeField] private bool playSlotPairFloatingText = true;
        [SerializeField] private SlotPairStepFloatingTextView slotPairStepFloatingTextView;
        [SerializeField] private RectTransform slotPairFloatingTextRoot;
        [SerializeField] private Camera battleCamera;
        [SerializeField] private Vector3 playerSlotPairFloatingWorldOffset = new Vector3(0f, 0f, -0.3f);
        [SerializeField] private Vector3 opponentSlotPairFloatingWorldOffset = new Vector3(0f, 0f, 0.3f);

        [Header("DeviceSlot Lock Dice Presentation")]
        /// <summary>Player Lock Dice가 이동할 Slot별 Anchor 배열이다.</summary>
        [SerializeField] private Transform[] playerLockedDiceSlotAnchors = new Transform[5];
        /// <summary>Opponent Lock Dice가 이동할 Slot별 Anchor 배열이다.</summary>
        [SerializeField] private Transform[] opponentLockedDiceSlotAnchors = new Transform[5];
        /// <summary>Lock Dice가 Slot Anchor로 이동하는 연출 시간이다.</summary>
        [SerializeField] private float lockedDiceMoveDuration = 0.16f; // Dice가 Slot Anchor로 이동하는 Tween 시간이다.
        /// <summary>Opponent Roll 결과가 나온 뒤 Dice를 Lock하기 전 대기 시간이다.</summary>
        [SerializeField] private float opponentLockedDiceMoveDelay = 0.3f; // Opponent Roll 결과 표시와 AI Lock 선택 사이에 두는 전용 지연 시간이다.
        /// <summary>SlotPair 판정 후 Dice를 Tray로 복귀시킬지 여부이다.</summary>
        [SerializeField] private bool restoreDiceToTrayAfterEvaluation = true; // 판정 연출 종료 후 Dice를 Tray 위치로 되돌릴지 결정한다.

        [Header("SlotPair Dice Jump Roll")]
        [SerializeField] private bool playDiceJumpRollDuringEvaluation = true;
        [SerializeField] private float slotPairDiceJumpHeight = 0.16f;
        [SerializeField] private float slotPairDiceJumpRollDuration = 0.42f;
        [SerializeField] private Vector3 slotPairDiceJumpRollEuler = new Vector3(180f, 180f, 0f);

        [Header("SlotPair Debug")]
        [SerializeField] private bool logSlotPairEvaluationSteps = true;

        [Header("Opponent AI Debug")]
        [SerializeField] private bool enableOpponentRollAIDebugLogs = true;

        [Header("Table Hologram View")]
        [SerializeField] private bool useTableHologramView = true;
        [SerializeField] private BattleTableHologramView tableHologramView;

        [Header("Popup")]
        [SerializeField] private bool showCastCandidatePopup = true;

        [Header("Left Info Texts")]
        [SerializeField] private TMP_Text roundTitleText;
        [SerializeField] private TMP_Text opponentHPText;
        [SerializeField] private TMP_Text playerHPText;

        [Header("Cast Popup")]
        [SerializeField] private CastCandidatePopupView castCandidatePopupView;

        [Header("Buttons")]
        [SerializeField] private Button submitSelectedButton;
        [SerializeField] private Button togglePopupButton;

        [Header("Turn Flow")]
        [SerializeField] private bool autoStartNextAttemptAfterCast = true;
        [SerializeField] private float autoNextAttemptDelay = 0.8f;
        [SerializeField] private float enemyTurnStartDelay = 0.7f;
        [SerializeField] private float enemyClashResultRevealDelay = 0.75f;
        [SerializeField] private float enemyTurnToPlayerDelay = 0.7f;
        [SerializeField] private bool playEnemyDiceCupPresentation = true;

        [Header("Enemy Turn Dice Presentation")]
        [SerializeField] private float ownerDiceRestMoveDuration = 0.4f;
        [SerializeField] private Vector3 enemyDiceCupEnterRollEuler = new Vector3(180f, 90f, 180f);
        [SerializeField] private Vector3 enemyDiceCupScatterRollEuler = new Vector3(240f, 180f, 120f);

        [Header("Money Overlay")]
        [SerializeField] private TMP_Text moneyText;

        [Header("External Input Lock")]
        [SerializeField] private bool externalGameplayInputLocked;
        [SerializeField] private string externalGameplayInputLockReason = "Gameplay input is locked.";

        #endregion

        #region Runtime State

        /// <summary>Player DeviceSlot별로 현재 고정된 Dice 인덱스를 저장한다.</summary>
        private readonly int[] lockedDiceIndexBySlot = { -1, -1, -1, -1, -1 };
        /// <summary>Opponent DeviceSlot별로 현재 고정된 Dice 인덱스를 저장한다.</summary>
        private readonly int[] opponentLockedDiceIndexBySlot = { -1, -1, -1, -1, -1 };
        private string currentRoundDisplayName = "Round";

        private CoreRoundSimulator simulator;
        private RoundState roundState;
        private CastBoardModelBuilder castBoardModelBuilder;
        private CastBoardViewModel currentCastBoardViewModel;
        private SlotPairDamagePreview currentSlotPairPreview;
        /// <summary>현재 선택 Cast의 족보 기준 계산 결과를 보관한다.</summary>
        private PatternResult currentPreviewPatternResult;
        private TableRuleEvaluationResult currentPreviewTableRuleResult;
        /// <summary>현재 SlotPair 연출이 최종 누적값 표시까지 완료되었는지 여부다.</summary>
        private bool isSlotPairResultPresentationComplete;
        private RollPatternType selectedPatternType = RollPatternType.None;
        private int earnedMoney;
        private TesseraRunSession runSession;
        private readonly List<int> lastSubmittedDiceValues = new List<int>();
        private bool hasLastSubmittedDiceValues;
        private bool roundEndNotified;
        private CancellationTokenSource slotPairSequenceCts;
        private BattleInteractionState interactionState = BattleInteractionState.None;
        private ClashCastResult pendingOpponentClashResult;
        private ClashResolveResult lastClashResolveResult;
        private StageRoundDefinitionSO currentRoundDefinition;
        private EnemyIntentDefinitionSO currentEnemyIntentDefinition;
        private InitiativeOwnerType lockedRoundInitiativeOwner = InitiativeOwnerType.Opponent;
        private bool hasLockedRoundInitiativeOwner;
        private DiceRoller opponentDiceRoller;
        private int combatEntrySerial;
        private int activeCombatSeed;

        /// <summary>RunInfo 족보 표시용 캐시 스냅샷 목록이다.</summary>
        private readonly List<RunInfoCastBookEntrySnapshot> cachedRunInfoCastBookSnapshots = new List<RunInfoCastBookEntrySnapshot>();

        /// <summary>RunInfo 족보 캐시가 현재 최신 상태가 아닌지 여부다.</summary>
        private bool isRunInfoCastBookCacheDirty = true;

        /// <summary>RunInfo 족보 캐시 재계산이 진행 중인지 여부다.</summary>
        private bool isRunInfoCastBookCacheRebuilding;

        /// <summary>RunInfo 족보 캐시 재계산 취소 토큰이다.</summary>
        private CancellationTokenSource runInfoCastBookCacheCts;

        /// <summary>RunInfo 족보 캐시 재계산 세대 번호다.</summary>
        private int runInfoCastBookCacheRebuildSerial;

        #endregion

        #region Events And Properties

        /// <summary>Round 승리 확정</summary>
        public event Action<ClashResolveResult> RoundWon;

        /// <summary>Round 패배 확정</summary>
        public event Action<ClashResolveResult> RoundLost;

        /// <summary>RunInfo 족보 캐시 갱신 완료 이벤트다.</summary>
        public event Action<IReadOnlyList<RunInfoCastBookEntrySnapshot>> RunInfoCastBookSnapshotsUpdated;

        /// <summary>현재 진행 중인 Core RoundState를 반환한다.</summary>
        public RoundState CurrentRoundState => roundState;

        #endregion

        #region Unity Lifecycle

        /// <summary>Core 시뮬레이터와 ViewModel 빌더를 준비한다.</summary>
        private void Awake()
        {
            activeCombatSeed = ResolveCombatSeed();
            simulator = new CoreRoundSimulator(activeCombatSeed);
            castBoardModelBuilder = CastBoardModelBuilder.CreateDefault();

            // 슬롯 클릭 콜백은 한 번만 연결하고 이후에는 내부 매핑만 갱신한다.
            InitializeDiceSlots();

            // Device 슬롯 드래그 재정렬을 위해 각 슬롯에 인덱스와 핸들러를 전달한다.
            InitializeDeviceSlots();
        }

        /// <summary>버튼 및 DeviceSlot 이벤트를 연결한다.</summary>
        private void OnEnable()
        {
            AddButtonListeners();
        }

        /// <summary>버튼 및 DeviceSlot 이벤트를 해제한다.</summary>
        private void OnDisable()
        {
            RemoveButtonListeners();
        }

        /// <summary>Presenter가 제거될 때 비동기 연출 작업을 정리한다.</summary>
        private void OnDestroy()
        {
            // 진행 중인 SlotPair 연출을 안전하게 중단한다.
            CancelSlotPairEvaluationSequence();
            CancelRunInfoCastBookCacheRebuild();
        }

        /// <summary>Inspector 배열 길이를 고정 슬롯 수에 맞게 보정한다.</summary>
        private void OnValidate()
        {
            if (slotPairDevices == null || slotPairDevices.Length != SlotPairDamageCalculator.SlotPairCount)
                slotPairDevices = ResizeDeviceArray(slotPairDevices, SlotPairDamageCalculator.SlotPairCount);

            if (opponentSlotPairDevices == null || opponentSlotPairDevices.Length != SlotPairDamageCalculator.SlotPairCount)
                opponentSlotPairDevices = ResizeDeviceArray(opponentSlotPairDevices, SlotPairDamageCalculator.SlotPairCount);

            if (playerLockedDiceSlotAnchors == null || playerLockedDiceSlotAnchors.Length != SlotPairDamageCalculator.SlotPairCount)
                playerLockedDiceSlotAnchors = ResizeTransformArray(playerLockedDiceSlotAnchors, SlotPairDamageCalculator.SlotPairCount);

            if (opponentLockedDiceSlotAnchors == null || opponentLockedDiceSlotAnchors.Length != SlotPairDamageCalculator.SlotPairCount)
                opponentLockedDiceSlotAnchors = ResizeTransformArray(opponentLockedDiceSlotAnchors, SlotPairDamageCalculator.SlotPairCount);
        }




        /// <summary>현재 RunSession의 DiceType 색상을 Player DiceView에 적용한다.</summary>
        private void ApplyPlayerDiceTypeVisuals()
        {
            if (diceTray3DView == null || runSession == null)
                return;

            List<Color> colors = new List<Color>(TesseraRunSession.PlayerDiceCount);

            for (int diceIndex = 0; diceIndex < TesseraRunSession.PlayerDiceCount; diceIndex++)
            {
                DiceTypeDefinitionSO diceType = runSession.GetEquippedDiceType(diceIndex);
                colors.Add(diceType != null ? diceType.VisualColor : Color.white);
            }

            diceTray3DView.SetDiceTypeVisualColors(DiceOwnerType.Player, colors);
        }

        /// <summary>Data 계층 DiceType SO 목록을 Core intrinsic 데이터 목록으로 변환한다.</summary>
        private static List<DiceTypeIntrinsicData> BuildDiceTypeIntrinsicData(IReadOnlyList<DiceTypeDefinitionSO> equippedDiceTypes)
        {
            if (equippedDiceTypes == null)
                return null;

            List<DiceTypeIntrinsicData> result = new List<DiceTypeIntrinsicData>(equippedDiceTypes.Count);

            for (int i = 0; i < equippedDiceTypes.Count; i++)
            {
                DiceTypeDefinitionSO diceType = equippedDiceTypes[i];
                result.Add(diceType != null
                    ? new DiceTypeIntrinsicData(diceType.DisplayName, diceType.IntrinsicEffectType, diceType.IntValue, diceType.FloatValue, (int)diceType.SynergyTag)
                    : DiceTypeIntrinsicData.Empty);
            }

            return result;
        }


        /// <summary>Data 계층 DiceSynergy SO 목록을 Core 규칙 데이터 목록으로 변환한다.</summary>
        private static List<DiceSynergyRuleData> BuildDiceSynergyRuleData(IReadOnlyList<DiceSynergyDefinitionSO> synergyDefinitions)
        {
            if (synergyDefinitions == null)
                return null;

            List<DiceSynergyRuleData> result = new List<DiceSynergyRuleData>(synergyDefinitions.Count);

            for (int i = 0; i < synergyDefinitions.Count; i++)
            {
                DiceSynergyDefinitionSO synergy = synergyDefinitions[i];
                if (synergy != null && synergy.EffectType != Tessera.Data.DiceSynergyEffectType.None)
                    result.Add(synergy.ToCoreRuleData());
            }

            return result;
        }

        /// <summary>RunSession의 DiceFaceUpgrade 장착 상태를 Core Round 계산용 데이터로 변환한다.</summary>
        private static List<DiceFaceUpgradeData> BuildDiceFaceUpgradeData(TesseraRunSession runSession)
        {
            if (runSession == null)
                return null;

            List<DiceFaceUpgradeData> result = new List<DiceFaceUpgradeData>(TesseraRunSession.PlayerDiceCount * TesseraRunSession.DiceFaceCount);
            for (int diceIndex = 0; diceIndex < TesseraRunSession.PlayerDiceCount; diceIndex++)
            {
                for (int faceIndex = 0; faceIndex < TesseraRunSession.DiceFaceCount; faceIndex++)
                {
                    DiceFaceUpgradeDefinitionSO upgrade = runSession.GetDiceFaceUpgrade(diceIndex, faceIndex);
                    result.Add(upgrade != null
                        ? new DiceFaceUpgradeData(true, upgrade.CreateReplacementFace())
                        : DiceFaceUpgradeData.Empty);
                }
            }

            return result;
        }

        /// <summary>출시용/디버그용 Combat Seed를 분리해 계산한다.</summary>
        private int ResolveCombatSeed()
        {
            combatEntrySerial++;

            if (useDeterministicCombatSeed || useFixedSeed)
                return debugCombatSeed != 0 ? debugCombatSeed : seed;

            return unchecked(Environment.TickCount ^ (combatEntrySerial * 397) ^ Guid.NewGuid().GetHashCode());
        }

        #endregion

        #region Public API

        /// <summary>외부 Stage/Bounty Flow에서 지정한 규칙, 상대 Device, 이월 HP/Overcharge 상태로 새 Round를 시작한다.</summary>
        public void StartRound(
            RoundRuleContext ruleContext,
            int carriedPlayerHP,
            OverchargeState stageOverchargeState,
            string roundDisplayName,
            SlotPairDeviceDefinitionSO[] roundOpponentSlotPairDevices,
            StageRoundDefinitionSO roundDefinition,
            EnemyIntent openingIntent,
            System.Collections.Generic.IReadOnlyList<Tessera.Data.DiceTypeDefinitionSO> equippedDiceTypes = null)
        {
            if (ruleContext == null)
            {
                Debug.LogWarning("[Tessera][Battle] Cannot start round. RuleContext is null.");
                return;
            }

            if (stageOverchargeState == null)
            {
                Debug.LogWarning("[Tessera][Battle] Cannot start round. StageOverchargeState is null.");
                return;
            }

            activeCombatSeed = ResolveCombatSeed();
            simulator = new CoreRoundSimulator(activeCombatSeed);
            roundState = simulator.StartRound(
                ruleContext,
                carriedPlayerHP,
                stageOverchargeState,
                BuildDiceTypeIntrinsicData(equippedDiceTypes),
                BuildDiceSynergyRuleData(diceSynergyDefinitions),
                BuildDiceFaceUpgradeData(runSession));
            ApplyPlayerDiceTypeVisuals();
            currentRoundDefinition = roundDefinition;
            currentEnemyIntentDefinition = currentRoundDefinition != null
                ? currentRoundDefinition.SelectIntentDefinitionForAttempt(1, activeCombatSeed)
                : null;

            ResolveAndStoreRoundInitiativeOwner(openingIntent);
            opponentDiceRoller = new DiceRoller(activeCombatSeed + 9109);
            if (logCombatSeed)
                Debug.Log($"[Tessera][Battle] CombatSeed={activeCombatSeed} Serial={combatEntrySerial}");

            currentRoundDisplayName = string.IsNullOrWhiteSpace(roundDisplayName) ? "Round" : roundDisplayName;
            roundEndNotified = false;
            SetInteractionState(BattleInteractionState.PlayerIdle);

            if (runSession == null)
                earnedMoney = 0;

            selectedPatternType = RollPatternType.None;
            currentSlotPairPreview = null;
            currentPreviewPatternResult = null;
            currentPreviewTableRuleResult = null;
            isSlotPairResultPresentationComplete = false;

            ClearLockSlotMapping();
            ClearLastSubmittedDiceValues();

            if (useManualDiceValuesOnStart)
                simulator.SetCurrentDiceValuesForTest(roundState, CreateManualDiceValues());

            SyncDevicesFromRunSession();
            SetOpponentSlotPairDevices(roundOpponentSlotPairDevices);

            pendingOpponentClashResult = null;
            lastClashResolveResult = null;

            RefreshClashPowerTexts();
            EnemyIntent lockedOpeningIntent = BuildOpeningEnemyIntentWithLockedInitiative(openingIntent);
            ApplyOpeningIntentToCurrentAttempt(lockedOpeningIntent);

            CancelSlotPairEvaluationSequence();
            ClearSlotPairEvaluationHighlights();

            string message = string.IsNullOrWhiteSpace(roundDisplayName)
                ? "Round started."
                : $"{roundDisplayName} started.";

            SetExternalGameplayInputLocked(false, string.Empty);
            RefreshAll(message);

            MarkRunInfoCastBookCacheDirty();
            RefreshRunInfoCastBookFallbackCache();
            RequestRunInfoCastBookCacheRebuild();

            if (roundState.CurrentAttempt.InitiativeOwner == InitiativeOwnerType.Opponent)
                StartOpponentFirstTurnAsync().Forget();
            else
                PreparePlayerFirstRoundDicePresentationAsync().Forget();
        }

        /// <summary>외부 RunSession을 연결하고 장착 Device/Money 상태를 동기화한다.</summary>
        public void BindRunSession(TesseraRunSession runSession)
        {
            this.runSession = runSession;

            SyncDevicesFromRunSession();
            ApplyPlayerDiceTypeVisuals();

            if (roundState != null)
                RefreshAll(null);
            else
                RefreshDeviceSlotViews();

            MarkRunInfoCastBookCacheDirty();
            RefreshRunInfoCastBookFallbackCache();
            RequestRunInfoCastBookCacheRebuild();
        }

        /// <summary>RunSession의 현재 장착 Device를 Presenter Debug Mirror와 DeviceSlot View에 다시 반영한다.</summary>
        public void RefreshEquippedDevicesFromRunSession(string reason)
        {
            SyncDevicesFromRunSession();
            ApplyPlayerDiceTypeVisuals();

            MarkRunInfoCastBookCacheDirty();
            RefreshRunInfoCastBookFallbackCache();
            RequestRunInfoCastBookCacheRebuild();

            if (roundState != null)
            {
                BuildCurrentSlotPairPreview();
                RefreshSelectedCastTexts();
                RefreshAll(reason);
                return;
            }

            RefreshDeviceSlotViews();

            if (!string.IsNullOrWhiteSpace(reason))
                SetMessage(reason);
        }

        /// <summary>Workshop/StageFlow에서 변경된 플레이어 HP를 TableScreen 표시 텍스트에 반영한다.</summary>
        public void RefreshExternalPlayerHPDisplay(int currentHP, int maxHP, string reason)
        {
            int safeMaxHP = Mathf.Max(1, maxHP);
            int safeCurrentHP = Mathf.Clamp(currentHP, 0, safeMaxHP);

            SetText(playerHPText, $"Player HP {safeCurrentHP}/{safeMaxHP}");

            if (!string.IsNullOrWhiteSpace(reason))
                SetMessage(reason);
        }

        public void RefreshExternalOverchargeDisplay(int currentOvercharge, string reason)
        {
            if (useTableHologramView && tableHologramView != null && roundState != null)
            {
                int remainingAttempts = CalculateRemainingAttempts();

                tableHologramView.RefreshBattleMeta(
                    remainingAttempts,
                    roundState.RuleContext.MaxAttempts,
                    roundState.RemainingRollsThisAttempt,
                    roundState.MaxRollsThisAttempt,
                    currentOvercharge);
            }

            if (!string.IsNullOrWhiteSpace(reason))
                SetMessage(reason);
        }

        /// <summary>외부 GameMode/StageFlow에 의해 Gameplay 입력 잠금 상태를 변경한다.</summary>
        public void SetExternalGameplayInputLocked(bool isLocked, string reason)
        {
            externalGameplayInputLocked = isLocked;
            externalGameplayInputLockReason = string.IsNullOrWhiteSpace(reason)
                ? "Gameplay input is locked."
                : reason;

            RefreshButtonStates();
            RefreshCastPopup();

            if (externalGameplayInputLocked)
                SetMessage(externalGameplayInputLockReason);
        }

        /// <summary>잠기지 않은 주사위를 다시 굴린다.</summary>
        public void RollUnlockedDice()
        {
            if (!TryCanRollUnlockedDice(out string failureMessage))
            {
                SetMessage(failureMessage);
                return;
            }

            RollUnlockedDiceCore("Unlocked dice rolled.");
        }

        /// <summary>현재 선택한 Cast를 SlotPair 계산값으로 제출한다.</summary>
        public void SubmitSelectedCast()
        {
            if (roundState == null)
            {
                SetMessage("No active round.");
                return;
            }

            if (roundState.IsRoundEnded || interactionState == BattleInteractionState.RoundEnded)
            {
                SetMessage("Round already ended. Awaiting stage decision.");
                return;
            }

            if (!CanActInCurrentAttempt())
            {
                SetMessage("Cannot submit now.");
                return;
            }

            if (!roundState.CanSubmitCastNow())
            {
                SetMessage("Roll the dice first.");
                return;
            }

            if (selectedPatternType == RollPatternType.None)
            {
                SetMessage("Select a cast first.");
                return;
            }

            // 제출 직전 비어 있는 Lock 슬롯을 DiceIndex 오름차순으로 자동 채운다.
            EnsureAllDiceAssignedToLockSlots();

            bool built = simulator.TryBuildPlayerClashCastResult(
                roundState,
                selectedPatternType,
                CreateLockSlotDiceIndexList(),
                CreatePlayerDeviceDefinitions(),
                out ClashCastResult playerResult);

            if (!built || playerResult == null)
            {
                RefreshAll("Selected cast cannot be submitted.");
                return;
            }

            SetInteractionState(BattleInteractionState.CastResolving);
            selectedPatternType = playerResult.PatternType;
            currentSlotPairPreview = playerResult.SlotPairDamagePreview;
            currentPreviewPatternResult = playerResult.PatternResult;
            currentPreviewTableRuleResult = playerResult.TableRuleEvaluationResult;
            isSlotPairResultPresentationComplete = false;

            RefreshClashPowerTexts();
            RefreshAll(BuildSubmittedCastStartMessage(playerResult));
            PlaySubmittedClashFlowAsync(playerResult).Forget();
        }

        /// <summary>Broken Cast를 SlotPair 계산값으로 제출한다.</summary>
        public void SubmitBrokenCast()
        {
            if (roundState == null)
            {
                SetMessage("No active round.");
                return;
            }

            if (roundState.IsRoundEnded || interactionState == BattleInteractionState.RoundEnded)
            {
                SetMessage("Round already ended. Awaiting stage decision.");
                return;
            }

            if (!CanActInCurrentAttempt())
            {
                SetMessage("Cannot submit Broken Cast now.");
                return;
            }

            if (!roundState.CanSubmitCastNow())
            {
                SetMessage("Roll the dice first.");
                return;
            }

            // Broken Cast도 SlotPair 연출 대상이므로 5칸을 먼저 채운다.
            EnsureAllDiceAssignedToLockSlots();

            bool built = simulator.TryBuildPlayerClashCastResult(
                roundState,
                RollPatternType.BrokenCast,
                CreateLockSlotDiceIndexList(),
                CreatePlayerDeviceDefinitions(),
                out ClashCastResult playerResult);

            if (!built || playerResult == null)
            {
                RefreshAll("Broken Cast cannot be submitted.");
                return;
            }

            SetInteractionState(BattleInteractionState.CastResolving);
            selectedPatternType = playerResult.PatternType;
            currentSlotPairPreview = playerResult.SlotPairDamagePreview;
            currentPreviewPatternResult = playerResult.PatternResult;
            currentPreviewTableRuleResult = playerResult.TableRuleEvaluationResult;
            isSlotPairResultPresentationComplete = false;

            RefreshClashPowerTexts();
            RefreshAll(BuildSubmittedCastStartMessage(playerResult));
            PlaySubmittedClashFlowAsync(playerResult).Forget();
        }

        /// <summary>다음 Attempt 시작을 시도한다.</summary>
        public void StartNextAttempt()
        {
            if (roundState == null)
            {
                SetMessage("No active round.");
                return;
            }

            if (roundState.IsRoundEnded || interactionState == BattleInteractionState.RoundEnded)
            {
                SetMessage("Round already ended. Awaiting stage decision.");
                return;
            }

            SetInteractionState(BattleInteractionState.AttemptTransition);

            int nextAttemptNumber = roundState.CurrentAttempt.AttemptNumber + 1;
            EnemyIntent nextIntent = BuildEnemyIntentForAttempt(nextAttemptNumber);

            bool started = simulator.TryStartNextAttempt(
                roundState,
                nextIntent);

            if (!started)
            {
                SetInteractionState(BattleInteractionState.PlayerIdle);
                RefreshAll("Cannot start next attempt.");
                return;
            }

            selectedPatternType = RollPatternType.None;
            currentSlotPairPreview = null;
            currentPreviewPatternResult = null;
            currentPreviewTableRuleResult = null;
            isSlotPairResultPresentationComplete = false;
            pendingOpponentClashResult = null;
            lastClashResolveResult = null;

            ClearClashPowerTexts();
            ApplyOpeningIntentToCurrentAttempt(roundState.CurrentEnemyIntent);
            ClearLockSlotMapping();

            for (int diceIndex = 0; diceIndex < roundState.Dice.Count; diceIndex++)
                simulator.SetDiceLocked(roundState, diceIndex, false);

            ApplyLastSubmittedDiceValuesToNewAttemptIfPossible();

            CancelSlotPairEvaluationSequence();
            ClearSlotPairEvaluationHighlights();

            if (diceTray3DView != null)
                diceTray3DView.RestoreAllDiceToTray(roundState.GetCurrentDiceValues(), lockedDiceMoveDuration);

            if (roundState.CurrentAttempt.InitiativeOwner == InitiativeOwnerType.Opponent)
            {
                StartOpponentFirstTurnAsync().Forget();
            }
            else
            {
                SetInteractionState(BattleInteractionState.PlayerIdle);
                RefreshAll("Next attempt started. Roll the dice.");
            }
        }

        /// <summary>Cast 후보 Popup 표시 여부를 토글한다.</summary>
        public void ToggleCastCandidatePopup()
        {
            if (!CanTogglePopup())
            {
                SetMessage("Cannot toggle popup now.");
                return;
            }

            showCastCandidatePopup = !showCastCandidatePopup;

            if (castCandidatePopupView != null)
                castCandidatePopupView.SetPopupVisible(showCastCandidatePopup);

            RefreshAll(showCastCandidatePopup ? "Cast popup ON." : "Cast popup OFF.");
        }

        /// <summary>IDeviceSlotReorderHandler: 두 Device 슬롯의 SlotPairDeviceDefinitionSO를 교체한다.</summary>
        public void RequestDeviceSlotSwap(int sourceSlotIndex, int targetSlotIndex)
        {
            if (!CanSwapPlayerDevicesNow()) return;
            if (sourceSlotIndex == targetSlotIndex) return;
            if (!IsValidDeviceSlotIndex(sourceSlotIndex)) return;
            if (!IsValidDeviceSlotIndex(targetSlotIndex)) return;
            if (slotPairDevices == null || slotPairDevices[sourceSlotIndex] == null) return;

            if (runSession != null)
            {
                if (!runSession.SwapEquippedDevices(sourceSlotIndex, targetSlotIndex))
                    return;

                SyncDevicesFromRunSession();
            }
            else
            {
                SlotPairDeviceDefinitionSO temp = slotPairDevices[sourceSlotIndex];
                slotPairDevices[sourceSlotIndex] = slotPairDevices[targetSlotIndex];
                slotPairDevices[targetSlotIndex] = temp;
            }

            RefreshDeviceSlotViews();

            if (roundState != null && !roundState.IsRoundEnded && !roundState.CurrentAttempt.IsSubmitted)
            {
                BuildCurrentSlotPairPreview();
                RefreshSelectedCastTexts();
            }

            MarkRunInfoCastBookCacheDirty();
            RefreshRunInfoCastBookFallbackCache();
            RequestRunInfoCastBookCacheRebuild();
        }

        /// <summary>현재 Gameplay 중 Player DeviceSlot swap이 가능한지 확인한다.</summary>
        public bool CanSwapPlayerDevicesNow()
        {
            if (slotPairDevices == null)
                return false;

            if (roundState == null)
                return false;

            if (roundState.IsRoundEnded)
                return false;

            if (roundState.CurrentAttempt == null)
                return false;

            if (roundState.CurrentAttempt.IsSubmitted)
                return false;

            switch (interactionState)
            {
                case BattleInteractionState.PlayerIdle:
                case BattleInteractionState.EnemyTurn:
                    return true;

                case BattleInteractionState.None:
                case BattleInteractionState.DiceRolling:
                case BattleInteractionState.CastResolving:
                case BattleInteractionState.AttemptTransition:
                case BattleInteractionState.RoundEnded:
                default:
                    return false;
            }
        }

        /// <summary>DeviceSlot 인덱스가 현재 장착 배열 범위 안에 있는지 확인한다.</summary>
        private bool IsValidDeviceSlotIndex(int slotIndex)
        {
            if (slotPairDevices == null)
                return false;

            return slotIndex >= 0 && slotIndex < slotPairDevices.Length;
        }

        /// <summary>현재 Run/Deck/Device 기준 RunInfo 족보 표시 스냅샷을 반환한다.</summary>
        public IReadOnlyList<RunInfoCastBookEntrySnapshot> BuildRunInfoCastBookSnapshots()
        {
            if (roundState == null)
                return new List<RunInfoCastBookEntrySnapshot>();

            if (cachedRunInfoCastBookSnapshots.Count <= 0)
                RefreshRunInfoCastBookFallbackCache();

            if (isRunInfoCastBookCacheDirty && !isRunInfoCastBookCacheRebuilding)
                RequestRunInfoCastBookCacheRebuild();

            return new List<RunInfoCastBookEntrySnapshot>(cachedRunInfoCastBookSnapshots);
        }

        #endregion

        #region Round Start And Attempt Flow

        /// <summary>Opponent 선공 Attempt 시작 흐름을 처리한다.</summary>
        private async UniTaskVoid StartOpponentFirstTurnAsync()
        {
            if (roundState == null)
                return;

            SetInteractionState(BattleInteractionState.EnemyTurn);
            RefreshDiceInteractionState();

            await MovePlayerDiceSetToRestIfNeededAsync();

            bool built = await BuildOpponentClashResultWithRollAIAsync("OpponentFirst", null);

            if (!built || pendingOpponentClashResult == null)
            {
                SetMessage("Opponent failed to prepare clash.");
                SetInteractionState(BattleInteractionState.PlayerIdle);
                await PreparePlayerFirstRoundDicePresentationAsync();
                RefreshAll("Player turn. Roll the dice.");
                return;
            }

            LogOpponentClashCastResult("OpponentFirst", pendingOpponentClashResult);

            await PlayOpponentSlotPairEvaluationSequenceAsync(pendingOpponentClashResult);

            SetMessage(BuildOpponentTargetMessage(pendingOpponentClashResult));
            RefreshClashPowerTexts();

            if (enemyClashResultRevealDelay > 0f)
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(enemyClashResultRevealDelay),
                    cancellationToken: this.GetCancellationTokenOnDestroy());
            }

            // 상대 선공 결과를 공개한 뒤, 플레이어 턴으로 넘기기 전에 상대 Dice를 RestPoint로 1회만 회수한다.
            await MoveOpponentDiceSetToRestIfNeededAsync();

            if (enemyTurnToPlayerDelay > 0f)
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(enemyTurnToPlayerDelay),
                    cancellationToken: this.GetCancellationTokenOnDestroy());
            }

            SetInteractionState(BattleInteractionState.PlayerIdle);

            // 위에서 상대 Dice를 이미 회수했으므로 Prepare 단계에서는 Opponent 회수를 생략한다.
            await PreparePlayerFirstRoundDicePresentationAsync(false);

            RefreshAll($"Beat Power {pendingOpponentClashResult.CastPower}, or use Broken Cast to reduce incoming Impact.");
        }

        /// <summary>상대 선공 Clash 결과를 플레이어에게 보여줄 메시지로 변환한다.</summary>
        private string BuildOpponentTargetMessage(ClashCastResult opponentResult)
        {
            if (opponentResult == null)
                return "Opponent failed to prepare clash.";

            string castName = CastBoardCatalog.GetDisplayName(opponentResult.PatternType);

            int score = opponentResult.SlotPairDamagePreview != null
                ? opponentResult.SlotPairDamagePreview.FinalScore
                : 0;

            string force = opponentResult.SlotPairDamagePreview != null
                ? opponentResult.SlotPairDamagePreview.FormatFinalForce()
                : "-";

            return $"Opponent set target: {castName} / Score {score} x Force {force} = Power {opponentResult.CastPower} / Expected Impact {opponentResult.ExpectedImpactDamage}.";
        }

        /// <summary>플레이어 Cast 제출 이후 SlotPair 연출, 상대 후공 계산, Clash 판정을 순서대로 처리한다.</summary>
        private async UniTaskVoid PlaySubmittedClashFlowAsync(ClashCastResult playerResult)
        {
            if (playerResult == null)
                return;

            await PlaySlotPairEvaluationSequenceAsync(playerResult);

            if (pendingOpponentClashResult == null)
                await BuildOpponentClashResultAfterPlayerSubmitAsync(playerResult);

            if (pendingOpponentClashResult == null)
            {
                SetMessage("Opponent clash result is missing.");
                SetInteractionState(BattleInteractionState.PlayerIdle);
                return;
            }

            lastClashResolveResult = simulator.ResolveClash(
                roundState,
                playerResult,
                pendingOpponentClashResult);

            MarkRunInfoCastBookCacheDirty();
            RefreshRunInfoCastBookFallbackCache();
            RequestRunInfoCastBookCacheRebuild();

            RefreshClashPowerTexts();
            LogClashResolveResult(lastClashResolveResult);
            RefreshLeftInfoTexts();
            SetMessage(lastClashResolveResult.Message);

            if (enemyTurnToPlayerDelay > 0f)
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(enemyTurnToPlayerDelay),
                    cancellationToken: this.GetCancellationTokenOnDestroy());
            }

            // 피해 적용과 결과 메시지 출력이 끝난 뒤 다음 Roll 전까지 이전 DamageText를 남기지 않는다.
            ClearClashPowerTexts();

            if (roundState.IsRoundEnded)
            {
                NotifyRoundEndIfNeeded(lastClashResolveResult);
                ForceRoundEndDiceTrayState(lastClashResolveResult);
                return;
            }

            await TryAutoStartNextAttemptAfterClashAsync();
        }

        /// <summary>플레이어 선공 이후 상대 후공 Clash 결과를 Opponent Roll AI로 생성한다.</summary>
        private async UniTask BuildOpponentClashResultAfterPlayerSubmitAsync(ClashCastResult playerResult)
        {
            SetInteractionState(BattleInteractionState.EnemyTurn);
            RefreshDiceInteractionState();

            await MovePlayerDiceSetToRestIfNeededAsync();

            if (enemyTurnStartDelay > 0f)
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(enemyTurnStartDelay),
                    cancellationToken: this.GetCancellationTokenOnDestroy());
            }

            bool built = await BuildOpponentClashResultWithRollAIAsync("OpponentSecond", playerResult);

            if (built && pendingOpponentClashResult != null)
            {
                LogOpponentClashCastResult("OpponentSecond", pendingOpponentClashResult);

                await PlayOpponentSlotPairEvaluationSequenceAsync(pendingOpponentClashResult);

                SetMessage(BuildOpponentTargetMessage(pendingOpponentClashResult));
                RefreshClashPowerTexts();

                if (enemyClashResultRevealDelay > 0f)
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(enemyClashResultRevealDelay),
                        cancellationToken: this.GetCancellationTokenOnDestroy());
                }
            }

            await MoveOpponentDiceSetToRestIfNeededAsync();
        }

        /// <summary>Round가 끝나지 않았으면 다음 Attempt를 자동으로 시작한다.</summary>
        private async UniTask TryAutoStartNextAttemptAfterClashAsync()
        {
            if (!autoStartNextAttemptAfterCast)
                return;

            if (roundState == null)
                return;

            if (roundState.IsRoundEnded)
            {
                RefreshAll("Round ended.");
                return;
            }

            if (!roundState.CurrentAttempt.IsSubmitted)
                return;

            if (autoNextAttemptDelay > 0f)
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(autoNextAttemptDelay),
                    cancellationToken: this.GetCancellationTokenOnDestroy());
            }

            if (roundState == null || roundState.IsRoundEnded)
                return;

            if (!roundState.CurrentAttempt.IsSubmitted)
                return;

            StartNextAttempt();
        }

        /// <summary>Clash 종료 결과 이벤트를 중복 없이 외부 Root로 전달한다.</summary>
        private void NotifyRoundEndIfNeeded(ClashResolveResult result)
        {
            if (result == null)
                return;

            if (roundEndNotified)
                return;

            bool isWon = result.OutcomeType == RoundOutcomeType.Won || (roundState != null && roundState.IsRoundWon);
            bool isLost = result.OutcomeType == RoundOutcomeType.Lost || (roundState != null && roundState.IsRoundLost);

            if (isWon)
            {
                roundEndNotified = true;
                RoundWon?.Invoke(result);
                return;
            }

            if (isLost)
            {
                roundEndNotified = true;
                RoundLost?.Invoke(result);
            }
        }

        /// <summary>Round 종료 후 Dice를 Tray에 고정하고 StageFlow 후속 전환을 기다리는 상태로 만든다.</summary>
        private void ForceRoundEndDiceTrayState(ClashResolveResult result)
        {
            if (roundState == null)
                return;

            SetInteractionState(BattleInteractionState.RoundEnded);
            CancelSlotPairEvaluationSequence();
            ClearSlotPairEvaluationHighlights();
            ClearLockSlotMapping();

            for (int diceIndex = 0; diceIndex < roundState.Dice.Count; diceIndex++)
                roundState.GetDice(diceIndex).SetLocked(false);

            if (diceTray3DView != null)
            {
                diceTray3DView.HideDiceSet(DiceOwnerType.Opponent);
                diceTray3DView.RestoreAllDiceToTray(roundState.GetCurrentDiceValues(), lockedDiceMoveDuration);
            }

            bool isWon = roundState.IsRoundWon;

            string message = isWon
                ? "Round won. Awaiting bounty decision."
                : "Round lost. Awaiting result decision.";

            Debug.Log(
                $"[Tessera][RoundEnd] {message} " +
                $"OpponentHP={roundState.Encounter.OpponentCurrentHP}/{roundState.Encounter.OpponentMaxHP} | " +
                $"PlayerHP={roundState.Encounter.PlayerCurrentHP}/{roundState.Encounter.PlayerMaxHP} | " +
                $"AppliedImpactDamageToOpponent={(result != null ? result.AppliedImpactDamageToOpponent.ToString() : "0")} | " +
                $"AppliedImpactDamageToPlayer={(result != null ? result.AppliedImpactDamageToPlayer.ToString() : "0")}");

            RefreshAll(message);
            ClearClashPowerTexts();
        }

        /// <summary>지정 Attempt 번호에서 사용할 EnemyIntent를 생성한다. Round Initiative는 Round 시작 시점 값으로 고정한다.</summary>
        private EnemyIntent BuildEnemyIntentForAttempt(int attemptNumber)
        {
            InitiativeOwnerType roundInitiativeOwner = ResolveCurrentRoundInitiativeOwner();

            if (currentRoundDefinition != null)
            {
                currentEnemyIntentDefinition = currentRoundDefinition.SelectIntentDefinitionForAttempt(
                    attemptNumber,
                    seed);

                EnemyIntent intent = currentRoundDefinition.BuildEnemyIntentForAttempt(
                    attemptNumber,
                    seed,
                    roundInitiativeOwner);

                Debug.Log(
                    $"[Tessera][Intent] Attempt={attemptNumber} | " +
                    $"Intent={(currentEnemyIntentDefinition != null ? currentEnemyIntentDefinition.DisplayName : "Fallback")} | " +
                    $"RoundInitiative={roundInitiativeOwner} | " +
                    $"IntentInitiative={(currentEnemyIntentDefinition != null ? currentEnemyIntentDefinition.InitiativeOwner.ToString() : "Fallback")} | " +
                    $"InitiativeLocked=True | " +
                    $"Category={intent.CategoryType} | " +
                    $"UseOpponentDevices={(currentEnemyIntentDefinition != null ? currentEnemyIntentDefinition.UseOpponentDevices.ToString() : "Fallback")} | " +
                    $"Policy={(currentEnemyIntentDefinition != null ? currentEnemyIntentDefinition.CastSelectionPolicy.ToString() : "Fallback")}");

                return intent;
            }

            currentEnemyIntentDefinition = null;

            if (roundState != null && roundState.CurrentEnemyIntent != null)
                return roundState.CurrentEnemyIntent;

            if (roundState != null)
            {
                EnemyIntentType intentType = roundInitiativeOwner == InitiativeOwnerType.Opponent
                    ? EnemyIntentType.Strike
                    : EnemyIntentType.None;

                return new EnemyIntent(
                    intentType,
                    roundState.RuleContext.EnemyStrikeDamage,
                    "Fallback intent.",
                    EnemyIntentCategoryType.Aggression,
                    roundInitiativeOwner);
            }

            return EnemyIntent.None();
        }

        /// <summary>Round 시작 시점의 Initiative를 저장한다.</summary>
        private void ResolveAndStoreRoundInitiativeOwner(EnemyIntent openingIntent,
            System.Collections.Generic.IReadOnlyList<Tessera.Data.DiceTypeDefinitionSO> equippedDiceTypes = null)
        {
            if (currentRoundDefinition != null)
            {
                lockedRoundInitiativeOwner = currentRoundDefinition.ResolveRoundInitiativeOwner();
                hasLockedRoundInitiativeOwner = true;
                return;
            }

            if (openingIntent != null)
            {
                lockedRoundInitiativeOwner = openingIntent.InitiativeOwner;
                hasLockedRoundInitiativeOwner = true;
                return;
            }

            lockedRoundInitiativeOwner = InitiativeOwnerType.Opponent;
            hasLockedRoundInitiativeOwner = true;
        }

        /// <summary>현재 Round에서 고정 사용할 Initiative를 반환한다.</summary>
        private InitiativeOwnerType ResolveCurrentRoundInitiativeOwner()
        {
            if (hasLockedRoundInitiativeOwner)
                return lockedRoundInitiativeOwner;

            if (currentRoundDefinition != null)
                return currentRoundDefinition.ResolveRoundInitiativeOwner();

            if (roundState != null && roundState.CurrentAttempt != null)
                return roundState.CurrentAttempt.InitiativeOwner;

            return InitiativeOwnerType.Opponent;
        }

        /// <summary>Round 고정 Initiative를 반영한 Opening EnemyIntent를 생성한다.</summary>
        private EnemyIntent BuildOpeningEnemyIntentWithLockedInitiative(EnemyIntent fallbackOpeningIntent)
        {
            InitiativeOwnerType roundInitiativeOwner = ResolveCurrentRoundInitiativeOwner();

            if (currentRoundDefinition != null)
                return currentRoundDefinition.BuildOpeningEnemyIntent(roundInitiativeOwner);

            if (fallbackOpeningIntent != null)
                return fallbackOpeningIntent;

            if (roundState == null)
                return EnemyIntent.None();

            EnemyIntentType intentType = roundInitiativeOwner == InitiativeOwnerType.Opponent
                ? EnemyIntentType.Strike
                : EnemyIntentType.None;

            return new EnemyIntent(
                intentType,
                roundState.RuleContext.EnemyStrikeDamage,
                "Opening intent.",
                EnemyIntentCategoryType.Aggression,
                roundInitiativeOwner);
        }

        /// <summary>Opening EnemyIntent를 현재 Attempt Initiative에 반영한다.</summary>
        private void ApplyOpeningIntentToCurrentAttempt(EnemyIntent openingIntent,
            System.Collections.Generic.IReadOnlyList<Tessera.Data.DiceTypeDefinitionSO> equippedDiceTypes = null)
        {
            if (roundState == null)
                return;

            if (roundState.CurrentAttempt == null)
                return;

            simulator.ApplyEnemyIntentToCurrentAttempt(
                roundState,
                openingIntent ?? EnemyIntent.Strike(roundState.RuleContext.EnemyStrikeDamage));
        }

        #endregion

        #region Player Roll And Input

        /// <summary>DiceCup 클릭으로 Roll 연출과 Core Roll을 순차 실행한다.</summary>
        private async UniTaskVoid PlayDiceCupRollSequenceAsync()
        {
            if (!TryCanRollUnlockedDice(out string failureMessage))
            {
                SetMessage(failureMessage);
                return;
            }

            List<int> beforeDiceValues = new List<int>(roundState.GetCurrentDiceValues());
            List<bool> lockStates = CreateDiceLockStates();

            SetInteractionState(BattleInteractionState.DiceRolling);
            RefreshButtonStates();

            bool rollSucceeded = false;

            try
            {
                SetMessage("Dice cup rolling...");

                bool rerolled = simulator.TryRerollUnlockedDice(roundState);

                if (!rerolled)
                {
                    RefreshTableHologramBattleMeta();
                    SetMessage("No rolls left.");
                    return;
                }

                rollSucceeded = true;

                // Roll 자원은 Core Roll 소모 즉시 감소시킨다. Dice 값 표시는 컵 연출 후 갱신한다.
                RefreshTableHologramBattleMeta();
                RefreshButtonStates();

                if (diceCup3DView != null && diceTray3DView != null)
                {
                    await diceTray3DView.PlayUnlockedDiceEnterCupAsync(
                        beforeDiceValues,
                        lockStates,
                        diceCup3DView.DiceEntryPosition,
                        diceCup3DView.DiceEntryRotation,
                        diceCupEnterDuration,
                        diceCupEnterStagger,
                        diceCupEnterArcHeight,
                        diceCupEnterRollEuler,
                        this.GetCancellationTokenOnDestroy());

                    await diceCup3DView.PlayLiftShakeDropAsync(
                        diceCupLiftHeight,
                        diceCupLiftDuration,
                        diceCupShakeDuration,
                        diceCupDropDuration,
                        diceCupShakeAngle,
                        diceCupShakeFrequency,
                        this.GetCancellationTokenOnDestroy());

                    List<int> afterDiceValues = new List<int>(roundState.GetCurrentDiceValues());
                    diceTray3DView.SetDiceValuesOnly(afterDiceValues, lockStates);

                    await diceTray3DView.PlayUnlockedDiceScatterFromCupAsync(
                        afterDiceValues,
                        lockStates,
                        diceCup3DView.DiceEntryPosition,
                        diceCup3DView.DiceEntryRotation,
                        diceCupScatterDuration,
                        diceCupScatterStagger,
                        diceCupScatterArcHeight,
                        diceCupScatterRollEuler,
                        this.GetCancellationTokenOnDestroy());
                }
            }
            catch (OperationCanceledException)
            {
                SetMessage("Dice cup roll canceled.");
            }
            finally
            {
                if (interactionState == BattleInteractionState.DiceRolling)
                    SetInteractionState(BattleInteractionState.PlayerIdle);

                if (rollSucceeded)
                    RefreshAll("Dice cup rolled unlocked dice.");
                else
                    RefreshButtonStates();
            }
        }

        /// <summary>DiceCup 클릭 입력을 Roll 흐름으로 연결한다.</summary>
        private void OnDiceCupClicked()
        {
            if (!useDiceCupRollInput)
                return;

            PlayDiceCupRollSequenceAsync().Forget();
        }

        /// <summary>현재 상태에서 Roll 가능한지 검사한다.</summary>
        private bool TryCanRollUnlockedDice(out string failureMessage)
        {
            failureMessage = string.Empty;

            if (externalGameplayInputLocked)
            {
                failureMessage = externalGameplayInputLockReason;
                return false;
            }

            if (interactionState == BattleInteractionState.DiceRolling)
            {
                failureMessage = "Roll is already in progress.";
                return false;
            }

            if (roundState == null)
            {
                failureMessage = "No active round.";
                return false;
            }

            if (roundState.IsRoundEnded || interactionState == BattleInteractionState.RoundEnded)
            {
                failureMessage = "Round already ended. Awaiting stage decision.";
                return false;
            }

            if (!CanActInCurrentAttempt())
            {
                failureMessage = "Cannot roll now.";
                return false;
            }

            if (roundState.RemainingBaseRollsThisAttempt <= 0 && !roundState.CanUseExtraRollThisAttempt)
            {
                failureMessage = "No rolls left for this attempt.";
                return false;
            }

            if (!roundState.IsFirstRollThisAttempt && !HasUnlockedDice())
            {
                failureMessage = "No unlocked dice.";
                return false;
            }

            return true;
        }

        /// <summary>Core Roll을 실행하고 전체 표시를 갱신한다.</summary>
        private bool RollUnlockedDiceCore(string successMessage)
        {
            if (!TryCanRollUnlockedDice(out string failureMessage))
            {
                RefreshAll(failureMessage);
                return false;
            }

            bool rerolled = simulator.TryRerollUnlockedDice(roundState);

            if (rerolled)
                RefreshAll(successMessage);
            else
                RefreshAll("No rolls left for this attempt.");

            return rerolled;
        }

        /// <summary>하나 이상 Unlock된 Dice가 있는지 확인한다.</summary>
        private bool HasUnlockedDice()
        {
            if (roundState == null || roundState.Dice == null)
                return false;

            for (int i = 0; i < roundState.Dice.Count; i++)
            {
                if (!roundState.Dice[i].IsLocked)
                    return true;
            }

            return false;
        }

        /// <summary>현재 Attempt에서 플레이어 조작이 가능한지 확인한다.</summary>
        private bool CanActInCurrentAttempt()
        {
            if (externalGameplayInputLocked)
                return false;

            if (roundState == null)
                return false;

            if (roundState.IsRoundEnded)
                return false;

            if (interactionState != BattleInteractionState.PlayerIdle)
                return false;

            return !roundState.CurrentAttempt.IsSubmitted;
        }

        /// <summary>현재 Attempt에서 Cast 선택, Popup 후보 표시, Preview 계산, 제출 준비가 가능한지 확인한다.</summary>
        private bool CanPrepareCastPreviewInCurrentAttempt()
        {
            if (!CanActInCurrentAttempt())
                return false;

            return roundState.CanSubmitCastNow();
        }

        /// <summary>DiceCup Roll 입력 가능 여부를 반환한다.</summary>
        private bool CanRollByDiceCupInput()
        {
            if (externalGameplayInputLocked)
                return false;

            if (roundState == null)
                return false;

            if (!CanActInCurrentAttempt())
                return false;

            if (roundState.RemainingBaseRollsThisAttempt <= 0 && !roundState.CanUseExtraRollThisAttempt)
                return false;

            return roundState.IsFirstRollThisAttempt || HasUnlockedDice();
        }

        /// <summary>Cast 후보 Popup 토글 가능 여부를 반환한다.</summary>
        private bool CanTogglePopup()
        {
            if (externalGameplayInputLocked)
                return false;

            if (roundState == null)
                return false;

            if (roundState.IsRoundEnded)
                return false;

            return interactionState == BattleInteractionState.PlayerIdle;
        }

        /// <summary>현재 Player Dice를 클릭/Lock할 수 있는지 확인한다.</summary>
        private bool CanInteractWithPlayerDice()
        {
            if (!CanActInCurrentAttempt())
                return false;

            return roundState.CanSubmitCastNow();
        }

        /// <summary>Presenter 상호작용 상태를 갱신한다.</summary>
        private void SetInteractionState(BattleInteractionState nextState)
        {
            interactionState = nextState;
        }

        #endregion

        #region Dice Lock And Slot Mapping

        /// <summary>지정 Dice의 Lock 상태를 토글하고, Lock 순서 기반 SlotPair 매핑을 갱신한다.</summary>
        private void ToggleDiceLock(int diceIndex)
        {
            if (!CanInteractWithPlayerDice())
            {
                SetMessage("Roll the dice before locking.");
                return;
            }

            if (diceIndex < 0 || diceIndex >= roundState.Dice.Count)
                return;

            bool willLock = !roundState.Dice[diceIndex].IsLocked;

            if (willLock)
            {
                int emptySlotIndex = FindFirstEmptyLockSlotIndex();

                if (emptySlotIndex < 0)
                {
                    SetMessage("No empty device slot.");
                    return;
                }

                simulator.SetDiceLocked(roundState, diceIndex, true);
                AssignDiceToFirstEmptyLockSlot(diceIndex);
            }
            else
            {
                simulator.SetDiceLocked(roundState, diceIndex, false);
                RemoveDiceFromLockSlot(diceIndex);
            }

            RefreshAll(willLock ? "Dice locked." : "Dice unlocked.");
        }

        /// <summary>3D LockSlot 클릭으로 해당 슬롯에 배치된 Dice를 Unlock한다.</summary>
        private void UnlockDiceFromLockSlot(int slotIndex)
        {
            if (!CanActInCurrentAttempt())
                return;

            if (slotIndex < 0 || slotIndex >= lockedDiceIndexBySlot.Length)
                return;

            int diceIndex = lockedDiceIndexBySlot[slotIndex];

            if (diceIndex < 0)
                return;

            if (diceIndex >= roundState.Dice.Count)
                return;

            // Core Lock 상태와 LockSlot 매핑을 함께 해제한다.
            simulator.SetDiceLocked(roundState, diceIndex, false);
            lockedDiceIndexBySlot[slotIndex] = -1;

            RefreshAll("Dice unlocked from lock slot.");
        }

        /// <summary>제출 직전 모든 주사위를 SlotPair 5칸에 Lock 순서 유지 + 남은 DiceIndex 오름차순으로 배치한다.</summary>
        private void EnsureAllDiceAssignedToLockSlots()
        {
            RepairInvalidLockSlotMapping();

            for (int diceIndex = 0; diceIndex < roundState.Dice.Count; diceIndex++)
            {
                if (diceIndex < 0 || diceIndex >= lockedDiceIndexBySlot.Length)
                    continue;

                if (FindLockSlotIndexByDiceIndex(diceIndex) >= 0)
                    continue;

                int emptySlotIndex = FindFirstEmptyLockSlotIndex();

                if (emptySlotIndex < 0)
                    break;

                lockedDiceIndexBySlot[emptySlotIndex] = diceIndex;
            }

            for (int slotIndex = 0; slotIndex < lockedDiceIndexBySlot.Length; slotIndex++)
            {
                int diceIndex = lockedDiceIndexBySlot[slotIndex];

                if (diceIndex < 0 || diceIndex >= roundState.Dice.Count)
                    continue;

                simulator.SetDiceLocked(roundState, diceIndex, true);
            }

            RefreshDiceViews();
        }

        /// <summary>Lock 슬롯 매핑을 모두 비운다.</summary>
        private void ClearLockSlotMapping()
        {
            for (int i = 0; i < lockedDiceIndexBySlot.Length; i++)
                lockedDiceIndexBySlot[i] = -1;
        }

        /// <summary>주사위를 첫 번째 빈 SlotPair 슬롯에 배치한다.</summary>
        private void AssignDiceToFirstEmptyLockSlot(int diceIndex)
        {
            if (diceIndex < 0 || diceIndex >= lockedDiceIndexBySlot.Length)
                return;

            if (FindLockSlotIndexByDiceIndex(diceIndex) >= 0)
                return;

            int emptySlotIndex = FindFirstEmptyLockSlotIndex();

            if (emptySlotIndex < 0)
                return;

            lockedDiceIndexBySlot[emptySlotIndex] = diceIndex;
        }

        /// <summary>지정 주사위의 SlotPair 매핑을 해제한다.</summary>
        private void RemoveDiceFromLockSlot(int diceIndex)
        {
            if (diceIndex >= 0 && diceIndex < lockedDiceIndexBySlot.Length)
            {
                if (lockedDiceIndexBySlot[diceIndex] == diceIndex)
                {
                    lockedDiceIndexBySlot[diceIndex] = -1;
                    return;
                }
            }

            int slotIndex = FindLockSlotIndexByDiceIndex(diceIndex);

            if (slotIndex < 0)
                return;

            // 예전 매핑이 남아 있는 경우까지 정리한다.
            lockedDiceIndexBySlot[slotIndex] = -1;
        }

        /// <summary>지정한 주사위가 배치된 Lock 슬롯 index를 찾는다.</summary>
        private int FindLockSlotIndexByDiceIndex(int diceIndex)
        {
            for (int i = 0; i < lockedDiceIndexBySlot.Length; i++)
            {
                if (lockedDiceIndexBySlot[i] == diceIndex)
                    return i;
            }

            return -1;
        }

        /// <summary>첫 번째 빈 Lock 슬롯 index를 찾는다.</summary>
        private int FindFirstEmptyLockSlotIndex()
        {
            for (int i = 0; i < lockedDiceIndexBySlot.Length; i++)
            {
                if (lockedDiceIndexBySlot[i] < 0)
                    return i;
            }

            return -1;
        }

        /// <summary>Core Lock 상태와 SlotPair DiceIndex 매핑이 어긋난 경우를 정리하되, 기존 Lock 순서는 보존한다.</summary>
        private void RepairInvalidLockSlotMapping()
        {
            if (roundState == null)
            {
                ClearLockSlotMapping();
                return;
            }

            bool[] usedDiceIndexes = new bool[lockedDiceIndexBySlot.Length];

            for (int slotIndex = 0; slotIndex < lockedDiceIndexBySlot.Length; slotIndex++)
            {
                int diceIndex = lockedDiceIndexBySlot[slotIndex];

                if (diceIndex < 0 || diceIndex >= roundState.Dice.Count)
                {
                    lockedDiceIndexBySlot[slotIndex] = -1;
                    continue;
                }

                if (!roundState.Dice[diceIndex].IsLocked)
                {
                    lockedDiceIndexBySlot[slotIndex] = -1;
                    continue;
                }

                if (diceIndex < usedDiceIndexes.Length && usedDiceIndexes[diceIndex])
                {
                    lockedDiceIndexBySlot[slotIndex] = -1;
                    continue;
                }

                if (diceIndex < usedDiceIndexes.Length)
                    usedDiceIndexes[diceIndex] = true;
            }

            for (int diceIndex = 0; diceIndex < roundState.Dice.Count; diceIndex++)
            {
                if (!roundState.Dice[diceIndex].IsLocked)
                    continue;

                if (diceIndex >= usedDiceIndexes.Length)
                    continue;

                if (usedDiceIndexes[diceIndex])
                    continue;

                int emptySlotIndex = FindFirstEmptyLockSlotIndex();

                if (emptySlotIndex < 0)
                    break;

                lockedDiceIndexBySlot[emptySlotIndex] = diceIndex;
                usedDiceIndexes[diceIndex] = true;
            }
        }

        /// <summary>현재 LockSlot 매핑을 리스트로 반환한다.</summary>
        private List<int> CreateLockSlotDiceIndexList()
        {
            List<int> result = new List<int>(lockedDiceIndexBySlot.Length);

            for (int i = 0; i < lockedDiceIndexBySlot.Length; i++)
                result.Add(lockedDiceIndexBySlot[i]);

            return result;
        }

        /// <summary>미리보기용 SlotPair DiceIndex 매핑을 반환한다.</summary>
        private List<int> CreatePreviewResolvedLockSlotDiceIndexList()
        {
            List<int> result = new List<int>(lockedDiceIndexBySlot.Length);
            bool[] usedDiceIndexes = new bool[lockedDiceIndexBySlot.Length];

            for (int slotIndex = 0; slotIndex < lockedDiceIndexBySlot.Length; slotIndex++)
            {
                int diceIndex = lockedDiceIndexBySlot[slotIndex];

                if (diceIndex >= 0 && diceIndex < lockedDiceIndexBySlot.Length)
                {
                    result.Add(diceIndex);
                    usedDiceIndexes[diceIndex] = true;
                }
                else
                {
                    result.Add(-1);
                }
            }

            for (int slotIndex = 0; slotIndex < result.Count; slotIndex++)
            {
                if (result[slotIndex] >= 0)
                    continue;

                for (int diceIndex = 0; diceIndex < lockedDiceIndexBySlot.Length; diceIndex++)
                {
                    if (usedDiceIndexes[diceIndex])
                        continue;

                    result[slotIndex] = diceIndex;
                    usedDiceIndexes[diceIndex] = true;
                    break;
                }
            }

            return result;
        }

        /// <summary>리스트가 지정 주사위 인덱스를 포함하는지 확인한다.</summary>
        private static bool ContainsDiceIndex(IReadOnlyList<int> diceIndexes, int diceIndex)
        {
            for (int i = 0; i < diceIndexes.Count; i++)
            {
                if (diceIndexes[i] == diceIndex)
                    return true;
            }

            return false;
        }

        /// <summary>리스트에서 첫 번째 빈 슬롯 인덱스를 찾는다.</summary>
        private static int FindFirstEmptyIndex(IReadOnlyList<int> diceIndexes)
        {
            for (int i = 0; i < diceIndexes.Count; i++)
            {
                if (diceIndexes[i] < 0)
                    return i;
            }

            return -1;
        }

        /// <summary>현재 주사위들의 Lock 상태 목록을 생성한다.</summary>
        private List<bool> CreateDiceLockStates()
        {
            List<bool> lockStates = new List<bool>(roundState.Dice.Count);

            for (int i = 0; i < roundState.Dice.Count; i++)
                lockStates.Add(roundState.Dice[i].IsLocked);

            return lockStates;
        }

        /// <summary>기억 중인 직전 Cast 주사위 값을 초기화한다.</summary>
        private void ClearLastSubmittedDiceValues()
        {
            lastSubmittedDiceValues.Clear();
            hasLastSubmittedDiceValues = false;
        }

        /// <summary>새 Attempt의 Dice 값을 직전 Cast 주사위 값으로 맞춘다.</summary>
        private bool ApplyLastSubmittedDiceValuesToNewAttemptIfPossible()
        {
            if (!hasLastSubmittedDiceValues)
                return false;

            if (roundState == null)
                return false;

            if (lastSubmittedDiceValues.Count != roundState.Dice.Count)
                return false;

            simulator.SetCurrentDiceValuesForTest(roundState, lastSubmittedDiceValues);
            return true;
        }

        #endregion

        #region Device Data

        /// <summary>3D Device 슬롯 표시 인덱스와 상호작용 정책을 초기화한다.</summary>
        private void InitializeDeviceSlots()
        {
            if (playerDeviceRack3DView != null)
            {
                playerDeviceRack3DView.SetInteractionEnabled(true);
                playerDeviceRack3DView.InitializeSlots();
            }

            if (opponentDeviceRack3DView != null)
            {
                opponentDeviceRack3DView.SetInteractionEnabled(false);
                opponentDeviceRack3DView.InitializeSlots();
            }
        }

        /// <summary>RunSession의 장착 Device 배열을 Presenter 내부 배열로 복사한다.</summary>
        private void SyncDevicesFromRunSession()
        {
            if (runSession == null)
                return;

            EnsureSlotPairDeviceArraySize();

            for (int i = 0; i < slotPairDevices.Length; i++)
            {
                slotPairDevices[i] = i < runSession.EquippedSlotPairDevices.Count
                    ? runSession.EquippedSlotPairDevices[i]
                    : null;
            }
        }

        /// <summary>현재 Round에서 사용할 상대 SlotPair Device 배열을 설정한다.</summary>
        private void SetOpponentSlotPairDevices(SlotPairDeviceDefinitionSO[] devices)
        {
            if (opponentSlotPairDevices == null ||
                opponentSlotPairDevices.Length != SlotPairDamageCalculator.SlotPairCount)
                opponentSlotPairDevices = new SlotPairDeviceDefinitionSO[SlotPairDamageCalculator.SlotPairCount];

            for (int i = 0; i < opponentSlotPairDevices.Length; i++)
            {
                opponentSlotPairDevices[i] = devices != null && i < devices.Length
                    ? devices[i]
                    : null;
            }
        }

        /// <summary>slotPairDevices 배열 크기를 SlotPairCount 기준으로 보정한다.</summary>
        private void EnsureSlotPairDeviceArraySize()
        {
            if (slotPairDevices != null && slotPairDevices.Length == SlotPairDamageCalculator.SlotPairCount)
                return;

            SlotPairDeviceDefinitionSO[] resizedDevices = new SlotPairDeviceDefinitionSO[SlotPairDamageCalculator.SlotPairCount];

            if (slotPairDevices != null)
            {
                int copyCount = Mathf.Min(slotPairDevices.Length, resizedDevices.Length);

                for (int i = 0; i < copyCount; i++)
                    resizedDevices[i] = slotPairDevices[i];
            }

            slotPairDevices = resizedDevices;
        }

        /// <summary>Device 배열 길이를 지정 길이로 보정한다.</summary>
        private static SlotPairDeviceDefinitionSO[] ResizeDeviceArray(
            SlotPairDeviceDefinitionSO[] source,
            int targetLength)
        {
            SlotPairDeviceDefinitionSO[] resized = new SlotPairDeviceDefinitionSO[targetLength];

            if (source == null)
                return resized;

            int copyCount = Mathf.Min(source.Length, targetLength);

            for (int i = 0; i < copyCount; i++)
                resized[i] = source[i];

            return resized;
        }

        /// <summary>Transform 배열 길이를 지정 길이로 보정한다.</summary>
        private static Transform[] ResizeTransformArray(
            Transform[] source,
            int targetLength)
        {
            Transform[] resized = new Transform[targetLength];

            if (source == null)
                return resized;

            int copyCount = Mathf.Min(source.Length, targetLength);

            for (int i = 0; i < copyCount; i++)
                resized[i] = source[i];

            return resized;
        }

        /// <summary>플레이어 장착 SlotPair Device SO를 Core 계산용 정의 리스트로 변환한다.</summary>
        private List<SlotPairDeviceDefinition> CreatePlayerDeviceDefinitions()
        {
            List<SlotPairDeviceDefinition> devices = new List<SlotPairDeviceDefinition>(SlotPairDamageCalculator.SlotPairCount);

            for (int i = 0; i < SlotPairDamageCalculator.SlotPairCount; i++)
                devices.Add(CreateDeviceDefinitionFromSlot(i));

            return devices;
        }

        /// <summary>상대 장착 SlotPair Device SO를 Core 계산용 정의 리스트로 변환한다.</summary>
        private List<SlotPairDeviceDefinition> CreateOpponentDeviceDefinitions()
        {
            List<SlotPairDeviceDefinition> devices = new List<SlotPairDeviceDefinition>(SlotPairDamageCalculator.SlotPairCount);

            for (int i = 0; i < SlotPairDamageCalculator.SlotPairCount; i++)
                devices.Add(CreateOpponentDeviceDefinitionFromSlot(i));

            return devices;
        }

        /// <summary>지정 상대 슬롯에 장착된 Device SO를 Core 계산용 정의로 변환한다.</summary>
        private SlotPairDeviceDefinition CreateOpponentDeviceDefinitionFromSlot(int index)
        {
            if (opponentSlotPairDevices == null)
                return SlotPairDeviceDefinition.None();

            if (index < 0 || index >= opponentSlotPairDevices.Length)
                return SlotPairDeviceDefinition.None();

            if (opponentSlotPairDevices[index] == null)
                return SlotPairDeviceDefinition.None();

            return opponentSlotPairDevices[index].ToCoreDefinition();
        }

        /// <summary>지정 슬롯에 장착된 Device SO를 Core 계산용 정의로 변환한다.</summary>
        private SlotPairDeviceDefinition CreateDeviceDefinitionFromSlot(int index)
        {
            if (slotPairDevices == null)
                return SlotPairDeviceDefinition.None();

            if (index < 0 || index >= slotPairDevices.Length)
                return SlotPairDeviceDefinition.None();

            if (slotPairDevices[index] == null)
                return SlotPairDeviceDefinition.None();

            return slotPairDevices[index].ToCoreDefinition();
        }

        /// <summary>지정 슬롯의 SlotPairDeviceDefinitionSO를 안전하게 반환한다.</summary>
        private SlotPairDeviceDefinitionSO GetSlotPairDeviceOrNull(int index)
        {
            if (slotPairDevices == null)
                return null;

            if (index < 0 || index >= slotPairDevices.Length)
                return null;

            return slotPairDevices[index];
        }

        /// <summary>지정 SlotPair 인덱스의 Device 표시 이름을 반환한다.</summary>
        private string GetSlotPairDeviceDisplayName(int slotIndex)
        {
            return GetSlotPairDeviceDisplayName(DiceOwnerType.Player, slotIndex);
        }

        /// <summary>지정 소유자의 SlotPair 인덱스 Device 표시 이름을 반환한다.</summary>
        private string GetSlotPairDeviceDisplayName(DiceOwnerType owner, int slotIndex)
        {
            SlotPairDeviceDefinitionSO[] devices = owner == DiceOwnerType.Opponent
                ? opponentSlotPairDevices
                : slotPairDevices;

            if (devices == null)
                return "No Device";

            if (slotIndex < 0 || slotIndex >= devices.Length)
                return "No Device";

            SlotPairDeviceDefinitionSO device = devices[slotIndex];

            if (device == null)
                return "No Device";

            if (string.IsNullOrWhiteSpace(device.DisplayName))
                return device.DeviceType.ToString();

            return device.DisplayName;
        }

        /// <summary>현재 표시할 Money 값을 반환한다.</summary>
        private int GetCurrentMoney()
        {
            if (runSession != null)
                return runSession.Money;

            return earnedMoney;
        }

        #endregion

        #region View Refresh

        /// <summary>전체 UI 표시를 현재 Round 상태 기준으로 다시 그린다.</summary>
        private void RefreshAll(string optionalMessage)
        {
            if (roundState == null) return;

            currentCastBoardViewModel = castBoardModelBuilder.Build(roundState);

            if (CanPrepareCastPreviewInCurrentAttempt())
            {
                if (selectedPatternType == RollPatternType.None)
                    selectedPatternType = currentCastBoardViewModel.RecommendedPatternType;

                if (!CanSelectedCastStillSubmit())
                    selectedPatternType = currentCastBoardViewModel.RecommendedPatternType;

                BuildCurrentSlotPairPreview();
            }
            else if (CanActInCurrentAttempt())
            {
                selectedPatternType = RollPatternType.None;
                currentSlotPairPreview = null;
                currentPreviewTableRuleResult = null;
                ClearTableHologramCastPreview();
            }

            RefreshLeftInfoTexts();
            RefreshDiceViews();
            RefreshDeviceSlotViews();
            RefreshCastPopup();
            RefreshButtonStates();

            if (!string.IsNullOrWhiteSpace(optionalMessage))
                SetMessage(optionalMessage);
        }

        /// <summary>선택된 Cast 기준 SlotPair 피해 미리보기를 계산한다.</summary>
        private void BuildCurrentSlotPairPreview()
        {
            if (roundState == null || roundState.IsRoundEnded || roundState.CurrentAttempt.IsSubmitted)
                return;

            if (!roundState.CanSubmitCastNow())
                return;

            currentSlotPairPreview = null;
            currentPreviewPatternResult = null;
            currentPreviewTableRuleResult = null;
            isSlotPairResultPresentationComplete = false;

            if (selectedPatternType == RollPatternType.None)
                return;

            List<int> previewLockSlots = CreatePreviewResolvedLockSlotDiceIndexList();

            simulator.TryBuildSlotPairDamagePreview(
                roundState,
                selectedPatternType,
                previewLockSlots,
                CreatePlayerDeviceDefinitions(),
                out PatternResult patternResult,
                out currentSlotPairPreview,
                out currentPreviewTableRuleResult);

            currentPreviewPatternResult = patternResult;
        }

        /// <summary>좌측 전투 정보 텍스트를 갱신한다.</summary>
        private void RefreshLeftInfoTexts()
        {
            if (roundState == null)
                return;

            SetText(roundTitleText, currentRoundDisplayName);
            SetText(opponentHPText, $"Opponent HP {roundState.Encounter.OpponentCurrentHP}/{roundState.Encounter.OpponentMaxHP}");

            if (runSession != null && interactionState == BattleInteractionState.RoundEnded)
                SetText(playerHPText, $"Player HP {runSession.PlayerCurrentHP}/{runSession.PlayerMaxHP}");
            else
                SetText(playerHPText, $"Player HP {roundState.Encounter.PlayerCurrentHP}/{roundState.Encounter.PlayerMaxHP}");

            RefreshTableHologramBattleMeta();
            RefreshSelectedCastTexts();
            RefreshClashPowerTexts();
        }

        private void RefreshMoneyOverlay()
        {

        }

        /// <summary>선택 Cast와 SlotPair 계산 미리보기 표시를 갱신한다.</summary>
        private void RefreshSelectedCastTexts()
        {
            if (selectedPatternType == RollPatternType.None || currentSlotPairPreview == null)
            {
                ClearTableHologramCastPreview();
                return;
            }

            string castName = CastBoardCatalog.GetDisplayName(selectedPatternType);
            int displayScore = isSlotPairResultPresentationComplete
                ? currentSlotPairPreview.FinalScore
                : currentSlotPairPreview.InitialScore;

            string forceValue = isSlotPairResultPresentationComplete
                ? currentSlotPairPreview.FormatFinalForce()
                : FormatForce(currentSlotPairPreview.BaseForce);

            RefreshTableHologramCastPreview(
                castName,
                displayScore,
                forceValue);
        }

        /// <summary>현재 Clash에서 비교할 플레이어/상대 피해 수치를 Hologram에 갱신한다.</summary>
        private void RefreshClashPowerTexts()
        {
            int playerCastPower = 0;
            int opponentCastPower = 0;

            if (lastClashResolveResult != null)
            {
                playerCastPower = lastClashResolveResult.PlayerResult != null
                    ? lastClashResolveResult.PlayerResult.CastPower
                    : 0;

                opponentCastPower = lastClashResolveResult.OpponentResult != null
                    ? lastClashResolveResult.OpponentResult.CastPower
                    : 0;
            }
            else
            {
                playerCastPower = currentPreviewTableRuleResult != null
                    ? ResolveDisplayedPlayerCastPower()
                    : 0;

                opponentCastPower = pendingOpponentClashResult != null
                    ? pendingOpponentClashResult.CastPower
                    : 0;
            }

            if (useTableHologramView && tableHologramView != null)
                tableHologramView.RefreshClashPower(playerCastPower, opponentCastPower);
        }

        /// <summary>현재 UI 단계에서 표시해야 하는 플레이어 CastPower 값을 반환한다.</summary>
        private int ResolveDisplayedPlayerCastPower()
        {
            if (isSlotPairResultPresentationComplete || currentSlotPairPreview == null)
                return Mathf.Max(0, currentPreviewTableRuleResult.ModifiedCastPower);

            return CalculateTableRuleAppliedCastPower(
                currentSlotPairPreview.InitialScore,
                currentSlotPairPreview.BaseForce,
                0);
        }

        /// <summary>지정 Score / Force / 추가 TruePower 누적값에 테이블 규칙까지 반영한 CastPower를 계산한다.</summary>
        private int CalculateTableRuleAppliedCastPower(int score, float force, int accumulatedTruePower)
        {
            if (currentPreviewPatternResult == null || roundState == null)
                return 0;

            int truePower = currentPreviewPatternResult.TruePower + accumulatedTruePower;
            int castPowerBeforeRules = Mathf.FloorToInt(score * force) + truePower;
            TableRuleEvaluationResult tableRuleResult = TableRuleEvaluator.Evaluate(
                roundState.RuleContext,
                currentPreviewPatternResult.PatternType,
                castPowerBeforeRules);

            return Mathf.Max(0, tableRuleResult.ModifiedCastPower);
        }

        /// <summary>Clash 피해 비교 표시를 기본값으로 초기화한다.</summary>
        private void ClearClashPowerTexts()
        {
            if (useTableHologramView && tableHologramView != null)
                tableHologramView.ClearClashPower();
        }

        /// <summary>테이블 홀로그램의 제한 자원 정보를 갱신한다.</summary>
        private void RefreshTableHologramBattleMeta()
        {
            if (!useTableHologramView) return;
            if (tableHologramView == null) return;
            if (roundState == null) return;

            // Attempt는 현재 번호가 아니라 남은 시도 횟수로 표시한다.
            int remainingAttempts = CalculateRemainingAttempts();

            tableHologramView.RefreshBattleMeta(
                remainingAttempts,
                roundState.RuleContext.MaxAttempts,
                roundState.RemainingRollsThisAttempt,
                roundState.MaxRollsThisAttempt,
                roundState.Overcharge.CurrentOvercharge);
        }

        /// <summary>현재 Round에서 남은 Attempt 횟수를 계산한다.</summary>
        private int CalculateRemainingAttempts()
        {
            if (roundState == null)
                return 0;

            if (roundState.CurrentAttempt == null)
                return 0;

            int remainingAttempts = roundState.RuleContext.MaxAttempts - roundState.CurrentAttempt.AttemptNumber + 1;

            if (roundState.CurrentAttempt.IsSubmitted)
                remainingAttempts--;

            return Mathf.Max(remainingAttempts, 0);
        }

        /// <summary>테이블 홀로그램의 Cast 미리보기를 비운다.</summary>
        private void ClearTableHologramCastPreview()
        {
            if (!useTableHologramView) return;
            if (tableHologramView == null) return;

            // 선택 Cast가 없으면 Hologram의 Cast/Score/Force만 기본값으로 되돌린다.
            tableHologramView.ClearCastPreview();
        }

        /// <summary>테이블 홀로그램의 Cast / Score / Force 미리보기를 갱신한다.</summary>
        private void RefreshTableHologramCastPreview(string castName, int score, string forceValue)
        {
            if (!useTableHologramView) return;
            if (tableHologramView == null) return;

            // 선택된 Cast의 핵심 계산값만 Hologram에 표시한다.
            tableHologramView.RefreshCastPreview(castName, score, forceValue);
        }

        /// <summary>테이블 홀로그램에 현재 SlotPair 계산 단계를 표시한다.</summary>
        private async UniTask RefreshTableHologramSlotPairStepAsync(
            ClashCastResult result,
            SlotPairDamageStep step,
            CancellationToken cancellationToken)
        {
            if (!useTableHologramView) return;
            if (tableHologramView == null) return;
            if (result == null || step == null) return;

            bool didScoreChange = step.ScoreAfter != step.ScoreBefore;
            bool didForceChange = Mathf.Abs(step.ForceAfter - step.ForceBefore) > 0.001f;

            if (!didScoreChange && !didForceChange)
                return;

            await tableHologramView.RefreshSlotPairStepAsync(
                step.SlotIndex,
                SlotPairDamageCalculator.SlotPairCount,
                CastBoardCatalog.GetDisplayName(result.PatternResult.PatternType),
                step.ScoreAfter,
                FormatForce(step.ForceAfter),
                didScoreChange,
                didForceChange,
                cancellationToken);

            tableHologramView.RefreshClashPower(
                CalculateTableRuleAppliedCastPower(step.ScoreAfter, step.ForceAfter, step.TruePowerAfter),
                pendingOpponentClashResult != null ? pendingOpponentClashResult.CastPower : 0);
        }

        /// <summary>3D Tray 주사위와 Lock 표시를 갱신한다.</summary>
        private void RefreshDiceViews()
        {
            IReadOnlyList<int> diceValues = roundState.GetCurrentDiceValues();

            if (diceTray3DView == null)
                return;

            IReadOnlyList<bool> lockStates = CreateDiceLockStates();

            diceTray3DView.SetDiceForDeviceSlotLockPresentation(
                diceValues,
                lockStates,
                lockedDiceMoveDuration);

            RefreshDeviceSlotLockedDicePresentation(false);
        }

        /// <summary>3D Device Rack을 현재 Player/Opponent Device 배열 기준으로 갱신한다.</summary>
        private void RefreshDeviceSlotViews()
        {
            if (playerDeviceRack3DView != null)
                playerDeviceRack3DView.SetDevices(slotPairDevices);

            if (opponentDeviceRack3DView != null)
                opponentDeviceRack3DView.SetDevices(opponentSlotPairDevices);

            RefreshDeviceSlotLockedDicePresentation();
        }

        /// <summary>Lock된 DiceView를 Lock 순서에 따라 PlayerDeviceSlot 하단 위치에 배치한다.</summary>
        private void RefreshDeviceSlotLockedDicePresentation(bool forceMoveAllLockedDice = false)
        {
            if (roundState == null) return;
            if (diceTray3DView == null) return;

            RepairInvalidLockSlotMapping();

            for (int slotIndex = 0; slotIndex < lockedDiceIndexBySlot.Length; slotIndex++)
            {
                int diceIndex = lockedDiceIndexBySlot[slotIndex];

                if (diceIndex < 0 || diceIndex >= roundState.Dice.Count)
                    continue;

                if (!roundState.Dice[diceIndex].IsLocked)
                    continue;

                if (!TryGetLockedDiceDeviceSlotPose(
                        slotIndex,
                        out Vector3 targetPosition,
                        out Quaternion targetRotation))
                    continue;

                if (!forceMoveAllLockedDice &&
                    diceTray3DView.IsDiceNearPosition(diceIndex, targetPosition, 0.02f))
                {
                    continue;
                }

                diceTray3DView.MoveDiceToLockedDeviceSlot(
                    diceIndex,
                    targetPosition,
                    targetRotation,
                    lockedDiceMoveDuration);
            }
        }

        /// <summary>Player Lock Dice Anchor 기준 표시 위치와 회전을 계산한다.</summary>
        private bool TryGetLockedDiceDeviceSlotPose(
            int slotIndex,
            out Vector3 worldPosition,
            out Quaternion worldRotation)
        {
            return TryGetLockedDiceSlotAnchorPose(DiceOwnerType.Player, slotIndex, out worldPosition, out worldRotation);
        }

        /// <summary>지정 소유자의 Lock Dice Anchor 기준 표시 위치와 회전을 계산한다.</summary>
        private bool TryGetLockedDiceSlotAnchorPose(
            DiceOwnerType owner,
            int slotIndex,
            out Vector3 worldPosition,
            out Quaternion worldRotation)
        {
            worldPosition = Vector3.zero;
            worldRotation = Quaternion.identity;

            Transform[] anchors = ResolveLockedDiceSlotAnchors(owner);

            if (anchors == null)
                return false;

            if (slotIndex < 0 || slotIndex >= anchors.Length)
                return false;

            if (anchors[slotIndex] == null)
                return false;

            worldPosition = anchors[slotIndex].position + new Vector3(0f, 0.025f, 0f);
            worldRotation = anchors[slotIndex].rotation;
            return true;
        }

        /// <summary>지정 소유자의 Lock Dice Anchor 배열을 반환한다.</summary>
        private Transform[] ResolveLockedDiceSlotAnchors(DiceOwnerType owner)
        {
            return owner == DiceOwnerType.Opponent
                ? opponentLockedDiceSlotAnchors
                : playerLockedDiceSlotAnchors;
        }

        /// <summary>Cast 후보 Popup을 갱신한다.</summary>
        private void RefreshCastPopup()
        {
            if (castCandidatePopupView == null)
                return;

            if (externalGameplayInputLocked)
            {
                castCandidatePopupView.SetPopupVisible(false);
                return;
            }

            castCandidatePopupView.SetPopupVisible(showCastCandidatePopup);

            if (!showCastCandidatePopup)
                return;

            if (!CanPrepareCastPreviewInCurrentAttempt())
            {
                castCandidatePopupView.Refresh(
                    null,
                    RollPatternType.None,
                    RollPatternType.None,
                    null);
                return;
            }

            castCandidatePopupView.Refresh(
                currentCastBoardViewModel,
                selectedPatternType,
                currentCastBoardViewModel.RecommendedPatternType,
                SelectCastCandidate,
                roundState != null ? roundState.Encounter.OpponentCurrentHP : 0,
                pendingOpponentClashResult != null ? pendingOpponentClashResult.CastPower : 0);
        }

        /// <summary>버튼 상호작용 가능 상태를 갱신한다.</summary>
        private void RefreshButtonStates()
        {
            bool hasRound = roundState != null;
            bool canPrepareCast = CanPrepareCastPreviewInCurrentAttempt();

            SetButtonInteractable(
                submitSelectedButton,
                canPrepareCast && selectedPatternType != RollPatternType.None);

            SetButtonInteractable(
                togglePopupButton,
                hasRound && CanTogglePopup());

            if (diceCup3DView != null)
                diceCup3DView.SetInteractable(useDiceCupRollInput && CanRollByDiceCupInput());

            RefreshDiceInteractionState();
        }

        /// <summary>현재 턴/Attempt 상태에 맞춰 Dice 클릭과 Hover 강조 가능 여부를 갱신한다.</summary>
        private void RefreshDiceInteractionState()
        {
            if (diceTray3DView == null)
                return;

            bool canInteractWithPlayerDice = CanInteractWithPlayerDice();

            diceTray3DView.SetDiceInteractionEnabled(
                DiceOwnerType.Player,
                canInteractWithPlayerDice,
                canInteractWithPlayerDice);

            diceTray3DView.SetDiceInteractionEnabled(
                DiceOwnerType.Opponent,
                false,
                false);
        }

        /// <summary>현재 선택된 Cast가 아직 제출 가능한 후보인지 확인한다.</summary>
        private bool CanSelectedCastStillSubmit()
        {
            if (currentCastBoardViewModel == null)
                return false;

            if (selectedPatternType == RollPatternType.None)
                return false;

            return currentCastBoardViewModel.TryGetEntry(selectedPatternType, out CastBoardEntryModel entry)
                    && entry.Status == CastBoardEntryStatus.Available;
        }

        /// <summary>Popup 후보 클릭으로 제출할 Cast를 선택한다.</summary>
        private void SelectCastCandidate(RollPatternType patternType)
        {
            if (!CanActInCurrentAttempt())
                return;

            selectedPatternType = patternType;
            RefreshAll(null);
        }

        #endregion

        #region Dice Presentation

        /// <summary>3D DiceView 클릭 콜백을 초기화한다.</summary>
        private void InitializeDiceSlots()
        {
            if (diceTray3DView != null)
                diceTray3DView.Initialize(ToggleDiceLock);
        }

        /// <summary>플레이어 Dice 세트를 RestPoint로 회수한다.</summary>
        private async UniTask MovePlayerDiceSetToRestIfNeededAsync()
        {
            if (diceTray3DView == null)
                return;

            if (!diceTray3DView.HasDiceSet(DiceOwnerType.Player))
                return;

            await diceTray3DView.MoveDiceSetToRestAsync(
                DiceOwnerType.Player,
                ownerDiceRestMoveDuration,
                this.GetCancellationTokenOnDestroy());
        }

        /// <summary>상대 Dice 세트를 RestPoint로 회수한다.</summary>
        private async UniTask MoveOpponentDiceSetToRestIfNeededAsync()
        {
            if (diceTray3DView == null)
                return;

            if (!diceTray3DView.HasDiceSet(DiceOwnerType.Opponent))
                return;

            await diceTray3DView.MoveDiceSetToRestAsync(
                DiceOwnerType.Opponent,
                ownerDiceRestMoveDuration,
                this.GetCancellationTokenOnDestroy());
        }

        /// <summary>Round 시작 시 Player Dice 표시 상태를 초기화한다.</summary>
        private async UniTask PreparePlayerFirstRoundDicePresentationAsync(bool moveOpponentDiceToRest = true)
        {
            if (diceTray3DView == null)
                return;

            if (roundState == null)
                return;

            IReadOnlyList<int> diceValues = roundState.GetCurrentDiceValues();
            List<bool> lockStates = CreateDiceLockStates();

            if (moveOpponentDiceToRest && diceTray3DView.HasDiceSet(DiceOwnerType.Opponent))
            {
                await diceTray3DView.MoveDiceSetToRestAsync(
                    DiceOwnerType.Opponent,
                    ownerDiceRestMoveDuration,
                    this.GetCancellationTokenOnDestroy());
            }

            if (diceTray3DView.HasDiceSet(DiceOwnerType.Player))
            {
                diceTray3DView.SetDiceValuesOnly(DiceOwnerType.Player, diceValues, lockStates);

                await diceTray3DView.MoveDiceSetToTrayAsync(
                    DiceOwnerType.Player,
                    diceValues,
                    lockStates,
                    ownerDiceRestMoveDuration,
                    this.GetCancellationTokenOnDestroy());
            }

            RefreshDiceInteractionState();
        }

        /// <summary>상대 턴에 공용 DiceCup과 Opponent Dice 세트를 이용한 빠른 주사위 연출을 재생한다.</summary>
        private async UniTask PlayEnemyDiceCupPresentationAsync(IReadOnlyList<int> enemyDiceValues, IReadOnlyList<bool> lockStates)
        {
            if (!playEnemyDiceCupPresentation)
                return;

            if (diceCup3DView == null)
                return;

            if (diceTray3DView == null || !diceTray3DView.HasDiceSet(DiceOwnerType.Opponent))
            {
                await diceCup3DView.PlayLiftShakeDropAsync(
                    diceCupLiftHeight * 0.65f,
                    diceCupLiftDuration * 0.75f,
                    diceCupShakeDuration * 0.55f,
                    diceCupDropDuration * 0.75f,
                    diceCupShakeAngle * 0.75f,
                    diceCupShakeFrequency,
                    this.GetCancellationTokenOnDestroy());

                return;
            }

            diceTray3DView.SetDiceValuesOnly(DiceOwnerType.Opponent, enemyDiceValues, lockStates);

            await diceTray3DView.MoveDiceSetToTrayAsync(
                DiceOwnerType.Opponent,
                enemyDiceValues,
                lockStates,
                ownerDiceRestMoveDuration,
                this.GetCancellationTokenOnDestroy());

            await diceTray3DView.PlayUnlockedDiceEnterCupAsync(
                DiceOwnerType.Opponent,
                enemyDiceValues,
                lockStates,
                diceCup3DView.DiceEntryPosition,
                diceCup3DView.DiceEntryRotation,
                diceCupEnterDuration * 0.75f,
                diceCupEnterStagger * 0.75f,
                diceCupEnterArcHeight,
                enemyDiceCupEnterRollEuler,
                this.GetCancellationTokenOnDestroy());

            await diceCup3DView.PlayLiftShakeDropAsync(
                diceCupLiftHeight * 0.65f,
                diceCupLiftDuration * 0.75f,
                diceCupShakeDuration * 0.55f,
                diceCupDropDuration * 0.75f,
                diceCupShakeAngle * 0.75f,
                diceCupShakeFrequency,
                this.GetCancellationTokenOnDestroy());

            await diceTray3DView.PlayUnlockedDiceScatterFromCupAsync(
                DiceOwnerType.Opponent,
                enemyDiceValues,
                lockStates,
                diceCup3DView.DiceEntryPosition,
                diceCup3DView.DiceEntryRotation,
                diceCupScatterDuration * 0.75f,
                diceCupScatterStagger * 0.75f,
                diceCupScatterArcHeight,
                enemyDiceCupScatterRollEuler,
                this.GetCancellationTokenOnDestroy());
        }

        /// <summary>상대가 Lock한 DiceView를 빈 Opponent DeviceSlot 순서대로 이동시킨다.</summary>
        private void MoveLockedOpponentDiceToDeviceSlots(IReadOnlyList<int> enemyDiceValues, IReadOnlyList<bool> lockStates)
        {
            if (diceTray3DView == null)
                return;

            if (enemyDiceValues == null || lockStates == null)
                return;

            ReleaseUnlockedOpponentDiceSlots(enemyDiceValues, lockStates);

            for (int diceIndex = 0; diceIndex < enemyDiceValues.Count && diceIndex < lockStates.Count; diceIndex++)
            {
                if (!lockStates[diceIndex])
                    continue;

                int slotIndex = FindOpponentLockedDiceSlot(diceIndex);

                if (slotIndex < 0)
                {
                    slotIndex = FindEmptyOpponentLockedDiceSlot();

                    if (slotIndex < 0)
                        return;

                    opponentLockedDiceIndexBySlot[slotIndex] = diceIndex;
                }

                if (!TryGetLockedDiceSlotAnchorPose(
                        DiceOwnerType.Opponent,
                        slotIndex,
                        out Vector3 targetPosition,
                        out Quaternion targetRotation))
                {
                    slotIndex++;
                    continue;
                }

                if (!diceTray3DView.IsDiceNearPosition(DiceOwnerType.Opponent, diceIndex, targetPosition, 0.02f))
                {
                    diceTray3DView.MoveDiceToLockedDeviceSlot(
                        DiceOwnerType.Opponent,
                        diceIndex,
                        targetPosition,
                        targetRotation,
                        lockedDiceMoveDuration);
                }
            }
        }

        /// <summary>Unlock되었거나 유효하지 않은 Opponent Lock Slot을 비우고 Dice를 Tray로 복귀시킨다.</summary>
        private void ReleaseUnlockedOpponentDiceSlots(IReadOnlyList<int> enemyDiceValues, IReadOnlyList<bool> lockStates)
        {
            for (int slotIndex = 0; slotIndex < opponentLockedDiceIndexBySlot.Length; slotIndex++)
            {
                int diceIndex = opponentLockedDiceIndexBySlot[slotIndex];

                if (diceIndex < 0)
                    continue;

                bool isValidDice = diceIndex < enemyDiceValues.Count && diceIndex < lockStates.Count;
                bool shouldStayLocked = isValidDice && lockStates[diceIndex];

                if (shouldStayLocked)
                    continue;

                opponentLockedDiceIndexBySlot[slotIndex] = -1;

                if (isValidDice && diceTray3DView != null)
                {
                    diceTray3DView.RestoreDiceToTray(
                        DiceOwnerType.Opponent,
                        diceIndex,
                        enemyDiceValues,
                        lockedDiceMoveDuration);
                }
            }
        }

        /// <summary>지정 Dice가 이미 배치된 Opponent Lock Slot 인덱스를 반환한다.</summary>
        private int FindOpponentLockedDiceSlot(int diceIndex)
        {
            for (int slotIndex = 0; slotIndex < opponentLockedDiceIndexBySlot.Length; slotIndex++)
            {
                if (opponentLockedDiceIndexBySlot[slotIndex] == diceIndex)
                    return slotIndex;
            }

            return -1;
        }

        /// <summary>비어 있는 첫 Opponent Lock Slot 인덱스를 반환한다.</summary>
        private int FindEmptyOpponentLockedDiceSlot()
        {
            int slotCount = Mathf.Min(opponentLockedDiceIndexBySlot.Length, SlotPairDamageCalculator.SlotPairCount);

            for (int slotIndex = 0; slotIndex < slotCount; slotIndex++)
            {
                if (opponentLockedDiceIndexBySlot[slotIndex] < 0)
                    return slotIndex;
            }

            return -1;
        }

        /// <summary>Opponent Lock Slot 매핑을 모두 초기화한다.</summary>
        private void ClearOpponentLockSlotMapping()
        {
            for (int slotIndex = 0; slotIndex < opponentLockedDiceIndexBySlot.Length; slotIndex++)
                opponentLockedDiceIndexBySlot[slotIndex] = -1;
        }

        /// <summary>지정 SlotPair 인덱스의 Player DeviceSlot을 강조한다.</summary>
        private void HighlightSlotPair(int slotIndex)
        {
            HighlightSlotPair(DiceOwnerType.Player, slotIndex);
        }

        /// <summary>지정 소유자의 SlotPair 인덱스 DeviceSlot을 강조한다.</summary>
        private void HighlightSlotPair(DiceOwnerType owner, int slotIndex)
        {
            // 새 구조에서는 DeviceSlot 하단 Dice와 DeviceSlot 자체가 LockSlot 역할을 겸한다.
            DeviceRack3DView targetRack = owner == DiceOwnerType.Opponent
                ? opponentDeviceRack3DView
                : playerDeviceRack3DView;

            if (targetRack != null)
                targetRack.HighlightSlot(slotIndex);
        }

        /// <summary>SlotPair 계산 연출 Highlight를 모두 해제한다.</summary>
        private void ClearSlotPairEvaluationHighlights()
        {
            if (playerDeviceRack3DView != null)
                playerDeviceRack3DView.ClearHighlight();

            if (opponentDeviceRack3DView != null)
                opponentDeviceRack3DView.ClearHighlight();
        }

        /// <summary>현재 SlotPair 단계의 DiceView를 제자리에서 점프/회전시킨다.</summary>
        private async UniTaskVoid PlayEvaluationDiceJumpRollAsync(
            SlotPairDamageStep step,
            CancellationToken cancellationToken)
        {
            PlayEvaluationDiceJumpRollAsync(DiceOwnerType.Player, step, cancellationToken).Forget();
            await UniTask.CompletedTask;
        }

        /// <summary>현재 SlotPair 단계의 지정 소유자 DiceView를 제자리에서 점프/회전시킨다.</summary>
        private async UniTaskVoid PlayEvaluationDiceJumpRollAsync(
            DiceOwnerType owner,
            SlotPairDamageStep step,
            CancellationToken cancellationToken)
        {
            if (!playDiceJumpRollDuringEvaluation) return;
            if (step == null) return;
            if (step.DiceIndex < 0) return;
            if (diceTray3DView == null) return;

            // 이미 DeviceSlot 하단에 배치된 DiceView를 제자리에서 반응시킨다.
            await diceTray3DView.PlayDiceJumpRollAsync(
                owner,
                step.DiceIndex,
                slotPairDiceJumpHeight,
                slotPairDiceJumpRollEuler,
                slotPairDiceJumpRollDuration,
                cancellationToken);
        }

        /// <summary>SlotPair Floating Text를 띄울 Player DeviceSlot 기준 월드 위치와 회전을 계산한다.</summary>
        private bool TryGetSlotPairFloatingWorldPose(
            int slotIndex,
            Vector3 localPresentationOffset,
            Quaternion localRotationOffset,
            out Vector3 worldPosition,
            out Quaternion worldRotation)
        {
            return TryGetSlotPairFloatingWorldPose(
                DiceOwnerType.Player,
                slotIndex,
                localPresentationOffset,
                localRotationOffset,
                out worldPosition,
                out worldRotation);
        }

        /// <summary>SlotPair Floating Text를 띄울 지정 소유자 DeviceSlot 기준 월드 위치와 회전을 계산한다.</summary>
        private bool TryGetSlotPairFloatingWorldPose(
            DiceOwnerType owner,
            int slotIndex,
            Vector3 localPresentationOffset,
            Quaternion localRotationOffset,
            out Vector3 worldPosition,
            out Quaternion worldRotation)
        {
            worldPosition = Vector3.zero;
            worldRotation = Quaternion.identity;

            DeviceRack3DView targetRack = ResolveDeviceRack(owner);

            if (targetRack == null)
                return false;

            Camera targetCamera = battleCamera != null ? battleCamera : Camera.main;

            if (!targetRack.TryGetSlotPresentationBasis(
                    slotIndex,
                    targetCamera,
                    out Vector3 slotCenter,
                    out Vector3 slotRight,
                    out Vector3 slotUp,
                    out Vector3 towardPlayer))
                return false;

            // Floating Text 전용으로 x=slotRight, y=slotUp, z=towardPlayer 로컬 offset을 적용한다.
            worldPosition = slotCenter
                            + slotRight * localPresentationOffset.x
                            + slotUp * localPresentationOffset.y
                            + towardPlayer * localPresentationOffset.z;

            worldRotation = Quaternion.LookRotation(towardPlayer, slotUp) * localRotationOffset;
            return true;
        }

        /// <summary>지정 소유자의 DeviceRack View를 반환한다.</summary>
        private DeviceRack3DView ResolveDeviceRack(DiceOwnerType owner)
        {
            return owner == DiceOwnerType.Opponent
                ? opponentDeviceRack3DView
                : playerDeviceRack3DView;
        }

        /// <summary>SlotPair 연산 연출 후 모든 DiceView를 DiceTray 위치로 복귀시킨다.</summary>
        private void RestoreEvaluationDicePlacement()
        {
            RestoreAllDiceToTrayAfterEvaluation();
        }

        /// <summary>연산 완료 후 모든 DiceView를 DiceTray 원래 위치로 복귀시킨다.</summary>
        private void RestoreAllDiceToTrayAfterEvaluation()
        {
            RestoreAllDiceToTrayAfterEvaluation(DiceOwnerType.Player, null);
        }

        /// <summary>연산 완료 후 지정 소유자의 DiceView를 DiceTray 원래 위치로 복귀시킨다.</summary>
        private void RestoreAllDiceToTrayAfterEvaluation(DiceOwnerType owner, IReadOnlyList<int> diceValuesOverride)
        {
            if (!restoreDiceToTrayAfterEvaluation)
                return;

            if (diceTray3DView == null)
                return;

            if (roundState == null)
                return;

            IReadOnlyList<int> diceValues = diceValuesOverride ?? roundState.GetCurrentDiceValues();

            // 상대 턴/다음 흐름으로 넘어가기 전 Dice를 모두 Tray에 정리한다.
            diceTray3DView.RestoreAllDiceToTray(owner, diceValues, lockedDiceMoveDuration);
        }

        #endregion

        #region SlotPair Evaluation Presentation

        /// <summary>Cast 제출 후 SlotPair 계산 순서 연출을 비동기로 재생한다.</summary>
        private async UniTask PlayOpponentSlotPairEvaluationSequenceAsync(ClashCastResult result)
        {
            if (result == null)
                return;

            PresentOpponentDiceForSlotPairEvaluation(result);
            SetMessage($"Opponent calculating {CastBoardCatalog.GetDisplayName(result.PatternType)}...");
            await PlaySlotPairEvaluationSequenceAsync(result);
        }

        /// <summary>Opponent의 확정 Cast에 맞춰 Dice를 각 SlotPair 슬롯에 배치한다.</summary>
        private void PresentOpponentDiceForSlotPairEvaluation(ClashCastResult result)
        {
            if (result == null || diceTray3DView == null)
                return;

            IReadOnlyList<int> diceValues = result.DiceValues;
            IReadOnlyList<int> lockSlotDiceIndexes = result.LockSlotDiceIndexes;
            List<bool> lockStates = CreateAllLockedStates(diceValues != null ? diceValues.Count : 0);

            diceTray3DView.SetDiceValuesOnly(DiceOwnerType.Opponent, diceValues, lockStates);

            for (int slotIndex = 0; slotIndex < opponentLockedDiceIndexBySlot.Length; slotIndex++)
                opponentLockedDiceIndexBySlot[slotIndex] = -1;

            if (lockSlotDiceIndexes == null)
                return;

            int slotCount = Mathf.Min(lockSlotDiceIndexes.Count, SlotPairDamageCalculator.SlotPairCount);

            for (int slotIndex = 0; slotIndex < slotCount; slotIndex++)
            {
                int diceIndex = lockSlotDiceIndexes[slotIndex];

                if (diceIndex < 0 || diceValues == null || diceIndex >= diceValues.Count)
                    continue;

                opponentLockedDiceIndexBySlot[slotIndex] = diceIndex;

                if (!TryGetLockedDiceSlotAnchorPose(
                        DiceOwnerType.Opponent,
                        slotIndex,
                        out Vector3 targetPosition,
                        out Quaternion targetRotation))
                    continue;

                diceTray3DView.MoveDiceToLockedDeviceSlot(
                    DiceOwnerType.Opponent,
                    diceIndex,
                    targetPosition,
                    targetRotation,
                    lockedDiceMoveDuration);
            }
        }

        /// <summary>지정 개수만큼 모두 Locked 상태인 목록을 생성한다.</summary>
        private static List<bool> CreateAllLockedStates(int count)
        {
            List<bool> lockStates = new List<bool>(Mathf.Max(0, count));

            for (int i = 0; i < count; i++)
                lockStates.Add(true);

            return lockStates;
        }

        /// <summary>Cast 제출 후 SlotPair 계산 순서 연출을 비동기로 재생한다.</summary>
        private async UniTask PlaySlotPairEvaluationSequenceAsync(ClashCastResult result)
        {
            if (!playSlotPairSequenceOnSubmit || result == null || result.SlotPairDamagePreview == null)
            {
                isSlotPairResultPresentationComplete = true;
                RefreshSelectedCastTexts();
                RefreshClashPowerTexts();
                return;
            }

            CancelSlotPairEvaluationSequence();

            CancellationTokenSource currentCts = new CancellationTokenSource();
            slotPairSequenceCts = currentCts;

            bool completed = false;

            try
            {
                LogSlotPairSequenceStarted(result);

                await PlaySlotPairEvaluationSequenceInternalAsync(result, currentCts.Token);
                completed = true;
                LogSlotPairSequenceCompleted(result);
            }
            catch (OperationCanceledException)
            {
                LogSlotPairSequenceCanceled();
            }
            finally
            {
                if (slotPairSequenceCts == currentCts)
                {
                    slotPairSequenceCts = null;

                    if (completed)
                    {
                        if (result.Owner == ClashParticipantType.Player && interactionState == BattleInteractionState.CastResolving)
                            RefreshDeviceSlotLockedDicePresentation();
                        else
                            RestoreAllDiceToTrayAfterEvaluation(ResolveDiceOwner(result), result.DiceValues);
                    }
                    else
                    {
                        RefreshDeviceSlotLockedDicePresentation();
                    }

                    ClearSlotPairEvaluationHighlights();

                    if (completed)
                    {
                        isSlotPairResultPresentationComplete = true;
                        RefreshSelectedCastTexts();
                        RefreshClashPowerTexts();
                    }
                }

                currentCts.Dispose();
            }
        }

        /// <summary>Clash 결과의 Dice 소유자를 반환한다.</summary>
        private static DiceOwnerType ResolveDiceOwner(ClashCastResult result)
        {
            return result != null && result.Owner == ClashParticipantType.Opponent
                ? DiceOwnerType.Opponent
                : DiceOwnerType.Player;
        }

        /// <summary>SlotPairDamagePreview의 Step 목록을 기준으로 DeviceSlot과 Dice 반응 연출을 순차 재생한다.</summary>
        private async UniTask PlaySlotPairEvaluationSequenceInternalAsync(
            ClashCastResult result,
            CancellationToken cancellationToken)
        {
            ClearSlotPairEvaluationHighlights();

            SlotPairDamagePreview preview = result.SlotPairDamagePreview;

            if (slotPairSequenceStartDelay > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(slotPairSequenceStartDelay), cancellationToken: cancellationToken);

            IReadOnlyList<SlotPairDamageStep> steps = preview.Steps;
            int stepCount = steps != null ? steps.Count : 0;
            float hologramUpdateLeadDuration = CalculateSlotPairHologramUpdateLeadDuration();

            for (int i = 0; i < stepCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                SlotPairDamageStep step = steps[i];

                HighlightSlotPair(ResolveDiceOwner(result), step.SlotIndex);

                LogSlotPairStep(result, step);

                DiceOwnerType owner = ResolveDiceOwner(result);

                PlayEvaluationDiceJumpRollAsync(owner, step, cancellationToken).Forget();
                PlaySlotPairFloatingTextAsync(owner, step, cancellationToken).Forget();

                if (hologramUpdateLeadDuration > 0f)
                    await UniTask.Delay(TimeSpan.FromSeconds(hologramUpdateLeadDuration), cancellationToken: cancellationToken);

                await RefreshTableHologramSlotPairStepAsync(result, step, cancellationToken);

                float remainingHighlightDuration = Mathf.Max(0f, slotPairHighlightDuration - hologramUpdateLeadDuration);

                if (remainingHighlightDuration > 0f)
                    await UniTask.Delay(TimeSpan.FromSeconds(remainingHighlightDuration), cancellationToken: cancellationToken);

                ClearSlotPairEvaluationHighlights();

                if (slotPairHighlightGap > 0f)
                    await UniTask.Delay(TimeSpan.FromSeconds(slotPairHighlightGap), cancellationToken: cancellationToken);
            }
        }

        /// <summary>SlotPair 슬롯 연출 내에서 홀로그램 누적값 갱신 전 대기 시간을 계산한다.</summary>
        private float CalculateSlotPairHologramUpdateLeadDuration()
        {
            if (slotPairHighlightDuration <= 0f)
                return 0f;

            float ratio = Mathf.Clamp01(slotPairHologramUpdateLeadRatio);
            return slotPairHighlightDuration * ratio;
        }

        /// <summary>진행 중인 SlotPair 계산 연출을 중단한다.</summary>
        private void CancelSlotPairEvaluationSequence()
        {
            if (slotPairSequenceCts == null)
                return;

            // UniTask Delay와 Highlight 시퀀스를 안전하게 취소한다.
            slotPairSequenceCts.Cancel();
            slotPairSequenceCts.Dispose();
            slotPairSequenceCts = null;
        }

        /// <summary>현재 SlotPair 단계의 변화량을 Floating Text로 표시한다.</summary>
        private async UniTaskVoid PlaySlotPairFloatingTextAsync(SlotPairDamageStep step, CancellationToken cancellationToken)
        {
            PlaySlotPairFloatingTextAsync(DiceOwnerType.Player, step, cancellationToken).Forget();
            await UniTask.CompletedTask;
        }

        /// <summary>현재 SlotPair 단계의 변화량을 지정 소유자의 Floating Text로 표시한다.</summary>
        private async UniTaskVoid PlaySlotPairFloatingTextAsync(DiceOwnerType owner, SlotPairDamageStep step, CancellationToken cancellationToken)
        {
            if (!playSlotPairFloatingText) return;
            if (step == null) return;
            if (slotPairStepFloatingTextView == null) return;
            if (!TryGetSlotPairFloatingAnchoredPosition(owner, step.SlotIndex, out Vector2 anchoredPosition)) return;

            string message = BuildSlotPairFloatingMessage(step);

            if (string.IsNullOrWhiteSpace(message)) return;

            // 현재 Highlight 중인 DeviceSlot 위에 Balatro식 짧은 변화량 텍스트를 띄운다.
            await slotPairStepFloatingTextView.PlayAsync(message, anchoredPosition, cancellationToken);
        }

        /// <summary>SlotPair Floating Text를 띄울 DeviceSlot 기준 Overlay 좌표를 계산한다.</summary>
        private bool TryGetSlotPairFloatingAnchoredPosition(int slotIndex, out Vector2 anchoredPosition)
        {
            return TryGetSlotPairFloatingAnchoredPosition(DiceOwnerType.Player, slotIndex, out anchoredPosition);
        }

        /// <summary>SlotPair Floating Text를 띄울 지정 소유자 DeviceSlot 기준 Overlay 좌표를 계산한다.</summary>
        private bool TryGetSlotPairFloatingAnchoredPosition(DiceOwnerType owner, int slotIndex, out Vector2 anchoredPosition)
        {
            anchoredPosition = Vector2.zero;

            DeviceRack3DView targetRack = ResolveDeviceRack(owner);

            if (targetRack == null)
                return false;

            RectTransform root = slotPairFloatingTextRoot;

            if (root == null && slotPairStepFloatingTextView != null)
                root = slotPairStepFloatingTextView.transform.parent as RectTransform;

            if (root == null)
                return false;

            Camera targetCamera = battleCamera != null ? battleCamera : Camera.main;

            if (targetCamera == null)
                return false;

            Vector3 floatingWorldOffset = ResolveSlotPairFloatingWorldOffset(owner);

            if (!TryGetSlotPairFloatingWorldPose(
                    owner,
                    slotIndex,
                    floatingWorldOffset,
                    Quaternion.identity,
                    out Vector3 worldPosition,
                    out Quaternion _))
                return false;

            // Transform pivot이 아니라 Renderer/Collider 중심 + 슬롯 로컬 Presentation offset 기준으로 Overlay 좌표를 만든다.
            Vector3 screenPosition = targetCamera.WorldToScreenPoint(worldPosition);

            if (screenPosition.z < 0f)
                return false;

            // UIRoot가 Screen Space Overlay이므로 camera 인자는 null을 사용한다.
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenPosition, null, out anchoredPosition);
        }

        /// <summary>소유자별 SlotPair Floating Text 로컬 위치 보정값을 반환한다.</summary>
        private Vector3 ResolveSlotPairFloatingWorldOffset(DiceOwnerType owner)
        {
            return owner == DiceOwnerType.Opponent
                ? opponentSlotPairFloatingWorldOffset
                : playerSlotPairFloatingWorldOffset;
        }

        /// <summary>SlotPair Step의 Score/Force 변화량을 짧은 Floating Text 문구로 변환한다.</summary>
        private string BuildSlotPairFloatingMessage(SlotPairDamageStep step)
        {
            if (step == null)
                return string.Empty;

            if (!step.DidApply)
                return "SKIP";

            List<string> messages = new List<string>();

            int scoreDelta = step.ScoreAfter - step.ScoreBefore;
            float forceDelta = step.ForceAfter - step.ForceBefore;

            if (scoreDelta != 0)
                messages.Add(FormatSignedScoreDelta(scoreDelta));

            if (Mathf.Abs(forceDelta) > 0.001f)
                messages.Add(FormatForceDelta(step.ForceBefore, step.ForceAfter));

            if (messages.Count <= 0)
                messages.Add("OK");

            return string.Join("\n", messages);
        }

        /// <summary>Score 변화량을 표시용 문자열로 변환한다.</summary>
        private static string FormatSignedScoreDelta(int scoreDelta)
        {
            if (scoreDelta > 0)
                return $"+{scoreDelta}";

            return scoreDelta.ToString();
        }

        /// <summary>Force 변화량을 표시용 문자열로 변환한다.</summary>
        private static string FormatForceDelta(float before, float after)
        {
            if (before > 0.001f)
            {
                float ratio = after / before;

                if (Mathf.Abs(ratio - 1f) > 0.001f && Mathf.Abs(after - before) > 0.001f)
                {
                    if (ratio > 1.01f)
                        return $"x{FormatCompactFloat(ratio)}";
                }
            }

            float delta = after - before;

            if (delta > 0f)
                return $"+{FormatCompactFloat(delta)}F";

            return $"{FormatCompactFloat(delta)}F";
        }

        /// <summary>float 값을 짧은 표시용 문자열로 변환한다.</summary>
        private static string FormatCompactFloat(float value)
        {
            if (Mathf.Abs(value - Mathf.Round(value)) < 0.001f)
                return Mathf.RoundToInt(value).ToString();

            return value.ToString("0.##");
        }

        #endregion

        #region Opponent Roll AI

        /// <summary>상대 Roll AI를 실행해 최종 Opponent Clash 결과를 생성한다.</summary>
        private async UniTask<bool> BuildOpponentClashResultWithRollAIAsync(string phaseName, ClashCastResult playerResult)
        {
            pendingOpponentClashResult = null;
            ClearOpponentLockSlotMapping();

            int rollCount = ResolveOpponentRollCount();
            OpponentCastSelectionPolicy castSelectionPolicy = ResolveOpponentCastSelectionPolicy();
            OpponentRollStrategyType rollStrategy = ResolveOpponentRollStrategy();

            List<DiceInstance> opponentDiceInstances = CreateOpponentDiceInstances();
            List<bool> lockStates = CreateEnemyDiceLockStates(opponentDiceInstances.Count);
            List<SlotPairDeviceDefinition> opponentDevices = CreateOpponentDeviceDefinitions();

            for (int rollIndex = 1; rollIndex <= rollCount; rollIndex++)
            {
                if (rollIndex > 1)
                    RollUnlockedOpponentDiceInstances(opponentDiceInstances, lockStates);

                List<int> enemyDiceValues = ExtractOpponentNumericDiceValues(opponentDiceInstances);

                await PlayEnemyDiceCupPresentationAsync(enemyDiceValues, lockStates);

                bool built = simulator.TryBuildBestOpponentClashCastResultPreview(
                    roundState,
                    enemyDiceValues,
                    opponentDevices,
                    castSelectionPolicy,
                    out ClashCastResult candidateResult);

                LogOpponentRollAIStep(
                    phaseName,
                    rollIndex,
                    rollCount,
                    enemyDiceValues,
                    lockStates,
                    candidateResult,
                    playerResult);

                if (!built || candidateResult == null)
                {
                    ClearEnemyDiceLocks(lockStates);
                    ApplyLockStatesToOpponentDiceInstances(opponentDiceInstances, lockStates);
                    continue;
                }

                if (ShouldStopOpponentRollAI(candidateResult, playerResult, rollIndex, rollCount))
                {
                    pendingOpponentClashResult = candidateResult;
                    break;
                }

                if (opponentLockedDiceMoveDelay > 0f)
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(opponentLockedDiceMoveDelay),
                        cancellationToken: this.GetCancellationTokenOnDestroy());
                }

                ApplyOpponentRollStrategyLocks(enemyDiceValues, candidateResult, lockStates, rollStrategy);
                ApplyLockStatesToOpponentDiceInstances(opponentDiceInstances, lockStates);
                MoveLockedOpponentDiceToDeviceSlots(enemyDiceValues, lockStates);

                LogOpponentRollAILockDecision(
                    phaseName,
                    rollIndex,
                    rollStrategy,
                    enemyDiceValues,
                    lockStates,
                    candidateResult);
            }

            if (pendingOpponentClashResult == null)
            {
                List<int> finalDiceValues = ExtractOpponentNumericDiceValues(opponentDiceInstances);

                bool built = simulator.TryBuildBestOpponentClashCastResultPreview(
                    roundState,
                    finalDiceValues,
                    opponentDevices,
                    castSelectionPolicy,
                    out pendingOpponentClashResult);

                if (!built || pendingOpponentClashResult == null)
                    return false;
            }

            bool recorded = simulator.TryBuildBestOpponentClashCastResult(
                roundState,
                pendingOpponentClashResult.DiceValues,
                opponentDevices,
                castSelectionPolicy,
                out ClashCastResult recordedResult);

            if (!recorded || recordedResult == null)
                return false;

            pendingOpponentClashResult = recordedResult;
            return true;
        }

        /// <summary>Round 규칙에서 상대 Attempt 기본 Roll 횟수를 반환한다.</summary>
        private int ResolveOpponentRollCount()
        {
            if (roundState != null && roundState.RuleContext != null)
                return roundState.RuleContext.OpponentBaseRollsPerAttempt;

            return RoundState.DefaultPlayerBaseRollsPerAttempt;
        }

        /// <summary>현재 Intent 기준 상대 Cast 선택 방식을 반환한다.</summary>
        private OpponentCastSelectionPolicy ResolveOpponentCastSelectionPolicy()
        {
            if (currentEnemyIntentDefinition == null)
                return OpponentCastSelectionPolicy.UtilityBest;

            return currentEnemyIntentDefinition.CastSelectionPolicy;
        }

        /// <summary>현재 Intent 기준 상대 Roll 유지 전략을 반환한다.</summary>
        private OpponentRollStrategyType ResolveOpponentRollStrategy()
        {
            if (currentEnemyIntentDefinition == null)
                return OpponentRollStrategyType.Balanced;

            return currentEnemyIntentDefinition.RollStrategy;
        }

        /// <summary>현재 Intent와 비교 대상에 따라 상대 Roll 조기 중단 여부를 판단한다.</summary>
        private bool ShouldStopOpponentRollAI(
            ClashCastResult candidateResult,
            ClashCastResult playerResult,
            int rollIndex,
            int maxRollCount)
        {
            if (candidateResult == null)
                return false;

            if (rollIndex >= maxRollCount)
                return true;

            if (currentEnemyIntentDefinition != null &&
                currentEnemyIntentDefinition.TargetPowerToStop > 0 &&
                candidateResult.CastPower >= currentEnemyIntentDefinition.TargetPowerToStop)
            {
                return true;
            }

            if (currentEnemyIntentDefinition != null &&
                currentEnemyIntentDefinition.TargetImpactToStop > 0 &&
                candidateResult.ExpectedImpactDamage >= currentEnemyIntentDefinition.TargetImpactToStop)
            {
                return true;
            }

            if (currentEnemyIntentDefinition != null &&
                currentEnemyIntentDefinition.StopIfBeatsPlayerPower &&
                playerResult != null &&
                candidateResult.CastPower > playerResult.CastPower)
            {
                return true;
            }

            if (playerResult != null && candidateResult.ExpectedImpactDamage >= roundState.Encounter.PlayerCurrentHP)
                return true;

            return false;
        }

        /// <summary>상대 주사위 잠금을 모두 해제한다.</summary>
        private static void ClearEnemyDiceLocks(List<bool> lockStates)
        {
            if (lockStates == null)
                return;

            for (int i = 0; i < lockStates.Count; i++)
                lockStates[i] = false;
        }

        /// <summary>Roll 전략에 따라 다음 Roll 전 유지할 상대 주사위를 결정한다.</summary>
        private void ApplyOpponentRollStrategyLocks(
            IReadOnlyList<int> diceValues,
            ClashCastResult candidateResult,
            List<bool> lockStates,
            OpponentRollStrategyType rollStrategy)
        {
            ClearEnemyDiceLocks(lockStates);

            if (diceValues == null || candidateResult == null || lockStates == null)
                return;

            switch (rollStrategy)
            {
                case OpponentRollStrategyType.GreedyBestDamage:
                    ApplyGreedyBestDamageLocks(diceValues, candidateResult, lockStates);
                    break;

                case OpponentRollStrategyType.KeepHighestGroup:
                    ApplyHighestGroupLocks(diceValues, lockStates);
                    break;

                case OpponentRollStrategyType.ChaseStraight:
                    ApplyStraightChaseLocks(diceValues, lockStates);
                    break;

                case OpponentRollStrategyType.Balanced:
                    ApplyBalancedOpponentLocks(diceValues, candidateResult, lockStates);
                    break;

                default:
                    ApplyBalancedOpponentLocks(diceValues, candidateResult, lockStates);
                    break;
            }

            EnsureAtLeastOneEnemyDieUnlocked(diceValues, lockStates);
        }

        /// <summary>현재 최고 피해 Cast에 직접 기여하는 주사위를 유지한다.</summary>
        private static void ApplyGreedyBestDamageLocks(
            IReadOnlyList<int> diceValues,
            ClashCastResult candidateResult,
            List<bool> lockStates)
        {
            RollPatternType patternType = candidateResult.PatternType;

            if (patternType >= RollPatternType.Aces && patternType <= RollPatternType.Sixes)
            {
                int targetFace = (int)patternType;
                LockFaceValue(diceValues, lockStates, targetFace);
                return;
            }

            if (patternType == RollPatternType.ThreeOfAKind ||
                patternType == RollPatternType.FourOfAKind ||
                patternType == RollPatternType.Tessera)
            {
                ApplyHighestGroupLocks(diceValues, lockStates);
                return;
            }

            if (patternType == RollPatternType.FullHouse)
            {
                LockPairOrBetterValues(diceValues, lockStates);
                return;
            }

            if (patternType == RollPatternType.SmallStraight || patternType == RollPatternType.LargeStraight)
            {
                ApplyStraightChaseLocks(diceValues, lockStates);
                return;
            }

            LockHighValueDice(diceValues, lockStates, 5);
        }

        /// <summary>상황별로 중복, 스트레이트, 고눈금 유지 전략을 선택한다.</summary>
        private static void ApplyBalancedOpponentLocks(
            IReadOnlyList<int> diceValues,
            ClashCastResult candidateResult,
            List<bool> lockStates)
        {
            RollPatternType patternType = candidateResult.PatternType;

            if (patternType == RollPatternType.ThreeOfAKind ||
                patternType == RollPatternType.FourOfAKind ||
                patternType == RollPatternType.FullHouse ||
                patternType == RollPatternType.Tessera)
            {
                ApplyGreedyBestDamageLocks(diceValues, candidateResult, lockStates);
                return;
            }

            int longestStraightCount = CountBestStraightProgress(diceValues);

            if (longestStraightCount >= 3)
            {
                ApplyStraightChaseLocks(diceValues, lockStates);
                return;
            }

            int bestGroupCount = GetBestRepeatedFaceCount(diceValues);

            if (bestGroupCount >= 2)
            {
                ApplyHighestGroupLocks(diceValues, lockStates);
                return;
            }

            LockHighValueDice(diceValues, lockStates, 5);
        }

        /// <summary>가장 높은 중복 그룹 또는 가장 높은 단일 주사위를 유지한다.</summary>
        private static void ApplyHighestGroupLocks(IReadOnlyList<int> diceValues, List<bool> lockStates)
        {
            int bestFace = FindBestRepeatedFace(diceValues);

            if (bestFace <= 0)
            {
                LockHighestSingleDie(diceValues, lockStates);
                return;
            }

            LockFaceValue(diceValues, lockStates, bestFace);
        }

        /// <summary>가장 완성도가 높은 스트레이트 후보의 서로 다른 눈금 1개씩을 유지한다.</summary>
        private static void ApplyStraightChaseLocks(IReadOnlyList<int> diceValues, List<bool> lockStates)
        {
            int[] bestSequence = FindBestStraightSequence(diceValues);

            if (bestSequence == null || bestSequence.Length <= 0)
            {
                LockHighestSingleDie(diceValues, lockStates);
                return;
            }

            bool[] usedFaces = new bool[7];

            for (int sequenceIndex = 0; sequenceIndex < bestSequence.Length; sequenceIndex++)
            {
                int face = bestSequence[sequenceIndex];

                if (face < 1 || face > 6 || usedFaces[face])
                    continue;

                for (int diceIndex = 0; diceIndex < diceValues.Count; diceIndex++)
                {
                    if (diceValues[diceIndex] != face)
                        continue;

                    lockStates[diceIndex] = true;
                    usedFaces[face] = true;
                    break;
                }
            }
        }

        /// <summary>지정 눈금과 같은 주사위를 모두 유지한다.</summary>
        private static void LockFaceValue(IReadOnlyList<int> diceValues, List<bool> lockStates, int faceValue)
        {
            for (int i = 0; i < diceValues.Count && i < lockStates.Count; i++)
            {
                if (diceValues[i] == faceValue)
                    lockStates[i] = true;
            }
        }

        /// <summary>2개 이상 나온 눈금의 주사위를 모두 유지한다.</summary>
        private static void LockPairOrBetterValues(IReadOnlyList<int> diceValues, List<bool> lockStates)
        {
            int[] counts = BuildFaceCounts(diceValues);

            for (int i = 0; i < diceValues.Count && i < lockStates.Count; i++)
            {
                int face = diceValues[i];

                if (face >= 1 && face <= 6 && counts[face] >= 2)
                    lockStates[i] = true;
            }
        }

        /// <summary>지정 값 이상의 고눈금 주사위를 유지한다.</summary>
        private static void LockHighValueDice(IReadOnlyList<int> diceValues, List<bool> lockStates, int minimumFaceValue)
        {
            bool lockedAny = false;

            for (int i = 0; i < diceValues.Count && i < lockStates.Count; i++)
            {
                if (diceValues[i] < minimumFaceValue)
                    continue;

                lockStates[i] = true;
                lockedAny = true;
            }

            if (!lockedAny)
                LockHighestSingleDie(diceValues, lockStates);
        }

        /// <summary>가장 높은 단일 주사위 1개를 유지한다.</summary>
        private static void LockHighestSingleDie(IReadOnlyList<int> diceValues, List<bool> lockStates)
        {
            int bestIndex = -1;
            int bestValue = int.MinValue;

            for (int i = 0; i < diceValues.Count && i < lockStates.Count; i++)
            {
                if (diceValues[i] <= bestValue)
                    continue;

                bestValue = diceValues[i];
                bestIndex = i;
            }

            if (bestIndex >= 0)
                lockStates[bestIndex] = true;
        }

        /// <summary>모든 주사위가 잠겨 다음 Roll이 무의미해지는 상황을 방지한다.</summary>
        private static void EnsureAtLeastOneEnemyDieUnlocked(IReadOnlyList<int> diceValues, List<bool> lockStates)
        {
            if (lockStates == null || lockStates.Count <= 0)
                return;

            bool hasUnlocked = false;

            for (int i = 0; i < lockStates.Count; i++)
            {
                if (lockStates[i])
                    continue;

                hasUnlocked = true;
                break;
            }

            if (hasUnlocked)
                return;

            int unlockIndex = FindLowestValueDiceIndex(diceValues, lockStates.Count);

            if (unlockIndex >= 0)
                lockStates[unlockIndex] = false;
        }

        /// <summary>가장 낮은 값의 주사위 인덱스를 반환한다.</summary>
        private static int FindLowestValueDiceIndex(IReadOnlyList<int> diceValues, int maxCount)
        {
            if (diceValues == null || diceValues.Count <= 0)
                return -1;

            int bestIndex = -1;
            int bestValue = int.MaxValue;
            int count = Mathf.Min(diceValues.Count, maxCount);

            for (int i = 0; i < count; i++)
            {
                if (diceValues[i] >= bestValue)
                    continue;

                bestValue = diceValues[i];
                bestIndex = i;
            }

            return bestIndex;
        }

        /// <summary>가장 좋은 중복 눈금을 반환한다. 중복이 없으면 0을 반환한다.</summary>
        private static int FindBestRepeatedFace(IReadOnlyList<int> diceValues)
        {
            int[] counts = BuildFaceCounts(diceValues);
            int bestFace = 0;
            int bestCount = 1;

            for (int face = 6; face >= 1; face--)
            {
                if (counts[face] <= bestCount)
                    continue;

                bestCount = counts[face];
                bestFace = face;
            }

            return bestFace;
        }

        /// <summary>가장 높은 중복 개수를 반환한다.</summary>
        private static int GetBestRepeatedFaceCount(IReadOnlyList<int> diceValues)
        {
            int[] counts = BuildFaceCounts(diceValues);
            int bestCount = 0;

            for (int face = 1; face <= 6; face++)
            {
                if (counts[face] > bestCount)
                    bestCount = counts[face];
            }

            return bestCount;
        }

        /// <summary>눈금별 개수를 계산한다.</summary>
        private static int[] BuildFaceCounts(IReadOnlyList<int> diceValues)
        {
            int[] counts = new int[7];

            if (diceValues == null)
                return counts;

            for (int i = 0; i < diceValues.Count; i++)
            {
                int face = diceValues[i];

                if (face < 1 || face > 6)
                    continue;

                counts[face]++;
            }

            return counts;
        }

        /// <summary>현재 주사위가 가진 최고 스트레이트 진행도를 반환한다.</summary>
        private static int CountBestStraightProgress(IReadOnlyList<int> diceValues)
        {
            int[] sequence = FindBestStraightSequence(diceValues);
            return sequence != null ? sequence.Length : 0;
        }

        /// <summary>현재 주사위에서 가장 많이 맞춰진 스트레이트 후보 눈금 배열을 반환한다.</summary>
        private static int[] FindBestStraightSequence(IReadOnlyList<int> diceValues)
        {
            int[] lowLarge = { 1, 2, 3, 4, 5 };
            int[] highLarge = { 2, 3, 4, 5, 6 };
            int[] lowSmall = { 1, 2, 3, 4 };
            int[] middleSmall = { 2, 3, 4, 5 };
            int[] highSmall = { 3, 4, 5, 6 };

            int[][] candidates =
            {
                lowLarge,
                highLarge,
                lowSmall,
                middleSmall,
                highSmall
            };

            bool[] exists = BuildFaceExists(diceValues);
            int[] bestMatchedFaces = Array.Empty<int>();
            int bestMatchCount = -1;

            for (int i = 0; i < candidates.Length; i++)
            {
                int[] candidate = candidates[i];
                List<int> matchedFaces = new List<int>();

                for (int j = 0; j < candidate.Length; j++)
                {
                    int face = candidate[j];

                    if (exists[face])
                        matchedFaces.Add(face);
                }

                if (matchedFaces.Count < bestMatchCount)
                    continue;

                if (matchedFaces.Count == bestMatchCount && candidate.Length < bestMatchedFaces.Length)
                    continue;

                bestMatchCount = matchedFaces.Count;
                bestMatchedFaces = matchedFaces.ToArray();
            }

            return bestMatchedFaces;
        }

        /// <summary>눈금 존재 여부를 계산한다.</summary>
        private static bool[] BuildFaceExists(IReadOnlyList<int> diceValues)
        {
            bool[] exists = new bool[7];

            if (diceValues == null)
                return exists;

            for (int i = 0; i < diceValues.Count; i++)
            {
                int face = diceValues[i];

                if (face < 1 || face > 6)
                    continue;

                exists[face] = true;
            }

            return exists;
        }

        /// <summary>현재 Round 정의를 기준으로 상대 DiceInstance 세트를 생성하고 첫 Roll까지 수행한다.</summary>
        private List<DiceInstance> CreateOpponentDiceInstances()
        {
            int diceCount = roundState != null && roundState.RuleContext != null
                ? roundState.RuleContext.DiceCount
                : 5;

            EnsureOpponentDiceRoller();

            EnemyDiceLoadoutDefinitionSO loadout = currentRoundDefinition != null
                ? currentRoundDefinition.OpponentDiceLoadout
                : null;

            if (loadout != null)
                return loadout.CreateRolledDiceSet(diceCount, opponentDiceRoller);

            return opponentDiceRoller.CreateRolledStandardDiceSet(diceCount);
        }

        /// <summary>상대 DiceRoller가 없으면 생성한다.</summary>
        private void EnsureOpponentDiceRoller()
        {
            if (opponentDiceRoller != null)
                return;

            opponentDiceRoller = new DiceRoller(activeCombatSeed + 9109);
            if (logCombatSeed)
                Debug.Log($"[Tessera][Battle] CombatSeed={activeCombatSeed} Serial={combatEntrySerial}");
        }

        /// <summary>잠기지 않은 상대 DiceInstance만 다시 굴린다.</summary>
        private void RollUnlockedOpponentDiceInstances(
            IReadOnlyList<DiceInstance> diceInstances,
            IReadOnlyList<bool> lockStates)
        {
            if (diceInstances == null)
                return;

            EnsureOpponentDiceRoller();
            ApplyLockStatesToOpponentDiceInstances(diceInstances, lockStates);
            opponentDiceRoller.RollUnlocked(diceInstances);
        }

        /// <summary>상대 DiceInstance 목록에 lockStates를 반영한다.</summary>
        private static void ApplyLockStatesToOpponentDiceInstances(
            IReadOnlyList<DiceInstance> diceInstances,
            IReadOnlyList<bool> lockStates)
        {
            if (diceInstances == null)
                return;

            for (int i = 0; i < diceInstances.Count; i++)
            {
                DiceInstance diceInstance = diceInstances[i];

                if (diceInstance == null)
                    continue;

                bool isLocked = lockStates != null && i < lockStates.Count && lockStates[i];
                diceInstance.SetLocked(isLocked);
            }
        }

        /// <summary>상대 DiceInstance 목록에서 Pattern 평가용 숫자값 목록을 추출한다.</summary>
        private List<int> ExtractOpponentNumericDiceValues(IReadOnlyList<DiceInstance> diceInstances)
        {
            List<int> values = new List<int>();

            if (diceInstances == null)
                return values;

            for (int i = 0; i < diceInstances.Count; i++)
            {
                if (TryExtractPatternValueFromDiceInstance(diceInstances[i], out int value))
                {
                    values.Add(ClampDiceValue(value));
                    continue;
                }

                Debug.LogWarning(
                    $"[Tessera][OpponentDice] Unsupported or empty dice face. " +
                    $"Index={i} | FallbackValue=1");

                values.Add(1);
            }

            return values;
        }

        /// <summary>DiceInstance의 현재 면에서 Pattern 평가용 숫자값을 추출한다.</summary>
        private static bool TryExtractPatternValueFromDiceInstance(DiceInstance diceInstance, out int value)
        {
            value = 0;

            if (diceInstance == null)
                return false;

            DiceFace currentFace = diceInstance.CurrentFace;

            if (!currentFace.IsNumber)
                return false;

            value = currentFace.NumberValue;
            return true;
        }

        /// <summary>상대 턴 연출용 Lock 상태 목록을 생성한다.</summary>
        private static List<bool> CreateEnemyDiceLockStates(int diceCount)
        {
            List<bool> lockStates = new List<bool>(diceCount);

            for (int i = 0; i < diceCount; i++)
                lockStates.Add(false);

            return lockStates;
        }

        #endregion

        #region Logging And Formatting

        /// <summary>전투 진행 메시지를 로그로 남긴다.</summary>
        private void SetMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            Debug.Log($"[Tessera][Message] {message}");
        }

        /// <summary>SlotPair 연출 시작 전 기본 Cast 값만 담은 메시지를 생성한다.</summary>
        private string BuildSubmittedCastStartMessage(ClashCastResult result)
        {
            if (result == null)
                return "Submit failed.";

            if (result.SlotPairDamagePreview == null)
                return $"{result.PatternResult.PatternType}: SlotPair evaluation started.";

            int basePower = CalculateTableRuleAppliedCastPower(
                result.SlotPairDamagePreview.InitialScore,
                result.SlotPairDamagePreview.BaseForce,
                0);

            return
                $"{result.PatternResult.PatternType}: " +
                $"Base Score {result.SlotPairDamagePreview.InitialScore} x " +
                $"Force {FormatForce(result.SlotPairDamagePreview.BaseForce)} = " +
                $"Base Power {basePower}.";
        }

        /// <summary>Clash 판정 결과를 디버그 로그로 출력한다.</summary>
        private void LogClashResolveResult(ClashResolveResult result)
        {
            if (result == null)
                return;

            string winnerText = result.Winner.HasValue
                ? result.Winner.Value.ToString()
                : "Tie";

            Debug.Log(
                $"[Tessera][Clash] Attempt={result.AttemptNumber} | " +
                $"Winner={winnerText} | " +
                $"PlayerDice=[{FormatDiceValuesForLog(result.PlayerResult != null ? result.PlayerResult.DiceValues : null)}] | " +
                $"PlayerCast={(result.PlayerResult != null ? result.PlayerResult.PatternType.ToString() : "None")} | " +
                $"PlayerPower={(result.PlayerResult != null ? result.PlayerResult.CastPower.ToString() : "0")} | " +
                $"PlayerExpectedImpact={(result.PlayerResult != null ? result.PlayerResult.ExpectedImpactDamage.ToString() : "0")} | " +
                $"OpponentDice=[{FormatDiceValuesForLog(result.OpponentResult != null ? result.OpponentResult.DiceValues : null)}] | " +
                $"OpponentCast={(result.OpponentResult != null ? result.OpponentResult.PatternType.ToString() : "None")} | " +
                $"OpponentPower={(result.OpponentResult != null ? result.OpponentResult.CastPower.ToString() : "0")} | " +
                $"OpponentExpectedImpact={(result.OpponentResult != null ? result.OpponentResult.ExpectedImpactDamage.ToString() : "0")} | " +
                $"AppliedImpactToPlayer={result.AppliedImpactDamageToPlayer} | " +
                $"AppliedImpactToOpponent={result.AppliedImpactDamageToOpponent} | " +
                $"BrokenDefense={result.PlayerUsedBrokenCastDefense} | " +
                $"Overcharge+={result.GrantedOverchargeAmount} | " +
                $"Outcome={result.OutcomeType}");
        }

        /// <summary>상대가 확정한 Clash Cast 결과를 디버그 로그로 출력한다.</summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogOpponentClashCastResult(string phaseName, ClashCastResult result)
        {
            if (!CanLogOpponentRollAIDebug())
                return;

            if (result == null)
                return;

            int rawDamage = result.SlotPairDamagePreview != null
                ? result.SlotPairDamagePreview.CastPowerBeforeTableRules
                : result.CastPower;

            int finalScore = result.SlotPairDamagePreview != null
                ? result.SlotPairDamagePreview.FinalScore
                : 0;

            string finalForce = result.SlotPairDamagePreview != null
                ? result.SlotPairDamagePreview.FormatFinalForce()
                : "-";

            string intentName = currentEnemyIntentDefinition != null
                ? currentEnemyIntentDefinition.DisplayName
                : "Fallback";

            string initiativeName = roundState != null && roundState.CurrentAttempt != null
                ? roundState.CurrentAttempt.InitiativeOwner.ToString()
                : "-";

            string useDevicesText = currentEnemyIntentDefinition != null
                ? currentEnemyIntentDefinition.UseOpponentDevices.ToString()
                : "Fallback";

            string chooseBestText = currentEnemyIntentDefinition != null
                ? currentEnemyIntentDefinition.CastSelectionPolicy.ToString()
                : "Fallback";

            string attemptNumberText = roundState != null && roundState.CurrentAttempt != null
                ? roundState.CurrentAttempt.AttemptNumber.ToString()
                : "-";

            Debug.Log(
                $"[Tessera][OpponentCast] Phase={phaseName} | " +
                $"Attempt={attemptNumberText} | " +
                $"Intent={intentName} | " +
                $"Initiative={initiativeName} | " +
                $"UseDevices={useDevicesText} | " +
                $"Policy={chooseBestText} | " +
                $"Dice=[{FormatDiceValuesForLog(result.DiceValues)}] | " +
                $"Cast={result.PatternType} | " +
                $"Score={finalScore} | " +
                $"Force={finalForce} | " +
                $"RawDamage={rawDamage} | " +
                $"CastPower={result.CastPower} | " +
                $"DiceLoadout={FormatOpponentDiceLoadoutDefinitionForLog()} | " +
                $"DeviceLoadout={FormatOpponentDeviceLoadoutForLog()}"); ;
        }

        /// <summary>상대 Roll AI의 평가 결과를 로그로 출력한다.</summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogOpponentRollAIStep(
                    string phaseName,
                    int rollIndex,
                    int maxRollCount,
                    IReadOnlyList<int> diceValues,
                    IReadOnlyList<bool> lockStates,
                    ClashCastResult candidateResult,
                    ClashCastResult playerResult)
        {
            if (!CanLogOpponentRollAIDebug())
                return;

            string candidateText = candidateResult != null
                ? $"{candidateResult.PatternType} / Damage {candidateResult.CastPower}"
                : "None";

            string playerDamageText = playerResult != null
                ? playerResult.CastPower.ToString()
                : "-";

            Debug.Log(
                $"[Tessera][OpponentRollAI] Phase={phaseName} | " +
                $"Roll={rollIndex}/{maxRollCount} | " +
                $"Intent={(currentEnemyIntentDefinition != null ? currentEnemyIntentDefinition.DisplayName : "Fallback")} | " +
                $"Strategy={ResolveOpponentRollStrategy()} | " +
                $"Dice=[{FormatDiceValuesForLog(diceValues)}] | " +
                $"Locks=[{FormatBoolListForLog(lockStates)}] | " +
                $"Candidate={candidateText} | " +
                $"PlayerDamage={playerDamageText} | " +
                $"TargetStop={(currentEnemyIntentDefinition != null ? currentEnemyIntentDefinition.TargetImpactToStop.ToString() : "0")} | " +
                $"StopIfBeatsPlayer={(currentEnemyIntentDefinition != null ? currentEnemyIntentDefinition.StopIfBeatsPlayerPower.ToString() : "False")}");
        }

        /// <summary>상대 Roll AI의 주사위 유지 결정을 로그로 출력한다.</summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogOpponentRollAILockDecision(
                    string phaseName,
                    int rollIndex,
                    OpponentRollStrategyType rollStrategy,
                    IReadOnlyList<int> diceValues,
                    IReadOnlyList<bool> lockStates,
                    ClashCastResult candidateResult)
        {
            if (!CanLogOpponentRollAIDebug())
                return;

            Debug.Log(
                $"[Tessera][OpponentRollAI][Lock] Phase={phaseName} | " +
                $"AfterRoll={rollIndex} | " +
                $"Strategy={rollStrategy} | " +
                $"Candidate={(candidateResult != null ? candidateResult.PatternType.ToString() : "None")} | " +
                $"Damage={(candidateResult != null ? candidateResult.CastPower.ToString() : "0")} | " +
                $"Dice=[{FormatDiceValuesForLog(diceValues)}] | " +
                $"NextLocks=[{FormatBoolListForLog(lockStates)}]");
        }

        /// <summary>상대 Roll AI 디버그 로그를 출력할 수 있는지 반환한다.</summary>
        private bool CanLogOpponentRollAIDebug()
        {
            return enableOpponentRollAIDebugLogs;
        }

        /// <summary>SlotPair 계산 시작 로그를 출력한다.</summary>
        private void LogSlotPairSequenceStarted(ClashCastResult result)
        {
            if (!logSlotPairEvaluationSteps)
                return;

            if (result == null || result.SlotPairDamagePreview == null)
                return;

            Debug.Log(
                $"[Tessera][SlotPair] Sequence Start | Cast={result.PatternResult.PatternType} | " +
                $"FinalScore={result.SlotPairDamagePreview.FinalScore} | " +
                $"FinalForce={result.SlotPairDamagePreview.FormatFinalForce()} | " +
                $"CastPowerBeforeRules={result.SlotPairDamagePreview.CastPowerBeforeTableRules} | " +
                $"CastPowerApplied={result.CastPower} | " +
                $"ExpectedImpact={result.ExpectedImpactDamage}");
        }

        /// <summary>SlotPair 계산 Step 로그를 출력한다.</summary>
        private void LogSlotPairStep(ClashCastResult result, SlotPairDamageStep step)
        {
            if (!logSlotPairEvaluationSteps)
                return;

            if (result == null || step == null)
                return;

            string deviceName = GetSlotPairDeviceDisplayName(ResolveDiceOwner(result), step.SlotIndex);
            string applyState = step.DidApply ? "APPLY" : "SKIP";

            Debug.Log(
                $"[Tessera][SlotPair] Step {step.SlotIndex + 1}/{SlotPairDamageCalculator.SlotPairCount} | " +
                $"State={applyState} | Cast={result.PatternResult.PatternType} | " +
                $"DiceIndex={step.DiceIndex} | DiceValue={step.DiceValue} | " +
                $"Device={deviceName} | DeviceType={step.DeviceType} | " +
                $"Score {step.ScoreBefore}->{step.ScoreAfter} | " +
                $"Force {FormatForce(step.ForceBefore)}->{FormatForce(step.ForceAfter)} | " +
                $"Message={step.Message}");
        }

        /// <summary>SlotPair 계산 종료 로그를 출력한다.</summary>
        private void LogSlotPairSequenceCompleted(ClashCastResult result)
        {
            if (!logSlotPairEvaluationSteps)
                return;

            if (result == null || result.SlotPairDamagePreview == null)
                return;

            Debug.Log(
                $"[Tessera][SlotPair] Sequence Complete | Cast={result.PatternResult.PatternType} | " +
                $"FinalScore={result.SlotPairDamagePreview.FinalScore} | " +
                $"FinalForce={result.SlotPairDamagePreview.FormatFinalForce()} | " +
                $"DamageApplied={result.CastPower}");
        }

        /// <summary>SlotPair 계산 취소 로그를 출력한다.</summary>
        private void LogSlotPairSequenceCanceled()
        {
            if (!logSlotPairEvaluationSteps)
                return;

            Debug.Log("[Tessera][SlotPair] Sequence Canceled");
        }

        /// <summary>정수 목록을 로그 출력용 문자열로 변환한다.</summary>
        private string FormatDiceValuesForLog(IReadOnlyList<int> diceValues)
        {
            if (diceValues == null || diceValues.Count <= 0)
                return "-";

            return string.Join(",", diceValues);
        }

        /// <summary>정수 리스트를 로그 표시용 문자열로 변환한다.</summary>
        private static string FormatIntListForLog(IReadOnlyList<int> values)
        {
            if (values == null || values.Count <= 0)
                return "-";

            List<string> parts = new List<string>(values.Count);

            for (int i = 0; i < values.Count; i++)
                parts.Add(values[i].ToString());

            return string.Join(", ", parts);
        }

        /// <summary>bool 목록을 로그 출력용 문자열로 변환한다.</summary>
        private static string FormatBoolListForLog(IReadOnlyList<bool> values)
        {
            if (values == null || values.Count <= 0)
                return "-";

            List<string> parts = new List<string>(values.Count);

            for (int i = 0; i < values.Count; i++)
                parts.Add(values[i] ? "L" : "R");

            return string.Join(",", parts);
        }

        /// <summary>상대 Device 로드아웃을 로그 표시용 문자열로 변환한다.</summary>
        private string FormatOpponentDeviceLoadoutForLog()
        {
            if (opponentSlotPairDevices == null || opponentSlotPairDevices.Length <= 0)
                return "-";

            List<string> parts = new List<string>(opponentSlotPairDevices.Length);

            for (int i = 0; i < opponentSlotPairDevices.Length; i++)
            {
                SlotPairDeviceDefinitionSO device = opponentSlotPairDevices[i];

                if (device == null)
                {
                    parts.Add($"{i + 1}:None");
                    continue;
                }

                string displayName = string.IsNullOrWhiteSpace(device.DisplayName)
                    ? device.DeviceType.ToString()
                    : device.DisplayName;

                parts.Add($"{i + 1}:{displayName}");
            }

            return string.Join(" | ", parts);
        }

        /// <summary>현재 상대 Dice 로드아웃 이름을 로그용 문자열로 반환한다.</summary>
        private string FormatOpponentDiceLoadoutDefinitionForLog()
        {
            EnemyDiceLoadoutDefinitionSO loadout = currentRoundDefinition != null
                ? currentRoundDefinition.OpponentDiceLoadout
                : null;

            if (loadout == null)
                return "DefaultD6";

            return loadout.DisplayName;
        }

        /// <summary>주사위 값을 표시용 문자열로 변환한다.</summary>
        private static string FormatDiceValue(SlotPairDamageStep step)
        {
            if (step.DiceIndex < 0 || step.DiceValue <= 0)
                return "-";

            return step.DiceValue.ToString();
        }

        /// <summary>Force 값을 표시용 문자열로 변환한다.</summary>
        private static string FormatForce(float force)
        {
            if (Math.Abs(force - (int)force) < 0.001f)
                return ((int)force).ToString();

            return force.ToString("0.##");
        }

        #endregion

        #region Run Info Cast Book

        /// <summary>RunInfo 족보에 표시할 Cast 타입 목록을 생성한다.</summary>
        private static RollPatternType[] CreateRunInfoCastBookPatternTypes()
        {
            return new RollPatternType[]
            {
                RollPatternType.Aces,
                RollPatternType.Twos,
                RollPatternType.Threes,
                RollPatternType.Fours,
                RollPatternType.Fives,
                RollPatternType.Sixes,
                RollPatternType.ThreeOfAKind,
                RollPatternType.FourOfAKind,
                RollPatternType.FullHouse,
                RollPatternType.SmallStraight,
                RollPatternType.LargeStraight,
                RollPatternType.Chance,
                RollPatternType.Tessera,
                RollPatternType.BrokenCast
            };
        }

                /// <summary>RunInfo 족보 캐시를 Dirty 상태로 표시한다.</summary>
        private void MarkRunInfoCastBookCacheDirty()
        {
            isRunInfoCastBookCacheDirty = true;
        }

        /// <summary>RunInfo 족보 정확 캐시 재계산을 요청한다.</summary>
        private void RequestRunInfoCastBookCacheRebuild()
        {
            if (roundState == null)
                return;

            CancelRunInfoCastBookCacheRebuild();

            runInfoCastBookCacheCts = new CancellationTokenSource();
            int rebuildSerial = ++runInfoCastBookCacheRebuildSerial;

            isRunInfoCastBookCacheRebuilding = true;
            RebuildRunInfoCastBookCacheAsync(rebuildSerial, runInfoCastBookCacheCts.Token).Forget();
        }

        /// <summary>진행 중인 RunInfo 족보 캐시 재계산을 취소한다.</summary>
        private void CancelRunInfoCastBookCacheRebuild()
        {
            if (runInfoCastBookCacheCts != null)
            {
                runInfoCastBookCacheCts.Cancel();
                runInfoCastBookCacheCts.Dispose();
                runInfoCastBookCacheCts = null;
            }

            isRunInfoCastBookCacheRebuilding = false;
        }

        /// <summary>RunInfo 족보 정확 캐시를 비동기로 재계산한다.</summary>
        private async UniTaskVoid RebuildRunInfoCastBookCacheAsync(int rebuildSerial, CancellationToken cancellationToken)
        {
            try
            {
                List<RunInfoCastBookEntrySnapshot> snapshots =
                    await BuildRunInfoCastBookSnapshotsExactAsync(cancellationToken);

                if (rebuildSerial != runInfoCastBookCacheRebuildSerial)
                    return;

                cachedRunInfoCastBookSnapshots.Clear();
                cachedRunInfoCastBookSnapshots.AddRange(snapshots);
                isRunInfoCastBookCacheDirty = false;

                PublishRunInfoCastBookSnapshotsUpdated();
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (rebuildSerial == runInfoCastBookCacheRebuildSerial)
                {
                    isRunInfoCastBookCacheRebuilding = false;

                    if (runInfoCastBookCacheCts != null)
                    {
                        runInfoCastBookCacheCts.Dispose();
                        runInfoCastBookCacheCts = null;
                    }
                }
            }
        }

        /// <summary>RunInfo 족보 정확 스냅샷을 패턴 단위로 나누어 계산한다.</summary>
        private async UniTask<List<RunInfoCastBookEntrySnapshot>> BuildRunInfoCastBookSnapshotsExactAsync(
            CancellationToken cancellationToken)
        {
            List<RunInfoCastBookEntrySnapshot> snapshots = new List<RunInfoCastBookEntrySnapshot>();

            if (roundState == null)
                return snapshots;

            PatternEvaluator patternEvaluator = PatternEvaluator.CreateDefault();
            SlotPairDamageCalculator slotPairDamageCalculator = new SlotPairDamageCalculator();
            List<SlotPairDeviceDefinition> playerDevices = CreatePlayerDeviceDefinitions();
            List<int> lockSlotDiceIndexes = CreateRunInfoDefaultLockSlotDiceIndexList();
            SlotPairCalculationContext calculationContext = CreateRunInfoSlotPairCalculationContext();
            RollPatternType[] patternTypes = CreateRunInfoCastBookPatternTypes();

            List<int> originalDiceValues = roundState.GetCurrentDiceValues();

            try
            {
                for (int i = 0; i < patternTypes.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    RollPatternType patternType = patternTypes[i];
                    bool isUnlimited = patternType == RollPatternType.BrokenCast;
                    int maxUses = ResolveRunInfoCastMaxUses(patternType);
                    int usedCount = roundState.GetPatternUseCount(patternType);
                    int remainingUses = isUnlimited ? int.MaxValue : Mathf.Max(0, maxUses - usedCount);

                    int score = 0;
                    float force = 0f;
                    int castPower = 0;

                    if (!isUnlimited)
                    {
                        TryBuildBestRunInfoCastBookPreview(
                            patternType,
                            patternEvaluator,
                            slotPairDamageCalculator,
                            playerDevices,
                            lockSlotDiceIndexes,
                            calculationContext,
                            out score,
                            out force,
                            out castPower);
                    }

                    snapshots.Add(new RunInfoCastBookEntrySnapshot(
                        patternType,
                        CastBoardCatalog.GetDisplayName(patternType),
                        score,
                        force,
                        FormatForce(force),
                        castPower,
                        remainingUses,
                        maxUses,
                        isUnlimited,
                        i));

                    if (simulator != null && originalDiceValues != null)
                        simulator.SetCurrentDiceValuesForTest(roundState, originalDiceValues);

                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }
            }
            finally
            {
                if (simulator != null && originalDiceValues != null)
                    simulator.SetCurrentDiceValuesForTest(roundState, originalDiceValues);
            }

            snapshots.Sort(CompareRunInfoCastBookSnapshots);
            return snapshots;
        }

        /// <summary>즉시 표시용 RunInfo 족보 간이 캐시를 생성한다.</summary>
        private void RefreshRunInfoCastBookFallbackCache()
        {
            List<RunInfoCastBookEntrySnapshot> fallbackSnapshots = BuildRunInfoCastBookFallbackSnapshots();

            cachedRunInfoCastBookSnapshots.Clear();
            cachedRunInfoCastBookSnapshots.AddRange(fallbackSnapshots);

            PublishRunInfoCastBookSnapshotsUpdated();
        }

        /// <summary>Canonical 주사위 조합 기준으로 RunInfo 족보 간이 스냅샷을 생성한다.</summary>
        private List<RunInfoCastBookEntrySnapshot> BuildRunInfoCastBookFallbackSnapshots()
        {
            List<RunInfoCastBookEntrySnapshot> snapshots = new List<RunInfoCastBookEntrySnapshot>();

            if (roundState == null)
                return snapshots;

            PatternEvaluator patternEvaluator = PatternEvaluator.CreateDefault();
            SlotPairDamageCalculator slotPairDamageCalculator = new SlotPairDamageCalculator();
            List<SlotPairDeviceDefinition> playerDevices = CreatePlayerDeviceDefinitions();
            List<int> lockSlotDiceIndexes = CreateRunInfoDefaultLockSlotDiceIndexList();
            SlotPairCalculationContext calculationContext = CreateRunInfoSlotPairCalculationContext();
            RollPatternType[] patternTypes = CreateRunInfoCastBookPatternTypes();

            List<int> originalDiceValues = roundState.GetCurrentDiceValues();

            try
            {
                for (int i = 0; i < patternTypes.Length; i++)
                {
                    RollPatternType patternType = patternTypes[i];
                    bool isUnlimited = patternType == RollPatternType.BrokenCast;
                    int maxUses = ResolveRunInfoCastMaxUses(patternType);
                    int usedCount = roundState.GetPatternUseCount(patternType);
                    int remainingUses = isUnlimited ? int.MaxValue : Mathf.Max(0, maxUses - usedCount);

                    int score = 0;
                    float force = 0f;
                    int castPower = 0;

                    if (!isUnlimited)
                    {
                        IReadOnlyList<int> rawDiceValues = CreateRunInfoCanonicalDiceValues(patternType);
                        IReadOnlyList<int> effectiveDiceValues = CreateRunInfoEffectiveDiceValues(rawDiceValues);

                        TryBuildRunInfoCastBookCandidate(
                            patternType,
                            patternEvaluator,
                            slotPairDamageCalculator,
                            effectiveDiceValues,
                            playerDevices,
                            lockSlotDiceIndexes,
                            calculationContext,
                            out score,
                            out force,
                            out castPower);
                    }

                    snapshots.Add(new RunInfoCastBookEntrySnapshot(
                        patternType,
                        CastBoardCatalog.GetDisplayName(patternType),
                        score,
                        force,
                        FormatForce(force),
                        castPower,
                        remainingUses,
                        maxUses,
                        isUnlimited,
                        i));
                }
            }
            finally
            {
                if (simulator != null && originalDiceValues != null)
                    simulator.SetCurrentDiceValuesForTest(roundState, originalDiceValues);
            }

            snapshots.Sort(CompareRunInfoCastBookSnapshots);
            return snapshots;
        }

        /// <summary>RunInfo 간이 표시용 대표 주사위 조합을 생성한다.</summary>
        private static IReadOnlyList<int> CreateRunInfoCanonicalDiceValues(RollPatternType patternType)
        {
            switch (patternType)
            {
                case RollPatternType.Aces:
                    return CreateRepeatedRunInfoDiceValues(1);

                case RollPatternType.Twos:
                    return CreateRepeatedRunInfoDiceValues(2);

                case RollPatternType.Threes:
                    return CreateRepeatedRunInfoDiceValues(3);

                case RollPatternType.Fours:
                    return CreateRepeatedRunInfoDiceValues(4);

                case RollPatternType.Fives:
                    return CreateRepeatedRunInfoDiceValues(5);

                case RollPatternType.Sixes:
                    return CreateRepeatedRunInfoDiceValues(6);

                case RollPatternType.ThreeOfAKind:
                    return new List<int> { 6, 6, 6, 5, 4 };

                case RollPatternType.FourOfAKind:
                    return new List<int> { 6, 6, 6, 6, 5 };

                case RollPatternType.FullHouse:
                    return new List<int> { 6, 6, 6, 5, 5 };

                case RollPatternType.SmallStraight:
                    return new List<int> { 3, 4, 5, 6, 6 };

                case RollPatternType.LargeStraight:
                    return new List<int> { 2, 3, 4, 5, 6 };

                case RollPatternType.Chance:
                    return CreateRepeatedRunInfoDiceValues(6);

                case RollPatternType.Tessera:
                    return CreateRepeatedRunInfoDiceValues(6);

                default:
                    return new List<int> { 1, 2, 3, 4, 5 };
            }
        }

        /// <summary>동일 눈금 5개로 RunInfo 대표 주사위 조합을 생성한다.</summary>
        private static IReadOnlyList<int> CreateRepeatedRunInfoDiceValues(int value)
        {
            int clampedValue = Mathf.Clamp(value, 1, 6);

            return new List<int>
            {
                clampedValue,
                clampedValue,
                clampedValue,
                clampedValue,
                clampedValue
            };
        }

        /// <summary>RunInfo 족보 캐시 갱신 이벤트를 발행한다.</summary>
        private void PublishRunInfoCastBookSnapshotsUpdated()
        {
            if (RunInfoCastBookSnapshotsUpdated == null)
                return;

            RunInfoCastBookSnapshotsUpdated.Invoke(new List<RunInfoCastBookEntrySnapshot>(cachedRunInfoCastBookSnapshots));
        }

        /// <summary>RunInfo 계산용 기본 SlotPair DiceIndex 매핑을 생성한다.</summary>
        private static List<int> CreateRunInfoDefaultLockSlotDiceIndexList()
        {
            List<int> lockSlotDiceIndexes = new List<int>(SlotPairDamageCalculator.SlotPairCount);

            for (int i = 0; i < SlotPairDamageCalculator.SlotPairCount; i++)
                lockSlotDiceIndexes.Add(i);

            return lockSlotDiceIndexes;
        }

        /// <summary>RunInfo 계산에 사용할 SlotPair 계산 컨텍스트를 생성한다.</summary>
        private SlotPairCalculationContext CreateRunInfoSlotPairCalculationContext()
        {
            int stageThreatLevel = runSession != null ? runSession.StageThreatLevel : 0;

            return new SlotPairCalculationContext(
                stageThreatLevel,
                roundState != null ? roundState.DiceTypes : null,
                roundState != null ? roundState.DiceSynergyRules : null);
        }

        /// <summary>RunInfo에서 표시할 Cast 최대 사용 횟수를 반환한다.</summary>
        private int ResolveRunInfoCastMaxUses(RollPatternType patternType)
        {
            if (patternType == RollPatternType.BrokenCast)
                return int.MaxValue;

            if (roundState == null || roundState.RuleContext == null)
                return 1;

            return Mathf.Max(1, roundState.RuleContext.MaxUsesPerCastPerRound);
        }

        /// <summary>현재 덱/Device 기준으로 특정 Cast의 최고 계산값을 탐색한다.</summary>
        private bool TryBuildBestRunInfoCastBookPreview(
            RollPatternType patternType,
            PatternEvaluator patternEvaluator,
            SlotPairDamageCalculator slotPairDamageCalculator,
            IReadOnlyList<SlotPairDeviceDefinition> playerDevices,
            IReadOnlyList<int> lockSlotDiceIndexes,
            SlotPairCalculationContext calculationContext,
            out int bestScore,
            out float bestForce,
            out int bestCastPower)
        {
            bestScore = 0;
            bestForce = 0f;
            bestCastPower = 0;

            if (roundState == null || patternEvaluator == null || slotPairDamageCalculator == null)
                return false;

            bool hasBest = false;
            List<int> rawDiceValues = new List<int>(SlotPairDamageCalculator.SlotPairCount)
            {
                1, 1, 1, 1, 1
            };

            for (int first = 1; first <= 6; first++)
            {
                rawDiceValues[0] = first;

                for (int second = 1; second <= 6; second++)
                {
                    rawDiceValues[1] = second;

                    for (int third = 1; third <= 6; third++)
                    {
                        rawDiceValues[2] = third;

                        for (int fourth = 1; fourth <= 6; fourth++)
                        {
                            rawDiceValues[3] = fourth;

                            for (int fifth = 1; fifth <= 6; fifth++)
                            {
                                rawDiceValues[4] = fifth;

                                IReadOnlyList<int> effectiveDiceValues = CreateRunInfoEffectiveDiceValues(rawDiceValues);

                                if (!TryBuildRunInfoCastBookCandidate(
                                        patternType,
                                        patternEvaluator,
                                        slotPairDamageCalculator,
                                        effectiveDiceValues,
                                        playerDevices,
                                        lockSlotDiceIndexes,
                                        calculationContext,
                                        out int candidateScore,
                                        out float candidateForce,
                                        out int candidateCastPower))
                                {
                                    continue;
                                }

                                if (!hasBest || IsBetterRunInfoCastBookCandidate(
                                        candidateScore,
                                        candidateForce,
                                        candidateCastPower,
                                        bestScore,
                                        bestForce,
                                        bestCastPower))
                                {
                                    bestScore = candidateScore;
                                    bestForce = candidateForce;
                                    bestCastPower = candidateCastPower;
                                    hasBest = true;
                                }
                            }
                        }
                    }
                }
            }

            return hasBest;
        }

        /// <summary>FaceUpgrade가 있으면 RunInfo 탐색용 주사위 값을 유효 숫자로 변환한다.</summary>
        private IReadOnlyList<int> CreateRunInfoEffectiveDiceValues(IReadOnlyList<int> rawDiceValues)
        {
            if (rawDiceValues == null)
                return null;

            if (roundState == null || !roundState.HasDiceFaceUpgrades || simulator == null)
                return rawDiceValues;

            simulator.SetCurrentDiceValuesForTest(roundState, rawDiceValues);

            List<DiceFace> effectiveFaces = roundState.GetCurrentEffectiveDiceFaces();
            List<int> effectiveValues = new List<int>(rawDiceValues.Count);

            for (int i = 0; i < rawDiceValues.Count; i++)
            {
                int fallbackValue = rawDiceValues[i];

                if (effectiveFaces == null || i >= effectiveFaces.Count || !effectiveFaces[i].IsNumber)
                {
                    effectiveValues.Add(fallbackValue);
                    continue;
                }

                int effectiveValue = effectiveFaces[i].NumberValue;

                if (effectiveValue < 1 || effectiveValue > 6)
                    effectiveValue = fallbackValue;

                effectiveValues.Add(effectiveValue);
            }

            return effectiveValues;
        }

        /// <summary>특정 주사위 배열에서 RunInfo Cast 후보 계산값을 생성한다.</summary>
        private bool TryBuildRunInfoCastBookCandidate(
            RollPatternType patternType,
            PatternEvaluator patternEvaluator,
            SlotPairDamageCalculator slotPairDamageCalculator,
            IReadOnlyList<int> diceValues,
            IReadOnlyList<SlotPairDeviceDefinition> playerDevices,
            IReadOnlyList<int> lockSlotDiceIndexes,
            SlotPairCalculationContext calculationContext,
            out int score,
            out float force,
            out int castPower)
        {
            score = 0;
            force = 0f;
            castPower = 0;

            if (diceValues == null)
                return false;

            if (!patternEvaluator.TryEvaluateSpecificPattern(diceValues, patternType, out PatternResult patternResult))
                return false;

            if (patternResult == null)
                return false;

            SlotPairDamagePreview preview = slotPairDamageCalculator.Calculate(
                patternResult,
                diceValues,
                lockSlotDiceIndexes,
                playerDevices,
                calculationContext);

            if (preview == null)
                return false;

            TableRuleEvaluationResult tableRuleResult = TableRuleEvaluator.Evaluate(
                roundState.RuleContext,
                patternType,
                preview.CastPowerBeforeTableRules);

            score = preview.FinalScore;
            force = preview.FinalForce;
            castPower = tableRuleResult != null
                ? Mathf.Max(0, tableRuleResult.ModifiedCastPower)
                : Mathf.Max(0, preview.CastPowerBeforeTableRules);

            return true;
        }

        /// <summary>RunInfo 후보 간 우선순위를 비교한다.</summary>
        private static bool IsBetterRunInfoCastBookCandidate(
            int candidateScore,
            float candidateForce,
            int candidateCastPower,
            int currentScore,
            float currentForce,
            int currentCastPower)
        {
            if (candidateCastPower != currentCastPower)
                return candidateCastPower > currentCastPower;

            if (candidateScore != currentScore)
                return candidateScore > currentScore;

            return candidateForce > currentForce;
        }

        /// <summary>RunInfo 족보 스냅샷 정렬 우선순위를 비교한다.</summary>
        private static int CompareRunInfoCastBookSnapshots(
            RunInfoCastBookEntrySnapshot left,
            RunInfoCastBookEntrySnapshot right)
        {
            if (left == null && right == null)
                return 0;

            if (left == null)
                return 1;

            if (right == null)
                return -1;

            int castPowerCompare = right.CastPower.CompareTo(left.CastPower);

            if (castPowerCompare != 0)
                return castPowerCompare;

            int scoreCompare = right.Score.CompareTo(left.Score);

            if (scoreCompare != 0)
                return scoreCompare;

            int forceCompare = right.ForceValue.CompareTo(left.ForceValue);

            if (forceCompare != 0)
                return forceCompare;

            return left.SortOrder.CompareTo(right.SortOrder);
        }

        #endregion

        #region Utility

        /// <summary>버튼 클릭 이벤트를 등록한다.</summary>
        private void AddButtonListeners()
        {
            if (diceCup3DView != null)
                diceCup3DView.Clicked += OnDiceCupClicked;

            if (submitSelectedButton != null)
                submitSelectedButton.onClick.AddListener(SubmitSelectedCast);

            if (togglePopupButton != null)
                togglePopupButton.onClick.AddListener(ToggleCastCandidatePopup);
        }

        /// <summary>버튼 클릭 이벤트를 해제한다.</summary>
        private void RemoveButtonListeners()
        {
            if (diceCup3DView != null)
                diceCup3DView.Clicked -= OnDiceCupClicked;

            if (submitSelectedButton != null)
                submitSelectedButton.onClick.RemoveListener(SubmitSelectedCast);

            if (togglePopupButton != null)
                togglePopupButton.onClick.RemoveListener(ToggleCastCandidatePopup);
        }

        /// <summary>인스펙터에 입력된 수동 주사위 값을 리스트로 만든다.</summary>
        private List<int> CreateManualDiceValues()
        {
            return new List<int>
            {
                ClampDiceValue(die1),
                ClampDiceValue(die2),
                ClampDiceValue(die3),
                ClampDiceValue(die4),
                ClampDiceValue(die5)
            };
        }

        /// <summary>주사위 값을 1~6 범위로 제한한다.</summary>
        private static int ClampDiceValue(int value)
        {
            return Mathf.Clamp(value, 1, 6);
        }

        /// <summary>TMP 텍스트 값을 안전하게 갱신한다.</summary>
        private static void SetText(TMP_Text targetText, string value)
        {
            if (targetText == null)
                return;

            targetText.text = value;
        }

        /// <summary>버튼 활성화 상태를 안전하게 갱신한다.</summary>
        private static void SetButtonInteractable(Button targetButton, bool isInteractable)
        {
            if (targetButton == null)
                return;

            targetButton.interactable = isInteractable;
        }

        #endregion

    }
}
