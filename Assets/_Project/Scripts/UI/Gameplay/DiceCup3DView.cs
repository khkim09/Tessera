using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tessera.UI
{
    /// <summary>테이블 위 DiceCup 3D 오브젝트의 클릭 입력, 컵 입구 기준점, 공중 흔들림 연출을 관리한다.</summary>
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

        /// <summary>오브젝트 제거 시 진행 중인 흔들림 연출을 정리한다.</summary>
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
            await PlayLiftShakeDropAsync(
                0f,
                0f,
                duration,
                0f,
                angle,
                frequency,
                cancellationToken);
        }

        /// <summary>DiceCup을 공중으로 띄운 뒤 흔들고 다시 내려놓는 연출을 재생한다.</summary>
        public async UniTask PlayLiftShakeDropAsync(
            float liftHeight,
            float liftDuration,
            float shakeDuration,
            float dropDuration,
            float shakeAngle,
            float shakeFrequency,
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
                    await PlayLiftAsync(
                        baseLocalPosition,
                        baseLocalPosition + Vector3.up * liftHeight,
                        baseLocalRotation,
                        liftDuration,
                        currentCts.Token);
                }

                await PlayAirShakeAsync(
                    baseLocalPosition + Vector3.up * liftHeight,
                    baseLocalRotation,
                    shakeDuration,
                    shakeAngle,
                    shakeFrequency,
                    currentCts.Token);

                if (dropDuration > 0f && liftHeight > 0f)
                {
                    await PlayLiftAsync(
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

        /// <summary>컵의 클릭 가능 여부를 변경한다.</summary>
        public void SetInteractable(bool isInteractable)
        {
            if (clickCollider == null)
                return;

            clickCollider.enabled = isInteractable;
        }

        /// <summary>컵을 지정 위치까지 부드럽게 이동한다.</summary>
        private async UniTask PlayLiftAsync(
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

                transform.localPosition = Vector3.Lerp(startLocalPosition, targetLocalPosition, easedT);
                transform.localRotation = baseLocalRotation;

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            transform.localPosition = targetLocalPosition;
            transform.localRotation = baseLocalRotation;
        }

        /// <summary>공중에서 컵을 흔든다.</summary>
        private async UniTask PlayAirShakeAsync(
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
                float fade = Mathf.Sin(t * Mathf.PI);
                float wave = Mathf.Sin(elapsed * frequency * Mathf.PI * 2f);
                float sideWave = Mathf.Cos(elapsed * frequency * Mathf.PI * 2f);

                transform.localPosition = liftedLocalPosition + new Vector3(sideWave * 0.025f * fade, 0f, wave * 0.015f * fade);
                transform.localRotation =
                    baseLocalRotation *
                    Quaternion.Euler(angle * wave * fade, 0f, angle * 0.45f * sideWave * fade);

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
