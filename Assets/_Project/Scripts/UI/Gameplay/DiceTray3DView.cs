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
            diceViews = GetComponentsInChildren<Dice3DView>(true);
        }

        /// <summary>Dice 클릭 콜백을 초기화한다.</summary>
        public void Initialize(Action<int> diceClickedCallback)
        {
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

        /// <summary>위치 변경 없이 주사위 값과 Lock 색상만 갱신한다.</summary>
        public void SetDiceValuesOnly(IReadOnlyList<int> diceValues, IReadOnlyList<bool> lockStates)
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

                bool isLocked = IsDiceLocked(lockStates, diceIndex);

                diceViews[diceIndex].SetDice(diceIndex, diceValues[diceIndex], isLocked);

                if (isLocked)
                    continue;

                if (!TryGetDicePointPose(diceIndex, out Vector3 trayPosition, out Quaternion trayRotation))
                    continue;

                diceViews[diceIndex].MoveTo(trayPosition, trayRotation, unlockedMoveDuration);
            }
        }

        /// <summary>지정 DiceView가 목표 위치 근처에 있는지 확인한다.</summary>
        public bool IsDiceNearPosition(int diceIndex, Vector3 targetPosition, float threshold)
        {
            if (!TryGetDiceView(diceIndex, out Dice3DView diceView))
                return false;

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

            return diceView.PlayJumpRollAsync(jumpHeight, rollEuler, duration, cancellationToken);
        }

        /// <summary>Roll 대상인 Unlock Dice들을 컵 입구로 이동시킨다.</summary>
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
                if (!CanAnimateUnlockedDice(diceValues, lockStates, diceIndex))
                    continue;

                Dice3DView diceView = diceViews[diceIndex];
                diceView.SetDice(diceIndex, diceValues[diceIndex], false);

                if (stagger > 0f && tasks.Count > 0)
                    await UniTask.Delay(TimeSpan.FromSeconds(stagger), cancellationToken: cancellationToken);

                tasks.Add(diceView.PlayArcMoveRollAsync(
                    cupEntryPosition,
                    cupEntryRotation,
                    duration,
                    arcHeight,
                    rollEuler,
                    cancellationToken));
            }

            if (tasks.Count > 0)
                await UniTask.WhenAll(tasks);
        }

        /// <summary>Roll 이후 Unlock Dice들을 컵 입구에서 DiceTray 포인트로 분사한다.</summary>
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
                if (!CanAnimateUnlockedDice(diceValues, lockStates, diceIndex))
                    continue;

                if (!TryGetDicePointPose(diceIndex, out Vector3 trayPosition, out Quaternion trayRotation))
                    continue;

                Dice3DView diceView = diceViews[diceIndex];
                diceView.SetDice(diceIndex, diceValues[diceIndex], false);
                diceView.MoveImmediate(cupEntryPosition, cupEntryRotation);

                if (stagger > 0f && tasks.Count > 0)
                    await UniTask.Delay(TimeSpan.FromSeconds(stagger), cancellationToken: cancellationToken);

                tasks.Add(diceView.PlayArcMoveRollAsync(
                    trayPosition,
                    trayRotation,
                    duration,
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

                diceViews[diceIndex].MoveTo(trayPosition, trayRotation, duration);
            }
        }

        /// <summary>현재 Core 상태 기준으로 모든 DiceView 위치를 다시 정렬한다.</summary>
        public void RestoreDicePlacement(IReadOnlyList<int> diceValues, IReadOnlyList<bool> lockStates)
        {
            SetDice(diceValues, lockStates);
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

                diceViews[i].Initialize(diceClickedCallback);
            }
        }

        /// <summary>지정 DiceView를 안전하게 가져온다.</summary>
        private bool TryGetDiceView(int diceIndex, out Dice3DView diceView)
        {
            diceView = null;

            if (diceViews == null)
                return false;

            if (diceIndex < 0 || diceIndex >= diceViews.Length)
                return false;

            diceView = diceViews[diceIndex];
            return diceView != null;
        }

        /// <summary>지정 DiceIndex의 DicePoint 위치와 회전을 가져온다.</summary>
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

        /// <summary>지정 Dice가 Lock되어 있는지 확인한다.</summary>
        private static bool IsDiceLocked(IReadOnlyList<bool> lockStates, int diceIndex)
        {
            return lockStates != null && diceIndex >= 0 && diceIndex < lockStates.Count && lockStates[diceIndex];
        }

        /// <summary>Roll 연출 대상 Unlock Dice인지 확인한다.</summary>
        private bool CanAnimateUnlockedDice(IReadOnlyList<int> diceValues, IReadOnlyList<bool> lockStates, int diceIndex)
        {
            if (!TryGetDiceView(diceIndex, out Dice3DView diceView))
                return false;

            if (diceValues == null || diceIndex < 0 || diceIndex >= diceValues.Count)
                return false;

            if (IsDiceLocked(lockStates, diceIndex))
                return false;

            return diceView != null;
        }
    }
}
