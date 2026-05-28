using Tessera.Data;

namespace Tessera.Runtime
{
    /// <summary>수배지 하나의 런타임 진행 상태를 보관한다.</summary>
    public class StageBountyNodeState
    {
        /// <summary>수배지 정의.</summary>
        public StageRoundDefinitionSO Definition { get; }

        /// <summary>클리어 여부.</summary>
        public bool IsCleared { get; private set; }

        /// <summary>폐기 여부.</summary>
        public bool IsDiscarded { get; private set; }

        /// <summary>현재 선택 가능 여부.</summary>
        public bool IsAvailable { get; private set; }

        /// <summary>분노/압력 적용 단계.</summary>
        public int EnrageLevel { get; private set; }

        /// <summary>보스 수배지 여부.</summary>
        public bool IsBoss => Definition != null && Definition.RoundType == Core.StageRoundType.Boss;

        /// <summary>일반 수배지 여부.</summary>
        public bool IsNormal => Definition != null && Definition.RoundType == Core.StageRoundType.Normal;

        /// <summary>수배지 상태를 생성한다.</summary>
        public StageBountyNodeState(StageRoundDefinitionSO definition)
        {
            Definition = definition;
            IsCleared = false;
            IsDiscarded = false;
            IsAvailable = definition != null && definition.InitiallyAvailable;
            EnrageLevel = 0;
        }

        /// <summary>선택 가능 상태를 지정한다.</summary>
        public void SetAvailable(bool isAvailable)
        {
            if (IsCleared || IsDiscarded)
            {
                IsAvailable = false;
                return;
            }

            IsAvailable = isAvailable;
        }

        /// <summary>수배지를 클리어 처리한다.</summary>
        public void MarkCleared()
        {
            IsCleared = true;
            IsDiscarded = false;
            IsAvailable = false;
        }

        /// <summary>수배지를 폐기 처리한다.</summary>
        public void MarkDiscarded()
        {
            if (IsCleared)
                return;

            IsDiscarded = true;
            IsAvailable = false;
        }

        /// <summary>분노 단계를 증가시킨다.</summary>
        public void IncreaseEnrage()
        {
            if (IsCleared || IsDiscarded)
                return;

            EnrageLevel++;
        }
    }
}
