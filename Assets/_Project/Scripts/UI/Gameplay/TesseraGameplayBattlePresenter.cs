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
        [SerializeField] private bool useDeterministicCombatSeed = false;
        [SerializeField] private int debugCombatSeed = 12345;
        [SerializeField] private bool logCombatSeed;
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

        [Header("SlotPair Floating Text")]
        [SerializeField] private bool playSlotPairFloatingText = true;
        [SerializeField] private SlotPairStepFloatingTextView slotPairStepFloatingTextView;
        [SerializeField] private RectTransform slotPairFloatingTextRoot;
        [SerializeField] private Camera battleCamera;
        [SerializeField] private Vector3 slotPairFloatingWorldOffset = new Vector3(0f, 0f, -0.3f);

        [Header("DeviceSlot Lock Dice Presentation")]
        [SerializeField] private Vector3 lockedDiceDeviceSlotLocalOffset = new Vector3(0f, 0f, 0.25f);
        [SerializeField] private Vector3 lockedDiceTiltEuler = new Vector3(0f, 45f, 0f);
        [SerializeField] private float lockedDiceMoveDuration = 0.16f;
        [SerializeField] private bool restoreDiceToTrayAfterEvaluation = true;

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

        private readonly int[] lockedDiceIndexBySlot = { -1, -1, -1, -1, -1 };
        private string currentRoundDisplayName = "Round";

        private CoreRoundSimulator simulator;
        private RoundState roundState;
        private CastBoardModelBuilder castBoardModelBuilder;
        private CastBoardViewModel currentCastBoardViewModel;
        private SlotPairDamagePreview currentSlotPairPreview;
        private TableRuleEvaluationResult currentPreviewTableRuleResult;
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

        #endregion

        #region Events And Properties

        /// <summary>Round 승리 확정</summary>
        public event Action<ClashResolveResult> RoundWon;

        /// <summary>Round 패배 확정</summary>
        public event Action<ClashResolveResult> RoundLost;

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
        }

        /// <summary>Inspector 배열 길이를 고정 슬롯 수에 맞게 보정한다.</summary>
        private void OnValidate()
        {
            if (slotPairDevices == null || slotPairDevices.Length != SlotPairDamageCalculator.SlotPairCount)
                slotPairDevices = ResizeDeviceArray(slotPairDevices, SlotPairDamageCalculator.SlotPairCount);

            if (opponentSlotPairDevices == null || opponentSlotPairDevices.Length != SlotPairDamageCalculator.SlotPairCount)
                opponentSlotPairDevices = ResizeDeviceArray(opponentSlotPairDevices, SlotPairDamageCalculator.SlotPairCount);
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
            EnemyIntent openingIntent)
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
            roundState = simulator.StartRound(ruleContext, carriedPlayerHP, stageOverchargeState);
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
            currentPreviewTableRuleResult = null;

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

            if (roundState != null)
                RefreshAll(null);
            else
                RefreshDeviceSlotViews();
        }

        /// <summary>RunSession의 현재 장착 Device를 Presenter Debug Mirror와 DeviceSlot View에 다시 반영한다.</summary>
        public void RefreshEquippedDevicesFromRunSession(string reason)
        {
            SyncDevicesFromRunSession();

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
            currentPreviewTableRuleResult = playerResult.TableRuleEvaluationResult;

            RefreshClashPowerTexts();
            RefreshAll(BuildClashCastMessage(playerResult));
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
            currentPreviewTableRuleResult = playerResult.TableRuleEvaluationResult;

            RefreshClashPowerTexts();
            RefreshAll(BuildClashCastMessage(playerResult));
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
            currentPreviewTableRuleResult = null;
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
        private void ResolveAndStoreRoundInitiativeOwner(EnemyIntent openingIntent)
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
        private void ApplyOpeningIntentToCurrentAttempt(EnemyIntent openingIntent)
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
            if (slotPairDevices == null)
                return "No Device";

            if (slotIndex < 0 || slotIndex >= slotPairDevices.Length)
                return "No Device";

            SlotPairDeviceDefinitionSO device = slotPairDevices[slotIndex];

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
            currentPreviewTableRuleResult = null;

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
            string forceValue = currentSlotPairPreview.FormatFinalForce();

            RefreshTableHologramCastPreview(
                castName,
                currentSlotPairPreview.FinalScore,
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
                    ? Mathf.Max(0, currentPreviewTableRuleResult.ModifiedCastPower)
                    : 0;

                opponentCastPower = pendingOpponentClashResult != null
                    ? pendingOpponentClashResult.CastPower
                    : 0;
            }

            if (useTableHologramView && tableHologramView != null)
                tableHologramView.RefreshClashPower(playerCastPower, opponentCastPower);
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
        private void RefreshTableHologramSlotPairStep(ClashCastResult result, SlotPairDamageStep step)
        {
            if (!useTableHologramView) return;
            if (tableHologramView == null) return;
            if (result == null || step == null) return;

            // SlotPair 계산 중에는 현재 단계 이후의 Score / Force 값을 표시한다.
            tableHologramView.RefreshSlotPairStep(
                step.SlotIndex,
                SlotPairDamageCalculator.SlotPairCount,
                CastBoardCatalog.GetDisplayName(result.PatternResult.PatternType),
                step.ScoreAfter,
                FormatForce(step.ForceAfter));
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

        /// <summary>DeviceSlot 하단 Lock Dice 표시 위치와 회전을 계산한다.</summary>
        private bool TryGetLockedDiceDeviceSlotPose(
            int slotIndex,
            out Vector3 worldPosition,
            out Quaternion worldRotation)
        {
            return TryGetDeviceSlotPresentationPose(
                slotIndex,
                lockedDiceDeviceSlotLocalOffset,
                Quaternion.Euler(lockedDiceTiltEuler),
                out worldPosition,
                out worldRotation);
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

        /// <summary>지정 SlotPair 인덱스의 Player DeviceSlot을 강조한다.</summary>
        private void HighlightSlotPair(int slotIndex)
        {
            // 새 구조에서는 DeviceSlot 하단 Dice와 DeviceSlot 자체가 LockSlot 역할을 겸한다.
            if (playerDeviceRack3DView != null)
                playerDeviceRack3DView.HighlightSlot(slotIndex);
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
            if (!playDiceJumpRollDuringEvaluation) return;
            if (step == null) return;
            if (step.DiceIndex < 0) return;
            if (diceTray3DView == null) return;

            // 이미 DeviceSlot 하단에 배치된 DiceView를 제자리에서 반응시킨다.
            await diceTray3DView.PlayDiceJumpRollAsync(
                step.DiceIndex,
                slotPairDiceJumpHeight,
                slotPairDiceJumpRollEuler,
                slotPairDiceJumpRollDuration,
                cancellationToken);
        }

        /// <summary>DeviceSlot 기준 Presentation 위치와 회전을 계산한다.</summary>
        private bool TryGetDeviceSlotPresentationPose(
            int slotIndex,
            Vector3 localPresentationOffset,
            Quaternion localRotationOffset,
            out Vector3 worldPosition,
            out Quaternion worldRotation)
        {
            worldPosition = Vector3.zero;
            worldRotation = Quaternion.identity;

            if (playerDeviceRack3DView == null)
                return false;

            Camera targetCamera = battleCamera != null ? battleCamera : Camera.main;

            if (!playerDeviceRack3DView.TryGetSlotPresentationBasis(
                    slotIndex,
                    targetCamera,
                    out Vector3 slotCenter,
                    out Vector3 slotRight,
                    out Vector3 slotUp,
                    out Vector3 towardPlayer))
                return false;

            // 기존 WorldOffset 필드명을 유지하되 x=slotRight, y=slotUp, z=towardPlayer 로컬 offset으로 해석한다.
            worldPosition = slotCenter
                            + slotRight * localPresentationOffset.x
                            + slotUp * localPresentationOffset.y
                            + towardPlayer * localPresentationOffset.z;

            worldRotation = Quaternion.LookRotation(towardPlayer, slotUp) * localRotationOffset;
            return true;
        }

        /// <summary>SlotPair 연산 연출 후 모든 DiceView를 DiceTray 위치로 복귀시킨다.</summary>
        private void RestoreEvaluationDicePlacement()
        {
            RestoreAllDiceToTrayAfterEvaluation();
        }

        /// <summary>연산 완료 후 모든 DiceView를 DiceTray 원래 위치로 복귀시킨다.</summary>
        private void RestoreAllDiceToTrayAfterEvaluation()
        {
            if (!restoreDiceToTrayAfterEvaluation)
                return;

            if (diceTray3DView == null)
                return;

            if (roundState == null)
                return;

            IReadOnlyList<int> diceValues = roundState.GetCurrentDiceValues();

            // 상대 턴/다음 흐름으로 넘어가기 전 Dice를 모두 Tray에 정리한다.
            diceTray3DView.RestoreAllDiceToTray(diceValues, lockedDiceMoveDuration);
        }

        #endregion

        #region SlotPair Evaluation Presentation

        /// <summary>Cast 제출 후 SlotPair 계산 순서 연출을 비동기로 재생한다.</summary>
        private async UniTask PlaySlotPairEvaluationSequenceAsync(ClashCastResult result)
        {
            if (!playSlotPairSequenceOnSubmit) return;
            if (result == null) return;
            if (result.SlotPairDamagePreview == null) return;

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
                        if (interactionState == BattleInteractionState.CastResolving)
                            RefreshDeviceSlotLockedDicePresentation();
                        else
                            RestoreAllDiceToTrayAfterEvaluation();
                    }
                    else
                    {
                        RefreshDeviceSlotLockedDicePresentation();
                    }

                    ClearSlotPairEvaluationHighlights();

                    if (completed)
                        RefreshSelectedCastTexts();
                }

                currentCts.Dispose();
            }
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

            for (int i = 0; i < stepCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                SlotPairDamageStep step = steps[i];

                // 현재 DiceIndex와 대응 PlayerDeviceSlot을 계산 대상으로 강조한다.
                HighlightSlotPair(step.SlotIndex);

                RefreshTableHologramSlotPairStep(result, step);
                LogSlotPairStep(result, step);

                PlayEvaluationDiceJumpRollAsync(step, cancellationToken).Forget();
                PlaySlotPairFloatingTextAsync(step, cancellationToken).Forget();

                if (slotPairHighlightDuration > 0f)
                    await UniTask.Delay(TimeSpan.FromSeconds(slotPairHighlightDuration), cancellationToken: cancellationToken);

                ClearSlotPairEvaluationHighlights();

                if (slotPairHighlightGap > 0f)
                    await UniTask.Delay(TimeSpan.FromSeconds(slotPairHighlightGap), cancellationToken: cancellationToken);
            }
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
            if (!playSlotPairFloatingText) return;
            if (step == null) return;
            if (slotPairStepFloatingTextView == null) return;
            if (!TryGetSlotPairFloatingAnchoredPosition(step.SlotIndex, out Vector2 anchoredPosition)) return;

            string message = BuildSlotPairFloatingMessage(step);

            if (string.IsNullOrWhiteSpace(message)) return;

            // 현재 Highlight 중인 DeviceSlot 위에 Balatro식 짧은 변화량 텍스트를 띄운다.
            await slotPairStepFloatingTextView.PlayAsync(message, anchoredPosition, cancellationToken);
        }

        /// <summary>SlotPair Floating Text를 띄울 DeviceSlot 기준 Overlay 좌표를 계산한다.</summary>
        private bool TryGetSlotPairFloatingAnchoredPosition(int slotIndex, out Vector2 anchoredPosition)
        {
            anchoredPosition = Vector2.zero;

            if (playerDeviceRack3DView == null)
                return false;

            RectTransform root = slotPairFloatingTextRoot;

            if (root == null && slotPairStepFloatingTextView != null)
                root = slotPairStepFloatingTextView.transform.parent as RectTransform;

            if (root == null)
                return false;

            Camera targetCamera = battleCamera != null ? battleCamera : Camera.main;

            if (targetCamera == null)
                return false;

            if (!TryGetDeviceSlotPresentationPose(
                    slotIndex,
                    slotPairFloatingWorldOffset,
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

                ApplyOpponentRollStrategyLocks(enemyDiceValues, candidateResult, lockStates, rollStrategy);
                ApplyLockStatesToOpponentDiceInstances(opponentDiceInstances, lockStates);

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

        /// <summary>상대가 현재 Attempt에서 사용할 수 있는 기본 Roll 횟수를 반환한다.</summary>
        private int ResolveOpponentRollCount()
        {
            return RoundState.BaseRollsPerAttempt;
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

        /// <summary>Clash Cast 결과 메시지를 생성한다.</summary>
        private string BuildClashCastMessage(ClashCastResult result)
        {
            if (result == null)
                return "Submit failed.";

            if (result.SlotPairDamagePreview != null)
            {
                return
                    $"{result.PatternResult.PatternType}: " +
                    $"Score {result.SlotPairDamagePreview.FinalScore} x " +
                    $"Force {result.SlotPairDamagePreview.FormatFinalForce()} = " +
                    $"Power {result.CastPower} / Expected Impact {result.ExpectedImpactDamage}.";
            }

            return $"{result.PatternResult.PatternType}: Power {result.CastPower} / Expected Impact {result.ExpectedImpactDamage}.";
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

            string deviceName = GetSlotPairDeviceDisplayName(step.SlotIndex);
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
