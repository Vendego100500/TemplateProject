
#if UNITY_EDITOR

using UnityEditor;

namespace AssetsSystem.Editor
{
    [CustomEditor(typeof(Ways))]
    public class WaysEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            Ways ways = (Ways)target;
            foreach (var path in ways.PrefabsPathwaysSerializedData)
            {
                EditorGUILayout.LabelField($"Name: {(EPrefabNames)path.Name}");
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField($"Path: {path.Path}");
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            foreach (var path in ways.ResourcesPathwaysSerializedData)
            {
                EditorGUILayout.LabelField($"Name: {(EResourceNames)path.Name}");
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField($"Path: {path.Path}");
                EditorGUI.indentLevel--;
            }
        }
    }
}

#endif