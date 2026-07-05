#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Tessera.Editor.Validation
{
    /// <summary>디버그 Shop 치트 입력이 Update 폴링/GetKeyDown이 아닌 New Input System 이벤트 경로인지 검증한다.</summary>
    public static class DebugShopCheatInputScenarioTestV1
    {
        private const string StageFlowControllerPath = "Assets/_Project/Scripts/Runtime/Stage/StageBountyFlowController.cs";
        private const string StageFlowEventsPath = "Assets/_Project/Scripts/Runtime/Events/StageFlowEvents.cs";
        private const string RuntimeAsmdefPath = "Assets/_Project/Scripts/Runtime/Tessera.Runtime.asmdef";

        /// <summary>Editor 메뉴에서 디버그 Shop 치트 입력 경로 검증을 실행한다.</summary>
        [MenuItem("Tools/Tessera/Validation/Run Debug Shop Cheat Input Scenario Test v1")]
        public static void Run()
        {
            int failures = 0;

            ValidateStageFlowController(ref failures);
            ValidateStageFlowEvents(ref failures);
            ValidateRuntimeAsmdef(ref failures);

            if (failures > 0)
            {
                Debug.LogError($"[DebugShopCheatScenarioTest] FAIL Count={failures}");
                return;
            }

            Debug.Log("[DebugShopCheatScenarioTest] PASS All Debug Shop Cheat input scenario checks v1");
        }

        private static void ValidateStageFlowController(ref int failures)
        {
            string source = ReadTextAsset(StageFlowControllerPath, ref failures);
            if (string.IsNullOrEmpty(source))
                return;

            PassIfContains(source, "using UnityEngine.InputSystem;", "StageFlowController UsesNewInputSystem", ref failures);
            PassIfContains(source, "new InputAction(", "StageFlowController CreatesInputAction", ref failures);
            PassIfContains(source, "debugShopCheatAction.performed += HandleDebugShopCheatActionPerformed;", "StageFlowController SubscribesPerformed", ref failures);
            PassIfContains(source, "TesseraEventBus.Publish(new DebugShopCheatRequestedEvent", "StageFlowController PublishesDebugEvent", ref failures);
            PassIfContains(source, "TesseraEventBus.Subscribe<DebugShopCheatRequestedEvent>", "StageFlowController SubscribesDebugEvent", ref failures);
            PassIfNotContains(source, "Input.GetKeyDown", "StageFlowController NoLegacyGetKeyDown", ref failures);
            PassIfNotContains(source, "private void Update()", "StageFlowController NoUpdatePolling", ref failures);
        }

        private static void ValidateStageFlowEvents(ref int failures)
        {
            string source = ReadTextAsset(StageFlowEventsPath, ref failures);
            if (string.IsNullOrEmpty(source))
                return;

            PassIfContains(source, "public readonly struct DebugShopCheatRequestedEvent", "StageFlowEvents DefinesDebugEvent", ref failures);
        }

        private static void ValidateRuntimeAsmdef(ref int failures)
        {
            string source = ReadTextAsset(RuntimeAsmdefPath, ref failures);
            if (string.IsNullOrEmpty(source))
                return;

            PassIfContains(source, "\"Unity.InputSystem\"", "RuntimeAsmdef ReferencesInputSystem", ref failures);
        }

        private static string ReadTextAsset(string assetPath, ref int failures)
        {
            if (File.Exists(assetPath))
                return File.ReadAllText(assetPath);

            failures++;
            Debug.LogError($"[DebugShopCheatScenarioTest] FAIL MissingAsset Path={assetPath}");
            return string.Empty;
        }

        private static void PassIfContains(string source, string expected, string label, ref int failures)
        {
            if (source.Contains(expected))
            {
                Debug.Log($"[DebugShopCheatScenarioTest] PASS {label}");
                return;
            }

            failures++;
            Debug.LogError($"[DebugShopCheatScenarioTest] FAIL {label} Missing={expected}");
        }

        private static void PassIfNotContains(string source, string forbidden, string label, ref int failures)
        {
            if (!source.Contains(forbidden))
            {
                Debug.Log($"[DebugShopCheatScenarioTest] PASS {label}");
                return;
            }

            failures++;
            Debug.LogError($"[DebugShopCheatScenarioTest] FAIL {label} Forbidden={forbidden}");
        }
    }
}
#endif
