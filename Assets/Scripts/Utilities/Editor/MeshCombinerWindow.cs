using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class MeshCombinerWindow : EditorWindow
{
    private GameObject root;

    [MenuItem("Tools/Mesh Combiner")]
    static void Open()
    {
        GetWindow<MeshCombinerWindow>("Mesh Combiner");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Combine Child Meshes", EditorStyles.boldLabel);

        root = (GameObject)EditorGUILayout.ObjectField(
            "Root Object",
            root,
            typeof(GameObject),
            true);

        GUI.enabled = root != null;

        if (GUILayout.Button("Combine Meshes"))
        {
            Combine(root);
        }

        GUI.enabled = true;
    }

    static void Combine(GameObject root)
    {
        MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>();

        List<CombineInstance> combines = new List<CombineInstance>();
        Material material = null;

        foreach (MeshFilter filter in filters)
        {
            if (filter.sharedMesh == null)
                continue;

            if (filter.transform == root.transform)
                continue;

            MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
            if (renderer != null && renderer.sharedMaterial != null && material == null)
                material = renderer.sharedMaterial;

            combines.Add(new CombineInstance
            {
                mesh = filter.sharedMesh,
                transform = root.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix
            });
        }

        if (combines.Count == 0)
        {
            EditorUtility.DisplayDialog("Mesh Combiner", "No meshes found.", "OK");
            return;
        }

        Mesh mesh = new Mesh
        {
            name = root.name + "_Combined",
            indexFormat = IndexFormat.UInt32
        };

        mesh.CombineMeshes(combines.ToArray(), true, true);

        // Center the pivot.
        Vector3 center = mesh.bounds.center;

        Vector3[] vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] -= center;
        }

        mesh.vertices = vertices;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();

        string path = EditorUtility.SaveFilePanelInProject(
            "Save Combined Mesh",
            mesh.name,
            "asset",
            "Choose where to save the combined mesh.");

        if (string.IsNullOrEmpty(path))
            return;

        AssetDatabase.CreateAsset(mesh, path);
        AssetDatabase.SaveAssets();

        GameObject combined = new GameObject(mesh.name);

        combined.transform.position = root.transform.TransformPoint(center);
        combined.transform.rotation = root.transform.rotation;
        combined.transform.localScale = root.transform.lossyScale;

        MeshFilter mf = combined.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        MeshRenderer mr = combined.AddComponent<MeshRenderer>();
        mr.sharedMaterial = material;

        Undo.RegisterCreatedObjectUndo(combined, "Create Combined Mesh");

        Selection.activeGameObject = combined;

        EditorGUIUtility.PingObject(combined);
    }
}