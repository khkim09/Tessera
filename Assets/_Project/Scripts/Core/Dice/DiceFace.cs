using System;

namespace Tessera.Core
{
    /// <summary>주사위의 한 면 정보를 표현한다.</summary>
    public readonly struct DiceFace : IEquatable<DiceFace>
    {
        /// <summary>이 주사위 면의 종류.</summary>
        public DiceFaceType Type { get; }

        /// <summary>숫자 면일 때 사용하는 눈금 값.</summary>
        public int NumberValue { get; }

        /// <summary>일반 숫자 면인지 확인한다.</summary>
        public bool IsNumber => Type == DiceFaceType.Number;

        /// <summary>주사위 면 정보를 생성한다.</summary>
        public DiceFace(DiceFaceType type, int numberValue)
        {
            if (type == DiceFaceType.Number && (numberValue < 1 || numberValue > 6))
                throw new ArgumentOutOfRangeException(nameof(numberValue), "숫자 주사위 면은 1~6 사이 값이어야 합니다.");

            Type = type;
            NumberValue = numberValue;
        }

        /// <summary>일반 숫자 주사위 면을 생성한다.</summary>
        public static DiceFace Number(int value)
        {
            return new DiceFace(DiceFaceType.Number, value);
        }

        /// <summary>두 주사위 면이 같은지 비교한다.</summary>
        public bool Equals(DiceFace other)
        {
            return Type == other.Type && NumberValue == other.NumberValue;
        }

        /// <summary>객체가 같은 주사위 면인지 비교한다.</summary>
        public override bool Equals(object obj)
        {
            return obj is DiceFace other && Equals(other);
        }

        /// <summary>주사위 면의 해시 값을 반환한다.</summary>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Type * 397) ^ NumberValue;
            }
        }

        /// <summary>디버그용 문자열을 반환한다.</summary>
        public override string ToString()
        {
            return Type == DiceFaceType.Number ? NumberValue.ToString() : $"{Type}:{NumberValue}";
        }
    }
}
