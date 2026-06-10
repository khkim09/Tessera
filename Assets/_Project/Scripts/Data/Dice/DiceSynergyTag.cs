namespace Tessera.Data
{
    /// <summary>DiceType 조합 시너지를 판정하기 위한 색/계열 태그다.</summary>
    public enum DiceSynergyTag
    {
        None = 0,      // 시너지 없음

        Red = 10,     // 홀수/공격 계열
        Blue = 20,    // 짝수/안정 계열
        Iron = 30,    // 고눈금/중량 계열
        Broken = 40,  // Broken Cast/Overcharge 계열
        Gold = 50,    // Money 보상 계열
        Green = 60,   // 저눈금/성장 계열
        Void = 70     // 피해 경감/무효화 계열
    }
}
