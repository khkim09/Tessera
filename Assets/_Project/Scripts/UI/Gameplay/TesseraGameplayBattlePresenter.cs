using System.Collections.Generic;
using Tessera.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tessera.UI
{
    /// <summary>실제 플레이형 Round 진행 UI와 Core Round 상태를 연결한다.</summary>
    public class TesseraGameplayBattlePresenter : MonoBehaviour
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

        [Header("Kill Candidate Debug")]
        [SerializeField] private bool useKillCandidatePreviewHpOverride;
        [SerializeField] private int killCandidatePreviewOpponentHp = 12;

        [Header("Left Info Texts")]
        [SerializeField] private TMP_Text roundTitleText;
        [SerializeField] private TMP_Text opponentHpText;
        [SerializeField] private TMP_Text playerHpText;
        [SerializeField] private TMP_Text attemptText;
        [SerializeField] private TMP_Text rollText;
        [SerializeField] private TMP_Text overchargeText;
        [SerializeField] private TMP_Text enemyIntentText;
        [SerializeField] private TMP_Text partsText;
        [SerializeField] private TMP_Text messageText;

        [Header("Dice Views")]
        [SerializeField] private DiceSlotView[] trayDiceSlots = new DiceSlotView[5];
        [SerializeField] private DiceSlotView[] lockDiceSlots = new DiceSlotView[5];

        [Header("Cast Popup")]
        [SerializeField] private CastCandidatePopupView castCandidatePopupView;
        [SerializeField] private TMP_Text selectedCastText;

        [Header("Buttons")]
        [SerializeField] private Button rollButton;
        [SerializeField] private Button submitSelectedButton;
        [SerializeField] private Button submitBrokenCastButton;
        [SerializeField] private Button nextAttemptButton;
        [SerializeField] private Button resetRoundButton;
        [SerializeField] private Button killCandidateTestButton;

        private readonly int[] lockedDiceIndexBySlot = { -1, -1, -1, -1, -1 };

        private CoreRoundSimulator simulator;
        private RoundState roundState;
        private CastBoardModelBuilder castBoardModelBuilder;
        private CastBoardViewModel currentCastBoardViewModel;
        private RollPatternType selectedPatternType = RollPatternType.None;
        private int earnedParts;

        /// <summary>Core 시뮬레이터와 ViewModel 빌더를 준비한다.</summary>
        private void Awake()
        {
            simulator = useFixedSeed ? new CoreRoundSimulator(seed) : new CoreRoundSimulator();
            castBoardModelBuilder = CastBoardModelBuilder.CreateDefault();

            // 슬롯 클릭 콜백은 한 번만 연결하고 이후에는 index 매핑만 갱신한다.
            InitializeDiceSlots();
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
            useKillCandidatePreviewHpOverride = false;

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

        /// <summary>현재 선택한 Cast를 제출한다.</summary>
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

            bool submitted = simulator.TrySubmitSpecificCast(roundState, selectedPatternType, out CastSubmitResult result);

            if (!submitted)
            {
                RefreshAll("Selected cast cannot be submitted.");
                return;
            }

            RefreshAll(BuildSubmitMessage(result));
        }

        /// <summary>Broken Cast를 직접 제출한다.</summary>
        public void SubmitBrokenCast()
        {
            if (!CanActInCurrentAttempt())
            {
                SetMessage("Cannot submit Broken Cast now.");
                return;
            }

            bool submitted = simulator.TrySubmitSpecificCast(roundState, RollPatternType.BrokenCast, out CastSubmitResult result);

            if (!submitted)
            {
                RefreshAll("Broken Cast cannot be submitted.");
                return;
            }

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
            useKillCandidatePreviewHpOverride = false;

            // 다음 Attempt에서 주사위가 새로 세팅될 수 있으므로 Lock 매핑도 정리한다.
            ClearLockSlotMapping();

            if (useManualDiceValuesOnNextAttempt)
                simulator.SetCurrentDiceValuesForTest(roundState, CreateManualDiceValues());

            RefreshAll("Next attempt started.");
        }

        /// <summary>Kill Candidate 강조를 확인하기 위한 임시 HP 기준을 토글한다.</summary>
        public void ToggleKillCandidatePreviewTest()
        {
            useKillCandidatePreviewHpOverride = !useKillCandidatePreviewHpOverride;

            string message = useKillCandidatePreviewHpOverride
                ? $"Kill preview ON. Highlight HP <= {killCandidatePreviewOpponentHp}."
                : "Kill preview OFF.";

            RefreshAll(message);
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

        /// <summary>주사위 Lock 상태를 전환하고 Lock 슬롯 매핑을 갱신한다.</summary>
        private void ToggleDiceLock(int diceIndex)
        {
            if (!CanActInCurrentAttempt())
                return;

            if (diceIndex < 0 || diceIndex >= roundState.Dice.Count)
                return;

            bool willLock = !roundState.Dice[diceIndex].IsLocked;

            // Core 상태를 먼저 갱신한 뒤 UI 슬롯 매핑을 맞춘다.
            simulator.ToggleDiceLock(roundState, diceIndex);

            if (willLock)
                AssignDiceToFirstEmptyLockSlot(diceIndex);
            else
                RemoveDiceFromLockSlot(diceIndex);

            RefreshAll("Dice lock changed.");
        }

        /// <summary>Cast 후보를 선택한다.</summary>
        private void SelectCastCandidate(RollPatternType patternType)
        {
            selectedPatternType = patternType;
            RefreshAll(null);
        }

        /// <summary>전체 UI 표시를 현재 Round 상태 기준으로 다시 그린다.</summary>
        private void RefreshAll(string optionalMessage)
        {
            if (roundState == null)
                return;

            currentCastBoardViewModel = castBoardModelBuilder.Build(roundState);

            // 선택이 없으면 현재 추천 후보를 기본 선택으로 사용한다.
            if (selectedPatternType == RollPatternType.None)
                selectedPatternType = currentCastBoardViewModel.RecommendedPatternType;

            // 사용 불가능해진 후보가 선택되어 있으면 다시 추천 후보로 이동한다.
            if (!CanSelectedCastStillSubmit())
                selectedPatternType = currentCastBoardViewModel.RecommendedPatternType;

            RefreshLeftInfoTexts();
            RefreshDiceViews();
            RefreshCastPopup();
            RefreshButtonStates();

            if (!string.IsNullOrWhiteSpace(optionalMessage))
                SetMessage(optionalMessage);
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
            SetText(selectedCastText, selectedPatternType == RollPatternType.None ? "Selected: -" : $"Selected: {CastBoardCatalog.GetDisplayName(selectedPatternType)}");
        }

        /// <summary>Tray 주사위와 Lock 슬롯 표시를 갱신한다.</summary>
        private void RefreshDiceViews()
        {
            IReadOnlyList<int> diceValues = roundState.GetCurrentDiceValues();

            for (int i = 0; i < trayDiceSlots.Length; i++)
            {
                if (trayDiceSlots[i] == null)
                    continue;

                bool isLocked = i < roundState.Dice.Count && roundState.Dice[i].IsLocked;

                // Tray는 원본 주사위 index를 그대로 유지한다.
                trayDiceSlots[i].BindTrayDice(i, diceValues[i], isLocked);
            }

            RefreshLockSlotViews(diceValues);
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

                // Lock 슬롯은 원본 diceIndex를 보관해야 클릭 시 정확히 Unlock된다.
                lockDiceSlots[slotIndex].BindLockedDice(diceIndex, diceValues[diceIndex]);
            }
        }

        /// <summary>Cast 후보 팝업을 갱신한다.</summary>
        private void RefreshCastPopup()
        {
            if (castCandidatePopupView == null)
                return;

            int opponentHpForHighlight = GetOpponentHpForKillCandidateHighlight();

            // 실제 표시 숫자는 RawCastScore지만, 킬 강조는 내부 최종 피해 기준으로 계산한다.
            castCandidatePopupView.Refresh(
                currentCastBoardViewModel,
                opponentHpForHighlight,
                selectedPatternType,
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
            SetButtonInteractable(killCandidateTestButton, hasRound);
        }

        /// <summary>선택된 Cast가 아직 제출 가능한지 확인한다.</summary>
        private bool CanSelectedCastStillSubmit()
        {
            if (currentCastBoardViewModel == null)
                return false;

            if (selectedPatternType == RollPatternType.None)
                return false;

            return currentCastBoardViewModel.TryGetEntry(selectedPatternType, out CastBoardEntryModel entry)
                   && entry.Status == CastBoardEntryStatus.Available
                   && (entry.RawCastScore > 0 || entry.PatternType == RollPatternType.BrokenCast);
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

                // Core 기준으로 이미 Unlock된 주사위는 UI 슬롯에서도 제거한다.
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

        /// <summary>킬 후보 강조에 사용할 적 HP 기준값을 반환한다.</summary>
        private int GetOpponentHpForKillCandidateHighlight()
        {
            if (useKillCandidatePreviewHpOverride)
                return killCandidatePreviewOpponentHp;

            return roundState.Encounter.OpponentCurrentHp;
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

            if (killCandidateTestButton != null)
                killCandidateTestButton.onClick.AddListener(ToggleKillCandidatePreviewTest);
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

            if (killCandidateTestButton != null)
                killCandidateTestButton.onClick.RemoveListener(ToggleKillCandidatePreviewTest);
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
