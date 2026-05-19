using System.Collections.Generic;
using System.Text;
using Tessera.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tessera.UI
{
    /// <summary>Core Round 상태를 임시 디버그 UI에 연결해 전투 흐름을 수동 검증한다.</summary>
    public class TesseraDebugBattlePresenter : MonoBehaviour
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

        [Header("Views")]
        [SerializeField] private CastBoardView castBoardView;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text attemptText;
        [SerializeField] private TMP_Text rollText;
        [SerializeField] private TMP_Text diceText;
        [SerializeField] private TMP_Text hpText;
        [SerializeField] private TMP_Text enemyIntentText;
        [SerializeField] private TMP_Text overchargeText;
        [SerializeField] private TMP_Text recommendedText;
        [SerializeField] private TMP_Text messageText;

        [Header("Buttons")]
        [SerializeField] private Button submitRecommendedButton;
        [SerializeField] private Button submitBrokenCastButton;
        [SerializeField] private Button rerollUnlockedButton;
        [SerializeField] private Button nextAttemptButton;
        [SerializeField] private Button resetRoundButton;

        private CoreRoundSimulator simulator;
        private RoundState roundState;
        private CastBoardModelBuilder castBoardModelBuilder;
        private CastBoardViewModel currentCastBoardViewModel;

        /// <summary>Core 시뮬레이터와 Cast Board 빌더를 준비한다.</summary>
        private void Awake()
        {
            simulator = useFixedSeed ? new CoreRoundSimulator(seed) : new CoreRoundSimulator();
            castBoardModelBuilder = CastBoardModelBuilder.CreateDefault();
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

        /// <summary>씬 시작 시 디버그 Round를 생성하고 UI를 갱신한다.</summary>
        private void Start()
        {
            StartDebugRound();
        }

        /// <summary>새 디버그 Round를 시작한다.</summary>
        public void StartDebugRound()
        {
            RoundRuleContext ruleContext = useBossRule
                ? RoundRuleContext.CreateDebugAcesBoss()
                : RoundRuleContext.CreateDefault();

            roundState = simulator.StartRound(ruleContext);

            if (useManualDiceValuesOnStart)
                simulator.SetCurrentDiceValuesForTest(roundState, CreateManualDiceValues());

            RefreshAll("디버그 Round를 시작했습니다.");
        }

        /// <summary>현재 추천 Cast를 제출한다.</summary>
        public void SubmitRecommendedCast()
        {
            if (!CanUseRound("추천 Cast를 제출할 수 없습니다."))
                return;

            if (currentCastBoardViewModel == null || currentCastBoardViewModel.RecommendedPatternType == RollPatternType.None)
            {
                SetMessage("제출 가능한 추천 Cast가 없습니다.");
                return;
            }

            bool submitted = simulator.TrySubmitSpecificCast(
                roundState,
                currentCastBoardViewModel.RecommendedPatternType,
                out CastSubmitResult result);

            if (!submitted)
            {
                SetMessage($"추천 Cast {currentCastBoardViewModel.RecommendedPatternType} 제출에 실패했습니다.");
                RefreshAll(null);
                return;
            }

            SetSubmitMessage(result);
            RefreshAll(null);
        }

        /// <summary>Broken Cast를 직접 제출한다.</summary>
        public void SubmitBrokenCast()
        {
            if (!CanUseRound("Broken Cast를 제출할 수 없습니다."))
                return;

            bool submitted = simulator.TrySubmitSpecificCast(
                roundState,
                RollPatternType.BrokenCast,
                out CastSubmitResult result);

            if (!submitted)
            {
                SetMessage("Broken Cast 제출에 실패했습니다.");
                RefreshAll(null);
                return;
            }

            SetSubmitMessage(result);
            RefreshAll(null);
        }

        /// <summary>잠기지 않은 주사위를 다시 굴린다.</summary>
        public void RerollUnlockedDice()
        {
            if (!CanUseRound("Reroll을 실행할 수 없습니다."))
                return;

            bool rerolled = simulator.TryRerollUnlockedDice(roundState);

            if (rerolled)
                RefreshAll("잠기지 않은 주사위를 다시 굴렸습니다.");
            else
                RefreshAll("Reroll 실패. 남은 Round Roll이 없습니다.");
        }

        /// <summary>다음 Attempt 시작을 시도한다.</summary>
        public void StartNextAttempt()
        {
            if (roundState == null)
            {
                SetMessage("진행 중인 Round가 없습니다.");
                return;
            }

            bool started = simulator.TryStartNextAttempt(roundState);

            if (!started)
            {
                RefreshAll("다음 Attempt를 시작할 수 없습니다.");
                return;
            }

            if (useManualDiceValuesOnNextAttempt)
                simulator.SetCurrentDiceValuesForTest(roundState, CreateManualDiceValues());

            RefreshAll("다음 Attempt를 시작했습니다.");
        }

        private bool CanUseRound(string failMessage)
        {
            if (roundState == null)
            {
                SetMessage("진행 중인 Round가 없습니다.");
                return false;
            }

            if (!roundState.IsRoundEnded)
                return true;

            SetMessage(failMessage);
            return false;
        }

        private void RefreshAll(string optionalMessage)
        {
            if (roundState == null)
                return;

            currentCastBoardViewModel = castBoardModelBuilder.Build(roundState);

            if (castBoardView != null)
                castBoardView.Refresh(currentCastBoardViewModel);

            RefreshTexts();

            if (!string.IsNullOrWhiteSpace(optionalMessage))
                SetMessage(optionalMessage);

            RefreshButtonStates();
        }

        private void RefreshTexts()
        {
            SetText(titleText, useBossRule ? "Tessera Debug Battle - Boss Rule" : "Tessera Debug Battle - Default");
            SetText(attemptText, $"Attempt {roundState.CurrentAttempt.AttemptNumber} / {roundState.RuleContext.MaxAttempts}");
            SetText(rollText, $"Rolls {roundState.RemainingRoundRolls} / {roundState.RuleContext.RoundRollPool}");
            SetText(diceText, $"Dice: {FormatDiceValues(roundState.GetCurrentDiceValues())}");
            SetText(hpText, $"Player HP {roundState.Encounter.PlayerCurrentHp}/{roundState.Encounter.PlayerMaxHp}   Opponent HP {roundState.Encounter.OpponentCurrentHp}/{roundState.Encounter.OpponentMaxHp}");
            SetText(enemyIntentText, $"Enemy Intent: {roundState.CurrentEnemyIntent.IntentType} / Damage {roundState.CurrentEnemyIntent.Damage}");
            SetText(overchargeText, $"Overcharge {roundState.Overcharge.CurrentOvercharge}   Next Free Reroll {roundState.Overcharge.NextAttemptFreeRerollTokens}");

            if (currentCastBoardViewModel != null)
                SetText(recommendedText, $"Recommended: {currentCastBoardViewModel.RecommendedPatternType} / Damage {currentCastBoardViewModel.RecommendedDamage}");
        }

        private void RefreshButtonStates()
        {
            bool hasRound = roundState != null;
            bool canAct = hasRound && !roundState.IsRoundEnded;
            bool canStartNextAttempt = canAct && roundState.CurrentAttempt.IsSubmitted;

            SetButtonInteractable(submitRecommendedButton, canAct && !roundState.CurrentAttempt.IsSubmitted);
            SetButtonInteractable(submitBrokenCastButton, canAct && !roundState.CurrentAttempt.IsSubmitted);
            SetButtonInteractable(rerollUnlockedButton, canAct && !roundState.CurrentAttempt.IsSubmitted && roundState.RemainingRoundRolls > 0);
            SetButtonInteractable(nextAttemptButton, canStartNextAttempt);
            SetButtonInteractable(resetRoundButton, true);
        }

        private void SetSubmitMessage(CastSubmitResult result)
        {
            if (result == null)
            {
                SetMessage("Cast 제출 결과가 없습니다.");
                return;
            }

            string enemyMessage = result.EnemyIntentResult.DidExecute
                ? $" / Enemy Strike {result.EnemyIntentResult.DamageToPlayer}, Player HP {result.EnemyIntentResult.PlayerHpAfterDamage}"
                : " / Enemy did not act";

            SetMessage($"{result.PatternResult.PatternType} 제출 완료. Damage {result.DamageApplied}, Opponent HP {result.OpponentHpAfterDamage}{enemyMessage}");
        }

        private void SetMessage(string message)
        {
            SetText(messageText, message);
        }

        private void AddButtonListeners()
        {
            if (submitRecommendedButton != null)
                submitRecommendedButton.onClick.AddListener(SubmitRecommendedCast);

            if (submitBrokenCastButton != null)
                submitBrokenCastButton.onClick.AddListener(SubmitBrokenCast);

            if (rerollUnlockedButton != null)
                rerollUnlockedButton.onClick.AddListener(RerollUnlockedDice);

            if (nextAttemptButton != null)
                nextAttemptButton.onClick.AddListener(StartNextAttempt);

            if (resetRoundButton != null)
                resetRoundButton.onClick.AddListener(StartDebugRound);
        }

        private void RemoveButtonListeners()
        {
            if (submitRecommendedButton != null)
                submitRecommendedButton.onClick.RemoveListener(SubmitRecommendedCast);

            if (submitBrokenCastButton != null)
                submitBrokenCastButton.onClick.RemoveListener(SubmitBrokenCast);

            if (rerollUnlockedButton != null)
                rerollUnlockedButton.onClick.RemoveListener(RerollUnlockedDice);

            if (nextAttemptButton != null)
                nextAttemptButton.onClick.RemoveListener(StartNextAttempt);

            if (resetRoundButton != null)
                resetRoundButton.onClick.RemoveListener(StartDebugRound);
        }

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

        private static int ClampDiceValue(int value)
        {
            return Mathf.Clamp(value, 1, 6);
        }

        private static void SetText(TMP_Text targetText, string value)
        {
            if (targetText == null)
                return;

            targetText.text = value;
        }

        private static void SetButtonInteractable(Button targetButton, bool isInteractable)
        {
            if (targetButton == null)
                return;

            targetButton.interactable = isInteractable;
        }

        private static string FormatDiceValues(IReadOnlyList<int> diceValues)
        {
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < diceValues.Count; i++)
            {
                if (i > 0)
                    builder.Append(", ");

                builder.Append(diceValues[i]);
            }

            return builder.ToString();
        }
    }
}
