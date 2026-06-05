#if UNITY_EDITOR
using Tessera.Infrastructure;
using UnityEditor;
using UnityEngine;

namespace Tessera.Editor
{
    /// <summary>ReadOnlyInspectorAttribute가 붙은 필드를 Inspector에서 비활성화 상태로 그린다.</summary>
    [CustomPropertyDrawer(typeof(ReadOnlyInspectorAttribute))]
    public class ReadOnlyInspectorDrawer : PropertyDrawer
    {
        /// <summary>읽기 전용 필드의 높이를 반환한다.</summary>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        /// <summary>읽기 전용 필드를 Inspector에 그린다.</summary>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            bool previousEnabled = GUI.enabled;
            GUI.enabled = false;

            EditorGUI.PropertyField(position, property, label, true);

            GUI.enabled = previousEnabled;
        }
    }
}
#endif
