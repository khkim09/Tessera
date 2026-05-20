using Tessera.Runtime;
using UnityEngine;

namespace Tessera.UI
{
    /// <summary>RoundSelect, Gameplay, Shop 화면 전환과 RunSession 공유를 관리한다.</summary>
    public class TesseraGameModeRoot : MonoBehaviour
    {
        [Header("Mode Roots")]
        [SerializeField] private GameObject roundSelectRoot;
        [SerializeField] private GameObject gameplayRoot;
        [SerializeField] private GameObject shopRoot;

        [Header("Presenters")]
        [SerializeField] private TesseraGameplayBattlePresenter gameplayPresenter;
        [SerializeField] private TesseraShopPresenter shopPresenter;

        [Header("Debug Start")]
        [SerializeField] private GameModeType startMode = GameModeType.Gameplay;
        [SerializeField] private int startParts = 30;
        [SerializeField] private int roundWinParts = 20;

        private TesseraRunSession runSession;
        private GameModeType currentMode = GameModeType.None;

        /// <summary>현재 RunSession을 반환한다.</summary>
        public TesseraRunSession RunSession => runSession;

        /// <summary>RunSession을 생성하고 Presenter에 연결한다.</summary>
        private void Awake()
        {
            runSession = new TesseraRunSession(startParts);

            if (gameplayPresenter != null)
                gameplayPresenter.BindRunSession(runSession);

            if (shopPresenter != null)
                shopPresenter.Initialize(runSession);
        }

        /// <summary>Presenter 이벤트를 연결하고 시작 화면으로 전환한다.</summary>
        private void OnEnable()
        {
            if (gameplayPresenter != null)
            {
                gameplayPresenter.RoundWon += HandleRoundWon;
                gameplayPresenter.RoundLost += HandleRoundLost;
            }

            if (shopPresenter != null)
                shopPresenter.NextRequested += HandleShopNextRequested;

            SwitchMode(startMode);
        }

        /// <summary>Presenter 이벤트를 해제한다.</summary>
        private void OnDisable()
        {
            if (gameplayPresenter != null)
            {
                gameplayPresenter.RoundWon -= HandleRoundWon;
                gameplayPresenter.RoundLost -= HandleRoundLost;
            }

            if (shopPresenter != null)
                shopPresenter.NextRequested -= HandleShopNextRequested;
        }

        /// <summary>지정 GameMode로 화면을 전환한다.</summary>
        public void SwitchMode(GameModeType mode)
        {
            currentMode = mode;

            SetRootActive(roundSelectRoot, mode == GameModeType.RoundSelect);
            SetRootActive(gameplayRoot, mode == GameModeType.Gameplay);
            SetRootActive(shopRoot, mode == GameModeType.Shop);

            if (mode == GameModeType.Shop && shopPresenter != null)
                shopPresenter.RefreshAll("Shop opened.");
        }

        /// <summary>Round 승리 시 Parts를 지급하고 Shop으로 이동한다.</summary>
        private void HandleRoundWon(Core.CastSubmitResult result)
        {
            if (runSession != null)
                runSession.AddParts(roundWinParts);

            SwitchMode(GameModeType.Shop);
        }

        /// <summary>Round 패배 시 현재는 Gameplay 화면에 머문다. 추후 Result 화면으로 연결한다.</summary>
        private void HandleRoundLost(Core.CastSubmitResult result)
        {
            // 패배 화면은 다음 단계에서 별도 Result/Retry 화면으로 분리한다.
        }

        /// <summary>Shop 다음 버튼 클릭 시 다음 Gameplay로 이동한다.</summary>
        private void HandleShopNextRequested()
        {
            if (gameplayPresenter != null)
                gameplayPresenter.StartDebugRound();

            SwitchMode(GameModeType.Gameplay);
        }

        /// <summary>Root GameObject 활성 상태를 안전하게 변경한다.</summary>
        private static void SetRootActive(GameObject targetRoot, bool isActive)
        {
            if (targetRoot == null)
                return;

            targetRoot.SetActive(isActive);
        }
    }
}
