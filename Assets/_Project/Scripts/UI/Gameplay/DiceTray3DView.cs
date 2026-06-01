using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Tessera.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace Tessera.UI
{
    /// <summary>공용 DiceTray 위의 Player/Opponent 3D 주사위 세트 표시, 값, 클릭 콜백, 이동과 Roll 연출을 관리한다.</summary>
    public class DiceTray3DView : MonoBehaviour
    {
        [Header("Player Dice Views")]
        [FormerlySerializedAs("diceViews")]
        [SerializeField] private Dice3DView[] playerDiceViews = new Dice3DView[5];

        [Header("Opponent Dice Views")]
        [SerializeField] private Dice3DView[] opponentDiceViews = new Dice3DView[5];

        [Header("Spawn Points")]
        [SerializeField] private Transform[] dicePoints = new Transform[5];

        [Header("Owner Rest Points")]
        [SerializeField] private Transform playerDiceRestPoint;
        [SerializeField] private Transform opponentDiceRestPoint;

        private Action<int> diceClickedCallback;

        /// <summary>인스펙터에서 자식 Dice3DView를 Player 주사위로 자동 수집한다.</summary>
        private void Reset()
        {
            playerDiceViews = GetComponentsInChildren<Dice3DView>(true);
        }

        /// <summary>Player Dice 클릭 콜백을 초기화하고 Opponent Dice 입력은 비활성화한다.</summary>
        public void Initialize(Action<int> diceClickedCallback)
        {
            this.diceClickedCallback = diceClickedCallback;
            InitializeDiceViews();
        }

        /// <summary>지정 소유자의 Dice 세트가 하나 이상 할당되어 있는지 확인한다.</summary>
        public bool HasDiceSet(DiceOwnerType owner)
        {
            Dice3DView[] diceViews = ResolveDiceViews(owner);

            if (diceViews == null || diceViews.Length <= 0)
                return false;

            for (int i = 0; i < diceViews.Length; i++)
            {
                if (diceViews[i] != null)
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
            if (!TryGetDiceView(owner, diceIndex, out Dice3DView diceView))
                return null;

            return diceView;
        }

        /// <summary>Player Core 주사위 값과 Lock 상태를 3D 주사위에 반영한다.</summary>
        public void SetDice(IReadOnlyList<int> diceValues, IReadOnlyList<bool> lockStates)
        {
            SetDice(DiceOwnerType.Player, diceValues, lockStates);
        }

        /// <summary>지정 소유자의 주사위 값과 Lock 상태를 3D 주사위에 반영한다.</summary>
        public void SetDice(DiceOwnerType owner, IReadOnlyList<int> diceValues, IReadOnlyList<bool> lockStates)
        {
            Dice3DView[] diceViews = ResolveDiceViews(owner);

            if (diceViews == null)
                return;

            for (int diceIndex = 0; diceIndex < diceViews.Length; diceIndex++)
            {
                Dice3DView diceView = diceViews[diceIndex];

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

                diceView.MoveImmediate(trayPosition, trayRotation);
            }
        }

        /// <summary>Player Dice 위치 변경 없이 주사위 값과 Lock 색상만 갱신한다.</summary>
        public void SetDiceValuesOnly(IReadOnlyList<int> diceValues, IReadOnlyList<bool> lockStates)
        {
            SetDiceValuesOnly(DiceOwnerType.Player, diceValues, lockStates);
        }

        /// <summary>지정 소유자 Dice 위치 변경 없이 주사위 값과 Lock 색상만 갱신한다.</summary>
        public void SetDiceValuesOnly(DiceOwnerType owner, IReadOnlyList<int> diceValues, IReadOnlyList<bool> lockStates)
        {
            Dice3DView[] diceViews = ResolveDiceViews(owner);

            if (diceViews == null)
                return;

            for (int diceIndex = 0; diceIndex < diceViews.Length; diceIndex++)
            {
                Dice3DView diceView = diceViews[diceIndex];

                if (diceView == null)
                    continue;

                if (diceValues == null || diceIndex >= diceValues.Count)
                {
                    diceView.Hide();
                    continue;
                }

                bool isLocked = IsDiceLocked(lockStates, diceIndex);
                diceView.SetDice(diceIndex, diceValues[diceIndex], isLocked);
            }
        }

        /// <summary>모든 소유자의 주사위 표시를 숨긴다.</summary>
        public void HideAll()
        {
            HideDiceSet(DiceOwnerType.Player);
            HideDiceSet(DiceOwnerType.Opponent);
        }

        /// <summary>지정 소유자의 주사위 표시를 숨긴다.</summary>
        public void HideDiceSet(DiceOwnerType owner)
        {
            Dice3DView[] diceViews = ResolveDiceViews(owner);

            if (diceViews == null)
                return;

            for (int i = 0; i < diceViews.Length; i++)
            {
                if (diceViews[i] != null)
                    diceViews[i].Hide();
            }
        }

        /// <summary>지정 소유자 Dice 세트를 단일 RestPoint로 회수하고 표시를 숨긴다.</summary>
        public async UniTask MoveDiceSetToRestAsync(DiceOwnerType owner, float duration, CancellationToken cancellationToken)
        {
            Dice3DView[] diceViews = ResolveDiceViews(owner);
            Transform restPoint = ResolveRestPoint(owner);

            if (diceViews == null)
                return;

            if (restPoint == null)
            {
                HideDiceSet(owner);
                return;
            }

            List<UniTask> tasks = new List<UniTask>();

            for (int diceIndex = 0; diceIndex < diceViews.Length; diceIndex++)
            {
                Dice3DView diceView = diceViews[diceIndex];

                if (diceView == null)
                    continue;

                diceView.SetDice(diceIndex, Mathf.Clamp(diceView.DiceValue, 1, 6), false);

                tasks.Add(diceView.PlayArcMoveRollAsync(
                    restPoint.position,
                    restPoint.rotation,
                    duration,
                    0.08f,
                    Vector3.zero,
                    cancellationToken));
            }

            if (tasks.Count > 0)
                await UniTask.WhenAll(tasks);

            HideDiceSet(owner);
        }

        /// <summary>지정 소유자 Dice 세트를 DicePoint로 배치한다.</summary>
        public async UniTask MoveDiceSetToTrayAsync(
            DiceOwnerType owner,
            IReadOnlyList<int> diceValues,
            IReadOnlyList<bool> lockStates,
            float duration,
            CancellationToken cancellationToken)
        {
            Dice3DView[] diceViews = ResolveDiceViews(owner);

            if (diceViews == null)
                return;

            List<UniTask> tasks = new List<UniTask>();

            for (int diceIndex = 0; diceIndex < diceViews.Length; diceIndex++)
            {
                Dice3DView diceView = diceViews[diceIndex];

                if (diceView == null)
                    continue;

                if (diceValues == null || diceIndex >= diceValues.Count)
                {
                    diceView.Hide();
                    continue;
                }

                if (!TryGetDicePointPose(diceIndex, out Vector3 trayPosition, out Quaternion trayRotation))
                    continue;

                bool isLocked = IsDiceLocked(lockStates, diceIndex);
                diceView.SetDice(diceIndex, diceValues[diceIndex], isLocked);
                tasks.Add(diceView.PlayArcMoveRollAsync(
                    trayPosition,
                    trayRotation,
                    duration,
                    0.08f,
                    Vector3.zero,
                    cancellationToken));
            }

            if (tasks.Count > 0)
                await UniTask.WhenAll(tasks);
        }

        /// <summary>Player DiceView를 DeviceSlot 하단 Lock 표시 위치로 이동시킨다.</summary>
        public void MoveDiceToLockedDeviceSlot(
            int diceIndex,
            Vector3 targetPosition,
            Quaternion targetRotation,
            float duration)
        {
            if (!TryGetDiceView(DiceOwnerType.Player, diceIndex, out Dice3DView diceView))
                return;

            diceView.MoveTo(targetPosition, targetRotation, duration);
        }

        /// <summary>Player DeviceSlot 하단 Lock Dice 구조에서 DiceView 표시값을 갱신한다.</summary>
        public void SetDiceForDeviceSlotLockPresentation(
            IReadOnlyList<int> diceValues,
            IReadOnlyList<bool> lockStates,
            float unlockedMoveDuration)
        {
            SetDiceForDeviceSlotLockPresentation(DiceOwnerType.Player, diceValues, lockStates, unlockedMoveDuration);
        }

        /// <summary>지정 소유자의 DeviceSlot 하단 Lock Dice 구조에서 DiceView 표시값을 갱신한다.</summary>
        public void SetDiceForDeviceSlotLockPresentation(
            DiceOwnerType owner,
            IReadOnlyList<int> diceValues,
            IReadOnlyList<bool> lockStates,
            float unlockedMoveDuration)
        {
            Dice3DView[] diceViews = ResolveDiceViews(owner);

            if (diceViews == null)
                return;

            for (int diceIndex = 0; diceIndex < diceViews.Length; diceIndex++)
            {
                Dice3DView diceView = diceViews[diceIndex];

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
            if (!TryGetDiceView(DiceOwnerType.Player, diceIndex, out Dice3DView diceView))
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
            if (!TryGetDiceView(DiceOwnerType.Player, diceIndex, out Dice3DView diceView))
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
            Dice3DView[] diceViews = ResolveDiceViews(owner);

            if (diceViews == null)
                return;

            List<UniTask> tasks = new List<UniTask>();

            for (int diceIndex = 0; diceIndex < diceViews.Length; diceIndex++)
            {
                if (!CanAnimateUnlockedDice(owner, diceValues, lockStates, diceIndex))
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
            Dice3DView[] diceViews = ResolveDiceViews(owner);

            if (diceViews == null)
                return;

            List<UniTask> tasks = new List<UniTask>();

            for (int diceIndex = 0; diceIndex < diceViews.Length; diceIndex++)
            {
                if (!CanAnimateUnlockedDice(owner, diceValues, lockStates, diceIndex))
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

        /// <summary>Player DiceView를 DiceTray의 원래 DicePoint 위치로 복귀시킨다.</summary>
        public void RestoreAllDiceToTray(IReadOnlyList<int> diceValues, float duration)
        {
            RestoreAllDiceToTray(DiceOwnerType.Player, diceValues, duration);
        }

        /// <summary>지정 소유자의 DiceView를 DiceTray의 원래 DicePoint 위치로 복귀시킨다.</summary>
        public void RestoreAllDiceToTray(DiceOwnerType owner, IReadOnlyList<int> diceValues, float duration)
        {
            Dice3DView[] diceViews = ResolveDiceViews(owner);

            if (diceViews == null)
                return;

            for (int diceIndex = 0; diceIndex < diceViews.Length; diceIndex++)
            {
                Dice3DView diceView = diceViews[diceIndex];

                if (diceView == null)
                    continue;

                if (diceValues == null || diceIndex >= diceValues.Count)
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

        /// <summary>지정 소유자 Dice 세트의 클릭/하이라이트 가능 여부를 설정한다.</summary>
        public void SetDiceInteractionEnabled(DiceOwnerType owner, bool canClick, bool canHoverVisual)
        {
            Dice3DView[] diceViews = ResolveDiceViews(owner);

            if (diceViews == null)
                return;

            for (int i = 0; i < diceViews.Length; i++)
            {
                if (diceViews[i] != null)
                    diceViews[i].SetInteractionEnabled(canClick, canHoverVisual);
            }
        }

        /// <summary>모든 DiceView에 클릭 콜백을 전달하고 기본 입력 상태를 설정한다.</summary>
        private void InitializeDiceViews()
        {
            InitializeDiceViews(DiceOwnerType.Player, diceClickedCallback);
            InitializeDiceViews(DiceOwnerType.Opponent, null);

            SetDiceInteractionEnabled(DiceOwnerType.Player, false, false);
            SetDiceInteractionEnabled(DiceOwnerType.Opponent, false, false);
        }

        /// <summary>지정 소유자의 DiceView 클릭 콜백을 초기화한다.</summary>
        private void InitializeDiceViews(DiceOwnerType owner, Action<int> callback)
        {
            Dice3DView[] diceViews = ResolveDiceViews(owner);

            if (diceViews == null)
                return;

            for (int i = 0; i < diceViews.Length; i++)
            {
                if (diceViews[i] != null)
                    diceViews[i].Initialize(callback);
            }
        }

        /// <summary>지정 소유자의 DiceView 배열을 반환한다.</summary>
        private Dice3DView[] ResolveDiceViews(DiceOwnerType owner)
        {
            return owner == DiceOwnerType.Opponent ? opponentDiceViews : playerDiceViews;
        }

        /// <summary>지정 소유자의 단일 RestPoint를 반환한다.</summary>
        private Transform ResolveRestPoint(DiceOwnerType owner)
        {
            return owner == DiceOwnerType.Opponent ? opponentDiceRestPoint : playerDiceRestPoint;
        }

        /// <summary>지정 DiceView를 안전하게 가져온다.</summary>
        private bool TryGetDiceView(DiceOwnerType owner, int diceIndex, out Dice3DView diceView)
        {
            diceView = null;
            Dice3DView[] diceViews = ResolveDiceViews(owner);

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
