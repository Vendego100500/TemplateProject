
using UnityEditor;
using UnityEngine;

namespace Core.SpriteDestruction
{
    public static class GridExplodableBaker
    {
        public static void BakePrefab(string prefabPath)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                GridExplodable explodable = contents.GetComponent<GridExplodable>();
                if (!explodable)
                {
                    Debug.LogError($"GridExplodableBaker: no GridExplodable on '{prefabPath}'.");
                    return;
                }

                SerializedObject serialized = new SerializedObject(explodable);
                serialized.FindProperty("_useColliderBounds").boolValue = true;
                Transform actor = contents.transform.Find("Actor");
                if (actor && actor.TryGetComponent(out SpriteRenderer spriteRenderer))
                {
                    serialized.FindProperty("_spriteSource").objectReferenceValue = spriteRenderer;
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
                if (!explodable.BakeFragments())
                {
                    return;
                }

                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                Debug.Log($"GridExplodableBaker: baked {explodable.BakedFragmentCount} fragments into '{prefabPath}'.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            AssetDatabase.SaveAssets();
        }

        public static void ClearPrefab(string prefabPath)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                GridExplodable explodable = contents.GetComponent<GridExplodable>();
                if (!explodable)
                {
                    Debug.LogError($"GridExplodableBaker: no GridExplodable on '{prefabPath}'.");
                    return;
                }

                explodable.ClearBakedFragments();
                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            AssetDatabase.SaveAssets();
        }
    }
}
