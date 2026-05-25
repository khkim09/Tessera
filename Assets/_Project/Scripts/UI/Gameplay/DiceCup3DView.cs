using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tessera.UI
{
    /// <summary>테이블 위 DiceCup 3D 오브젝트의 클릭 입력과 공중 흔들림 연출을 관리한다.</summary>
    public class DiceCup3DView : MonoBehaviour, IPointerClickHandler
    {
        [Header("Click")]
        [SerializeField] private Collider clickCollider;
        [SerializeField] private bool autoEnableCollider = true;

        [Header("Dice Entry")]
        [SerializeField] private Transform diceEntryPoint;

        private CancellationTokenSource motionCts;

        /// <summary>DiceCup 클릭 이벤트</summary>
        public event Action Clicked;

        /// <summary>Dice가 컵으로 들어가거나 컵에서 나오는 기준 위치를 반환한다.</summary>
        public Vector3 DiceEntryPosition => diceEntryPoint != null ? diceEntryPoint.position : transform.position + transform.up * 0.2f;

        /// <summary>Dice가 컵 입구에서 사용할 기준 회전을 반환한다.</summary>
        public Quaternion DiceEntryRotation => diceEntryPoint != null ? diceEntryPoint.rotation : transform.rotation;

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

        /// <summary>오브젝트 제거 시 진행 중인 컵 연출을 정리한다.</summary>
        private void OnDestroy()
        {
            CancelMotion();
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

        /// <summary>DiceCup을 제자리에서 짧게 흔드는 연출을 재생한다.</summary>
        public async UniTask PlayShakeAsync(
            float duration,
            float angle,
            float frequency,
            CancellationToken cancellationToken)
        {
            await PlayLiftShakeAsync(
                0f,
                0f,
                duration,
                0f,
                angle,
                frequency,
                cancellationToken);
        }

        /// <summary>DiceCup이 살짝 떠오른 뒤 공중에서 흔들리고 원위치로 내려오는 연출을 재생한다.</summary>
        public async UniTask PlayLiftShakeAsync(
            float liftHeight,
            float liftDuration,
            float shakeDuration,
            float dropDuration,
            float angle,
            float frequency,
            CancellationToken cancellationToken)
        {
            CancelMotion();

            CancellationTokenSource currentCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            motionCts = currentCts;

            Vector3 baseLocalPosition = transform.localPosition;
            Quaternion baseLocalRotation = transform.localRotation;

            try
            {
                if (liftDuration > 0f && liftHeight > 0f)
                {
                    await PlayLiftSegmentAsync(
                        baseLocalPosition,
                        baseLocalPosition + Vector3.up * liftHeight,
                        baseLocalRotation,
                        liftDuration,
                        currentCts.Token);
                }

                await PlayAirShakeSegmentAsync(
                    baseLocalPosition + Vector3.up * Mathf.Max(0f, liftHeight),
                    baseLocalRotation,
                    shakeDuration,
                    angle,
                    frequency,
                    currentCts.Token);

                if (dropDuration > 0f && liftHeight > 0f)
                {
                    await PlayLiftSegmentAsync(
                        baseLocalPosition + Vector3.up * liftHeight,
                        baseLocalPosition,
                        baseLocalRotation,
                        dropDuration,
                        currentCts.Token);
                }

                transform.localPosition = baseLocalPosition;
                transform.localRotation = baseLocalRotation;
            }
            catch (OperationCanceledException)
            {
                // Attempt 전환/오브젝트 제거/새 Roll 요청으로 정상 취소된다.
                transform.localPosition = baseLocalPosition;
                transform.localRotation = baseLocalRotation;
            }
            finally
            {
                if (motionCts == currentCts)
                    motionCts = null;

                currentCts.Dispose();
            }
        }

        /// <summary>컵의 상승/하강 구간을 재생한다.</summary>
        private async UniTask PlayLiftSegmentAsync(
            Vector3 startLocalPosition,
            Vector3 targetLocalPosition,
            Quaternion baseLocalRotation,
            float duration,
            CancellationToken cancellationToken)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                cancellationToken.ThrowIfCancellationRequested();

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = 1f - Mathf.Pow(1f - t, 3f);

                // 컵은 수직으로 떠올랐다가 내려온다.
                transform.localPosition = Vector3.Lerp(startLocalPosition, targetLocalPosition, easedT);
                transform.localRotation = baseLocalRotation;

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            transform.localPosition = targetLocalPosition;
            transform.localRotation = baseLocalRotation;
        }

        /// <summary>컵이 공중에 떠 있는 상태에서 좌우/전후로 흔들리는 구간을 재생한다.</summary>
        private async UniTask PlayAirShakeSegmentAsync(
            Vector3 liftedLocalPosition,
            Quaternion baseLocalRotation,
            float duration,
            float angle,
            float frequency,
            CancellationToken cancellationToken)
        {
            if (duration <= 0f)
                return;

            float elapsed = 0f;

            while (elapsed < duration)
            {
                cancellationToken.ThrowIfCancellationRequested();

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float fadeIn = Mathf.Clamp01(t / 0.15f);
                float fadeOut = Mathf.Clamp01((1f - t) / 0.2f);
                float envelope = fadeIn * fadeOut;
                float wave = Mathf.Sin(elapsed * frequency * Mathf.PI * 2f);
                float sideWave = Mathf.Cos(elapsed * frequency * Mathf.PI * 2f * 0.72f);

                // 공중에서 살짝 흔들리도록 위치와 회전을 동시에 흔든다.
                transform.localPosition = liftedLocalPosition + new Vector3(sideWave * 0.035f * envelope, 0f, wave * 0.025f * envelope);
                transform.localRotation =
                    baseLocalRotation *
                    Quaternion.Euler(angle * wave * envelope, 0f, angle * 0.55f * sideWave * envelope);

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            transform.localPosition = liftedLocalPosition;
            transform.localRotation = baseLocalRotation;
        }

        /// <summary>진행 중인 컵 연출을 취소한다.</summary>
        private void CancelMotion()
        {
            if (motionCts == null)
                return;

            motionCts.Cancel();
            motionCts.Dispose();
            motionCts = null;
        }
    }
}
