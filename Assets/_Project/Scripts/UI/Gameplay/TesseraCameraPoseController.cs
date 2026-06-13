using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Tessera.UI
{
    /// <summary>Battle/Shop 등 화면 모드별 카메라 포즈 전환을 담당한다.</summary>
    public class TesseraCameraPoseController : MonoBehaviour
    {
        /// <summary>이동시킬 대상 카메라 Transform이다.</summary>
        [SerializeField] private Transform targetCamera;

        /// <summary>전투 기본 사선 카메라 포즈 Anchor다.</summary>
        [SerializeField] private Transform battleViewAnchor;

        /// <summary>Shop 전용 TopDown 카메라 포즈 Anchor다.</summary>
        [SerializeField] private Transform shopTopDownViewAnchor;

        /// <summary>전투 포즈로 복귀할 때 걸리는 시간이다.</summary>
        [SerializeField] private float battleViewMoveDuration = 0.35f;

        /// <summary>Shop TopDown 포즈로 이동할 때 걸리는 시간이다.</summary>
        [SerializeField] private float shopViewMoveDuration = 0.45f;

        /// <summary>카메라 이동 보간 곡선이다.</summary>
        [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        /// <summary>현재 실행 중인 카메라 이동 취소 토큰이다.</summary>
        private CancellationTokenSource moveCancellationSource;

        /// <summary>컴포넌트가 파괴될 때 진행 중인 이동을 취소한다.</summary>
        private void OnDestroy()
        {
            CancelMove();
        }

        /// <summary>카메라를 전투 기본 포즈로 이동시킨다.</summary>
        public void MoveToBattleView(string reason)
        {
            MoveToPoseAsync(battleViewAnchor, battleViewMoveDuration, reason).Forget();
        }

        /// <summary>카메라를 Shop TopDown 포즈로 이동시킨다.</summary>
        public void MoveToShopView(string reason)
        {
            MoveToPoseAsync(shopTopDownViewAnchor, shopViewMoveDuration, reason).Forget();
        }

        /// <summary>지정 Anchor 포즈로 카메라를 비동기 이동시킨다.</summary>
        private async UniTaskVoid MoveToPoseAsync(Transform targetPose, float duration, string reason)
        {
            if (targetCamera == null || targetPose == null)
                return;

            // 이전 이동이 남아 있으면 중복 보간을 막기 위해 취소한다.
            CancelMove();

            moveCancellationSource = new CancellationTokenSource();
            CancellationToken cancellationToken = moveCancellationSource.Token;

            Vector3 startPosition = targetCamera.position;
            Quaternion startRotation = targetCamera.rotation;
            Vector3 targetPosition = targetPose.position;
            Quaternion targetRotation = targetPose.rotation;

            float safeDuration = Mathf.Max(0.01f, duration);
            float elapsed = 0f;

            try
            {
                while (elapsed < safeDuration)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // deltaTime 기반으로 자연스럽게 목표 포즈까지 보간한다.
                    elapsed += Time.deltaTime;
                    float normalizedTime = Mathf.Clamp01(elapsed / safeDuration);
                    float easedTime = moveCurve != null
                        ? moveCurve.Evaluate(normalizedTime)
                        : normalizedTime;

                    targetCamera.position = Vector3.Lerp(startPosition, targetPosition, easedTime);
                    targetCamera.rotation = Quaternion.Slerp(startRotation, targetRotation, easedTime);

                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }

                targetCamera.position = targetPosition;
                targetCamera.rotation = targetRotation;
            }
            catch (OperationCanceledException)
            {
                // 새 카메라 이동 요청이 들어오면 기존 이동은 정상 취소로 처리한다.
            }
        }

        /// <summary>진행 중인 카메라 이동을 취소한다.</summary>
        private void CancelMove()
        {
            if (moveCancellationSource == null)
                return;

            moveCancellationSource.Cancel();
            moveCancellationSource.Dispose();
            moveCancellationSource = null;
        }
    }
}
