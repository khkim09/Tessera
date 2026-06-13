using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Tessera.Runtime;
using TMPro;
using UnityEngine;

namespace Tessera.UI
{
    /// <summary>항상 표시되는 Money / Chain Overlay View다.</summary>
    public class MoneyOverlayView : MonoBehaviour
    {
        [SerializeField] private TMP_Text chainText;
        [SerializeField] private TMP_Text moneyText;
        [SerializeField] private TMP_Text moneyDeltaText;

        [Header("Display")]
        [SerializeField] private string chainPrefix = "x";
        [SerializeField] private string moneyPrefix = "$";
        [SerializeField] private float moneyDeltaDisplaySeconds = 0.75f;

        /// <summary>현재 Overlay에 실제 표시 중인 Money 값이다.</summary>
        private int displayedMoney;

        /// <summary>Money 표시값이 한 번 이상 초기화되었는지 여부다.</summary>
        private bool hasDisplayedMoney;

        /// <summary>Money Delta 표시 중복 제어용 버전 값이다.</summary>
        private int moneyDeltaDisplayVersion;

        private IDisposable economyChangedSubscription;

        /// <summary>Stage Economy 갱신 이벤트를 구독한다.</summary>
        private void OnEnable()
        {
            economyChangedSubscription = TesseraEventBus.Subscribe<StageEconomyChangedEvent>(HandleStageEconomyChanged);
            TesseraEventBus.Publish(new StageEconomyRefreshRequestedEvent());
        }

        /// <summary>Stage Economy 갱신 이벤트 구독을 해제한다.</summary>
        private void OnDisable()
        {
            economyChangedSubscription?.Dispose();
            economyChangedSubscription = null;

            // 비활성화 이후 지연 갱신이 UI를 건드리지 않도록 버전을 증가시킨다.
            moneyDeltaDisplayVersion++;

            if (moneyDeltaText != null)
                moneyDeltaText.gameObject.SetActive(false);
        }

        /// <summary>RunSession과 BoardState 기준으로 Overlay를 즉시 갱신한다.</summary>
        public void Refresh(TesseraRunSession runSession, StageBountyBoardState boardState)
        {
            int money = runSession != null ? runSession.Money : 0;
            int chain = boardState != null ? boardState.ChainCount : 0;

            RefreshChainText(chain);
            SetMoneyText(money);

            displayedMoney = money;
            hasDisplayedMoney = true;

            if (moneyDeltaText != null)
                moneyDeltaText.gameObject.SetActive(false);
        }

        /// <summary>Stage Economy 갱신 이벤트를 처리한다.</summary>
        private void HandleStageEconomyChanged(StageEconomyChangedEvent gameEvent)
        {
            int nextMoney = gameEvent.RunSession != null ? gameEvent.RunSession.Money : 0;
            int chain = gameEvent.BoardState != null ? gameEvent.BoardState.ChainCount : 0;

            RefreshChainText(chain);

            if (!hasDisplayedMoney)
            {
                SetMoneyText(nextMoney);
                displayedMoney = nextMoney;
                hasDisplayedMoney = true;
                return;
            }

            int deltaMoney = nextMoney - displayedMoney;

            if (deltaMoney > 0)
            {
                // 수익은 +$N을 잠시 보여준 뒤 보유 Money에 합산 표시한다.
                ShowMoneyGainThenRefreshAsync(deltaMoney, nextMoney, this.GetCancellationTokenOnDestroy()).Forget();
                return;
            }

            // 지출/동일 값은 즉시 반영한다.
            moneyDeltaDisplayVersion++;
            SetMoneyText(nextMoney);
            displayedMoney = nextMoney;

            if (moneyDeltaText != null)
                moneyDeltaText.gameObject.SetActive(false);
        }

        #region Helper

        /// <summary>Chain 텍스트를 갱신한다.</summary>
        private void RefreshChainText(int chain)
        {
            if (chainText != null)
                chainText.text = $"{chainPrefix} {chain}";
        }

        /// <summary>Money 텍스트를 지정 값으로 갱신한다.</summary>
        private void SetMoneyText(int money)
        {
            if (moneyText != null)
                moneyText.text = $"{moneyPrefix}{money}";
        }

        /// <summary>Money 증가량을 잠시 표시한 뒤 최종 Money를 반영한다.</summary>
        private async UniTaskVoid ShowMoneyGainThenRefreshAsync(
            int deltaMoney,
            int nextMoney,
            CancellationToken cancellationToken)
        {
            int version = ++moneyDeltaDisplayVersion;

            if (moneyDeltaText != null)
            {
                moneyDeltaText.gameObject.SetActive(true);
                moneyDeltaText.text = $"+{moneyPrefix}{deltaMoney}";
            }

            await UniTask.Delay(
                TimeSpan.FromSeconds(Mathf.Max(0f, moneyDeltaDisplaySeconds)),
                cancellationToken: cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                return;

            if (version != moneyDeltaDisplayVersion)
                return;

            SetMoneyText(nextMoney);
            displayedMoney = nextMoney;

            if (moneyDeltaText != null)
                moneyDeltaText.gameObject.SetActive(false);
        }

        #endregion
    }
}
