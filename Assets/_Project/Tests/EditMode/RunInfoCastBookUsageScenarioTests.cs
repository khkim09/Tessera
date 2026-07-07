using System.Collections.Generic;
using NUnit.Framework;
using Tessera.Core;
using Tessera.UI;

namespace Tessera.Tests
{
    public class RunInfoCastBookUsageScenarioTests
    {
        private static readonly IReadOnlyList<int> LockSlots = new[] { 0, 1, 2, 3, 4 };
        private static readonly IReadOnlyList<SlotPairDeviceDefinition> NoDevices = new List<SlotPairDeviceDefinition>();

        [Test]
        public void SubmittedCastDisablesMatchingCastBookEntryAndUpdatesUsedTextForCurrentRound()
        {
            CoreRoundSimulator simulator = new CoreRoundSimulator(10);
            RoundRuleContext ruleContext = CreateRuleContext(maxAttempts: 2, opponentMaxHp: 999);
            RoundState roundState = simulator.StartRound(ruleContext, ruleContext.PlayerMaxHP, new OverchargeState());
            simulator.SetCurrentDiceValuesForTest(roundState, new[] { 1, 1, 1, 1, 1 });

            Assert.That(simulator.TryBuildPlayerClashCastResult(roundState, RollPatternType.Aces, LockSlots, NoDevices, out ClashCastResult playerResult), Is.True);
            Assert.That(simulator.TryBuildBestOpponentClashCastResult(roundState, new[] { 1, 2, 3, 4, 6 }, NoDevices, out ClashCastResult opponentResult), Is.True);

            simulator.ResolveClash(roundState, playerResult, opponentResult);

            RunInfoCastBookEntrySnapshot snapshot = CreateCastBookSnapshot(roundState, RollPatternType.Aces);
            CastBoardEntryModel boardEntry = CreateCastBoardEntry(roundState, RollPatternType.Aces);

            Assert.That(boardEntry.Status, Is.EqualTo(CastBoardEntryStatus.Used), "Cast 후보 UI는 이번 Round에 이미 제출된 Aces를 Used로 표시해야 한다.");
            Assert.That(snapshot.IsUnavailable, Is.True, "족보 UI used overlay는 같은 Round에서 제출 완료된 Cast에 활성화되어야 한다.");
            Assert.That(snapshot.RemainingUseText, Is.EqualTo("0"), "족보 UI Used/remaining text는 사용 후 0으로 갱신되어야 한다.");
        }

        [Test]
        public void UsedOverlayPersistsAcrossAttemptsUntilRoundEnds()
        {
            CoreRoundSimulator simulator = new CoreRoundSimulator(20);
            RoundRuleContext ruleContext = CreateRuleContext(maxAttempts: 2, opponentMaxHp: 999);
            RoundState roundState = simulator.StartRound(ruleContext, ruleContext.PlayerMaxHP, new OverchargeState());
            simulator.SetCurrentDiceValuesForTest(roundState, new[] { 1, 1, 1, 1, 1 });

            Assert.That(simulator.TryBuildPlayerClashCastResult(roundState, RollPatternType.Aces, LockSlots, NoDevices, out ClashCastResult playerResult), Is.True);
            Assert.That(simulator.TryBuildBestOpponentClashCastResult(roundState, new[] { 1, 2, 3, 4, 6 }, NoDevices, out ClashCastResult opponentResult), Is.True);
            ClashResolveResult resolveResult = simulator.ResolveClash(roundState, playerResult, opponentResult);
            Assert.That(resolveResult.CanStartNextAttempt, Is.True);

            Assert.That(simulator.TryStartNextAttempt(roundState), Is.True);

            RunInfoCastBookEntrySnapshot snapshot = CreateCastBookSnapshot(roundState, RollPatternType.Aces);
            CastBoardEntryModel boardEntry = CreateCastBoardEntry(roundState, RollPatternType.Aces);

            Assert.That(boardEntry.Status, Is.EqualTo(CastBoardEntryStatus.Used));
            Assert.That(snapshot.IsUnavailable, Is.True, "Attempt가 바뀌어도 같은 Round 안에서는 족보 UI used overlay가 유지되어야 한다.");
            Assert.That(snapshot.RemainingUseText, Is.EqualTo("0"));
        }

        [Test]
        public void UsedOverlayClearsWhenRoundEnds()
        {
            CoreRoundSimulator simulator = new CoreRoundSimulator(30);
            RoundRuleContext ruleContext = CreateRuleContext(maxAttempts: 1, opponentMaxHp: 1);
            RoundState roundState = simulator.StartRound(ruleContext, ruleContext.PlayerMaxHP, new OverchargeState());
            simulator.SetCurrentDiceValuesForTest(roundState, new[] { 6, 6, 6, 6, 6 });

            Assert.That(simulator.TryBuildPlayerClashCastResult(roundState, RollPatternType.Sixes, LockSlots, NoDevices, out ClashCastResult playerResult), Is.True);
            Assert.That(simulator.TryBuildBestOpponentClashCastResult(roundState, new[] { 1, 2, 3, 4, 6 }, NoDevices, out ClashCastResult opponentResult), Is.True);
            simulator.ResolveClash(roundState, playerResult, opponentResult);

            Assert.That(roundState.IsRoundEnded, Is.True);
            RunInfoCastBookEntrySnapshot snapshot = CreateCastBookSnapshot(roundState, RollPatternType.Sixes);

            Assert.That(roundState.GetPatternUseCount(RollPatternType.Sixes), Is.EqualTo(0), "Round 종료 시 Cast 사용 lock이 초기화되어야 한다.");
            Assert.That(snapshot.IsUnavailable, Is.False, "Round 종료 후 족보 UI used overlay는 비활성화되어야 한다.");
            Assert.That(snapshot.RemainingUseText, Is.EqualTo("1"), "Round 종료 후 사용 가능 횟수 text는 기본값으로 복구되어야 한다.");
        }

        private static RoundRuleContext CreateRuleContext(int maxAttempts, int opponentMaxHp)
        {
            return new RoundRuleContext(
                diceCount: 5,
                maxAttempts: maxAttempts,
                baseRollsPerAttempt: RoundState.DefaultPlayerBaseRollsPerAttempt,
                opponentBaseRollsPerAttempt: 3,
                playerMaxHP: 20,
                opponentMaxHP: opponentMaxHp,
                maxUsesPerCastPerRound: 1,
                maxBrokenCastUsesPerRound: 3,
                enemyStrikeDamage: 1,
                brokenCastGrantsOvercharge: true,
                brokenCastOverchargeAmount: 1,
                brokenCastGrantsNextAttemptFreeReroll: true,
                brokenCastFreeRerollTokenAmount: 1,
                impactCap: 0);
        }

        private static RunInfoCastBookEntrySnapshot CreateCastBookSnapshot(RoundState roundState, RollPatternType patternType)
        {
            int maxUses = roundState.RuleContext.MaxUsesPerCastPerRound;
            int remainingUses = System.Math.Max(0, maxUses - roundState.GetPatternUseCount(patternType));

            return new RunInfoCastBookEntrySnapshot(
                patternType,
                CastBoardCatalog.GetDisplayName(patternType),
                score: 0,
                forceValue: 0f,
                forceText: "0",
                castPower: 0,
                baseImpact: 0,
                remainingUses: remainingUses,
                maxUses: maxUses,
                isUnlimited: false,
                sortOrder: 0);
        }

        private static CastBoardEntryModel CreateCastBoardEntry(RoundState roundState, RollPatternType patternType)
        {
            CastBoardViewModel viewModel = CastBoardModelBuilder.CreateDefault().Build(roundState, roundState.GetCurrentDiceValues());
            foreach (CastBoardEntryModel entry in viewModel.Entries)
            {
                if (entry.PatternType == patternType)
                    return entry;
            }

            Assert.Fail($"{patternType} CastBoard entry was not created.");
            return null;
        }
    }
}
