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

namespace Tessera.UI
{
    /// <summary>실제 플레이형 Round 진행 UI와 Core Round 상태를 연결한다.</summary>
    public class TesseraGameplayBattlePresenter : MonoBehaviour, IDeviceSlotReorderHandler
    {
        [Header("Round Rule")]
        [SerializeField] private bool useBossRule = true;
        [SerializeField] private bool useFixedSeed = true;
        [SerializeField] private int seed = 12345;

        [Header("Manual Dice")]
        [SerializeField] private bool useManualDiceValuesOnStart = true;
        [SerializeField] private bool useManualDiceValuesOnNextAttempt = true;
        [SerializeField] private int die1 = 2;
        [SerializeField] private int die2 = 2;
        [SerializeField] private int die3 = 2;
        [SerializeField] private int die4 = 5;
        [SerializeField] private int die5 = 5;

        [Header("Slot Pair Devices")]
        [SerializeField] private SlotPairDeviceDefinitionSO[] slotPairDevices = new SlotPairDeviceDefinitionSO[5];

        [Header("Legacy Canvas Device Slot Views")]
        [SerializeField] private DeviceSlotView[] deviceSlotViews = new DeviceSlotView[5]; // 기존 device slot

        [Header("3D Device Views")]
        [SerializeField] private DeviceRack3DView playerDeviceRack3DView; // 플레이어 device slot
        [SerializeField] private DeviceRack3DView opponentDeviceRack3DView; // 상대 device slot

        [Header("3D Dice Tray Views")]
        [SerializeField] private DiceTray3DView diceTray3DView; // 주사위 굴리는 tray

        [Header("Dice Cup Roll Input")]
        [SerializeField] private DiceCup3DView diceCup3DView;
        [SerializeField] private bool useDiceCupRollInput = true;
        [SerializeField] private float diceCupShakeDuration = 0.35f;
        [SerializeField] private float diceCupShakeAngle = 10f;
        [SerializeField] private float diceCupShakeFrequency = 8f;

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

        [Header("Table Hologram View")]
        [SerializeField] private bool useTableHologramView = true;
        [SerializeField] private BattleTableHologramView tableHologramView;

        [Header("Popup")]
        [SerializeField] private bool showCastCandidatePopup = true;

        [Header("Left Info Texts")]
        [SerializeField] private TMP_Text roundTitleText;
        [SerializeField] private TMP_Text opponentHpText;
        [SerializeField] private TMP_Text playerHpText;

        [Header("Dice Views")]
        [SerializeField] private DiceSlotView[] trayDiceSlots = new DiceSlotView[5];

        [Header("Cast Popup")]
        [SerializeField] private CastCandidatePopupView castCandidatePopupView;

        [Header("Buttons")]
        [SerializeField] private Button submitSelectedButton;
        [SerializeField] private Button togglePopupButton;

        [Header("Turn Flow")]
        [SerializeField] private bool autoStartNextAttemptAfterCast = true;
        [SerializeField] private float autoNextAttemptDelay = 0.35f;

        [Header("Money Overlay")]
        [SerializeField] private TMP_Text moneyText;

        private readonly int[] lockedDiceIndexBySlot = { -1, -1, -1, -1, -1 };

        private CoreRoundSimulator simulator;
        private RoundState roundState;
        private CastBoardModelBuilder castBoardModelBuilder;
        private CastBoardViewModel currentCastBoardViewModel;
        private SlotPairDamagePreview currentSlotPairPreview;
        private TableRuleEvaluationResult currentPreviewTableRuleResult;
        private RollPatternType selectedPatternType = RollPatternType.None;
        private int earnedParts;
        private TesseraRunSession runSession;
        private bool roundEndNotified;
        private CancellationTokenSource slotPairSequenceCts;
        private bool isDiceCupRollSequencePlaying;

        #region Event

        /// <summary>Round 승리 확정</summary>
        public event Action<CastSubmitResult> RoundWon;

        /// <summary>Round 패배 확정</summary>
        public event Action<CastSubmitResult> RoundLost;

        #endregion

        /// <summary>Core 시뮬레이터와 ViewModel 빌더를 준비한다.</summary>
        private void Awake()
        {
            simulator = useFixedSeed ? new CoreRoundSimulator(seed) : new CoreRoundSimulator();
            castBoardModelBuilder = CastBoardModelBuilder.CreateDefault();

            // 슬롯 클릭 콜백은 한 번만 연결하고 이후에는 내부 매핑만 갱신한다.
            InitializeDiceSlots();

            // Device 슬롯 드래그 재정렬을 위해 각 슬롯에 인덱스와 핸들러를 전달한다.
            InitializeDeviceSlots();
        }

        /// <summary>버튼 이벤트를 연결한다.</summary>
        private void OnEnable()
        {
            AddButtonListeners();
        }

        /// <summary>버튼 이벤트를 해제한다.</summary>
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

        /// <summary>씬 시작 시 테스트 Round를 시작한다.</summary>
        private void Start()
        {
            StartDebugRound();
        }

        /// <summary>새 테스트 Round를 시작한다.</summary>
        public void StartDebugRound()
        {
            RoundRuleContext ruleContext = useBossRule
                ? RoundRuleContext.CreateDebugAcesBoss()
                : RoundRuleContext.CreateDefault();

            roundState = simulator.StartRound(ruleContext);
            roundEndNotified = false;

            if (runSession == null) earnedParts = 0;

            selectedPatternType = RollPatternType.None;
            currentSlotPairPreview = null;
            currentPreviewTableRuleResult = null;

            // 새 Round에서는 이전 Lock 슬롯 배치를 모두 초기화한다.
            ClearLockSlotMapping();

            if (useManualDiceValuesOnStart)
                simulator.SetCurrentDiceValuesForTest(roundState, CreateManualDiceValues());

            SyncDevicesFromRunSession();
            CancelSlotPairEvaluationSequence();
            ClearSlotPairEvaluationHighlights();
            RefreshAll("Round started.");
        }

        /// <summary>외부 RunSession을 연결하고 장착 Device/Parts 상태를 동기화한다.</summary>
        public void BindRunSession(TesseraRunSession runSession)
        {
            this.runSession = runSession;

            SyncDevicesFromRunSession();

            if (roundState != null)
                RefreshAll(null);
            else
                RefreshDeviceSlotViews();
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

        /// <summary>DiceCup 클릭으로 Roll 연출과 Core Roll을 순차 실행한다.</summary>
        private async UniTaskVoid PlayDiceCupRollSequenceAsync()
        {
            if (isDiceCupRollSequencePlaying)
                return;

            if (!TryCanRollUnlockedDice(out string failureMessage))
            {
                SetMessage(failureMessage);
                return;
            }

            isDiceCupRollSequencePlaying = true;
            RefreshButtonStates();

            try
            {
                SetMessage("Dice cup rolling...");

                if (diceCup3DView != null)
                {
                    await diceCup3DView.PlayShakeAsync(
                        diceCupShakeDuration,
                        diceCupShakeAngle,
                        diceCupShakeFrequency,
                        this.GetCancellationTokenOnDestroy());
                }

                if (!TryCanRollUnlockedDice(out failureMessage))
                {
                    SetMessage(failureMessage);
                    return;
                }

                RollUnlockedDiceCore("Dice cup rolled unlocked dice.");
            }
            catch (OperationCanceledException)
            {
                SetMessage("Dice cup roll canceled.");
            }
            finally
            {
                isDiceCupRollSequencePlaying = false;
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

            if (!CanActInCurrentAttempt())
            {
                failureMessage = "Cannot roll now.";
                return false;
            }

            if (roundState.RemainingRoundRolls <= 0)
            {
                failureMessage = "No rolls left.";
                return false;
            }

            return true;
        }

        /// <summary>Core Roll을 실행하고 전체 표시를 갱신한다.</summary>
        private bool RollUnlockedDiceCore(string successMessage)
        {
            bool rerolled = simulator.TryRerollUnlockedDice(roundState);

            if (rerolled)
                RefreshAll(successMessage);
            else
                RefreshAll("No rolls left.");

            return rerolled;
        }

        /// <summary>현재 선택한 Cast를 SlotPair 계산값으로 제출한다.</summary>
        public void SubmitSelectedCast()
        {
            if (!CanActInCurrentAttempt())
            {
                SetMessage("Cannot submit now.");
                return;
            }

            if (selectedPatternType == RollPatternType.None)
            {
                SetMessage("Select a cast first.");
                return;
            }

            // 제출 직전 비어 있는 Lock 슬롯을 DiceIndex 오름차순으로 자동 채운다.
            EnsureAllDiceAssignedToLockSlots();

            bool submitted = simulator.TrySubmitSpecificCast(
                roundState,
                selectedPatternType,
                CreateLockSlotDiceIndexList(),
                CreateDebugDeviceDefinitions(),
                out CastSubmitResult result);

            if (!submitted)
            {
                RefreshAll("Selected cast cannot be submitted.");
                return;
            }

            // 제출 후에는 Core 결과를 그대로 보존해야 한다.
            selectedPatternType = result.PatternResult.PatternType;
            currentSlotPairPreview = result.SlotPairDamagePreview;
            currentPreviewTableRuleResult = result.TableRuleEvaluationResult;

            // RefreshAll(BuildSubmitMessage(result));
            // PlaySlotPairEvaluationSequenceAsync(result).Forget();
            // NotifyRoundEndIfNeeded(result);

            RefreshAll(BuildSubmitMessage(result));
            PlaySubmittedCastFlowAsync(result).Forget();
        }

        /// <summary>Broken Cast를 SlotPair 계산값으로 제출한다.</summary>
        public void SubmitBrokenCast()
        {
            if (!CanActInCurrentAttempt())
            {
                SetMessage("Cannot submit Broken Cast now.");
                return;
            }

            // Broken Cast도 SlotPair 연출 대상이므로 5칸을 먼저 채운다.
            EnsureAllDiceAssignedToLockSlots();

            bool submitted = simulator.TrySubmitSpecificCast(
                roundState,
                RollPatternType.BrokenCast,
                CreateLockSlotDiceIndexList(),
                CreateDebugDeviceDefinitions(),
                out CastSubmitResult result);

            if (!submitted)
            {
                RefreshAll("Broken Cast cannot be submitted.");
                return;
            }

            // 제출 후에는 Core 결과를 그대로 보존해야 한다.
            selectedPatternType = result.PatternResult.PatternType;
            currentSlotPairPreview = result.SlotPairDamagePreview;
            currentPreviewTableRuleResult = result.TableRuleEvaluationResult;

            // RefreshAll(BuildSubmitMessage(result));
            // PlaySlotPairEvaluationSequenceAsync(result).Forget();
            // NotifyRoundEndIfNeeded(result);

            RefreshAll(BuildSubmitMessage(result));
            PlaySubmittedCastFlowAsync(result).Forget();
        }

        /// <summary>다음 Attempt 시작을 시도한다.</summary>
        public void StartNextAttempt()
        {
            if (roundState == null)
            {
                SetMessage("No active round.");
                return;
            }

            bool started = simulator.TryStartNextAttempt(roundState);

            if (!started)
            {
                RefreshAll("Cannot start next attempt.");
                return;
            }

            selectedPatternType = RollPatternType.None;
            currentSlotPairPreview = null;
            currentPreviewTableRuleResult = null;

            ClearLockSlotMapping();

            for (int diceIndex = 0; diceIndex < roundState.Dice.Count; diceIndex++)
                simulator.SetDiceLocked(roundState, diceIndex, false);

            if (useManualDiceValuesOnNextAttempt)
                simulator.SetCurrentDiceValuesForTest(roundState, CreateManualDiceValues());

            CancelSlotPairEvaluationSequence();
            ClearSlotPairEvaluationHighlights();

            if (diceTray3DView != null)
                diceTray3DView.RestoreAllDiceToTray(roundState.GetCurrentDiceValues(), lockedDiceMoveDuration);

            RefreshAll("Next attempt started. Roll the dice.");
        }

        /// <summary>Cast 후보 Popup 표시 여부를 토글한다.</summary>
        public void ToggleCastCandidatePopup()
        {
            showCastCandidatePopup = !showCastCandidatePopup;

            if (castCandidatePopupView != null)
                castCandidatePopupView.SetPopupVisible(showCastCandidatePopup);

            RefreshAll(showCastCandidatePopup ? "Cast popup ON." : "Cast popup OFF.");
        }

        /// <summary>주사위 슬롯 클릭 콜백과 3D Dice 표시 인덱스를 초기화한다.</summary>
        private void InitializeDiceSlots()
        {
            if (trayDiceSlots != null)
            {
                for (int i = 0; i < trayDiceSlots.Length; i++)
                {
                    if (trayDiceSlots[i] != null)
                        trayDiceSlots[i].Initialize(i, ToggleDiceLock);
                }
            }

            // 3D DiceView 클릭도 기존 ToggleDiceLock 흐름으로 연결한다.
            if (diceTray3DView != null)
                diceTray3DView.Initialize(ToggleDiceLock);
        }

        /// <summary>Device 슬롯 드래그 재정렬과 3D 표시 인덱스를 초기화한다.</summary>
        private void InitializeDeviceSlots()
        {
            if (deviceSlotViews != null)
            {
                for (int i = 0; i < deviceSlotViews.Length; i++)
                {
                    if (deviceSlotViews[i] != null)
                        deviceSlotViews[i].Initialize(i, this);
                }
            }

            // 3D Device 슬롯은 표시용 인덱스만 초기화한다.
            if (playerDeviceRack3DView != null)
                playerDeviceRack3DView.InitializeSlots();

            if (opponentDeviceRack3DView != null)
                opponentDeviceRack3DView.InitializeSlots();
        }

        /// <summary>IDeviceSlotReorderHandler: 두 Device 슬롯의 SlotPairDeviceDefinitionSO를 교체한다.</summary>
        public void RequestDeviceSlotSwap(int sourceSlotIndex, int targetSlotIndex)
        {
            if (sourceSlotIndex == targetSlotIndex)
                return;

            if (runSession != null)
            {
                // 정식 Run에서는 RunSession이 장착 Device의 원본 데이터다.
                if (!runSession.SwapEquippedDevices(sourceSlotIndex, targetSlotIndex))
                    return;

                SyncDevicesFromRunSession();
            }
            else
            {
                // Debug 단독 실행에서는 Presenter 내부 배열만 교체한다.
                if (slotPairDevices == null) return;
                if (sourceSlotIndex < 0 || sourceSlotIndex >= slotPairDevices.Length) return;
                if (targetSlotIndex < 0 || targetSlotIndex >= slotPairDevices.Length) return;

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

        /// <summary>지정 Dice의 Lock 상태를 토글하고, Lock 순서 기반 SlotPair 매핑을 갱신한다.</summary>
        private void ToggleDiceLock(int diceIndex)
        {
            if (!CanActInCurrentAttempt())
                return;

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

        /// <summary>Popup 후보 클릭으로 제출할 Cast를 선택한다.</summary>
        private void SelectCastCandidate(RollPatternType patternType)
        {
            selectedPatternType = patternType;
            RefreshAll(null);
        }

        /// <summary>전체 UI 표시를 현재 Round 상태 기준으로 다시 그린다.</summary>
        private void RefreshAll(string optionalMessage)
        {
            if (roundState == null) return;

            currentCastBoardViewModel = castBoardModelBuilder.Build(roundState);

            // 제출 전 조작 가능한 상태에서만 자동 추천 Cast를 갱신한다.
            if (CanActInCurrentAttempt())
            {
                if (selectedPatternType == RollPatternType.None)
                    selectedPatternType = currentCastBoardViewModel.RecommendedPatternType;

                if (!CanSelectedCastStillSubmit())
                    selectedPatternType = currentCastBoardViewModel.RecommendedPatternType;

                BuildCurrentSlotPairPreview();
            }

            // 제출 후이거나 Round 종료 상태라면 방금 제출한 SlotPair 결과를 유지한다.
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
            // 이미 제출된 Attempt나 종료된 Round에서는 새 Preview를 만들면 안 된다.
            if (roundState == null || roundState.IsRoundEnded || roundState.CurrentAttempt.IsSubmitted)
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
                CreateDebugDeviceDefinitions(),
                out PatternResult patternResult,
                out currentSlotPairPreview,
                out currentPreviewTableRuleResult);
        }

        /// <summary>좌측 전투 정보 텍스트를 갱신한다.</summary>
        private void RefreshLeftInfoTexts()
        {
            SetText(roundTitleText, useBossRule ? "Boss Round" : "Round");
            SetText(opponentHpText, $"Opponent HP {roundState.Encounter.OpponentCurrentHp}/{roundState.Encounter.OpponentMaxHp}");
            SetText(playerHpText, $"Player HP {roundState.Encounter.PlayerCurrentHp}/{roundState.Encounter.PlayerMaxHp}");

            RefreshTableHologramBattleMeta();
            RefreshSelectedCastTexts();
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
                roundState.RemainingRoundRolls,
                roundState.RuleContext.RoundRollPool,
                roundState.Overcharge.CurrentOvercharge);
        }

        /// <summary>현재 Round에서 남은 Attempt 횟수를 계산한다.</summary>
        private int CalculateRemainingAttempts()
        {
            if (roundState == null)
                return 0;

            // 현재 Attempt도 아직 제출 전이면 사용 가능한 횟수에 포함한다.
            int remainingAttempts = roundState.RuleContext.MaxAttempts - roundState.CurrentAttempt.AttemptNumber + 1;

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
        private void RefreshTableHologramSlotPairStep(CastSubmitResult result, SlotPairDamageStep step)
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

        /// <summary>Tray 주사위와 Lock 표시를 갱신한다.</summary>
        private void RefreshDiceViews()
        {
            IReadOnlyList<int> diceValues = roundState.GetCurrentDiceValues();

            if (trayDiceSlots != null)
            {
                for (int i = 0; i < trayDiceSlots.Length; i++)
                {
                    if (trayDiceSlots[i] == null)
                        continue;

                    bool isLocked = i < roundState.Dice.Count && roundState.Dice[i].IsLocked;

                    // Legacy Canvas Tray는 원본 DiceIndex를 그대로 유지한다.
                    trayDiceSlots[i].BindTrayDice(i, diceValues[i], isLocked);
                }
            }

            if (diceTray3DView != null)
            {
                IReadOnlyList<bool> lockStates = CreateDiceLockStates();

                diceTray3DView.SetDiceForDeviceSlotLockPresentation(
                    diceValues,
                    lockStates,
                    lockedDiceMoveDuration);

                RefreshDeviceSlotLockedDicePresentation(false);
            }
        }

        /// <summary>Device Slot View 5개를 현재 slotPairDevices 배열 기준으로 갱신한다.</summary>
        private void RefreshDeviceSlotViews()
        {
            if (deviceSlotViews != null)
            {
                for (int i = 0; i < deviceSlotViews.Length; i++)
                {
                    if (deviceSlotViews[i] == null)
                        continue;

                    // Legacy Canvas Device 슬롯은 기존 프로토타입 호환용으로 유지한다.
                    SlotPairDeviceDefinitionSO device = GetSlotPairDeviceOrNull(i);
                    deviceSlotViews[i].SetDevice(device);
                }
            }

            // 3D 테이블 위 플레이어 Device 슬롯도 같은 장착 상태를 표시한다.
            if (playerDeviceRack3DView != null)
                playerDeviceRack3DView.SetDevices(slotPairDevices);

            // 상대 Device는 아직 Core 계산과 연결하지 않았으므로 현재는 비어 있는 상태로 표시한다.
            if (opponentDeviceRack3DView != null)
                opponentDeviceRack3DView.SetDevices((SlotPairDeviceDefinitionSO[])null);

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
            if (castCandidatePopupView == null) return;

            castCandidatePopupView.SetPopupVisible(showCastCandidatePopup);

            if (!showCastCandidatePopup) return;

            if (roundState == null || roundState.IsRoundEnded || roundState.CurrentAttempt.IsSubmitted)
                return;

            castCandidatePopupView.Refresh(
                currentCastBoardViewModel,
                selectedPatternType,
                currentCastBoardViewModel.RecommendedPatternType,
                SelectCastCandidate);
        }

        /// <summary>버튼 상호작용 가능 상태를 갱신한다.</summary>
        private void RefreshButtonStates()
        {
            bool hasRound = roundState != null;
            bool canAct = hasRound && !roundState.IsRoundEnded && !roundState.CurrentAttempt.IsSubmitted;
            bool canRoll = canAct && roundState.RemainingRoundRolls > 0 && !isDiceCupRollSequencePlaying;

            // Roll 입력은 DiceCup으로 이전한다. Legacy RollButton은 전환 기간용으로만 유지한다.
            SetButtonInteractable(submitSelectedButton, canAct && selectedPatternType != RollPatternType.None);
            SetButtonInteractable(togglePopupButton, hasRound);
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

        /// <summary>현재 Attempt에서 플레이어 조작이 가능한지 확인한다.</summary>
        private bool CanActInCurrentAttempt()
        {
            if (roundState == null)
                return false;

            if (roundState.IsRoundEnded)
                return false;

            return !roundState.CurrentAttempt.IsSubmitted;
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

        /// <summary>현재 장착된 SlotPair Device SO를 Core 계산용 정의 리스트로 변환한다.</summary>
        private List<SlotPairDeviceDefinition> CreateDebugDeviceDefinitions()
        {
            List<SlotPairDeviceDefinition> devices = new List<SlotPairDeviceDefinition>(SlotPairDamageCalculator.SlotPairCount);

            for (int i = 0; i < SlotPairDamageCalculator.SlotPairCount; i++)
                devices.Add(CreateDeviceDefinitionFromSlot(i));

            return devices;
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

        /// <summary>현재 표시할 Parts 값을 반환한다.</summary>
        private int GetCurrentParts()
        {
            if (runSession != null)
                return runSession.Parts;

            return earnedParts;
        }

        /// <summary>Round 종료 결과 이벤트를 중복 없이 외부 Root로 전달한다.</summary>
        private void NotifyRoundEndIfNeeded(CastSubmitResult result)
        {
            if (result == null)
                return;

            if (roundEndNotified)
                return;

            if (result.IsRoundWon)
            {
                roundEndNotified = true;
                RoundWon?.Invoke(result);
                return;
            }

            if (result.IsRoundLost)
            {
                roundEndNotified = true;
                RoundLost?.Invoke(result);
            }
        }

        /// <summary>Cast 제출 이후 SlotPair 연출, Round 종료 확인, 다음 Attempt 자동 진행을 순서대로 처리한다.</summary>
        private async UniTaskVoid PlaySubmittedCastFlowAsync(CastSubmitResult result)
        {
            if (result == null)
                return;

            // Cast 제출 후 계산 연출이 끝난 뒤 후속 흐름을 진행한다.
            await PlaySlotPairEvaluationSequenceAsync(result);

            NotifyRoundEndIfNeeded(result);

            if (IsRoundEndResult(result))
            {
                ForceRoundEndDiceTrayState(result);
                return;
            }

            await TryAutoStartNextAttemptAfterCastAsync(result);
        }

        /// <summary>Cast 결과 또는 현재 RoundState 기준으로 Round 종료 여부를 확인한다.</summary>
        private bool IsRoundEndResult(CastSubmitResult result)
        {
            if (result == null)
                return false;

            if (result.IsRoundWon || result.IsRoundLost)
                return true;

            return roundState != null && roundState.IsRoundEnded;
        }

        /// <summary>Round 종료 후 Dice를 Tray에 고정하고 후속 전환 대기 상태로 만든다.</summary>
        private void ForceRoundEndDiceTrayState(CastSubmitResult result)
        {
            if (roundState == null)
                return;

            CancelSlotPairEvaluationSequence();
            ClearSlotPairEvaluationHighlights();
            ClearLockSlotMapping();

            for (int diceIndex = 0; diceIndex < roundState.Dice.Count; diceIndex++)
                simulator.SetDiceLocked(roundState, diceIndex, false);

            if (diceTray3DView != null)
                diceTray3DView.RestoreAllDiceToTray(roundState.GetCurrentDiceValues(), lockedDiceMoveDuration);

            string message = result.IsRoundWon
                ? "Round won. Dice fixed to tray. Shop transition is pending."
                : "Round lost. Dice fixed to tray. Result UI is pending.";

            Debug.Log(
                $"[Tessera][RoundEnd] {message} " +
                $"OpponentHP={roundState.Encounter.OpponentCurrentHp}/{roundState.Encounter.OpponentMaxHp} | " +
                $"PlayerHP={roundState.Encounter.PlayerCurrentHp}/{roundState.Encounter.PlayerMaxHp} | " +
                $"DamageApplied={result.DamageApplied}");

            RefreshAll(message);
        }

        /// <summary>Round가 끝나지 않았으면 상대 턴 표시 후 다음 Attempt를 자동으로 시작한다.</summary>
        private async UniTask TryAutoStartNextAttemptAfterCastAsync(CastSubmitResult result)
        {
            if (!autoStartNextAttemptAfterCast)
                return;

            if (result == null)
                return;

            if (roundState == null)
                return;

            if (result.IsRoundWon)
            {
                RefreshAll("Round won.");
                return;
            }

            if (result.IsRoundLost)
            {
                RefreshAll("Round lost.");
                return;
            }

            if (roundState.IsRoundEnded)
            {
                RefreshAll("Round ended.");
                return;
            }

            if (!roundState.CurrentAttempt.IsSubmitted)
                return;

            if (result.EnemyIntentResult.DidExecute)
                SetMessage($"Enemy turn: player took {result.EnemyIntentResult.DamageToPlayer} damage.");
            else
                SetMessage("Enemy turn: no action.");

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

        /// <summary>Cast 제출 후 SlotPair 계산 순서 연출을 비동기로 재생한다.</summary>
        private async UniTask PlaySlotPairEvaluationSequenceAsync(CastSubmitResult result)
        {
            if (!playSlotPairSequenceOnSubmit) return;
            if (result == null) return;
            if (result.SlotPairDamagePreview == null) return;

            // 이전 SlotPair 연출이 남아 있으면 중단하고 새 연출을 시작한다.
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
                // 새 라운드/시도 전환 시 정상 취소된다.
                LogSlotPairSequenceCanceled();
            }
            finally
            {
                if (slotPairSequenceCts == currentCts)
                {
                    slotPairSequenceCts = null;

                    if (completed)
                        RestoreAllDiceToTrayAfterEvaluation();
                    else
                        RefreshDeviceSlotLockedDicePresentation();

                    ClearSlotPairEvaluationHighlights();

                    if (completed)
                        RefreshSelectedCastTexts();
                }

                currentCts.Dispose();
            }
        }

        /// <summary>SlotPairDamagePreview의 Step 목록을 기준으로 DeviceSlot과 Dice 반응 연출을 순차 재생한다.</summary>
        private async UniTask PlaySlotPairEvaluationSequenceInternalAsync(
            CastSubmitResult result,
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

        /// <summary>제출 결과 메시지를 생성한다.</summary>
        private string BuildSubmitMessage(CastSubmitResult result)
        {
            if (result == null)
                return "Submit failed.";

            if (result.IsRoundWon && runSession == null)
                earnedParts += 20;

            string enemyMessage = result.EnemyIntentResult.DidExecute
                ? $" Enemy hit {result.EnemyIntentResult.DamageToPlayer}."
                : " Enemy did not act.";

            if (result.SlotPairDamagePreview != null)
                return $"{result.PatternResult.PatternType}: Score {result.SlotPairDamagePreview.FinalScore} x Force {result.SlotPairDamagePreview.FormatFinalForce()} = Damage {result.DamageApplied}.{enemyMessage}";

            return $"{result.PatternResult.PatternType} dealt {result.DamageApplied}.{enemyMessage}";
        }

        /// <summary>전투 진행 메시지를 로그로 남긴다.</summary>
        private void SetMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            Debug.Log($"[Tessera][Message] {message}");
        }

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

        /// <summary>현재 주사위들의 Lock 상태 목록을 생성한다.</summary>
        private List<bool> CreateDiceLockStates()
        {
            List<bool> lockStates = new List<bool>(roundState.Dice.Count);

            for (int i = 0; i < roundState.Dice.Count; i++)
                lockStates.Add(roundState.Dice[i].IsLocked);

            return lockStates;
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

        #region Debug

        /// <summary>SlotPair 계산 시작 로그를 출력한다.</summary>
        private void LogSlotPairSequenceStarted(CastSubmitResult result)
        {
            if (!logSlotPairEvaluationSteps)
                return;

            if (result == null || result.SlotPairDamagePreview == null)
                return;

            Debug.Log(
                $"[Tessera][SlotPair] Sequence Start | Cast={result.PatternResult.PatternType} | " +
                $"FinalScore={result.SlotPairDamagePreview.FinalScore} | " +
                $"FinalForce={result.SlotPairDamagePreview.FormatFinalForce()} | " +
                $"DamageBeforeRules={result.SlotPairDamagePreview.DamageBeforeTableRules} | " +
                $"DamageApplied={result.DamageApplied}");
        }

        /// <summary>SlotPair 계산 Step 로그를 출력한다.</summary>
        private void LogSlotPairStep(CastSubmitResult result, SlotPairDamageStep step)
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
        private void LogSlotPairSequenceCompleted(CastSubmitResult result)
        {
            if (!logSlotPairEvaluationSteps)
                return;

            if (result == null || result.SlotPairDamagePreview == null)
                return;

            Debug.Log(
                $"[Tessera][SlotPair] Sequence Complete | Cast={result.PatternResult.PatternType} | " +
                $"FinalScore={result.SlotPairDamagePreview.FinalScore} | " +
                $"FinalForce={result.SlotPairDamagePreview.FormatFinalForce()} | " +
                $"DamageApplied={result.DamageApplied}");
        }

        /// <summary>SlotPair 계산 취소 로그를 출력한다.</summary>
        private void LogSlotPairSequenceCanceled()
        {
            if (!logSlotPairEvaluationSteps)
                return;

            Debug.Log("[Tessera][SlotPair] Sequence Canceled");
        }

        #endregion
    }
}
