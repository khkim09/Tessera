using UnityEngine;

namespace Tessera.Infrastructure
{
    /// <summary>Unity Inspector에서 런타임 확인용 필드를 읽기 전용으로 표시하기 위한 Attribute다.</summary>
    public class ReadOnlyInspectorAttribute : PropertyAttribute
    {
    }
}
