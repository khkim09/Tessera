using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Tessera.UI
{
    /// <summary>DiceTray3D 위의 3D 주사위 표시 위치, 값, 클릭 콜백, DeviceSlot 이동과 Roll 연출을 관리한다.</summary>
    public class DiceTray3DView : MonoBehaviour
    {
        [Header("Dice Views")]
        [SerializeField] private Dice3DView[] diceViews = new Dice3DView[5];

        [Header("Spawn Points")]
        [SerializeField] private Transform[] dicePoints = new Transform[5];

        private Action<int> diceClickedCallback;

        /// <summary>인스펙터에서 자식 Dice3DView를 자동 수집한다.</summary>
        private void Reset()
        {
            // 자식 주사위 View를 자동으로 수집한다.
            diceViews = GetComponentsInChildren<Dice3DView>(true);
        }

        /// <summary>Dice 클릭 콜백을 초기화한다.</summary>
        public void Initialize(Action<int> diceClickedCallback)
        {
            // DiceView가 클릭되면 Presenter로 DiceIndex를 전달하도록 연결한다.
            this.diceClickedCallback = diceClickedCallback;
            InitializeDiceViews();
        }

        /// <summary>현재 Core 주사위 값과 Lock 상태를 3D 주사위에 반영한다.</summary>
        public void SetDice(IReadOnlyList<int> diceValues, IReadOnlyList<bool> lockStates)
        {
            if (diceViews == null)
                return;

            for (int diceIndex = 0; diceIndex < diceViews.Length; diceIndex++)
            {
                if (diceViews[diceIndex] == null)
                    continue;

                if (diceValues == null || diceIndex < 0 || diceIndex >= diceValues.Count)
                {
                    diceViews[diceIndex].Hide();
                    continue;
                }

                bool isLocked = IsDiceLocked(lockStates, diceIndex);
                diceViews[diceIndex].SetDice(diceIndex, diceValues[diceIndex], isLocked);

                if (isLocked)
                    continue;

                if (!TryGetDicePointPose(diceIndex, out Vector3 trayPosition, out Quaternion trayRotation))
                    continue;

                diceViews[diceIndex].MoveImmediate(trayPosition, trayRotation);
            }
        }

        /// <summary>모든 주사위 표시를 숨긴다.</summary>
        public void HideAll()
        {
            if (diceViews == null)
                return;

            for (int i = 0; i < diceViews.Length; i++)
            {
                if (diceViews[i] == null)
                    continue;

                diceViews[i].Hide();
            }
        }

        /// <summary>지정 DiceView를 DeviceSlot 하단 Lock 표시 위치로 이동시킨다.</summary>
        public void MoveDiceToLockedDeviceSlot(
            int diceIndex,
            Vector3 targetPosition,
            Quaternion targetRotation,
            float duration)
        {
            if (!TryGetDiceView(diceIndex, out Dice3DView diceView))
                return;

            // Lock된 주사위를 DeviceSlot 하단 표시 위치로 이동한다.
            diceView.MoveTo(targetPosition, targetRotation, duration);
        }

        /// <summary>DeviceSlot 하단 Lock Dice 구조에서 DiceView 표시값을 갱신한다.</summary>
        public void SetDiceForDeviceSlotLockPresentation(
            IReadOnlyList<int> diceValues,
            IReadOnlyList<bool> lockStates,
            float unlockedMoveDuration)
        {
            if (diceViews == null)
                return;

            for (int diceIndex = 0; diceIndex < diceViews.Length; diceIndex++)
            {
                if (diceViews[diceIndex] == null)
                    continue;

                if (diceValues == null || diceIndex >= diceValues.Count)
                {
                    diceViews[diceIndex].Hide();
                    continue;
                }

                bool isLocked = lockStates != null && diceIndex < lockStates.Count && lockStates[diceIndex];

                // Dice 값과 Lock 시각 상태는 갱신하되, Lock된 Dice의 위치는 건드리지 않는다.
                diceViews[diceIndex].SetDice(diceIndex, diceValues[diceIndex], isLocked);

                if (isLocked)
                    continue;

                if (!TryGetDicePointPose(diceIndex, out Vector3 trayPosition, out Quaternion trayRotation))
                    continue;

                // Unlock된 Dice만 DiceTray 원래 위치로 정렬한다.
                diceViews[diceIndex].MoveTo(trayPosition, trayRotation, unlockedMoveDuration);
            }
        }

        /// <summary>지정 DiceView가 목표 위치 근처에 있는지 확인한다.</summary>
        public bool IsDiceNearPosition(int diceIndex, Vector3 targetPosition, float threshold)
        {
            if (!TryGetDiceView(diceIndex, out Dice3DView diceView))
                return false;

            // 이미 목표 위치에 가까우면 불필요한 재이동을 막는다.
            float sqrDistance = (diceView.transform.position - targetPosition).sqrMagnitude;
            return sqrDistance <= threshold * threshold;
        }

        /// <summary>지정 DiceView에 제자리 점프/회전 연출을 재생한다.</summary>
        public UniTask PlayDiceJumpRollAsync(
            int diceIndex,
            float jumpHeight,
            Vector3 rollEuler,
            float duration,
            CancellationToken cancellationToken)
        {
            if (!TryGetDiceView(diceIndex, out Dice3DView diceView))
                return UniTask.CompletedTask;

            // 현재 위치에서 SlotPair 계산 반응 연출을 재생한다.
            return diceView.PlayJumpRollAsync(jumpHeight, rollEuler, duration, cancellationToken);
        }

        /// <summary>Unlock 상태의 DiceView들을 컵 입구로 이동시킨다.</summary>
        public async UniTask PlayUnlockedDiceEnterCupAsync(
            IReadOnlyList<int> diceValues,
            IReadOnlyList<bool> lockStates,
            Vector3 cupEntryPosition,
            Quaternion cupEntryRotation,
            float duration,
            float stagger,
            float arcHeight,
            Vector3 rollEuler,
            CancellationToken cancellationToken)
        {
            if (diceViews == null)
                return;

            List<UniTask> tasks = new List<UniTask>();

            for (int diceIndex = 0; diceIndex < diceViews.Length; diceIndex++)
            {
                if (!CanAnimateUnlockedDice(diceIndex, diceValues, lockStates))
                    continue;

                tasks.Add(PlayDiceEnterCupDelayedAsync(
                    diceIndex,
                    diceValues[diceIndex],
                    cupEntryPosition,
                    cupEntryRotation,
                    duration,
                    stagger * tasks.Count,
                    arcHeight,
                    rollEuler,
                    cancellationToken));
            }

            if (tasks.Count > 0)
                await UniTask.WhenAll(tasks);
        }

        /// <summary>Unlock 상태의 DiceView들을 컵 입구에서 Tray 위치로 흩뿌린다.</summary>
        public async UniTask PlayUnlockedDiceScatterFromCupAsync(
            IReadOnlyList<int> diceValues,
            IReadOnlyList<bool> lockStates,
            Vector3 cupEntryPosition,
            Quaternion cupEntryRotation,
            float duration,
            float stagger,
            float arcHeight,
            Vector3 rollEuler,
            CancellationToken cancellationToken)
        {
            if (diceViews == null)
                return;

            List<UniTask> tasks = new List<UniTask>();

            for (int diceIndex = 0; diceIndex < diceViews.Length; diceIndex++)
            {
                if (!CanAnimateUnlockedDice(diceIndex, diceValues, lockStates))
                    continue;

                if (!TryGetDicePointPose(diceIndex, out Vector3 trayPosition, out Quaternion trayRotation))
                    continue;

                tasks.Add(PlayDiceScatterDelayedAsync(
                    diceIndex,
                    diceValues[diceIndex],
                    cupEntryPosition,
                    cupEntryRotation,
                    trayPosition,
                    trayRotation,
                    duration,
                    stagger * tasks.Count,
                    arcHeight,
                    rollEuler,
                    cancellationToken));
            }

            if (tasks.Count > 0)
                await UniTask.WhenAll(tasks);
        }

        /// <summary>모든 DiceView를 DiceTray의 원래 DicePoint 위치로 복귀시킨다.</summary>
        public void RestoreAllDiceToTray(IReadOnlyList<int> diceValues, float duration)
        {
            if (diceViews == null)
                return;

            for (int diceIndex = 0; diceIndex < diceViews.Length; diceIndex++)
            {
                if (diceViews[diceIndex] == null)
                    continue;

                if (diceValues == null || diceIndex < 0 || diceIndex >= diceValues.Count)
                {
                    diceViews[diceIndex].Hide();
                    continue;
                }

                diceViews[diceIndex].SetDice(diceIndex, diceValues[diceIndex], false);

                if (!TryGetDicePointPose(diceIndex, out Vector3 trayPosition, out Quaternion trayRotation))
                    continue;

                // Lock 상태와 무관하게 원래 DicePoint 위치로 복귀시킨다.
                diceViews[diceIndex].MoveTo(trayPosition, trayRotation, duration);
            }
        }

        /// <summary>모든 DiceView에 클릭 콜백을 전달한다.</summary>
        private void InitializeDiceViews()
        {
            if (diceViews == null)
                return;

            for (int i = 0; i < diceViews.Length; i++)
            {
                if (diceViews[i] == null)
                    continue;

                // 각 DiceView가 클릭되면 원본 DiceIndex를 Presenter로 전달한다.
                diceViews[i].Initialize(diceClickedCallback);
            }
        }

        /// <summary>지정 DiceIndex의 DiceView를 안전하게 반환한다.</summary>
        private bool TryGetDiceView(int diceIndex, out Dice3DView diceView)
        {
            diceView = null;

            if (diceViews == null)
                return false;

            if (diceIndex < 0 || diceIndex >= diceViews.Length)
                return false;

            if (diceViews[diceIndex] == null)
                return false;

            diceView = diceViews[diceIndex];
            return true;
        }

        /// <summary>지정 DiceIndex의 Tray 위치와 회전을 반환한다.</summary>
        private bool TryGetDicePointPose(int diceIndex, out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;

            if (dicePoints == null)
                return false;

            if (diceIndex < 0 || diceIndex >= dicePoints.Length)
                return false;

            if (dicePoints[diceIndex] == null)
                return false;

            position = dicePoints[diceIndex].position;
            rotation = dicePoints[diceIndex].rotation;
            return true;
        }

        /// <summary>지정 Dice가 Roll 연출 대상인지 확인한다.</summary>
        private bool CanAnimateUnlockedDice(
            int diceIndex,
            IReadOnlyList<int> diceValues,
            IReadOnlyList<bool> lockStates)
        {
            if (!TryGetDiceView(diceIndex, out _))
                return false;

            if (diceValues == null || diceIndex < 0 || diceIndex >= diceValues.Count)
                return false;

            // Lock된 Dice는 이번 Roll 대상이 아니므로 컵 연출에서 제외한다.
            return !IsDiceLocked(lockStates, diceIndex);
        }

        /// <summary>지연 후 DiceView를 컵 입구로 이동시킨다.</summary>
        private async UniTask PlayDiceEnterCupDelayedAsync(
            int diceIndex,
            int diceValue,
            Vector3 cupEntryPosition,
            Quaternion cupEntryRotation,
            float duration,
            float delay,
            float arcHeight,
            Vector3 rollEuler,
            CancellationToken cancellationToken)
        {
            if (delay > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: cancellationToken);

            if (!TryGetDiceView(diceIndex, out Dice3DView diceView))
                return;

            // 컵으로 들어갈 때는 기존 값을 유지한다.
            diceView.SetDice(diceIndex, diceValue, false);
            await diceView.PlayArcMoveRollAsync(cupEntryPosition, cupEntryRotation, duration, arcHeight, rollEuler, cancellationToken);
        }

        /// <summary>지연 후 DiceView를 컵 입구에서 Tray 위치로 흩뿌린다.</summary>
        private async UniTask PlayDiceScatterDelayedAsync(
            int diceIndex,
            int diceValue,
            Vector3 cupEntryPosition,
            Quaternion cupEntryRotation,
            Vector3 trayPosition,
            Quaternion trayRotation,
            float duration,
            float delay,
            float arcHeight,
            Vector3 rollEuler,
            CancellationToken cancellationToken)
        {
            if (delay > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: cancellationToken);

            if (!TryGetDiceView(diceIndex, out Dice3DView diceView))
                return;

            // 컵에서 나올 때는 Core Roll 이후의 새 값으로 갱신한다.
            diceView.SetDice(diceIndex, diceValue, false);
            diceView.MoveImmediate(cupEntryPosition, cupEntryRotation);
            await diceView.PlayArcMoveRollAsync(trayPosition, trayRotation, duration, arcHeight, rollEuler, cancellationToken);
        }

        /// <summary>지정 DiceIndex의 Lock 상태를 안전하게 반환한다.</summary>
        private static bool IsDiceLocked(IReadOnlyList<bool> lockStates, int diceIndex)
        {
            if (lockStates == null)
                return false;

            if (diceIndex < 0 || diceIndex >= lockStates.Count)
                return false;

            return lockStates[diceIndex];
        }
    }
}
