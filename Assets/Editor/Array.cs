using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace AdvancedArrayTool
{
    public class ArrayTool : EditorWindow
    {
        private List<IArrayGenerator> generators = new List<IArrayGenerator>();
        private int selectedGeneratorIndex = -1;
        private Vector2 scrollPosition;

        private SerializedObject serializedObject;
        private SerializedProperty selectedGeneratorProperty;

        // Dictionary to store generated arrays
        private Dictionary<string, GameObject> generatedArrays = new Dictionary<string, GameObject>();

        [MenuItem("Window/Advanced Array Tool")]
        public static void ShowWindow()
        {
            GetWindow<ArrayTool>("Advanced Array Tool");
        }

        private void OnEnable()
        {
            serializedObject = new SerializedObject(this);
            selectedGeneratorProperty = serializedObject.FindProperty("selectedGeneratorIndex");
            LoadGenerators();
        }

        private void OnDisable()
        {
            SaveGenerators();
        }

        private void OnGUI()
        {
            serializedObject.Update();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawGeneratorList();
            DrawGeneratorSettings();

            if (GUILayout.Button("Generate Array"))
            {
                GenerateArray();
            }
            // Add button to clear all generated arrays
            if (GUILayout.Button("Clear All Arrays"))
            {
                ClearAllArrays();
            }

            EditorGUILayout.EndScrollView();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawGeneratorList()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Array Generators", EditorStyles.boldLabel);
            if (GUILayout.Button("Add Generator", GUILayout.Width(100)))
            {
                AddNewGenerator();
            }
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < generators.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Toggle(selectedGeneratorIndex == i, generators[i].Name, EditorStyles.toolbarButton))
                {
                    selectedGeneratorIndex = i;
                }
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    RemoveGenerator(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawGeneratorSettings()
        {
            if (selectedGeneratorIndex >= 0 && selectedGeneratorIndex < generators.Count)
            {
                IArrayGenerator generator = generators[selectedGeneratorIndex];
                generator.DrawSettings();
            }
        }

        private void AddNewGenerator()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Grid Generator"), false, () => CreateGenerator<GridArrayGenerator>());
            menu.AddItem(new GUIContent("Circle Generator"), false, () => CreateGenerator<CircleArrayGenerator>());
            menu.ShowAsContext();
        }

        private void CreateGenerator<T>() where T : IArrayGenerator, new()
        {
            T newGenerator = new T();
            newGenerator.Initialize();
            generators.Add(newGenerator);
            selectedGeneratorIndex = generators.Count - 1;
        }

        private void RemoveGenerator(int index)
        {
            if (index >= 0 && index < generators.Count)
            {
                generators.RemoveAt(index);
                if (selectedGeneratorIndex >= generators.Count)
                {
                    selectedGeneratorIndex = generators.Count - 1;
                }
            }
        }

        private void GenerateArray()
        {
            if (selectedGeneratorIndex >= 0 && selectedGeneratorIndex < generators.Count)
            {
                IArrayGenerator generator = generators[selectedGeneratorIndex];

                // Create a parent object for the array
                GameObject parentObject = new GameObject($"{generator.Name} Array");
                Undo.RegisterCreatedObjectUndo(parentObject, "Create Array Parent");


                // Generate the array and set the parent
                generator.Generate(parentObject.transform);

                // Store the generated array
                string key = $"{generator.Name}_{System.Guid.NewGuid()}";
                generatedArrays[key] = parentObject;
            }
        }

        // Method to clear all generated arrays
        private void ClearAllArrays()
        {
            foreach (var array in generatedArrays.Values)
            {
                if (array != null)
                {
                    Undo.DestroyObjectImmediate(array);
                }
            }
            generatedArrays.Clear();
        }

        private void SaveGenerators()
        {
            string data = JsonUtility.ToJson(new SerializableGeneratorList { generators = generators.Cast<ArrayGeneratorBase>().ToList() });
            EditorPrefs.SetString("AdvancedArrayToolData", data);
        }

        private void LoadGenerators()
        {
            string data = EditorPrefs.GetString("AdvancedArrayToolData", "");
            if (!string.IsNullOrEmpty(data))
            {
                SerializableGeneratorList loadedData = JsonUtility.FromJson<SerializableGeneratorList>(data);
                if (loadedData != null && loadedData.generators != null)
                {
                    generators = loadedData.generators.Cast<IArrayGenerator>().ToList();

                }
                else
                {
                    generators = new List<IArrayGenerator>();
                }
            }

            else
            {
                generators = new List<IArrayGenerator>();

            }
        }
    }

    public interface IArrayGenerator
    {
        string Name { get; set; }
        void Initialize();
        void DrawSettings();
        void Generate(Transform parent);
    }

    [System.Serializable]
    public abstract class ArrayGeneratorBase : IArrayGenerator
    {
        public string Name { get; set; }
        public GameObject Prefab;
        public Vector3 PositionOffset;
        public Vector3 RotationOffset;
        public Vector3 ScaleMultiplier = Vector3.one;
        public bool RandomizeRotation;
        public bool RandomizeScale;

        public abstract void Initialize();
        public abstract void DrawSettings();
        public abstract void Generate(Transform parent);

        protected void InstantiatePrefab(Vector3 position, Quaternion rotation, Transform parent)
        {
            if (Prefab == null) return;

            GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(Prefab);
            Undo.RegisterCreatedObjectUndo(obj, "Create Array Object");

            obj.transform.position = position + PositionOffset;
            obj.transform.SetParent(parent, true);

            Quaternion finalRotation = rotation * Quaternion.Euler(RotationOffset);
            if (RandomizeRotation)
            {
                finalRotation *= Quaternion.Euler(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f));
            }
            obj.transform.rotation = finalRotation;

            Vector3 finalScale = Vector3.Scale(obj.transform.localScale, ScaleMultiplier);
            if (RandomizeScale)
            {
                finalScale = Vector3.Scale(finalScale, new Vector3(Random.Range(0.5f, 1.5f), Random.Range(0.5f, 1.5f), Random.Range(0.5f, 1.5f)));
            }
            obj.transform.localScale = finalScale;
        }
    }

    [System.Serializable]
    public class GridArrayGenerator : ArrayGeneratorBase
    {
        public int Rows = 1;
        public int Columns = 1;
        public int Ylayers = 1;
        public float Spacing = 1f;

        public override void Initialize()
        {
            Name = "Grid Generator";
        }

        public override void DrawSettings()
        {
            Prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", Prefab, typeof(GameObject), false);
            Rows = EditorGUILayout.IntField("Rows", Rows);
            Columns = EditorGUILayout.IntField("Columns", Columns);
            Ylayers = EditorGUILayout.IntField("Y Layers", Ylayers);
            Spacing = EditorGUILayout.FloatField("Spacing", Spacing);
            PositionOffset = EditorGUILayout.Vector3Field("Position Offset", PositionOffset);
            RotationOffset = EditorGUILayout.Vector3Field("Rotation Offset", RotationOffset);
            ScaleMultiplier = EditorGUILayout.Vector3Field("Scale Multiplier", ScaleMultiplier);
            RandomizeRotation = EditorGUILayout.Toggle("Randomize Rotation", RandomizeRotation);
            RandomizeScale = EditorGUILayout.Toggle("Randomize Scale", RandomizeScale);
        }

        public override void Generate(Transform parent)
        {
            for (int z = 0; z< Ylayers; z++)
            {
                for(int y= 0; y< Rows; y++)
                {
                    for(int x= 0; x< Columns; x++)
                    {
                        Vector3 position = new Vector3(x * Spacing , y *Spacing , z * Spacing );
                        InstantiatePrefab(position, Quaternion.identity, parent);
                    }
                }
            }

        }
        
    }

    [System.Serializable]
    public class CircleArrayGenerator : ArrayGeneratorBase
    {
        public int NumberOfObjects = 10;
        public float Radius = 5f;

        public override void Initialize()
        {
            Name = "Circle Generator";
        }

        public override void DrawSettings()
        {
            Prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", Prefab, typeof(GameObject), false);
            NumberOfObjects = EditorGUILayout.IntField("Number of Objects", NumberOfObjects);
            Radius = EditorGUILayout.FloatField("Radius", Radius);
            PositionOffset = EditorGUILayout.Vector3Field("Position Offset", PositionOffset);
            RotationOffset = EditorGUILayout.Vector3Field("Rotation Offset", RotationOffset);
            ScaleMultiplier = EditorGUILayout.Vector3Field("Scale Multiplier", ScaleMultiplier);
            RandomizeRotation = EditorGUILayout.Toggle("Randomize Rotation", RandomizeRotation);
            RandomizeScale = EditorGUILayout.Toggle("Randomize Scale", RandomizeScale);
        }

        public override void Generate(Transform parent)
        {
            for (int i = 0; i < NumberOfObjects; i++)
            {
                float angle = i * Mathf.PI * 2 / NumberOfObjects;
                float x = Mathf.Cos(angle) * Radius;
                float z = Mathf.Sin(angle) * Radius;
                Vector3 position = new Vector3(x, 0, z);
                Quaternion rotation = Quaternion.Euler(0, -angle * Mathf.Rad2Deg, 0);
                InstantiatePrefab(position, rotation, parent);
            }
        }
    }

    [System.Serializable]
    public class SerializableGeneratorList
    {
        public List<ArrayGeneratorBase> generators;
    }
}