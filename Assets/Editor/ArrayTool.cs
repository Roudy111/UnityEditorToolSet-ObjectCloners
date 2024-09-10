using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ArrayTool : EditorWindow
{
    private List<ArraySet> arraySets = new List<ArraySet>();
    private int selectedArraySetIndex = -1;
    private Vector2 scrollPosition;
    private bool showArraySetList = true;

    // Array settings
    private GameObject prefab;
    private ArrayMode mode = ArrayMode.Grid;
    private int rows = 1;
    private int columns = 1;
    private float spacing = 1.0f;
    private int numberOfObjects = 10;
    private float radius = 5f;
    private Vector3 rotationOffset = Vector3.zero;
    private bool randomizeRotation = false;
    private Vector3 scaleMultiplier = Vector3.one;
    private bool randomizeScale = false;

    public enum ArrayMode { Grid, Circle }

    [MenuItem("MyTools/Advanced Array Tool")]
    public static void ShowWindow()
    {
        GetWindow<ArrayTool>("Advanced Array Tool");
    }

    private void OnEnable()
    {
        LoadArraySets();
    }

    private void OnDisable()
    {
        SaveArraySets();
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        DrawArraySetList();
        DrawArraySettings();

        if (GUILayout.Button("Cleanup All Array Sets"))
        {
            if (EditorUtility.DisplayDialog("Cleanup Confirmation",
                "Are you sure you want to delete all array sets? This action cannot be undone.",
                "Yes", "No"))
            {
                CleanupArraySets();
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawArraySetList()
    {
        EditorGUILayout.BeginHorizontal();
        showArraySetList = EditorGUILayout.Foldout(showArraySetList, "Array Sets", true);
        if (GUILayout.Button("Create New Array Set", GUILayout.Width(150)))
        {
            CreateNewArraySet();
        }
        EditorGUILayout.EndHorizontal();

        if (showArraySetList)
        {
            EditorGUI.indentLevel++;
            for (int i = 0; i < arraySets.Count; i++)
            {
                ArraySet set = arraySets[i];
                EditorGUILayout.BeginHorizontal();

                string newName = EditorGUILayout.TextField(set.Name);
                if (newName != set.Name)
                {
                    set.Name = newName;
                    if (set.ParentObject != null)
                    {
                        set.ParentObject.name = newName;
                    }
                    EditorUtility.SetDirty(set.ParentObject);
                }

                if (GUILayout.Button("Edit", GUILayout.Width(60)))
                {
                    selectedArraySetIndex = i;
                    LoadArraySetSettings(set);
                }
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    DeleteArraySet(i);
                    EditorGUILayout.EndHorizontal();
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
    }

    private void DrawArraySettings()
    {
        EditorGUILayout.LabelField("Array Settings", EditorStyles.boldLabel);

        prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", prefab, typeof(GameObject), false);
        mode = (ArrayMode)EditorGUILayout.EnumPopup("Mode", mode);

        if (mode == ArrayMode.Grid)
        {
            rows = EditorGUILayout.IntField("Rows", rows);
            columns = EditorGUILayout.IntField("Columns", columns);
            spacing = EditorGUILayout.FloatField("Spacing", spacing);
        }
        else
        {
            numberOfObjects = EditorGUILayout.IntField("Number of Objects", numberOfObjects);
            radius = EditorGUILayout.FloatField("Radius", radius);
        }

        rotationOffset = EditorGUILayout.Vector3Field("Rotation Offset", rotationOffset);
        randomizeRotation = EditorGUILayout.Toggle("Randomize Rotation", randomizeRotation);
        scaleMultiplier = EditorGUILayout.Vector3Field("Scale Multiplier", scaleMultiplier);
        randomizeScale = EditorGUILayout.Toggle("Randomize Scale", randomizeScale);

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(selectedArraySetIndex == -1 ? "Generate New Array Set" : "Update Array Set"))
        {
            if (selectedArraySetIndex == -1)
            {
                CreateNewArraySet();
            }
            else
            {
                UpdateArraySet(selectedArraySetIndex);
            }
        }
        if (selectedArraySetIndex != -1 && GUILayout.Button("Cancel Editing"))
        {
            selectedArraySetIndex = -1;
        }
        EditorGUILayout.EndHorizontal();
    }

    private void CreateNewArraySet()
    {
        if (prefab == null) return;

        string newName = $"Array_Set_{arraySets.Count + 1}";
        GameObject parentObject = new GameObject(newName);
        Undo.RegisterCreatedObjectUndo(parentObject, "Create Array Set");

        ArraySet newSet = ScriptableObject.CreateInstance<ArraySet>();
        newSet.Initialize(parentObject, prefab, mode, rows, columns, spacing, numberOfObjects, radius,
                          rotationOffset, randomizeRotation, scaleMultiplier, randomizeScale);
        newSet.Name = newName;

        GenerateArraySet(newSet);
        arraySets.Add(newSet);
        selectedArraySetIndex = arraySets.Count - 1;

        EditorUtility.SetDirty(newSet);
        AssetDatabase.CreateAsset(newSet, $"Assets/ArraySet_{newSet.GetInstanceID()}.asset");
        AssetDatabase.SaveAssets();
    }

    private void UpdateArraySet(int index)
    {
        if (index < 0 || index >= arraySets.Count) return;

        ArraySet set = arraySets[index];
        set.UpdateSettings(prefab, mode, rows, columns, spacing, numberOfObjects, radius,
                           rotationOffset, randomizeRotation, scaleMultiplier, randomizeScale);

        ClearArraySet(set);
        GenerateArraySet(set);
        EditorUtility.SetDirty(set);
        AssetDatabase.SaveAssets();
    }

    private void DeleteArraySet(int index)
    {
        if (index < 0 || index >= arraySets.Count) return;

        ArraySet set = arraySets[index];
        if (set.ParentObject != null)
        {
            Undo.DestroyObjectImmediate(set.ParentObject);
        }
        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(set));
        arraySets.RemoveAt(index);

        if (selectedArraySetIndex == index)
        {
            selectedArraySetIndex = -1;
        }
        else if (selectedArraySetIndex > index)
        {
            selectedArraySetIndex--;
        }

        AssetDatabase.SaveAssets();
    }

    private void LoadArraySetSettings(ArraySet set)
    {
        prefab = set.Prefab;
        mode = set.Mode;
        rows = set.Rows;
        columns = set.Columns;
        spacing = set.Spacing;
        numberOfObjects = set.NumberOfObjects;
        radius = set.Radius;
        rotationOffset = set.RotationOffset;
        randomizeRotation = set.RandomizeRotation;
        scaleMultiplier = set.ScaleMultiplier;
        randomizeScale = set.RandomizeScale;
    }

    private void GenerateArraySet(ArraySet set)
    {
        if (set.Mode == ArrayMode.Grid)
        {
            GenerateGrid(set);
        }
        else
        {
            GenerateCircle(set);
        }
    }

    private void GenerateGrid(ArraySet set)
    {
        for (int y = 0; y < set.Rows; y++)
        {
            for (int x = 0; x < set.Columns; x++)
            {
                Vector3 position = new Vector3(x * set.Spacing, 0, y * set.Spacing);
                InstantiatePrefab(set, position, Quaternion.identity);
            }
        }
    }

    private void GenerateCircle(ArraySet set)
    {
        for (int i = 0; i < set.NumberOfObjects; i++)
        {
            float angle = i * Mathf.PI * 2 / set.NumberOfObjects;
            float x = Mathf.Cos(angle) * set.Radius;
            float z = Mathf.Sin(angle) * set.Radius;
            Vector3 position = new Vector3(x, 0, z);
            Quaternion rotation = Quaternion.Euler(0, -angle * Mathf.Rad2Deg, 0);
            InstantiatePrefab(set, position, rotation);
        }
    }

    private void InstantiatePrefab(ArraySet set, Vector3 position, Quaternion rotation)
    {
        GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(set.Prefab);
        Undo.RegisterCreatedObjectUndo(obj, "Create " + obj.name);

        obj.transform.SetParent(set.ParentObject.transform);
        obj.transform.position = position;

        Quaternion finalRotation = rotation * Quaternion.Euler(set.RotationOffset);
        if (set.RandomizeRotation)
        {
            finalRotation *= Quaternion.Euler(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f));
        }
        obj.transform.rotation = finalRotation;

        Vector3 finalScale = Vector3.Scale(obj.transform.localScale, set.ScaleMultiplier);
        if (set.RandomizeScale)
        {
            finalScale = Vector3.Scale(finalScale, new Vector3(Random.Range(0.5f, 1.5f), Random.Range(0.5f, 1.5f), Random.Range(0.5f, 1.5f)));
        }
        obj.transform.localScale = finalScale;

        set.AddCreatedObject(obj);
    }

    private void ClearArraySet(ArraySet set)
    {
        set.ClearCreatedObjects();
    }

    private void CleanupArraySets()
    {
        foreach (ArraySet set in arraySets)
        {
            if (set.ParentObject != null)
            {
                DestroyImmediate(set.ParentObject);
            }
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(set));
        }
        arraySets.Clear();
        AssetDatabase.SaveAssets();
    }

    private void SaveArraySets()
    {
        string data = JsonUtility.ToJson(new SerializableArraySetList { sets = arraySets });
        EditorPrefs.SetString("ArrayToolData", data);
    }

    private void LoadArraySets()
    {
        string data = EditorPrefs.GetString("ArrayToolData", "");
        if (!string.IsNullOrEmpty(data))
        {
            SerializableArraySetList loadedData = JsonUtility.FromJson<SerializableArraySetList>(data);
            arraySets = loadedData.sets;
        }
    }
}

[System.Serializable]
public class ArraySet : ScriptableObject
{
    public string Name;
    public GameObject ParentObject;
    public GameObject Prefab;
    public ArrayTool.ArrayMode Mode;
    public int Rows;
    public int Columns;
    public float Spacing;
    public int NumberOfObjects;
    public float Radius;
    public Vector3 RotationOffset;
    public bool RandomizeRotation;
    public Vector3 ScaleMultiplier;
    public bool RandomizeScale;
    [SerializeField] private List<GameObject> CreatedObjects = new List<GameObject>();

    public void Initialize(GameObject parentObject, GameObject prefab, ArrayTool.ArrayMode mode, int rows, int columns,
                    float spacing, int numberOfObjects, float radius, Vector3 rotationOffset, bool randomizeRotation,
                    Vector3 scaleMultiplier, bool randomizeScale)
    {
        ParentObject = parentObject;
        UpdateSettings(prefab, mode, rows, columns, spacing, numberOfObjects, radius,
                       rotationOffset, randomizeRotation, scaleMultiplier, randomizeScale);
    }

    public void UpdateSettings(GameObject prefab, ArrayTool.ArrayMode mode, int rows, int columns,
                               float spacing, int numberOfObjects, float radius, Vector3 rotationOffset,
                               bool randomizeRotation, Vector3 scaleMultiplier, bool randomizeScale)
    {
        Prefab = prefab;
        Mode = mode;
        Rows = rows;
        Columns = columns;
        Spacing = spacing;
        NumberOfObjects = numberOfObjects;
        Radius = radius;
        RotationOffset = rotationOffset;
        RandomizeRotation = randomizeRotation;
        ScaleMultiplier = scaleMultiplier;
        RandomizeScale = randomizeScale;
    }

    public void AddCreatedObject(GameObject obj)
    {
        CreatedObjects.Add(obj);
    }

    public void ClearCreatedObjects()
    {
        foreach (GameObject obj in CreatedObjects)
        {
            if (obj != null)
            {
                DestroyImmediate(obj);
            }
        }
        CreatedObjects.Clear();
    }
}

[System.Serializable]
public class SerializableArraySetList
{
    public List<ArraySet> sets;
}