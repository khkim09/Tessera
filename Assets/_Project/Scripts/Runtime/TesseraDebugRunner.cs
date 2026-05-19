using System.Collections.Generic;
using System.Text;
using Tessera.Core;
using UnityEngine;

namespace Tessera.Runtime
{
    /// <summary>Unity 씬에서 Tessera Core 규칙과 Round 흐름을 검증한다.</summary>
    public class TesseraDebugRunner : MonoBehaviour
    {
        [Header("Debug Seed")]
        [SerializeField] private bool useFixedSeed = true;
        [SerializeField] private int seed = 12345;

        [Header("Manual Dice Test")]
        [SerializeField] private bool useManualDiceValues = true;
        [SerializeField] private int die1 = 2;
        [SerializeField] private int die2 = 2;
        [SerializeField] private int die3 = 2;
        [SerializeField] private int die4 = 5;
        [SerializeField] private int die5 = 5;

        [Header("Manual Pattern Submit Test")]
        [SerializeField] private RollPatternType requestedPatternType = RollPatternType.FullHouse;

        [Header("Auto Run")]
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private bool runRoundFlowTestOnStart;

        /// <summary>씬 시작 시 선택된 디버그 테스트를 실행한다.</summary>
        private void Start()
        {
            if (!runOnStart)
                return;

            if (runRoundFlowTestOnStart)
                RunRoundFlowDebugTest();
            else
                RunSingleCastDebugTest();
        }

        /// <summary>현재 주사위 값으로 가능한 Cast 목록과 단일 Cast 제출 흐름을 검증한다.</summary>
        [ContextMenu("Run Single Cast Debug Test")]
        public void RunSingleCastDebugTest()
        {
            CoreRoundSimulator simulator = CreateSimulator();
            RoundState roundState = simulator.StartDefaultRound();

            if (useManualDiceValues)
                simulator.SetCurrentDiceValuesForTest(roundState, CreateManualDiceValues());

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("========== Tessera Single Cast Debug ==========");
            AppendAttemptState(builder, "Before Submit", roundState);
            AppendAvailablePatterns(builder, simulator.GetAvailablePatterns(roundState));

            CastSubmitResult submitResult = simulator.SubmitBestCast(roundState);

            AppendSubmitResult(builder, submitResult, roundState);
            AppendEnemyIntentResult(builder, submitResult.EnemyIntentResult, roundState);
            AppendAttemptState(builder, "After Submit", roundState);
            AppendPatternUseCounts(builder, roundState);
            builder.AppendLine("==============================================");

            Debug.Log(builder.ToString());
        }

        /// <summary>특정 Cast를 직접 선택해서 제출하는 흐름을 검증한다.</summary>
        [ContextMenu("Run Specific Pattern Submit Debug Test")]
        public void RunSpecificPatternSubmitDebugTest()
        {
            CoreRoundSimulator simulator = CreateSimulator();
            RoundState roundState = simulator.StartDefaultRound();

            if (useManualDiceValues)
                simulator.SetCurrentDiceValuesForTest(roundState, CreateManualDiceValues());

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("========== Tessera Specific Pattern Submit Debug ==========");
            AppendAttemptState(builder, "Before Specific Submit", roundState);
            AppendAvailablePatterns(builder, simulator.GetAvailablePatterns(roundState));
            builder.AppendLine($"Requested Pattern: {requestedPatternType}");

            bool submitted = simulator.TrySubmitSpecificCast(roundState, requestedPatternType, out CastSubmitResult submitResult);
            builder.AppendLine($"Submit Success: {submitted}");

            if (submitted)
            {
                AppendSubmitResult(builder, submitResult, roundState);
                AppendEnemyIntentResult(builder, submitResult.EnemyIntentResult, roundState);
                AppendAttemptState(builder, "After Specific Submit", roundState);
            }
            else
            {
                builder.AppendLine($"Cannot submit {requestedPatternType} with current dice or round limits.");
                builder.AppendLine($"요약: {requestedPatternType} 제출 실패. 현재 주사위 조건, Round 사용 제한, Table Rule 중 하나에 막혔습니다.");
            }

            AppendPatternUseCounts(builder, roundState);
            builder.AppendLine("==========================================================");

            Debug.Log(builder.ToString());
        }

        /// <summary>Broken Cast 보상, 상대 Strike, 다음 Attempt 흐름을 검증한다.</summary>
        [ContextMenu("Run Round Flow Debug Test")]
        public void RunRoundFlowDebugTest()
        {
            CoreRoundSimulator simulator = CreateSimulator();
            RoundState roundState = simulator.StartDefaultRound();
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("========== Tessera Round Flow Debug ==========");

            simulator.SetCurrentDiceValuesForTest(roundState, new List<int> { 1, 2, 4, 5, 6 });
            AppendAttemptState(builder, "Before Submit Attempt 1", roundState);
            AppendAvailablePatterns(builder, simulator.GetAvailablePatterns(roundState));

            simulator.TrySubmitSpecificCast(roundState, RollPatternType.BrokenCast, out CastSubmitResult firstResult);
            AppendSubmitResult(builder, firstResult, roundState);
            AppendEnemyIntentResult(builder, firstResult.EnemyIntentResult, roundState);
            AppendPatternUseCounts(builder, roundState);

            bool startedSecondAttempt = simulator.TryStartNextAttempt(roundState);
            builder.AppendLine($"Start Attempt 2: {startedSecondAttempt}");
            builder.AppendLine(startedSecondAttempt
                ? "요약: Attempt 2 시작 성공. Broken Cast 보상이 다음 Attempt로 정상 이월되었습니다."
                : "요약: Attempt 2 시작 실패. Round 종료 상태이거나 최대 Attempt에 도달했습니다.");

            AppendAttemptState(builder, "After Start Attempt 2", roundState);

            simulator.SetCurrentDiceValuesForTest(roundState, new List<int> { 2, 2, 2, 5, 5 });
            AppendAttemptState(builder, "Before Submit Attempt 2", roundState);
            AppendAvailablePatterns(builder, simulator.GetAvailablePatterns(roundState));

            CastSubmitResult secondResult = simulator.SubmitBestCast(roundState);
            AppendSubmitResult(builder, secondResult, roundState);
            AppendEnemyIntentResult(builder, secondResult.EnemyIntentResult, roundState);
            AppendPatternUseCounts(builder, roundState);

            builder.AppendLine("==============================================");

            Debug.Log(builder.ToString());
        }

        /// <summary>같은 Round 안에서 동일 Cast 2회 제출 제한을 검증한다.</summary>
        [ContextMenu("Run Duplicate Cast Limit Debug Test")]
        public void RunDuplicateCastLimitDebugTest()
        {
            CoreRoundSimulator simulator = CreateSimulator();
            RoundState roundState = simulator.StartDefaultRound();
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("========== Tessera Duplicate Cast Limit Debug ==========");

            simulator.SetCurrentDiceValuesForTest(roundState, new List<int> { 2, 2, 2, 5, 5 });
            AppendAttemptState(builder, "Attempt 1 Before FullHouse Submit", roundState);
            AppendAvailablePatterns(builder, simulator.GetAvailablePatterns(roundState));

            bool firstSubmitted = simulator.TrySubmitSpecificCast(roundState, RollPatternType.FullHouse, out CastSubmitResult firstResult);
            builder.AppendLine($"Submit FullHouse Attempt 1 Success: {firstSubmitted}");
            builder.AppendLine(firstSubmitted
                ? "요약: Attempt 1에서 FullHouse 제출 성공. 사용 횟수 제한 검증을 위한 첫 제출이 정상 처리되었습니다."
                : "요약: Attempt 1에서 FullHouse 제출 실패. 이 테스트에서는 실패하면 안 됩니다.");

            if (firstSubmitted)
            {
                AppendSubmitResult(builder, firstResult, roundState);
                AppendEnemyIntentResult(builder, firstResult.EnemyIntentResult, roundState);
            }

            AppendPatternUseCounts(builder, roundState);

            bool startedSecondAttempt = simulator.TryStartNextAttempt(roundState);
            builder.AppendLine($"Start Attempt 2: {startedSecondAttempt}");
            builder.AppendLine(startedSecondAttempt
                ? "요약: Attempt 2 시작 성공. 이제 동일 Cast 재사용 제한을 검증합니다."
                : "요약: Attempt 2 시작 실패. 동일 Cast 재사용 제한 테스트를 진행할 수 없습니다.");

            if (startedSecondAttempt)
            {
                simulator.SetCurrentDiceValuesForTest(roundState, new List<int> { 2, 2, 2, 5, 5 });
                AppendAttemptState(builder, "Attempt 2 Before Duplicate FullHouse Submit", roundState);
                AppendAvailablePatterns(builder, simulator.GetAvailablePatterns(roundState));

                bool secondSubmitted = simulator.TrySubmitSpecificCast(roundState, RollPatternType.FullHouse, out CastSubmitResult secondResult);
                builder.AppendLine($"Submit FullHouse Attempt 2 Success: {secondSubmitted}");

                if (secondSubmitted)
                {
                    AppendSubmitResult(builder, secondResult, roundState);
                    AppendEnemyIntentResult(builder, secondResult.EnemyIntentResult, roundState);
                    builder.AppendLine("요약: 비정상. FullHouse가 같은 Round에서 2회 제출되었습니다.");
                }
                else
                {
                    builder.AppendLine("Duplicate FullHouse was blocked correctly by round usage limit.");
                    builder.AppendLine("요약: 정상. FullHouse는 이미 사용되어 Attempt 2에서 제출이 차단되었습니다.");
                }
            }

            AppendPatternUseCounts(builder, roundState);
            builder.AppendLine("========================================================");

            Debug.Log(builder.ToString());
        }

        /// <summary>Lock과 Round Roll Pool 사용 흐름을 검증한다.</summary>
        [ContextMenu("Run Lock And Reroll Debug Test")]
        public void RunLockAndRerollDebugTest()
        {
            CoreRoundSimulator simulator = CreateSimulator();
            RoundState roundState = simulator.StartDefaultRound();
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("========== Tessera Lock/Reroll Debug ==========");
            AppendAttemptState(builder, "Initial Roll", roundState);

            simulator.SetDiceLocked(roundState, 0, true);
            simulator.SetDiceLocked(roundState, 1, true);
            builder.AppendLine("Lock Dice 0, 1");
            builder.AppendLine("요약: Dice 0, Dice 1 잠금 처리 완료. 다음 Reroll에서는 잠기지 않은 주사위만 굴러야 합니다.");
            AppendAttemptState(builder, "After Lock", roundState);

            bool rerolled = simulator.TryRerollUnlockedDice(roundState);
            builder.AppendLine($"Reroll Unlocked Dice: {rerolled}");
            builder.AppendLine(rerolled
                ? "요약: Reroll 성공. Round Roll Pool이 1 감소하고 잠기지 않은 주사위만 갱신되어야 합니다."
                : "요약: Reroll 실패. 남은 Round Roll이 없거나 Round가 진행 불가능한 상태입니다.");

            AppendAttemptState(builder, "After Reroll", roundState);
            AppendAvailablePatterns(builder, simulator.GetAvailablePatterns(roundState));

            CastSubmitResult submitResult = simulator.SubmitBestCast(roundState);
            AppendSubmitResult(builder, submitResult, roundState);
            AppendEnemyIntentResult(builder, submitResult.EnemyIntentResult, roundState);
            AppendPatternUseCounts(builder, roundState);

            builder.AppendLine("===============================================");

            Debug.Log(builder.ToString());
        }

        /// <summary>Boss Table Rule이 Cast 피해와 사용 가능 여부에 미치는 영향을 검증한다.</summary>
        [ContextMenu("Run Boss Rule Debug Test")]
        public void RunBossRuleDebugTest()
        {
            CoreRoundSimulator simulator = CreateSimulator();
            RoundState roundState = simulator.StartRound(RoundRuleContext.CreateDebugAcesBoss());
            StringBuilder builder = new StringBuilder();

            simulator.SetCurrentDiceValuesForTest(roundState, new List<int> { 2, 2, 2, 5, 5 });

            builder.AppendLine("========== Tessera Boss Rule Debug ==========");
            AppendTableRules(builder, roundState.RuleContext);
            AppendAttemptState(builder, "Before Boss Rule Submit", roundState);
            AppendAvailablePatterns(builder, simulator.GetAvailablePatterns(roundState));

            CastSubmitResult submitResult = simulator.SubmitBestCast(roundState);

            AppendSubmitResult(builder, submitResult, roundState);
            AppendEnemyIntentResult(builder, submitResult.EnemyIntentResult, roundState);
            AppendPatternUseCounts(builder, roundState);
            builder.AppendLine("=============================================");

            Debug.Log(builder.ToString());
        }

        /// <summary>Stage 1의 Round 1, Round 2, Boss Round 정의와 진행 상태를 검증한다.</summary>
        [ContextMenu("Run Stage Definition Debug Test")]
        public void RunStageDefinitionDebugTest()
        {
            StageSimulator stageSimulator = useFixedSeed ? new StageSimulator(seed) : new StageSimulator();
            StageProgressState stageProgressState = stageSimulator.StartDebugStageOne();
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("========== Tessera Stage Definition Debug ==========");
            AppendStageState(builder, stageProgressState);

            StageRoundDefinition round1 = stageProgressState.GetCurrentRound();
            builder.AppendLine();
            builder.AppendLine($"Current Round: {round1.DisplayName}");
            builder.AppendLine($"Can Skip: {round1.CanSkip}");

            bool skippedRound1 = stageSimulator.TrySkipCurrentRound(stageProgressState);
            builder.AppendLine($"Skip Round 1 Result: {skippedRound1}");
            builder.AppendLine(skippedRound1
                ? "요약: Round 1 Skip 성공. Skip 보상이 Parts에 정상 반영되어야 합니다."
                : "요약: Round 1 Skip 실패. 일반 Round는 기본적으로 Skip 가능해야 합니다.");
            AppendStageState(builder, stageProgressState);

            StageRoundDefinition round2 = stageProgressState.GetCurrentRound();
            builder.AppendLine();
            builder.AppendLine($"Current Round: {round2.DisplayName}");
            builder.AppendLine($"Can Skip: {round2.CanSkip}");

            stageSimulator.CompleteCurrentRound(stageProgressState);
            builder.AppendLine("Complete Round 2 Result: True");
            builder.AppendLine("요약: Round 2 완료 처리 성공. 완료 보상이 Parts에 정상 반영되어야 합니다.");
            AppendStageState(builder, stageProgressState);

            StageRoundDefinition bossRound = stageProgressState.GetCurrentRound();
            builder.AppendLine();
            builder.AppendLine($"Current Round: {bossRound.DisplayName}");
            builder.AppendLine($"Round Type: {bossRound.RoundType}");
            builder.AppendLine($"Can Skip: {bossRound.CanSkip}");
            AppendTableRules(builder, bossRound.RuleContext);

            bool skippedBoss = stageSimulator.TrySkipCurrentRound(stageProgressState);
            builder.AppendLine($"Skip Boss Round Result: {skippedBoss}");
            builder.AppendLine(!skippedBoss
                ? "요약: 정상. Boss Round는 Skip 불가로 차단되었습니다."
                : "요약: 비정상. Boss Round가 Skip되었습니다.");

            stageSimulator.CompleteCurrentRound(stageProgressState);
            builder.AppendLine("Complete Boss Round Result: True");
            builder.AppendLine("요약: Boss Round 완료 처리 성공. Stage Cleared가 True가 되어야 합니다.");
            AppendStageState(builder, stageProgressState);

            builder.AppendLine("====================================================");

            Debug.Log(builder.ToString());
        }

        /// <summary>Cast Board UI에 전달할 표시 데이터 생성 결과를 검증한다.</summary>
        [ContextMenu("Run Cast Board ViewModel Debug Test")]
        public void RunCastBoardViewModelDebugTest()
        {
            CoreRoundSimulator simulator = CreateSimulator();
            RoundState roundState = simulator.StartRound(RoundRuleContext.CreateDebugAcesBoss());
            CastBoardModelBuilder castBoardBuilder = CastBoardModelBuilder.CreateDefault();
            StringBuilder builder = new StringBuilder();

            simulator.SetCurrentDiceValuesForTest(roundState, new List<int> { 2, 2, 2, 5, 5 });

            builder.AppendLine("========== Tessera Cast Board ViewModel Debug ==========");
            AppendTableRules(builder, roundState.RuleContext);
            AppendAttemptState(builder, "Before Cast Board Build", roundState);

            CastBoardViewModel viewModel = castBoardBuilder.Build(roundState);
            AppendCastBoardViewModel(builder, viewModel);

            simulator.TrySubmitSpecificCast(roundState, RollPatternType.FullHouse, out CastSubmitResult submitResult);
            AppendSubmitResult(builder, submitResult, roundState);
            AppendEnemyIntentResult(builder, submitResult.EnemyIntentResult, roundState);

            bool startedNextAttempt = simulator.TryStartNextAttempt(roundState);
            builder.AppendLine($"Start Next Attempt: {startedNextAttempt}");
            builder.AppendLine(startedNextAttempt
                ? "요약: 다음 Attempt 시작 성공. Cast Board에서 사용된 Cast가 Used 상태로 바뀌어야 합니다."
                : "요약: 다음 Attempt 시작 실패. Round 종료 또는 Attempt 제한 상태입니다.");

            if (startedNextAttempt)
            {
                simulator.SetCurrentDiceValuesForTest(roundState, new List<int> { 2, 2, 2, 5, 5 });
                AppendAttemptState(builder, "After FullHouse Used", roundState);

                CastBoardViewModel afterUseViewModel = castBoardBuilder.Build(roundState);
                AppendCastBoardViewModel(builder, afterUseViewModel);
            }

            builder.AppendLine("========================================================");

            Debug.Log(builder.ToString());
        }

        private CoreRoundSimulator CreateSimulator()
        {
            if (useFixedSeed)
                return new CoreRoundSimulator(seed);

            return new CoreRoundSimulator();
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

        private static void AppendTableRules(StringBuilder builder, RoundRuleContext ruleContext)
        {
            builder.AppendLine();
            builder.AppendLine("[Table Rules]");

            if (ruleContext.TableRules.Count == 0)
            {
                builder.AppendLine("None");
                builder.AppendLine("요약: 현재 Round에 적용된 Table Rule이 없습니다.");
                return;
            }

            for (int i = 0; i < ruleContext.TableRules.Count; i++)
            {
                TableRule rule = ruleContext.TableRules[i];
                builder.AppendLine($"{i + 1}. {rule.RuleType} / Value={rule.Value} / {rule.Description}");
            }

            builder.AppendLine($"요약: Table Rule {ruleContext.TableRules.Count}개가 적용 중입니다. Cast 피해나 사용 가능 여부가 변경될 수 있습니다.");
        }

        private static void AppendAttemptState(StringBuilder builder, string title, RoundState roundState)
        {
            builder.AppendLine();
            builder.AppendLine($"[{title}]");
            builder.AppendLine($"Attempt: {roundState.CurrentAttempt.AttemptNumber} / {roundState.RuleContext.MaxAttempts}");
            builder.AppendLine($"Round Rolls: {roundState.RemainingRoundRolls} / {roundState.RuleContext.RoundRollPool}");
            builder.AppendLine($"Dice: {FormatDiceValues(roundState.GetCurrentDiceValues())}");
            builder.AppendLine($"Locks: {FormatLockStates(roundState.Dice)}");
            builder.AppendLine($"Free Reroll Tokens: {roundState.CurrentAttempt.FreeRerollTokens}");
            builder.AppendLine($"Pending Next Attempt Free Reroll Tokens: {roundState.Overcharge.NextAttemptFreeRerollTokens}");
            builder.AppendLine($"Overcharge: {roundState.Overcharge.CurrentOvercharge}");
            builder.AppendLine($"Enemy Intent: {roundState.CurrentEnemyIntent.IntentType} / Damage {roundState.CurrentEnemyIntent.Damage}");
            builder.AppendLine($"Opponent HP: {roundState.Encounter.OpponentCurrentHp} / {roundState.Encounter.OpponentMaxHp}");
            builder.AppendLine($"Player HP: {roundState.Encounter.PlayerCurrentHp} / {roundState.Encounter.PlayerMaxHp}");
            builder.AppendLine($"Round Outcome: {roundState.OutcomeType}");
            builder.AppendLine($"요약: 현재 Attempt {roundState.CurrentAttempt.AttemptNumber}/{roundState.RuleContext.MaxAttempts}, Roll {roundState.RemainingRoundRolls}/{roundState.RuleContext.RoundRollPool}, Player HP {roundState.Encounter.PlayerCurrentHp}/{roundState.Encounter.PlayerMaxHp}, Opponent HP {roundState.Encounter.OpponentCurrentHp}/{roundState.Encounter.OpponentMaxHp} 상태입니다.");
        }

        private static void AppendAvailablePatterns(StringBuilder builder, IReadOnlyList<PatternResult> availablePatterns)
        {
            builder.AppendLine();
            builder.AppendLine("[Available Casts]");

            for (int i = 0; i < availablePatterns.Count; i++)
            {
                PatternResult result = availablePatterns[i];
                builder.AppendLine($"{i + 1}. {result.PatternType} | Raw={result.RawCastScore}, Damage={result.FinalDamage}, IncludedSum={result.IncludedDiceSum}");
            }

            builder.AppendLine($"요약: 현재 제출 가능한 Cast는 {availablePatterns.Count}개입니다.");
        }

        private static void AppendSubmitResult(StringBuilder builder, CastSubmitResult result, RoundState roundState)
        {
            builder.AppendLine();
            builder.AppendLine("[Submit Result]");
            builder.AppendLine($"Pattern: {result.PatternResult.PatternType}");
            builder.AppendLine($"Raw Cast Score: {result.PatternResult.RawCastScore}");
            builder.AppendLine($"Included Dice Sum: {result.PatternResult.IncludedDiceSum}");
            builder.AppendLine($"Flat Bonus: {result.PatternResult.FlatBonus}");
            builder.AppendLine($"Damage Multiplier: {result.PatternResult.DamageMultiplier}");
            builder.AppendLine($"Extra Bonus: {result.PatternResult.ExtraBonus}");
            builder.AppendLine($"Damage Before Table Rules: {result.TableRuleEvaluationResult.OriginalDamage}");
            builder.AppendLine($"Damage After Table Rules: {result.TableRuleEvaluationResult.ModifiedDamage}");
            builder.AppendLine($"Table Rule Message: {result.TableRuleEvaluationResult.Message}");
            builder.AppendLine($"Damage Applied: {result.DamageApplied}");
            builder.AppendLine($"Opponent HP After Damage: {result.OpponentHpAfterDamage} / {roundState.Encounter.OpponentMaxHp}");
            builder.AppendLine($"Broken Cast: {result.IsBrokenCast}");
            builder.AppendLine($"Granted Overcharge: {result.GrantedOverchargeAmount}");
            builder.AppendLine($"Granted Next Attempt Free Reroll Tokens: {result.GrantedNextAttemptFreeRerollTokens}");
            builder.AppendLine($"Outcome Type: {result.OutcomeType}");
            builder.AppendLine($"Round Won: {result.IsRoundWon}");
            builder.AppendLine($"Round Lost: {result.IsRoundLost}");
            builder.AppendLine($"Can Start Next Attempt: {result.CanStartNextAttempt}");
            builder.AppendLine($"Message: {result.Message}");

            if (result.IsBrokenCast)
                builder.AppendLine($"요약: Broken Cast 제출 완료. 피해 {result.DamageApplied}, Overcharge +{result.GrantedOverchargeAmount}, 다음 Attempt 무료 리롤 +{result.GrantedNextAttemptFreeRerollTokens} 처리 상태입니다.");
            else
                builder.AppendLine($"요약: {result.PatternResult.PatternType} 제출 완료. 최종 피해 {result.DamageApplied}, 상대 HP {result.OpponentHpAfterDamage}/{roundState.Encounter.OpponentMaxHp}, 다음 Attempt 가능 여부는 {result.CanStartNextAttempt}입니다.");
        }

        private static void AppendEnemyIntentResult(StringBuilder builder, EnemyIntentResult result, RoundState roundState)
        {
            builder.AppendLine();
            builder.AppendLine("[Enemy Intent Result]");
            builder.AppendLine($"Executed: {result.DidExecute}");
            builder.AppendLine($"Intent Type: {result.IntentType}");
            builder.AppendLine($"Damage To Player: {result.DamageToPlayer}");
            builder.AppendLine($"Player HP After Damage: {result.PlayerHpAfterDamage} / {roundState.Encounter.PlayerMaxHp}");
            builder.AppendLine($"Message: {result.Message}");
            builder.AppendLine(result.DidExecute
                ? $"요약: 상대 Intent 실행 완료. 플레이어가 {result.DamageToPlayer} 피해를 받아 HP가 {result.PlayerHpAfterDamage}/{roundState.Encounter.PlayerMaxHp}가 되었습니다."
                : "요약: 상대 Intent는 실행되지 않았습니다. 보통 상대가 이미 처치되었거나 Intent가 None인 경우입니다.");
        }

        private static void AppendPatternUseCounts(StringBuilder builder, RoundState roundState)
        {
            builder.AppendLine();
            builder.AppendLine("[Cast Use Counts]");

            bool hasAny = false;

            foreach (KeyValuePair<RollPatternType, int> pair in roundState.PatternUseCounts)
            {
                builder.AppendLine($"{pair.Key}: {pair.Value}");
                hasAny = true;
            }

            if (!hasAny)
                builder.AppendLine("None");

            builder.AppendLine(hasAny
                ? "요약: 현재 Round에서 사용한 Cast 기록이 정상 누적되고 있습니다."
                : "요약: 아직 이번 Round에서 사용한 Cast가 없습니다.");
        }

        private static void AppendStageState(StringBuilder builder, StageProgressState stageProgressState)
        {
            builder.AppendLine();
            builder.AppendLine("[Stage State]");
            builder.AppendLine($"Stage: {stageProgressState.StageDefinition.DisplayName}");
            builder.AppendLine($"Current Round Index: {stageProgressState.CurrentRoundIndex}");
            builder.AppendLine($"Earned Parts: {stageProgressState.EarnedParts}");
            builder.AppendLine($"Stage Cleared: {stageProgressState.IsStageCleared}");
            builder.AppendLine($"Stage Failed: {stageProgressState.IsStageFailed}");
            builder.AppendLine($"Stage Reward: {stageProgressState.StageDefinition.StageRewardDescription}");
            builder.AppendLine("Round States:");

            for (int i = 0; i < stageProgressState.RoundCompletionStates.Count; i++)
                builder.AppendLine($"  {i + 1}. {stageProgressState.RoundCompletionStates[i]}");

            builder.AppendLine($"요약: Stage 진행도는 Round Index {stageProgressState.CurrentRoundIndex}, 누적 Parts {stageProgressState.EarnedParts}, Cleared={stageProgressState.IsStageCleared}, Failed={stageProgressState.IsStageFailed}입니다.");
        }

        private static void AppendCastBoardViewModel(StringBuilder builder, CastBoardViewModel viewModel)
        {
            builder.AppendLine();
            builder.AppendLine("[Cast Board ViewModel]");
            builder.AppendLine($"Recommended: {viewModel.RecommendedPatternType} / Damage {viewModel.RecommendedDamage}");

            int availableCount = 0;
            int usedCount = 0;
            int blockedCount = 0;
            int conditionNotMetCount = 0;

            for (int i = 0; i < viewModel.Entries.Count; i++)
            {
                CastBoardEntryModel entry = viewModel.Entries[i];

                if (entry.Status == CastBoardEntryStatus.Available)
                    availableCount++;
                else if (entry.Status == CastBoardEntryStatus.Used)
                    usedCount++;
                else if (entry.Status == CastBoardEntryStatus.BlockedByTableRule)
                    blockedCount++;
                else if (entry.Status == CastBoardEntryStatus.ConditionNotMet)
                    conditionNotMetCount++;

                builder.AppendLine(
                    $"{i + 1}. {entry.DisplayName} " +
                    $"| Type={entry.PatternType} " +
                    $"| Status={entry.Status} " +
                    $"| Raw={entry.RawCastScore} " +
                    $"| Damage={entry.DamageBeforeTableRules}->{entry.DamageAfterTableRules} " +
                    $"| Use={entry.UseCount}/{entry.MaxUseCount} " +
                    $"| Recommended={entry.IsRecommended} " +
                    $"| Message={entry.Message}");
            }

            builder.AppendLine($"요약: 추천 Cast는 {viewModel.RecommendedPatternType}, 예상 피해는 {viewModel.RecommendedDamage}입니다. Available {availableCount}개, Used {usedCount}개, Blocked {blockedCount}개, 조건 미충족 {conditionNotMetCount}개입니다.");
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

        private static string FormatLockStates(IReadOnlyList<DiceInstance> dice)
        {
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < dice.Count; i++)
            {
                if (i > 0)
                    builder.Append(", ");

                builder.Append(dice[i].IsLocked ? "L" : "-");
            }

            return builder.ToString();
        }
    }
}
