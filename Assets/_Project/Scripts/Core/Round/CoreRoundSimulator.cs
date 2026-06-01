using System;
using System.Collections.Generic;

namespace Tessera.Core
{
    /// <summary>UI 없이 Core Round 흐름을 실행하고 검증한다.</summary>
    public class CoreRoundSimulator
    {
        private readonly DiceRoller diceRoller;
        private readonly PatternEvaluator patternEvaluator;
        private readonly SlotPairDamageCalculator slotPairDamageCalculator;

        /// <summary>시드 없는 랜덤 굴림 기반 Round 시뮬레이터를 생성한다.</summary>
        public CoreRoundSimulator()
        {
            diceRoller = new DiceRoller();
            patternEvaluator = PatternEvaluator.CreateDefault();
            slotPairDamageCalculator = new SlotPairDamageCalculator();
        }

        /// <summary>고정 시드 기반 Round 시뮬레이터를 생성한다.</summary>
        public CoreRoundSimulator(int seed)
        {
            diceRoller = new DiceRoller(seed);
            patternEvaluator = PatternEvaluator.CreateDefault();
            slotPairDamageCalculator = new SlotPairDamageCalculator();
        }

        /// <summary>지정한 HP와 Stage Overcharge 상태를 유지한 채 새 Round를 시작한다.</summary>
        public RoundState StartRound(
            RoundRuleContext ruleContext,
            int playerCurrentHP,
            OverchargeState stageOverchargeState)
        {
            if (ruleContext == null)
                throw new ArgumentNullException(nameof(ruleContext));

            if (stageOverchargeState == null)
                throw new ArgumentNullException(nameof(stageOverchargeState));

            int resolvedPlayerHP = Math.Max(1, Math.Min(ruleContext.PlayerMaxHP, playerCurrentHP));

            EncounterState encounter = new EncounterState(
                ruleContext.PlayerMaxHP,
                ruleContext.OpponentMaxHP,
                resolvedPlayerHP,
                ruleContext.OpponentMaxHP);

            AttemptState firstAttempt = CreateAttempt(stageOverchargeState, 1);
            List<DiceInstance> dice = diceRoller.CreateRolledStandardDiceSet(ruleContext.DiceCount);
            EnemyIntent enemyIntent = EnemyIntent.Strike(ruleContext.EnemyStrikeDamage);

            return new RoundState(ruleContext, encounter, stageOverchargeState, dice, firstAttempt, enemyIntent);
        }

        /// <summary>현재 주사위로 제출 가능한 Cast 카테고리 목록을 반환한다.</summary>
        public List<PatternResult> GetAvailablePatterns(RoundState roundState)
        {
            ValidatePlayableRound(roundState);

            List<int> diceValues = roundState.GetCurrentDiceValues();
            List<PatternResult> allResults = patternEvaluator.EvaluateAll(diceValues);
            List<PatternResult> filteredResults = new List<PatternResult>();

            for (int i = 0; i < allResults.Count; i++)
            {
                PatternResult result = allResults[i];

                if (!roundState.CanUseCastThisRound(result.PatternType))
                    continue;

                TableRuleEvaluationResult tableRuleResult = TableRuleEvaluator.Evaluate(roundState.RuleContext, result);

                if (tableRuleResult.IsCastBlocked)
                    continue;

                filteredResults.Add(result);
            }

            return filteredResults;
        }

        /// <summary>선택한 Cast 카테고리가 현재 주사위와 Round 제한에서 제출 가능한지 확인한다.</summary>
        public bool CanSubmitPattern(RoundState roundState, RollPatternType patternType)
        {
            ValidatePlayableRound(roundState);

            if (!roundState.CanUseCastThisRound(patternType))
                return false;

            List<int> diceValues = roundState.GetCurrentDiceValues();

            if (!patternEvaluator.TryEvaluateSpecificPattern(diceValues, patternType, out PatternResult patternResult))
                return false;

            TableRuleEvaluationResult tableRuleResult = TableRuleEvaluator.Evaluate(roundState.RuleContext, patternResult);
            return !tableRuleResult.IsCastBlocked;
        }

        /// <summary>지정한 주사위의 잠금 상태를 변경한다.</summary>
        public void SetDiceLocked(RoundState roundState, int diceIndex, bool isLocked)
        {
            ValidatePlayableRound(roundState);
            roundState.GetDice(diceIndex).SetLocked(isLocked);
        }

        /// <summary>지정한 주사위의 잠금 상태를 반전한다.</summary>
        public void ToggleDiceLock(RoundState roundState, int diceIndex)
        {
            ValidatePlayableRound(roundState);

            DiceInstance dice = roundState.GetDice(diceIndex);
            dice.SetLocked(!dice.IsLocked);
        }

        /// <summary>잠기지 않은 주사위들을 Round Roll Pool을 소모해 다시 굴린다.</summary>
        public bool TryRerollUnlockedDice(RoundState roundState)
        {
            ValidatePlayableRound(roundState);

            if (!roundState.TrySpendRoundRoll())
                return false;

            diceRoller.RollUnlocked(roundState.Dice);
            roundState.MarkCurrentAttemptCastReady(CastReadinessSource.RollPerformed);
            return true;
        }

        /// <summary>무료 리롤 토큰으로 지정한 주사위 1개를 다시 굴린다.</summary>
        public bool TryUseFreeRerollOnDice(RoundState roundState, int diceIndex)
        {
            ValidatePlayableRound(roundState);

            if (!roundState.CurrentAttempt.TrySpendFreeRerollToken())
                return false;

            DiceInstance dice = roundState.GetDice(diceIndex);
            diceRoller.RollSingle(dice);
            roundState.MarkCurrentAttemptCastReady(CastReadinessSource.RollPerformed);
            return true;
        }

        // TODO : 원하는 주사위 면 교체 Device 구현 시 호출
        // ex. simulator.MarkCurrentAttemptCastReady(roundState, CastReadinessSource.ForcedDiceFaceSelection);
        /// <summary>외부 효과로 현재 Attempt가 Cast 가능한 주사위 상태를 확보했음을 표시한다.</summary>
        public void MarkCurrentAttemptCastReady(RoundState roundState, CastReadinessSource source)
        {
            ValidatePlayableRound(roundState);
            roundState.MarkCurrentAttemptCastReady(source);
        }

        /// <summary>SlotPair 계산값을 사용해 특정 Cast를 제출한다.</summary>
        public bool TrySubmitSpecificCast(
            RoundState roundState,
            RollPatternType patternType,
            IReadOnlyList<int> lockSlotDiceIndexes,
            IReadOnlyList<SlotPairDeviceDefinition> deviceDefinitions,
            out CastSubmitResult result)
        {
            ValidatePlayableRound(roundState);

            if (!TryBuildSlotPairDamagePreview(
                    roundState,
                    patternType,
                    lockSlotDiceIndexes,
                    deviceDefinitions,
                    out PatternResult patternResult,
                    out SlotPairDamagePreview preview,
                    out TableRuleEvaluationResult tableRuleResult))
            {
                result = null;
                return false;
            }

            result = SubmitResolvedCast(roundState, patternResult, preview, tableRuleResult);
            return true;
        }

        /// <summary>SlotPair 기준 선택 Cast 피해 미리보기를 생성한다.</summary>
        public bool TryBuildSlotPairDamagePreview(
            RoundState roundState,
            RollPatternType patternType,
            IReadOnlyList<int> lockSlotDiceIndexes,
            IReadOnlyList<SlotPairDeviceDefinition> deviceDefinitions,
            out PatternResult patternResult,
            out SlotPairDamagePreview preview,
            out TableRuleEvaluationResult tableRuleResult)
        {
            ValidatePlayableRound(roundState);

            patternResult = null;
            preview = null;
            tableRuleResult = null;

            if (!TryResolvePatternAndTableRule(
                    roundState,
                    patternType,
                    out patternResult,
                    out TableRuleEvaluationResult baseTableRuleResult))
            {
                return false;
            }

            List<int> diceValues = roundState.GetCurrentDiceValues();
            preview = slotPairDamageCalculator.Calculate(patternResult, diceValues, lockSlotDiceIndexes, deviceDefinitions);
            tableRuleResult = TableRuleEvaluator.Evaluate(roundState.RuleContext, patternResult.PatternType, preview.DamageBeforeTableRules);

            if (tableRuleResult.IsCastBlocked)
                return false;

            return true;
        }

        /// <summary>제출 이후 다음 Attempt 시작을 시도한다.</summary>
        public bool TryStartNextAttempt(RoundState roundState)
        {
            if (roundState == null)
                throw new ArgumentNullException(nameof(roundState));

            if (roundState.IsRoundEnded)
                return false;

            if (!roundState.CurrentAttempt.IsSubmitted)
                return false;

            if (roundState.CurrentAttempt.AttemptNumber >= roundState.RuleContext.MaxAttempts)
                return false;

            int nextAttemptNumber = roundState.CurrentAttempt.AttemptNumber + 1;
            AttemptState nextAttempt = CreateAttempt(roundState.Overcharge, nextAttemptNumber);
            List<DiceInstance> dice = diceRoller.CreateRolledStandardDiceSet(roundState.RuleContext.DiceCount);

            roundState.StartAttempt(nextAttempt, dice);
            roundState.SetEnemyIntent(EnemyIntent.Strike(roundState.RuleContext.EnemyStrikeDamage));
            return true;
        }

        /// <summary>제출된 Cast 이후 상대 턴 Intent 실행을 시도한다.</summary>
        public bool TryResolveEnemyTurn(
            RoundState roundState,
            out EnemyIntentResult enemyIntentResult,
            out RoundOutcomeType outcomeType)
        {
            if (roundState == null)
                throw new ArgumentNullException(nameof(roundState));

            enemyIntentResult = EnemyIntentResult.NotExecuted(roundState.Encounter.PlayerCurrentHP);
            outcomeType = RoundOutcomeType.Ongoing;

            if (roundState.IsRoundEnded)
            {
                outcomeType = roundState.OutcomeType;
                return false;
            }

            if (roundState.CurrentAttempt == null || !roundState.CurrentAttempt.IsSubmitted)
                return false;

            if (roundState.Encounter.IsOpponentDefeated)
            {
                roundState.MarkWon();
                outcomeType = RoundOutcomeType.Won;
                return false;
            }

            enemyIntentResult = ExecuteEnemyIntent(roundState);
            outcomeType = ResolveRoundOutcomeAfterEnemyTurn(roundState);

            if (outcomeType == RoundOutcomeType.Won)
                roundState.MarkWon();
            else if (outcomeType == RoundOutcomeType.Lost)
                roundState.MarkLost();

            return enemyIntentResult.DidExecute;
        }

        /// <summary>테스트용으로 현재 주사위 값을 강제로 설정한다.</summary>
        public void SetCurrentDiceValuesForTest(RoundState roundState, IReadOnlyList<int> diceValues)
        {
            if (roundState == null)
                throw new ArgumentNullException(nameof(roundState));

            if (diceValues == null)
                throw new ArgumentNullException(nameof(diceValues));

            if (diceValues.Count != roundState.Dice.Count)
                throw new ArgumentException("테스트 주사위 값 개수가 현재 주사위 개수와 다릅니다.", nameof(diceValues));

            for (int i = 0; i < diceValues.Count; i++)
            {
                if (diceValues[i] < 1 || diceValues[i] > 6)
                    throw new ArgumentOutOfRangeException(nameof(diceValues), $"주사위 값은 1~6 사이여야 합니다. Index: {i}");

                roundState.GetDice(i).SetCurrentFace(DiceFace.Number(diceValues[i]));
            }
        }

        /// <summary>현재 Round 상태에서 지정한 Cast 카테고리가 패턴과 테이블 규칙을 모두 통과하는지 시도한다.</summary>
        private bool TryResolvePatternAndTableRule(
            RoundState roundState,
            RollPatternType patternType,
            out PatternResult patternResult,
            out TableRuleEvaluationResult tableRuleResult)
        {
            patternResult = null;
            tableRuleResult = null;

            if (!roundState.CanUseCastThisRound(patternType))
                return false;

            List<int> diceValues = roundState.GetCurrentDiceValues();

            if (!patternEvaluator.TryEvaluateSpecificPattern(diceValues, patternType, out patternResult))
                return false;

            tableRuleResult = TableRuleEvaluator.Evaluate(roundState.RuleContext, patternResult);

            if (tableRuleResult.IsCastBlocked)
                return false;

            return true;
        }

        private CastSubmitResult SubmitResolvedCast(
    RoundState roundState,
    PatternResult patternResult,
    SlotPairDamagePreview slotPairDamagePreview,
    TableRuleEvaluationResult tableRuleResult)
        {
            if (roundState.CurrentAttempt.IsSubmitted)
                throw new InvalidOperationException("이미 제출된 Attempt입니다.");

            if (tableRuleResult.IsCastBlocked)
                throw new InvalidOperationException($"테이블 규칙으로 인해 제출할 수 없는 Cast입니다. PatternType: {patternResult.PatternType}");

            int damage = tableRuleResult.ModifiedDamage;

            roundState.Encounter.ApplyDamageToOpponent(damage);
            roundState.CurrentAttempt.MarkSubmitted();

            bool isBrokenCast = patternResult.PatternType == RollPatternType.BrokenCast;
            bool didGrantOvercharge = false;
            int grantedOvercharge = 0;
            int grantedFreeRerollTokens = 0;

            if (isBrokenCast && !tableRuleResult.IsBrokenCastRewardSuppressed)
                ApplyBrokenCastReward(roundState, out didGrantOvercharge, out grantedOvercharge, out grantedFreeRerollTokens);

            EnemyIntentResult enemyIntentResult = EnemyIntentResult.NotExecuted(roundState.Encounter.PlayerCurrentHP);

            RoundOutcomeType outcomeType = ResolveRoundOutcomeAfterPlayerCast(roundState);

            if (outcomeType == RoundOutcomeType.Won)
                roundState.MarkWon();

            bool canStartNextAttempt = !roundState.IsRoundEnded && roundState.CurrentAttempt.IsSubmitted;
            string message = BuildSubmitMessage(patternResult, outcomeType, canStartNextAttempt);

            CastSubmitResult result = new CastSubmitResult(
                roundState.CurrentAttempt.AttemptNumber,
                patternResult,
                tableRuleResult,
                damage,
                roundState.Encounter.OpponentCurrentHP,
                isBrokenCast,
                didGrantOvercharge,
                grantedOvercharge,
                grantedFreeRerollTokens,
                enemyIntentResult,
                outcomeType,
                canStartNextAttempt,
                message,
                slotPairDamagePreview);

            roundState.AddSubmitResult(result);
            return result;
        }

        /// <summary>플레이어 Cast 직후의 Round 결과만 판정한다. EnemyTurn 피해는 아직 적용하지 않는다.</summary>
        private static RoundOutcomeType ResolveRoundOutcomeAfterPlayerCast(RoundState roundState)
        {
            if (roundState.Encounter.IsOpponentDefeated)
                return RoundOutcomeType.Won;

            return RoundOutcomeType.Ongoing;
        }

        /// <summary>EnemyTurn 처리 이후 Round 결과를 판정한다.</summary>
        private static RoundOutcomeType ResolveRoundOutcomeAfterEnemyTurn(RoundState roundState)
        {
            if (roundState.Encounter.IsOpponentDefeated)
                return RoundOutcomeType.Won;

            if (roundState.Encounter.IsPlayerDefeated)
                return RoundOutcomeType.Lost;

            if (roundState.IsLastAttempt())
                return RoundOutcomeType.Lost;

            if (ShouldLoseBecauseNoRoundRollsRemain(roundState))
                return RoundOutcomeType.Lost;

            return RoundOutcomeType.Ongoing;
        }

        private static EnemyIntentResult ExecuteEnemyIntent(RoundState roundState)
        {
            EnemyIntent intent = roundState.CurrentEnemyIntent;

            if (intent.IntentType == EnemyIntentType.None)
                return EnemyIntentResult.NotExecuted(roundState.Encounter.PlayerCurrentHP);

            if (intent.IntentType == EnemyIntentType.Strike)
            {
                roundState.Encounter.ApplyDamageToPlayer(intent.Damage);

                return new EnemyIntentResult(
                    true,
                    EnemyIntentType.Strike,
                    intent.Damage,
                    roundState.Encounter.PlayerCurrentHP,
                    $"Enemy used Strike for {intent.Damage} damage.");
            }

            return EnemyIntentResult.NotExecuted(roundState.Encounter.PlayerCurrentHP);
        }

        private static AttemptState CreateAttempt(OverchargeState overcharge, int attemptNumber)
        {
            if (overcharge == null)
                throw new ArgumentNullException(nameof(overcharge));

            int freeRerollTokens = overcharge.DrainNextAttemptFreeRerollTokens();

            return new AttemptState(
                attemptNumber,
                0,
                freeRerollTokens);
        }

        private static void ApplyBrokenCastReward(
            RoundState roundState,
            out bool didGrantOvercharge,
            out int grantedOvercharge,
            out int grantedFreeRerollTokens)
        {
            didGrantOvercharge = false;
            grantedOvercharge = 0;
            grantedFreeRerollTokens = 0;

            RoundRuleContext rule = roundState.RuleContext;

            if (rule.BrokenCastGrantsOvercharge && rule.BrokenCastOverchargeAmount > 0)
            {
                roundState.Overcharge.AddOvercharge(rule.BrokenCastOverchargeAmount);
                didGrantOvercharge = true;
                grantedOvercharge = rule.BrokenCastOverchargeAmount;
            }

            if (rule.BrokenCastGrantsNextAttemptFreeReroll && rule.BrokenCastFreeRerollTokenAmount > 0)
            {
                roundState.Overcharge.AddNextAttemptFreeRerollTokens(rule.BrokenCastFreeRerollTokenAmount);
                grantedFreeRerollTokens = rule.BrokenCastFreeRerollTokenAmount;
            }
        }

        /// <summary>현재 Cast 제출 이후 Round 결과를 판정한다.</summary>
        private static RoundOutcomeType ResolveRoundOutcome(RoundState roundState)
        {
            if (roundState.Encounter.IsOpponentDefeated)
                return RoundOutcomeType.Won;

            if (roundState.Encounter.IsPlayerDefeated)
                return RoundOutcomeType.Lost;

            if (roundState.IsLastAttempt())
                return RoundOutcomeType.Lost;

            if (ShouldLoseBecauseNoRoundRollsRemain(roundState))
                return RoundOutcomeType.Lost;

            return RoundOutcomeType.Ongoing;
        }

        /// <summary>다음 Attempt는 남아 있지만 Round Roll Pool이 없어 더 이상 유효하게 진행할 수 없는지 확인한다.</summary>
        private static bool ShouldLoseBecauseNoRoundRollsRemain(RoundState roundState)
        {
            if (roundState == null)
                return false;

            if (roundState.CurrentAttempt == null)
                return false;

            if (!roundState.CurrentAttempt.IsSubmitted)
                return false;

            if (roundState.RemainingRoundRolls > 0)
                return false;

            return !roundState.IsLastAttempt();
        }

        private static void ValidatePlayableRound(RoundState roundState)
        {
            if (roundState == null)
                throw new ArgumentNullException(nameof(roundState));

            if (roundState.IsRoundEnded)
                throw new InvalidOperationException("이미 종료된 Round입니다.");
        }

        private static string BuildSubmitMessage(
            PatternResult patternResult,
            RoundOutcomeType outcomeType,
            bool canStartNextAttempt)
        {
            if (outcomeType == RoundOutcomeType.Won)
                return $"Round Won by {patternResult.PatternType}.";

            if (outcomeType == RoundOutcomeType.Lost)
                return $"Round Lost after {patternResult.PatternType}.";

            if (canStartNextAttempt)
                return $"Submitted {patternResult.PatternType}. Next Attempt is available.";

            return $"Submitted {patternResult.PatternType}.";
        }
    }
}
