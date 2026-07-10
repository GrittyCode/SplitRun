using System;

using UnityEditor;
using UnityEngine;

using SplitRun.Utility;

namespace SplitRun.EditorTools
{
    [CustomPropertyDrawer(typeof(EnumKeyedArray<,>), useForChildren: true)]
    public class EnumKeyedArrayDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;

            SerializedProperty values = property.FindPropertyRelative(EnumKeyedArray.k_ValuesField);
            if (values == null || !property.isExpanded) return height;

            for (int i = 0; i < values.arraySize; i++)
            {
                height += EditorGUI.GetPropertyHeight(values.GetArrayElementAtIndex(i), includeChildren: true)
                          + EditorGUIUtility.standardVerticalSpacing;
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty values = property.FindPropertyRelative(EnumKeyedArray.k_ValuesField);
            if (values == null)
            {
                EditorGUI.LabelField(position, label, new GUIContent("EnumKeyedArray has no values array."));
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            var row = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(row, property.isExpanded, label, toggleOnLabelClick: true);

            if (property.isExpanded)
                DrawRows(values, row);

            EditorGUI.EndProperty();
        }

        private void DrawRows(SerializedProperty values, Rect row)
        {
            string[] names = Enum.GetNames(KeyEnumType());
            int count = Mathf.Min(names.Length, values.arraySize);

            EditorGUI.indentLevel++;

            for (int i = 0; i < count; i++)
            {
                SerializedProperty element = values.GetArrayElementAtIndex(i);

                row.y     += row.height + EditorGUIUtility.standardVerticalSpacing;
                row.height = EditorGUI.GetPropertyHeight(element, includeChildren: true);

                EditorGUI.PropertyField(row, element, new GUIContent(names[i]), includeChildren: true);
            }

            EditorGUI.indentLevel--;
        }

        // The drawer is bound to the field, so the key enum is the field type's first generic argument.
        private Type KeyEnumType() => fieldInfo.FieldType.GetGenericArguments()[0];
    }
}
