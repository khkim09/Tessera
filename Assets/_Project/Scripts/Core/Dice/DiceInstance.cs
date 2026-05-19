using System;
using System.Collections.Generic;

namespace Tessera.Core
{
    /// <summary>하나의 주사위 상태와 현재 눈금, 잠금 여부를 관리한다.</summary>
    public class DiceInstance
    {
        private readonly List<DiceFace> _faces;

        /// <summary>이 주사위가 가질 수 있는 모든 면 목록.</summary>
        public IReadOnlyList<DiceFace> Faces => _faces;

        /// <summary>현재 굴려진 주사위 면.</summary>
        public DiceFace CurrentFace { get; private set; }

        /// <summary>리롤 대상에서 제외되는 잠금 상태인지 확인한다.</summary>
        public bool IsLocked { get; private set; }

        /// <summary>주사위 인스턴스를 생성한다.</summary>
        public DiceInstance(IReadOnlyList<DiceFace> faces, DiceFace initialFace)
        {
            if (faces == null)
                throw new ArgumentNullException(nameof(faces));

            if (faces.Count == 0)
                throw new ArgumentException("주사위는 최소 1개 이상의 면을 가져야 합니다.", nameof(faces));

            _faces = new List<DiceFace>(faces);
            CurrentFace = initialFace;
            IsLocked = false;
        }

        /// <summary>기본 6면 숫자 주사위를 생성한다.</summary>
        public static DiceInstance CreateStandardD6()
        {
            List<DiceFace> faces = new List<DiceFace>(6)
            {
                DiceFace.Number(1),
                DiceFace.Number(2),
                DiceFace.Number(3),
                DiceFace.Number(4),
                DiceFace.Number(5),
                DiceFace.Number(6)
            };

            return new DiceInstance(faces, faces[0]);
        }

        /// <summary>현재 주사위 면을 변경한다.</summary>
        public void SetCurrentFace(DiceFace face)
        {
            CurrentFace = face;
        }

        /// <summary>이 주사위를 잠근다.</summary>
        public void Lock()
        {
            IsLocked = true;
        }

        /// <summary>이 주사위의 잠금을 해제한다.</summary>
        public void Unlock()
        {
            IsLocked = false;
        }

        /// <summary>잠금 상태를 직접 설정한다.</summary>
        public void SetLocked(bool isLocked)
        {
            IsLocked = isLocked;
        }

        /// <summary>현재 숫자 눈금을 반환한다.</summary>
        public int GetCurrentNumberValue()
        {
            if (!CurrentFace.IsNumber)
                throw new InvalidOperationException($"현재 주사위 면은 숫자 면이 아닙니다. Type: {CurrentFace.Type}");

            return CurrentFace.NumberValue;
        }
    }
}
