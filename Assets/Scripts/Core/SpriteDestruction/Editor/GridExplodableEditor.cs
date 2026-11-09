
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Core.SpriteDestruction
{
    [CustomEditor(typeof(GridExplodable))]
    public class GridExplodableEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GridExplodable explodable = (GridExplodable)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Fragment Bake", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                explodable.HasBakedFragments
                    ? $"Baked fragments: {explodable.BakedFragmentCount}. Runtime explode will reuse them and only swap the current sprite."
                    : "No baked fragments. Explode will generate pieces at runtime (can hitch).",
                explodable.HasBakedFragments ? MessageType.Info : MessageType.Warning);

            SerializedProperty cellSize = serializedObject.FindProperty("_cellSize");
            SerializedProperty useColliderBounds = serializedObject.FindProperty("_useColliderBounds");
            if (SpriteGridExploder.TryGetGridLayout(
                    explodable.gameObject,
                    cellSize.floatValue,
                    useColliderBounds.boolValue,
                    out SpriteGridExploder.GridLayout layout))
            {
                EditorGUILayout.LabelField("Grid", $"{layout.Columns} × {layout.Rows}  ({layout.Columns * layout.Rows} cells)");
            }

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                if (GUILayout.Button("Bake Fragments"))
                {
                    Bake(explodable);
                }

                using (new EditorGUI.DisabledScope(!explodable.HasBakedFragments))
                {
                    if (GUILayout.Button("Clear Baked Fragments"))
                    {
                        Clear(explodable);
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private static void Bake(GridExplodable explodable)
        {
            if (TryGetPrefabAssetPath(explodable, out string assetPath)
                && PrefabStageUtility.GetPrefabStage(explodable.gameObject) == null
                && !PrefabUtility.IsPartOfPrefabInstance(explodable))
            {
                GridExplodableBaker.BakePrefab(assetPath);
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(explodable.gameObject, "Bake Fragments");
            if (explodable.BakeFragments())
            {
                MarkDirty(explodable);
            }
        }

        private static void Clear(GridExplodable explodable)
        {
            if (TryGetPrefabAssetPath(explodable, out string assetPath)
                && PrefabStageUtility.GetPrefabStage(explodable.gameObject) == null
                && !PrefabUtility.IsPartOfPrefabInstance(explodable))
            {
                GridExplodableBaker.ClearPrefab(assetPath);
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(explodable.gameObject, "Clear Baked Fragments");
            explodable.ClearBakedFragments();
            MarkDirty(explodable);
        }

        private static bool TryGetPrefabAssetPath(GridExplodable explodable, out string assetPath)
        {
            assetPath = AssetDatabase.GetAssetPath(explodable);
            if (string.IsNullOrEmpty(assetPath))
            {
                assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(explodable);
            }

            return !string.IsNullOrEmpty(assetPath) && assetPath.EndsWith(".prefab");
        }

        private static void MarkDirty(GridExplodable explodable)
        {
            EditorUtility.SetDirty(explodable);
            EditorUtility.SetDirty(explodable.gameObject);
            PrefabStage stage = PrefabStageUtility.GetPrefabStage(explodable.gameObject);
            if (stage != null)
            {
                EditorSceneManager.MarkSceneDirty(stage.scene);
            }

            if (PrefabUtility.IsPartOfPrefabInstance(explodable))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(explodable);
            }
        }
    }
}
