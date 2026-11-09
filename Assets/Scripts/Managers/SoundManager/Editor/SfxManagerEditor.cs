#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace Managers.Editor
{
    [CustomEditor(typeof(SfxManager))]
    public class SfxManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();

            if (!GUILayout.Button("Cache Clips"))
            {
                return;
            }

            SfxManager manager = (SfxManager)target;
            Undo.RecordObject(manager, "Cache SFX Clips");
            int cachedCount = manager.CacheClips();
            EditorUtility.SetDirty(manager);

            Debug.Log($"Cached {cachedCount} SFX clips.", manager);
        }
    }
}

#endif
