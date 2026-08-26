
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AssetsSystem.Editor
{
    [InitializeOnLoad, CreateAssetMenu(fileName = "WaysCreator", menuName = "Scriptable Object/Ways Creator", order = 51)]
    public class WaysCreator : ScriptableObject
    {
        private const string WAYS_CREATOR_PATH = "Assets/Gamedata/Resources/ScriptableObjects/WaysCreator.asset";
        private const string WAYS_PATH = "Assets/Gamedata/Resources/ScriptableObjects/Ways.asset";
        private const string PREFAB_NAMES_PATH = "Assets/Scripts/AssetsSystem/Enums/EPrefabNames.cs";
        private const string RESOURCE_NAMES_PATH = "Assets/Scripts/AssetsSystem/Enums/EResourceNames.cs";

        private static WaysCreator _instance;
        private static Ways _ways;

        [SerializeField] [HideInInspector] private List<PrefabKeeper> _prefabs = new();
        [SerializeField] [HideInInspector] private List<ResourceKeeper> _resources = new();


        static WaysCreator()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        public static WaysCreator Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = AssetDatabase.LoadAssetAtPath<WaysCreator>(WAYS_CREATOR_PATH);
                }

                return _instance;
            }
        }

        private static Ways Ways
        {
            get
            {
                if (!_ways)
                {
                    _ways = AssetDatabase.LoadAssetAtPath<Ways>(WAYS_PATH);
                }

                if (_ways)
                {
                    return _ways;
                }

                _ways = CreateInstance<Ways>();
                AssetDatabase.CreateAsset(_ways, WAYS_PATH);
                return _ways;
            }
        }

        public List<PrefabKeeper> Prefabs => _prefabs;
        public List<ResourceKeeper> Resources => _resources;


        public static void AddNameToPrefabsEnum(string name)
        {
            Debug.Log("Adding new value to prefabs Enum = " + name);

            string[] oldNames = Enum.GetNames(typeof(EPrefabNames));
            int[] oldValues = (int[])Enum.GetValues(typeof(EPrefabNames));
            using (StreamWriter writer = File.CreateText(PREFAB_NAMES_PATH))
            {
                writer.WriteLine("public enum EPrefabNames {");
                int i = 0;
                foreach (var item in oldNames)
                {
                    writer.WriteLine("    " + item + " = " + oldValues[i] + ",");
                    i++;
                }

                int val = i == 0 ? 0 : oldValues[i - 1] + 1;
                writer.WriteLine("    " + name + " = " + val + ",");
                writer.WriteLine("}");
            }

            AssetDatabase.ImportAsset(PREFAB_NAMES_PATH, ImportAssetOptions.ForceUpdate);
        }

        public static void RemoveNameFromPrefabsEnum(string name)
        {
            Debug.Log("Remove value from prefabs Enum = " + name);

            string[] oldNames = Enum.GetNames(typeof(EPrefabNames));
            int[] oldValues = (int[])Enum.GetValues(typeof(EPrefabNames));
            using (StreamWriter writer = File.CreateText(PREFAB_NAMES_PATH))
            {
                writer.WriteLine("public enum EPrefabNames {");
                int i = 0;
                foreach (var item in oldNames)
                {
                    if (item != name)
                    {
                        writer.WriteLine("    " + item + " = " + oldValues[i] + ",");
                    }
                    i++;
                }

                writer.WriteLine("}");
            }

            AssetDatabase.ImportAsset(PREFAB_NAMES_PATH, ImportAssetOptions.ForceUpdate);
        }

        public static void AddNameToResourcesEnum(string name)
        {
            Debug.Log("Adding new value to resources Enum = " + name);

            string[] oldNames = Enum.GetNames(typeof(EResourceNames));
            int[] oldValues = (int[])Enum.GetValues(typeof(EResourceNames));
            using (StreamWriter writer = File.CreateText(RESOURCE_NAMES_PATH))
            {
                writer.WriteLine("public enum EResourceNames {");
                int i = 0;
                foreach (var item in oldNames)
                {
                    writer.WriteLine("    " + item + " = " + oldValues[i] + ",");
                    i++;
                }

                int val = i == 0 ? 0 : oldValues[i - 1] + 1;
                writer.WriteLine("    " + name + " = " + val);
                writer.WriteLine("}");
            }

            AssetDatabase.ImportAsset(RESOURCE_NAMES_PATH, ImportAssetOptions.ForceUpdate);
        }

        public static void RemoveNameFromResourcesEnum(string name)
        {
            Debug.Log("Remove value from resources Enum = " + name);

            string[] oldNames = Enum.GetNames(typeof(EResourceNames));
            int[] oldValues = (int[])Enum.GetValues(typeof(EResourceNames));
            using (StreamWriter writer = File.CreateText(RESOURCE_NAMES_PATH))
            {
                writer.WriteLine("public enum EResourceNames {");
                int i = 0;
                foreach (var item in oldNames)
                {
                    if (item != name)
                    {
                        writer.WriteLine("    " + item + " = " + oldValues[i] + ",");
                    }
                    i++;
                }

                writer.WriteLine("}");
            }

            AssetDatabase.ImportAsset(RESOURCE_NAMES_PATH, ImportAssetOptions.ForceUpdate);
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (EditorApplication.isPlaying || !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            Debug.Log("OnPlayModeChanged call SavePathways");
            Instance.SavePathways();
        }

        private void SavePathways()
        {
            SavePrefabsPathways();
            SaveResourcesPathways();
            EditorUtility.SetDirty(Ways);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public void AddNewPrefab(EPrefabNames prefabName, GameObject prefab)
        {
            _prefabs.Add(new PrefabKeeper { Prefab = prefab, Name = prefabName });
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        public void AddNewResource(EResourceNames resourceName, Object resource)
        {
            _resources.Add(new ResourceKeeper { Resource = resource, Name = resourceName });
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        private void SavePrefabsPathways()
        {
            List<PathKeeper> list = Ways.PrefabsPathwaysSerializedData;
            list.Clear();

            foreach (var item in _prefabs)
            {
                if (Enum.IsDefined(typeof(EPrefabNames), item.Name) && item.Prefab)
                {
                    string path = AssetDatabase.GetAssetPath(item.Prefab);
                    list.Add(new PathKeeper((int)item.Name, path));
                }
            }
        }

        private void SaveResourcesPathways()
        {
            List<PathKeeper> list = Ways.ResourcesPathwaysSerializedData;
            list.Clear();

            foreach (var item in _resources)
            {
                if (Enum.IsDefined(typeof(EResourceNames), item.Name) && item.Resource)
                {
                    string path = AssetDatabase.GetAssetPath(item.Resource);
                    list.Add(new PathKeeper((int)item.Name, path));
                }
            }
        }
    }

    [Serializable]
    public class PrefabKeeper
    {
        public GameObject Prefab;
        public EPrefabNames Name;
    }

    [Serializable]
    public class ResourceKeeper
    {
        public Object Resource;
        public EResourceNames Name;
    }

    [CustomEditor(typeof(WaysCreator))]
    internal class WaysCreatorInspector : UnityEditor.Editor
    {
        private GameObject _newPrefab;
        private string _newPrefabsEnumName = "";
        private Object _newResource;
        private string _newResourcesEnumName = "";

        private void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GUI.enabled = !EditorApplication.isPlaying;
            WaysCreator myTarget = (WaysCreator)target;
            Undo.RecordObject(myTarget, "WaysCreator change");

            if (PrefabsGUI(myTarget) | ResourcesGUI(myTarget))
            {
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssets();
            }

            GUILayout.Space(6);
            EditorGUILayout.LabelField("Add new prefab", EditorStyles.boldLabel);

            _newPrefabsEnumName = EditorGUILayout.TextField("Name", _newPrefabsEnumName);
            _newPrefab = EditorGUILayout.ObjectField("Prefab", _newPrefab, typeof(GameObject), false) as GameObject;

            GUI.enabled = !EditorApplication.isCompiling && !string.IsNullOrEmpty(_newPrefabsEnumName) &&
                          !Enum.IsDefined(typeof(EPrefabNames), _newPrefabsEnumName);
            if (GUILayout.Button("Add new enum value and prefab"))
            {
                EditorPrefs.SetString("NewPrefabsEnumName", _newPrefabsEnumName);
                if (_newPrefab)
                {
                    EditorPrefs.SetString("NewPrefabGUID", AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_newPrefab)));
                }

                WaysCreator.AddNameToPrefabsEnum(_newPrefabsEnumName);
            }

            GUILayout.Space(6);
            GUI.enabled = !EditorApplication.isPlaying;
            EditorGUILayout.LabelField("Add new resource", EditorStyles.boldLabel);

            _newResourcesEnumName = EditorGUILayout.TextField("Name", _newResourcesEnumName);
            _newResource = EditorGUILayout.ObjectField("Resource", _newResource, typeof(Object), false);

            GUI.enabled = !EditorApplication.isCompiling && !string.IsNullOrEmpty(_newResourcesEnumName) &&
                          !Enum.IsDefined(typeof(EResourceNames), _newResourcesEnumName);
            if (GUILayout.Button("Add new enum value and resource"))
            {
                EditorPrefs.SetString("NewResourcesEnumName", _newResourcesEnumName);
                if (_newResource)
                {
                    EditorPrefs.SetString("NewResourceGUID", AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_newResource)));
                }

                WaysCreator.AddNameToResourcesEnum(_newResourcesEnumName);
            }

            GUI.enabled = !EditorApplication.isPlaying;

            if (EditorApplication.isCompiling)
            {
                EditorGUILayout.HelpBox("Please wait for compilation to finish", MessageType.Info);
            }
        }

        private static bool PrefabsGUI(WaysCreator myTarget)
        {
            GUI.enabled = !EditorApplication.isCompiling && !EditorApplication.isPlaying;
            EditorGUILayout.LabelField("Prefabs", EditorStyles.boldLabel);

            bool needToSave = false;
            Array enumPrefabValues = Enum.GetValues(typeof(EPrefabNames));
            foreach (EPrefabNames itemName in enumPrefabValues)
            {
                GUILayout.BeginHorizontal();
                PrefabKeeper prefabKeeper = myTarget.Prefabs.Find(p => p.Name == itemName);
                if (prefabKeeper == null)
                {
                    prefabKeeper = new PrefabKeeper { Name = itemName, Prefab = null };
                    myTarget.Prefabs.Add(prefabKeeper);
                    needToSave = true;
                }

                GameObject obj = EditorGUILayout.ObjectField(itemName.ToString(), prefabKeeper.Prefab, typeof(GameObject), false) as GameObject;
                if (prefabKeeper.Prefab != obj)
                {
                    prefabKeeper.Prefab = obj;
                    needToSave = true;
                }

                Color tempColor = GUI.color;
                GUI.color = Color.red;
                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    WaysCreator.RemoveNameFromPrefabsEnum(itemName.ToString());
                }

                GUI.color = tempColor;
                GUILayout.EndHorizontal();
            }

            if (myTarget.Prefabs.Count > enumPrefabValues.Length)
            {
                for (int i = myTarget.Prefabs.Count - 1; i >= 0; --i)
                {
                    if (Enum.IsDefined(typeof(EPrefabNames), myTarget.Prefabs[i].Name))
                    {
                        continue;
                    }

                    needToSave = true;
                    myTarget.Prefabs.RemoveAt(i);
                    if (myTarget.Prefabs.Count == enumPrefabValues.Length)
                    {
                        break;
                    }
                }
            }

            if (EditorApplication.isCompiling)
            {
                EditorGUILayout.HelpBox("Please wait for compilation to finish", MessageType.Info);
            }

            return needToSave;
        }

        private static bool ResourcesGUI(WaysCreator myTarget)
        {
            GUI.enabled = !EditorApplication.isCompiling && !EditorApplication.isPlaying;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Resources", EditorStyles.boldLabel);

            bool needToSave = false;
            Array enumResourceValues = Enum.GetValues(typeof(EResourceNames));
            foreach (EResourceNames itemName in enumResourceValues)
            {
                GUILayout.BeginHorizontal();
                ResourceKeeper resourceKeeper = myTarget.Resources.Find(p => p.Name == itemName);
                if (resourceKeeper == null)
                {
                    resourceKeeper = new ResourceKeeper { Name = itemName, Resource = null };
                    myTarget.Resources.Add(resourceKeeper);
                    needToSave = true;
                }

                Object obj = EditorGUILayout.ObjectField(itemName.ToString(), resourceKeeper.Resource, typeof(Object), false);
                if (resourceKeeper.Resource != obj)
                {
                    resourceKeeper.Resource = obj;
                    needToSave = true;
                }

                Color tempColor = GUI.color;
                GUI.color = Color.red;
                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    WaysCreator.RemoveNameFromResourcesEnum(itemName.ToString());
                }

                GUI.color = tempColor;
                GUILayout.EndHorizontal();
            }

            if (myTarget.Resources.Count > enumResourceValues.Length)
            {
                for (int i = myTarget.Resources.Count - 1; i >= 0; --i)
                {
                    if (!Enum.IsDefined(typeof(EResourceNames), myTarget.Resources[i].Name))
                    {
                        myTarget.Resources.RemoveAt(i);
                        needToSave = true;
                        if (myTarget.Resources.Count == enumResourceValues.Length)
                        {
                            break;
                        }
                    }
                }
            }

            if (EditorApplication.isCompiling)
            {
                EditorGUILayout.HelpBox("Please wait for compilation to finish", MessageType.Info);
            }

            return needToSave;
        }

        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            if (EditorPrefs.HasKey("NewPrefabsEnumName"))
            {
                GameObject newPrefab = null;
                string newEnumName = EditorPrefs.GetString("NewPrefabsEnumName");
                if (EditorPrefs.HasKey("NewPrefabGUID"))
                {
                    string path = AssetDatabase.GUIDToAssetPath(EditorPrefs.GetString("NewPrefabGUID"));
                    if (!string.IsNullOrEmpty(path))
                    {
                        newPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    }
                }

                WaysCreator.Instance.AddNewPrefab((EPrefabNames)Enum.Parse(typeof(EPrefabNames), newEnumName), newPrefab);
                Debug.Log("Scripts reloaded: " + newEnumName + " added to WaysCreator");
                EditorPrefs.DeleteKey("NewPrefabsEnumName");
                EditorPrefs.DeleteKey("NewPrefabGUID");
            }

            if (EditorPrefs.HasKey("NewResourcesEnumName"))
            {
                Object newResource = null;
                string newEnumName = EditorPrefs.GetString("NewResourcesEnumName");
                if (EditorPrefs.HasKey("NewResourceGUID"))
                {
                    string path = AssetDatabase.GUIDToAssetPath(EditorPrefs.GetString("NewResourceGUID"));
                    if (!string.IsNullOrEmpty(path))
                    {
                        newResource = AssetDatabase.LoadAssetAtPath<Object>(path);
                    }
                }

                WaysCreator.Instance.AddNewResource((EResourceNames)Enum.Parse(typeof(EResourceNames), newEnumName), newResource);
                Debug.Log("Scripts reloaded: " + newEnumName + " added to WaysCreator");
                EditorPrefs.DeleteKey("NewResourcesEnumName");
                EditorPrefs.DeleteKey("NewResourceGUID");
            }
        }

        private void OnUndoRedo()
        {
            AssetDatabase.SaveAssets();
        }
    }

#endif
}