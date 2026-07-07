#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Tessera.Core;
using Tessera.UI;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Tessera.Editor.Validation
{
    /// <summary>RunInfo 족보 UI의 Cast 사용 상태 연동을 검증한다.</summary>
    public static class RunInfoCastBookUsedOverlayScenarioTestV1
    {
        private const RollPatternType ScenarioPattern = RollPatternType.Sixes;

        [MenuItem("Tools/Tessera/Validation/Run RunInfo CastBook Used Overlay Scenario Test v1")]
        public static void Run()
        {
            int failures = 0;

            ValidateCastUseActivatesAndKeepsUsedOverlay(ref failures);
            ValidateCastUseResetsOnNewRound(ref failures);
            ValidateUsedTextRefreshesAfterCastUse(ref failures);

            if (failures > 0)
            {
                Debug.LogError($"[RunInfoCastBookUsedOverlayScenarioTest] FAIL Count={failures}");
                return;
            }

            Debug.Log("[RunInfoCastBookUsedOverlayScenarioTest] PASS All RunInfo CastBook used overlay scenario checks v1");
        }

        private static void ValidateCastUseActivatesAndKeepsUsedOverlay(ref int failures)
        {
            using (ScenarioContext context = ScenarioContext.Create())
            {
            RunInfoCastBookEntrySnapshot beforeUse = context.GetSnapshot(ScenarioPattern);
            bool builtAndResolved = context.ResolvePlayerCastUse(ScenarioPattern, new List<int> { 6, 6, 6, 6, 6 });
            RunInfoCastBookEntrySnapshot afterUse = context.GetSnapshot(ScenarioPattern);
            BoundEntryState entryState = BindEntry(afterUse);

            if (!builtAndResolved || beforeUse == null || afterUse == null || beforeUse.IsUnavailable || !afterUse.IsUnavailable || !entryState.OverlayActive)
            {
                Fail(ref failures, "UsedOverlayAfterCast", "Cast 사용 후 족보 snapshot unavailable=true, overlay active=true", $"Built={builtAndResolved}, BeforeUnavailable={beforeUse?.IsUnavailable}, AfterUnavailable={afterUse?.IsUnavailable}, Overlay={entryState.OverlayActive}");
                return;
            }

            RunInfoCastBookEntrySnapshot refreshedAfterUse = context.GetSnapshot(ScenarioPattern);
            BoundEntryState refreshedEntryState = BindEntry(refreshedAfterUse);

            if (refreshedAfterUse == null || !refreshedAfterUse.IsUnavailable || !refreshedEntryState.OverlayActive)
                Fail(ref failures, "UsedOverlayPersistsInRound", "같은 Round 재갱신 후에도 used overlay 유지", $"Unavailable={refreshedAfterUse?.IsUnavailable}, Overlay={refreshedEntryState.OverlayActive}");
            else
                Debug.Log("[RunInfoCastBookUsedOverlayScenarioTest] PASS UsedOverlayAfterCastAndPersists");
            }
        }

        private static void ValidateCastUseResetsOnNewRound(ref int failures)
        {
            using (ScenarioContext context = ScenarioContext.Create())
            {
            bool builtAndResolved = context.ResolvePlayerCastUse(ScenarioPattern, new List<int> { 6, 6, 6, 6, 6 });
            RunInfoCastBookEntrySnapshot usedSnapshot = context.GetSnapshot(ScenarioPattern);

            context.StartFreshRound();
            RunInfoCastBookEntrySnapshot resetSnapshot = context.GetSnapshot(ScenarioPattern);
            BoundEntryState resetEntryState = BindEntry(resetSnapshot);

            if (!builtAndResolved || usedSnapshot == null || !usedSnapshot.IsUnavailable || resetSnapshot == null || resetSnapshot.IsUnavailable || resetEntryState.OverlayActive)
                Fail(ref failures, "UsedOverlayRoundReset", "새 Round에서 remaining use 및 overlay 초기화", $"Built={builtAndResolved}, UsedUnavailable={usedSnapshot?.IsUnavailable}, ResetUnavailable={resetSnapshot?.IsUnavailable}, ResetOverlay={resetEntryState.OverlayActive}");
            else
                Debug.Log("[RunInfoCastBookUsedOverlayScenarioTest] PASS UsedOverlayRoundReset");
            }
        }

        private static void ValidateUsedTextRefreshesAfterCastUse(ref int failures)
        {
            using (ScenarioContext context = ScenarioContext.Create())
            {
            RunInfoCastBookEntrySnapshot beforeUse = context.GetSnapshot(ScenarioPattern);
            BoundEntryState beforeEntryState = BindEntry(beforeUse);

            bool builtAndResolved = context.ResolvePlayerCastUse(ScenarioPattern, new List<int> { 6, 6, 6, 6, 6 });
            RunInfoCastBookEntrySnapshot afterUse = context.GetSnapshot(ScenarioPattern);
            BoundEntryState afterEntryState = BindEntry(afterUse);

            if (!builtAndResolved || beforeEntryState.RemainingUseText != "1" || afterEntryState.RemainingUseText != "0" || afterUse == null || afterUse.RemainingUses != 0)
                Fail(ref failures, "UsedTextRefresh", "사용 전 text=1, 사용 후 text=0", $"Built={builtAndResolved}, BeforeText={beforeEntryState.RemainingUseText}, AfterText={afterEntryState.RemainingUseText}, AfterRemaining={afterUse?.RemainingUses}");
            else
                Debug.Log("[RunInfoCastBookUsedOverlayScenarioTest] PASS UsedTextRefresh");
            }
        }

        private static BoundEntryState BindEntry(RunInfoCastBookEntrySnapshot snapshot)
        {
            GameObject root = new GameObject("RunInfoCastBookEntryScenarioRoot");
            GameObject textObject = new GameObject("RemainingUseText");
            GameObject overlayObject = new GameObject("UnavailableOverlay");

            try
            {
                textObject.transform.SetParent(root.transform);
                overlayObject.transform.SetParent(root.transform);
                overlayObject.SetActive(false);

                RunInfoCastBookEntryView entryView = root.AddComponent<RunInfoCastBookEntryView>();
                TMP_Text remainingUseText = textObject.AddComponent<TextMeshPro>();

                SetPrivateField(entryView, "remainingUseText", remainingUseText);
                SetPrivateField(entryView, "unavailableOverlay", overlayObject);

                entryView.Bind(snapshot);

                return new BoundEntryState(overlayObject.activeSelf, remainingUseText.text);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
                throw new MissingFieldException(target.GetType().Name, fieldName);

            field.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
                throw new MissingMethodException(target.GetType().Name, methodName);

            method.Invoke(target, null);
        }

        private static void Fail(ref int failures, string location, string expected, string actual)
        {
            failures++;
            Debug.LogError($"[RunInfoCastBookUsedOverlayScenarioTest] FAIL Location={location} Expected={expected} Actual={actual}");
        }

        private readonly struct BoundEntryState
        {
            public bool OverlayActive { get; }
            public string RemainingUseText { get; }

            public BoundEntryState(bool overlayActive, string remainingUseText)
            {
                OverlayActive = overlayActive;
                RemainingUseText = remainingUseText;
            }
        }

        private sealed class ScenarioContext : IDisposable
        {
            private readonly CoreRoundSimulator simulator;
            private readonly RoundRuleContext ruleContext;
            private readonly GameObject presenterObject;
            private readonly TesseraGameplayBattlePresenter presenter;
            private RoundState roundState;

            private ScenarioContext(CoreRoundSimulator simulator, RoundRuleContext ruleContext, GameObject presenterObject, TesseraGameplayBattlePresenter presenter, RoundState roundState)
            {
                this.simulator = simulator;
                this.ruleContext = ruleContext;
                this.presenterObject = presenterObject;
                this.presenter = presenter;
                this.roundState = roundState;
            }

            public static ScenarioContext Create()
            {
                CoreRoundSimulator simulator = new CoreRoundSimulator(20260707);
                RoundRuleContext ruleContext = RoundRuleContext.CreateDefault();
                RoundState roundState = simulator.StartRound(ruleContext, 20, new OverchargeState());

                GameObject presenterObject = new GameObject("RunInfoCastBookScenarioPresenter");
                TesseraGameplayBattlePresenter presenter = presenterObject.AddComponent<TesseraGameplayBattlePresenter>();

                ScenarioContext context = new ScenarioContext(simulator, ruleContext, presenterObject, presenter, roundState);
                context.BindPresenterState();
                context.RefreshRunInfoCache();
                return context;
            }

            public RunInfoCastBookEntrySnapshot GetSnapshot(RollPatternType patternType)
            {
                RefreshRunInfoCache();
                return presenter.BuildRunInfoCastBookSnapshots().FirstOrDefault(snapshot => snapshot.PatternType == patternType);
            }

            public bool ResolvePlayerCastUse(RollPatternType patternType, IReadOnlyList<int> diceValues)
            {
                simulator.SetCurrentDiceValuesForTest(roundState, diceValues);
                simulator.MarkCurrentAttemptCastReady(roundState, CastReadinessSource.RollPerformed);

                List<int> lockSlots = new List<int> { 0, 1, 2, 3, 4 };
                List<SlotPairDeviceDefinition> devices = CreateEmptyDevices();

                bool playerBuilt = simulator.TryBuildPlayerClashCastResult(roundState, patternType, lockSlots, devices, out ClashCastResult playerResult);
                bool opponentBuilt = simulator.TryBuildClashCastResult(roundState, ClashParticipantType.Opponent, RollPatternType.Chance, lockSlots, devices, new List<int> { 1, 1, 1, 1, 1 }, out ClashCastResult opponentResult);

                if (!playerBuilt || !opponentBuilt)
                    return false;

                simulator.ResolveClash(roundState, playerResult, opponentResult);
                RefreshRunInfoCache();
                return roundState.GetPatternUseCount(patternType) == 1;
            }

            public void StartFreshRound()
            {
                roundState = simulator.StartRound(ruleContext, 20, new OverchargeState());
                BindPresenterState();
                RefreshRunInfoCache();
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(presenterObject);
            }

            private void BindPresenterState()
            {
                SetPrivateField(presenter, "simulator", simulator);
                SetPrivateField(presenter, "roundState", roundState);
                SetPrivateField(presenter, "castBoardModelBuilder", CastBoardModelBuilder.CreateDefault());
            }

            private void RefreshRunInfoCache()
            {
                InvokePrivate(presenter, "MarkRunInfoCastBookCacheDirty");
                InvokePrivate(presenter, "RefreshRunInfoCastBookFallbackCache");
            }

            private static List<SlotPairDeviceDefinition> CreateEmptyDevices()
            {
                return new List<SlotPairDeviceDefinition> { null, null, null, null, null };
            }
        }
    }
}
#endif
