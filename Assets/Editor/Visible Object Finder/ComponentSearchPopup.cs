using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditor.IMGUI.Controls;

public class ComponentSearchPopup : PopupWindowContent
{
    private readonly List<Type> allTypes;
    private readonly Action<Type> onSelect;

    private string search = "";
    private Vector2 scroll;

    private List<Type> filtered;

    private SearchField searchField;

    public ComponentSearchPopup(List<Type> types, Action<Type> onSelect)
    {
        allTypes = types;
        this.onSelect = onSelect;
        filtered = new List<Type>(types);
    }

    public override Vector2 GetWindowSize()
    {
        return new Vector2(300, 400);
    }

    public override void OnOpen()
    {
        searchField = new SearchField();
    }

    public override void OnGUI(Rect rect)
    {
        GUILayout.BeginVertical();

        search = searchField.OnGUI(search);

        Filter();

        scroll = GUILayout.BeginScrollView(scroll);

        foreach (var type in filtered)
        {
            if (GUILayout.Button(type.Name, EditorStyles.label))
            {
                onSelect?.Invoke(type);
                editorWindow.Close();
            }
        }

        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    private void Filter()
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            filtered = allTypes;
            return;
        }

        filtered = allTypes
            .Where(t => t.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
            .ToList();
    }
}