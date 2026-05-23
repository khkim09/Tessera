using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Tessera.UI
{
    /// <summary>SlotPair 계산 중 슬롯 위에 뜨는 짧은 Floating Text 연출을 관리한다.</summary>
    public class SlotPairStepFloatingTextView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text messageText;

        [Header("Animation")]
        [SerializeField] private float riseDistance = 55f;
        [SerializeField] private float duration = 0.65f;
        [SerializeField] private float startScale = 0.85f;
        [SerializeField] private float peakScale = 1.25f;

        private CancellationTokenSource playCts;

        /// <summary>컴포넌트가 추가될 때 기본 참조를 자동 보정한다.</summary>
        private void Reset()
        {
            // 같은 오브젝트에서 필요한 UI 컴포넌트를 자동 수집한다.
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            messageText = GetComponentInChildren<TMP_Text>(true);
        }

        /// <summary>런타임 시작 시 누락된 참조를 보정한다.</summary>
        private void Awake()
        {
            // Inspector 누락을 방지하기 위해 런타임에서도 한 번 더 보정한다.
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (messageText == null)
                messageText = GetComponentInChildren<TMP_Text>(true);

            HideImmediate();
        }

        /// <summary>오브젝트 제거 시 진행 중인 Floating Text 연출을 정리한다.</summary>
        private void OnDestroy()
        {
            // 남아 있는 UniTask 연출을 안전하게 취소한다.
            CancelPlayTask();
        }

        /// <summary>Floating Text를 즉시 숨긴다.</summary>
        public void HideImmediate()
        {
            // 초기 상태에서는 화면에 보이지 않게 한다.
            CancelPlayTask();

            if (canvasGroup != null)
                canvasGroup.alpha = 0f;

            gameObject.SetActive(false);
        }

        /// <summary>지정 위치에서 Floating Text 연출을 재생한다.</summary>
        public async UniTask PlayAsync(string message, Vector2 anchoredPosition, CancellationToken externalToken)
        {
            CancelPlayTask();

            CancellationTokenSource currentCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            playCts = currentCts;

            try
            {
                await PlayInternalAsync(message, anchoredPosition, currentCts.Token);
            }
            catch (OperationCanceledException)
            {
                // 새 연출 또는 전투 상태 전환으로 정상 취소될 수 있다.
            }
            finally
            {
                if (playCts == currentCts)
                    playCts = null;

                currentCts.Dispose();
            }
        }

        /// <summary>Floating Text 위치, 크기, 알파 변화 연출을 처리한다.</summary>
        private async UniTask PlayInternalAsync(string message, Vector2 anchoredPosition, CancellationToken cancellationToken)
        {
            if (rectTransform == null || canvasGroup == null || messageText == null)
                return;

            gameObject.SetActive(true);

            messageText.text = message;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.localScale = Vector3.one * startScale;
            canvasGroup.alpha = 1f;

            Vector2 startPosition = anchoredPosition;
            Vector2 endPosition = anchoredPosition + new Vector2(0f, riseDistance);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                cancellationToken.ThrowIfCancellationRequested();

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = 1f - Mathf.Pow(1f - t, 3f);

                // 위로 떠오르면서 후반부에 자연스럽게 사라지게 한다.
                rectTransform.anchoredPosition = Vector2.Lerp(startPosition, endPosition, easedT);

                float scaleT = Mathf.Sin(t * Mathf.PI);
                rectTransform.localScale = Vector3.one * Mathf.Lerp(startScale, peakScale, scaleT);

                if (t > 0.55f)
                    canvasGroup.alpha = Mathf.Lerp(1f, 0f, (t - 0.55f) / 0.45f);

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        /// <summary>현재 진행 중인 Floating Text 연출을 취소한다.</summary>
        private void CancelPlayTask()
        {
            if (playCts == null)
                return;

            // 이전 Floating Text UniTask를 중단한다.
            playCts.Cancel();
            playCts.Dispose();
            playCts = null;
        }
    }
}
