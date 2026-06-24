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
        private readonly ImpactDamageCalculator impactDamageCalculator;

        /// <summary>시드 없는 랜덤 굴림 기반 Round 시뮬레이터를 생성한다.</summary>
        public CoreRoundSimulator()
        {
            diceRoller = new DiceRoller();
            patternEvaluator = PatternEvaluator.CreateDefault();
            slotPairDamageCalculator = new SlotPairDamageCalculator();
            impactDamageCalculator = new ImpactDamageCalculator();
        }

        /// <summary>고정 시드 기반 Round 시뮬레이터를 생성한다.</summary>
        public CoreRoundSimulator(int seed)
        {
            diceRoller = new DiceRoller(seed);
            patternEvaluator = PatternEvaluator.CreateDefault();
            slotPairDamageCalculator = new SlotPairDamageCalculator();
            impactDamageCalculator = new ImpactDamageCalculator();
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

            RoundState roundState = new RoundState(
                ruleContext,
                encounter,
                stageOverchargeState,
                dice,
                firstAttempt,
                enemyIntent);

            roundState.ApplyCurrentIntentInitiativeToAttempt();
            return roundState;
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

        /// <summary>잠기지 않은 주사위들을 현재 Attempt의 Roll 자원을 소모해 다시 굴린다.</summary>
        public bool TryRerollUnlockedDice(RoundState roundState)
        {
            ValidatePlayableRound(roundState);

            bool shouldRollAllDice = roundState.IsFirstRollThisAttempt;

            if (!roundState.TrySpendAttemptRoll())
                return false;

            if (shouldRollAllDice)
                diceRoller.RollAll(roundState.Dice);
            else
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

        /// <summary>외부 효과로 현재 Attempt가 Cast 가능한 주사위 상태를 확보했음을 표시한다.</summary>
        public void MarkCurrentAttemptCastReady(RoundState roundState, CastReadinessSource source)
        {
            ValidatePlayableRound(roundState);
            roundState.MarkCurrentAttemptCastReady(source);
        }

        /// <summary>SlotPair 기준 선택 Cast 피해 미리보기를 생성한다. HP에는 적용하지 않는다.</summary>
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

            List<int> diceValues = roundState.GetCurrentDiceValues();

            return TryBuildSlotPairDamagePreviewFromDiceValues(
                roundState,
                patternType,
                diceValues,
                lockSlotDiceIndexes,
                deviceDefinitions,
                out patternResult,
                out preview,
                out tableRuleResult);
        }

        /// <summary>지정 주체의 Cast 계산 결과를 생성한다. HP에는 적용하지 않는다.</summary>
        public bool TryBuildClashCastResult(
            RoundState roundState,
            ClashParticipantType owner,
            RollPatternType patternType,
            IReadOnlyList<int> lockSlotDiceIndexes,
            IReadOnlyList<SlotPairDeviceDefinition> deviceDefinitions,
            IReadOnlyList<int> diceValues,
            out ClashCastResult clashCastResult)
        {
            ValidatePlayableRound(roundState);

            clashCastResult = null;

            if (!TryBuildSlotPairDamagePreviewFromDiceValues(
                    roundState,
                    patternType,
                    diceValues,
                    lockSlotDiceIndexes,
                    deviceDefinitions,
                    out PatternResult patternResult,
                    out SlotPairDamagePreview preview,
                    out TableRuleEvaluationResult tableRuleResult))
            {
                return false;
            }

            clashCastResult = new ClashCastResult(
                owner,
                patternResult,
                preview,
                tableRuleResult,
                diceValues,
                lockSlotDiceIndexes,
                roundState.RuleContext,
                impactDamageCalculator);

            if (owner == ClashParticipantType.Player)
                roundState.CurrentAttempt.SetPlayerClashResult(clashCastResult);
            else
                roundState.CurrentAttempt.SetOpponentClashResult(clashCastResult);

            return true;
        }

        /// <summary>현재 플레이어 주사위 기준 Player Clash Cast 결과를 생성한다.</summary>
        public bool TryBuildPlayerClashCastResult(
            RoundState roundState,
            RollPatternType patternType,
            IReadOnlyList<int> lockSlotDiceIndexes,
            IReadOnlyList<SlotPairDeviceDefinition> deviceDefinitions,
            out ClashCastResult clashCastResult)
        {
            ValidatePlayableRound(roundState);

            return TryBuildClashCastResult(
                roundState,
                ClashParticipantType.Player,
                patternType,
                lockSlotDiceIndexes,
                deviceDefinitions,
                roundState.GetCurrentDiceValues(),
                out clashCastResult);
        }

        /// <summary>상대 주사위 값과 Device를 기준으로 가장 강한 Opponent Clash Cast 결과를 생성하고 Attempt에 기록한다.</summary>
        public bool TryBuildBestOpponentClashCastResult(
            RoundState roundState,
            IReadOnlyList<int> opponentDiceValues,
            IReadOnlyList<SlotPairDeviceDefinition> opponentDeviceDefinitions,
            out ClashCastResult clashCastResult)
        {
            return TryBuildBestOpponentClashCastResult(
                roundState,
                opponentDiceValues,
                opponentDeviceDefinitions,
                true,
                out clashCastResult);
        }

        /// <summary>상대 주사위 값과 Device를 기준으로 선택 정책에 맞는 Opponent Clash Cast 결과를 생성하고 Attempt에 기록한다.</summary>
        public bool TryBuildBestOpponentClashCastResult(
            RoundState roundState,
            IReadOnlyList<int> opponentDiceValues,
            IReadOnlyList<SlotPairDeviceDefinition> opponentDeviceDefinitions,
            OpponentCastSelectionPolicy castSelectionPolicy,
            out ClashCastResult clashCastResult)
        {
            return TryBuildBestOpponentClashCastResult(
                roundState,
                opponentDiceValues,
                opponentDeviceDefinitions,
                castSelectionPolicy != OpponentCastSelectionPolicy.DebugFirstValid,
                out clashCastResult);
        }

        /// <summary>상대 주사위 값과 Device를 기준으로 Opponent Clash Cast 결과를 생성하고 Attempt에 기록한다.</summary>
        public bool TryBuildBestOpponentClashCastResult(
            RoundState roundState,
            IReadOnlyList<int> opponentDiceValues,
            IReadOnlyList<SlotPairDeviceDefinition> opponentDeviceDefinitions,
            bool chooseBestAvailableCast,
            out ClashCastResult clashCastResult)
        {
            ValidatePlayableRound(roundState);

            clashCastResult = null;

            if (!TryBuildBestOpponentClashCastResultPreview(
                    roundState,
                    opponentDiceValues,
                    opponentDeviceDefinitions,
                    chooseBestAvailableCast,
                    out ClashCastResult previewResult))
            {
                return false;
            }

            roundState.CurrentAttempt.SetOpponentClashResult(previewResult);
            clashCastResult = previewResult;
            return true;
        }

        /// <summary>상대 주사위 값과 Device를 기준으로 선택 정책에 맞는 Opponent Clash Cast 결과를 계산하되 Attempt에는 기록하지 않는다.</summary>
        public bool TryBuildBestOpponentClashCastResultPreview(
            RoundState roundState,
            IReadOnlyList<int> opponentDiceValues,
            IReadOnlyList<SlotPairDeviceDefinition> opponentDeviceDefinitions,
            OpponentCastSelectionPolicy castSelectionPolicy,
            out ClashCastResult clashCastResult)
        {
            return TryBuildBestOpponentClashCastResultPreview(
                roundState,
                opponentDiceValues,
                opponentDeviceDefinitions,
                castSelectionPolicy != OpponentCastSelectionPolicy.DebugFirstValid,
                out clashCastResult);
        }

        /// <summary>상대 주사위 값과 Device를 기준으로 Opponent Clash Cast 결과를 계산하되 Attempt에는 기록하지 않는다.</summary>
        public bool TryBuildBestOpponentClashCastResultPreview(
            RoundState roundState,
            IReadOnlyList<int> opponentDiceValues,
            IReadOnlyList<SlotPairDeviceDefinition> opponentDeviceDefinitions,
            bool chooseBestAvailableCast,
            out ClashCastResult clashCastResult)
        {
            ValidatePlayableRound(roundState);

            if (opponentDiceValues == null)
                throw new ArgumentNullException(nameof(opponentDiceValues));

            clashCastResult = null;

            List<PatternResult> patternResults = patternEvaluator.EvaluateAll(opponentDiceValues);
            List<int> lockSlotDiceIndexes = CreateSequentialLockSlotDiceIndexes(opponentDiceValues.Count);
            ClashCastResult selectedResult = null;

            for (int i = 0; i < patternResults.Count; i++)
            {
                PatternResult candidatePattern = patternResults[i];

                if (candidatePattern.PatternType == RollPatternType.None || candidatePattern.PatternType == RollPatternType.BrokenCast)
                    continue;

                if (!TryBuildClashCastResultWithoutRecording(
                        roundState,
                        ClashParticipantType.Opponent,
                        candidatePattern.PatternType,
                        opponentDiceValues,
                        lockSlotDiceIndexes,
                        opponentDeviceDefinitions,
                        out ClashCastResult candidateResult))
                {
                    continue;
                }

                if (!chooseBestAvailableCast)
                {
                    selectedResult = candidateResult;
                    break;
                }

                if (!IsBetterOpponentCastCandidate(candidateResult, selectedResult))
                    continue;

                selectedResult = candidateResult;
            }

            if (selectedResult == null)
                return false;

            clashCastResult = selectedResult;
            return true;
        }

        /// <summary>상대 Cast 후보 중 더 좋은 결과인지 비교한다.</summary>
        private static bool IsBetterOpponentCastCandidate(ClashCastResult candidate, ClashCastResult currentBest)
        {
            if (candidate == null)
                return false;

            if (currentBest == null)
                return true;

            if (candidate.CastPower != currentBest.CastPower)
                return candidate.CastPower > currentBest.CastPower;

            if (candidate.ExpectedImpactDamage != currentBest.ExpectedImpactDamage)
                return candidate.ExpectedImpactDamage > currentBest.ExpectedImpactDamage;

            int candidateRawCastPower = candidate.SlotPairDamagePreview != null
                ? candidate.SlotPairDamagePreview.CastPowerBeforeTableRules
                : candidate.CastPower;

            int currentRawCastPower = currentBest.SlotPairDamagePreview != null
                ? currentBest.SlotPairDamagePreview.CastPowerBeforeTableRules
                : currentBest.CastPower;

            if (candidateRawCastPower != currentRawCastPower)
                return candidateRawCastPower > currentRawCastPower;

            return candidate.PatternType > currentBest.PatternType;
        }

        /// <summary>Player와 Opponent의 Cast 결과를 비교하여 승자 독식 피해를 적용한다.</summary>
        public ClashResolveResult ResolveClash(
            RoundState roundState,
            ClashCastResult playerResult,
            ClashCastResult opponentResult)
        {
            ValidatePlayableRound(roundState);

            if (playerResult == null)
                throw new ArgumentNullException(nameof(playerResult));

            if (opponentResult == null)
                throw new ArgumentNullException(nameof(opponentResult));

            int playerCastPower = Math.Max(0, playerResult.CastPower);
            int opponentCastPower = Math.Max(0, opponentResult.CastPower);

            ClashParticipantType? winner = null;
            ImpactDamageBreakdown playerImpactDamage = ImpactDamageBreakdown.Zero(roundState.RuleContext.ImpactCap);
            ImpactDamageBreakdown opponentImpactDamage = ImpactDamageBreakdown.Zero(roundState.RuleContext.ImpactCap);

            int appliedImpactDamageToPlayer = 0;
            int appliedImpactDamageToOpponent = 0;
            bool playerUsedBrokenCastDefense = false;

            if (playerCastPower > opponentCastPower)
            {
                winner = ClashParticipantType.Player;

                playerImpactDamage = impactDamageCalculator.Calculate(
                    roundState.RuleContext,
                    playerResult,
                    opponentResult,
                    100);

                appliedImpactDamageToOpponent = playerImpactDamage.AppliedImpactDamage;
            }
            else if (opponentCastPower > playerCastPower)
            {
                winner = ClashParticipantType.Opponent;

                int finalModifierPercent = 100;

                if (playerResult.IsBrokenCast)
                {
                    finalModifierPercent = 50;
                    playerUsedBrokenCastDefense = true;
                }

                opponentImpactDamage = impactDamageCalculator.Calculate(
                    roundState.RuleContext,
                    opponentResult,
                    playerResult,
                    finalModifierPercent);

                appliedImpactDamageToPlayer = opponentImpactDamage.AppliedImpactDamage;
            }

            if (appliedImpactDamageToOpponent > 0)
                roundState.Encounter.ApplyDamageToOpponent(appliedImpactDamageToOpponent);

            if (appliedImpactDamageToPlayer > 0)
                roundState.Encounter.ApplyDamageToPlayer(appliedImpactDamageToPlayer);

            bool didGrantOvercharge = false;
            int grantedOvercharge = 0;
            int grantedFreeRerollTokens = 0;

            if (playerResult.IsBrokenCast &&
                winner == ClashParticipantType.Opponent &&
                !playerResult.TableRuleEvaluationResult.IsBrokenCastRewardSuppressed)
            {
                ApplyBrokenCastReward(
                    roundState,
                    out didGrantOvercharge,
                    out grantedOvercharge,
                    out grantedFreeRerollTokens);
            }

            roundState.AddClashPatternUse(playerResult.PatternType);
            roundState.AddClashPatternUse(opponentResult.PatternType);

            RoundOutcomeType outcomeType = ResolveRoundOutcomeAfterClash(roundState);

            bool canStartNextAttempt =
                outcomeType == RoundOutcomeType.Ongoing &&
                !roundState.IsLastAttempt();

            string message = BuildClashMessage(
                roundState,
                winner,
                appliedImpactDamageToPlayer,
                appliedImpactDamageToOpponent,
                playerUsedBrokenCastDefense,
                outcomeType);

            ClashResolveResult result = new ClashResolveResult(
                roundState.CurrentAttempt.AttemptNumber,
                playerResult,
                opponentResult,
                winner,
                playerImpactDamage,
                opponentImpactDamage,
                appliedImpactDamageToPlayer,
                appliedImpactDamageToOpponent,
                playerUsedBrokenCastDefense,
                didGrantOvercharge,
                grantedOvercharge,
                grantedFreeRerollTokens,
                outcomeType,
                canStartNextAttempt,
                message);

            roundState.CurrentAttempt.MarkClashResolved(result);

            if (outcomeType == RoundOutcomeType.Won)
                roundState.MarkWon();
            else if (outcomeType == RoundOutcomeType.Lost)
                roundState.MarkLost();

            return result;
        }

        /// <summary>제출 이후 다음 Attempt 시작을 시도한다. 현재 Round 기본 EnemyIntent를 사용한다.</summary>
        public bool TryStartNextAttempt(RoundState roundState)
        {
            if (roundState == null)
                throw new ArgumentNullException(nameof(roundState));

            EnemyIntent fallbackIntent = EnemyIntent.Strike(roundState.RuleContext.EnemyStrikeDamage);

            return TryStartNextAttempt(roundState, fallbackIntent);
        }

        /// <summary>제출 이후 지정한 EnemyIntent를 적용해 다음 Attempt 시작을 시도한다.</summary>
        public bool TryStartNextAttempt(
            RoundState roundState,
            EnemyIntent nextEnemyIntent)
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

            EnemyIntent resolvedIntent = nextEnemyIntent
                ?? EnemyIntent.Strike(roundState.RuleContext.EnemyStrikeDamage);

            roundState.SetEnemyIntent(resolvedIntent);
            roundState.ApplyCurrentIntentInitiativeToAttempt();

            return true;
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

        /// <summary>현재 Round의 EnemyIntent를 설정하고 현재 Attempt의 Initiative에 반영한다.</summary>
        public void ApplyEnemyIntentToCurrentAttempt(RoundState roundState, EnemyIntent enemyIntent)
        {
            ValidatePlayableRound(roundState);

            EnemyIntent resolvedIntent = enemyIntent ?? EnemyIntent.None();

            roundState.SetEnemyIntent(resolvedIntent);
            roundState.ApplyCurrentIntentInitiativeToAttempt();
        }

        /// <summary>지정 주사위 값으로 SlotPair 피해 미리보기를 생성한다.</summary>
        private bool TryBuildSlotPairDamagePreviewFromDiceValues(
            RoundState roundState,
            RollPatternType patternType,
            IReadOnlyList<int> diceValues,
            IReadOnlyList<int> lockSlotDiceIndexes,
            IReadOnlyList<SlotPairDeviceDefinition> deviceDefinitions,
            out PatternResult patternResult,
            out SlotPairDamagePreview preview,
            out TableRuleEvaluationResult tableRuleResult)
        {
            patternResult = null;
            preview = null;
            tableRuleResult = null;

            if (diceValues == null)
                throw new ArgumentNullException(nameof(diceValues));

            if (!roundState.CanUseCastThisRound(patternType))
                return false;

            if (!patternEvaluator.TryEvaluateSpecificPattern(diceValues, patternType, out patternResult))
                return false;

            TableRuleEvaluationResult baseTableRuleResult = TableRuleEvaluator.Evaluate(roundState.RuleContext, patternResult);

            if (baseTableRuleResult.IsCastBlocked)
                return false;

            // StageThreat 조건 Device가 계산 중 판정될 수 있도록 SlotPair 계산 컨텍스트를 구성한다.
            SlotPairCalculationContext calculationContext = CreateSlotPairCalculationContext(roundState);

            // SlotPair 1~5를 좌에서 우로 계산하고, StageThreat 조건 Device까지 함께 평가한다.
            preview = slotPairDamageCalculator.Calculate(
                patternResult,
                diceValues,
                lockSlotDiceIndexes,
                deviceDefinitions,
                calculationContext);

            tableRuleResult = TableRuleEvaluator.Evaluate(
                roundState.RuleContext,
                patternResult.PatternType,
                preview.CastPowerBeforeTableRules);

            if (tableRuleResult.IsCastBlocked)
                return false;

            return true;
        }

        /// <summary>현재 Round 상태에서 SlotPair 계산에 필요한 외부 컨텍스트를 생성한다.</summary>
        private static SlotPairCalculationContext CreateSlotPairCalculationContext(RoundState roundState)
        {
            if (roundState == null)
                throw new ArgumentNullException(nameof(roundState));

            // RuleContext가 없을 경우 StageThreat 조건 Device는 기본값 0으로 평가한다.
            int stageThreatLevel = roundState.RuleContext != null
                ? roundState.RuleContext.StageThreatLevel
                : 0;

            // SlotPairDamageCalculator에는 읽기 전용 계산 컨텍스트만 전달한다.
            return new SlotPairCalculationContext(stageThreatLevel);
        }

        /// <summary>Attempt 상태에 기록하지 않고 Clash Cast 결과만 계산한다.</summary>
        private bool TryBuildClashCastResultWithoutRecording(
            RoundState roundState,
            ClashParticipantType owner,
            RollPatternType patternType,
            IReadOnlyList<int> diceValues,
            IReadOnlyList<int> lockSlotDiceIndexes,
            IReadOnlyList<SlotPairDeviceDefinition> deviceDefinitions,
            out ClashCastResult clashCastResult)
        {
            clashCastResult = null;

            if (!TryBuildSlotPairDamagePreviewFromDiceValues(
                    roundState,
                    patternType,
                    diceValues,
                    lockSlotDiceIndexes,
                    deviceDefinitions,
                    out PatternResult patternResult,
                    out SlotPairDamagePreview preview,
                    out TableRuleEvaluationResult tableRuleResult))
            {
                return false;
            }

            clashCastResult = new ClashCastResult(
                owner,
                patternResult,
                preview,
                tableRuleResult,
                diceValues,
                lockSlotDiceIndexes,
                roundState.RuleContext,
                impactDamageCalculator);

            return true;
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

        /// <summary>SlotPair 0~4에 DiceIndex 0~4를 순서대로 대응시킨다.</summary>
        private static List<int> CreateSequentialLockSlotDiceIndexes(int diceCount)
        {
            List<int> diceIndexes = new List<int>(SlotPairDamageCalculator.SlotPairCount);

            for (int slotIndex = 0; slotIndex < SlotPairDamageCalculator.SlotPairCount; slotIndex++)
            {
                if (slotIndex < diceCount)
                    diceIndexes.Add(slotIndex);
                else
                    diceIndexes.Add(-1);
            }

            return diceIndexes;
        }

        /// <summary>Clash 이후 승리/패배/진행 상태를 판정한다.</summary>
        private static RoundOutcomeType ResolveRoundOutcomeAfterClash(RoundState roundState)
        {
            if (roundState == null)
                throw new ArgumentNullException(nameof(roundState));

            if (roundState.Encounter.IsOpponentDefeated)
                return RoundOutcomeType.Won;

            if (roundState.Encounter.IsPlayerDefeated)
                return RoundOutcomeType.Lost;

            if (roundState.CurrentAttempt == null)
                return RoundOutcomeType.Lost;

            if (roundState.CurrentAttempt.AttemptNumber >= roundState.RuleContext.MaxAttempts)
                return RoundOutcomeType.Lost;

            return RoundOutcomeType.Ongoing;
        }

        private static void ValidatePlayableRound(RoundState roundState)
        {
            if (roundState == null)
                throw new ArgumentNullException(nameof(roundState));

            if (roundState.IsRoundEnded)
                throw new InvalidOperationException("이미 종료된 Round입니다.");
        }

        /// <summary>Clash 결과 메시지를 생성한다.</summary>
        private static string BuildClashMessage(
            RoundState roundState,
            ClashParticipantType? winner,
            int appliedImpactDamageToPlayer,
            int appliedImpactDamageToOpponent,
            bool playerUsedBrokenCastDefense,
            RoundOutcomeType outcomeType)
        {
            if (outcomeType == RoundOutcomeType.Won)
                return $"Clash won. Opponent took {appliedImpactDamageToOpponent} Impact.";

            if (outcomeType == RoundOutcomeType.Lost)
            {
                if (roundState != null && roundState.Encounter.IsPlayerDefeated)
                    return $"Round lost. Player HP reached 0 after taking {appliedImpactDamageToPlayer} Impact.";

                if (roundState != null && roundState.CurrentAttempt != null &&
                    roundState.CurrentAttempt.AttemptNumber >= roundState.RuleContext.MaxAttempts)
                    return "Round lost. No attempts remain.";


                return $"Round lost. Player took {appliedImpactDamageToPlayer} Impact.";
            }

            if (winner == ClashParticipantType.Player)
                return $"Player wins clash. Opponent took {appliedImpactDamageToOpponent} Impact.";

            if (winner == ClashParticipantType.Opponent)
            {
                if (playerUsedBrokenCastDefense)
                    return $"Opponent wins clash. Broken Cast reduced Impact to {appliedImpactDamageToPlayer}.";

                return $"Opponent wins clash. Player took {appliedImpactDamageToPlayer} Impact.";
            }

            return "Clash tied. No Impact.";
        }
    }
}
