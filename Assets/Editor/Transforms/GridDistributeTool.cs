using UnityEngine;
using UnityEditor;
using System.Linq;

/// <summary>
/// Arranges the currently selected GameObjects around a
/// center point (average position of the selection), in a
/// rectangular grid or the surface of a sphere. Place this script inside an "Editor" Folder
/// (e.g. Assets/Editor/GridDistributeTool.cs).
/// </summary>
public class GridDistributeTool : EditorWindow
{
    private enum DistributionMode { Grid, Sphere }
    private enum DistributionPlane { XY, XZ, YZ }
    private enum SortOrder { HierarchyOrder, Name, CurrentPosition }

    private DistributionMode mode = DistributionMode.Grid;
    private SortOrder sortOrder = SortOrder.HierarchyOrder;

    // ---- Grid settings ----
    private int gridSizeX = 3;   // columns
    private int gridSizeY = 3;   // rows
    private float spacingX = 2f;
    private float spacingY = 2f;
    private DistributionPlane plane = DistributionPlane.XZ;

    // ---- Sphere settings ----
    private float sphereRadius = 5f;
    private bool orientOutward = false;

    
    
    [MenuItem("Tools/Grid Distribute Tool")]
    public static void ShowWindow()
    {
        var window = GetWindow<GridDistributeTool>("Distribute Tool");
        window.minSize = new Vector2(300, 340);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Distribute Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        mode = (DistributionMode)EditorGUILayout.EnumPopup("Distribution Mode", mode);
        EditorGUILayout.Space();

        if (mode == DistributionMode.Grid)
            DrawGridGUI();
        else
            DrawSphereGUI();

        EditorGUILayout.Space();
        sortOrder = (SortOrder)EditorGUILayout.EnumPopup(
            new GUIContent("Object Order", "How selected objects are ordered into grid/sphere slots."),
            sortOrder);

        EditorGUILayout.Space();
        DrawSelectionInfo();

        EditorGUILayout.Space();
        GUI.enabled = Selection.gameObjects.Length > 0;

        if (Selection.gameObjects.Length == 1)
        {
            GUIContent content = new GUIContent("Select more than one object to allow distribution", EditorGUIUtility.IconContent("console.infoicon").image);
            
            GUIStyle infoboxStyle = new GUIStyle(EditorStyles.wordWrappedLabel) 
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
            };
            infoboxStyle.normal.textColor = Color.white;
            
            EditorGUILayout.LabelField(content, infoboxStyle, GUILayout.MinHeight(32), GUILayout.ExpandWidth(true));
        }
        else if (GUILayout.Button("Distribute Selected Objects", GUILayout.Height(32)))
        {
            DistributeSelection();
        }

        GUI.enabled = true;
    }

    // Repaint when a new selection is made. Instantly updates our Tool Window to reflect changes
    private void OnEnable()
    {
        Selection.selectionChanged += Repaint;
    }

    private void OnDisable()
    {
        Selection.selectionChanged -= Repaint;
    }

    private void DrawGridGUI()
    {
        EditorGUILayout.LabelField("Grid Size (columns x rows)", EditorStyles.miniBoldLabel);
        gridSizeX = Mathf.Max(1, EditorGUILayout.IntField("Columns (X)", gridSizeX));
        gridSizeY = Mathf.Max(1, EditorGUILayout.IntField("Rows (Y)", gridSizeY));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Spacing", EditorStyles.miniBoldLabel);
        spacingX = EditorGUILayout.FloatField("Spacing X", spacingX);
        spacingY = EditorGUILayout.FloatField("Spacing Y", spacingY);

        EditorGUILayout.Space();
        plane = (DistributionPlane)EditorGUILayout.EnumPopup(
            new GUIContent("Distribution Plane", "Which world plane the grid is laid out on. Use XZ for a flat ground grid, XY for 2D/UI work."),
            plane);
    }

    private void DrawSphereGUI()
    {
        EditorGUILayout.LabelField("Sphere Settings", EditorStyles.miniBoldLabel);
        
        sphereRadius = Mathf.Max(0.01f, EditorGUILayout.FloatField(
            new GUIContent("Radius", "Distance from the center point to each object."), sphereRadius));
        
        orientOutward = EditorGUILayout.Toggle(
            new GUIContent("Orient Outward", "Rotate each object so its local up direction points away from the sphere's center."),
            orientOutward);
    }

    private void DrawSelectionInfo()
    {
        int selectedCount = Selection.gameObjects.Length;

        if (mode == DistributionMode.Grid)
        {
            int capacity = gridSizeX * gridSizeY;
            EditorGUILayout.HelpBox(
                $"Selected objects: {selectedCount}\nGrid capacity: {capacity} ({gridSizeX} x {gridSizeY})" +
                (selectedCount > capacity ? "\nSelection exceeds capacity — col/rows will both grow to keep an even layout" : ""),
                selectedCount > capacity ? MessageType.Warning : MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox(
                $"Selected objects: {selectedCount}\nWill be spread evenly across the sphere surface using a Fibonacci lattice (no clustering at the poles).",
                MessageType.Info);
        }
    }

    private void DistributeSelection()
    {
        if (Selection.gameObjects.Length == 0) return;

        GameObject[] selected = SortSelection(Selection.gameObjects);
        Vector3 center = ComputeCenter(selected);

        Undo.RecordObjects(selected.Select(g => g.transform).ToArray(), "Distribute Objects");

        if (mode == DistributionMode.Grid)
            DistributeGrid(selected, center);
        else
            DistributeSphere(selected, center);
    }

    private GameObject[] SortSelection(GameObject[] selected)
    {
        switch (sortOrder)
        {
            case SortOrder.Name:
                return selected.OrderBy(g => g.name).ToArray();
            
            case SortOrder.CurrentPosition:
                return selected
                    .OrderBy(g => g.transform.position.x)
                    .ThenBy(g => g.transform.position.y)
                    .ThenBy(g => g.transform.position.z)
                    .ToArray();
            
            default: // Hierarchy Order
                return selected.OrderBy(g => g.transform.GetSiblingIndex()).ToArray();
        }
    }

    private Vector3 ComputeCenter(GameObject[] selected)
    {
        Vector3 center = Vector3.zero;
        
        foreach (var go in selected)
            center += go.transform.position;
        
        return center / selected.Length;
    }

    // ------------------------------------------------------------------
    // Grid distribution
    // ------------------------------------------------------------------
    private void DistributeGrid(GameObject[] selected, Vector3 center)
    {
        int count = selected.Length;

        // Use the user-defined grid size as-is if it already fits. If not,
        // grow BOTH columns and rows together (keeping roughly the same
        // aspect ratio) so extra objects spread as evenly as possible
        // instead of just stacking rows.
        int columns = Mathf.Max(1, gridSizeX);
        int rows = Mathf.Max(1, gridSizeY);
        int capacity = columns * rows;

        if (count > capacity)
        {
            float aspectRatio = rows > 0 ? (float)columns / rows : 1f;
            columns = Mathf.Max(columns, Mathf.CeilToInt(Mathf.Sqrt(count * aspectRatio)));
            rows = Mathf.Max(rows, Mathf.CeilToInt((float)count / columns));

            while (columns * rows < count)
            {
                if (columns <= rows) columns++;
                else rows++;
            }

            gridSizeX = columns;
            gridSizeY = rows;
        }

        float totalWidth = (columns - 1) * spacingX;
        float totalHeight = (rows - 1) * spacingY;

        for (int i = 0; i < count; i++)
        {
            int col = i % columns;
            int row = i / columns;

            float offsetA = col * spacingX - totalWidth * 0.5f;
            float offsetB = totalHeight * 0.5f - row * spacingY; // row 0 at the "top"

            Vector3 newPos = center;

            switch (plane)
            {
                case DistributionPlane.XY:
                    newPos.x = center.x + offsetA;
                    newPos.y = center.y + offsetB;
                    newPos.z = center.z;
                    break;
                case DistributionPlane.XZ:
                    newPos.x = center.x + offsetA;
                    newPos.y = center.y;
                    newPos.z = center.z + offsetB;
                    break;
                case DistributionPlane.YZ:
                    newPos.x = center.x;
                    newPos.y = center.y + offsetA;
                    newPos.z = center.z + offsetB;
                    break;
            }

            selected[i].transform.position = newPos;
        }

        Debug.Log($"[Distribute Tool] Distributed {count} object(s) into a {columns} x {rows} grid centered at {center}.");
    }

    // ------------------------------------------------------------------
    // Sphere distribution
    // ------------------------------------------------------------------
    private void DistributeSphere(GameObject[] selected, Vector3 center)
    {
        int count = selected.Length;

        for (int i = 0; i < count; i++)
        {
            Vector3 dir = FibonacciSpherePoint(i, count);
            selected[i].transform.position = center + dir * sphereRadius;

            if (orientOutward)
                selected[i].transform.rotation = Quaternion.LookRotation(GetTangent(dir), dir);
        }

        Debug.Log($"[Distribute Tool] Distributed {count} object(s) evenly across a sphere of radius {sphereRadius} centered at {center}.");
    }

    /// <summary>
    /// Returns a point on the unit sphere for index i of total, using a
    /// Fibonacci lattice. This spreads points near-uniformly across the
    /// entire sphere surface — including the poles — with no clustering
    /// or banding, which is what makes it look "evenly spaced" visually.
    /// </summary>
    private Vector3 FibonacciSpherePoint(int i, int total)
    {
        if (total <= 1) return Vector3.up;

        float goldenRatio = (1f + Mathf.Sqrt(5f)) * 0.5f;
        float theta = 2f * Mathf.PI * i / goldenRatio;              // azimuthal angle
        float phi = Mathf.Acos(1f - 2f * (i + 0.5f) / total);       // polar angle

        float sinPhi = Mathf.Sin(phi);
        float x = sinPhi * Mathf.Cos(theta);
        float y = sinPhi * Mathf.Sin(theta);
        float z = Mathf.Cos(phi);

        // Remap so the pole axis (z) becomes Unity's world-up (Y).
        return new Vector3(x, z, y);
    }

    /// <summary>
    /// Returns an arbitrary tangent direction perpendicular to normal,
    /// used as the "forward" axis when orienting objects outward.
    /// </summary>
    private Vector3 GetTangent(Vector3 normal)
    {
        Vector3 helper = Mathf.Abs(normal.y) < 0.99f ? Vector3.up : Vector3.right;
        return Vector3.Cross(helper, normal).normalized;
    }
}
