using System.Collections.Generic;
using Tessera.Core;
using Tessera.Data;
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

        [Header("Device Slot Views")]
        [SerializeField] private DeviceSlotView[] deviceSlotViews = new DeviceSlotView[5];

        [Header("Popup")]
        [SerializeField] private bool showCastCandidatePopup = true;

        [Header("Left Info Texts")]
        [SerializeField] private TMP_Text roundTitleText;
        [SerializeField] private TMP_Text opponentHpText;
        [SerializeField] private TMP_Text playerHpText;
        [SerializeField] private TMP_Text attemptText;
        [SerializeField] private TMP_Text rollText;
        [SerializeField] private TMP_Text overchargeText;
        [SerializeField] private TMP_Text enemyIntentText;
        [SerializeField] private TMP_Text partsText;
        [SerializeField] private TMP_Text selectedCastText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text forceText;
        [SerializeField] private TMP_Text previewDamageText;
        [SerializeField] private TMP_Text messageText;

        [Header("Dice Views")]
        [SerializeField] private DiceSlotView[] trayDiceSlots = new DiceSlotView[5];
        [SerializeField] private DiceSlotView[] lockDiceSlots = new DiceSlotView[5];

        [Header("Cast Popup")]
        [SerializeField] private CastCandidatePopupView castCandidatePopupView;

        [Header("Buttons")]
        [SerializeField] private Button rollButton;
        [SerializeField] private Button submitSelectedButton;
        [SerializeField] private Button submitBrokenCastButton;
        [SerializeField] private Button nextAttemptButton;
        [SerializeField] private Button resetRoundButton;
        [SerializeField] private Button togglePopupButton;

        private readonly int[] lockedDiceIndexBySlot = { -1, -1, -1, -1, -1 };

        private CoreRoundSimulator simulator;
        private RoundState roundState;
        private CastBoardModelBuilder castBoardModelBuilder;
        private CastBoardViewModel currentCastBoardViewModel;
        private SlotPairDamagePreview currentSlotPairPreview;
        private TableRuleEvaluationResult currentPreviewTableRuleResult;
        private RollPatternType selectedPatternType = RollPatternType.None;
        private int earnedParts;

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
            earnedParts = 0;
            selectedPatternType = RollPatternType.None;
            currentSlotPairPreview = null;
            currentPreviewTableRuleResult = null;

            // 새 Round에서는 이전 Lock 슬롯 배치를 모두 초기화한다.
            ClearLockSlotMapping();

            if (useManualDiceValuesOnStart)
                simulator.SetCurrentDiceValuesForTest(roundState, CreateManualDiceValues());

            RefreshAll("Round started.");
        }

        /// <summary>잠기지 않은 주사위를 다시 굴린다.</summary>
        public void RollUnlockedDice()
        {
            if (!CanActInCurrentAttempt())
            {
                SetMessage("Cannot roll now.");
                return;
            }

            bool rerolled = simulator.TryRerollUnlockedDice(roundState);

            if (rerolled)
                RefreshAll("Unlocked dice rolled.");
            else
                RefreshAll("No rolls left.");
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

            RefreshAll(BuildSubmitMessage(result));
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

            RefreshAll(BuildSubmitMessage(result));
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

            // 새 Attempt의 주사위는 새 객체이므로 Lock 슬롯 매핑을 초기화한다.
            ClearLockSlotMapping();

            if (useManualDiceValuesOnNextAttempt)
                simulator.SetCurrentDiceValuesForTest(roundState, CreateManualDiceValues());

            RefreshAll("Next attempt started.");
        }

        /// <summary>Cast 후보 Popup 표시 여부를 토글한다.</summary>
        public void ToggleCastCandidatePopup()
        {
            showCastCandidatePopup = !showCastCandidatePopup;

            if (castCandidatePopupView != null)
                castCandidatePopupView.SetPopupVisible(showCastCandidatePopup);

            RefreshAll(showCastCandidatePopup ? "Cast popup ON." : "Cast popup OFF.");
        }

        /// <summary>주사위 슬롯 클릭 콜백을 등록한다.</summary>
        private void InitializeDiceSlots()
        {
            for (int i = 0; i < trayDiceSlots.Length; i++)
            {
                if (trayDiceSlots[i] != null)
                    trayDiceSlots[i].Initialize(i, ToggleDiceLock);
            }

            for (int i = 0; i < lockDiceSlots.Length; i++)
            {
                if (lockDiceSlots[i] != null)
                    lockDiceSlots[i].Initialize(-1, ToggleDiceLock);
            }
        }

        /// <summary>Device 슬롯 드래그 재정렬을 위해 각 슬롯에 인덱스와 핸들러를 전달한다.</summary>
        private void InitializeDeviceSlots()
        {
            if (deviceSlotViews == null)
                return;

            for (int i = 0; i < deviceSlotViews.Length; i++)
            {
                if (deviceSlotViews[i] != null)
                    deviceSlotViews[i].Initialize(i, this);
            }
        }

        /// <summary>IDeviceSlotReorderHandler: 두 Device 슬롯의 SlotPairDeviceDefinitionSO를 교체한다.</summary>
        public void RequestDeviceSlotSwap(int sourceSlotIndex, int targetSlotIndex)
        {
            if (slotPairDevices == null)
                return;

            if (sourceSlotIndex < 0 || sourceSlotIndex >= slotPairDevices.Length)
                return;

            if (targetSlotIndex < 0 || targetSlotIndex >= slotPairDevices.Length)
                return;

            if (sourceSlotIndex == targetSlotIndex)
                return;

            // 두 슬롯의 Device SO를 맞바꾼다.
            SlotPairDeviceDefinitionSO temp = slotPairDevices[sourceSlotIndex];
            slotPairDevices[sourceSlotIndex] = slotPairDevices[targetSlotIndex];
            slotPairDevices[targetSlotIndex] = temp;

            // Swap 이후 전체 UI를 현재 데이터 기준으로 다시 그린다.
            // Round, Dice Lock, Cast 선택 상태는 변경하지 않는다.
            RefreshAll("Device order changed.");
        }

        /// <summary>주사위 Lock 상태를 전환하고 Lock 슬롯 매핑을 갱신한다.</summary>
        private void ToggleDiceLock(int diceIndex)

        {
            if (!CanActInCurrentAttempt()) return;
            if (diceIndex < 0 || diceIndex >= roundState.Dice.Count) return;

            bool willLock = !roundState.Dice[diceIndex].IsLocked;

            // Core Lock 상태를 먼저 갱신한다.
            simulator.ToggleDiceLock(roundState, diceIndex);

            if (willLock)
                AssignDiceToFirstEmptyLockSlot(diceIndex);
            else
                RemoveDiceFromLockSlot(diceIndex);

            RefreshAll("Dice lock changed.");
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
            SetText(attemptText, $"Attempt {roundState.CurrentAttempt.AttemptNumber}/{roundState.RuleContext.MaxAttempts}");
            SetText(rollText, $"Rolls {roundState.RemainingRoundRolls}/{roundState.RuleContext.RoundRollPool}");
            SetText(overchargeText, $"Overcharge {roundState.Overcharge.CurrentOvercharge}");
            SetText(enemyIntentText, $"Intent {roundState.CurrentEnemyIntent.IntentType} {roundState.CurrentEnemyIntent.Damage}");
            SetText(partsText, $"Parts {earnedParts}");

            RefreshSelectedCastTexts();
        }

        /// <summary>선택 Cast와 SlotPair 계산 미리보기 텍스트를 갱신한다.</summary>
        private void RefreshSelectedCastTexts()
        {
            if (selectedPatternType == RollPatternType.None || currentSlotPairPreview == null)
            {
                SetText(selectedCastText, "Cast -");
                SetText(scoreText, "Score -");
                SetText(forceText, "Force -");
                SetText(previewDamageText, "Damage -");
                return;
            }

            int damageAfterRules = currentPreviewTableRuleResult != null
                ? currentPreviewTableRuleResult.ModifiedDamage
                : currentSlotPairPreview.DamageBeforeTableRules;

            SetText(selectedCastText, $"Cast {CastBoardCatalog.GetDisplayName(selectedPatternType)}");
            SetText(scoreText, $"Score {currentSlotPairPreview.FinalScore}");
            SetText(forceText, $"Force x{currentSlotPairPreview.FormatFinalForce()}");
            SetText(previewDamageText, $"Damage {damageAfterRules}");
        }

        /// <summary>Tray 주사위와 Lock 슬롯 표시를 갱신한다.</summary>
        private void RefreshDiceViews()
        {
            IReadOnlyList<int> diceValues = roundState.GetCurrentDiceValues();

            for (int i = 0; i < trayDiceSlots.Length; i++)
            {
                if (trayDiceSlots[i] == null) continue;

                bool isLocked = i < roundState.Dice.Count && roundState.Dice[i].IsLocked;

                // Tray는 원본 DiceIndex를 그대로 유지한다.
                trayDiceSlots[i].BindTrayDice(i, diceValues[i], isLocked);
            }

            RefreshLockSlotViews(diceValues);
        }

        /// <summary>상단 Device Slot View 5개를 현재 slotPairDevices 배열 기준으로 갱신한다.</summary>
        private void RefreshDeviceSlotViews()
        {
            if (deviceSlotViews == null) return;

            for (int i = 0; i < deviceSlotViews.Length; i++)
            {
                if (deviceSlotViews[i] == null)
                    continue;

                // slotPairDevices가 없거나 범위를 벗어나면 Empty 슬롯으로 표시한다.
                SlotPairDeviceDefinitionSO device = null;

                if (slotPairDevices != null && i >= 0 && i < slotPairDevices.Length)
                    device = slotPairDevices[i];

                deviceSlotViews[i].SetDevice(device);
            }
        }

        /// <summary>Lock 슬롯을 현재 매핑 순서대로 갱신한다.</summary>
        private void RefreshLockSlotViews(IReadOnlyList<int> diceValues)
        {
            RepairInvalidLockSlotMapping();

            for (int slotIndex = 0; slotIndex < lockDiceSlots.Length; slotIndex++)
            {
                if (lockDiceSlots[slotIndex] == null)
                    continue;

                int diceIndex = lockedDiceIndexBySlot[slotIndex];

                if (diceIndex < 0)
                {
                    lockDiceSlots[slotIndex].BindEmpty();
                    continue;
                }

                // Lock 슬롯은 원본 DiceIndex를 보관해야 클릭 시 정확히 Unlock된다.
                lockDiceSlots[slotIndex].BindLockedDice(diceIndex, diceValues[diceIndex]);
            }
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
            bool canNext = hasRound && !roundState.IsRoundEnded && roundState.CurrentAttempt.IsSubmitted;

            SetButtonInteractable(rollButton, canAct && roundState.RemainingRoundRolls > 0);
            SetButtonInteractable(submitSelectedButton, canAct && selectedPatternType != RollPatternType.None);
            SetButtonInteractable(submitBrokenCastButton, canAct);
            SetButtonInteractable(nextAttemptButton, canNext);
            SetButtonInteractable(resetRoundButton, true);
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

        /// <summary>제출 직전 모든 주사위를 LockSlot 5칸에 배치한다.</summary>
        private void EnsureAllDiceAssignedToLockSlots()
        {
            RepairInvalidLockSlotMapping();

            for (int diceIndex = 0; diceIndex < roundState.Dice.Count; diceIndex++)
            {
                if (FindLockSlotIndexByDiceIndex(diceIndex) >= 0)
                    continue;

                int emptySlotIndex = FindFirstEmptyLockSlotIndex();

                if (emptySlotIndex < 0)
                    break;

                // 비어 있는 슬롯은 DiceIndex 오름차순으로 자동 채운다.
                lockedDiceIndexBySlot[emptySlotIndex] = diceIndex;
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

        /// <summary>주사위를 첫 번째 빈 Lock 슬롯에 배치한다.</summary>
        private void AssignDiceToFirstEmptyLockSlot(int diceIndex)
        {
            if (FindLockSlotIndexByDiceIndex(diceIndex) >= 0)
                return;

            int emptySlotIndex = FindFirstEmptyLockSlotIndex();

            if (emptySlotIndex < 0)
                return;

            lockedDiceIndexBySlot[emptySlotIndex] = diceIndex;
        }

        /// <summary>주사위가 들어가 있는 Lock 슬롯만 비운다.</summary>
        private void RemoveDiceFromLockSlot(int diceIndex)
        {
            int slotIndex = FindLockSlotIndexByDiceIndex(diceIndex);

            if (slotIndex < 0)
                return;

            // 슬롯은 당겨지지 않고 해당 자리만 빈 칸으로 남는다.
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

        /// <summary>Core의 Lock 상태와 UI Lock 슬롯 매핑이 어긋난 경우를 정리한다.</summary>
        private void RepairInvalidLockSlotMapping()
        {
            for (int slotIndex = 0; slotIndex < lockedDiceIndexBySlot.Length; slotIndex++)
            {
                int diceIndex = lockedDiceIndexBySlot[slotIndex];

                if (diceIndex < 0)
                    continue;

                if (diceIndex >= roundState.Dice.Count)
                {
                    lockedDiceIndexBySlot[slotIndex] = -1;
                    continue;
                }

                // Core 기준으로 Unlock된 주사위는 UI 슬롯에서도 제거한다.
                if (!roundState.Dice[diceIndex].IsLocked)
                    lockedDiceIndexBySlot[slotIndex] = -1;
            }

            for (int diceIndex = 0; diceIndex < roundState.Dice.Count; diceIndex++)
            {
                if (!roundState.Dice[diceIndex].IsLocked)
                    continue;

                if (FindLockSlotIndexByDiceIndex(diceIndex) >= 0)
                    continue;

                // Core에는 Lock인데 UI 매핑이 없으면 가장 앞 빈 슬롯에 복구한다.
                AssignDiceToFirstEmptyLockSlot(diceIndex);
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

        /// <summary>미리보기용으로 자동 채움까지 반영한 LockSlot 매핑을 반환한다.</summary>
        private List<int> CreatePreviewResolvedLockSlotDiceIndexList()
        {
            List<int> result = CreateLockSlotDiceIndexList();

            for (int diceIndex = 0; diceIndex < roundState.Dice.Count; diceIndex++)
            {
                if (ContainsDiceIndex(result, diceIndex))
                    continue;

                int emptySlotIndex = FindFirstEmptyIndex(result);

                if (emptySlotIndex < 0)
                    break;

                result[emptySlotIndex] = diceIndex;
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

        /// <summary>제출 결과 메시지를 생성한다.</summary>
        private string BuildSubmitMessage(CastSubmitResult result)
        {
            if (result == null)
                return "Submit failed.";

            if (result.IsRoundWon)
                earnedParts += 20;

            string enemyMessage = result.EnemyIntentResult.DidExecute
                ? $" Enemy hit {result.EnemyIntentResult.DamageToPlayer}."
                : " Enemy did not act.";

            if (result.SlotPairDamagePreview != null)
                return $"{result.PatternResult.PatternType}: Score {result.SlotPairDamagePreview.FinalScore} x Force {result.SlotPairDamagePreview.FormatFinalForce()} = Damage {result.DamageApplied}.{enemyMessage}";

            return $"{result.PatternResult.PatternType} dealt {result.DamageApplied}.{enemyMessage}";
        }

        /// <summary>좌측 메시지 텍스트를 갱신한다.</summary>
        private void SetMessage(string message)
        {
            SetText(messageText, message);
        }

        /// <summary>버튼 클릭 이벤트를 등록한다.</summary>
        private void AddButtonListeners()
        {
            if (rollButton != null)
                rollButton.onClick.AddListener(RollUnlockedDice);

            if (submitSelectedButton != null)
                submitSelectedButton.onClick.AddListener(SubmitSelectedCast);

            if (submitBrokenCastButton != null)
                submitBrokenCastButton.onClick.AddListener(SubmitBrokenCast);

            if (nextAttemptButton != null)
                nextAttemptButton.onClick.AddListener(StartNextAttempt);

            if (resetRoundButton != null)
                resetRoundButton.onClick.AddListener(StartDebugRound);

            if (togglePopupButton != null)
                togglePopupButton.onClick.AddListener(ToggleCastCandidatePopup);
        }

        /// <summary>버튼 클릭 이벤트를 해제한다.</summary>
        private void RemoveButtonListeners()
        {
            if (rollButton != null)
                rollButton.onClick.RemoveListener(RollUnlockedDice);

            if (submitSelectedButton != null)
                submitSelectedButton.onClick.RemoveListener(SubmitSelectedCast);

            if (submitBrokenCastButton != null)
                submitBrokenCastButton.onClick.RemoveListener(SubmitBrokenCast);

            if (nextAttemptButton != null)
                nextAttemptButton.onClick.RemoveListener(StartNextAttempt);

            if (resetRoundButton != null)
                resetRoundButton.onClick.RemoveListener(StartDebugRound);

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
    }
}
