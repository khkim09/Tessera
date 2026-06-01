using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Tessera.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace Tessera.UI
{
    /// <summary>공용 DiceTray3D 위의 Player/Opponent 3D 주사위 세트 표시, 클릭 콜백, DeviceSlot 이동과 Roll 연출을 관리한다.</summary>
    public class DiceTray3DView : MonoBehaviour
    {
        [Header("Player Dice Views")]
        [FormerlySerializedAs("diceViews")]
        [SerializeField] private Dice3DView[] playerDiceViews = new Dice3DView[5];

        [Header("Opponent Dice Views")]
        [SerializeField] private Dice3DView[] opponentDiceViews = new Dice3DView[5];

        [Header("Spawn Points")]
        [SerializeField] private Transform[] dicePoints = new Transform[5];

        private Action<int> diceClickedCallback;

        /// <summary>인스펙터에서 자식 Player Dice3DView를 자동 수집한다.</summary>
        private void Reset()
        {
            playerDiceViews = GetComponentsInChildren<Dice3DView>(true);
        }

        /// <summary>Player Dice 클릭 콜백을 초기화한다.</summary>
        public void Initialize(Action<int> diceClickedCallback)
        {
            this.diceClickedCallback = diceClickedCallback;
            InitializeDiceViews();
        }

        /// <summary>지정 소유자의 DiceView 배열이 하나 이상 연결되어 있는지 확인한다.</summary>
        public bool HasDiceSet(DiceOwnerType owner)
        {
            Dice3DView[] targetDiceViews = ResolveDiceViews(owner);

            if (targetDiceViews == null || targetDiceViews.Length == 0)
                return false;

            for (int i = 0; i < targetDiceViews.Length; i++)
            {
                if (targetDiceViews[i] != null)
                    return true;
            }

            return false;
        }

        /// <summary>지정 소유자의 DiceView 목록을 반환한다.</summary>
        public IReadOnlyList<Dice3DView> GetDiceViews(DiceOwnerType owner)
        {
            return ResolveDiceViews(owner);
        }

        /// <summary>지정 소유자의 DiceView를 반환한다.</summary>
        public Dice3DView GetDiceView(DiceOwnerType owner, int diceIndex)
        {
            return TryGetDiceView(owner, diceIndex, out Dice3DView diceView)
                ? diceView
                : null;
        }

        /// <summary>현재 Core 주사위 값과 Lock 상태를 Player 3D 주사위에 반영한다.</summary>
        public void SetDice(IReadOnlyList<int> diceValues, IReadOnlyList<bool> lockStates)
        {
            SetDice(DiceOwnerType.Player, diceValues, lockStates);
        }

        /// <summary>현재 Core 주사위 값과 Lock 상태를 지정 소유자의 3D 주사위에 반영한다.</summary>
        public void SetDice(DiceOwnerType owner, IReadOnlyList<int> diceValues, IReadOnlyList<bool> lockStates)
        {
            Dice3DView[] targetDiceViews = ResolveDiceViews(owner);

            if (targetDiceViews == null)
                return;

            for (int diceIndex = 0; diceIndex < targetDiceViews.Length; diceIndex++)
            {
                Dice3DView diceView = targetDiceViews[diceIndex];

                if (diceView == null)
                    continue;

                if (diceValues == null || diceIndex < 0 || diceIndex >= diceValues.Count)
                {
                    diceView.Hide();
                    continue;
                }

                bool isLocked = IsDiceLocked(lockStates, diceIndex);
                diceView.SetDice(diceIndex, diceValues[diceIndex], isLocked);

                if (isLocked)
                    continue;

                if (!TryGetDicePointPose(diceIndex, out Vector3 trayPosition, out Quaternion trayRotation))
                    continue;

                diceView.MoveImmediate(trayPosition, trayRotation);
            }
        }

        /// <summary>위치 변경 없이 Player 주사위 값과 Lock 색상만 갱신한다.</summary>
        public void SetDiceValuesOnly(IReadOnlyList<int> diceValues, IReadOnlyList<bool> lockStates)
        {
            SetDiceValuesOnly(DiceOwnerType.Player, diceValues, lockStates);
        }

        /// <summary>위치 변경 없이 지정 소유자의 주사위 값과 Lock 색상만 갱신한다.</summary>
        public void SetDiceValuesOnly(DiceOwnerType owner, IReadOnlyList<int> diceValues, IReadOnlyList<bool> lockStates)
        {
            Dice3DView[] targetDiceViews = ResolveDiceViews(owner);

            if (targetDiceViews == null)
                return;

            for (int diceIndex = 0; diceIndex < targetDiceViews.Length; diceIndex++)
            {
                Dice3DView diceView = targetDiceViews[diceIndex];

                if (diceView == null)
                    continue;

                if (diceValues == null || diceIndex < 0 || diceIndex >= diceValues.Count)
                {
                    diceView.Hide();
                    continue;
                }

                bool isLocked = IsDiceLocked(lockStates, diceIndex);
                diceView.SetDice(diceIndex, diceValues[diceIndex], isLocked);
            }
        }

        /// <summary>Player/Opponent 모든 주사위 표시를 숨긴다.</summary>
        public void HideAll()
        {
            HideDiceSet(DiceOwnerType.Player);
            HideDiceSet(DiceOwnerType.Opponent);
        }

        /// <summary>지정 소유자의 모든 주사위 표시를 숨긴다.</summary>
        public void HideDiceSet(DiceOwnerType owner)
        {
            Dice3DView[] targetDiceViews = ResolveDiceViews(owner);

            if (targetDiceViews == null)
                return;

            for (int i = 0; i < targetDiceViews.Length; i++)
            {
                if (targetDiceViews[i] == null)
                    continue;

                targetDiceViews[i].Hide();
            }
        }

        /// <summary>Player DiceView를 DeviceSlot 하단 Lock 표시 위치로 이동시킨다.</summary>
        public void MoveDiceToLockedDeviceSlot(
            int diceIndex,
            Vector3 targetPosition,
            Quaternion targetRotation,
            float duration)
        {
            MoveDiceToLockedDeviceSlot(DiceOwnerType.Player, diceIndex, targetPosition, targetRotation, duration);
        }

        /// <summary>지정 소유자의 DiceView를 DeviceSlot 하단 Lock 표시 위치로 이동시킨다.</summary>
        public void MoveDiceToLockedDeviceSlot(
            DiceOwnerType owner,
            int diceIndex,
            Vector3 targetPosition,
            Quaternion targetRotation,
            float duration)
        {
            if (!TryGetDiceView(owner, diceIndex, out Dice3DView diceView))
                return;

            diceView.MoveTo(targetPosition, targetRotation, duration);
        }

        /// <summary>DeviceSlot 하단 Lock Dice 구조에서 Player DiceView 표시값을 갱신한다.</summary>
        public void SetDiceForDeviceSlotLockPresentation(
            IReadOnlyList<int> diceValues,
            IReadOnlyList<bool> lockStates,
            float unlockedMoveDuration)
        {
            SetDiceForDeviceSlotLockPresentation(DiceOwnerType.Player, diceValues, lockStates, unlockedMoveDuration);
        }

        /// <summary>DeviceSlot 하단 Lock Dice 구조에서 지정 소유자의 DiceView 표시값을 갱신한다.</summary>
        public void SetDiceForDeviceSlotLockPresentation(
            DiceOwnerType owner,
            IReadOnlyList<int> diceValues,
            IReadOnlyList<bool> lockStates,
            float unlockedMoveDuration)
        {
            Dice3DView[] targetDiceViews = ResolveDiceViews(owner);

            if (targetDiceViews == null)
                return;

            for (int diceIndex = 0; diceIndex < targetDiceViews.Length; diceIndex++)
            {
                Dice3DView diceView = targetDiceViews[diceIndex];

                if (diceView == null)
                    continue;

                if (diceValues == null || diceIndex >= diceValues.Count)
                {
                    diceView.Hide();
                    continue;
                }

                bool isLocked = IsDiceLocked(lockStates, diceIndex);

                diceView.SetDice(diceIndex, diceValues[diceIndex], isLocked);

                if (isLocked)
                    continue;

                if (!TryGetDicePointPose(diceIndex, out Vector3 trayPosition, out Quaternion trayRotation))
                    continue;

                diceView.MoveTo(trayPosition, trayRotation, unlockedMoveDuration);
            }
        }

        /// <summary>Player DiceView가 목표 위치 근처에 있는지 확인한다.</summary>
        public bool IsDiceNearPosition(int diceIndex, Vector3 targetPosition, float threshold)
        {
            return IsDiceNearPosition(DiceOwnerType.Player, diceIndex, targetPosition, threshold);
        }

        /// <summary>지정 소유자의 DiceView가 목표 위치 근처에 있는지 확인한다.</summary>
        public bool IsDiceNearPosition(DiceOwnerType owner, int diceIndex, Vector3 targetPosition, float threshold)
        {
            if (!TryGetDiceView(owner, diceIndex, out Dice3DView diceView))
                return false;

            float sqrDistance = (diceView.transform.position - targetPosition).sqrMagnitude;
            return sqrDistance <= threshold * threshold;
        }

        /// <summary>Player DiceView에 제자리 점프/회전 연출을 재생한다.</summary>
        public UniTask PlayDiceJumpRollAsync(
            int diceIndex,
            float jumpHeight,
            Vector3 rollEuler,
            float duration,
            CancellationToken cancellationToken)
        {
            return PlayDiceJumpRollAsync(DiceOwnerType.Player, diceIndex, jumpHeight, rollEuler, duration, cancellationToken);
        }

        /// <summary>지정 소유자의 DiceView에 제자리 점프/회전 연출을 재생한다.</summary>
        public UniTask PlayDiceJumpRollAsync(
            DiceOwnerType owner,
            int diceIndex,
            float jumpHeight,
            Vector3 rollEuler,
            float duration,
            CancellationToken cancellationToken)
        {
            if (!TryGetDiceView(owner, diceIndex, out Dice3DView diceView))
                return UniTask.CompletedTask;

            return diceView.PlayJumpRollAsync(jumpHeight, rollEuler, duration, cancellationToken);
        }

        /// <summary>Player Roll 대상인 Unlock Dice들을 컵 입구로 이동시킨다.</summary>
        public UniTask PlayUnlockedDiceEnterCupAsync(
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
            return PlayUnlockedDiceEnterCupAsync(
                DiceOwnerType.Player,
                diceValues,
                lockStates,
                cupEntryPosition,
                cupEntryRotation,
                duration,
                stagger,
                arcHeight,
                rollEuler,
                cancellationToken);
        }

        /// <summary>지정 소유자의 Roll 대상 Unlock Dice들을 컵 입구로 이동시킨다.</summary>
        public async UniTask PlayUnlockedDiceEnterCupAsync(
            DiceOwnerType owner,
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
            Dice3DView[] targetDiceViews = ResolveDiceViews(owner);

            if (targetDiceViews == null)
                return;

            List<UniTask> tasks = new List<UniTask>();

            for (int diceIndex = 0; diceIndex < targetDiceViews.Length; diceIndex++)
            {
                if (!CanAnimateUnlockedDice(owner, diceValues, lockStates, diceIndex))
                    continue;

                Dice3DView diceView = targetDiceViews[diceIndex];
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

        /// <summary>Player Roll 이후 Unlock Dice들을 컵 입구에서 DiceTray 포인트로 분사한다.</summary>
        public UniTask PlayUnlockedDiceScatterFromCupAsync(
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
            return PlayUnlockedDiceScatterFromCupAsync(
                DiceOwnerType.Player,
                diceValues,
                lockStates,
                cupEntryPosition,
                cupEntryRotation,
                duration,
                stagger,
                arcHeight,
                rollEuler,
                cancellationToken);
        }

        /// <summary>지정 소유자의 Roll 이후 Unlock Dice들을 컵 입구에서 DiceTray 포인트로 분사한다.</summary>
        public async UniTask PlayUnlockedDiceScatterFromCupAsync(
            DiceOwnerType owner,
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
            Dice3DView[] targetDiceViews = ResolveDiceViews(owner);

            if (targetDiceViews == null)
                return;

            List<UniTask> tasks = new List<UniTask>();

            for (int diceIndex = 0; diceIndex < targetDiceViews.Length; diceIndex++)
            {
                if (!CanAnimateUnlockedDice(owner, diceValues, lockStates, diceIndex))
                    continue;

                if (!TryGetDicePointPose(diceIndex, out Vector3 trayPosition, out Quaternion trayRotation))
                    continue;

                Dice3DView diceView = targetDiceViews[diceIndex];
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

        /// <summary>Player DiceView를 DiceTray의 원래 DicePoint 위치로 복귀시킨다.</summary>
        public void RestoreAllDiceToTray(IReadOnlyList<int> diceValues, float duration)
        {
            RestoreAllDiceToTray(DiceOwnerType.Player, diceValues, duration);
        }

        /// <summary>지정 소유자의 DiceView를 DiceTray의 원래 DicePoint 위치로 복귀시킨다.</summary>
        public void RestoreAllDiceToTray(DiceOwnerType owner, IReadOnlyList<int> diceValues, float duration)
        {
            Dice3DView[] targetDiceViews = ResolveDiceViews(owner);

            if (targetDiceViews == null)
                return;

            for (int diceIndex = 0; diceIndex < targetDiceViews.Length; diceIndex++)
            {
                Dice3DView diceView = targetDiceViews[diceIndex];

                if (diceView == null)
                    continue;

                if (diceValues == null || diceIndex < 0 || diceIndex >= diceValues.Count)
                {
                    diceView.Hide();
                    continue;
                }

                diceView.SetDice(diceIndex, diceValues[diceIndex], false);

                if (!TryGetDicePointPose(diceIndex, out Vector3 trayPosition, out Quaternion trayRotation))
                    continue;

                diceView.MoveTo(trayPosition, trayRotation, duration);
            }
        }

        /// <summary>현재 Core 상태 기준으로 Player DiceView 위치를 다시 정렬한다.</summary>
        public void RestoreDicePlacement(IReadOnlyList<int> diceValues, IReadOnlyList<bool> lockStates)
        {
            SetDice(DiceOwnerType.Player, diceValues, lockStates);
        }

        /// <summary>현재 Core 상태 기준으로 지정 소유자 DiceView 위치를 다시 정렬한다.</summary>
        public void RestoreDicePlacement(DiceOwnerType owner, IReadOnlyList<int> diceValues, IReadOnlyList<bool> lockStates)
        {
            SetDice(owner, diceValues, lockStates);
        }

        /// <summary>모든 DiceView에 클릭 콜백을 전달한다. Opponent Dice는 클릭 입력을 비활성 콜백으로 둔다.</summary>
        private void InitializeDiceViews()
        {
            InitializeDiceSet(DiceOwnerType.Player, diceClickedCallback);
            InitializeDiceSet(DiceOwnerType.Opponent, null);
        }

        /// <summary>지정 소유자의 DiceView 클릭 콜백을 초기화한다.</summary>
        private void InitializeDiceSet(DiceOwnerType owner, Action<int> callback)
        {
            Dice3DView[] targetDiceViews = ResolveDiceViews(owner);

            if (targetDiceViews == null)
                return;

            for (int i = 0; i < targetDiceViews.Length; i++)
            {
                if (targetDiceViews[i] == null)
                    continue;

                targetDiceViews[i].Initialize(callback);
            }
        }

        /// <summary>지정 소유자의 DiceView 배열을 반환한다.</summary>
        private Dice3DView[] ResolveDiceViews(DiceOwnerType owner)
        {
            return owner == DiceOwnerType.Opponent ? opponentDiceViews : playerDiceViews;
        }

        /// <summary>Player DiceView를 안전하게 가져온다.</summary>
        private bool TryGetDiceView(int diceIndex, out Dice3DView diceView)
        {
            return TryGetDiceView(DiceOwnerType.Player, diceIndex, out diceView);
        }

        /// <summary>지정 소유자의 DiceView를 안전하게 가져온다.</summary>
        private bool TryGetDiceView(DiceOwnerType owner, int diceIndex, out Dice3DView diceView)
        {
            diceView = null;

            Dice3DView[] targetDiceViews = ResolveDiceViews(owner);

            if (targetDiceViews == null)
                return false;

            if (diceIndex < 0 || diceIndex >= targetDiceViews.Length)
                return false;

            diceView = targetDiceViews[diceIndex];
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

        /// <summary>Player Roll 연출 대상 Unlock Dice인지 확인한다.</summary>
        private bool CanAnimateUnlockedDice(IReadOnlyList<int> diceValues, IReadOnlyList<bool> lockStates, int diceIndex)
        {
            return CanAnimateUnlockedDice(DiceOwnerType.Player, diceValues, lockStates, diceIndex);
        }

        /// <summary>지정 소유자의 Roll 연출 대상 Unlock Dice인지 확인한다.</summary>
        private bool CanAnimateUnlockedDice(DiceOwnerType owner, IReadOnlyList<int> diceValues, IReadOnlyList<bool> lockStates, int diceIndex)
        {
            if (!TryGetDiceView(owner, diceIndex, out Dice3DView diceView))
                return false;

            if (diceValues == null || diceIndex < 0 || diceIndex >= diceValues.Count)
                return false;

            if (IsDiceLocked(lockStates, diceIndex))
                return false;

            return diceView != null;
        }
    }
}
