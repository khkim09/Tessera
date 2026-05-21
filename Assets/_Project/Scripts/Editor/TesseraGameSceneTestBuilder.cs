using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

using UnityEngine.UI;
using System.Collections.Generic;


namespace Tessera.Editor
{
    /// <summary>GameScene_Test 전용 Battle Table Scene Hierarchy를 자동 생성하는 Editor Tool.
    /// Main Camera GameView 기준으로 1인칭 테이블 전투 구도를 구성한다.</summary>
    public static class TesseraGameSceneTestBuilder
    {
        // ── 씬 경로 상수 ──────────────────────────────────────────────────
        private const string ScenePath = "Assets/_Project/Scenes/GameScene_Test.unity";

        // ── Hierarchy 이름 상수 ────────────────────────────────────────────
        private const string RootName = "TesseraSceneRoot";
        private const string SystemsName = "Systems";
        private const string BattleTableRootName = "BattleTableRoot";
        private const string CameraRigName = "CameraRig";
        private const string CameraTargetsName = "CameraTargets";
        private const string TableRootName = "TableRoot";
        private const string TableMeshName = "TableMesh";
        private const string InfoBoard3DName = "InfoBoard3D";
        private const string InfoBoardBodyName = "InfoBoardBody";
        private const string InfoBoardScreenName = "InfoBoardScreen";
        private const string DiceTray3DName = "DiceTray3D";
        private const string TrayFloorName = "TrayFloor";
        private const string WallBackName = "Wall_Back";
        private const string WallFrontName = "Wall_Front";
        private const string WallLeftName = "Wall_Left";
        private const string WallRightName = "Wall_Right";
        private const string DiceSpawnAreaName = "DiceSpawnArea";
        private const string DicePointsName = "DicePoints";
        private const string LockSlotRack3DName = "LockSlotRack3D";
        private const string DeviceRack3DName = "DeviceRack3D";
        private const string ConsumableRack3DName = "ConsumableRack3D";
        private const string ActionButtons3DName = "ActionButtons3D";
        private const string OpponentRootName = "OpponentRoot";
        private const string OpponentSilhouetteName = "OpponentSilhouette";
        private const string HammerAnchorName = "HammerAnchor";
        private const string IntentAnchorName = "IntentAnchor";
        private const string BattleWorldAnchorsName = "BattleWorldAnchors";
        private const string CastCandidateAnchorName = "CastCandidateAnchor";
        private const string DeviceTooltipAnchorName = "DeviceTooltipAnchor";
        private const string MessageAnchorName = "MessageAnchor";
        private const string ProductionCanvasRootName = "ProductionCanvasRoot";
        private const string DebugCanvasRootName = "DebugCanvasRoot";
        private const string LightingName = "Lighting";

        // ── 메뉴 항목 ──────────────────────────────────────────────────────

        /// <summary>Tools > Tessera > Scene > Rebuild GameScene Test Battle Table 메뉴 항목.
        /// GameScene_Test.unity 씬을 열고 BattleTableRoot를 Main Camera 기준 1인칭 구도로 재생성한다.</summary>
        [MenuItem("Tools/Tessera/Scene/Rebuild GameScene Test Battle Table")]
        private static void RebuildGameSceneTestBattleTable()
        {
            // 씬 열기 또는 생성
            OpenOrCreateScene();

            // 기존 TesseraSceneRoot가 있으면 삭제
            DeleteExistingTesseraSceneRoot();

            // 전체 Hierarchy 생성
            CreateFullHierarchy();

            // 씬 저장
            SaveScene();

            Debug.Log("[Tessera] GameScene_Test battle table rebuilt with camera-framed layout.");
        }

        // ── 씬 관리 ────────────────────────────────────────────────────────

        /// <summary>GameScene_Test.unity 씬을 열거나, 없으면 새로 생성하여 저장한다.</summary>
        private static void OpenOrCreateScene()
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset != null)
            {
                // 씬이 존재하면 연다
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ScenePath);
            }
            else
            {
                // 씬이 없으면 새 씬 생성 후 저장
                UnityEngine.SceneManagement.Scene newScene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                    UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects,
                    UnityEditor.SceneManagement.NewSceneMode.Single
                );
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(newScene, ScenePath);
            }
        }

        /// <summary>현재 씬에서 TesseraSceneRoot 오브젝트를 찾아 안전하게 삭제한다.
        /// Inspector 참조 끊김 오류를 방지하기 위해 Selection을 먼저 해제한다.</summary>
        private static void DeleteExistingTesseraSceneRoot()
        {
            GameObject existingRoot = GameObject.Find(RootName);
            if (existingRoot != null)
            {
                // Inspector 참조 끊김 방지를 위해 Selection 해제
                if (Selection.activeGameObject == existingRoot)
                {
                    Selection.activeGameObject = null;
                }
                Object.DestroyImmediate(existingRoot);
            }
        }

        /// <summary>씬을 dirty 처리하고 저장한다.</summary>
        private static void SaveScene()
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene()
            );
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        }

        // ── 전체 Hierarchy 생성 ────────────────────────────────────────────

        /// <summary>전체 Battle Table Hierarchy를 생성하고 최종적으로 TesseraSceneRoot를 선택한다.</summary>
        private static void CreateFullHierarchy()
        {
            // TesseraSceneRoot
            GameObject root = CreateEmpty(RootName, null);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            // 기존 Main Camera, Directional Light, EventSystem 처리
            MoveExistingCoreObjects(root);

            // Systems
            GameObject systems = CreateEmpty(SystemsName, root.transform);
            MoveEventSystemUnder(systems);
            CreateEmpty("RunSessionController", systems.transform);
            CreateEmpty("GameModeRoot", systems.transform);
            CreateEmpty("TesseraInputRouter", systems.transform);

            // BattleTableRoot
            GameObject battleTableRoot = CreateEmpty(BattleTableRootName, root.transform);

            // CameraRig
            GameObject cameraRig = CreateEmpty(CameraRigName, battleTableRoot.transform);
            MoveMainCameraUnder(cameraRig);
            SetupMainCamera();

            // CameraTargets
            GameObject cameraTargets = CreateEmpty(CameraTargetsName, cameraRig.transform);
            CreateCameraTarget(cameraTargets, "BattleTableViewTarget", new Vector3(0.15f, 1.55f, -2.25f), new Vector3(56f, -3f, 0f));
            CreateCameraTarget(cameraTargets, "OpponentViewTarget", new Vector3(0f, 1.62f, -2.15f), new Vector3(43f, 0f, 0f));
            CreateCameraTarget(cameraTargets, "ImpactViewTarget", new Vector3(0f, 1.58f, -2.25f), new Vector3(48f, 0f, 0f));

            // TableRoot
            GameObject tableRoot = CreateEmpty(TableRootName, battleTableRoot.transform);
            tableRoot.transform.localPosition = Vector3.zero;

            // TableMesh
            CreateTableMesh(tableRoot);

            // InfoBoard3D
            CreateInfoBoard3D(tableRoot);

            // DiceTray3D
            CreateDiceTray3D(tableRoot);

            // LockSlotRack3D
            CreateLockSlotRack3D(tableRoot);

            // DeviceRack3D
            CreateDeviceRack3D(tableRoot);

            // ConsumableRack3D
            CreateConsumableRack3D(tableRoot);

            // ActionButtons3D
            CreateActionButtons3D(tableRoot);

            // OpponentRoot
            CreateOpponentRoot(battleTableRoot);

            // BattleWorldAnchors
            CreateBattleWorldAnchors(battleTableRoot);

            // ProductionCanvasRoot
            GameObject productionCanvasRoot = CreateEmpty(ProductionCanvasRootName, root.transform);
            CreateScreenSpaceCanvases(productionCanvasRoot);

            // DebugCanvasRoot
            GameObject debugCanvasRoot = CreateEmpty(DebugCanvasRootName, root.transform);
            CreateDebugPrototypeCanvas(debugCanvasRoot);

            // Lighting
            GameObject lighting = CreateEmpty(LightingName, root.transform);
            MoveDirectionalLightUnder(lighting);
            SetupDirectionalLight();
            CreateTableKeyLight(lighting);
            CreateTableFillLight(lighting);

            // 최종 선택
            Selection.activeGameObject = root;
        }

        // ── 기존 오브젝트 처리 ────────────────────────────────────────────

        /// <summary>씬에 이미 존재하는 Main Camera, Directional Light, EventSystem을 찾아 TesseraSceneRoot 하위로 이동시킨다.
        /// 없으면 새로 생성한다.</summary>
        private static void MoveExistingCoreObjects(GameObject root)
        {
            // Main Camera: 없으면 새로 생성
            Camera mainCam = Object.FindFirstObjectByType<Camera>();

            if (mainCam == null)
            {
                GameObject camGO = new GameObject("Main Camera");
                camGO.tag = "MainCamera";
                camGO.AddComponent<Camera>();
                camGO.AddComponent<AudioListener>();
                mainCam = camGO.GetComponent<Camera>();
            }

            // Directional Light: 없으면 새로 생성
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);

            bool hasDirectional = false;
            foreach (Light l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    hasDirectional = true;
                    break;
                }
            }
            if (!hasDirectional)
            {
                GameObject dirLightGO = new GameObject("Directional Light");
                dirLightGO.AddComponent<Light>().type = LightType.Directional;
            }

            // EventSystem: 없으면 새로 생성
            EventSystem es = Object.FindFirstObjectByType<EventSystem>();

            if (es == null)
            {
                GameObject esGO = new GameObject("EventSystem");
                esGO.AddComponent<EventSystem>();
                esGO.AddComponent<StandaloneInputModule>();
            }
        }

        /// <summary>Main Camera 오브젝트를 찾아 cameraRig 하위로 이동시킨다.</summary>
        private static void MoveMainCameraUnder(GameObject cameraRig)
        {
            Camera mainCam = Object.FindFirstObjectByType<Camera>();

            if (mainCam != null)
            {
                mainCam.transform.SetParent(cameraRig.transform, worldPositionStays: true);
            }
        }

        /// <summary>EventSystem 오브젝트를 찾아 systems 하위로 이동시킨다.</summary>
        private static void MoveEventSystemUnder(GameObject systems)
        {
            EventSystem es = Object.FindFirstObjectByType<EventSystem>();

            if (es != null)
            {
                es.transform.SetParent(systems.transform, worldPositionStays: true);
            }
        }

        /// <summary>Directional Light 오브젝트를 찾아 lighting 하위로 이동시킨다.</summary>
        private static void MoveDirectionalLightUnder(GameObject lighting)
        {
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);

            foreach (Light l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    l.transform.SetParent(lighting.transform, worldPositionStays: true);
                    break;
                }
            }
        }

        // ── Main Camera 설정 ──────────────────────────────────────────────

        /// <summary>Main Camera의 Transform, FOV, Near/Far, Projection, Tag를 설정한다.
        /// 1인칭 테이블 전투 구도: 사선으로 테이블을 내려다보는 FPS 카메라.</summary>
        private static void SetupMainCamera()
        {
            Camera mainCam = Object.FindFirstObjectByType<Camera>();

            if (mainCam == null) return;

            mainCam.transform.localPosition = new Vector3(0.15f, 1.55f, -2.25f);
            mainCam.transform.localRotation = Quaternion.Euler(56f, -3f, 0f);
            mainCam.fieldOfView = 50f;
            mainCam.nearClipPlane = 0.03f;
            mainCam.farClipPlane = 100f;
            mainCam.orthographic = false;
            mainCam.tag = "MainCamera";
        }

        // ── Directional Light 설정 ─────────────────────────────────────────

        /// <summary>Directional Light의 Rotation과 Intensity를 설정한다.</summary>
        private static void SetupDirectionalLight()
        {
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);

            foreach (Light l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    l.transform.localRotation = Quaternion.Euler(50f, -30f, 0f);
                    l.intensity = 0.8f;
                    break;
                }
            }
        }

        // ── CameraTarget 생성 ──────────────────────────────────────────────

        /// <summary>지정된 위치와 회전값으로 빈 CameraTarget 오브젝트를 생성한다.</summary>
        private static void CreateCameraTarget(GameObject parent, string name, Vector3 position, Vector3 rotation)
        {
            GameObject target = CreateEmpty(name, parent.transform);
            target.transform.localPosition = position;
            target.transform.localRotation = Quaternion.Euler(rotation);
        }

        // ── TableMesh 생성 ─────────────────────────────────────────────────

        /// <summary>TableMesh Cube를 생성하고 어두운 목재/갈색 계열 Material과 BoxCollider를 설정한다.</summary>
        private static void CreateTableMesh(GameObject tableRoot)
        {
            GameObject mesh = CreateCube(TableMeshName, tableRoot.transform);
            mesh.transform.localPosition = new Vector3(0f, 0.72f, 0f);
            mesh.transform.localScale = new Vector3(5.2f, 0.16f, 3.2f);
            SetMeshMaterial(mesh, new Color(0.25f, 0.20f, 0.15f)); // 어두운 갈색/회색
        }

        // ── InfoBoard3D 생성 ──────────────────────────────────────────────

        /// <summary>InfoBoard3D와 하위 InfoBoardBody, InfoBoardScreen을 생성한다.
        /// 화면 좌측에 위치하여 플레이어 정보를 표시한다.</summary>
        private static void CreateInfoBoard3D(GameObject tableRoot)
        {
            GameObject infoBoard = CreateEmpty(InfoBoard3DName, tableRoot.transform);
            infoBoard.transform.localPosition = new Vector3(-1.82f, 0.86f, -0.35f);
            infoBoard.transform.localRotation = Quaternion.Euler(72f, 0f, 0f);
            infoBoard.transform.localScale = Vector3.one;

            // InfoBoardBody
            GameObject body = CreateCube(InfoBoardBodyName, infoBoard.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.82f, 0.035f, 1.35f);
            SetMeshMaterial(body, new Color(0.10f, 0.15f, 0.10f)); // 어두운 녹색/검정

            // InfoBoardScreen
            GameObject screen = CreateCube(InfoBoardScreenName, infoBoard.transform);
            screen.transform.localPosition = new Vector3(0f, 0.025f, 0f);
            screen.transform.localScale = new Vector3(0.72f, 0.01f, 1.15f);
            SetMeshMaterial(screen, new Color(0.05f, 0.05f, 0.08f)); // 어두운 화면
        }

        // ── DiceTray3D 생성 ────────────────────────────────────────────────

        /// <summary>DiceTray3D와 하위 TrayFloor, 벽면(Wall_Back/Front/Left/Right),
        /// DiceSpawnArea, DicePoints를 생성한다. 화면 중앙~우측에 위치한다.</summary>
        private static void CreateDiceTray3D(GameObject tableRoot)
        {
            GameObject tray = CreateEmpty(DiceTray3DName, tableRoot.transform);
            tray.transform.localPosition = new Vector3(0.55f, 0.86f, -0.35f);
            tray.transform.localRotation = Quaternion.identity;

            // TrayFloor
            GameObject floor = CreateCube(TrayFloorName, tray.transform);
            floor.transform.localPosition = Vector3.zero;
            floor.transform.localScale = new Vector3(2.35f, 0.035f, 1.25f);
            SetMeshMaterial(floor, new Color(0.15f, 0.18f, 0.22f)); // 어두운 청회색

            // Wall_Back
            GameObject wallBack = CreateCube(WallBackName, tray.transform);
            wallBack.transform.localPosition = new Vector3(0f, 0.08f, 0.66f);
            wallBack.transform.localScale = new Vector3(2.45f, 0.16f, 0.08f);
            SetMeshMaterial(wallBack, new Color(0.12f, 0.12f, 0.12f));

            // Wall_Front
            GameObject wallFront = CreateCube(WallFrontName, tray.transform);
            wallFront.transform.localPosition = new Vector3(0f, 0.08f, -0.66f);
            wallFront.transform.localScale = new Vector3(2.45f, 0.16f, 0.08f);
            SetMeshMaterial(wallFront, new Color(0.12f, 0.12f, 0.12f));

            // Wall_Left
            GameObject wallLeft = CreateCube(WallLeftName, tray.transform);
            wallLeft.transform.localPosition = new Vector3(-1.23f, 0.08f, 0f);
            wallLeft.transform.localScale = new Vector3(0.08f, 0.16f, 1.25f);
            SetMeshMaterial(wallLeft, new Color(0.12f, 0.12f, 0.12f));

            // Wall_Right
            GameObject wallRight = CreateCube(WallRightName, tray.transform);
            wallRight.transform.localPosition = new Vector3(1.23f, 0.08f, 0f);
            wallRight.transform.localScale = new Vector3(0.08f, 0.16f, 1.25f);
            SetMeshMaterial(wallRight, new Color(0.12f, 0.12f, 0.12f));

            // DiceSpawnArea
            GameObject spawnArea = CreateEmpty(DiceSpawnAreaName, tray.transform);
            spawnArea.transform.localPosition = new Vector3(0f, 0.14f, 0f);

            // DicePoints
            GameObject dicePoints = CreateEmpty(DicePointsName, tray.transform);
            Vector3[] dicePointPositions = new Vector3[]
            {
                new Vector3(-0.70f, 0.16f, -0.20f),
                new Vector3(-0.35f, 0.16f,  0.15f),
                new Vector3( 0.00f, 0.16f, -0.05f),
                new Vector3( 0.35f, 0.16f,  0.22f),
                new Vector3( 0.70f, 0.16f, -0.18f),
            };
            for (int i = 0; i < 5; i++)
            {
                GameObject point = CreateEmpty($"DicePoint_{i}", dicePoints.transform);
                point.transform.localPosition = dicePointPositions[i];
            }
        }

        // ── LockSlotRack3D 생성 ────────────────────────────────────────────

        /// <summary>LockSlotRack3D와 5개의 LockSlot Cube를 생성한다.
        /// DiceTray 위쪽(z 방향)에 위치한다.</summary>
        private static void CreateLockSlotRack3D(GameObject tableRoot)
        {
            GameObject rack = CreateEmpty(LockSlotRack3DName, tableRoot.transform);
            rack.transform.localPosition = new Vector3(0.55f, 0.88f, 0.38f);
            rack.transform.localRotation = Quaternion.identity;

            Vector3[] lockSlotPositions = new Vector3[]
            {
                new Vector3(-0.88f, 0f, 0f),
                new Vector3(-0.44f, 0f, 0f),
                new Vector3( 0.00f, 0f, 0f),
                new Vector3( 0.44f, 0f, 0f),
                new Vector3( 0.88f, 0f, 0f),
            };
            for (int i = 0; i < 5; i++)
            {
                GameObject slot = CreateCube($"LockSlot_{i}", rack.transform);
                slot.transform.localPosition = lockSlotPositions[i];
                slot.transform.localScale = new Vector3(0.34f, 0.035f, 0.34f);
                SetMeshMaterial(slot, new Color(0.25f, 0.25f, 0.25f)); // 회색
            }
        }

        // ── DeviceRack3D 생성 ──────────────────────────────────────────────

        /// <summary>DeviceRack3D와 5개의 DeviceSlot Cube를 생성한다.
        /// LockSlot보다 위쪽(z 방향)에 위치하며, 약간 기울어져 있다.</summary>
        private static void CreateDeviceRack3D(GameObject tableRoot)
        {
            GameObject rack = CreateEmpty(DeviceRack3DName, tableRoot.transform);
            rack.transform.localPosition = new Vector3(0.55f, 0.92f, 0.86f);
            rack.transform.localRotation = Quaternion.Euler(70f, 0f, 0f);

            Vector3[] deviceSlotPositions = new Vector3[]
            {
                new Vector3(-0.96f, 0f, 0f),
                new Vector3(-0.48f, 0f, 0f),
                new Vector3( 0.00f, 0f, 0f),
                new Vector3( 0.48f, 0f, 0f),
                new Vector3( 0.96f, 0f, 0f),
            };
            for (int i = 0; i < 5; i++)
            {
                GameObject slot = CreateCube($"DeviceSlot_{i}", rack.transform);
                slot.transform.localPosition = deviceSlotPositions[i];
                slot.transform.localScale = new Vector3(0.38f, 0.025f, 0.58f);
                SetMeshMaterial(slot, new Color(0.12f, 0.12f, 0.28f)); // 짙은 남색
            }
        }

        // ── ConsumableRack3D 생성 ──────────────────────────────────────────

        /// <summary>ConsumableRack3D와 2개의 ConsumableSlot Cube를 생성한다.
        /// DeviceRack 오른쪽(x 방향)에 위치한다.</summary>
        private static void CreateConsumableRack3D(GameObject tableRoot)
        {
            GameObject rack = CreateEmpty(ConsumableRack3DName, tableRoot.transform);
            rack.transform.localPosition = new Vector3(1.95f, 0.92f, 0.86f);
            rack.transform.localRotation = Quaternion.Euler(70f, 0f, 0f);

            // ConsumableSlot_0
            GameObject slot0 = CreateCube("ConsumableSlot_0", rack.transform);
            slot0.transform.localPosition = new Vector3(0f, 0f, 0.18f);
            slot0.transform.localScale = new Vector3(0.34f, 0.025f, 0.50f);
            SetMeshMaterial(slot0, new Color(0.25f, 0.10f, 0.25f)); // 보라색/어두운

            // ConsumableSlot_1
            GameObject slot1 = CreateCube("ConsumableSlot_1", rack.transform);
            slot1.transform.localPosition = new Vector3(0f, 0f, -0.34f);
            slot1.transform.localScale = new Vector3(0.34f, 0.025f, 0.50f);
            SetMeshMaterial(slot1, new Color(0.25f, 0.10f, 0.25f)); // 보라색/어두운
        }

        // ── ActionButtons3D 생성 ───────────────────────────────────────────

        /// <summary>ActionButtons3D와 5개의 액션 버튼 Cube를 생성한다.
        /// 테이블 앞쪽(z 방향)에 위치한다.</summary>
        private static void CreateActionButtons3D(GameObject tableRoot)
        {
            GameObject actionButtons = CreateEmpty(ActionButtons3DName, tableRoot.transform);
            actionButtons.transform.localPosition = new Vector3(0.55f, 0.90f, -1.12f);
            actionButtons.transform.localRotation = Quaternion.identity;

            string[] buttonNames = new string[] { "RollButton3D", "CastButton3D", "BreakButton3D", "ResetButton3D", "NextButton3D" };
            Vector3[] buttonPositions = new Vector3[]
            {
                new Vector3(-0.72f, 0f, 0f),
                new Vector3(-0.36f, 0f, 0f),
                new Vector3( 0.00f, 0f, 0f),
                new Vector3( 0.36f, 0f, 0f),
                new Vector3( 0.72f, 0f, 0f),
            };
            Color[] buttonColors = new Color[]
            {
                new Color(0.35f, 0.25f, 0.20f), // Roll: 갈색 계열
                new Color(0.20f, 0.35f, 0.25f), // Cast: 녹색 계열
                new Color(0.35f, 0.20f, 0.20f), // Break: 붉은 계열
                new Color(0.25f, 0.25f, 0.35f), // Reset: 청색 계열
                new Color(0.30f, 0.30f, 0.20f), // Next: 황색 계열
            };

            for (int i = 0; i < 5; i++)
            {
                GameObject btn = CreateCube(buttonNames[i], actionButtons.transform);
                btn.transform.localPosition = buttonPositions[i];
                btn.transform.localScale = new Vector3(0.28f, 0.055f, 0.18f);
                SetMeshMaterial(btn, buttonColors[i]);
            }
        }

        // ── OpponentRoot 생성 ──────────────────────────────────────────────

        /// <summary>OpponentRoot와 하위 OpponentSilhouette, HammerAnchor, IntentAnchor를 생성한다.
        /// 먼 상단의 placeholder anchor 수준으로만 구성한다.</summary>
        private static void CreateOpponentRoot(GameObject battleTableRoot)
        {
            GameObject opponentRoot = CreateEmpty(OpponentRootName, battleTableRoot.transform);
            opponentRoot.transform.localPosition = new Vector3(0f, 0.85f, 1.55f);

            // OpponentSilhouette
            GameObject silhouette = CreateCube(OpponentSilhouetteName, opponentRoot.transform);
            silhouette.transform.localPosition = new Vector3(0f, 0.55f, 0.25f);
            silhouette.transform.localScale = new Vector3(0.45f, 0.75f, 0.30f);
            SetMeshMaterial(silhouette, new Color(0.20f, 0.20f, 0.20f)); // 어두운 회색

            // HammerAnchor
            GameObject hammerAnchor = CreateEmpty(HammerAnchorName, opponentRoot.transform);
            hammerAnchor.transform.localPosition = new Vector3(0f, 1.25f, 0f);

            // IntentAnchor
            GameObject intentAnchor = CreateEmpty(IntentAnchorName, opponentRoot.transform);
            intentAnchor.transform.localPosition = new Vector3(0.75f, 0.75f, 0.1f);
        }

        // ── BattleWorldAnchors 생성 ────────────────────────────────────────

        /// <summary>BattleWorldAnchors 하위에 CastCandidateAnchor, DeviceTooltipAnchor, MessageAnchor를
        /// 빈 GameObject로만 생성한다. 거대한 TMP Canvas는 만들지 않는다.</summary>
        private static void CreateBattleWorldAnchors(GameObject battleTableRoot)
        {
            GameObject anchors = CreateEmpty(BattleWorldAnchorsName, battleTableRoot.transform);

            // CastCandidateAnchor
            GameObject castAnchor = CreateEmpty(CastCandidateAnchorName, anchors.transform);
            castAnchor.transform.localPosition = new Vector3(1.85f, 1.05f, -0.35f);
            castAnchor.transform.localRotation = Quaternion.Euler(65f, -20f, 0f);

            // DeviceTooltipAnchor
            GameObject tooltipAnchor = CreateEmpty(DeviceTooltipAnchorName, anchors.transform);
            tooltipAnchor.transform.localPosition = new Vector3(0.55f, 1.20f, 0.92f);
            tooltipAnchor.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);

            // MessageAnchor
            GameObject messageAnchor = CreateEmpty(MessageAnchorName, anchors.transform);
            messageAnchor.transform.localPosition = new Vector3(0.55f, 1.02f, -1.28f);
            messageAnchor.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);
        }

        // ── Screen Space Canvas 생성 ───────────────────────────────────────

        /// <summary>ProductionCanvasRoot 하위에 Screen Space Overlay Canvas 5개를 생성한다.
        /// RoundSelect, Shop, Result는 비활성화, Overlay는 활성화, Pause는 비활성화.</summary>
        private static void CreateScreenSpaceCanvases(GameObject productionCanvasRoot)
        {
            string[] canvasNames = new string[] { "RoundSelectCanvas", "ShopCanvas", "ResultCanvas", "OverlayCanvas", "PauseCanvas" };
            int[] sortingOrders = new int[] { 100, 110, 120, 500, 600 };
            bool[] activeStates = new bool[] { false, false, false, true, false };

            for (int i = 0; i < 5; i++)
            {
                GameObject canvasGO = CreateScreenSpaceCanvas(
                    productionCanvasRoot.transform,
                    canvasNames[i],
                    sortingOrders[i]
                );
                canvasGO.SetActive(activeStates[i]);
            }
        }

        /// <summary>Screen Space Overlay Canvas를 생성하고 CanvasScaler를 설정한다.
        /// Reference Resolution 1920x1080, Match 0.5.</summary>
        private static GameObject CreateScreenSpaceCanvas(Transform parent, string canvasName, int sortingOrder)
        {
            GameObject canvasGO = new GameObject(canvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(parent, worldPositionStays: false);

            Canvas canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            return canvasGO;
        }

        // ── Debug Canvas 생성 ──────────────────────────────────────────────

        /// <summary>DebugCanvasRoot 하위에 DebugPrototypeCanvas를 Screen Space Overlay로 생성한다.
        /// 기본적으로 비활성화 상태이다.</summary>
        private static void CreateDebugPrototypeCanvas(GameObject debugCanvasRoot)
        {
            GameObject canvasGO = CreateScreenSpaceCanvas(debugCanvasRoot.transform, "DebugPrototypeCanvas", 0);
            canvasGO.SetActive(false);
        }

        // ── Lighting 생성 ──────────────────────────────────────────────────

        /// <summary>TableKeyLight Point Light를 생성한다. 테이블 중앙 상단에서 비춘다.</summary>
        private static void CreateTableKeyLight(GameObject lighting)
        {
            GameObject lightGO = new GameObject("TableKeyLight", typeof(Light));
            lightGO.transform.SetParent(lighting.transform, worldPositionStays: false);
            lightGO.transform.localPosition = new Vector3(0.4f, 2.1f, -0.6f);

            Light l = lightGO.GetComponent<Light>();
            l.type = LightType.Point;
            l.range = 5f;
            l.intensity = 2.5f;
            l.color = new Color(1f, 0.95f, 0.85f);
        }

        /// <summary>TableFillLight Point Light를 생성한다. 좌측에서 보조 조명을 비춘다.</summary>
        private static void CreateTableFillLight(GameObject lighting)
        {
            GameObject lightGO = new GameObject("TableFillLight", typeof(Light));
            lightGO.transform.SetParent(lighting.transform, worldPositionStays: false);
            lightGO.transform.localPosition = new Vector3(-1.6f, 1.5f, -0.2f);

            Light l = lightGO.GetComponent<Light>();
            l.type = LightType.Point;
            l.range = 4f;
            l.intensity = 0.8f;
            l.color = new Color(0.85f, 0.90f, 1f);
        }

        // ── 유틸리티 함수 ────────────────────────────────────────────────────

        /// <summary>지정된 이름과 부모 Transform으로 빈 GameObject를 생성한다.</summary>
        private static GameObject CreateEmpty(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            if (parent != null)
            {
                go.transform.SetParent(parent, worldPositionStays: false);
            }
            return go;
        }

        /// <summary>지정된 이름과 부모 Transform으로 Cube GameObject를 생성한다.</summary>
        private static GameObject CreateCube(string name, Transform parent)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            if (parent != null)
            {
                go.transform.SetParent(parent, worldPositionStays: false);
            }
            return go;
        }

        /// <summary>Renderer에 임시 Material을 생성하여 할당한다. 색상만 지정한다.</summary>
        private static void SetMeshMaterial(GameObject go, Color color)
        {
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;

            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = color;
            renderer.sharedMaterial = mat;
        }
    }
}
