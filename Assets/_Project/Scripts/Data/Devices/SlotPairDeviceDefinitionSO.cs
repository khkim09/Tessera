using Tessera.Core;
using UnityEngine;

namespace Tessera.Data
{
    /// <summary>SlotPair 한 칸에 장착되는 Device 효과 데이터를 에셋으로 정의한다.</summary>
    [CreateAssetMenu(
        fileName = "SlotPairDevice_",
        menuName = "Tessera/Devices/Slot Pair Device Definition")]
    public class SlotPairDeviceDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string deviceId = "device.none";
        [SerializeField] private string displayName = "None";
        [SerializeField] private string description = "No device effect.";
        [SerializeField] private Sprite icon;

        [Header("Effect")]
        [SerializeField] private SlotPairDeviceType deviceType = SlotPairDeviceType.None;
        [SerializeField] private int intValue;
        [SerializeField] private float floatValue = 1f;
        [SerializeField] private float forceThreshold;
        [SerializeField] private RollPatternType requiredPatternType = RollPatternType.None;

        /// <summary>Device 고유 ID를 반환한다.</summary>
        public string DeviceId => deviceId;

        /// <summary>플레이어에게 표시할 Device 이름을 반환한다.</summary>
        public string DisplayName => displayName;

        /// <summary>플레이어에게 표시할 Device 설명을 반환한다.</summary>
        public string Description => description;

        /// <summary>Device 슬롯에 표시할 아이콘 이미지를 반환한다.</summary>
        public Sprite Icon => icon;

        /// <summary>Device 효과 종류를 반환한다.</summary>
        public SlotPairDeviceType DeviceType => deviceType;

        /// <summary>정수 보정값을 반환한다.</summary>
        public int IntValue => intValue;

        /// <summary>Force 곱연산 값을 반환한다.</summary>
        public float FloatValue => floatValue;

        /// <summary>Force 조건 기준값을 반환한다.</summary>
        public float ForceThreshold => forceThreshold;

        /// <summary>조건 비교용 Cast 타입을 반환한다.</summary>
        public RollPatternType RequiredPatternType => requiredPatternType;

        /// <summary>SO 데이터를 Core 계산용 DeviceDefinition으로 변환한다.</summary>
        public SlotPairDeviceDefinition ToCoreDefinition()
        {
            if (deviceType == SlotPairDeviceType.AddScoreByDiceValue)
                return SlotPairDeviceDefinition.AddScoreByDiceValue(GetSafeIntValue(1));

            if (deviceType == SlotPairDeviceType.AddForceIfDiceIncluded)
                return SlotPairDeviceDefinition.AddForceIfDiceIncluded(GetSafeIntValue(1));

            if (deviceType == SlotPairDeviceType.AddForceIfSameAsPrevious)
                return SlotPairDeviceDefinition.AddForceIfSameAsPrevious(GetSafeIntValue(1));

            if (deviceType == SlotPairDeviceType.MultiplyForceIfCurrentForceAtLeast)
                return SlotPairDeviceDefinition.MultiplyForceIfCurrentForceAtLeast(
                    Mathf.Max(0f, forceThreshold),
                    GetSafeFloatValue(1f));

            if (deviceType == SlotPairDeviceType.AddScoreIfCastType)
                return SlotPairDeviceDefinition.AddScoreIfCastType(requiredPatternType, GetSafeIntValue(1));

            return SlotPairDeviceDefinition.None();
        }

        /// <summary>인스펙터 입력 정수값을 안전한 최소값으로 보정한다.</summary>
        private int GetSafeIntValue(int fallback)
        {
            if (intValue <= 0)
                return fallback;

            return intValue;
        }

        /// <summary>인스펙터 입력 실수값을 안전한 최소값으로 보정한다.</summary>
        private float GetSafeFloatValue(float fallback)
        {
            if (floatValue <= 0f)
                return fallback;

            return floatValue;
        }
    }
}