using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tessera.UI
{
    /// <summary>테이블 위 DiceCup 3D 오브젝트의 클릭 입력과 흔들림 연출을 관리한다.</summary>
    public class DiceCup3DView : MonoBehaviour, IPointerClickHandler
    {
        [Header("Click")]
        [SerializeField] private Collider clickCollider;
        [SerializeField] private bool autoEnableCollider = true;

        private CancellationTokenSource shakeCts;

        /// <summary>DiceCup 클릭 이벤트</summary>
        public event Action Clicked;

        /// <summary>컴포넌트 추가 시 기본 Collider 참조를 자동 수집한다.</summary>
        private void Reset()
        {
            clickCollider = GetComponent<Collider>();
        }

        /// <summary>런타임 시작 시 클릭 Collider 상태를 보정한다.</summary>
        private void Awake()
        {
            if (clickCollider == null)
                clickCollider = GetComponent<Collider>();

            if (autoEnableCollider && clickCollider != null)
                clickCollider.enabled = true;
        }

        /// <summary>오브젝트 제거 시 진행 중인 흔들림 연출을 정리한다.</summary>
        private void OnDestroy()
        {
            CancelShake();
        }

        /// <summary>마우스 클릭 시 Presenter에 Roll 요청을 전달한다.</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData == null)
                return;

            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            Clicked?.Invoke();
        }

        /// <summary>DiceCup을 짧게 흔드는 연출을 재생한다.</summary>
        public async UniTask PlayShakeAsync(
            float duration,
            float angle,
            float frequency,
            CancellationToken cancellationToken)
        {
            if (duration <= 0f)
                return;

            CancelShake();

            CancellationTokenSource currentCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            shakeCts = currentCts;

            Quaternion baseLocalRotation = transform.localRotation;
            float elapsed = 0f;

            try
            {
                while (elapsed < duration)
                {
                    currentCts.Token.ThrowIfCancellationRequested();

                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    float fade = 1f - t;
                    float wave = Mathf.Sin(elapsed * frequency * Mathf.PI * 2f);

                    transform.localRotation =
                        baseLocalRotation *
                        Quaternion.Euler(angle * wave * fade, 0f, angle * 0.45f * wave * fade);

                    await UniTask.Yield(PlayerLoopTiming.Update, currentCts.Token);
                }

                transform.localRotation = baseLocalRotation;
            }
            catch (OperationCanceledException)
            {
                transform.localRotation = baseLocalRotation;
            }
            finally
            {
                if (shakeCts == currentCts)
                    shakeCts = null;

                currentCts.Dispose();
            }
        }

        /// <summary>진행 중인 흔들림 연출을 취소한다.</summary>
        private void CancelShake()
        {
            if (shakeCts == null)
                return;

            shakeCts.Cancel();
            shakeCts.Dispose();
            shakeCts = null;
        }
    }
}
