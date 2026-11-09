
using UnityEditor;
using UnityEditor.UI;

namespace UI.Buttons
{
    [CustomEditor(typeof(MultiGraphicButton), true)]
    [CanEditMultipleObjects]
    public class MultiGraphicButtonEditor : ButtonEditor
    {
        private SerializedProperty _graphics;
        private SerializedProperty _sound;

        protected override void OnEnable()
        {
            base.OnEnable();
            
            _graphics = serializedObject.FindProperty("_graphics");
            _sound = serializedObject.FindProperty("_sound");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            EditorGUILayout.PropertyField(_graphics, true);
            EditorGUILayout.PropertyField(_sound);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
