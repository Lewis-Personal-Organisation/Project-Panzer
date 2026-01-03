using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

public static class SceneData
{
    public class LabelEntryBase
    {
        public Color color;
        public float x;
        public float y;
        public float width;
        public float height;
    }
    public class LabelEntryFixed : LabelEntryBase
    {
        public readonly string text;

        public LabelEntryFixed(string text, Color colour, float x, float y, float width = 550f, float height = 25F)
        {
            this.text = text;
            this.color = colour == default ? Color.black : colour;
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }
    }
    public class LabelEntryDynamic :  LabelEntryBase
    {
        public readonly string preText;
        public string dataText;

        public LabelEntryDynamic(string preText, string dataText, Color colour, float x, float y, float width = 550f, float height = 25F)
        {
            this.preText = preText;
            this.dataText = dataText;
            this.color = colour == default ? Color.black : colour;
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }
    }
    
    public static List<LabelEntryDynamic> labels = new List<LabelEntryDynamic>();
    public static List<LabelEntryFixed> labelsFixed = new List<LabelEntryFixed>();
    public static Texture2D texture;
    public static string GetTextFixed(int index) => $"{labelsFixed[index].text}";
    public static Color GetColourFixed(int index) => labelsFixed[index].color;
    public static Rect GetRectFixed(int index) => new Rect(labelsFixed[index].x, labelsFixed[index].y, labelsFixed[index].width, labelsFixed[index].height);
    public static string GetText(int index) => $"{labels[index].preText}{labels[index].dataText}";
    public static Color GetColour(int index) => labels[index].color;
    public static Rect GetRect(int index) => new Rect(labels[index].x, labels[index].y, labels[index].width, labels[index].height);

    /// <summary>
    /// Demo texture rendering
    /// </summary>
    public static void Texture(int width, int height, Color colour)
    {
        texture = new Texture2D(width, height);
        Color[] pixels = new Color[width * height];

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = colour;
        }

        texture.SetPixels(pixels);
        texture.Apply();
    }

    /// <summary>
    /// Used to render a label which is always fixed in value. Use Label() to render a label who's' value might change over time
    /// </summary>
    public static void LabelFixed(string text, float x = 10f, float y = 10, float width = 550f, float height = 25F, Color colour = default)
    {
        bool exists = LabelFixedExists(text) > -1;

        if (!exists)
        {
            labelsFixed.Add(new LabelEntryFixed(text, colour, x, y, width, height));
        }
    }
    
    /// <summary>
    /// Used to render a label with values which might change. Use LabelFixed() for values which don't change
    /// </summary>
    public static void Label(string preText, string dataText, float x = 10f, float y = 10, float width = 550f, float height = 25F, Color colour = default)
    {
        int index = LabelExists(preText);
        
        // If Index present, change its data. Else, Create with data
        if (index != -1)
        {
            labels[index].dataText = dataText;
        }
        else
        {
            labels.Add(new LabelEntryDynamic(preText, dataText, colour, x, y, width, height));
        }
    }
    
    /// <summary>
    /// Checks if a Dynamic Label exists
    /// </summary>
    private static int LabelExists(string preText)
    {
        for (int i = 0; i < labels.Count; i++)
        {
            if (labels[i].preText == preText)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Checks if a FixedLabelExists
    /// </summary>
    private static int LabelFixedExists(string text)
    {
        for (int i = 0; i < labels.Count; i++)
        {
            if (labels[i].dataText == text)
            {
                return i;
            }
        }
        return -1;
    }
}