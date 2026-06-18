using Tessera.Core;
using UnityEngine;

namespace Tessera.Data
{
    /// <summary>SlotPair 한 칸에 장착되는 Device 효과 데이터를 에셋으로 정의한다.</summary>
    [CreateAssetMenu(
        fileName = "SlotPairDevice_",
        menuName = "Tessera/Devices/Slot Pair Device Definition")]
    public class SlotPairDeviceDefinitionSO : ScriptableObject, IShopItemDefinition
    {
        [Header("Identity")]
        [SerializeField] private string deviceId = "device.none";
        [SerializeField] private string displayName = "None";
        [TextArea]
        [SerializeField] private string description = "No device effect.";
        [SerializeField] private int tier = 1;
        [SerializeField] private Sprite icon;
        [SerializeField] private GameObject equippedViewPrefab;

        [Header("Shop")]
        [SerializeField] private int rarity = 1; // Shop 등장, 해금 판단에 사용하는 희귀도
        [SerializeField] private int unlockStage = 1; // Shop 등장 가능 최소 Stage
        [SerializeField] private int baseMoneyPrice = 4; // 기본 가격
        [SerializeField] private int baseOverchargePrice; // 기본 Overcharge 가격

        [Header("Effect")]
        [SerializeField] private SlotPairDeviceType deviceType = SlotPairDeviceType.None;
        [SerializeField] private int intValue;
        [SerializeField] private float floatValue = 1f;
        [SerializeField] private float forceThreshold;

        [Header("Cast Conditions")]
        [SerializeField] private RollPatternType requiredPatternType = RollPatternType.None;
        [SerializeField] private RollPatternType secondaryPatternType = RollPatternType.None;

        [Header("Dice Conditions")]
        [SerializeField] private DiceValueParity requiredParity = DiceValueParity.Any;
        [SerializeField] private int requiredMinDiceValue = 1;
        [SerializeField] private int requiredMaxDiceValue = 6;

        [Header("Slot Conditions")]
        [Tooltip("0-based SlotPair index. -1 means no slot condition.")]
        [SerializeField] private int requiredSlotIndex = -1;

        [Header("Run Conditions")]
        [SerializeField] private int requiredStageThreatLevel;

        [Header("True Damage")]
        [SerializeField] private int trueDamageValue;

        public string DeviceId => deviceId;
        public string DisplayName => displayName;
        public string Description => description;
        public int Tier => Mathf.Max(1, tier);
        public Sprite Icon => icon;
        public GameObject EquippedViewPrefab => equippedViewPrefab;

        public string ItemId => DeviceId;
        public int Rarity => Mathf.Max(1, rarity);
        public int UnlockStage => Mathf.Max(1, unlockStage);
        public int BaseMoneyPrice => Mathf.Max(0, baseMoneyPrice);
        public int BaseOverchargePrice => Mathf.Max(0, baseOverchargePrice);

        public SlotPairDeviceType DeviceType => deviceType;
        public int IntValue => intValue;
        public float FloatValue => floatValue;
        public float ForceThreshold => forceThreshold;

        public RollPatternType RequiredPatternType => requiredPatternType;
        public RollPatternType SecondaryPatternType => secondaryPatternType;

        public DiceValueParity RequiredParity => requiredParity;
        public int RequiredMinDiceValue => Mathf.Clamp(requiredMinDiceValue, 1, 6);
        public int RequiredMaxDiceValue => Mathf.Clamp(requiredMaxDiceValue, 1, 6);

        public int RequiredSlotIndex => requiredSlotIndex;
        public int RequiredStageThreatLevel => Mathf.Max(0, requiredStageThreatLevel);
        public int TrueDamageValue => Mathf.Max(0, trueDamageValue);

        /// <summary>Device SO를 현재 Core 계산용 정의로 변환한다.</summary>
        public SlotPairDeviceDefinition ToCoreDefinition()
        {
            if (deviceType == SlotPairDeviceType.AddScoreByDiceValue)
                return SlotPairDeviceDefinition.AddScoreByDiceValue(GetSafeIntValue(1));

            if (deviceType == SlotPairDeviceType.AddForceIfDiceIncluded)
                return SlotPairDeviceDefinition.AddForceIfDiceIncluded(GetSafeIntValue(1));

            if (deviceType == SlotPairDeviceType.AddForceIfSameAsPrevious)
                return SlotPairDeviceDefinition.AddForceIfSameAsPrevious(GetSafeIntValue(1));

            if (deviceType == SlotPairDeviceType.MultiplyForceIfCurrentForceAtLeast)
            {
                return SlotPairDeviceDefinition.MultiplyForceIfCurrentForceAtLeast(
                    Mathf.Max(0f, forceThreshold),
                    GetSafeFloatValue(1f));
            }

            if (deviceType == SlotPairDeviceType.AddScoreIfCastType)
                return SlotPairDeviceDefinition.AddScoreIfCastType(requiredPatternType, GetSafeIntValue(1));

            return SlotPairDeviceDefinition.Create(
                deviceType,
                GetSafeIntValue(0),
                GetSafeFloatValue(1f),
                Mathf.Max(0f, forceThreshold),
                requiredPatternType,
                secondaryPatternType,
                requiredParity,
                RequiredMinDiceValue,
                RequiredMaxDiceValue,
                requiredSlotIndex,
                RequiredStageThreatLevel,
                TrueDamageValue,
                description);
        }

        /// <summary>양수가 아니면 대체 정수 값을 반환한다.</summary>
        private int GetSafeIntValue(int fallback)
        {
            return intValue <= 0 ? fallback : intValue;
        }

        /// <summary>양수가 아니면 대체 실수 값을 반환한다.</summary>
        private float GetSafeFloatValue(float fallback)
        {
            return floatValue <= 0f ? fallback : floatValue;
        }
    }
}
