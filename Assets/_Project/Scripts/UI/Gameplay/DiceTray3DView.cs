using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tessera.UI
{
    /// <summary>DiceTray3D 위의 3D 주사위 표시 위치, 값, 클릭 콜백, LockSlot 이동을 관리한다.</summary>
    public class DiceTray3DView : MonoBehaviour
    {
        [Header("Dice Views")]
        [SerializeField] private Dice3DView[] diceViews = new Dice3DView[5];

        [Header("Spawn Points")]
        [SerializeField] private Transform[] dicePoints = new Transform[5];

        [Header("Lock Movement")]
        [SerializeField] private Vector3 lockSlotWorldOffset = new Vector3(0f, 0.11f, 0f);
        [SerializeField] private float moveDuration = 0.18f;
        [SerializeField] private bool useAnimatedMovement = true;

        private Action<int> diceClickedCallback;
        private bool hasInitializedPlacement;

        /// <summary>인스펙터에서 자식 Dice3DView를 자동 수집한다.</summary>
        private void Reset()
        {
            // 자식 주사위 View를 자동 수집한다.
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
            SetDice(diceValues, lockStates, null, null);
        }

        /// <summary>현재 Core 주사위 값, Lock 상태, LockSlot 매핑을 3D 주사위에 반영한다.</summary>
        public void SetDice(
            IReadOnlyList<int> diceValues,
            IReadOnlyList<bool> lockStates,
            IReadOnlyList<int> lockedDiceIndexBySlot,
            LockSlotRack3DView lockSlotRack3DView)
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

                // Lock 상태면 LockSlot 위로, 아니면 DicePoint 위치로 이동한다.
                MoveDiceByState(diceIndex, isLocked, lockedDiceIndexBySlot, lockSlotRack3DView);
            }

            hasInitializedPlacement = true;
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

        /// <summary>Lock 상태에 따라 주사위 목표 위치를 결정하고 이동한다.</summary>
        private void MoveDiceByState(
            int diceIndex,
            bool isLocked,
            IReadOnlyList<int> lockedDiceIndexBySlot,
            LockSlotRack3DView lockSlotRack3DView)
        {
            if (isLocked && TryGetLockSlotPose(diceIndex, lockedDiceIndexBySlot, lockSlotRack3DView, out Vector3 lockPosition, out Quaternion lockRotation))
            {
                // Lock된 주사위는 해당 LockSlot 위에 올려둔다.
                MoveDiceTo(diceIndex, lockPosition, lockRotation);
                return;
            }

            if (TryGetDicePointPose(diceIndex, out Vector3 trayPosition, out Quaternion trayRotation))
            {
                // Unlock된 주사위는 원래 Tray 위치로 복귀한다.
                MoveDiceTo(diceIndex, trayPosition, trayRotation);
            }
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

        /// <summary>지정 DiceIndex가 배치된 LockSlot의 위치와 회전을 반환한다.</summary>
        private bool TryGetLockSlotPose(
            int diceIndex,
            IReadOnlyList<int> lockedDiceIndexBySlot,
            LockSlotRack3DView lockSlotRack3DView,
            out Vector3 position,
            out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;

            if (lockSlotRack3DView == null)
                return false;

            int slotIndex = FindSlotIndexByDiceIndex(diceIndex, lockedDiceIndexBySlot);

            if (slotIndex < 0)
                return false;

            Transform slotTransform = lockSlotRack3DView.GetSlotTransform(slotIndex);

            if (slotTransform == null)
                return false;

            // 슬롯 표면 위로 약간 띄운 위치를 계산한다.
            position = slotTransform.position + lockSlotWorldOffset;
            rotation = slotTransform.rotation;
            return true;
        }

        /// <summary>특정 DiceIndex가 들어간 LockSlot 인덱스를 찾는다.</summary>
        private static int FindSlotIndexByDiceIndex(int diceIndex, IReadOnlyList<int> lockedDiceIndexBySlot)
        {
            if (lockedDiceIndexBySlot == null)
                return -1;

            for (int slotIndex = 0; slotIndex < lockedDiceIndexBySlot.Count; slotIndex++)
            {
                if (lockedDiceIndexBySlot[slotIndex] == diceIndex)
                    return slotIndex;
            }

            return -1;
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

        /// <summary>지정 DiceView를 목표 위치와 회전으로 이동시킨다.</summary>
        private void MoveDiceTo(int diceIndex, Vector3 targetPosition, Quaternion targetRotation)
        {
            if (diceViews == null)
                return;

            if (diceIndex < 0 || diceIndex >= diceViews.Length)
                return;

            if (diceViews[diceIndex] == null)
                return;

            if (!useAnimatedMovement || !Application.isPlaying || !hasInitializedPlacement)
            {
                // 최초 배치나 에디터 상태에서는 즉시 이동한다.
                diceViews[diceIndex].MoveImmediate(targetPosition, targetRotation);
                return;
            }

            // 런타임 Lock/Unlock은 짧은 이동 연출로 처리한다.
            diceViews[diceIndex].MoveTo(targetPosition, targetRotation, moveDuration);
        }
    }
}
