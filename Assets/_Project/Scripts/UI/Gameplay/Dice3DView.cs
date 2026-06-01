using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tessera.UI
{
    /// <summary>테이블 위 3D 주사위 하나의 표시, 클릭 입력, 값별 실제 면 회전, 이동/굴림 연출을 관리한다.</summary>
    public class Dice3DView : MonoBehaviour, IPointerClickHandler
    {
        [Header("References")]
        [SerializeField] private MeshRenderer diceRenderer;
        [SerializeField] private Transform diceVisualRoot;
        [SerializeField] private TMP_Text valueText;

        [Header("Face Rotation Mapping")]
        [SerializeField] private bool applyFaceRotation = true;
        [SerializeField]
        private Vector3[] valueToTopLocalEuler =
        {
            new Vector3(0f, 0f, -90f),
            Vector3.zero,
            new Vector3(-90f, 0f, 0f),
            new Vector3(90f, 0f, 0f),
            new Vector3(180f, 0f, 0f),
            new Vector3(0f, 0f, 90f)
        };

        [Header("Colors")]
        [SerializeField] private Color normalColor = new Color(0.85f, 0.85f, 0.82f, 1f);
        [SerializeField] private Color lockedColor = new Color(0.30f, 0.48f, 0.88f, 1f);
        [SerializeField] private Color hoverColor = new Color(0.95f, 0.92f, 0.70f, 1f);

        private Material runtimeMaterial;
        private CancellationTokenSource moveCts;
        private Action<int> clickedCallback;
        private int diceIndex = -1;
        private int diceValue = 1;
        private bool isLocked;
        private bool clickInputEnabled = true;
        private bool hoverVisualEnabled = true;

        /// <summary>현재 원본 DiceIndex를 반환한다.</summary>
        public int DiceIndex => diceIndex;

        /// <summary>현재 주사위 값을 반환한다.</summary>
        public int DiceValue => diceValue;

        /// <summary>현재 Lock 상태를 반환한다.</summary>
        public bool IsLocked => isLocked;

        /// <summary>컴포넌트가 추가될 때 기본 참조를 자동 보정한다.</summary>
        private void Reset()
        {
            diceRenderer = GetComponentInChildren<MeshRenderer>(true);
            diceVisualRoot = diceRenderer != null ? diceRenderer.transform : transform;
        }

        /// <summary>런타임 전용 Material과 DiceVisualRoot 기본값을 준비한다.</summary>
        private void Awake()
        {
            if (diceRenderer == null)
                diceRenderer = GetComponentInChildren<MeshRenderer>(true);

            if (diceVisualRoot == null && diceRenderer != null)
                diceVisualRoot = diceRenderer.transform;

            EnsureRuntimeMaterial();
            ApplyFaceRotation(diceValue);
        }

        /// <summary>오브젝트가 제거될 때 진행 중인 이동 작업을 정리한다.</summary>
        private void OnDestroy()
        {
            CancelMoveTask();
        }

        /// <summary>클릭 콜백을 초기화한다.</summary>
        public void Initialize(Action<int> clickedCallback)
        {
            this.clickedCallback = clickedCallback;
        }

        /// <summary>주사위 표시 정보를 갱신한다.</summary>
        public void SetDice(int diceIndex, int diceValue, bool isLocked)
        {
            this.diceIndex = diceIndex;
            this.diceValue = Mathf.Clamp(diceValue, 1, 6);
            this.isLocked = isLocked;

            SetText(valueText, this.diceValue.ToString());
            ApplyFaceRotation(this.diceValue);
            SetDiceColor(GetBaseColor());
            gameObject.SetActive(true);
        }

        /// <summary>주사위 표시를 숨긴다.</summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>주사위를 지정 위치와 회전으로 즉시 이동한다.</summary>
        public void MoveImmediate(Vector3 targetPosition, Quaternion targetRotation)
        {
            CancelMoveTask();

            transform.position = targetPosition;
            transform.rotation = targetRotation;

            // DiceVisualRoot가 루트 자신인 임시 구조에서도 최종 윗면 보정이 적용되도록 마지막에 호출한다.
            ApplyFaceRotation(diceValue);
        }

        /// <summary>주사위를 지정 위치와 회전으로 부드럽게 이동한다.</summary>
        public void MoveTo(Vector3 targetPosition, Quaternion targetRotation, float duration)
        {
            if (duration <= 0f)
            {
                MoveImmediate(targetPosition, targetRotation);
                return;
            }

            CancelMoveTask();

            CancellationTokenSource currentCts = new CancellationTokenSource();
            moveCts = currentCts;
            MoveToAsync(targetPosition, targetRotation, duration, currentCts).Forget();
        }

        /// <summary>현재 위치에서 살짝 점프하며 한 바퀴 구르는 연출을 재생한다.</summary>
        public async UniTask PlayJumpRollAsync(
            float jumpHeight,
            Vector3 rollEuler,
            float duration,
            CancellationToken cancellationToken)
        {
            if (duration <= 0f)
                return;

            CancelMoveTask();

            CancellationTokenSource currentCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            moveCts = currentCts;

            Vector3 basePosition = transform.position;
            Quaternion baseRotation = transform.rotation;
            float elapsed = 0f;

            try
            {
                while (elapsed < duration)
                {
                    currentCts.Token.ThrowIfCancellationRequested();

                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    float heightT = Mathf.Sin(t * Mathf.PI);
                    Quaternion rollRotation = Quaternion.Euler(rollEuler * t);

                    transform.position = basePosition + Vector3.up * (heightT * jumpHeight);
                    transform.rotation = baseRotation * rollRotation;

                    await UniTask.Yield(PlayerLoopTiming.Update, currentCts.Token);
                }

                transform.position = basePosition;
                transform.rotation = baseRotation;
                ApplyFaceRotation(diceValue);
            }
            catch (OperationCanceledException)
            {
                ApplyFaceRotation(diceValue);
            }
            finally
            {
                CompleteMoveTask(currentCts);
            }
        }

        /// <summary>목표 위치까지 포물선 이동과 회전을 동시에 재생한다.</summary>
        public async UniTask PlayArcMoveRollAsync(
            Vector3 targetPosition,
            Quaternion targetRotation,
            float duration,
            float arcHeight,
            Vector3 rollEuler,
            CancellationToken cancellationToken)
        {
            if (duration <= 0f)
            {
                MoveImmediate(targetPosition, targetRotation);
                return;
            }

            CancelMoveTask();

            CancellationTokenSource currentCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            moveCts = currentCts;

            Vector3 startPosition = transform.position;
            Quaternion startRotation = transform.rotation;
            float elapsed = 0f;

            try
            {
                while (elapsed < duration)
                {
                    currentCts.Token.ThrowIfCancellationRequested();

                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    float easedT = 1f - Mathf.Pow(1f - t, 3f);
                    float arcT = Mathf.Sin(t * Mathf.PI);

                    Vector3 position = Vector3.Lerp(startPosition, targetPosition, easedT);
                    position += Vector3.up * (arcT * arcHeight);

                    Quaternion baseRotation = Quaternion.Slerp(startRotation, targetRotation, easedT);
                    Quaternion rollRotation = Quaternion.Euler(rollEuler * t);

                    transform.position = position;
                    transform.rotation = baseRotation * rollRotation;

                    await UniTask.Yield(PlayerLoopTiming.Update, currentCts.Token);
                }

                transform.position = targetPosition;
                transform.rotation = targetRotation;
                ApplyFaceRotation(diceValue);
            }
            catch (OperationCanceledException)
            {
                ApplyFaceRotation(diceValue);
            }
            finally
            {
                CompleteMoveTask(currentCts);
            }
        }

        /// <summary>마우스 클릭 시 Dice Lock/Unlock 요청을 전달한다.</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData == null)
                return;

            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (diceIndex < 0)
                return;

            if (!clickInputEnabled)
                return;

            clickedCallback?.Invoke(diceIndex);
        }

        /// <summary>마우스가 올라왔을 때 임시 강조한다.</summary>
        private void OnMouseEnter()
        {
            if (!hoverVisualEnabled)
                return;

            SetDiceColor(hoverColor);
        }

        /// <summary>마우스가 벗어났을 때 현재 상태 색상으로 복구한다.</summary>
        private void OnMouseExit()
        {
            SetDiceColor(GetBaseColor());
        }

        /// <summary>주사위 이동을 UniTask로 처리한다.</summary>
        private async UniTaskVoid MoveToAsync(
            Vector3 targetPosition,
            Quaternion targetRotation,
            float duration,
            CancellationTokenSource currentCts)
        {
            Vector3 startPosition = transform.position;
            Quaternion startRotation = transform.rotation;
            float elapsed = 0f;
            CancellationToken cancellationToken = currentCts.Token;

            try
            {
                while (elapsed < duration)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    float easedT = 1f - Mathf.Pow(1f - t, 3f);

                    transform.position = Vector3.Lerp(startPosition, targetPosition, easedT);
                    transform.rotation = Quaternion.Slerp(startRotation, targetRotation, easedT);

                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }

                transform.position = targetPosition;
                transform.rotation = targetRotation;
                ApplyFaceRotation(diceValue);
            }
            catch (OperationCanceledException)
            {
                ApplyFaceRotation(diceValue);
            }
            finally
            {
                CompleteMoveTask(currentCts);
            }
        }

        /// <summary>현재 Dice 값에 맞는 실제 모델 윗면 회전을 적용한다.</summary>
        private void ApplyFaceRotation(int value)
        {
            if (!applyFaceRotation)
                return;

            Transform rotationRoot = diceVisualRoot;

            if (rotationRoot == null && diceRenderer != null)
                rotationRoot = diceRenderer.transform;

            if (rotationRoot == null)
                return;

            int index = Mathf.Clamp(value, 1, 6) - 1;

            if (valueToTopLocalEuler == null || index < 0 || index >= valueToTopLocalEuler.Length)
                return;

            rotationRoot.localRotation = Quaternion.Euler(valueToTopLocalEuler[index]);
        }

        /// <summary>진행 중인 이동 작업을 완료 처리한다.</summary>
        private void CompleteMoveTask(CancellationTokenSource currentCts)
        {
            if (moveCts != currentCts)
                return;

            moveCts = null;
            currentCts.Dispose();
        }

        /// <summary>진행 중인 이동 작업을 취소한다.</summary>
        private void CancelMoveTask()
        {
            if (moveCts == null)
                return;

            moveCts.Cancel();
            moveCts.Dispose();
            moveCts = null;
        }

        /// <summary>현재 상태 기준 기본 색상을 반환한다.</summary>
        private Color GetBaseColor()
        {
            return isLocked ? lockedColor : normalColor;
        }

        /// <summary>주사위 색상을 변경한다.</summary>
        private void SetDiceColor(Color color)
        {
            if (diceRenderer == null)
                return;

            EnsureRuntimeMaterial();

            if (runtimeMaterial == null)
                return;

            runtimeMaterial.color = color;
        }

        /// <summary>런타임 전용 Material 인스턴스를 준비한다.</summary>
        private void EnsureRuntimeMaterial()
        {
            if (diceRenderer == null)
                return;

            if (runtimeMaterial != null)
                return;

            runtimeMaterial = diceRenderer.material;
        }

        /// <summary>TMP 텍스트를 안전하게 갱신한다.</summary>
        private static void SetText(TMP_Text targetText, string value)
        {
            if (targetText == null)
                return;

            targetText.text = value;
        }

        /// <summary>주사위 클릭 입력 가능 여부를 설정한다.</summary>
        public void SetClickInputEnabled(bool isEnabled)
        {
            clickInputEnabled = isEnabled;
        }

        /// <summary>마우스 Hover 시 시각 강조 가능 여부를 설정한다.</summary>
        public void SetHoverVisualEnabled(bool isEnabled)
        {
            hoverVisualEnabled = isEnabled;

            if (!hoverVisualEnabled)
                SetDiceColor(GetBaseColor());
        }

        /// <summary>주사위 상호작용 상태를 설정한다.</summary>
        public void SetInteractionEnabled(bool canClick, bool canHoverVisual)
        {
            SetClickInputEnabled(canClick);
            SetHoverVisualEnabled(canHoverVisual);
        }
    }
}
