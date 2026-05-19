using Tessera.Core;
using Tessera.Data;
using Tessera.UI;
using UnityEditor;
using UnityEngine;

namespace Tessera.Editor
{
    /// <summary>SlotPairDeviceDefinitionSO 테스트 에셋 3종을 생성/재생성하고 Presenter에 할당하는 Editor 유틸리티.</summary>
    public static class SlotPairDeviceTestAssetCreator
    {
        private const string FolderPath = "Assets/_Project/ScriptableObjects/Devices";

        // ── 에셋 경로 상수 ──────────────────────────────────────────────
        private const string AssetPathA = FolderPath + "/SlotPairDevice_AddScore_DiceValueX2.asset";
        private const string AssetPathB = FolderPath + "/SlotPairDevice_SamePreviousForce1.asset";
        private const string AssetPathC = FolderPath + "/SlotPairDevice_ForceOver4_X1_5.asset";

        /// <summary>Tools > Tessera > Devices > Create Test SlotPair Devices 메뉴 항목.</summary>
        [MenuItem("Tools/Tessera/Devices/Create Test SlotPair Devices")]
        private static void CreateTestAssets()
        {
            EnsureFolderExists();
            DeleteExistingAssets();
            CreateFreshAssets();
            FinalizeCreation("3 test SlotPairDevice assets recreated successfully.");
        }

        /// <summary>Tools > Tessera > Devices > Create And Assign Test SlotPair Devices To Selected Presenter 메뉴 항목.</summary>
        [MenuItem("Tools/Tessera/Devices/Create And Assign Test SlotPair Devices To Selected Presenter")]
        private static void CreateAndAssignToSelectedPresenter()
        {
            EnsureFolderExists();
            DeleteExistingAssets();
            CreateFreshAssets();
            FinalizeCreation("3 test SlotPairDevice assets recreated successfully.");

            // 선택된 GameObject에서 Presenter 찾기
            GameObject selected = Selection.activeGameObject;

            if (selected == null)
            {
                Debug.LogWarning("[SlotPairDeviceTestAssetCreator] No GameObject selected. Assets created but not assigned.");
                return;
            }

            TesseraGameplayBattlePresenter presenter = selected.GetComponent<TesseraGameplayBattlePresenter>();

            if (presenter == null)
            {
                Debug.LogWarning($"[SlotPairDeviceTestAssetCreator] Selected GameObject '{selected.name}' has no TesseraGameplayBattlePresenter. Assets created but not assigned.");
                return;
            }

            // SerializedObject로 private serialized 필드 slotPairDevices에 접근
            SerializedObject so = new SerializedObject(presenter);
            SerializedProperty devicesProp = so.FindProperty("slotPairDevices");

            if (devicesProp == null || !devicesProp.isArray)
            {
                Debug.LogError("[SlotPairDeviceTestAssetCreator] Could not find slotPairDevices SerializedProperty on presenter.");
                return;
            }

            // 배열 크기가 5인지 확인하고 맞춤
            if (devicesProp.arraySize != 5)
                devicesProp.arraySize = 5;

            // slotPairDevices[0] = SlotPairDevice_AddScore_DiceValueX2
            devicesProp.GetArrayElementAtIndex(0).objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<SlotPairDeviceDefinitionSO>(AssetPathA);

            // slotPairDevices[1] = SlotPairDevice_SamePreviousForce1
            devicesProp.GetArrayElementAtIndex(1).objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<SlotPairDeviceDefinitionSO>(AssetPathB);

            // slotPairDevices[2] = null
            devicesProp.GetArrayElementAtIndex(2).objectReferenceValue = null;

            // slotPairDevices[3] = SlotPairDevice_ForceOver4_X1_5
            devicesProp.GetArrayElementAtIndex(3).objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<SlotPairDeviceDefinitionSO>(AssetPathC);

            // slotPairDevices[4] = null
            devicesProp.GetArrayElementAtIndex(4).objectReferenceValue = null;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(presenter);

            Debug.Log($"[SlotPairDeviceTestAssetCreator] Assigned devices to presenter on '{selected.name}'.");
        }

        /// <summary>대상 폴더가 없으면 생성한다.</summary>
        private static void EnsureFolderExists()
        {
            if (!AssetDatabase.IsValidFolder(FolderPath))
            {
                string parent = "Assets/_Project/ScriptableObjects";
                string newFolder = "Devices";
                AssetDatabase.CreateFolder(parent, newFolder);
                Debug.Log($"[SlotPairDeviceTestAssetCreator] Created folder: {FolderPath}");
            }
        }

        /// <summary>기존 테스트 에셋 3종을 모두 삭제한다. 없으면 스킵한다.</summary>
        private static void DeleteExistingAssets()
        {
            DeleteAssetIfExists(AssetPathA);
            DeleteAssetIfExists(AssetPathB);
            DeleteAssetIfExists(AssetPathC);
        }

        /// <summary>지정 경로에 에셋이 있으면 삭제한다.</summary>
        private static void DeleteAssetIfExists(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
                Debug.Log($"[SlotPairDeviceTestAssetCreator] Deleted existing asset: {path}");
            }
        }

        /// <summary>3개의 테스트 에셋을 새로 생성한다.</summary>
        private static void CreateFreshAssets()
        {
            CreateAssetA();
            CreateAssetB();
            CreateAssetC();
        }

        /// <summary>에셋 A: Dice Value Doubler (AddScoreByDiceValue, intValue=2)를 생성한다.</summary>
        private static void CreateAssetA()
        {
            SlotPairDeviceDefinitionSO asset = ScriptableObject.CreateInstance<SlotPairDeviceDefinitionSO>();
            AssetDatabase.CreateAsset(asset, AssetPathA);

            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("deviceId").stringValue = "device.add_score.dice_value_x2";
            so.FindProperty("displayName").stringValue = "Dice Value Doubler";
            so.FindProperty("description").stringValue = "Adds current dice value x2 to Score.";
            so.FindProperty("deviceType").enumValueIndex = (int)SlotPairDeviceType.AddScoreByDiceValue;
            so.FindProperty("intValue").intValue = 2;
            so.FindProperty("floatValue").floatValue = 1f;
            so.FindProperty("forceThreshold").floatValue = 0f;
            so.FindProperty("requiredPatternType").enumValueIndex = (int)RollPatternType.None;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(asset);
        }

        /// <summary>에셋 B: Echo Gear (AddForceIfSameAsPrevious, intValue=1)를 생성한다.</summary>
        private static void CreateAssetB()
        {
            SlotPairDeviceDefinitionSO asset = ScriptableObject.CreateInstance<SlotPairDeviceDefinitionSO>();
            AssetDatabase.CreateAsset(asset, AssetPathB);

            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("deviceId").stringValue = "device.force.same_previous_1";
            so.FindProperty("displayName").stringValue = "Echo Gear";
            so.FindProperty("description").stringValue = "Adds Force +1 if this dice matches the previous slot dice.";
            so.FindProperty("deviceType").enumValueIndex = (int)SlotPairDeviceType.AddForceIfSameAsPrevious;
            so.FindProperty("intValue").intValue = 1;
            so.FindProperty("floatValue").floatValue = 1f;
            so.FindProperty("forceThreshold").floatValue = 0f;
            so.FindProperty("requiredPatternType").enumValueIndex = (int)RollPatternType.None;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(asset);
        }

        /// <summary>에셋 C: Pressure Amplifier (MultiplyForceIfCurrentForceAtLeast, floatValue=1.5, forceThreshold=4)를 생성한다.</summary>
        private static void CreateAssetC()
        {
            SlotPairDeviceDefinitionSO asset = ScriptableObject.CreateInstance<SlotPairDeviceDefinitionSO>();
            AssetDatabase.CreateAsset(asset, AssetPathC);

            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("deviceId").stringValue = "device.force_over_4_x1_5";
            so.FindProperty("displayName").stringValue = "Pressure Amplifier";
            so.FindProperty("description").stringValue = "Multiplies Force by 1.5 if current Force is at least 4.";
            so.FindProperty("deviceType").enumValueIndex = (int)SlotPairDeviceType.MultiplyForceIfCurrentForceAtLeast;
            so.FindProperty("intValue").intValue = 0;
            so.FindProperty("floatValue").floatValue = 1.5f;
            so.FindProperty("forceThreshold").floatValue = 4f;
            so.FindProperty("requiredPatternType").enumValueIndex = (int)RollPatternType.None;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(asset);
        }

        /// <summary>에셋 저장, 리프레시, 폴더 선택, 로그 출력을 수행한다.</summary>
        private static void FinalizeCreation(string message)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 생성된 폴더를 Project Window에서 선택
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(FolderPath);
            EditorGUIUtility.PingObject(Selection.activeObject);

            Debug.Log($"[SlotPairDeviceTestAssetCreator] {message}");
        }
    }
}
