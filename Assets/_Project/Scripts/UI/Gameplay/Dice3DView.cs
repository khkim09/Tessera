using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tessera.UI
{
    /// <summary>테이블 위 3D 주사위 하나의 임시 표시, 클릭 입력, 위치 이동을 관리한다.</summary>
    public class Dice3DView : MonoBehaviour, IPointerClickHandler
    {
        [Header("References")]
        [SerializeField] private MeshRenderer diceRenderer;
        [SerializeField] private TMP_Text valueText;

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

        /// <summary>현재 원본 DiceIndex를 반환한다.</summary>
        public int DiceIndex => diceIndex;

        /// <summary>현재 주사위 값을 반환한다.</summary>
        public int DiceValue => diceValue;

        /// <summary>현재 Lock 상태를 반환한다.</summary>
        public bool IsLocked => isLocked;

        /// <summary>컴포넌트가 추가될 때 기본 참조를 자동 보정한다.</summary>
        private void Reset()
        {
            // 같은 오브젝트의 MeshRenderer를 기본 렌더러로 사용한다.
            diceRenderer = GetComponent<MeshRenderer>();
        }

        /// <summary>런타임 전용 Material을 준비한다.</summary>
        private void Awake()
        {
            // 주사위별 색상 변경이 독립적으로 동작하도록 Material 인스턴스를 준비한다.
            EnsureRuntimeMaterial();
        }

        /// <summary>오브젝트가 제거될 때 진행 중인 이동 작업을 정리한다.</summary>
        private void OnDestroy()
        {
            // UniTask 이동 연출이 남아 있으면 안전하게 취소한다.
            CancelMoveTask();
        }

        /// <summary>클릭 콜백을 초기화한다.</summary>
        public void Initialize(Action<int> clickedCallback)
        {
            // DiceTray3DView가 전달한 Lock/Unlock 콜백을 저장한다.
            this.clickedCallback = clickedCallback;
        }

        /// <summary>주사위 표시 정보를 갱신한다.</summary>
        public void SetDice(int diceIndex, int diceValue, bool isLocked)
        {
            this.diceIndex = diceIndex;
            this.diceValue = diceValue;
            this.isLocked = isLocked;

            // 값 텍스트와 색상을 현재 Core 상태에 맞춘다.
            SetText(valueText, diceValue.ToString());
            SetDiceColor(GetBaseColor());
            gameObject.SetActive(true);
        }

        /// <summary>주사위 표시를 숨긴다.</summary>
        public void Hide()
        {
            // 아직 연결되지 않은 주사위는 비활성화한다.
            gameObject.SetActive(false);
        }

        /// <summary>주사위를 지정 위치와 회전으로 즉시 이동한다.</summary>
        public void MoveImmediate(Vector3 targetPosition, Quaternion targetRotation)
        {
            // 초기 배치나 즉시 복구가 필요할 때 사용한다.
            CancelMoveTask();
            transform.position = targetPosition;
            transform.rotation = targetRotation;
        }

        /// <summary>주사위를 지정 위치와 회전으로 부드럽게 이동한다.</summary>
        public void MoveTo(Vector3 targetPosition, Quaternion targetRotation, float duration)
        {
            if (duration <= 0f)
            {
                MoveImmediate(targetPosition, targetRotation);
                return;
            }

            // 기존 이동 중이면 중복 이동을 취소하고 새 이동을 시작한다.
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

            // 이동 연출과 겹치지 않도록 기존 이동 작업을 먼저 중단한다.
            CancelMoveTask();

            Vector3 basePosition = transform.position;
            Quaternion baseRotation = transform.rotation;
            float elapsed = 0f;

            try
            {
                while (elapsed < duration)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    float heightT = Mathf.Sin(t * Mathf.PI);

                    // 360도 회전은 Slerp로 보간하면 같은 회전으로 취급될 수 있으므로 프레임별 회전량을 직접 적용한다.
                    Vector3 currentRollEuler = rollEuler * t;
                    Quaternion rollRotation = Quaternion.Euler(currentRollEuler);

                    transform.position = basePosition + Vector3.up * (heightT * jumpHeight);
                    transform.rotation = baseRotation * rollRotation;

                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }

                transform.position = basePosition;
                transform.rotation = baseRotation;
            }
            catch (OperationCanceledException)
            {
                // SlotPair 연출 취소나 Attempt 전환 시 정상 취소된다.
            }
        }

        // /// <summary>계산 대상 주사위 강조 상태를 변경한다.</summary>
        // public void SetHighlighted(bool isHighlighted)
        // {
        //     // Highlight 상태는 Hover 색상과 기본 색상 전환으로 표현한다.
        //     SetDiceColor(isHighlighted ? hoverColor : GetBaseColor());
        // }

        /// <summary>마우스 클릭 시 Dice Lock/Unlock 요청을 전달한다.</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData == null)
                return;

            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (diceIndex < 0)
                return;

            // Presenter의 ToggleDiceLock으로 원본 DiceIndex를 전달한다.
            clickedCallback?.Invoke(diceIndex);
        }

        /// <summary>마우스가 올라왔을 때 임시 강조한다.</summary>
        private void OnMouseEnter()
        {
            // PhysicsRaycaster가 없어도 에디터 테스트에서 시각 피드백을 준다.
            SetDiceColor(hoverColor);
        }

        /// <summary>마우스가 벗어났을 때 현재 상태 색상으로 복구한다.</summary>
        private void OnMouseExit()
        {
            // Hover 해제 시 Lock 상태 기준 색상으로 돌아간다.
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

                    // Ease-out으로 슬롯에 자연스럽게 안착시킨다.
                    transform.position = Vector3.Lerp(startPosition, targetPosition, easedT);
                    transform.rotation = Quaternion.Slerp(startRotation, targetRotation, easedT);

                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }

                transform.position = targetPosition;
                transform.rotation = targetRotation;
            }
            catch (OperationCanceledException)
            {
                // 새 이동 요청이나 오브젝트 제거 시 정상 취소된다.
            }
            finally
            {
                if (moveCts == currentCts)
                {
                    moveCts = null;
                    currentCts.Dispose();
                }
            }
        }

        /// <summary>진행 중인 이동 작업을 취소한다.</summary>
        private void CancelMoveTask()
        {
            if (moveCts == null)
                return;

            // 기존 이동 UniTask를 안전하게 중단한다.
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

            // sharedMaterial 대신 material을 사용해 개별 주사위 색상을 독립 처리한다.
            runtimeMaterial = diceRenderer.material;
        }

        /// <summary>TMP 텍스트를 안전하게 갱신한다.</summary>
        private static void SetText(TMP_Text targetText, string value)
        {
            if (targetText == null)
                return;

            targetText.text = value;
        }
    }
}
