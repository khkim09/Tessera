using Tessera.Runtime;
using UnityEngine;

namespace Tessera.UI
{
    /// <summary>GameMode Root 전환과 Stage Flow 시작을 관리한다.</summary>
    public class TesseraGameModeRoot : MonoBehaviour
    {
        [Header("Mode Roots")]
        [SerializeField] private GameObject gameplayRoot;
        [SerializeField] private GameObject shopRoot;
        [SerializeField] private GameObject bountyBoardRoot;
        [SerializeField] private GameObject rewardDecisionRoot;
        [SerializeField] private GameObject roundFailureDecisionRoot;
        [SerializeField] private GameObject resultRoot;

        [Header("Flow")]
        [SerializeField] private StageBountyFlowController stageBountyFlowController;

        [Header("Debug Start")]
        [SerializeField] private int startMoney = 30;
        [SerializeField] private int playerMaxHP = 100;

        private TesseraRunSession runSession;
        private GameModeType currentMode = GameModeType.None;
        private System.IDisposable gameModeRequestSubscription;

        /// <summary>현재 RunSession을 반환한다.</summary>
        public TesseraRunSession RunSession => runSession;

        /// <summary>RunSession 생성, StageFlow 초기화, GameMode 이벤트 구독을 수행한다.</summary>
        private void Awake()
        {
            // TesseraEventBus.ClearAll();

            runSession = new TesseraRunSession(startMoney, playerMaxHP);

            if (stageBountyFlowController != null)
                stageBountyFlowController.Initialize(runSession);

            gameModeRequestSubscription = TesseraEventBus.Subscribe<GameModeChangeRequestedEvent>(HandleGameModeChangeRequested);
        }

        /// <summary>Stage Flow를 시작한다.</summary>
        private void Start()
        {
            SwitchMode(GameModeType.None);

            if (stageBountyFlowController != null)
                stageBountyFlowController.StartFlow();
        }

        /// <summary>이벤트 구독을 해제한다.</summary>
        private void OnDestroy()
        {
            gameModeRequestSubscription?.Dispose();
            gameModeRequestSubscription = null;
        }

        /// <summary>지정 GameMode로 Root 활성 상태를 전환한다.</summary>
        public void SwitchMode(GameModeType mode)
        {
            currentMode = mode;

            SetRootActive(gameplayRoot, mode == GameModeType.Gameplay);
            SetRootActive(shopRoot, mode == GameModeType.Shop);
            SetRootActive(bountyBoardRoot, mode == GameModeType.BountyBoard);
            SetRootActive(rewardDecisionRoot, mode == GameModeType.RewardDecision);
            SetRootActive(roundFailureDecisionRoot, mode == GameModeType.RoundFailureDecision);
            SetRootActive(resultRoot, mode == GameModeType.Result);

            TesseraEventBus.Publish(new GameModeChangedEvent(currentMode));
        }

        /// <summary>GameMode 변경 요청 이벤트를 처리한다.</summary>
        private void HandleGameModeChangeRequested(GameModeChangeRequestedEvent gameEvent)
        {
            SwitchMode(gameEvent.RequestedMode);
        }

        /// <summary>대상 Root 활성 상태를 변경한다.</summary>
        private static void SetRootActive(GameObject targetRoot, bool isActive)
        {
            if (targetRoot == null)
                return;

            targetRoot.SetActive(isActive);
        }
    }
}
