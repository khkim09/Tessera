namespace Tessera.Core
{
    /// <summary>Core Round 계산에서 특정 Dice/Face 슬롯에 적용할 FaceUpgrade 결과 데이터다.</summary>
    public readonly struct DiceFaceUpgradeData
    {
        public static DiceFaceUpgradeData Empty => new DiceFaceUpgradeData(false, DiceFace.Number(1));

        public bool IsValid { get; }
        public DiceFace ReplacementFace { get; }

        public DiceFaceUpgradeData(bool isValid, DiceFace replacementFace)
        {
            IsValid = isValid;
            ReplacementFace = replacementFace;
        }
    }
}
