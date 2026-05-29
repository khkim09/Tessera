using Tessera.Runtime;
using UnityEngine;

namespace Tessera.UI
{
    /// <summary>GameMode Root 전환과 Stage Flow 시작을 관리한다.</summary>
    public class TesseraGameModeRoot : MonoBehaviour
    {
        [Header("Mode Roots")]
        [SerializeField] private GameObject roundSelectRoot;
        [SerializeField] private GameObject gameplayRoot;
        [SerializeField] private GameObject shopRoot;
        [SerializeField] private GameObject bountyBoardRoot;
        [SerializeField] private GameObject rewardDecisionRoot;
        [SerializeField] private GameObject roundFailureDecisionRoot;
        [SerializeField] private GameObject resultRoot;

        [Header("Flow")]
        [SerializeField] private StageBountyFlowController stageBountyFlowController;

        [Header("Debug Start")]
        [SerializeField] private int startParts = 30;
        [SerializeField] private int playerMaxHp = 100;

        private TesseraRunSession runSession;
        private GameModeType currentMode = GameModeType.None;
        private System.IDisposable gameModeRequestSubscription;

        /// <summary>현재 RunSession을 반환한다.</summary>
        public TesseraRunSession RunSession => runSession;

        private void Awake()
        {
            TesseraEventBus.ClearAll();

            runSession = new TesseraRunSession(startParts, playerMaxHp);

            if (stageBountyFlowController != null)
                stageBountyFlowController.Initialize(runSession);

            gameModeRequestSubscription = TesseraEventBus.Subscribe<GameModeChangeRequestedEvent>(HandleGameModeChangeRequested);
        }

        private void Start()
        {
            if (stageBountyFlowController != null)
                stageBountyFlowController.StartFlow();
        }

        private void OnDestroy()
        {
            gameModeRequestSubscription?.Dispose();
            gameModeRequestSubscription = null;
        }

        /// <summary>지정 GameMode로 화면을 전환한다.</summary>
        public void SwitchMode(GameModeType mode)
        {
            currentMode = mode;

            SetRootActive(roundSelectRoot, mode == GameModeType.RoundSelect);
            SetRootActive(gameplayRoot, mode == GameModeType.Gameplay);
            SetRootActive(shopRoot, mode == GameModeType.Shop);
            SetRootActive(bountyBoardRoot, mode == GameModeType.BountyBoard);
            SetRootActive(rewardDecisionRoot, mode == GameModeType.RewardDecision);
            SetRootActive(roundFailureDecisionRoot, mode == GameModeType.RoundFailureDecision);
            SetRootActive(resultRoot, mode == GameModeType.Result);

            TesseraEventBus.Publish(new GameModeChangedEvent(currentMode));
        }

        private void HandleGameModeChangeRequested(GameModeChangeRequestedEvent gameEvent)
        {
            SwitchMode(gameEvent.RequestedMode);
        }

        private static void SetRootActive(GameObject targetRoot, bool isActive)
        {
            if (targetRoot == null)
                return;

            targetRoot.SetActive(isActive);
        }
    }
}
