using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

public class VisibleObjectFinder : EditorWindow
{
    private string searchString = "";
    private Camera targetCamera;
    private bool useSceneViewCamera = true;

    private bool filterByComponent = false;
    private bool invertComponentFilter = false;

    private Type selectedComponentType;

    private readonly List<Type> componentTypes = new();

    [MenuItem("Tools/Visible Object Finder")]
    public static void ShowWindow()
    {
        GetWindow<VisibleObjectFinder>("Visible Object Finder");
    }

    private void OnEnable()
    {
        componentTypes.Clear();

        componentTypes.AddRange(
            AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Array.Empty<Type>(); }
                })
                .Where(t => typeof(Component).IsAssignableFrom(t) && !t.IsAbstract)
                .OrderBy(t => t.Name)
        );
    }

    private void OnGUI()
    {
        GUILayout.Label("Visible Object Finder", EditorStyles.boldLabel);

        useSceneViewCamera = EditorGUILayout.Toggle("Use Scene View Camera", useSceneViewCamera);

        if (!useSceneViewCamera)
        {
            targetCamera = (Camera)EditorGUILayout.ObjectField(
                "Camera", targetCamera, typeof(Camera), true);
        }

        GUILayout.Space(8);

        searchString = EditorGUILayout.TextField("Name Contains", searchString);

        GUILayout.Space(8);

        filterByComponent = EditorGUILayout.Toggle("Filter By Component", filterByComponent);

        if (filterByComponent)
        {
            EditorGUILayout.BeginHorizontal();

            string label = selectedComponentType != null
                ? selectedComponentType.Name
                : "<None Selected>";

            if (GUILayout.Button(label, EditorStyles.popup))
            {
                PopupWindow.Show(
                    GUILayoutUtility.GetRect(0, 0),
                    new ComponentSearchPopup(componentTypes, OnComponentSelected));
            }

            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                selectedComponentType = null;
            }

            EditorGUILayout.EndHorizontal();

            invertComponentFilter = EditorGUILayout.Toggle("Invert (Find Without)", invertComponentFilter);
        }

        GUILayout.Space(15);

        if (GUILayout.Button("Find Visible Objects"))
        {
            FindObjects();
        }
    }

    private void OnComponentSelected(Type type)
    {
        selectedComponentType = type;
        Repaint();
    }

    private void FindObjects()
    {
        Camera cam = useSceneViewCamera
            ? SceneView.lastActiveSceneView?.camera
            : targetCamera;

        if (cam == null)
        {
            Debug.LogWarning("No camera available.");
            return;
        }

        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);

        Renderer[] renderers =
            FindObjectsByType<Renderer>(FindObjectsSortMode.None);

        List<GameObject> matches = new();

        foreach (Renderer r in renderers)
        {
            GameObject go = r.gameObject;

            if (!r.enabled || !go.activeInHierarchy)
                continue;

            if (!GeometryUtility.TestPlanesAABB(planes, r.bounds))
                continue;

            if (!string.IsNullOrWhiteSpace(searchString) &&
                !go.name.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                continue;

            if (filterByComponent && selectedComponentType != null)
            {
                bool hasComponent = go.GetComponent(selectedComponentType) != null;

                if (!invertComponentFilter && !hasComponent)
                    continue;

                if (invertComponentFilter && hasComponent)
                    continue;
            }

            matches.Add(go);
        }

        Selection.objects = matches.ToArray();
        Debug.Log($"Selected {matches.Count} visible objects.");
    }
}